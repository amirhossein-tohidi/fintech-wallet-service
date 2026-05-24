namespace Wallet.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool success, string? error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool success, string? error) : base(success, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);
    public new static Result<T> Failure(string error) => new(default, false, error);
}