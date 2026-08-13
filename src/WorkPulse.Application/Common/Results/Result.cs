namespace WorkPulse.Application.Common.Results;

public class Result<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static Result<T> Failure(string error) => new() { Succeeded = false, Error = error };
}
