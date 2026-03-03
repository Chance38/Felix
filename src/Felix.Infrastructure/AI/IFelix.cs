namespace Felix.Infrastructure.AI;

public record ProcessResult(string Response, string ConversationId);

public interface IFelix
{
    Task<ProcessResult> ProcessAsync(string userMessage, string? conversationId = null, CancellationToken cancellationToken = default);
}
