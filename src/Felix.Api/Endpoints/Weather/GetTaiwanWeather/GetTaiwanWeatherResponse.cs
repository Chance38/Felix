namespace Felix.Api.Endpoints.Weather.GetTaiwanWeather;

/// <summary>
/// 台灣天氣摘要回應
/// </summary>
public sealed class GetTaiwanWeatherResponse
{
    /// <summary>
    /// 地點名稱
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// 目前溫度 (°C)
    /// </summary>
    public int Temperature { get; init; }

    /// <summary>
    /// 體感溫度 (°C)
    /// </summary>
    public int ApparentTemperature { get; init; }

    /// <summary>
    /// 目前天氣狀況
    /// </summary>
    public string Weather { get; init; } = "";

    /// <summary>
    /// 白天溫度範圍 (6:00-18:00)
    /// </summary>
    public string DayTemperature { get; init; } = "";

    /// <summary>
    /// 晚上溫度範圍 (18:00-06:00)
    /// </summary>
    public string NightTemperature { get; init; } = "";

    /// <summary>
    /// 目前是否下雨
    /// </summary>
    public bool IsRainingNow { get; init; }

    /// <summary>
    /// 雨停描述（如：預計下午 3 點雨停）
    /// </summary>
    public string? RainStopDescription { get; init; }
}
