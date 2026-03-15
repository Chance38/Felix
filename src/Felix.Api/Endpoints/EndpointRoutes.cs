using Felix.Api.Endpoints.Assistant.EditModel;
using Felix.Api.Endpoints.Assistant.Model;
using Felix.Api.Endpoints.Assistant.Process;
using Felix.Api.Filters;

namespace Felix.Api.Endpoints;

public static class EndpointRoutes
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/assistant/process", ProcessEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<ProcessRequest>>();

        app.MapGet("/api/v1/assistant/model", ModelEndpoint.HandleAsync);

        app.MapPost("/api/v1/assistant/model", EditModelEndpoint.HandleAsync)
            .AddEndpointFilter<ValidationFilter<EditModelRequest>>();
    }
}
