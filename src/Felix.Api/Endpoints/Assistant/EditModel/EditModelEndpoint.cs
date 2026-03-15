using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Assistant.EditModel;

public static class EditModelEndpoint
{
    public static async Task<IResult> HandleAsync(
        EditModelRequest request,
        IAiModelManager aiModelManager)
    {
        var previous = await aiModelManager.GetCurrentProviderAsync();
        var result = await aiModelManager.SetCurrentProviderAsync(request.Provider!);

        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);

        return Results.Ok(new EditModelResponse
        {
            PreviousModel = previous,
            CurrentModel = result.Value!
        });
    }
}
