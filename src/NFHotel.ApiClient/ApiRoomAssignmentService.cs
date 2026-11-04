using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;

namespace NFHotel.ApiClient;

/// <summary><see cref="IRoomAssignmentService"/> over HTTP: the API holds the assignments and checks the role of the caller (MIL-060).</summary>
public sealed class ApiRoomAssignmentService : IRoomAssignmentService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiRoomAssignmentService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<RoomAssignmentOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default) =>
        await _api.GetAsync<RoomAssignmentOverviewDto>(ApiRoutes.RoomAssignments, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> AssignAsync(int roomId, AssignRoomRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PutWithoutValueAsync($"{ApiRoutes.RoomAssignments}/{roomId}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default) =>
        await _api.GetAsync<IReadOnlyList<RoomTradeDto>>(ApiRoutes.RoomTrades, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<RoomTradeDto>> OfferTradeAsync(OfferRoomTradeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PostAsync<RoomTradeDto>(ApiRoutes.RoomTrades, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "accept", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "decline", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "withdraw", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "approve", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "reject", cancellationToken);

    private async Task<Result<RoomTradeDto>> StepAsync(int id, string step, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return await _api.PostAsync<RoomTradeDto>($"{ApiRoutes.RoomTrades}/{id}/{step}", null, cancellationToken).ConfigureAwait(false);
    }
}
