namespace Felix.Infrastructure.ExternalClients.Weather;

/// <summary>
/// 台灣天氣 API 設定
/// </summary>
public sealed class TaiwanWeatherOptions
{
    /// <summary>
    /// 設定區段名稱
    /// </summary>
    public const string SectionName = "ExternalServices:CentralWeather";

    /// <summary>
    /// 中央氣象署 API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
