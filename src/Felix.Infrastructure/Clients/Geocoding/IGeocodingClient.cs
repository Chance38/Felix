using Felix.Common;

namespace Felix.Infrastructure.Clients.Geocoding;

public interface IGeocodingClient
{
    Task<Result<GeoLocation>> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default);
    Task<Result<string>> GetLocationNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}

public record GeoLocation(string Name, double Latitude, double Longitude);
