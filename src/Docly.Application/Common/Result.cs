namespace Docly.Application.Common;

/// <summary>Outcome of a use case that can fail with user-facing messages instead of throwing.</summary>
public class Result
{
    protected Result(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}

public sealed class Result<T> : Result
{
    private Result(bool succeeded, T? value, IReadOnlyList<string> errors) : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, []);

    public new static Result<T> Failure(params string[] errors) => new(false, default, errors);
}
