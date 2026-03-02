using Felix.Infrastructure;
using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Assistant.Process;

public static class ProcessEndpoint
{
    public static async Task<IResult> HandleAsync(
        ProcessRequest request,
        IFelix felix,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (request.Location != null)
        {
            requestContext.SetLocation(request.Location.Latitude, request.Location.Longitude);
        }

        var response = await felix.ProcessAsync(request.Message!, cancellationToken);

        return Results.Ok(new ProcessResponse
        {
            Response = response
        });
    }
}
