namespace Felix.Infrastructure.AI;

public interface IFelix
{
    Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default);
}
