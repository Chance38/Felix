using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Felix.Infrastructure.AI.Tools;
using Felix.Infrastructure.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;

namespace Felix.Infrastructure.AI;

public partial class Felix(
    IKernelFactory kernelFactory,
    IGeminiKeyManager keyManager,
    IRequestContext requestContext,
    IMcpClientManager mcpClientManager,
    IEnumerable<ILocalTool> localTools,
    ILogger<Felix> logger) : IFelix
{
    private readonly Dictionary<string, ILocalTool> _localTools = localTools.ToDictionary(t => t.Name);
    private const int MaxToolCalls = 5;

    private const string SystemPromptTemplate = """
        你是 Felix，一位私人管家。繁體中文，語氣簡潔專業並帶有人性。

        ## 工具使用

        需要查詢資料時，只回覆 JSON（不加其他文字）：
        ```json
        {{"tool": "工具名稱", "args": {{"參數名": "值"}}}}
        ```

        可用工具：
        {0}

        ## 回覆原則

        根據用戶的問題類型，參考對應的 Skill 說明來回覆。
        """;

    private static readonly Lazy<string> Skills = new(LoadSkills);

    [GeneratedRegex(@"\{.*""tool"".*\}", RegexOptions.Singleline)]
    private static partial Regex ToolCallRegex();

    public async Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                var currentKey = keyManager.GetCurrentKey();
                var kernel = kernelFactory.CreateKernel(currentKey);

                var result = await ExecuteAsync(kernel, userMessage, cancellationToken);
                keyManager.IncrementRequestCount();
                return result;
            }
            catch (HttpOperationException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                keyManager.IncrementRequestCount();
                if (!keyManager.TryNextKey())
                {
                    return "抱歉，目前服務繁忙，請稍後再試。";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "處理用戶訊息時發生錯誤");
                return "抱歉，處理過程中發生問題，請稍後再試。";
            }
        }
    }

    private async Task<string> ExecuteAsync(Kernel kernel, string userMessage, CancellationToken cancellationToken)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // 建立工具清單和 MCP 工具映射
        var (toolList, mcpToolMap) = await BuildToolListAsync(cancellationToken);
        var systemPrompt = string.Format(SystemPromptTemplate, toolList);
        var fullSystemPrompt = systemPrompt + "\n\n" + Skills.Value;
        var fullUserMessage = BuildUserMessage(userMessage);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(fullSystemPrompt);
        chatHistory.AddUserMessage(fullUserMessage);

        // 多輪對話：AI 可能需要呼叫多個工具
        for (var i = 0; i < MaxToolCalls; i++)
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                kernel: kernel,
                cancellationToken: cancellationToken);

            var content = response.Content ?? "";

            if (TryParseToolCall(content, out var toolName, out var args))
            {
                // 呼叫工具（本地或 MCP）
                var toolResult = await CallToolAsync(toolName, args, mcpToolMap, cancellationToken);

                // 把結果加入對話歷史，繼續下一輪
                chatHistory.AddAssistantMessage(content);
                chatHistory.AddUserMessage($"工具執行結果：\n{toolResult}");
            }
            else
            {
                return content;
            }
        }

        return "抱歉，已嘗試多次但仍無法完成您的請求，請換個方式描述或稍後再試。";
    }

    /// <summary>
    /// 解析 AI 回應中的工具呼叫 JSON
    /// </summary>
    private static bool TryParseToolCall(string content, out string toolName, out Dictionary<string, JsonElement> args)
    {
        toolName = "";
        args = [];

        var jsonMatch = ToolCallRegex().Match(content);
        if (!jsonMatch.Success)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonMatch.Value);
            var root = doc.RootElement;

            if (root.TryGetProperty("tool", out var toolProp))
            {
                toolName = toolProp.GetString() ?? "";
            }

            if (root.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in argsProp.EnumerateObject())
                {
                    args[prop.Name] = prop.Value.Clone();
                }
            }

            return !string.IsNullOrEmpty(toolName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 呼叫工具（先查本地，再查 MCP）
    /// </summary>
    private async Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, JsonElement> args,
        Dictionary<string, IMcpClient> mcpToolMap,
        CancellationToken cancellationToken)
    {
        // 本地工具
        if (_localTools.TryGetValue(toolName, out var localTool))
        {
            return await localTool.ExecuteAsync(args, cancellationToken);
        }

        if (mcpToolMap.TryGetValue(toolName, out var client))
        {
            try
            {
                var result = await client.CallToolAsync(
                    toolName,
                    args.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonElement(kvp.Value)),
                    cancellationToken: cancellationToken);

                return string.Join("\n", result.Content
                    .Where(c => c.Type == "text")
                    .Select(c => c.Text));
            }
            catch (Exception ex)
            {
                return $"工具執行失敗：{ex.Message}";
            }
        }

        return $"找不到工具：{toolName}";
    }

    /// <summary>
    /// 將 JsonElement 轉換為一般物件
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private string BuildUserMessage(string userMessage)
    {
        if (requestContext.Location == null)
        {
            return userMessage;
        }

        var loc = requestContext.Location;
        return $"{userMessage}\n\n（用戶目前位置：緯度 {loc.Latitude}, 經度 {loc.Longitude}）";
    }

    private async Task<(string ToolList, Dictionary<string, IMcpClient> McpToolMap)> BuildToolListAsync(CancellationToken cancellationToken)
    {
        var tools = new List<string>();
        var mcpToolMap = new Dictionary<string, IMcpClient>();

        // 本地工具
        foreach (var tool in _localTools.Values)
        {
            tools.Add($"- {tool.Name}");
        }

        // MCP 工具
        foreach (var client in mcpClientManager.GetClients())
        {
            var mcpTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            foreach (var tool in mcpTools)
            {
                var description = string.IsNullOrEmpty(tool.Description) ? "" : $": {tool.Description}";
                tools.Add($"- {tool.Name}{description}");
                mcpToolMap[tool.Name] = client;
            }
        }

        return (string.Join("\n", tools), mcpToolMap);
    }

    private static string LoadSkills()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

        var skills = new List<string>();
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            skills.Add(reader.ReadToEnd());
        }

        return string.Join("\n\n---\n\n", skills);
    }
}
