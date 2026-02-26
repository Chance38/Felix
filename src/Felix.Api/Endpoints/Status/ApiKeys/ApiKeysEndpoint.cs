using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Status.ApiKeys;

public static class ApiKeysEndpoint
{
    public static IResult Handle(IGeminiKeyManager keyManager)
    {
        var currentIndex = keyManager.GetCurrentKeyIndex();
        var allKeysStatus = keyManager.GetAllKeysStatus();

        var response = new ApiKeysResponse
        {
            CurrentKeyIndex = currentIndex,
            TotalRequestsSinceStartup = keyManager.GetTotalRequestCount(),
            Keys = allKeysStatus.Select(k => new ApiKeyStatus
            {
                Index = k.Index,
                MaskedKey = k.MaskedKey,
                RequestCount = k.RequestCount,
                IsActive = k.Index == currentIndex
            }).ToList()
        };

        return Results.Ok(response);
    }
}
