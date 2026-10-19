using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Distributes a failed <see cref="Result"/> over the fields of the guest form, so each error
/// stands at the field it is about.
/// </summary>
/// <remarks>
/// Application collects all broken rules at once (BR-60, BR-96). If they are shown as one list
/// at the top, the user must guess which field "The email must contain both '@' and '.'"
/// belongs to. This type is that translation — and only that: the texts still come from
/// <see cref="ErrorMessages"/>, so there is no Danish string here.
/// <para>
/// Codes the class does not know — for example <c>booking.room_not_available</c> from the creation —
/// end up in <see cref="Other"/> and are shown as an ordinary error message. An unknown code must
/// never disappear silently.
/// </para>
/// </remarks>
public sealed class GuestFieldErrors
{
    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    /// <summary>No errors. The starting point of the form.</summary>
    public static readonly GuestFieldErrors None = new();

    /// <summary>The error text at first name, or <c>null</c>.</summary>
    public string? FirstName { get; private init; }

    /// <summary>The error text at last name, or <c>null</c>.</summary>
    public string? LastName { get; private init; }

    /// <summary>The error text at email, or <c>null</c>.</summary>
    public string? Email { get; private init; }

    /// <summary>The error text at phone number, or <c>null</c>.</summary>
    public string? PhoneNumber { get; private init; }

    /// <summary>The error text at country, or <c>null</c>.</summary>
    public string? Country { get; private init; }

    /// <summary>The error text at passport number, or <c>null</c>.</summary>
    public string? PassportNumber { get; private init; }

    /// <summary>The errors that do not belong to a single field. Shown together above the form.</summary>
    public IReadOnlyList<string> Other { get; private init; } = NoMessages;

    /// <summary>
    /// Distributes the errors of the result over the fields.
    /// </summary>
    /// <param name="result">The result from <c>ValidateFields</c> or <c>CreateAsync</c>.</param>
    /// <returns>The errors distributed per field. <see cref="None"/> if the result succeeded.</returns>
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
