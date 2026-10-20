namespace NFHotel.Application.UseCases;

/// <summary>
/// The outcome of a use case that delivers a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the value, typically a DTO.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    /// <summary>Creates a result with or without a value.</summary>
    /// <param name="isSuccess">True if the use case succeeded.</param>
    /// <param name="value">The value on success; otherwise <c>default</c>.</param>
    /// <param name="errors">The errors on failure; otherwise empty.</param>
    private Result(bool isSuccess, TValue value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// The value. Read it only when <see cref="Result.IsSuccess"/> is true.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the result is a failure. Reading the value of a failed result is a
    /// programmer error, not a user outcome.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failed result cannot be read.");

    /// <summary>Creates a successful result with a value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    public static Result<TValue> Success(TValue value) => new(true, value, Array.Empty<Error>());

    /// <summary>Creates a failed result with one error code.</summary>
    /// <param name="code">The error code.</param>
    /// <returns>A failed result.</returns>
    public static new Result<TValue> Failure(string code) => Failure(new Error(code));

    /// <summary>Creates a failed result with one error.</summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static new Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TValue>(false, default!, new[] { error });
    }

    /// <summary>Creates a failed result with several errors (BR-60, BR-96).</summary>
    /// <param name="errors">The errors. Must not be empty.</param>
    /// <returns>A failed result.</returns>
    public static new Result<TValue> Failure(IReadOnlyList<Error> errors) =>
        new(false, default!, errors);

    /// <summary>Creates a failed result from a list of error codes (A-09).</summary>
    /// <param name="codes">The error codes. Must not be empty.</param>
    /// <returns>A failed result.</returns>
    public static new Result<TValue> FromCodes(IReadOnlyList<string> codes) =>
        new(false, default!, ToErrors(codes));
}
