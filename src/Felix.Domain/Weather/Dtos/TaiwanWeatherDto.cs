namespace Felix.Domain.Weather.Dtos;

/// <summary>
/// 台灣天氣摘要 DTO
/// </summary>
public sealed class TaiwanWeatherDto
{
    public required string Location { get; init; }

    /// <summary>
    /// 目前溫度
    /// </summary>
    public int Temperature { get; init; }

    /// <summary>
    /// 體感溫度
    /// </summary>
    public int ApparentTemperature { get; init; }

    /// <summary>
    /// 目前天氣
    /// </summary>
    public string Weather { get; init; } = "";

    /// <summary>
    /// 白天溫度範圍（如：22-27°C）
    /// </summary>
    public string DayTemperature { get; init; } = "";

    /// <summary>
    /// 晚上溫度範圍（如：18-22°C）
    /// </summary>
    public string NightTemperature { get; init; } = "";

    /// <summary>
    /// 目前是否下雨
    /// </summary>
    public bool IsRainingNow { get; init; }

    /// <summary>
    /// 雨停描述
    /// </summary>
    public string? RainStopDescription { get; init; }
}
