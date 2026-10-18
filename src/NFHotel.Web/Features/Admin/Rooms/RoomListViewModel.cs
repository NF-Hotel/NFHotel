using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// Presentation logic for the room overview: filtering, cleaning and operating transitions as well as
/// deletion.
/// </summary>
/// <remarks>
/// B-10: the class contains <b>no</b> business rules. Which transitions are legal
/// on a room comes from <c>RoomListItemDto.Actions</c>, which Application computes from
/// the state machine of the domain — exactly as <c>BookingActionsDto</c> does for bookings
/// (BR-22). <see cref="AvailableActions"/> only translates the true fields to button texts.
/// <para>
/// The service checks anyway on click: between render and click another user may have
/// moved the room on. If that happens, the error code comes back and becomes a Danish message.
/// </para>
/// <para>
/// Every call goes through <see cref="IUseCaseRunner{TService}"/> and thereby gets its own
/// DI scope and its own <c>DbContext</c> (Architecture.md section 8).
/// </para>
/// </remarks>
public sealed class RoomListViewModel
{
    /// <summary>The number of possible buttons — only for the initial capacity of the list.</summary>
    private const int RoomActionCount = 11;

    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="rooms">Runs use cases on <see cref="IRoomService"/>.</param>
    public RoomListViewModel(IUseCaseRunner<IRoomService> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms = rooms;
    }

    /// <summary>The rooms shown right now.</summary>
    public IReadOnlyList<RoomListItemDto> Rooms { get; private set; } = Array.Empty<RoomListItemDto>();

    /// <summary>The options in the dropdowns of the filter, fetched from the service.</summary>
    public RoomFilterOptionsDto? FilterOptions { get; private set; }

    /// <summary>Errors to show to the user, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Receipt after a successful deletion.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is in progress, so buttons can be disabled.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the screen has fetched data at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the list was fetched without error, but is empty.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Rooms.Count == 0;

    /// <summary>True when at least one filter field is set.</summary>
    public bool HasFilter =>
        FilterFloor is not null ||
        FilterSize is not null ||
        FilterStatus is not null ||
        FilterHousekeepingStatus is not null;

    /// <summary>The room the action panel works on, or <c>null</c>.</summary>
    public RoomListItemDto? SelectedRoom { get; private set; }

    /// <summary>The room waiting for the user to confirm a deletion.</summary>
    public RoomListItemDto? RoomPendingDeletion { get; private set; }

    /// <summary>Filter: floor. <c>null</c> means all.</summary>
    public int? FilterFloor { get; set; }

    /// <summary>Filter: room size. <c>null</c> means all.</summary>
    public RoomSize? FilterSize { get; set; }

    /// <summary>Filter: room status. <c>null</c> means all.</summary>
    public RoomStatus? FilterStatus { get; set; }

    /// <summary>Filter: cleaning status. <c>null</c> means all.</summary>
    public HousekeepingStatus? FilterHousekeepingStatus { get; set; }

    /// <summary>
    /// The transitions that are legal on the selected room right now, with Danish button text.
    /// </summary>
    /// <remarks>
    /// The list is empty when no room is selected. The order follows the workflow: operation first,
    /// then the cleaning flow, then service.
    /// </remarks>
    public IReadOnlyList<RoomActionOption> AvailableActions => BuildActions(SelectedRoom?.Actions);

    /// <summary>
    /// Fetches filter options and the first list. Called from <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var optionsLoaded = await LoadFilterOptionsAsync(cancellationToken).ConfigureAwait(false);

        if (!optionsLoaded)
        {
            // If the filter options cannot be fetched, the data source is down. Trying the list
            // afterwards would only make the user wait for the same error twice.
            IsLoaded = true;

            return;
        }

        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches the rooms that match the current filter.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        var filter = new RoomFilter(FilterFloor, FilterSize, FilterStatus, FilterHousekeepingStatus);

        IsBusy = true;

        try
        {
            var result = await _rooms
                .RunAsync((service, token) => service.SearchAsync(filter, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Rooms = Array.Empty<RoomListItemDto>();

                return;
            }

            Errors = Array.Empty<string>();
            Rooms = result.Value;
            SelectedRoom = FindSelectedIn(Rooms);
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
            RoomPendingDeletion = null;
        }
    }

    /// <summary>
    /// Resets all filter fields and fetches the list again.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task ClearFilterAsync(CancellationToken cancellationToken = default)
    {
        FilterFloor = null;
        FilterSize = null;
        FilterStatus = null;
        FilterHousekeepingStatus = null;

        return SearchAsync(cancellationToken);
    }

    /// <summary>
    /// Selects or deselects the room the action panel works on.
    /// </summary>
    /// <param name="room">The room that was clicked.</param>
    public void ToggleSelection(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        SelectedRoom = SelectedRoom?.RoomId == room.RoomId ? null : room;
        RoomPendingDeletion = null;
    }

    /// <summary>
    /// Tells whether a room is the selected one.
    /// </summary>
    /// <param name="room">The room in the row.</param>
    /// <returns>True if the room is selected.</returns>
    public bool IsSelected(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return SelectedRoom?.RoomId == room.RoomId;
    }

    /// <summary>
    /// Performs one of the transitions of the room on the selected room and fetches the list again.
    /// </summary>
    /// <param name="action">The transition to perform.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task ExecuteAsync(RoomAction action, CancellationToken cancellationToken = default)
    {
        var room = SelectedRoom;

        if (room is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        SuccessMessage = null;

        try
        {
            var result = await _rooms
                .RunAsync((service, token) => InvokeAsync(service, action, room.RoomId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);

                return;
            }

            Errors = Array.Empty<string>();
        }
        finally
        {
            IsBusy = false;
        }

        // The list is fetched again, because a transition can move the room out of the active filter.
        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asks for confirmation before a room is deleted.
    /// </summary>
    /// <param name="room">The room the user wants to delete.</param>
    public void RequestDelete(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        RoomPendingDeletion = room;
    }

    /// <summary>
    /// Undoes a deletion that has not yet been confirmed.
    /// </summary>
    public void AbortDelete() => RoomPendingDeletion = null;

    /// <summary>
    /// Deletes the room the user has confirmed (BR-78, BR-112).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// The service rejects rooms with bookings; the screen tries and shows the answer.
    /// </remarks>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        var room = RoomPendingDeletion;

        if (room is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        SuccessMessage = null;

        try
        {
            var result = await _rooms
                .RunAsync((service, token) => service.DeleteAsync(room.RoomId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                RoomPendingDeletion = null;

                return;
            }

            Errors = Array.Empty<string>();
            SuccessMessage = $"Room {room.RoomNumber} was deleted.";

            if (SelectedRoom?.RoomId == room.RoomId)
            {
                SelectedRoom = null;
            }
        }
        finally
        {
            IsBusy = false;
        }

        var message = SuccessMessage;

        await SearchAsync(cancellationToken).ConfigureAwait(false);

        SuccessMessage = message;
    }

    /// <summary>
    /// Clears the receipt from the last action.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;

    /// <summary>
    /// Fetches the options of the filter.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>True if the options were fetched.</returns>
    private async Task<bool> LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var result = await _rooms
            .RunAsync((service, token) => service.GetFilterOptionsAsync(token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);

            return false;
        }

        FilterOptions = result.Value;

        return true;
    }

    private RoomListItemDto? FindSelectedIn(IReadOnlyList<RoomListItemDto> rooms)
    {
        var selectedId = SelectedRoom?.RoomId;

        if (selectedId is null)
        {
            return null;
        }

        // The room must be found again in the new list, otherwise the action panel shows an outdated status.
        return rooms.FirstOrDefault(room => room.RoomId == selectedId.Value);
    }

    /// <summary>
    /// Translates the true fields of <paramref name="actions"/> to buttons.
    /// </summary>
    /// <param name="actions">The legal transitions of the room, or <c>null</c> if no room is selected.</param>
    /// <returns>The buttons in the order they should appear in the panel.</returns>
    private static IReadOnlyList<RoomActionOption> BuildActions(RoomActionsDto? actions)
    {
        if (actions is null)
        {
            return Array.Empty<RoomActionOption>();
        }

        var options = new List<RoomActionOption>(RoomActionCount);

        Add(options, actions.CanTakeOutOfService, RoomAction.TakeOutOfService, "Take out of service");
        Add(options, actions.CanSendToMaintenance, RoomAction.SendToMaintenance, "Send to maintenance");
        Add(options, actions.CanReturnToService, RoomAction.ReturnToService, "Return to service");

        Add(options, actions.CanMarkDailyCleaningDue, RoomAction.MarkDailyCleaningDue, "Mark daily cleaning due");
        Add(options, actions.CanMarkDepartureCleaningDue, RoomAction.MarkDepartureCleaningDue, "Mark departure cleaning due");
        Add(options, actions.CanStartCleaning, RoomAction.StartCleaning, "Start cleaning");
        Add(options, actions.CanCompleteCleaning, RoomAction.CompleteCleaning, "Finish cleaning");

        Add(options, actions.CanReportServiceNeeded, RoomAction.ReportServiceNeeded, "Report service needed");
        Add(options, actions.CanStartService, RoomAction.StartService, "Start service");
        Add(options, actions.CanCompleteService, RoomAction.CompleteServiceClean, "Finish service — room is clean");
        Add(options, actions.CanCompleteService, RoomAction.CompleteServiceStillDirty, "Finish service — room still needs cleaning");

        return options;
    }

    private static void Add(List<RoomActionOption> options, bool isAllowed, RoomAction action, string label)
    {
        if (isAllowed)
        {
            options.Add(new RoomActionOption(action, label));
        }
    }

    private static Task<Result<RoomDetailsDto>> InvokeAsync(
        IRoomService service,
        RoomAction action,
        int roomId,
        CancellationToken cancellationToken) => action switch
        {
            RoomAction.TakeOutOfService => service.TakeOutOfServiceAsync(roomId, cancellationToken),
            RoomAction.SendToMaintenance => service.SendToMaintenanceAsync(roomId, cancellationToken),
            RoomAction.ReturnToService => service.ReturnToServiceAsync(roomId, cancellationToken),
            RoomAction.MarkDailyCleaningDue => service.MarkDailyCleaningDueAsync(roomId, cancellationToken),
            RoomAction.MarkDepartureCleaningDue => service.MarkDepartureCleaningDueAsync(roomId, cancellationToken),
            RoomAction.StartCleaning => service.StartCleaningAsync(roomId, cancellationToken),
            RoomAction.CompleteCleaning => service.CompleteCleaningAsync(roomId, cancellationToken),
            RoomAction.ReportServiceNeeded => service.ReportServiceNeededAsync(roomId, cancellationToken),
            RoomAction.StartService => service.StartServiceAsync(roomId, cancellationToken),
            RoomAction.CompleteServiceStillDirty => service.CompleteServiceAsync(roomId, requiresCleaning: true, cancellationToken),
            RoomAction.CompleteServiceClean => service.CompleteServiceAsync(roomId, requiresCleaning: false, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Ukendt rumhandling.")
        };
}