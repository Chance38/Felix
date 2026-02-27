using System.ComponentModel;
using Felix.Infrastructure.Clients.Geocoding;
using Felix.Infrastructure.Clients.Weather;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI.Plugins;

public class WeatherPlugin(
    IWeatherClient weatherClient,
    IGeocodingClient geocodingClient,
    IRequestContext requestContext)
{
    [KernelFunction("get_weather")]
    [Description("根據經緯度座標取得天氣資訊。")]
    public async Task<string> GetWeatherAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var result = await weatherClient.GetWeatherAsync(latitude, longitude, cancellationToken);

        if (result.IsFailed)
        {
            return $"無法取得天氣資訊：{result.Error}";
        }

        // 取得地名
        var locationResult = await geocodingClient.GetLocationNameAsync(latitude, longitude, cancellationToken);
        var locationName = locationResult.IsSuccess ? locationResult.Value! : "該地區";

        var weather = result.Value!;
        return $"{locationName} 目前 {weather.Temperature}{weather.TemperatureUnit}，{weather.Description}";
    }

    [KernelFunction("get_current_location_weather")]
    [Description("取得使用者目前所在位置的天氣。當使用者問天氣但沒有指定城市時使用。")]
    public async Task<string> GetCurrentLocationWeatherAsync(CancellationToken cancellationToken = default)
    {
        var location = requestContext.Location;

        if (location == null)
        {
            return "無法取得您的位置資訊，請告訴我您想查詢哪個城市的天氣。";
        }

        return await GetWeatherAsync(location.Latitude, location.Longitude, cancellationToken);
    }
}
