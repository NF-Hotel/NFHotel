using NFHotel.Application.Dto.Staff;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>The sign-in of the back office (ADR-0005).</summary>
public interface IStaffAuthService
{
    /// <summary>
    /// Signs a member of staff in. An unknown user, a wrong password and an inactive account give the same error.
    /// </summary>
    /// <param name="request">The user name and password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The session, or <c>staff.invalid_credentials</c>, or <c>staff.too_many_attempts</c> when the user name
    /// has used up its attempts.
    /// </returns>
    Task<Result<StaffSessionDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password of the signed-in caller (UC-005, ADR-0017). The current password must match, the new one
    /// must pass <see cref="PasswordPolicy"/>, and a wrong current password counts against a limit per account.
    /// </summary>
    /// <param name="request">The current and the new password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// A new session (the old token has ended), or <c>staff.current_password_invalid</c>, the code of the rule that
    /// the new password breaks, <c>staff.too_many_attempts</c>, or <c>api.staff_unauthorized</c> without a caller.
    /// </returns>
    Task<Result<StaffSessionDto>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives the account of the signed-in member a name while it has none, which the mobile app asks for after the first
    /// sign-in (MIL-059). Once there is a name, only a Manager changes it.
    /// </summary>
    /// <param name="request">The first and the last name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The session with the name, or <c>staff.name_already_set</c>, the codes of the names, or <c>api.staff_unauthorized</c>
    /// without a caller.
    /// </returns>
    Task<Result<StaffSessionDto>> SetOwnNameAsync(RenameStaffRequest request, CancellationToken cancellationToken = default);
}
