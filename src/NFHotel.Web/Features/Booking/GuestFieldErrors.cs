using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Fordeler et fejlet <see cref="Result"/> ud på gæsteformularens felter, så hver fejl
/// står ved det felt den handler om.
/// </summary>
/// <remarks>
/// Application samler alle brudte regler på én gang (BR-60, BR-96). Vises de som én liste
/// øverst, skal brugeren selv gætte hvilket felt "E-mailen skal indeholde både '@' og '.'"
/// hører til. Denne type er den oversættelse — og kun den: teksterne kommer stadig fra
/// <see cref="ErrorMessages"/>, så der findes ingen dansk streng her.
/// <para>
/// Koder klassen ikke kender — fx <c>booking.room_not_available</c> fra oprettelsen —
/// havner i <see cref="Other"/> og vises som en almindelig fejlbesked. En ukendt kode må
/// aldrig forsvinde tavst.
/// </para>
/// </remarks>
public sealed class GuestFieldErrors
{
    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    /// <summary>Ingen fejl. Formularens udgangspunkt.</summary>
    public static readonly GuestFieldErrors None = new();

    /// <summary>Fejlteksten ved fornavn, eller <c>null</c>.</summary>
    public string? FirstName { get; private init; }

    /// <summary>Fejlteksten ved efternavn, eller <c>null</c>.</summary>
    public string? LastName { get; private init; }

    /// <summary>Fejlteksten ved e-mail, eller <c>null</c>.</summary>
    public string? Email { get; private init; }

    /// <summary>Fejlteksten ved telefonnummer, eller <c>null</c>.</summary>
    public string? PhoneNumber { get; private init; }

    /// <summary>Fejlteksten ved land, eller <c>null</c>.</summary>
    public string? Country { get; private init; }

    /// <summary>Fejlteksten ved pasnummer, eller <c>null</c>.</summary>
    public string? PassportNumber { get; private init; }

    /// <summary>De fejl der ikke hører til et enkelt felt. Vises samlet over formularen.</summary>
    public IReadOnlyList<string> Other { get; private init; } = NoMessages;

    /// <summary>
    /// Fordeler resultatets fejl ud på felterne.
    /// </summary>
    /// <param name="result">Resultatet fra <c>ValidateFields</c> eller <c>CreateAsync</c>.</param>
    /// <returns>Fejlene fordelt pr. felt. <see cref="None"/> hvis resultatet lykkedes.</returns>
    public static GuestFieldErrors FromResult(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return None;
        }

        string? firstName = null;
        string? lastName = null;
        string? email = null;
        string? phoneNumber = null;
        string? country = null;
        string? passportNumber = null;
        List<string>? other = null;

        foreach (var error in result.Errors)
        {
            var text = ErrorMessages.Describe(error);

            switch (error.Code)
            {
                case ErrorCodes.Guest.FirstNameRequired:
                case ErrorCodes.Guest.FirstNameTooLong:
                    firstName ??= text;
                    break;

                case ErrorCodes.Guest.LastNameRequired:
                case ErrorCodes.Guest.LastNameTooLong:
                    lastName ??= text;
                    break;

                case ErrorCodes.Guest.EmailRequired:
                case ErrorCodes.Guest.EmailInvalid:
                case ErrorCodes.Guest.EmailTooLong:
                    email ??= text;
                    break;

                case ErrorCodes.Guest.PhoneNumberRequired:
                case ErrorCodes.Guest.PhoneNumberTooLong:
                    phoneNumber ??= text;
                    break;

                case ErrorCodes.Guest.CountryRequired:
                case ErrorCodes.Guest.CountryTooLong:
                    country ??= text;
                    break;

                case ErrorCodes.Guest.PassportNumberTooLong:
                    passportNumber ??= text;
                    break;

                default:
                    (other ??= []).Add(text);
                    break;
            }
        }

        return new GuestFieldErrors
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            Country = country,
            PassportNumber = passportNumber,
            Other = other is null ? NoMessages : other
        };
    }
}