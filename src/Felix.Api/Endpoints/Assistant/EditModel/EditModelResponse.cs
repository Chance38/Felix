namespace Felix.Api.Endpoints.Assistant.EditModel;

public class EditModelResponse
{
    public required string PreviousModel { get; init; }
    public required string CurrentModel { get; init; }
}
