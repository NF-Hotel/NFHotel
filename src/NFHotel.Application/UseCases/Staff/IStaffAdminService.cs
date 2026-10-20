using NFHotel.Application.Dto.Staff;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// What a Manager does with staff accounts (UC-006, ADR-0015). Every operation is for the signed-in caller; the
/// permission <see cref="Permissions.StaffManage"/> is checked where the call arrives.
/// </summary>
public interface IStaffAdminService
{
    /// <summary>Lists all accounts.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The accounts ordered by user name.</returns>
    Task<Result<IReadOnlyList<StaffAccountDto>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates an account that must choose a new password at the first sign-in.</summary>
    /// <param name="request">The user name, the temporary password and the role.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or the codes of what is wrong, or <c>staff.user_name_taken</c>.</returns>
    Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);

    /// <summary>Disables an account and ends its sessions.</summary>
    /// <param name="id">The account.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, <c>staff.not_found</c>, <c>staff.cannot_disable_self</c> or <c>staff.last_manager</c>.</returns>
    Task<Result> DisableAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Enables a disabled account; it must choose a new password.</summary>
    /// <param name="id">The account.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success or <c>staff.not_found</c>.</returns>
    Task<Result> EnableAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sets a temporary password, ends the sessions of the account and clears its sign-in limit.</summary>
    /// <param name="id">The account.</param>
    /// <param name="request">The temporary password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, <c>staff.not_found</c>, <c>staff.cannot_reset_self</c> or the code of a password rule.</returns>
    Task<Result> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads one page of the audit trail.</summary>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The lines per page, 1 to 200.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The page, newest line first.</returns>
    Task<Result<StaffAuditPageDto>> ListAuditAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
