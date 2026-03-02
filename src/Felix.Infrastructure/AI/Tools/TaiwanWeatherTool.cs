using System.Text.Json;
using Felix.Infrastructure.Clients.Weather;

namespace Felix.Infrastructure.AI.Tools;

public class TaiwanWeatherTool(ITaiwanWeatherClient weatherClient) : ILocalTool
{
    public string Name => "get_taiwan_weather";

    public async Task<string> ExecuteAsync(Dictionary<string, JsonElement> args, CancellationToken cancellationToken = default)
    {
        var location = args.TryGetValue("location", out var loc)
            ? loc.GetString() ?? ""
            : "";

        if (string.IsNullOrWhiteSpace(location))
        {
            return "請提供縣市名稱";
        }

        return await weatherClient.GetWeatherAsync(location, cancellationToken);
    }
}
