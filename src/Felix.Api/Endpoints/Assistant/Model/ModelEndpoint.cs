using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Assistant.Model;

public static class ModelEndpoint
{
    public static async Task<IResult> HandleAsync(IAiModelManager aiModelManager)
    {
        var models = aiModelManager.GetProviders();
        var current = await aiModelManager.GetCurrentProviderAsync();

        return Results.Ok(new ModelResponse
        {
            Models = models,
            CurrentModel = current
        });
    }
}
