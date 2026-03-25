using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Felix.Infrastructure.AI.Tools;
using Felix.Infrastructure.Mcp;
using Felix.Infrastructure.Persistence.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;

namespace Felix.Infrastructure.AI;

public partial class Felix(
    IAiModelManager aiModelManager,
    IRequestContext requestContext,
    IMcpClientManager mcpClientManager,
    IRedisContext redisContext,
    IEnumerable<ILocalTool> localTools,
    ILogger<Felix> logger) : IFelix
{
    private readonly Dictionary<string, ILocalTool> _localTools = localTools.ToDictionary(t => t.Name);
    private const int MaxToolCalls = 5;

    private const string SystemPromptTemplate = """
        你是 Felix，一位私人管家。繁體中文，語氣簡潔專業並帶有人性。
        你非常有條理和邏輯，你在遇到需求時會先分析需求，並確認自己擁有的 Skill 中有沒有符合的情境，
        都沒有的時候才會使用手上的工具靈機應變

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

    // 問題一修正：tool list、mcpToolMap、system prompt 在 runtime 不會改變，cache 起來
    private string? _cachedSystemPrompt;
    private Dictionary<string, IMcpClient>? _cachedMcpToolMap;

    [GeneratedRegex(@"\{.*""tool"".*\}", RegexOptions.Singleline)]
    private static partial Regex ToolCallRegex();

    private (string SystemPrompt, Dictionary<string, IMcpClient> McpToolMap) GetCachedContext()
    {
        if (_cachedSystemPrompt != null && _cachedMcpToolMap != null)
            return (_cachedSystemPrompt, _cachedMcpToolMap);

        var tools = new List<string>();
        var mcpToolMap = new Dictionary<string, IMcpClient>();

        foreach (var tool in _localTools.Values)
            tools.Add($"- {tool.Name}: {tool.Description}");

        foreach (var (client, mcpTools) in mcpClientManager.GetCachedTools())
        {
            foreach (var tool in mcpTools)
            {
                var description = string.IsNullOrEmpty(tool.Description) ? "" : $": {tool.Description}";
                tools.Add($"- {tool.Name}{description}");
                mcpToolMap[tool.Name] = client;
            }
        }

        var toolList = string.Join("\n", tools);
        _cachedSystemPrompt = string.Format(SystemPromptTemplate, toolList) + "\n\n" + Skills.Value;
        _cachedMcpToolMap = mcpToolMap;

        return (_cachedSystemPrompt, _cachedMcpToolMap);
    }

    private async Task<ConversationHistory> TryGetHistoryAsync(string conversationId)
    {
        try
        {
            return await redisContext.Conversations.GetAsync(conversationId) ?? new ConversationHistory();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "無法載入對話歷史，將使用空白歷史");
            return new ConversationHistory();
        }
    }

    private async Task TrySaveHistoryAsync(string conversationId, ConversationHistory history)
    {
        try
        {
            await redisContext.Conversations.SaveAsync(conversationId, history);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "無法儲存對話歷史");
        }
    }

    private static bool TryParseToolCall(string content, out string toolName, out Dictionary<string, JsonElement> args)
    {
        toolName = "";
        args = [];

        var jsonMatch = ToolCallRegex().Match(content);
        if (!jsonMatch.Success)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(jsonMatch.Value);
            var root = doc.RootElement;

            if (root.TryGetProperty("tool", out var toolProp))
                toolName = toolProp.GetString() ?? "";

            if (root.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in argsProp.EnumerateObject())
                    args[prop.Name] = prop.Value.Clone();
            }

            return !string.IsNullOrEmpty(toolName);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, JsonElement> args,
        Dictionary<string, IMcpClient> mcpToolMap,
        CancellationToken cancellationToken)
    {
        if (_localTools.TryGetValue(toolName, out var localTool))
            return await localTool.ExecuteAsync(args, cancellationToken);

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

    // 問題五修正：改用 StringBuilder 避免 string += 產生多餘 allocation
    private string BuildUserMessage(string userMessage)
    {
        var now = DateTime.Now;
        var sb = new StringBuilder();
        sb.Append(userMessage);
        sb.Append($"\n\n（現在時間：{now:yyyy-MM-dd HH:mm}，{GetTimeOfDay(now)}）");

        if (requestContext.Location != null)
        {
            var loc = requestContext.Location;
            sb.Append($"\n（用戶位置：緯度 {loc.Latitude}, 經度 {loc.Longitude}）");
        }

        return sb.ToString();
    }

    private static string GetTimeOfDay(DateTime time) => time.Hour switch
    {
        >= 6 and < 12 => "早上",
        >= 12 and < 18 => "下午",
        _ => "晚上"
    };

    public async IAsyncEnumerable<StreamChunk> ProcessStreamAsync(
        string userMessage,
        string? conversationId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var convId = string.IsNullOrEmpty(conversationId)
            ? Guid.NewGuid().ToString("N")[..16]
            : conversationId;

        var history = await TryGetHistoryAsync(convId);
        var kernelResult = await aiModelManager.GetCurrentKernelAsync();

        if (kernelResult.IsFailed)
        {
            yield return new StreamChunk($"抱歉，AI 服務設定有誤：{kernelResult.Error}");
            yield return new StreamChunk("", IsDone: true, ConversationId: convId);
            yield break;
        }

        var kernel = kernelResult.Value!;
        var fullResponse = new StringBuilder();
        // 問題三修正：把 error 封裝成 local function，讓主流程結構更清晰
        string? streamError = null;

        await foreach (var chunk in ExecuteStreamAsync(kernel, userMessage, history, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (chunk.IsError)
            {
                streamError = chunk.Content;
                break;
            }

            fullResponse.Append(chunk.Content);
            yield return new StreamChunk(chunk.Content);
        }

        if (streamError != null)
        {
            yield return new StreamChunk(streamError);
        }
        else
        {
            // 問題二修正：只在成功時才存歷史，避免存入空的 assistant message
            history.AddUserMessage(userMessage);
            history.AddAssistantMessage(fullResponse.ToString());
            _ = TrySaveHistoryAsync(convId, history);
        }

        yield return new StreamChunk("", IsDone: true, ConversationId: convId);
    }

    private readonly record struct StreamItem(string Content, bool IsError = false);

    /// <summary>
    /// 串流執行 AI 對話。對於工具呼叫（短 JSON）先緩衝後執行；最終回覆則即時串流輸出。
    /// </summary>
    private async IAsyncEnumerable<StreamItem> ExecuteStreamAsync(
        Kernel kernel,
        string userMessage,
        ConversationHistory history,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var (systemPrompt, mcpToolMap) = GetCachedContext();
        var fullUserMessage = BuildUserMessage(userMessage);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        foreach (var msg in history.Messages)
        {
            if (msg.Role == "user")
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                chatHistory.AddAssistantMessage(msg.Content);
        }

        chatHistory.AddUserMessage(fullUserMessage);

        for (var i = 0; i < MaxToolCalls; i++)
        {
            var fullContent = new StringBuilder();
            var pendingChunks = new List<string>();
            var isStreaming = false;
            string? streamError = null;

            // C# 不允許在 try/catch 裡 yield，用 manual enumerator 讓 yield 在 try/catch 外執行
            var enumerator = chatService.GetStreamingChatMessageContentsAsync(
                chatHistory, kernel: kernel, cancellationToken: cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            try
            {
                while (streamError == null)
                {
                    bool hasNext;
                    string text;

                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                        if (!hasNext) break;
                        text = enumerator.Current.Content ?? "";
                    }
                    catch (HttpOperationException ex)
                    {
                        logger.LogError(ex, "Streaming error (StatusCode: {StatusCode})", ex.StatusCode);
                        streamError = "抱歉，AI API 發生錯誤，請稍後再試。";
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Streaming error: {Type} - {Message}", ex.GetType().Name, ex.Message);
                        streamError = "抱歉，處理過程中發生問題，請稍後再試。";
                        break;
                    }

                    if (string.IsNullOrEmpty(text)) continue;

                    fullContent.Append(text);

                    if (isStreaming)
                    {
                        yield return new StreamItem(text);
                    }
                    else
                    {
                        pendingChunks.Add(text);

                        // 累積到 20 字後判斷：不以 { 或 ```（markdown JSON）開頭 → 最終回覆，開始串流
                        if (fullContent.Length >= 20)
                        {
                            var accumulated = fullContent.ToString().TrimStart();
                            if (!accumulated.StartsWith('{') && !accumulated.StartsWith("```"))
                            {
                                isStreaming = true;
                                foreach (var pending in pendingChunks)
                                    yield return new StreamItem(pending);
                                pendingChunks.Clear();
                            }
                        }
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            if (streamError != null)
            {
                yield return new StreamItem(streamError, IsError: true);
                yield break;
            }

            var content = fullContent.ToString();

            if (TryParseToolCall(content, out var toolName, out var args))
            {
                var toolResult = await CallToolAsync(toolName, args, mcpToolMap, cancellationToken);
                chatHistory.AddAssistantMessage(content);
                chatHistory.AddUserMessage($"工具執行結果：\n{toolResult}");
            }
            else
            {
                if (!isStreaming)
                    yield return new StreamItem(content);
                yield break;
            }
        }

        yield return new StreamItem("抱歉，已嘗試多次但仍無法完成您的請求，請換個方式描述或稍後再試。", IsError: true);
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
