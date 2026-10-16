using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Errors;

/// <summary>
/// Fejlkoderne for brud på guest-invarianterne. Koden er kontrakten (A-09); Application oversætter den til tekst.
/// </summary>
public static class GuestErrors
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
}