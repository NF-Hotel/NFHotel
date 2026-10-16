using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases;

/// <summary>
/// The outcome of a use case: did it succeed, and if not, which rules were broken.
/// </summary>
/// <remarks>
/// Expected rule violations ("the booking is already checked in") are <b>outcomes</b>, not
/// exceptions. They are returned as <see cref="Result"/>, so the caller cannot overlook them —
/// that is the direct antidote to the old system's check-in/check-out, which failed down into
/// <c>Debug.WriteLine</c> while the status had already been changed in memory.
/// <para>
/// Unexpected errors (the database is down, a null parameter from a programmer error) are still
/// thrown as exceptions and caught centrally in Web.
/// </para>
/// <para>
/// Several errors can be carried at once, because BR-60 and BR-96 require that a form can show
/// all validation errors at the same time.
/// </para>
/// </remarks>
public class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = Array.Empty<Error>();

    /// <summary>
    /// Creates a result. Use the factory methods <see cref="Success()"/> and
    /// <see cref="Failure(string)"/> rather than calling this directly.
    /// </summary>
    /// <param name="isSuccess">True if the use case succeeded.</param>
    /// <param name="errors">The errors. Must be empty on success and non-empty on failure.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if success and errors contradict each other — that is a programmer error, not a
    /// brugerudfald.
    /// </exception>
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (isSuccess && errors.Count > 0)
        {
            throw new ArgumentException("Et vellykket resultat kan ikke bære fejl.", nameof(errors));
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new ArgumentException("Et fejlet resultat skal bære mindst én fejl.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>True if the use case succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True if the use case was rejected by a rule.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>All broken rules, in field order. Empty on success.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The first error, or <c>null</c> on success. A convenience for a UI that shows only one.</summary>
    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>Creates a successful result without a value.</summary>
    /// <returns>A result with <see cref="IsSuccess"/> set.</returns>
    public static Result Success() => new(true, NoErrors);

    /// <summary>Creates a failed result with one error code.</summary>
    /// <param name="code">The error code from <see cref="ErrorCodes"/> or from a domain rule.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string code) => Failure(new Error(code));

    /// <summary>Creates a failed result with one error.</summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, new[] { error });
    }

    /// <summary>Creates a failed result with several errors (BR-60, BR-96).</summary>
    /// <param name="errors">The errors. Must not be empty.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    /// <summary>
    /// Creates a failed result from a list of error codes — the form the domain's
    /// <c>Validate</c>-metoder returnerer (A-09).
    /// </summary>
    /// <param name="codes">The error codes. Must not be empty.</param>
    /// <returns>A failed result.</returns>
    public static Result FromCodes(IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return new Result(false, ToErrors(codes));
    }

    /// <summary>Translates error codes to <see cref="Error"/> objects. Shared by both result types.</summary>
    /// <param name="codes">The error codes.</param>
    /// <returns>The errors in the same order.</returns>
    protected static IReadOnlyList<Error> ToErrors(IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        var errors = new Error[codes.Count];

        for (var i = 0; i < codes.Count; i++)
        {
            errors[i] = new Error(codes[i]);
        }

        return errors;
    }
}