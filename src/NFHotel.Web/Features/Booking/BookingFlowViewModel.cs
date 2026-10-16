using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Al tilstand og alle servicekald i kundens bookingflow på <c>/book</c>:
/// datoer, ledige rum, gæsteoplysninger og kvittering.
/// </summary>
/// <remarks>
/// <c>Book.razor</c> er markup. Den kalder metoderne herunder og læser properties — den
/// kender hverken <c>IBookingService</c>, <c>IGuestService</c> eller en eneste fejlkode
/// (B-10, UI-Spec afsnit 4).
/// <para>
/// Trinnet er en <i>visning</i>, ikke en tilladelse. Hver overgang kontrollerer datoerne
/// og rumvalget igen, så BR-C-04 holder også hvis en tidligere handling efterlod
/// <see cref="CurrentStep"/> længere fremme end indtastningen berettiger.
/// </para>
/// <para>
/// Ingen metode rydder brugerens indtastning. At gå tilbage til trin 1, rette en dato og
/// komme frem igen skal koste et klik, ikke et navn og en e-mail. Det eneste der kan
/// forsvinde, er et rumvalg der ikke længere er ledigt — og det er en regel (BR-C-02),
/// ikke et tab.
/// </para>
/// <para>
/// "I dag" kommer fra <see cref="IClock"/> og aldrig fra <c>DateTime.Now</c> (A-05), så
/// klientens datovalidering bruger samme dag som servicen bagefter.
/// </para>
/// </remarks>
public sealed class BookingFlowViewModel
{
    /// <summary>Standardophold når siden åbnes: én nat, i morgen.</summary>
    private const int DefaultNights = 1;

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();
    private static readonly IReadOnlyList<AvailableRoomDto> NoRooms = Array.Empty<AvailableRoomDto>();

    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IGuestService> _guests;
    private readonly IClock _clock;

    /// <summary>
    /// Opretter viewmodellen med et tomt flow og et forslag til datoer.
    /// </summary>
    /// <param name="bookings">Kører use cases på <see cref="IBookingService"/>.</param>
    /// <param name="guests">Kører use cases på <see cref="IGuestService"/>.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public BookingFlowViewModel(
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

        CheckInDate = clock.Today.AddDays(1);
        CheckOutDate = clock.Today.AddDays(1 + DefaultNights);
    }

    // ------------------------------------------------------------------
    // TILSTAND
    // ------------------------------------------------------------------

    /// <summary>Det trin der vises lige nu.</summary>
    public BookingFlowStep CurrentStep { get; private set; } = BookingFlowStep.Dates;

    /// <summary>Sand mens et servicekald er i gang. Knapper deaktiveres, siden hopper ikke.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Fejl der ikke hører til et enkelt felt, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    /// <summary>Ankomstdato, inklusiv. <c>null</c> indtil brugeren har valgt.</summary>
    public DateOnly? CheckInDate { get; set; }

    /// <summary>Afrejsedato, eksklusiv. <c>null</c> indtil brugeren har valgt.</summary>
    public DateOnly? CheckOutDate { get; set; }

    /// <summary>De rum der er ledige i perioden (BR-C-02). Tom indtil trin 2 er hentet.</summary>
    public IReadOnlyList<AvailableRoomDto> AvailableRooms { get; private set; } = NoRooms;

    /// <summary>Sand når tilgængeligheden er hentet mindst én gang for de aktuelle datoer.</summary>
    public bool AreRoomsLoaded { get; private set; }

    /// <summary>Det valgte rum, eller <c>null</c>.</summary>
    public AvailableRoomDto? SelectedRoom { get; private set; }

    /// <summary>Gæstens fornavn.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gæstens efternavn.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gæstens e-mail.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gæstens telefonnummer.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Gæstens land.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gæstens pasnummer. Valgfrit (BR-57).</summary>
    public string PassportNumber { get; set; } = string.Empty;

    /// <summary>Valideringsfejl fordelt på gæsteformularens felter.</summary>
    public GuestFieldErrors GuestErrors { get; private set; } = GuestFieldErrors.None;

    /// <summary>Den oprettede booking, eller <c>null</c> indtil trin 4.</summary>
    public BookingDetailsDto? Confirmation { get; private set; }

    /// <summary>Sand når brugeren har forsøgt at gå videre fra datotrinnet mindst én gang.</summary>
    /// <remarks>
    /// Styrer alene om "skal udfyldes" må vises. En tom formular er ikke en fejl, før
    /// brugeren har forsøgt at bruge den.
    /// </remarks>
    private bool HasAttemptedDates { get; set; }

    // ------------------------------------------------------------------
    // AFLEDT TILSTAND
    // ------------------------------------------------------------------

    /// <summary>
    /// Sand når begge datoer er udfyldt, ankomsten ikke ligger i fortiden, og afrejsen er
    /// efter ankomsten. Porten til trin 2 (BR-C-04).
    /// </summary>
    public bool HasValidDates =>
        CheckInDate is DateOnly checkIn
        && CheckOutDate is DateOnly checkOut
        && checkIn != default
        && checkIn >= _clock.Today
        && checkOut > checkIn;

    /// <summary>Fejlteksten ved ankomstfeltet, eller <c>null</c>.</summary>
    public string? CheckInError => Describe(CheckInErrorCode);

    /// <summary>Fejlteksten ved afrejsefeltet, eller <c>null</c>.</summary>
    public string? CheckOutError => Describe(CheckOutErrorCode);

    /// <summary>
    /// Antal overnatninger i den valgte periode, eller 0 hvis perioden ikke er hel endnu.
    /// </summary>
    /// <remarks>
    /// Tælles af domænets <see cref="DateRange"/>, så "en nat" har én definition i hele
    /// systemet (BR-109, BR-117). Beregnes uden om <see cref="HasValidDates"/>, så tallet
    /// også opdateres mens brugeren retter en dato der ligger i fortiden.
    /// </remarks>
    public int Nights =>
        CheckInDate is DateOnly checkIn
        && CheckOutDate is DateOnly checkOut
        && checkIn != default
        && checkOut > checkIn
            ? new DateRange(checkIn, checkOut).Nights
            : 0;

    /// <summary>Antallet af nætter som tekst, eller en opfordring hvis perioden ikke er hel.</summary>
    public string NightsText => Nights > 0
        ? PublicDateText.Nights(Nights)
        : "Pick your check-in and check-out dates and we'll work out the nights.";

    /// <summary>Perioden som tekst, eller tom hvis begge datoer ikke er valgt.</summary>
    public string PeriodText =>
        CheckInDate is DateOnly checkIn && CheckOutDate is DateOnly checkOut && checkOut > checkIn
            ? PublicDateText.Period(checkIn, checkOut)
            : string.Empty;

    /// <summary>
    /// Kort opsummering af det valgte til trin 3 — uden pris, som resten af flowet
    /// (BR-C-05).
    /// </summary>
    public string SelectionSummary => SelectedRoom is null
        ? string.Empty
        : string.Join(
            " · ",
            DescribeRoom(SelectedRoom),
            PeriodText,
            PublicDateText.Nights(Nights),
            PaymentText);

    /// <summary>Sand når "find ledige rum" må trykkes.</summary>
    public bool CanFindRooms => !IsBusy && HasValidDates;

    /// <summary>Sand når bookingen må sendes.</summary>
    public bool CanSubmit => !IsBusy && HasValidDates && SelectedRoom is not null;

    /// <summary>
    /// Teksten der står, hvor en pris ellers ville stå. Prismodellen findes ikke i Fase 1
    /// (BR-C-05, BR-31), og et tomt felt ville ligne en fejl frem for et fravalg.
    /// </summary>
    public static string PaymentText => "Payment on arrival";

    // ------------------------------------------------------------------
    // VISNINGSHJÆLPERE
    // ------------------------------------------------------------------

    /// <summary>
    /// Giver klasserne til ét punkt i trinindikatoren.
    /// </summary>
    /// <param name="step">Trinnet der tegnes.</param>
    /// <returns>Klassestrengen, fx <c>"step step-active"</c>.</returns>
    public string StepClass(BookingFlowStep step) => step switch
    {
        _ when step < CurrentStep => "step step-done",
        _ when step == CurrentStep => "step step-active",
        _ => "step"
    };

    /// <summary>
    /// Giver <c>aria-current</c> til ét punkt i trinindikatoren.
    /// </summary>
    /// <param name="step">Trinnet der tegnes.</param>
    /// <returns><c>"step"</c> på det aktuelle trin; ellers <c>null</c>, så attributten udelades.</returns>
    public string? StepAria(BookingFlowStep step) => step == CurrentStep ? "step" : null;

    /// <summary>
    /// Beskriver et rum til kunden: type, etage og hvor mange der er plads til.
    /// </summary>
    /// <param name="room">Rummet.</param>
    /// <returns>Fx <c>"Room 204 · Double room · Floor 2 · sleeps 2"</c>.</returns>
    /// <remarks>
    /// Værelsestypen kommer fra <see cref="DisplayText"/>, så kunde- og admin-siden bruger
    /// samme ord om samme værelsestype.
    /// </remarks>
    public static string DescribeRoom(AvailableRoomDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return string.Join(
            " · ",
            $"Room {room.RoomNumber}",
            DisplayText.Describe(room.Size),
            DescribeFloor(room.Floor),
            DescribeCapacity(room.Capacity));
    }

    /// <summary>
    /// Beskriver en etage.
    /// </summary>
    /// <param name="floor">Etagen som den står på rummet.</param>
    /// <returns>Fx <c>"Floor 2"</c>.</returns>
    /// <remarks>
    /// Tallet skrives som det står i domænet. Etagenumre er husets egne, og en omregning
    /// til "stueetagen" ville være et gæt på en nummerering ingen regel beskriver.
    /// </remarks>
    public static string DescribeFloor(int floor) => $"Floor {floor}";

    /// <summary>
    /// Beskriver et rums kapacitet.
    /// </summary>
    /// <param name="capacity">Antal personer rummet kan rumme.</param>
    /// <returns>Fx <c>"sleeps 2"</c>.</returns>
    public static string DescribeCapacity(int capacity) => capacity == 1
        ? "sleeps 1"
        : $"sleeps {capacity}";

    // ------------------------------------------------------------------
    // OVERGANGE
    // ------------------------------------------------------------------

    /// <summary>
    /// Henter de ledige rum for de valgte datoer og går til trin 2 (BR-C-02, BR-C-04).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Ugyldige datoer holder brugeren på trin 1 med en besked ved feltet. Servicekaldet
    /// sker altså aldrig for en periode vi allerede ved er ulovlig.
    /// </remarks>
    public async Task FindRoomsAsync(CancellationToken cancellationToken = default)
    {
        HasAttemptedDates = true;

        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        var checkIn = CheckInDate!.Value;
        var checkOut = CheckOutDate!.Value;

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync(
                    (service, token) => service.GetAvailableRoomsAsync(checkIn, checkOut, null, token),
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                AvailableRooms = NoRooms;
                AreRoomsLoaded = false;

                return;
            }

            Errors = NoMessages;
            AvailableRooms = result.Value;
            AreRoomsLoaded = true;

            // Et rum valgt til en tidligere periode må ikke følge med over i en ny.
            if (SelectedRoom is not null && !Contains(AvailableRooms, SelectedRoom.RoomId))
            {
                SelectedRoom = null;
            }

            CurrentStep = BookingFlowStep.Room;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Vælger et rum og går til trin 3 (BR-C-04).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <remarks>
    /// Kun rum fra den hentede liste kan vælges. Listen indeholder pr. BR-C-02 kun rum der
    /// er ledige i perioden, så kunden kan ikke vælge et rum hun ikke har fået vist.
    /// </remarks>
    public void SelectRoom(int roomId)
    {
        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        var room = Find(AvailableRooms, roomId);

        if (room is null)
        {
            return;
        }

        SelectedRoom = room;
        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Guest;
    }

    /// <summary>
    /// Går tilbage til trin 1. Indtastningen bevares.
    /// </summary>
    public void BackToDates()
    {
        if (IsBusy)
        {
            return;
        }

        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Dates;
    }

    /// <summary>
    /// Går tilbage til trin 2. Gæsteoplysningerne bevares.
    /// </summary>
    /// <remarks>
    /// Er rummene aldrig hentet — eller er datoerne ændret til noget ugyldigt siden — ender
    /// brugeren på trin 1 i stedet for på en tom rumliste.
    /// </remarks>
    public void BackToRooms()
    {
        if (IsBusy)
        {
            return;
        }

        Errors = NoMessages;
        CurrentStep = HasValidDates && AreRoomsLoaded
            ? BookingFlowStep.Room
            : BookingFlowStep.Dates;
    }

    /// <summary>
    /// Validerer gæsteoplysningerne og opretter bookingen (BR-C-01, BR-C-03).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// <c>ValidateFields</c> kaldes <b>før</b> oprettelsen. Det er ikke en dublering af
    /// reglerne — det er den samme regel, kaldt uden IO — og det er forskellen på at få
    /// sine fejl at se og at få dem at se efter en rundtur i databasen.
    /// <para>
    /// Bookingen oprettes uden <c>GuestId</c> og med <c>NewGuest</c> udfyldt (BR-47).
    /// Statussen sættes af domænet til <c>Pending</c>; bekræftelsen er personalets
    /// handling (BR-C-03), og der er derfor ingen knap til den her.
    /// </para>
    /// <para>
    /// Bliver rummet optaget mellem visningen og klikket, hentes listen igen, og kunden
    /// sendes tilbage til trin 2 med beskeden i behold. Alternativet — en fejl på en
    /// formular hvis rum ikke længere findes — er en blindgyde.
    /// </para>
    /// </remarks>
    public async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            HasAttemptedDates = true;
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        if (SelectedRoom is not { } room)
        {
            CurrentStep = HasValidDates && AreRoomsLoaded ? BookingFlowStep.Room : BookingFlowStep.Dates;

            return;
        }

        var fields = BuildGuestFields();

        var validation = _guests.Run(service => service.ValidateFields(fields));

        if (validation.IsFailure)
        {
            GuestErrors = GuestFieldErrors.FromResult(validation);
            Errors = GuestErrors.Other;

            return;
        }

        GuestErrors = GuestFieldErrors.None;
        Errors = NoMessages;

        var request = new CreateBookingRequest(
            CheckInDate!.Value,
            CheckOutDate!.Value,
            room.RoomId,
            GuestId: null,
            NewGuest: fields);

        Result<BookingDetailsDto> result;

        IsBusy = true;

        try
        {
            result = await _bookings
                .RunAsync((service, token) => service.CreateAsync(request, token), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        if (result.IsSuccess)
        {
            Confirmation = result.Value;
            CurrentStep = BookingFlowStep.Receipt;

            return;
        }

        GuestErrors = GuestFieldErrors.FromResult(result);
        Errors = GuestErrors.Other;

        if (!MentionsRoom(result))
        {
            return;
        }

        var messages = Errors;

        SelectedRoom = null;

        await FindRoomsAsync(cancellationToken).ConfigureAwait(false);

        if (Errors.Count == 0)
        {
            Errors = messages;
        }
    }

    /// <summary>
    /// Starter et nyt flow forfra. Bruges fra kvitteringen.
    /// </summary>
    public void StartOver()
    {
        if (IsBusy)
        {
            return;
        }

        CheckInDate = _clock.Today.AddDays(1);
        CheckOutDate = _clock.Today.AddDays(1 + DefaultNights);
        HasAttemptedDates = false;
        AvailableRooms = NoRooms;
        AreRoomsLoaded = false;
        SelectedRoom = null;
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Country = string.Empty;
        PassportNumber = string.Empty;
        GuestErrors = GuestFieldErrors.None;
        Confirmation = null;
        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Dates;
    }

    // ------------------------------------------------------------------
    // PRIVAT
    // ------------------------------------------------------------------

    /// <summary>Fejlkoden ved ankomstfeltet, eller <c>null</c>.</summary>
    private string? CheckInErrorCode
    {
        get
        {
            if (CheckInDate is not DateOnly checkIn || checkIn == default)
            {
                return HasAttemptedDates ? ErrorCodes.Booking.StartDateRequired : null;
            }

            return checkIn < _clock.Today ? ErrorCodes.Booking.StartDateInPast : null;
        }
    }

    /// <summary>Fejlkoden ved afrejsefeltet, eller <c>null</c>.</summary>
    private string? CheckOutErrorCode
    {
        get
        {
            if (CheckOutDate is not DateOnly checkOut || checkOut == default)
            {
                return HasAttemptedDates ? ErrorCodes.Booking.EndDateRequired : null;
            }

            return CheckInDate is DateOnly checkIn && checkOut <= checkIn
                ? ErrorCodes.Booking.EndNotAfterStart
                : null;
        }
    }

    /// <summary>
    /// Oversætter én fejlkode til dansk gennem <see cref="ErrorMessages"/>.
    /// </summary>
    /// <param name="code">Fejlkoden, eller <c>null</c>.</param>
    /// <returns>Den danske tekst, eller <c>null</c>.</returns>
    /// <remarks>
    /// Datovalideringen bruger med vilje Application-lagets koder frem for sine egne
    /// tekster. Så siger klientvalideringen ordret det samme som servicen ville sige, og
    /// der findes stadig kun ét sted med danske fejltekster (A-09).
    /// </remarks>
    private static string? Describe(string? code) =>
        code is null ? null : ErrorMessages.Describe(new Error(code));

    /// <summary>Bygger gæstefelterne af formularen, trimmet (BR-47).</summary>
    private GuestFields BuildGuestFields() => new(
        FirstName.Trim(),
        LastName.Trim(),
        Email.Trim(),
        PhoneNumber.Trim(),
        Country.Trim(),
        string.IsNullOrWhiteSpace(PassportNumber) ? null : PassportNumber.Trim());

    /// <summary>Sand hvis resultatet handler om rummet frem for om gæsten.</summary>
    private static bool MentionsRoom(Result result)
    {
        foreach (var error in result.Errors)
        {
            if (error.Code is ErrorCodes.Booking.RoomNotAvailable or ErrorCodes.Booking.RoomNotBookable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Finder et rum i listen, eller <c>null</c>.</summary>
    private static AvailableRoomDto? Find(IReadOnlyList<AvailableRoomDto> rooms, int roomId)
    {
        foreach (var room in rooms)
        {
            if (room.RoomId == roomId)
            {
                return room;
            }
        }

        return null;
    }

    /// <summary>Sand hvis listen indeholder rummet.</summary>
    private static bool Contains(IReadOnlyList<AvailableRoomDto> rooms, int roomId) =>
        Find(rooms, roomId) is not null;
}