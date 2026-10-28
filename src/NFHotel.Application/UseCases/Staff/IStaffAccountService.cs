using NFHotel.Application.Dto.Staff;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// Creating staff accounts when the API starts and from its command line (ADR-0005). The screens of a Manager use
/// <see cref="IStaffAdminService"/>.
/// </summary>
public interface IStaffAccountService
{
    /// <summary>Creates an active account.</summary>
    /// <param name="request">The user name, the password and the role.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or the codes of the fields that are wrong, or <c>staff.user_name_taken</c>.</returns>
    Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets a temporary password for an account from the command line, and requires a change at the next sign-in.</summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The temporary password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, <c>staff.not_found</c> or the code of the password rule that fails.</returns>
    Task<Result> ResetPasswordByNameAsync(string userName, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the first Manager when no account exists, and requires a new password at the first sign-in. Does
    /// nothing when an account exists.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success whether it created the account or found one; the codes if the account was refused.</returns>
    Task<Result> EnsureFirstManagerAsync(string userName, string password, CancellationToken cancellationToken = default);

    /// <summary>Creates the first Manager when no account exists. Does nothing when one does.</summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="mustChangePassword">True when the first Manager must choose a new password at the first sign-in (ADR-0017).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success whether it created the account or found one; the codes if the account was refused.</returns>
    Task<Result> EnsureFirstManagerAsync(string userName, string password, bool mustChangePassword, CancellationToken cancellationToken = default);
}
