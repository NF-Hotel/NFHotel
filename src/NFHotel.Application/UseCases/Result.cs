using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases;

/// <summary>
/// Udfaldet af en use case: lykkedes den, og hvis ikke, hvilke regler blev brudt.
/// </summary>
/// <remarks>
/// Forventede regelbrud ("bookingen er allerede tjekket ind") er <b>udfald</b>, ikke
/// undtagelser. De returneres som <see cref="Result"/>, så kalderen ikke kan overse dem —
/// det er den direkte modgift mod det gamle systems check-in/check-ud, der fejlede ned i
/// <c>Debug.WriteLine</c> mens statussen allerede var ændret i hukommelsen.
/// <para>
/// Uventede fejl (databasen er nede, en null-parameter fra en programmørfejl) kastes
/// fortsat som exceptions og fanges centralt i Web.
/// </para>
/// <para>
/// Flere fejl kan bæres på én gang, fordi BR-60 og BR-96 kræver at en formular kan vise
/// alle valideringsfejl samtidig.
/// </para>
/// </remarks>
public class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = Array.Empty<Error>();

    /// <summary>
    /// Opretter et resultat. Brug fabriksmetoderne <see cref="Success()"/> og
    /// <see cref="Failure(string)"/> frem for at kalde denne direkte.
    /// </summary>
    /// <param name="isSuccess">Sand hvis use casen lykkedes.</param>
    /// <param name="errors">Fejlene. Skal være tom ved succes og ikke-tom ved fejl.</param>
    /// <exception cref="ArgumentException">
    /// Kastes hvis succes og fejl modsiger hinanden — det er en programmørfejl, ikke et
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

    /// <summary>Sand hvis use casen lykkedes.</summary>
    public bool IsSuccess { get; }

    /// <summary>Sand hvis use casen blev afvist af en regel.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Alle brudte regler, i feltrækkefølge. Tom ved succes.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>Den første fejl, eller <c>null</c> ved succes. Bekvemmelighed for UI der kun viser én.</summary>
    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>Opretter et vellykket resultat uden værdi.</summary>
    /// <returns>Et resultat med <see cref="IsSuccess"/> sat.</returns>
    public static Result Success() => new(true, NoErrors);

    /// <summary>Opretter et fejlet resultat med én fejlkode.</summary>
    /// <param name="code">Fejlkoden fra <see cref="ErrorCodes"/> eller fra en domæneregel.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static Result Failure(string code) => Failure(new Error(code));

    /// <summary>Opretter et fejlet resultat med én fejl.</summary>
    /// <param name="error">Fejlen.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, new[] { error });
    }

    /// <summary>Opretter et fejlet resultat med flere fejl (BR-60, BR-96).</summary>
    /// <param name="errors">Fejlene. Må ikke være tom.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    /// <summary>
    /// Opretter et fejlet resultat ud fra en liste af fejlkoder — formen domænets
    /// <c>Validate</c>-metoder returnerer (A-09).
    /// </summary>
    /// <param name="codes">Fejlkoderne. Må ikke være tom.</param>
    /// <returns>Et fejlet resultat.</returns>
    public static Result FromCodes(IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return new Result(false, ToErrors(codes));
    }

    /// <summary>Oversætter fejlkoder til <see cref="Error"/>-objekter. Delt af begge resultattyper.</summary>
    /// <param name="codes">Fejlkoderne.</param>
    /// <returns>Fejlene i samme rækkefølge.</returns>
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