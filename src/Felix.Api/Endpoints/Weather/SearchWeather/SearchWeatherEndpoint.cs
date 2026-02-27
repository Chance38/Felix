using Felix.Common;
using Felix.Infrastructure.Clients.Geocoding;
using Felix.Infrastructure.Clients.Weather;

namespace Felix.Api.Endpoints.Weather.SearchWeather;

public static class SearchWeatherEndpoint
{
    public static async Task<IResult> HandleAsync(
        SearchWeatherRequest request,
        IGeocodingClient geocodingClient,
        IWeatherClient weatherClient,
        CancellationToken cancellationToken)
    {
        // 先取得座標
        var geoResult = await geocodingClient.GetCoordinatesAsync(request.City!, cancellationToken);

        if (geoResult.IsFailed)
        {
            if (geoResult.ErrorCode == ErrorCode.NotFound)
            {
                return Results.Ok<SearchWeatherResponse?>(null);
            }

            var error = new ApiErrorResponse
            {
                Errors = [new ApiError { Message = geoResult.Error! }]
            };
            return Results.Json(error, statusCode: 502);
        }

        var location = geoResult.Value!;

        // 再取得天氣
        var weatherResult = await weatherClient.GetWeatherAsync(location.Latitude, location.Longitude, cancellationToken);

        if (weatherResult.IsFailed)
        {
            var error = new ApiErrorResponse
            {
                Errors = [new ApiError { Message = weatherResult.Error! }]
            };
            return Results.Json(error, statusCode: 502);
        }

        var weather = weatherResult.Value!;
        var response = new SearchWeatherResponse
        {
            City = location.Name,
            Temperature = weather.Temperature,
            TemperatureUnit = weather.TemperatureUnit,
            Description = weather.Description
        };

        return Results.Ok(response);
    }
}
