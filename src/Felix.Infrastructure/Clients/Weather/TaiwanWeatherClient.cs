using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Felix.Infrastructure.Clients.Weather;

public interface ITaiwanWeatherClient
{
    Task<string> GetWeatherAsync(string locationName, CancellationToken cancellationToken = default);
}

public class TaiwanWeatherClient : ITaiwanWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TaiwanWeatherClient> _logger;

    // 36 小時天氣預報 API
    private const string ApiUrl = "https://opendata.cwa.gov.tw/api/v1/rest/datastore/F-C0032-001";

    public TaiwanWeatherClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TaiwanWeatherClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["CwaApiKey"]
            ?? throw new InvalidOperationException("CwaApiKey is not configured");
    }

    public async Task<string> GetWeatherAsync(string locationName, CancellationToken cancellationToken = default)
    {
        // 標準化縣市名稱（台 → 臺）
        var normalizedLocation = NormalizeLocationName(locationName);

        var url = $"{ApiUrl}?Authorization={_apiKey}&locationName={Uri.EscapeDataString(normalizedLocation)}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseWeatherResponse(json, normalizedLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得台灣天氣資料失敗: {Location}", normalizedLocation);
            return $"無法取得 {locationName} 的天氣資料";
        }
    }

    private static string NormalizeLocationName(string location)
    {
        // 處理常見的縣市名稱變體
        return location
            .Replace("台", "臺")
            .Replace("台北", "臺北")
            .Replace("台中", "臺中")
            .Replace("台南", "臺南")
            .Replace("台東", "臺東");
    }

    private static string ParseWeatherResponse(string json, string locationName)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("records", out var records) ||
            !records.TryGetProperty("location", out var locations) ||
            locations.GetArrayLength() == 0)
        {
            return $"找不到 {locationName} 的天氣資料";
        }

        var location = locations[0];
        var locName = location.GetProperty("locationName").GetString();
        var weatherElements = location.GetProperty("weatherElement");

        // 建立時段索引的資料結構
        var forecasts = new Dictionary<int, Dictionary<string, string>>();

        foreach (var element in weatherElements.EnumerateArray())
        {
            var elementName = element.GetProperty("elementName").GetString();
            var timeArray = element.GetProperty("time");

            for (var i = 0; i < timeArray.GetArrayLength(); i++)
            {
                if (!forecasts.ContainsKey(i))
                {
                    forecasts[i] = [];
                    var timeSlot = timeArray[i];
                    forecasts[i]["startTime"] = timeSlot.GetProperty("startTime").GetString() ?? "";
                }

                var parameter = timeArray[i].GetProperty("parameter");
                var value = parameter.GetProperty("parameterName").GetString() ?? "";
                forecasts[i][elementName ?? ""] = value;
            }
        }

        var parts = new List<string> { $"地點：{locName}" };
        var orderedForecasts = forecasts.OrderBy(f => f.Key).Take(2).ToList();

        foreach (var (_, data) in orderedForecasts)
        {
            var start = DateTime.Parse(data["startTime"]);
            var period = GetPeriodName(start);
            var temp = GetAverageTemp(data);
            var weather = data.GetValueOrDefault("Wx", "");
            var rain = data.GetValueOrDefault("PoP", "0");

            parts.Add($"{period}：{temp}°C，{weather}，降雨機率 {rain}%");
        }

        // 分析雨停時間
        var rainAnalysis = AnalyzeRain(orderedForecasts);
        if (!string.IsNullOrEmpty(rainAnalysis))
        {
            parts.Add(rainAnalysis);
        }

        return string.Join("，", parts);
    }

    private static int GetAverageTemp(Dictionary<string, string> data)
    {
        var min = int.TryParse(data.GetValueOrDefault("MinT", "0"), out var minVal) ? minVal : 0;
        var max = int.TryParse(data.GetValueOrDefault("MaxT", "0"), out var maxVal) ? maxVal : 0;
        return (min + max) / 2;
    }

    private static string GetPeriodName(DateTime forecastStart)
    {
        // 06:00-18:00 白天，18:00-06:00 夜晚
        return forecastStart.Hour is >= 6 and < 18 ? "白天" : "夜晚";
    }

    private static string AnalyzeRain(List<KeyValuePair<int, Dictionary<string, string>>> forecasts)
    {
        if (forecasts.Count < 2) return "";

        var firstRain = int.TryParse(forecasts[0].Value.GetValueOrDefault("PoP", "0"), out var r1) ? r1 : 0;
        var secondRain = int.TryParse(forecasts[1].Value.GetValueOrDefault("PoP", "0"), out var r2) ? r2 : 0;
        var secondStart = DateTime.Parse(forecasts[1].Value["startTime"]);
        var secondPeriod = GetPeriodName(secondStart);

        // 判斷雨勢變化
        if (firstRain >= 50 && secondRain < 30)
        {
            return $"預計{secondPeriod}雨停";
        }
        if (firstRain >= 50 && secondRain >= 50)
        {
            return "整天有雨";
        }

        return "";
    }
}
