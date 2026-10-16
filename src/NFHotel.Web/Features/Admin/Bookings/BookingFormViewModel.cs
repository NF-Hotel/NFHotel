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
/// Præsentationslogik for at oprette en booking og for at omlægge en eksisterende.
/// </summary>
/// <remarks>
/// Uden denne skærm kan systemet kun fodres med SQL: <c>CreateAsync</c> og
/// <c>RescheduleAsync</c> havde ingen indgang i UI'et.
/// <para>
/// Flowet er datoer → ledige rum → gæst. Rummene kommer fra
/// <see cref="IBookingService.GetAvailableRoomsAsync"/>, som både kender bookbarhed
/// (BR-110) og overlap (BR-19) — skærmen viser altså aldrig et rum den ikke kan bruge, og
/// den afgør heller ikke selv hvilke der er ledige.
/// </para>
/// <para>
/// B-10: klassen validerer ingenting selv. Gæstefelterne spørger
/// <see cref="IGuestService.ValidateFields"/>, og alt andet afgøres af servicen når der
/// gemmes. <see cref="CanSave"/> er derfor kun et spørgsmål om formularen er <i>udfyldt</i>
/// — ikke om den er <i>gyldig</i>.
/// </para>
/// </remarks>
public sealed class BookingFormViewModel
{
    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IGuestService> _guests;
    private readonly IClock _clock;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="bookings">Kører use cases på <see cref="IBookingService"/>.</param>
    /// <param name="guests">Kører use cases på <see cref="IGuestService"/>.</param>
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

    /// <summary>Sand når formularen er åben.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Om formularen opretter eller omlægger.</summary>
    public BookingFormMode Mode { get; private set; } = BookingFormMode.Create;

    /// <summary>Bookingen der omlægges, eller <c>null</c> ved oprettelse.</summary>
    public int? BookingId { get; private set; }

    /// <summary>Bookingnummeret der omlægges, til overskriften (BR-109).</summary>
    public string? BookingNumber { get; private set; }

    /// <summary>Formularens overskrift.</summary>
    public string Title => Mode == BookingFormMode.Create
        ? "New booking"
        : $"Reschedule booking {BookingNumber}";

    /// <summary>Ankomstdato, inklusiv.</summary>
    public DateOnly CheckInDate { get; set; }

    /// <summary>Afrejsedato, eksklusiv.</summary>
    public DateOnly CheckOutDate { get; set; }

    /// <summary>De ledige rum i den valgte periode (BR-19, BR-110).</summary>
    public IReadOnlyList<AvailableRoomDto> AvailableRooms { get; private set; } =
        Array.Empty<AvailableRoomDto>();

    /// <summary>Sand når der er søgt efter rum mindst én gang med de nuværende datoer.</summary>
    public bool HasSearchedRooms { get; private set; }

    /// <summary>Sand når søgningen kom tilbage uden fejl, men uden ledige rum.</summary>
    public bool HasNoAvailableRooms =>
        HasSearchedRooms && Errors.Count == 0 && AvailableRooms.Count == 0;

    /// <summary>Det valgte rum, eller <c>null</c>.</summary>
    public int? SelectedRoomId { get; private set; }

    /// <summary>Om gæsten vælges blandt de eksisterende eller oprettes nu (BR-47).</summary>
    public BookingGuestMode GuestMode { get; private set; } = BookingGuestMode.Existing;

    /// <summary>Fritekst til gæstesøgningen.</summary>
    public string? GuestSearchText { get; set; }

    /// <summary>Gæsterne der matcher søgningen.</summary>
    public IReadOnlyList<GuestListItemDto> Guests { get; private set; } =
        Array.Empty<GuestListItemDto>();

    /// <summary>Sand når der er søgt efter gæster mindst én gang.</summary>
    public bool HasSearchedGuests { get; private set; }

    /// <summary>Den valgte gæst, eller <c>null</c>.</summary>
    public int? SelectedGuestId { get; private set; }

    /// <summary>Ny gæst: fornavn.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Ny gæst: efternavn.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Ny gæst: e-mail.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Ny gæst: telefonnummer.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Ny gæst: land.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Ny gæst: pasnummer. Valgfrit (BR-57).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>Valideringsfejl på den nye gæsts felter, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> GuestErrors { get; private set; } = Array.Empty<string>();

    /// <summary>Fejl fra formularen som helhed, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Kvittering efter en vellykket oprettelse eller omlægning.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>Sand mens et kald er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand hvis det seneste gem lykkedes — bruges til at hente listen igen.</summary>
    public bool SavedSuccessfully { get; private set; }

    /// <summary>Sand når gæstens felter er udfyldt så domænet accepterer dem.</summary>
    public bool NewGuestIsValid { get; private set; }

    /// <summary>
    /// Sand når formularen er udfyldt nok til at det giver mening at forsøge at gemme.
    /// </summary>
    /// <remarks>
    /// Ikke en validering: perioden, rummets bookbarhed og overlappet afgøres af servicen.
    /// Her tjekkes kun at brugeren har truffet de valg formularen beder om, så knappen
    /// ikke kan sende en halvt udfyldt anmodning af sted.
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
    /// Åbner en tom formular til en ny booking.
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
    /// Åbner formularen for at omlægge en booking (BR-14).
    /// </summary>
    /// <param name="booking">Bookingen der skal flyttes.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Søger straks efter ledige rum, fordi bookingens nuværende periode allerede er valgt.
    /// Bookingen selv udelades fra opslaget, så den ikke spærrer for sit eget rum.
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
    /// Lukker formularen uden at gemme.
    /// </summary>
    public void Close()
    {
        Reset();

        IsOpen = false;
    }

    /// <summary>
    /// Henter de rum der er ledige i den valgte periode.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Datoerne kontrolleres ikke her: servicen svarer med
    /// <c>booking.end_not_after_start</c> eller <c>booking.start_date_in_past</c>, og
    /// skærmen viser den danske tekst. Reglen findes dermed ét sted (BR-103, BR-104).
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

            // Det tidligere valgte rum kan være optaget i den nye periode. Så skal det ud
            // af valget frem for at blive sendt med i anmodningen.
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
    /// Vælger rummet bookingen skal ligge på.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    public void SelectRoom(int roomId) => SelectedRoomId = roomId;

    /// <summary>
    /// Skifter mellem eksisterende og ny gæst (BR-47).
    /// </summary>
    /// <param name="mode">Den ønskede tilstand.</param>
    public void SetGuestMode(BookingGuestMode mode)
    {
        GuestMode = mode;

        // Præcis én af de to må være udfyldt, så det andet valg nulstilles.
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
    /// Søger blandt de eksisterende gæster.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
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
    /// Vælger den gæst bookingen skal stå på.
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    public void SelectGuest(int guestId) => SelectedGuestId = guestId;

    /// <summary>
    /// Validerer den nye gæsts felter mod domænets regler (BR-52 til BR-56).
    /// </summary>
    /// <remarks>
    /// Bevidst ikke async — <see cref="IGuestService.ValidateFields"/> laver ingen IO.
    /// Alle fejl vises samtidig (BR-60, BR-96).
    /// </remarks>
    public void ValidateNewGuest()
    {
        var fields = NewGuestFields();
        var result = _guests.Run(service => service.ValidateFields(fields));

        GuestErrors = ErrorMessages.Describe(result);
        NewGuestIsValid = result.IsSuccess;
    }

    /// <summary>
    /// Gemmer formularen: opretter bookingen eller flytter den.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
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
    /// Rydder kvitteringen fra sidste gem.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;

    private Task<Result<BookingDetailsDto>> CreateAsync(int roomId, CancellationToken cancellationToken)
    {
        // BR-47: præcis én af de to udfyldes. Valget i UI'et afgør hvilken.
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
    /// Nulstiller alle felter, så en ny åbning aldrig arver den forriges valg.
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