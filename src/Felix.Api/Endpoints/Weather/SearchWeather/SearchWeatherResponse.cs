namespace Felix.Api.Endpoints.Weather.SearchWeather;

public class SearchWeatherResponse
{
    public required string City { get; set; }
    public required double Temperature { get; set; }
    public required string TemperatureUnit { get; set; }
    public required string Description { get; set; }
}
