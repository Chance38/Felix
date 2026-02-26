namespace Felix.Api.Endpoints.Status.ApiKeys;

public class ApiKeysResponse
{
    public required List<ApiKeyStatus> Keys { get; set; }
    public required int CurrentKeyIndex { get; set; }
    public required int TotalRequestsSinceStartup { get; set; }
}

public class ApiKeyStatus
{
    public required int Index { get; set; }
    public required string MaskedKey { get; set; }
    public required int RequestCount { get; set; }
    public required bool IsActive { get; set; }
}
