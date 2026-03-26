using Felix.Common;
using Felix.Domain.Weather.Dtos;

namespace Felix.Domain.Weather;

/// <summary>
/// 天氣服務介面
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// 取得台灣地區天氣預報
    /// </summary>
    /// <param name="location">地點名稱 (縣市或區)</param>
    /// <param name="city">可選的縣市名稱 (當 location 是區時需要)</param>
    /// <param name="ct">取消權杖</param>
    Task<Result<TaiwanWeatherDto>> GetTaiwanWeatherAsync(
        string location,
        string? city = null,
        CancellationToken ct = default);
}
