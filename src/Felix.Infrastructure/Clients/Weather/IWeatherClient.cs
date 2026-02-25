using Felix.Common;

namespace Felix.Infrastructure.Clients.Weather;

public interface IWeatherClient
{
    Task<Result<WeatherData>> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
}

public record WeatherData(
    string City,
    double Temperature,
    string TemperatureUnit,
    string Description
);
