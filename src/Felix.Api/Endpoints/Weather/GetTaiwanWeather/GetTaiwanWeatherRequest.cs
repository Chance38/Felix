namespace Felix.Api.Endpoints.Weather.GetTaiwanWeather;

/// <summary>
/// 取得台灣天氣請求
/// </summary>
public sealed class GetTaiwanWeatherRequest
{
    /// <summary>
    /// 地點名稱 (縣市名稱或區域名稱)
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// 可選的縣市名稱 (當 Location 是區時使用)
    /// </summary>
    public string? City { get; set; }
}
