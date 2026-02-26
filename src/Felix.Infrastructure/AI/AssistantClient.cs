using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Felix.Infrastructure.AI;

public class AssistantClient(
    IKernelFactory kernelFactory,
    IGeminiKeyManager keyManager) : IAssistantClient
{
    private const string SystemPrompt = """
        你是 Felix，一位經驗豐富的私人管家。

        風格：
        - 沉穩、專業、值得信賴
        - 說話簡潔有條理，但帶有溫度
        - 適時給予實用的建議，像是照顧主人多年的管家
        - 用繁體中文，語氣成熟穩重

        重要：當你使用工具取得資訊後，不要只是複述工具的回傳內容。
        請用你自己的話重新表達，並加上貼心的建議或關心。

        範例：
        - 工具回傳「台北目前 24°C，陰天」
        - 你應該說「台北現在 24 度，天氣陰陰的。如果要外出，建議帶把傘備著比較保險。」
        """;

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
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // 429 錯誤，嘗試切換到下一個 API Key
                if (!keyManager.TryNextKey())
                {
                    return "抱歉，目前服務繁忙，請稍後再試。";
                }
                // 繼續迴圈，用新的 key 重試
            }
        }
    }

    private async Task<string> ExecuteAsync(Kernel kernel, string userMessage, CancellationToken cancellationToken)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var settings = new GeminiPromptExecutionSettings
        {
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage(userMessage);

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);

        return response.Content ?? "抱歉，我無法處理這個請求。";
    }
}
