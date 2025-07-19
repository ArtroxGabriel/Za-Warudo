using System.Diagnostics.CodeAnalysis;

namespace ZaWarudo.Model;

public readonly struct Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_error))]
    public readonly bool IsSuccess { get; }

    public readonly bool IsError => !IsSuccess;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static Result<TValue, TError> Success(TValue value)
    {
        return new Result<TValue, TError>(value);
    }

    public static Result<TValue, TError> Error(TError error)
    {
        return new Result<TValue, TError>(error);
    }

    public readonly TValue GetValueOrThrow()
    {
        if (!IsSuccess)
            throw new InvalidOperationException($"Cannot get value from error result. Error: {_error}");
        return _value;
    }

    public readonly TError GetErrorOrThrow()
    {
        if (IsSuccess)
            throw new InvalidOperationException("Cannot get error from success result");
        return _error;
    }

    public readonly void Match(Action<TValue> onSuccess, Action<TError> onError)
    {
        if (IsSuccess)
            onSuccess(_value);
        else
            onError(_error);
    }

    public readonly TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<TError, TResult> onError)
    {
        return IsSuccess ? onSuccess(_value) : onError(_error);
    }

    public readonly Result<TNew, TError> Map<TNew>(Func<TValue, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew, TError>.Success(mapper(_value))
            : Result<TNew, TError>.Error(_error);
    }

    public readonly Result<TNew, TError> Bind<TNew>(Func<TValue, Result<TNew, TError>> binder)
    {
        return IsSuccess ? binder(_value) : Result<TNew, TError>.Error(_error);
    }
}

public readonly struct Unit
{
    public static Unit Value => default;
}
