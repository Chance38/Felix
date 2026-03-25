namespace Felix.Infrastructure.AI;

public record StreamChunk(string Content, bool IsDone = false, string? ConversationId = null);

public interface IFelix
{
    IAsyncEnumerable<StreamChunk> ProcessStreamAsync(string userMessage, string? conversationId = null, CancellationToken cancellationToken = default);
}
