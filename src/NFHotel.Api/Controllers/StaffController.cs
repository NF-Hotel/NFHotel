using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The staff accounts over HTTP (ADR-0017): the change of the own password for every role, and account management and the
/// audit trail for a Manager. The writes of a Manager are limited per account by the policy <see cref="WriteLimitPolicy"/>.
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
