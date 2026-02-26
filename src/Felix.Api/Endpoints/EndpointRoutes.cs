using Felix.Api.Endpoints.Assistant.Process;
using Felix.Api.Endpoints.Status.ApiKeys;
using Felix.Api.Endpoints.Weather.SearchWeather;
using Felix.Api.Filters;

namespace Felix.Api.Endpoints;

public static class EndpointRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        // AI 入口
        app.MapPost("/api/v1/assistant/process", ProcessEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<ProcessRequest>>();

        // 直接 API
        app.MapPost("/api/v1/weather", SearchWeatherEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<SearchWeatherRequest>>();

        // 狀態查詢
        app.MapGet("/api/v1/status/api-keys", ApiKeysEndpoint.Handle);
    }
}
