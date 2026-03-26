using Felix.Common;
using Felix.Domain.Weather;
using Felix.Domain.Weather.Dtos;
using Felix.Infrastructure.ExternalClients.Weather;

namespace Felix.Infrastructure.Services;

/// <summary>
/// 天氣服務實作
/// </summary>
public sealed class WeatherService(ITaiwanWeatherClient taiwanWeatherClient) : IWeatherService
{
    /// <inheritdoc />
    public async Task<Result<TaiwanWeatherDto>> GetTaiwanWeatherAsync(
        string location,
        string? city = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return Result<TaiwanWeatherDto>.Failure("請提供地點名稱");
        }

        var forecast = await taiwanWeatherClient.GetWeatherAsync(location, city, ct);

        if (!string.IsNullOrEmpty(forecast.Error))
        {
            return Result<TaiwanWeatherDto>.Failure(forecast.Error);
        }

        return Result<TaiwanWeatherDto>.Success(BuildSummary(forecast));
    }

    private static TaiwanWeatherDto BuildSummary(WeatherForecast forecast)
    {
        var now = DateTime.Now;
        var periods = forecast.Periods;

        // 找當前時段
        var current = periods.FirstOrDefault(p => p.StartTime <= now && p.EndTime > now)
                      ?? periods.FirstOrDefault()
                      ?? new WeatherPeriod();

        // 計算白天溫度範圍 (6:00-18:00)
        var today = now.Date;
        var dayPeriods = periods.Where(p =>
            p.StartTime.Date == today &&
            p.StartTime.Hour >= 6 &&
            p.StartTime.Hour < 18).ToList();

        var dayTemp = dayPeriods.Count > 0
            ? $"{dayPeriods.Min(p => p.Temperature)}-{dayPeriods.Max(p => p.Temperature)}°C"
            : "";

        // 計算晚上溫度範圍 (今晚 18:00 - 明天 06:00)
        var nightPeriods = periods.Where(p =>
            (p.StartTime.Date == today && p.StartTime.Hour >= 18) ||
            (p.StartTime.Date == today.AddDays(1) && p.StartTime.Hour < 6)).ToList();

        var nightTemp = nightPeriods.Count > 0
            ? $"{nightPeriods.Min(p => p.Temperature)}-{nightPeriods.Max(p => p.Temperature)}°C"
            : "";

        return new TaiwanWeatherDto
        {
            Location = forecast.Location,
            Temperature = current.Temperature,
            ApparentTemperature = current.ApparentTemperature,
            Weather = current.Weather,
            DayTemperature = dayTemp,
            NightTemperature = nightTemp,
            IsRainingNow = forecast.IsRainingNow,
            RainStopDescription = string.IsNullOrEmpty(forecast.RainStopDescription)
                ? null
                : forecast.RainStopDescription
        };
    }
}
