using System.Net.Http.Json;
using Felix.Common;

namespace Felix.Infrastructure.Clients.Weather;

public class WeatherClient : IWeatherClient
{
    private const string ForecastBaseUrl = "https://api.open-meteo.com";

    private readonly HttpClient _httpClient;

    public WeatherClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<WeatherData>> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ForecastBaseUrl}/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code";
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoWeatherResponse>(url, cancellationToken);

            if (response?.Current is null)
            {
                return Result<WeatherData>.ExternalError("無法取得天氣資料");
            }

            var weatherData = new WeatherData(
                Temperature: response.Current.Temperature,
                TemperatureUnit: "°C",
                Description: GetWeatherDescription(response.Current.WeatherCode)
            );

            return Result<WeatherData>.Success(weatherData);
        }
        catch (HttpRequestException ex)
        {
            return Result<WeatherData>.ExternalError($"天氣服務無法使用：{ex.Message}");
        }
    }

    private static string GetWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "晴朗",
            1 => "大致晴朗",
            2 => "多雲",
            3 => "陰天",
            45 or 48 => "霧",
            51 or 53 or 55 => "毛毛雨",
            61 or 63 or 65 => "雨",
            71 or 73 or 75 => "雪",
            77 => "霰",
            80 or 81 or 82 => "陣雨",
            85 or 86 => "陣雪",
            95 => "雷暴",
            96 or 99 => "雷暴夾冰雹",
            _ => "未知"
        };
    }
}
