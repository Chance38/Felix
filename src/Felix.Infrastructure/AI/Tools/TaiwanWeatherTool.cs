using System.Text.Json;
using Felix.Infrastructure.Clients.Weather;

namespace Felix.Infrastructure.AI.Tools;

public class TaiwanWeatherTool(ITaiwanWeatherClient weatherClient) : ILocalTool
{
    public string Name => "get_taiwan_weather";
    public string Description => "查詢台灣各地區的天氣預報，需提供 location（區域）與可選的 city（城市）參數";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<string> ExecuteAsync(Dictionary<string, JsonElement> args, CancellationToken cancellationToken = default)
    {
        var location = args.TryGetValue("location", out var loc)
            ? loc.GetString() ?? ""
            : "";

        var city = args.TryGetValue("city", out var c)
            ? c.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(location))
        {
            return "請提供地點名稱";
        }

        var forecast = await weatherClient.GetWeatherAsync(location, city, cancellationToken);

        if (!string.IsNullOrEmpty(forecast.Error))
        {
            return forecast.Error;
        }

        return JsonSerializer.Serialize(forecast, JsonOptions);
    }
}
