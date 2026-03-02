using Felix.Api.Endpoints.Assistant.Process;
using Felix.Api.Endpoints.Status.ApiKeys;
using Felix.Api.Filters;

namespace Felix.Api.Endpoints;

public static class EndpointRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/assistant/process", ProcessEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<ProcessRequest>>();

        // 狀態查詢
        app.MapGet("/api/v1/status/api-keys", ApiKeysEndpoint.Handle);
    }
}
