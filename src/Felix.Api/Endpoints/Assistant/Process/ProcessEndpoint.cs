using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Assistant.Process;

public static class ProcessEndpoint
{
    public static async Task<IResult> HandleAsync(
        ProcessRequest request,
        IAssistantClient assistantClient,
        CancellationToken cancellationToken)
    {
        var response = await assistantClient.ProcessAsync(request.Message!, cancellationToken);

        return Results.Ok(new ProcessResponse
        {
            Response = response
        });
    }
}
