using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Planning;

/// <summary>
/// State of the room assignments (MIL-060): every room with its housekeeper, the number of rooms of each housekeeper,
/// and the trades that wait for a Manager because they leave somebody below the minimum. The page holds only markup and
/// binding; the API checks every rule again.
/// </summary>
public sealed class RoomAssignmentsViewModel
{
    private readonly IUseCaseRunner<IRoomAssignmentService> _assignments;

    /// <summary>Creates the view model.</summary>
    /// <param name="assignments">Runs the use cases of the assignments.</param>
    public RoomAssignmentsViewModel(IUseCaseRunner<IRoomAssignmentService> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        _assignments = assignments;
    }

    /// <summary>The rooms with their housekeeper, the housekeepers, and the minimum; <c>null</c> before the first load.</summary>
    public RoomAssignmentOverviewDto? Overview { get; private set; }

    /// <summary>The trades that wait for a Manager.</summary>
    public IReadOnlyList<RoomTradeDto> AwaitingApproval { get; private set; } = [];

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the rooms have been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Tells whether a housekeeper has fewer rooms than the minimum.</summary>
    /// <param name="housekeeper">The housekeeper.</param>
    /// <returns>True below the minimum.</returns>
    public bool IsBelowMinimum(HousekeeperDto housekeeper)
    {
        ArgumentNullException.ThrowIfNull(housekeeper);

        return Overview is { } overview && housekeeper.RoomCount < overview.MinimumRooms;
    }

    /// <summary>What a trade is, for example <c>Hilda Berg gives 101, 102 to Otto Lund for 201</c>.</summary>
    /// <param name="trade">The trade.</param>
    /// <returns>The text.</returns>
    public static string Describe(RoomTradeDto trade)
    {
        ArgumentNullException.ThrowIfNull(trade);

        var give = string.Join(", ", trade.Give.Select(room => room.RoomNumber));
        var take = string.Join(", ", trade.Take.Select(room => room.RoomNumber));
        return trade.Take.Count == 0
            ? $"{trade.FromName} gives {give} to {trade.ToName}"
            : $"{trade.FromName} gives {give} to {trade.ToName} for {take}";
    }

    /// <summary>Loads the rooms and the trades that wait.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when everything is loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var overview = await _assignments.RunAsync((service, token) => service.GetOverviewAsync(token), cancellationToken).ConfigureAwait(false);
            var trades = await _assignments.RunAsync((service, token) => service.ListTradesAsync(token), cancellationToken).ConfigureAwait(false);

            Errors = [.. new Result[] { overview, trades }.Where(result => result.IsFailure).SelectMany(ErrorMessages.Describe).Distinct()];
            Overview = overview.IsSuccess ? overview.Value : null;
            AwaitingApproval = trades.IsSuccess ? [.. trades.Value.Where(trade => trade.State == TradeState.AwaitingApproval)] : [];
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Gives a room to a housekeeper, or leaves it without one.</summary>
    /// <param name="room">The room.</param>
    /// <param name="staffId">The housekeeper, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the rooms are loaded again.</returns>
    public Task AssignAsync(RoomAssignmentDto room, int? staffId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        return RunAsync(
            (service, token) => service.AssignAsync(room.RoomId, new AssignRoomRequest(staffId), token),
            staffId is null ? $"Room {room.RoomNumber} has no housekeeper now." : $"Assigned room {room.RoomNumber}.",
            cancellationToken);
    }

    /// <summary>Approves a trade; the rooms change hands.</summary>
    /// <param name="trade">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the rooms are loaded again.</returns>
    public Task ApproveAsync(RoomTradeDto trade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trade);

        return RunAsync(
            async (service, token) => await service.ApproveTradeAsync(trade.Id, token).ConfigureAwait(false),
            "Approved the trade. The rooms have changed hands.",
            cancellationToken);
    }

    /// <summary>Rejects a trade; the rooms stay.</summary>
    /// <param name="trade">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the rooms are loaded again.</returns>
    public Task RejectAsync(RoomTradeDto trade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trade);

        return RunAsync(
            async (service, token) => await service.RejectTradeAsync(trade.Id, token).ConfigureAwait(false),
            "Rejected the trade.",
            cancellationToken);
    }

    private async Task RunAsync(Func<IRoomAssignmentService, CancellationToken, Task<Result>> action, string success, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Result result;
        try
        {
            result = await _assignments.RunAsync(action, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            SuccessMessage = null;
            return;
        }

        Errors = [];
        SuccessMessage = success;
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }
}
