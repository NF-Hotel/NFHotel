using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>The guest use cases over HTTP: one endpoint per method of <see cref="IGuestService"/>.</summary>
[ApiController]
[Route(ApiRoutes.Guests)]
public sealed class GuestsController : ControllerBase
{
    private readonly IGuestService _guests;

    /// <summary>Creates the controller.</summary>
    /// <param name="guests">The guest use cases.</param>
    public GuestsController(IGuestService guests)
    {
        ArgumentNullException.ThrowIfNull(guests);
        _guests = guests;
    }

    /// <summary>Gets one guest.</summary>
    /// <param name="id">The guest key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The details, or 404.</returns>
    [HttpGet(ApiRoutes.ById)]
    [RequiresPermission(Permissions.ViewOtherGuestData)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _guests.GetByIdAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Searches guests by name, e-mail or country.</summary>
    /// <param name="searchText">The text to look for; empty lists everyone.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The matching guests, sorted by name.</returns>
    [HttpGet]
    [RequiresPermission(Permissions.ViewOtherGuestData)]
    public async Task<IActionResult> SearchAsync([FromQuery] string? searchText, CancellationToken cancellationToken) =>
        this.ToResponse(await _guests.SearchAsync(searchText, cancellationToken).ConfigureAwait(false));

    /// <summary>Creates a guest.</summary>
    /// <param name="request">The fields of the guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The new guest.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.GuestsManage)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateGuestRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _guests.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Updates a guest.</summary>
    /// <param name="request">The key and the new fields.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The guest after the change.</returns>
    [HttpPut]
    [RequiresPermission(Permissions.GuestsManage)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateGuestRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _guests.UpdateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes a guest who has no bookings.</summary>
    /// <param name="id">The guest key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200, or the reason it was refused.</returns>
    [HttpDelete(ApiRoutes.ById)]
    [RequiresPermission(Permissions.GuestsManage)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _guests.DeleteAsync(id, cancellationToken).ConfigureAwait(false));
}