using System.Net.Http.Json;
using Felix.Common;

namespace Felix.Infrastructure.Clients.Weather;

public class WeatherClient : IWeatherClient
{
    private const string GeocodingBaseUrl = "https://geocoding-api.open-meteo.com";
    private const string ForecastBaseUrl = "https://api.open-meteo.com";

    private readonly HttpClient _httpClient;

    public WeatherClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<WeatherData>> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            var locationResult = await GetCoordinatesAsync(city, cancellationToken);
            if (locationResult.IsFailed)
            {
                return Result<WeatherData>.NotFound(locationResult.Error!);
            }

            var location = locationResult.Value!;
            var weatherResult = await GetWeatherByCoordinatesAsync(location.Latitude, location.Longitude, cancellationToken);
            if (weatherResult.IsFailed)
            {
                return Result<WeatherData>.ExternalError(weatherResult.Error!);
            }

            var weather = weatherResult.Value!;
            var weatherData = new WeatherData(
                City: location.Name,
                Temperature: weather.Temperature,
                TemperatureUnit: "°C",
                Description: GetWeatherDescription(weather.WeatherCode)
            );

            return Result<WeatherData>.Success(weatherData);
        }
        catch (HttpRequestException ex)
        {
            return Result<WeatherData>.ExternalError($"Weather service unavailable: {ex.Message}");
        }
    }

    private async Task<Result<GeoLocation>> GetCoordinatesAsync(string city, CancellationToken cancellationToken)
    {
        var url = $"{GeocodingBaseUrl}/v1/search?name={Uri.EscapeDataString(city)}&count=1";
        var response = await _httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);

        if (response?.Results is null || response.Results.Length == 0)
        {
            return Result<GeoLocation>.NotFound($"City not found: {city}");
        }

        var result = response.Results[0];
        return Result<GeoLocation>.Success(new GeoLocation(result.Name, result.Latitude, result.Longitude));
    }

    private async Task<Result<CurrentWeather>> GetWeatherByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var url = $"{ForecastBaseUrl}/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code";
        var response = await _httpClient.GetFromJsonAsync<OpenMeteoWeatherResponse>(url, cancellationToken);

        if (response?.Current is null)
        {
            return Result<CurrentWeather>.ExternalError("Failed to get weather data");
        }

        return Result<CurrentWeather>.Success(response.Current);
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

internal record GeoLocation(string Name, double Latitude, double Longitude);
