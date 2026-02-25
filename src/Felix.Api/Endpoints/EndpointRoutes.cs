using Felix.Api.Endpoints.Weather.SearchWeather;
using Felix.Api.Filters;

namespace Felix.Api.Endpoints;

public static class EndpointRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/weather", SearchWeatherEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<SearchWeatherRequest>>();
    }
}
