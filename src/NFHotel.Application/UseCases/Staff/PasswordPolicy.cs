using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// The one place that decides whether a new staff password is acceptable (ADR-0015): length, not the user name, not
/// the current password, not a common one. There are no composition rules (NIST SP 800-63B).
/// </summary>
public static class PasswordPolicy
{
    /// <summary>The shortest password that is accepted.</summary>
    public const int MinLength = 12;

    /// <summary>
    /// The longest password that is accepted. A longer one is refused without hashing it, so it cannot be used to
    /// burn processor time.
    /// </summary>
    public const int MaxLength = 256;

    /// <summary>Checks a new password.</summary>
    /// <param name="password">The new password as typed.</param>
    /// <param name="userName">The user name of the account, in any case.</param>
    /// <param name="currentPassword">The current password, or <c>null</c> when there is none to compare with.</param>
    /// <returns>The error code of the first rule that fails, or <c>null</c> when the password is acceptable.</returns>
    public static string? Check(string? password, string? userName, string? currentPassword = null)
    {
        var text = password ?? string.Empty;
        if (text.Length < MinLength || text.Length > MaxLength)
        {
            return ErrorCodes.Staff.PasswordInvalid;
        }

        if (!string.IsNullOrWhiteSpace(userName) && string.Equals(text, userName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCodes.Staff.PasswordSameAsUserName;
        }

        if (currentPassword is not null && string.Equals(text, currentPassword, StringComparison.Ordinal))
        {
            return ErrorCodes.Staff.PasswordSameAsCurrent;
        }

        return CommonPasswords.IsCommon(text) ? ErrorCodes.Staff.PasswordTooCommon : null;
    }
}
