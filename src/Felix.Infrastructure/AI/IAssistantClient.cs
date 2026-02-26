namespace Felix.Infrastructure.AI;

public interface IAssistantClient
{
    Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default);
}
