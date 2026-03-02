using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Felix.Infrastructure.Clients.Weather;

public interface ITaiwanWeatherClient
{
    Task<WeatherForecast> GetWeatherAsync(string location, string? city = null, CancellationToken cancellationToken = default);
}

public class TaiwanWeatherClient : ITaiwanWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TaiwanWeatherClient> _logger;

    private const string BaseUrl = "https://opendata.cwa.gov.tw/api/v1/rest/datastore";
    private const string CityLevelApi = "F-D0047-089"; // 縣市等級

    // 鄉鎮預報 API（僅支援特定鄉鎮時使用）
    private static readonly Dictionary<string, string> TownshipApiCodes = new()
    {
        ["桃園市"] = "F-D0047-005",
        ["新北市"] = "F-D0047-069",
    };

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

    public async Task<WeatherForecast> GetWeatherAsync(string location, string? city = null, CancellationToken cancellationToken = default)
    {
        var normalizedLocation = Normalize(location);
        var normalizedCity = city != null ? Normalize(city) : null;

        // 決定用哪個 API
        string apiCode;
        if (normalizedCity != null && TownshipApiCodes.TryGetValue(normalizedCity, out var townshipApi))
        {
            // 鄉鎮查詢：用對應縣市的鄉鎮 API
            apiCode = townshipApi;
        }
        else
        {
            // 縣市查詢：用縣市等級 API
            apiCode = CityLevelApi;
        }

        var url = $"{BaseUrl}/{apiCode}?Authorization={_apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseWeatherResponse(json, normalizedLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得天氣資料失敗: {Location}", location);
            return new WeatherForecast { Location = location, Error = $"無法取得 {location} 的天氣資料" };
        }
    }

    private static string Normalize(string name) => name.Replace("台", "臺").Trim();

    private static WeatherForecast ParseWeatherResponse(string json, string locationName)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("records", out var records) ||
            !records.TryGetProperty("Locations", out var locationsArray) ||
            locationsArray.GetArrayLength() == 0)
        {
            return new WeatherForecast { Location = locationName, Error = "找不到資料" };
        }

        var locations = locationsArray[0];
        if (!locations.TryGetProperty("Location", out var locationArray))
        {
            return new WeatherForecast { Location = locationName, Error = "找不到資料" };
        }

        // 找到對應的地點
        JsonElement? targetLocation = null;
        var searchName = StripSuffix(locationName);

        foreach (var loc in locationArray.EnumerateArray())
        {
            var locName = loc.GetProperty("LocationName").GetString() ?? "";
            if (StripSuffix(locName) == searchName)
            {
                targetLocation = loc;
                break;
            }
        }

        if (targetLocation == null)
        {
            return new WeatherForecast { Location = locationName, Error = $"找不到 {locationName}" };
        }

        var location = targetLocation.Value;
        var locNameResult = location.GetProperty("LocationName").GetString() ?? locationName;
        var weatherElements = location.GetProperty("WeatherElement");

        var elementMap = new Dictionary<string, JsonElement>();
        foreach (var element in weatherElements.EnumerateArray())
        {
            var name = element.GetProperty("ElementName").GetString() ?? "";
            elementMap[name] = element.GetProperty("Time");
        }

        var periods = ParsePeriods(elementMap);
        var rainAnalysis = AnalyzeRainStop(periods);

        return new WeatherForecast
        {
            Location = locNameResult,
            Periods = periods,
            RainStopTime = rainAnalysis.StopTime,
            RainStopDescription = rainAnalysis.Description,
            IsRainingNow = rainAnalysis.IsRainingNow
        };
    }

    private static string StripSuffix(string name) =>
        name.Replace("區", "").Replace("市", "").Replace("縣", "")
            .Replace("鄉", "").Replace("鎮", "");

    private static List<WeatherPeriod> ParsePeriods(Dictionary<string, JsonElement> elementMap)
    {
        var periods = new List<WeatherPeriod>();

        if (!elementMap.TryGetValue("溫度", out var tempElement))
            return periods;

        var timeCount = tempElement.GetArrayLength();

        for (var i = 0; i < timeCount && i < 24; i++)
        {
            var period = new WeatherPeriod();
            var timeSlot = tempElement[i];

            period.StartTime = DateTime.Parse(timeSlot.GetProperty("DataTime").GetString() ?? "");
            period.EndTime = period.StartTime.AddHours(1);
            period.Temperature = GetInt(timeSlot, "Temperature");

            if (elementMap.TryGetValue("體感溫度", out var at) && i < at.GetArrayLength())
                period.ApparentTemperature = GetInt(at[i], "ApparentTemperature");

            if (elementMap.TryGetValue("降雨機率", out var pop) && i < pop.GetArrayLength())
                period.RainProbability = GetInt(pop[i], "ProbabilityOfPrecipitation");

            if (elementMap.TryGetValue("天氣現象", out var wx) && i < wx.GetArrayLength())
                period.Weather = GetString(wx[i], "Weather");

            if (elementMap.TryGetValue("相對濕度", out var rh) && i < rh.GetArrayLength())
                period.Humidity = GetInt(rh[i], "RelativeHumidity");

            periods.Add(period);
        }

        return periods;
    }

    private static int GetInt(JsonElement slot, string field)
    {
        if (slot.TryGetProperty("ElementValue", out var values) && values.GetArrayLength() > 0 &&
            values[0].TryGetProperty(field, out var f))
        {
            return int.TryParse(f.GetString(), out var result) ? result : 0;
        }
        return 0;
    }

    private static string GetString(JsonElement slot, string field)
    {
        if (slot.TryGetProperty("ElementValue", out var values) && values.GetArrayLength() > 0 &&
            values[0].TryGetProperty(field, out var f))
        {
            return f.GetString() ?? "";
        }
        return "";
    }

    private static (DateTime? StopTime, string Description, bool IsRainingNow) AnalyzeRainStop(List<WeatherPeriod> periods)
    {
        if (periods.Count == 0) return (null, "", false);

        var now = DateTime.Now;
        var current = periods.FirstOrDefault(p => p.StartTime <= now && p.EndTime > now) ?? periods[0];

        var isRaining = current.RainProbability >= 50 || current.Weather.Contains("雨");
        if (!isRaining) return (null, "", false);

        var startIdx = periods.FindIndex(p => p.StartTime >= now);
        if (startIdx < 0) startIdx = 0;

        for (var i = startIdx; i < periods.Count - 1; i++)
        {
            var hasRain = periods[i].RainProbability >= 50 || periods[i].Weather.Contains("雨");
            var nextHasRain = periods[i + 1].RainProbability >= 50 || periods[i + 1].Weather.Contains("雨");

            if (!hasRain && !nextHasRain)
            {
                return (periods[i].StartTime, $"預計{FormatTime(periods[i].StartTime)}雨停", true);
            }
        }

        return (null, "持續有雨", true);
    }

    private static string FormatTime(DateTime time)
    {
        var hour = time.Hour;
        return hour switch
        {
            0 => "凌晨 12 點",
            >= 1 and < 6 => $"凌晨 {hour} 點",
            >= 6 and < 12 => $"早上 {hour} 點",
            12 => "中午 12 點",
            >= 13 and < 18 => $"下午 {hour - 12} 點",
            _ => $"晚上 {hour - 12} 點"
        };
    }
}

public class WeatherForecast
{
    public string Location { get; set; } = "";
    public List<WeatherPeriod> Periods { get; set; } = [];
    public DateTime? RainStopTime { get; set; }
    public string RainStopDescription { get; set; } = "";
    public bool IsRainingNow { get; set; }
    public string? Error { get; set; }
}

public class WeatherPeriod
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Temperature { get; set; }
    public int ApparentTemperature { get; set; }
    public int RainProbability { get; set; }
    public string Weather { get; set; } = "";
    public int Humidity { get; set; }
}
