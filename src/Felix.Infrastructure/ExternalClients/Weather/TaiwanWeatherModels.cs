namespace Felix.Infrastructure.ExternalClients.Weather;

/// <summary>
/// 天氣預報
/// </summary>
public sealed class WeatherForecast
{
    public string Location { get; set; } = "";
    public List<WeatherPeriod> Periods { get; set; } = [];
    public DateTime? RainStopTime { get; set; }
    public string RainStopDescription { get; set; } = "";
    public bool IsRainingNow { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 天氣預報時段
/// </summary>
public sealed class WeatherPeriod
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Temperature { get; set; }
    public int ApparentTemperature { get; set; }
    public int RainProbability { get; set; }
    public string Weather { get; set; } = "";
    public int Humidity { get; set; }
}
