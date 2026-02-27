using System.ComponentModel;
using Felix.Infrastructure.Clients.Geocoding;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI.Plugins;

public class GeocodingPlugin(IGeocodingClient geocodingClient)
{
    [KernelFunction("get_coordinates")]
    [Description("將城市名稱轉換為經緯度座標。需要英文城市名。")]
    public async Task<string> GetCoordinatesAsync(
        [Description("城市名稱（英文），例如：Taipei、Tokyo、New York")] string city,
        CancellationToken cancellationToken = default)
    {
        var result = await geocodingClient.GetCoordinatesAsync(city, cancellationToken);

        if (result.IsFailed)
        {
            return $"無法找到城市 {city} 的座標：{result.Error}";
        }

        var location = result.Value!;
        return $"{location.Name} 的座標是 latitude={location.Latitude}, longitude={location.Longitude}";
    }
}
