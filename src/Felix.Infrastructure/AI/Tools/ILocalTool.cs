using System.Text.Json;

namespace Felix.Infrastructure.AI.Tools;

public interface ILocalTool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(Dictionary<string, JsonElement> args, CancellationToken cancellationToken = default);
}
