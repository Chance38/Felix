using Felix.Common;
using Felix.Domain.Weather;

namespace Felix.Api.Endpoints.Weather.GetTaiwanWeather;

/// <summary>
/// 取得台灣天氣端點
/// </summary>
public static class GetTaiwanWeatherEndpoint
{
    public static async Task<IResult> HandleAsync(
        GetTaiwanWeatherRequest request,
        IWeatherService weatherService,
        CancellationToken ct)
    {
        var result = await weatherService.GetTaiwanWeatherAsync(
            request.Location,
            request.City,
            ct);

        if (result.IsFailed)
        {
            var error = result.Error!;
            if (error.Contains("找不到"))
            {
                return Results.NotFound(new ApiErrorResponse
                {
                    Errors = [new ApiError { Message = error }]
                });
            }

            return Results.BadRequest(new ApiErrorResponse
            {
                Errors = [new ApiError { Message = error }]
            });
        }

        var dto = result.Value!;
        return Results.Ok(new GetTaiwanWeatherResponse
        {
            Location = dto.Location,
            Temperature = dto.Temperature,
            ApparentTemperature = dto.ApparentTemperature,
            Weather = dto.Weather,
            DayTemperature = dto.DayTemperature,
            NightTemperature = dto.NightTemperature,
            IsRainingNow = dto.IsRainingNow,
            RainStopDescription = dto.RainStopDescription
        });
    }
}
