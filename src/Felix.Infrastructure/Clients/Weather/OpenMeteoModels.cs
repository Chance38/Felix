using System.Text.Json.Serialization;

namespace Felix.Infrastructure.Clients.Weather;

internal class OpenMeteoWeatherResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; set; }
}

internal class CurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
}
