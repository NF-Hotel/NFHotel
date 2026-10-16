namespace NFHotel.Application.UseCases;

/// <summary>
/// Udfaldet af en use case der leverer en værdi ved succes.
/// </summary>
/// <typeparam name="TValue">Værditypen, typisk en DTO.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    /// <summary>Opretter et resultat med eller uden værdi.</summary>
    /// <param name="isSuccess">Sand hvis use casen lykkedes.</param>
    /// <param name="value">Værdien ved succes; ellers <c>default</c>.</param>
    /// <param name="errors">Fejlene ved fejl; ellers tom.</param>
    private Result(bool isSuccess, TValue value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// Værdien. Læs den kun når <see cref="Result.IsSuccess"/> er sand.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Kastes hvis resultatet er en fejl. At læse værdien af et fejlet resultat er en
    /// programmørfejl, ikke et brugerudfald.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Værdien på et fejlet resultat kan ikke læses.");

    /// <summary>Opretter et vellykket resultat med en værdi.</summary>
    /// <param name="value">Værdien.</param>
    /// <returns>Et vellykket resultat.</returns>
    public static Result<TValue> Success(TValue value) => new(true, value, Array.Empty<Error>());

    /// <summary>Opretter et fejlet resultat med én fejlkode.</summary>
    /// <param name="code">Fejlkoden.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static new Result<TValue> Failure(string code) => Failure(new Error(code));

    /// <summary>Opretter et fejlet resultat med én fejl.</summary>
    /// <param name="error">Fejlen.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static new Result<TValue> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TValue>(false, default!, new[] { error });
    }

    /// <summary>Opretter et fejlet resultat med flere fejl (BR-60, BR-96).</summary>
    /// <param name="errors">Fejlene. Må ikke være tom.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static new Result<TValue> Failure(IReadOnlyList<Error> errors) =>
        new(false, default!, errors);

    /// <summary>Opretter et fejlet resultat ud fra en liste af fejlkoder (A-09).</summary>
    /// <param name="codes">Fejlkoderne. Må ikke være tom.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static new Result<TValue> FromCodes(IReadOnlyList<string> codes) =>
        new(false, default!, ToErrors(codes));
}