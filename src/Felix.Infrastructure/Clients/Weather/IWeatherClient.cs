using Felix.Common;

namespace Felix.Infrastructure.Clients.Weather;

public interface IWeatherClient
{
    Task<Result<WeatherData>> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}

public record WeatherData(
    double Temperature,
    string TemperatureUnit,
    string Description
);
