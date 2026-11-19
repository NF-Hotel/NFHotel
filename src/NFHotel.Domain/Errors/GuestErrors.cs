using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Errors;

/// <summary>
/// The error codes for breaches of the guest invariants. The code is the contract (A-09); Application translates it to text.
/// </summary>
public static class GuestErrors
{
    /// <summary>Error code: first name is missing (BR-52, BR-91).</summary>
    public const string FirstNameRequired = "guest.first_name_required";

    /// <summary>Error code: the first name is longer than <see cref="Guest.MaxNameLength"/> (A-10).</summary>
    public const string FirstNameTooLong = "guest.first_name_too_long";

    /// <summary>Error code: the first name holds a character that no name holds (MIL-086, ADR-0050).</summary>
    public const string FirstNameInvalid = "guest.first_name_invalid";

    /// <summary>Error code: last name is missing (BR-53, BR-92).</summary>
    public const string LastNameRequired = "guest.last_name_required";

    /// <summary>Error code: the last name is longer than <see cref="Guest.MaxNameLength"/> (A-10).</summary>
    public const string LastNameTooLong = "guest.last_name_too_long";

    /// <summary>Error code: the last name holds a character that no name holds (MIL-086, ADR-0050).</summary>
    public const string LastNameInvalid = "guest.last_name_invalid";

    /// <summary>Error code: email is missing (BR-93).</summary>
    public const string EmailRequired = "guest.email_required";

    /// <summary>Error code: the email lacks '@' or '.' (BR-55, BR-93).</summary>
    public const string EmailInvalid = "guest.email_invalid";

    /// <summary>Error code: the email is longer than <see cref="Guest.MaxEmailLength"/> (A-10).</summary>
    public const string EmailTooLong = "guest.email_too_long";

    /// <summary>Error code: phone number is missing (BR-54, BR-94).</summary>
    public const string PhoneNumberRequired = "guest.phone_number_required";

    /// <summary>Error code: the phone number is longer than <see cref="Guest.MaxPhoneNumberLength"/> (A-10).</summary>
    public const string PhoneNumberTooLong = "guest.phone_number_too_long";

    /// <summary>Error code: the phone number is not 7 to 15 digits with an optional leading plus (MIL-086, ADR-0050).</summary>
    public const string PhoneNumberInvalid = "guest.phone_number_invalid";

    /// <summary>Error code: country is missing (BR-56, BR-95).</summary>
    public const string CountryRequired = "guest.country_required";

    /// <summary>Error code: the country is longer than <see cref="Guest.MaxCountryLength"/> (A-10).</summary>
    public const string CountryTooLong = "guest.country_too_long";

    /// <summary>Error code: the country is not a name of the list of countries (MIL-086, ADR-0050).</summary>
    public const string CountryInvalid = "guest.country_invalid";

    /// <summary>Error code: the passport number is longer than <see cref="Guest.MaxPassportNumberLength"/> (A-10).</summary>
    public const string PassportNumberTooLong = "guest.passport_number_too_long";

    /// <summary>Error code: the passport number holds other characters than ASCII letters and digits (MIL-086, ADR-0050).</summary>
    public const string PassportNumberInvalid = "guest.passport_number_invalid";
}
