using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Dashboard;

/// <summary>
/// Præsentationslogik for admin-dashboardet: dagens ankomster, dagens afrejser og de rum
/// der afventer rengøring.
/// </summary>
/// <remarks>
/// B-10: klassen afgør ingenting selv. Den henter dagens bookinger gennem
/// <see cref="IBookingService.GetOverviewAsync"/> og alle rum gennem
/// <see cref="IRoomService.SearchAsync"/>, og deler rækkerne op på datoer og på
/// <c>RoomActionsDto</c>.
/// <para>
/// Bemærk hvordan "kræver rengøring" afgøres: ikke ved at opregne rengøringsstatusser her,
/// men ved at spørge <c>Actions.CanStartCleaning</c> — altså om domænet vil tillade at
/// rengøringen startes. Det er den samme kilde knapperne på rumoversigten bruger, så de to
/// skærme aldrig kan blive uenige om hvad der mangler at blive gjort.
/// </para>
/// <para>
/// "I dag" kommer fra <see cref="IClock"/> og aldrig fra <c>DateTime.Now</c> (A-05).
/// </para>
/// </remarks>
public sealed class AdminDashboardViewModel
{
    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Opretter viewmodellen og låser dagens dato fast fra hotellets ur.
    /// </summary>
    /// <param name="bookings">Kører use cases på <see cref="IBookingService"/>.</param>
    /// <param name="rooms">Kører use cases på <see cref="IRoomService"/>.</param>
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

    /// <summary>Hotellets dato i dag.</summary>
    public DateOnly Today { get; }

    /// <summary>Bookinger med ankomst i dag.</summary>
    public IReadOnlyList<BookingListItemDto> Arrivals { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>Bookinger der giver rummet fri i dag (A-01).</summary>
    public IReadOnlyList<BookingListItemDto> Departures { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>Rum hvor rengøringen kan sættes i gang, altså mangler at blive gjort.</summary>
    public IReadOnlyList<RoomListItemDto> RoomsAwaitingCleaning { get; private set; } =
        Array.Empty<RoomListItemDto>();

    /// <summary>Fejl der skal vises til brugeren, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Sand mens et kald er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand når skærmen har hentet data mindst én gang.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Sand når dagen er hentet uden fejl og hverken har ankomster eller afrejser.</summary>
    public bool IsQuietDay =>
        IsLoaded && Errors.Count == 0 && Arrivals.Count == 0 && Departures.Count == 0;

    /// <summary>
    /// Henter dagens tal. Kaldes fra <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    /// <summary>
    /// Henter dagens bookinger og rummenes stand forfra.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
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
    /// Henter bookingerne i dagens vindue og deler dem i ankomster og afrejser.
    /// </summary>
    /// <param name="messages">Fejltekster fra denne del af hentningen lægges her.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Vinduet er én dag: fra i dag til og med i morgen, eksklusiv. Periodefilteret er
    /// altid aktivt, netop for ikke at gentage fejlen hvor en oversigt hentede hele
    /// tabellen og filtrerede i hukommelsen (BR-111).
    /// </remarks>
    private async Task LoadTodaysBookingsAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var query = new BookingOverviewQuery(
            Today,
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
            Departures = Array.Empty<BookingListItemDto>();

            return;
        }

        var bookings = result.Value;

        // EffectiveEndDate og ikke EndDate: en gæst der er rejst tidligt, afleverer rummet
        // i dag, selvom bookingen løber videre på papiret (A-01).
        Arrivals = bookings.Where(booking => booking.StartDate == Today).ToArray();
        Departures = bookings.Where(booking => booking.EffectiveEndDate == Today).ToArray();
    }

    /// <summary>
    /// Henter alle rum og finder dem der afventer rengøring.
    /// </summary>
    /// <param name="messages">Fejltekster fra denne del af hentningen lægges her.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    private async Task LoadRoomsAsync(List<string> messages, CancellationToken cancellationToken)
    {
        var result = await _rooms
            .RunAsync((service, token) => service.GetAllAsync(token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            messages.AddRange(ErrorMessages.Describe(result));
            RoomsAwaitingCleaning = Array.Empty<RoomListItemDto>();

            return;
        }

        RoomsAwaitingCleaning = result.Value
            .Where(room => room.Actions.CanStartCleaning)
            .ToArray();
    }
}