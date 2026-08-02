namespace TicketReservationSystem.Application.Abstractions;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error is not NoneError ||
            !isSuccess && error is NoneError)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, new NoneError());
    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    public T Value { get; } = default!;

    protected Result(T value) : base(true, new NoneError())
    {
        Value = value;
    }

    protected Result(Error error) : base(false, error)
    {
    }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);
}
