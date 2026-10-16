using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Rules;

/// <summary>
/// Statsløs gæstevalidering (BR-46, BR-52 til BR-56, BR-60, BR-91 til BR-96).
/// </summary>
/// <remarks>
/// <see cref="Guest.Create"/> fejler hurtigt på første brud; <see cref="Validate"/> samler
/// ALLE fejl, så en formular kan vise dem på én gang (BR-60, BR-96). Begge veje bruger de
/// samme prædikater — reglen findes ét sted.
/// <para>
/// Der returneres <b>fejlkoder</b>, ikke de gamle engelske brugertekster (A-09).
/// Application mapper kode til tekst; UI kan oversætte. Reglen handler om <i>hvad der
/// kræves</i>, ikke om den engelske ordlyd, og systemet betjenes på dansk.
/// </para>
/// </remarks>
public static class GuestRules
{
    /// <summary>Fejlkode: fornavn mangler (BR-52, BR-91).</summary>
    public const string FirstNameRequired = "guest.first_name_required";

    /// <summary>Fejlkode: fornavnet er længere end <see cref="Guest.MaxNameLength"/> (A-10).</summary>
    public const string FirstNameTooLong = "guest.first_name_too_long";

    /// <summary>Fejlkode: efternavn mangler (BR-53, BR-92).</summary>
    public const string LastNameRequired = "guest.last_name_required";

    /// <summary>Fejlkode: efternavnet er længere end <see cref="Guest.MaxNameLength"/> (A-10).</summary>
    public const string LastNameTooLong = "guest.last_name_too_long";

    /// <summary>Fejlkode: e-mail mangler (BR-93).</summary>
    public const string EmailRequired = "guest.email_required";

    /// <summary>Fejlkode: e-mailen mangler '@' eller '.' (BR-55, BR-93).</summary>
    public const string EmailInvalid = "guest.email_invalid";

    /// <summary>Fejlkode: e-mailen er længere end <see cref="Guest.MaxEmailLength"/> (A-10).</summary>
    public const string EmailTooLong = "guest.email_too_long";

    /// <summary>Fejlkode: telefonnummer mangler (BR-54, BR-94).</summary>
    public const string PhoneNumberRequired = "guest.phone_number_required";

    /// <summary>Fejlkode: telefonnummeret er længere end <see cref="Guest.MaxPhoneNumberLength"/> (A-10).</summary>
    public const string PhoneNumberTooLong = "guest.phone_number_too_long";

    /// <summary>Fejlkode: land mangler (BR-56, BR-95).</summary>
    public const string CountryRequired = "guest.country_required";

    /// <summary>Fejlkode: landet er længere end <see cref="Guest.MaxCountryLength"/> (A-10).</summary>
    public const string CountryTooLong = "guest.country_too_long";

    /// <summary>Fejlkode: pasnummeret er længere end <see cref="Guest.MaxPassportNumberLength"/> (A-10).</summary>
    public const string PassportNumberTooLong = "guest.passport_number_too_long";

    private const char EmailAtSign = '@';
    private const char EmailDot = '.';

    /// <summary>
    /// Afgør om e-mailen er gyldig (BR-55, BR-93).
    /// </summary>
    /// <param name="email">E-mailadressen.</param>
    /// <returns>Sand hvis adressen indeholder både '@' og '.'.</returns>
    /// <remarks>
    /// Bevidst ikke regex. BR-93 er præcis "indeholder '@' og '.'", og en strammere regel
    /// ville bryde paritet med det gamle system.
    /// </remarks>
    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Contains(EmailAtSign)
        && email.Contains(EmailDot);

    /// <summary>
    /// Samler alle valideringsfejl for en gæst, så en formular kan vise dem på én gang
    /// (BR-60, BR-96).
    /// </summary>
    /// <param name="firstName">Fornavn.</param>
    /// <param name="lastName">Efternavn.</param>
    /// <param name="email">E-mail.</param>
    /// <param name="phoneNumber">Telefonnummer.</param>
    /// <param name="country">Land.</param>
    /// <param name="passportNumber">
    /// Pasnummer. Valgfrit og uvalideret ud over længden (BR-57) — udelades af kald der
    /// kun validerer de fem obligatoriske felter.
    /// </param>
    /// <returns>
    /// Fejlkoder i feltrækkefølge: fornavn, efternavn, e-mail, telefon, land, pasnummer.
    /// Tom liste betyder gyldig.
    /// </returns>
    public static IReadOnlyList<string> Validate(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        string? country,
        string? passportNumber = null)
    {
        var errors = new List<string>();

        AddTextErrors(errors, firstName, Guest.MaxNameLength, FirstNameRequired, FirstNameTooLong);
        AddTextErrors(errors, lastName, Guest.MaxNameLength, LastNameRequired, LastNameTooLong);
        AddEmailErrors(errors, email);
        AddTextErrors(errors, phoneNumber, Guest.MaxPhoneNumberLength, PhoneNumberRequired, PhoneNumberTooLong);
        AddTextErrors(errors, country, Guest.MaxCountryLength, CountryRequired, CountryTooLong);

        if (passportNumber is not null && passportNumber.Length > Guest.MaxPassportNumberLength)
        {
            errors.Add(PassportNumberTooLong);
        }

        return errors;
    }

    /// <summary>
    /// Tilføjer fejlkoder for et obligatorisk tekstfelt: tomt felt eller for lang værdi.
    /// </summary>
    private static void AddTextErrors(
        List<string> errors,
        string? value,
        int maxLength,
        string requiredCode,
        string tooLongCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(requiredCode);
            return;
        }

        if (value.Length > maxLength)
        {
            errors.Add(tooLongCode);
        }
    }

    /// <summary>
    /// Tilføjer fejlkoder for e-mailfeltet. Adskilt fra <see cref="AddTextErrors"/>, fordi
    /// e-mailen ud over længden også har et formatkrav (BR-55).
    /// </summary>
    private static void AddEmailErrors(List<string> errors, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(EmailRequired);
            return;
        }

        if (!IsValidEmail(email))
        {
            errors.Add(EmailInvalid);
        }

        if (email.Length > Guest.MaxEmailLength)
        {
            errors.Add(EmailTooLong);
        }
    }
}