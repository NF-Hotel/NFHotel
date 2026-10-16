using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// Presentation logic for the booking overview: period filter, free text search, sorting,
/// status filters and the four status transitions that can be performed from the list.
/// </summary>
/// <remarks>
/// B-10: the class knows no status transitions. Which buttons are legal comes from
/// <c>BookingListItemDto.Actions</c> (BR-22), which Application computes from the domain —
/// exactly so that BR-02, BR-11, BR-14, BR-22 and the XAML triggers BR-121 to BR-123 become
/// <b>én</b> implementering i stedet for seks.
/// <para>
/// "Today" comes from <see cref="IClock"/> and never from <c>DateTime.Now</c> (A-05).
/// The period filter is always active — the old system's week and month views did not filter
/// at all and fetched the whole table (BR-111).
/// </para>
/// <para>
/// The status filter is the screen's own: the service delivers the period, and the list is hidden from here.
/// It is a narrowing of the view of rows that have already been fetched, not a rule —
/// the filter can never make a booking look different, only be hidden.
/// </para>
/// </remarks>
public sealed class BookingListViewModel
{
    private const int DefaultPeriodLengthInDays = 14;

    private readonly IUseCaseRunner<IBookingService> _bookings;

    private IReadOnlyList<BookingListItemDto> _allBookings = Array.Empty<BookingListItemDto>();

    /// <summary>
    /// Creates the viewmodel and sets the default period to the next 14 days.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public BookingListViewModel(IUseCaseRunner<IBookingService> bookings, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;

        PeriodStart = clock.Today;
        PeriodEnd = clock.Today.AddDays(DefaultPeriodLengthInDays);
    }

    /// <summary>The bookings shown right now, after the status filter.</summary>
    public IReadOnlyList<BookingListItemDto> Bookings { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>Errors to show to the user, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the screen has fetched data at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the list was fetched without error, but is empty.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Bookings.Count == 0;

    /// <summary>True when the period has bookings, but the status filter hides them all.</summary>
    public bool IsHiddenByStatusFilter => IsEmpty && _allBookings.Count > 0;

    /// <summary>The first date of the period, inclusive.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>The last date of the period, exclusive.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Free text on guest name or room number (BR-23, BR-25).</summary>
    public string? SearchText { get; set; }

    /// <summary>The field to sort on (BR-32).</summary>
    public BookingSortField SortBy { get; private set; } = BookingSortField.StartDate;

    /// <summary>Sorteringsretningen.</summary>
    public SortDirection Direction { get; private set; } = SortDirection.Ascending;

    /// <summary>Whether cancelled bookings are fetched too (the soft delete of BR-13).</summary>
    public bool IncludeCancelled { get; set; }

    /// <summary>Show only this status. <c>null</c> shows all fetched bookings.</summary>
    public BookingStatus? StatusFilter { get; set; }

    /// <summary>The booking waiting for the user to confirm a cancellation (BR-04).</summary>
    public BookingListItemDto? BookingPendingCancellation { get; private set; }

    /// <summary>
    /// Fetches the first list. Called from <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        SearchAsync(cancellationToken);

    /// <summary>
    /// Fetches the bookings that match the current query.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        var query = new BookingOverviewQuery(
            PeriodStart,
            PeriodEnd,
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            SortBy,
            Direction,
            IncludeCancelled);

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync((service, token) => service.GetOverviewAsync(query, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                _allBookings = Array.Empty<BookingListItemDto>();
                Bookings = Array.Empty<BookingListItemDto>();

                return;
            }

            Errors = Array.Empty<string>();
            _allBookings = result.Value;
            ApplyStatusFilter();
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;

            // A row that has been fetched again is not the same instance as the one the user
            // clicked on. The confirmation would point to an outdated view.
            BookingPendingCancellation = null;
        }
    }

    /// <summary>
    /// Clears search field, status filter and cancelled, and fetches the list again.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task ClearFilterAsync(CancellationToken cancellationToken = default)
    {
        SearchText = null;
        StatusFilter = null;
        IncludeCancelled = false;

        return SearchAsync(cancellationToken);
    }

    /// <summary>
    /// Sets the status filter and updates the displayed list without fetching again.
    /// </summary>
    /// <param name="status">The status to show, or <c>null</c> for all.</param>
    /// <remarks>
    /// Filters in the result already fetched, because the period filter — the one that
    /// decides how much is fetched — is unchanged. A service call would give the same
    /// rows again.
    /// </remarks>
    public void FilterByStatus(BookingStatus? status)
    {
        StatusFilter = status;

        ApplyStatusFilter();
    }

    /// <summary>
    /// Sorts on a field. A click on the field already sorted on reverses the direction.
    /// </summary>
    /// <param name="field">The field to sort on.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task SortByAsync(BookingSortField field, CancellationToken cancellationToken = default)
    {
        Direction = SortBy == field && Direction == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;

        SortBy = field;

        return SearchAsync(cancellationToken);
    }

    /// <summary>
    /// Tells whether a column is the one currently sorted on.
    /// </summary>
    /// <param name="field">Kolonnens felt.</param>
    /// <returns>True if the list is sorted on the field.</returns>
    public bool IsSortedBy(BookingSortField field) => SortBy == field;

    /// <summary>
    /// Gives the value for <c>aria-sort</c> on a column heading.
    /// </summary>
    /// <param name="field">Kolonnens felt.</param>
    /// <returns><c>"ascending"</c>, <c>"descending"</c> or <c>"none"</c>.</returns>
    public string AriaSortFor(BookingSortField field)
    {
        if (SortBy != field)
        {
            return "none";
        }

        return Direction == SortDirection.Ascending ? "ascending" : "descending";
    }

    /// <summary>
    /// Asks for confirmation before a cancellation (BR-04).
    /// </summary>
    /// <param name="booking">The booking the user wants to cancel.</param>
    public void RequestCancel(BookingListItemDto booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        BookingPendingCancellation = booking;
    }

    /// <summary>
    /// Undoes a cancellation that has not yet been confirmed.
    /// </summary>
    public void AbortCancel() => BookingPendingCancellation = null;

    /// <summary>
    /// Carries out the cancellation the user has confirmed (BR-04, BR-11).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task ConfirmCancelAsync(CancellationToken cancellationToken = default)
    {
        var booking = BookingPendingCancellation;

        if (booking is null)
        {
            return;
        }

        await ExecuteAsync(BookingAction.Cancel, booking.BookingId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs one of the status transitions of the booking and fetches the list again.
    /// </summary>
    /// <param name="action">The transition to perform.</param>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// The button is shown only when <c>Actions</c> says it is legal, but the service checks
    /// anyway: between render and click another user may have moved the booking on.
    /// If that happens, the error code comes back and becomes a neutral Danish message.
    /// </remarks>
    public async Task ExecuteAsync(
        BookingAction action,
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync((service, token) => InvokeAsync(service, action, bookingId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                BookingPendingCancellation = null;

                return;
            }

            Errors = Array.Empty<string>();
        }
        finally
        {
            IsBusy = false;
        }

        // Fetched again, because the transition can move the booking out of the filter — for example a
        // cancellation when "show cancelled" is switched off.
        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the status filter to the fetched rows.
    /// </summary>
    private void ApplyStatusFilter() =>
        Bookings = StatusFilter is null
            ? _allBookings
            : _allBookings.Where(booking => booking.Status == StatusFilter.Value).ToArray();

    private static Task<Result<BookingDetailsDto>> InvokeAsync(
        IBookingService service,
        BookingAction action,
        int bookingId,
        CancellationToken cancellationToken) => action switch
        {
            BookingAction.Confirm => service.ConfirmAsync(bookingId, cancellationToken),
            BookingAction.CheckIn => service.CheckInAsync(bookingId, cancellationToken),
            BookingAction.CheckOut => service.CheckOutAsync(bookingId, cancellationToken),
            BookingAction.Cancel => service.CancelAsync(bookingId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Ukendt bookinghandling.")
        };
}