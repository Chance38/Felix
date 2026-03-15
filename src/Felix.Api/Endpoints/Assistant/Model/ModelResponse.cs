namespace Felix.Api.Endpoints.Assistant.Model;

public class ModelResponse
{
    public required IReadOnlyList<string> Models { get; init; }
    public required string CurrentModel { get; init; }
}
