using Felix.Common;
using Felix.Infrastructure.Clients.Weather;

namespace Felix.Api.Endpoints.Weather.SearchWeather;

public static class SearchWeatherEndpoint
{
    public static async Task<IResult> HandleAsync(
        SearchWeatherRequest request,
        IWeatherClient weatherClient,
        CancellationToken cancellationToken)
    {
        var result = await weatherClient.GetWeatherAsync(request.City!, cancellationToken);

        if (result.IsFailed)
        {
            if (result.ErrorCode == ErrorCode.NotFound)
            {
                return Results.Ok<SearchWeatherResponse?>(null);
            }

            var error = new ApiErrorResponse
            {
                Errors = [new ApiError { Message = result.Error! }]
            };
            return Results.Json(error, statusCode: 502);
        }

        var response = new SearchWeatherResponse
        {
            City = result.Value!.City,
            Temperature = result.Value.Temperature,
            TemperatureUnit = result.Value.TemperatureUnit,
            Description = result.Value.Description
        };

        return Results.Ok(response);
    }
}
