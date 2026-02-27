using System.Net.Http.Json;
using Felix.Common;

namespace Felix.Infrastructure.Clients.Geocoding;

public class GeocodingClient : IGeocodingClient
{
    private const string OpenMeteoGeocodingUrl = "https://geocoding-api.open-meteo.com";
    private const string NominatimUrl = "https://nominatim.openstreetmap.org";

    private readonly HttpClient _httpClient;

    public GeocodingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<GeoLocation>> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{OpenMeteoGeocodingUrl}/v1/search?name={Uri.EscapeDataString(city)}&count=1";
            var response = await _httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);

            if (response?.Results is null || response.Results.Length == 0)
            {
                return Result<GeoLocation>.NotFound($"找不到城市：{city}");
            }

            var result = response.Results[0];
            return Result<GeoLocation>.Success(new GeoLocation(result.Name, result.Latitude, result.Longitude));
        }
        catch (HttpRequestException ex)
        {
            return Result<GeoLocation>.ExternalError($"地理編碼服務無法使用：{ex.Message}");
        }
    }

    public async Task<Result<string>> GetLocationNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{NominatimUrl}/reverse?lat={latitude}&lon={longitude}&format=json&zoom=10";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Felix-Assistant/1.0");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NominatimResponse>(cancellationToken);
                if (result?.Address != null)
                {
                    var name = result.Address.City
                        ?? result.Address.Town
                        ?? result.Address.County
                        ?? result.Address.State
                        ?? "目前位置";
                    return Result<string>.Success(name);
                }
            }

            return Result<string>.Success("目前位置");
        }
        catch (HttpRequestException)
        {
            return Result<string>.Success("目前位置");
        }
    }
}

internal class GeocodingResponse
{
    public GeocodingResult[]? Results { get; set; }
}

internal class GeocodingResult
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

internal class NominatimResponse
{
    public NominatimAddress? Address { get; set; }
}

internal class NominatimAddress
{
    public string? City { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? State { get; set; }
}
