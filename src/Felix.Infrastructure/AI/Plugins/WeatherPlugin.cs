using System.ComponentModel;
using Felix.Infrastructure.Clients.Weather;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI.Plugins;

public class WeatherPlugin(IWeatherClient weatherClient)
{
    [KernelFunction("get_weather")]
    [Description("取得指定城市的目前天氣狀況，包含溫度和天氣描述")]
    public async Task<string> GetWeatherAsync(
        [Description("城市名稱，例如：Taipei、Tokyo、New York")] string city,
        CancellationToken cancellationToken = default)
    {
        var result = await weatherClient.GetWeatherAsync(city, cancellationToken);

        if (result.IsFailed)
        {
            return $"無法取得 {city} 的天氣資訊：{result.Error}";
        }

        var weather = result.Value!;
        return $"{weather.City} 目前 {weather.Temperature}{weather.TemperatureUnit}，{weather.Description}";
    }
}
