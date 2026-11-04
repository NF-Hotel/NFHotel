using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The staff accounts over HTTP (ADR-0017, MIL-059): the change of the own password, and the own name while there is none,
/// for every role; account management and the audit trail for a Manager. The writes of a Manager are limited per account by
/// the policy <see cref="WriteLimitPolicy"/>. The mobile app reaches these routes with the staff token alone (ADR-0015).
/// </summary>
[ApiController]
[Route(ApiRoutes.Staff)]
public sealed class StaffController : ControllerBase
{
    /// <summary>The name of the rate limit policy of the Manager writes.</summary>
    public const string WriteLimitPolicy = "staff-manage";

    private readonly IStaffAuthService _auth;
    private readonly IStaffAdminService _admin;

    /// <summary>Creates the controller.</summary>
    /// <param name="auth">The use case of the own password.</param>
    /// <param name="admin">The use cases of a Manager.</param>
    public StaffController(IStaffAuthService auth, IStaffAdminService admin)
    {
        ArgumentNullException.ThrowIfNull(auth);
        ArgumentNullException.ThrowIfNull(admin);

        _auth = auth;
        _admin = admin;
    }

    /// <summary>Changes the password of the signed-in member of staff.</summary>
    /// <param name="request">The current and the new password.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with a new session, 401, 422 with the codes, or 429.</returns>
    [HttpPost(ApiRoutes.StaffMyPassword)]
    [RequiresPermission(Permissions.StaffChangeOwnPassword)]
    public async Task<IActionResult> ChangeOwnPasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _auth.ChangePasswordAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Gives the account of the caller a name while it has none (MIL-059).</summary>
    /// <param name="request">The first and the last name.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with the session, 401, 409 when the account has a name already, or 422 with the codes.</returns>
    [HttpPost(ApiRoutes.StaffMyName)]
    [RequiresPermission(Permissions.StaffSetOwnName)]
    public async Task<IActionResult> SetOwnNameAsync([FromBody] RenameStaffRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _auth.SetOwnNameAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the accounts.</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The accounts.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.StaffManage)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.ListAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Creates an account that must choose a new password at the first sign-in.</summary>
    /// <param name="request">The user name, the temporary password and the role.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 409 for a taken name, or 422 with the codes.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateStaffRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.CreateAsync(request with { MustChangePassword = true }, cancellationToken).ConfigureAwait(false));

    /// <summary>Disables an account.</summary>
    /// <param name="id">The account.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, or 409 for the own account or the last Manager.</returns>
    [HttpPost(ApiRoutes.StaffDisable)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> DisableAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.DisableAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Enables an account again.</summary>
    /// <param name="id">The account.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 or 404.</returns>
    [HttpPost(ApiRoutes.StaffEnable)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> EnableAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.EnableAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Sets a temporary password for an account.</summary>
    /// <param name="id">The account.</param>
    /// <param name="request">The temporary password.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, 409 for the own account, or 422 with the codes of the password rule.</returns>
    [HttpPost(ApiRoutes.StaffResetPassword)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> ResetPasswordAsync(int id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.ResetPasswordAsync(id, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Sets the first and the last name of an account (MIL-059).</summary>
    /// <param name="id">The account.</param>
    /// <param name="request">The names.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, or 422 with the codes.</returns>
    [HttpPost(ApiRoutes.StaffName)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> RenameAsync(int id, [FromBody] RenameStaffRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.RenameAsync(id, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Sets or clears the e-mail address of an account (MIL-059).</summary>
    /// <param name="id">The account.</param>
    /// <param name="request">The address.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, 409 for an address that another account has, or 422 with the codes.</returns>
    [HttpPost(ApiRoutes.StaffEmail)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> ChangeEmailAsync(int id, [FromBody] ChangeEmailRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.ChangeEmailAsync(id, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Gives an account another role, which ends its sessions (MIL-059).</summary>
    /// <param name="id">The account.</param>
    /// <param name="request">The role.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, 409 for the own account or the last Manager, or 422 for an unknown role.</returns>
    [HttpPost(ApiRoutes.StaffRole)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> ChangeRoleAsync(int id, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.ChangeRoleAsync(id, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes an account for good (MIL-059).</summary>
    /// <param name="id">The account.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, 404, or 409 for the own account or the last Manager.</returns>
    [HttpDelete(ApiRoutes.ById)]
    [RequiresPermission(Permissions.StaffManage)]
    [EnableRateLimiting(WriteLimitPolicy)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _admin.DeleteAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Reads one page of the audit trail, newest first.</summary>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The lines per page.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The page.</returns>
    [HttpGet(ApiRoutes.StaffAudit)]
    [RequiresPermission(Permissions.StaffManage)]
    public async Task<IActionResult> ListAuditAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        this.ToResponse(await _admin.ListAuditAsync(page, pageSize, cancellationToken).ConfigureAwait(false));
}
