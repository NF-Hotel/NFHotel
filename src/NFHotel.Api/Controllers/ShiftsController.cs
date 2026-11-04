using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.UseCases.Shifts;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The shifts (MIL-060): every member of staff reads the shifts of the own role, a Manager reads and plans all of them.
/// The trades are in <see cref="ShiftTradesController"/>.
/// </summary>
[ApiController]
[Route(ApiRoutes.Shifts)]
public sealed class ShiftsController : ControllerBase
{
    private readonly IShiftService _shifts;

    /// <summary>Creates the controller.</summary>
    /// <param name="shifts">The use cases of the shifts.</param>
    public ShiftsController(IShiftService shifts)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        _shifts = shifts;
    }

    /// <summary>Lists the shifts that overlap a period.</summary>
    /// <param name="from">The start of the period.</param>
    /// <param name="to">The end of the period; at most 62 days after the start.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shifts by start, or 422 for a period that is not valid.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ShiftsView)]
    public async Task<IActionResult> ListAsync([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.ListAsync(from, to, cancellationToken).ConfigureAwait(false));

    /// <summary>Plans a shift.</summary>
    /// <param name="request">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shift, 409 for an overlap, or 422 with the codes.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.ShiftsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> CreateAsync([FromBody] SaveShiftRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Changes a shift.</summary>
    /// <param name="id">The shift.</param>
    /// <param name="request">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shift, 404, 409 for an overlap, or 422 with the codes.</returns>
    [HttpPut(ApiRoutes.ById)]
    [RequiresPermission(Permissions.ShiftsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] SaveShiftRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes a shift and its trades.</summary>
    /// <param name="id">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>200 or 404.</returns>
    [HttpDelete(ApiRoutes.ById)]
    [RequiresPermission(Permissions.ShiftsManage)]
    [EnableRateLimiting(StaffController.WriteLimitPolicy)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _shifts.DeleteAsync(id, cancellationToken).ConfigureAwait(false));
}
