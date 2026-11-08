using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Dashboard;

/// <summary>
/// Presentation logic for the admin dashboard: today's arrivals, today's departures and the rooms
/// that are waiting for cleaning.
/// </summary>
/// <remarks>
/// B-10: the class decides nothing itself. It fetches today's bookings through
/// <see cref="IBookingService.GetOverviewAsync"/> and all rooms through
/// <see cref="IRoomService.SearchAsync"/>, and divides the rows by dates and by
/// <c>RoomActionsDto</c>.
/// <para>
/// Note how "requires cleaning" is decided: not by listing cleaning statuses here,
/// but by asking <c>Actions.CanStartCleaning</c> — that is, whether the domain will allow the
/// cleaning to be started. It is the same source the buttons on the room overview use, so the two
/// screens can never disagree about what remains to be done.
/// </para>
/// <para>
/// "Today" comes from <see cref="IClock"/> and never from <c>DateTime.Now</c> (A-05).
/// </para>
/// </remarks>
public sealed class AdminDashboardViewModel
{
    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Creates the viewmodel and fixes today's date from the hotel's clock.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    /// <param name="rooms">Runs use cases on <see cref="IRoomService"/>.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public AdminDashboardViewModel(
        IUseCaseRunner<IBookingService> bookings,
        IUseCaseRunner<IRoomService> rooms,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _rooms = rooms;

        Today = clock.Today;
    }

    /// <summary>The hotel's date today.</summary>
    public DateOnly Today { get; }

    /// <summary>Bookings with arrival today.</summary>
    public IReadOnlyList<BookingListItemDto> Arrivals { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>The arrivals that are not confirmed yet, and so wait on the desk.</summary>
    public IReadOnlyList<BookingListItemDto> PendingArrivals { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>How many of today's arrivals have been checked in.</summary>
    public int ArrivalsCheckedIn { get; private set; }

    /// <summary>How many of today's departures have been checked out.</summary>
    public int DeparturesCheckedOut { get; private set; }

    /// <summary>The rooms with a guest tonight: confirmed or checked-in bookings that run over the night.</summary>
    public int OccupiedTonight { get; private set; }

    /// <summary>The rooms of the hotel, whatever their state.</summary>
    public int RoomCount { get; private set; }

    /// <summary>The share of the rooms with a guest tonight, 0 to 100.</summary>
    public int OccupancyPercent => RoomCount == 0 ? 0 : (int)Math.Round(100.0 * OccupiedTonight / RoomCount);

    /// <summary>The rooms where cleaning is under way right now.</summary>
    public int RoomsBeingCleaned { get; private set; }

    /// <summary>Rooms that need a technician, or have one at work.</summary>
    public IReadOnlyList<RoomListItemDto> RoomsNeedingService { get; private set; } =
        Array.Empty<RoomListItemDto>();

    /// <summary>Rooms that cannot be sold: out of service or in maintenance.</summary>
    public IReadOnlyList<RoomListItemDto> RoomsOutOfService { get; private set; } =
        Array.Empty<RoomListItemDto>();

    /// <summary>True when nothing waits on a decision: no pending arrival, no room needing service, none blocked.</summary>
    public bool NothingNeedsAttention =>
        PendingArrivals.Count == 0 && RoomsNeedingService.Count == 0 && RoomsOutOfService.Count == 0;

    /// <summary>Bookings that release the room today (A-01).</summary>
    public IReadOnlyList<BookingListItemDto> Departures { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>Rooms where cleaning can be started, that is, still to be done.</summary>
    public IReadOnlyList<RoomListItemDto> RoomsAwaitingCleaning { get; private set; } =
        Array.Empty<RoomListItemDto>();

    /// <summary>Errors to show to the user, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the screen has fetched data at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the day was fetched without error and has neither arrivals nor departures.</summary>
    public bool IsQuietDay =>
        IsLoaded && Errors.Count == 0 && Arrivals.Count == 0 && Departures.Count == 0;

    /// <summary>
    /// Fetches today's figures. Called from <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    /// <summary>
    /// Fetches today's bookings and the state of the rooms from scratch.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var messages = new List<string>();

            await LoadTodaysBookingsAsync(messages, cancellationToken).ConfigureAwait(false);
            await LoadRoomsAsync(messages, cancellationToken).ConfigureAwait(false);

            Errors = messages;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>
    /// Fetches the bookings in today's window and divides them into arrivals and departures.
    /// </summary>
    /// <param name="messages">Error texts from this part of the fetching are put here.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// The window is one day: from today up to and including tomorrow, exclusive. The period filter is
    /// always active, precisely so as not to repeat the fault where an overview fetched the whole
    /// table and filtered in memory (BR-111).
    /// </remarks>
    private async Task LoadTodaysBookingsAsync(List<string> messages, CancellationToken cancellationToken)
    {
        // The window is half-open and tests the effective end against its start (BookingRepository.FilterByWindow),
        // so it has to begin yesterday for a booking that hands its room back today to be in the result at all.
        var query = new BookingOverviewQuery(
            Today.AddDays(-1),
            Today.AddDays(1),
            SearchText: null,
            BookingSortField.RoomNumber,
            SortDirection.Ascending,
            IncludeCancelled: false);

        var result = await _bookings
            .RunAsync((service, token) => service.GetOverviewAsync(query, token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            messages.AddRange(ErrorMessages.Describe(result));
            Arrivals = Array.Empty<BookingListItemDto>();
            PendingArrivals = Array.Empty<BookingListItemDto>();
            Departures = Array.Empty<BookingListItemDto>();
            ArrivalsCheckedIn = 0;
            DeparturesCheckedOut = 0;
            OccupiedTonight = 0;

            return;
        }

        var bookings = result.Value;

        // EffectiveEndDate and not EndDate: a guest who has left early hands the room back
        // today, even though the booking runs on on paper (A-01).
        Arrivals = bookings.Where(booking => booking.StartDate == Today).ToArray();
        Departures = bookings.Where(booking => booking.EffectiveEndDate == Today).ToArray();
        PendingArrivals = Arrivals.Where(booking => booking.Status == BookingStatus.Pending).ToArray();
        ArrivalsCheckedIn = Arrivals.Count(booking => booking.Status == BookingStatus.CheckedIn);
        DeparturesCheckedOut = Departures.Count(booking => booking.Status == BookingStatus.CheckedOut);
        OccupiedTonight = bookings.Count(booking =>
            booking.StartDate <= Today
            && booking.EffectiveEndDate > Today
            && booking.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn);
    }

    /// <summary>
    /// Fetches all rooms and finds the ones waiting for cleaning.
    /// </summary>
    /// <param name="messages">Error texts from this part of the fetching are put here.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task LoadRoomsAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var result = await _rooms
            .RunAsync((service, token) => service.GetAllAsync(token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            messages.AddRange(ErrorMessages.Describe(result));
            RoomsAwaitingCleaning = Array.Empty<RoomListItemDto>();
            RoomsNeedingService = Array.Empty<RoomListItemDto>();
            RoomsOutOfService = Array.Empty<RoomListItemDto>();
            RoomCount = 0;
            RoomsBeingCleaned = 0;

            return;
        }

        var rooms = result.Value;

        RoomsAwaitingCleaning = rooms
            .Where(room => room.Actions.CanStartCleaning)
            .ToArray();
        RoomsNeedingService = rooms
            .Where(room => room.HousekeepingStatus is HousekeepingStatus.ServiceRequired or HousekeepingStatus.ServiceInProgress)
            .ToArray();
        RoomsOutOfService = rooms
            .Where(room => room.Status != RoomStatus.Available)
            .ToArray();
        RoomCount = rooms.Count;
        RoomsBeingCleaned = rooms.Count(room => room.HousekeepingStatus == HousekeepingStatus.CleaningInProgress);
    }
}
