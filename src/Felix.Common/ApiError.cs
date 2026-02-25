namespace Felix.Common;

public class ApiErrorResponse
{
    public List<ApiError> Errors { get; init; } = [];
}

public class ApiError
{
    public string? Field { get; init; }
    public required string Message { get; init; }
}
