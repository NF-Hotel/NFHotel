using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IGuestService"/> over HTTP. A key below one and a missing request throw the same exceptions as the
/// real service, before any call is made. <see cref="ValidateFields"/> does no I/O, so it runs the same rule
/// locally instead of calling the API.
/// </summary>
public sealed class ApiGuestService : IGuestService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiGuestService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<GuestDetailsDto>> GetByIdAsync(int guestId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guestId);
        return await _api.GetAsync<GuestDetailsDto>($"{ApiRoutes.Guests}/{guestId}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(searchText)
            ? ApiRoutes.Guests
            : $"{ApiRoutes.Guests}?searchText={Uri.EscapeDataString(searchText)}";
        return await _api.GetAsync<IReadOnlyList<GuestListItemDto>>(path, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<GuestDetailsDto>> CreateAsync(CreateGuestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Fields);
        return await _api.PostAsync<GuestDetailsDto>(ApiRoutes.Guests, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<GuestDetailsDto>> UpdateAsync(UpdateGuestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Fields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.GuestId);
        return await _api.PutAsync<GuestDetailsDto>(ApiRoutes.Guests, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int guestId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guestId);
        return await _api.DeleteAsync($"{ApiRoutes.Guests}/{guestId}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Result ValidateFields(GuestFields fields) => GuestService.Validate(fields);
}
