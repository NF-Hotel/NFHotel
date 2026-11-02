using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// Presentation logic for creating a booking and for rescheduling an existing one.
/// </summary>
/// <remarks>
/// Without this screen the system can only be fed with SQL: <c>CreateAsync</c> and
/// <c>RescheduleAsync</c> had no entry point in the UI.
/// <para>
/// The flow is dates → free rooms → guest. The rooms come from
/// <see cref="IBookingService.GetAvailableRoomsAsync"/>, which knows both bookability
/// (BR-110) and overlap (BR-19) — the screen thus never shows a room it cannot use, and
/// it does not itself decide which ones are free.
/// </para>
/// <para>
/// B-10: the class validates nothing itself. The guest fields ask
/// <see cref="IGuestService.ValidateFields"/>, and everything else is decided by the service when
/// saving. <see cref="CanSave"/> is therefore only a question of whether the form is <i>filled in</i>
/// — not whether it is <i>valid</i>.
/// </para>
/// </remarks>
public sealed class BookingFormViewModel
{
    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IGuestService> _guests;
    private readonly IClock _clock;

    /// <summary>
    /// Creates the view model.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    /// <param name="guests">Runs use cases on <see cref="IGuestService"/>.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public BookingFormViewModel(
        IUseCaseRunner<IBookingService> bookings,
        IUseCaseRunner<IGuestService> guests,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(guests);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _guests = guests;
        _clock = clock;

        CheckInDate = clock.Today;
        CheckOutDate = clock.Today.AddDays(1);
    }

    /// <summary>True when the form is open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Whether the form creates or reschedules.</summary>
    public BookingFormMode Mode { get; private set; } = BookingFormMode.Create;

    /// <summary>The booking being rescheduled, or <c>null</c> on creation.</summary>
    public int? BookingId { get; private set; }

    /// <summary>The number of the booking being rescheduled, for the heading (BR-109).</summary>
    public string? BookingNumber { get; private set; }

    /// <summary>The heading of the form.</summary>
    public string Title => Mode == BookingFormMode.Create
        ? "New booking"
        : $"Reschedule booking {BookingNumber}";

    /// <summary>Arrival date, inclusive.</summary>
    public DateOnly CheckInDate { get; set; }

    /// <summary>Departure date, exclusive.</summary>
    public DateOnly CheckOutDate { get; set; }

    /// <summary>The free rooms in the chosen period (BR-19, BR-110).</summary>
    public IReadOnlyList<AvailableRoomDto> AvailableRooms { get; private set; } =
        Array.Empty<AvailableRoomDto>();

    /// <summary>True when rooms have been searched for at least once with the current dates.</summary>
    public bool HasSearchedRooms { get; private set; }

    /// <summary>True when the search came back without error, but without free rooms.</summary>
    public bool HasNoAvailableRooms =>
        HasSearchedRooms && Errors.Count == 0 && AvailableRooms.Count == 0;

    /// <summary>The chosen room, or <c>null</c>.</summary>
    public int? SelectedRoomId { get; private set; }

    /// <summary>Whether the guest is chosen among the existing ones or created now (BR-47).</summary>
    public BookingGuestMode GuestMode { get; private set; } = BookingGuestMode.Existing;

    /// <summary>Free text for the guest search.</summary>
    public string? GuestSearchText { get; set; }

    /// <summary>The guests that match the search.</summary>
    public IReadOnlyList<GuestListItemDto> Guests { get; private set; } =
        Array.Empty<GuestListItemDto>();

    /// <summary>True when guests have been searched for at least once.</summary>
    public bool HasSearchedGuests { get; private set; }

    /// <summary>The chosen guest, or <c>null</c>.</summary>
    public int? SelectedGuestId { get; private set; }

    /// <summary>New guest: first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>New guest: last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>New guest: email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>New guest: phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>New guest: country.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>New guest: passport number. Optional (BR-57).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>Validation errors on the fields of the new guest, already translated to Danish.</summary>
    public IReadOnlyList<string> GuestErrors { get; private set; } = Array.Empty<string>();

    /// <summary>Errors from the form as a whole, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Receipt after a successful creation or reschedule.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True if the latest save succeeded — used to fetch the list again.</summary>
    public bool SavedSuccessfully { get; private set; }

    /// <summary>True when the fields of the guest are filled in so the domain accepts them.</summary>
    public bool NewGuestIsValid { get; private set; }

    /// <summary>
    /// True when the form is filled in enough that it makes sense to try to save.
    /// </summary>
    /// <remarks>
    /// Not a validation: the period, the bookability of the room and the overlap are decided by the service.
    /// Here it is only checked that the user has made the choices the form asks for, so the button
    /// cannot send off a half-filled request.
    /// </remarks>
    public bool CanSave
    {
        get
        {
            if (IsBusy || SelectedRoomId is null)
            {
                return false;
            }

            if (Mode == BookingFormMode.Reschedule)
            {
                return true;
            }

            return GuestMode == BookingGuestMode.Existing
                ? SelectedGuestId is not null
                : NewGuestIsValid;
        }
    }

    /// <summary>
    /// Opens an empty form for a new booking.
    /// </summary>
    public void BeginCreate()
    {
        Reset();

        Mode = BookingFormMode.Create;
        BookingId = null;
        BookingNumber = null;
        CheckInDate = _clock.Today;
        CheckOutDate = _clock.Today.AddDays(1);
        IsOpen = true;
    }

    /// <summary>
    /// Opens the form to reschedule a booking (BR-14).
    /// </summary>
    /// <param name="booking">The booking to be moved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Searches for free rooms immediately, because the current period of the booking is already chosen.
    /// The booking itself is left out of the lookup, so it does not block its own room.
    /// </remarks>
    public async Task BeginRescheduleAsync(
        BookingListItemDto booking,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Reset();

        Mode = BookingFormMode.Reschedule;
        BookingId = booking.BookingId;
        BookingNumber = booking.BookingNumber;
        CheckInDate = booking.StartDate;
        CheckOutDate = booking.EndDate;
        SelectedRoomId = booking.RoomId;
        IsOpen = true;

        await SearchRoomsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes the form without saving.
    /// </summary>
    public void Close()
    {
        Reset();

        IsOpen = false;
    }

    /// <summary>
    /// Fetches the rooms that are free in the chosen period.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// The dates are not checked here: the service answers with
    /// <c>booking.end_not_after_start</c> or <c>booking.start_date_in_past</c>, and
    /// the screen shows the Danish text. The rule thus exists in one place (BR-103, BR-104).
    /// </remarks>
    public async Task SearchRoomsAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        SuccessMessage = null;

        try
        {
            var checkIn = CheckInDate;
            var checkOut = CheckOutDate;
            var excluded = BookingId;

            var result = await _bookings
                .RunAsync(
                    (service, token) => service.GetAvailableRoomsAsync(checkIn, checkOut, excluded, token),
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                AvailableRooms = Array.Empty<AvailableRoomDto>();
                SelectedRoomId = null;

                return;
            }

            Errors = Array.Empty<string>();
            AvailableRooms = result.Value;

            // The previously chosen room may be occupied in the new period. Then it must be removed
            // from the selection rather than being sent along in the request.
            if (SelectedRoomId is not null &&
                !AvailableRooms.Any(room => room.RoomId == SelectedRoomId.Value))
            {
                SelectedRoomId = null;
            }
        }
        finally
        {
            IsBusy = false;
            HasSearchedRooms = true;
        }
    }

    /// <summary>
    /// Chooses the room the booking should be on.
    /// </summary>
    /// <param name="roomId">The room id.</param>
    public void SelectRoom(int roomId) => SelectedRoomId = roomId;

    /// <summary>
    /// Switches between existing and new guest (BR-47).
    /// </summary>
    /// <param name="mode">The desired mode.</param>
    public void SetGuestMode(BookingGuestMode mode)
    {
        GuestMode = mode;

        // Exactly one of the two may be filled in, so the other choice is reset.
        if (mode == BookingGuestMode.Existing)
        {
            NewGuestIsValid = false;
            GuestErrors = Array.Empty<string>();
        }
        else
        {
            SelectedGuestId = null;

            ValidateNewGuest();
        }
    }

    /// <summary>
    /// Searches among the existing guests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SearchGuestsAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var searchText = GuestSearchText;

            var result = await _guests
                .RunAsync((service, token) => service.SearchAsync(searchText, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Guests = Array.Empty<GuestListItemDto>();

                return;
            }

            Errors = Array.Empty<string>();
            Guests = result.Value;
        }
        finally
        {
            IsBusy = false;
            HasSearchedGuests = true;
        }
    }

    /// <summary>
    /// Chooses the guest the booking should be on.
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    public void SelectGuest(int guestId) => SelectedGuestId = guestId;

    /// <summary>
    /// Validates the fields of the new guest against the rules of the domain (BR-52 to BR-56).
    /// </summary>
    /// <remarks>
    /// Deliberately not async — <see cref="IGuestService.ValidateFields"/> does no IO.
    /// All errors are shown at the same time (BR-60, BR-96).
    /// </remarks>
    public void ValidateNewGuest()
    {
        var fields = NewGuestFields();
        var result = _guests.Run(service => service.ValidateFields(fields));

        GuestErrors = ErrorMessages.Describe(result);
        NewGuestIsValid = result.IsSuccess;
    }

    /// <summary>
    /// Saves the form: creates the booking or moves it.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        SavedSuccessfully = false;

        if (IsBusy || SelectedRoomId is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = Mode == BookingFormMode.Create
                ? await CreateAsync(SelectedRoomId.Value, cancellationToken).ConfigureAwait(false)
                : await RescheduleAsync(SelectedRoomId.Value, cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);

                return;
            }

            var booking = result.Value;

            Errors = Array.Empty<string>();
            SavedSuccessfully = true;
            SuccessMessage = Mode == BookingFormMode.Create
                ? $"Booking {booking.BookingNumber} was created."
                : $"Booking {booking.BookingNumber} was moved to the new dates.";

            var message = SuccessMessage;

            Reset();

            SuccessMessage = message;
            IsOpen = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears the receipt from the last save.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;

    private Task<Result<BookingDetailsDto>> CreateAsync(int roomId, CancellationToken cancellationToken)
    {
        // BR-47: exactly one of the two is filled in. The choice in the UI decides which.
        var request = new CreateBookingRequest(
            CheckInDate,
            CheckOutDate,
            roomId,
            GuestMode == BookingGuestMode.Existing ? SelectedGuestId : null,
            GuestMode == BookingGuestMode.New ? NewGuestFields() : null);

        return _bookings.RunAsync(
            (service, token) => service.CreateAsync(request, token),
            cancellationToken);
    }

    private Task<Result<BookingDetailsDto>> RescheduleAsync(int roomId, CancellationToken cancellationToken)
    {
        var request = new RescheduleBookingRequest(
            BookingId ?? 0,
            CheckInDate,
            CheckOutDate,
            roomId);

        return _bookings.RunAsync(
            (service, token) => service.RescheduleAsync(request, token),
            cancellationToken);
    }

    private GuestFields NewGuestFields() => new(
        FirstName,
        LastName,
        Email,
        PhoneNumber,
        Country,
        string.IsNullOrWhiteSpace(PassportNumber) ? null : PassportNumber);

    /// <summary>
    /// Resets all fields, so a new opening never inherits the choices of the previous one.
    /// </summary>
    private void Reset()
    {
        AvailableRooms = Array.Empty<AvailableRoomDto>();
        HasSearchedRooms = false;
        SelectedRoomId = null;

        GuestMode = BookingGuestMode.Existing;
        GuestSearchText = null;
        Guests = Array.Empty<GuestListItemDto>();
        HasSearchedGuests = false;
        SelectedGuestId = null;

        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Country = string.Empty;
        PassportNumber = null;
        NewGuestIsValid = false;

        GuestErrors = Array.Empty<string>();
        Errors = Array.Empty<string>();
        SuccessMessage = null;
    }
}
