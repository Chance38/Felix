namespace Felix.Infrastructure.ExternalClients.Weather;

/// <summary>
/// 台灣天氣 API 客戶端介面
/// </summary>
public interface ITaiwanWeatherClient
{
    /// <summary>
    /// 取得天氣預報
    /// </summary>
    /// <param name="location">地點名稱 (縣市或區)</param>
    /// <param name="city">可選的縣市名稱 (當 location 是區時需要)</param>
    /// <param name="ct">取消權杖</param>
    Task<WeatherForecast> GetWeatherAsync(
        string location,
        string? city = null,
        CancellationToken ct = default);
}
