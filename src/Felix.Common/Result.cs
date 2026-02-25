namespace Felix.Common;

public enum ErrorCode
{
    NotFound,
    ExternalServiceError
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailed => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public ErrorCode? ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorCode? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> NotFound(string error) => new(false, default, error, Common.ErrorCode.NotFound);
    public static Result<T> ExternalError(string error) => new(false, default, error, Common.ErrorCode.ExternalServiceError);
}
