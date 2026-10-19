using System.Globalization;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IRoomService"/> over HTTP. A key below one and a missing request throw the same exceptions as the
/// real service, before any call is made; everything else comes back as the same <see cref="Result"/>.
/// </summary>
public sealed class ApiRoomService : IRoomService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiRoomService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<RoomDetailsDto>> GetByIdAsync(int roomId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);
        return await _api.GetAsync<RoomDetailsDto>($"{ApiRoutes.Rooms}/{roomId}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<RoomListItemDto>>(ApiRoutes.Rooms, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(RoomFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return await _api.PostAsync<IReadOnlyList<RoomListItemDto>>($"{ApiRoutes.Rooms}/{ApiRoutes.Search}", filter, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<RoomFilterOptionsDto>> GetFilterOptionsAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<RoomFilterOptionsDto>($"{ApiRoutes.Rooms}/{ApiRoutes.FilterOptions}", cancellationToken);

    /// <inheritdoc />
    public async Task<Result<RoomDetailsDto>> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _api.PostAsync<RoomDetailsDto>(ApiRoutes.Rooms, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RoomDetailsDto>> UpdateAsync(UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.RoomId);
        return await _api.PutAsync<RoomDetailsDto>(ApiRoutes.Rooms, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int roomId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);
        return await _api.DeleteAsync($"{ApiRoutes.Rooms}/{roomId}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> TakeOutOfServiceAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "take-out-of-service", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> SendToMaintenanceAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "send-to-maintenance", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> ReturnToServiceAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "return-to-service", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> MarkDailyCleaningDueAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "mark-daily-cleaning-due", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> MarkDepartureCleaningDueAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "mark-departure-cleaning-due", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> StartCleaningAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "start-cleaning", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> CompleteCleaningAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "complete-cleaning", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> ReportServiceNeededAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "report-service-needed", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> StartServiceAsync(int roomId, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, "start-service", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomDetailsDto>> CompleteServiceAsync(int roomId, bool requiresCleaning, CancellationToken cancellationToken = default) =>
        ActionAsync(roomId, string.Create(CultureInfo.InvariantCulture, $"complete-service?requiresCleaning={(requiresCleaning ? "true" : "false")}"), cancellationToken);

    private async Task<Result<RoomDetailsDto>> ActionAsync(int roomId, string action, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);
        return await _api.PostAsync<RoomDetailsDto>($"{ApiRoutes.Rooms}/{roomId}/{action}", null, cancellationToken).ConfigureAwait(false);
    }
}
