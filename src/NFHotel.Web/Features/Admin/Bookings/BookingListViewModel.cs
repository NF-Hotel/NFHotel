using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// Præsentationslogik for bookingoversigten: periodefilter, fritekstsøgning, sortering,
/// statusfiltre og de fire statusovergange der kan udføres fra listen.
/// </summary>
/// <remarks>
/// B-10: klassen kender ingen statusovergange. Hvilke knapper der er lovlige, kommer fra
/// <c>BookingListItemDto.Actions</c> (BR-22), som Application beregner ud fra domænet —
/// præcis så BR-02, BR-11, BR-14, BR-22 og XAML-triggerne BR-121 til BR-123 bliver til
/// <b>én</b> implementering i stedet for seks.
/// <para>
/// "I dag" kommer fra <see cref="IClock"/> og aldrig fra <c>DateTime.Now</c> (A-05).
/// Periodefilteret er altid aktivt — det gamle systems uge- og månedsvisning filtrerede
/// slet ikke og hentede hele tabellen (BR-111).
/// </para>
/// <para>
/// Statusfilteret er skærmens eget: servicen leverer perioden, og listen skjules herfra.
/// Det er en visningsindsnævring af rækker der allerede er hentet, ikke en regel —
/// filteret kan aldrig få en booking til at se anderledes ud, kun til at være skjult.
/// </para>
/// </remarks>
public sealed class BookingListViewModel
{
    private const int DefaultPeriodLengthInDays = 14;

    private readonly IUseCaseRunner<IBookingService> _bookings;

    private IReadOnlyList<BookingListItemDto> _allBookings = Array.Empty<BookingListItemDto>();

    /// <summary>
    /// Opretter viewmodellen og sætter standardperioden til de næste 14 dage.
    /// </summary>
    /// <param name="bookings">Kører use cases på <see cref="IBookingService"/>.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public BookingListViewModel(IUseCaseRunner<IBookingService> bookings, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;

        PeriodStart = clock.Today;
        PeriodEnd = clock.Today.AddDays(DefaultPeriodLengthInDays);
    }

    /// <summary>Bookingerne der vises lige nu, efter statusfilteret.</summary>
    public IReadOnlyList<BookingListItemDto> Bookings { get; private set; } =
        Array.Empty<BookingListItemDto>();

    /// <summary>Fejl der skal vises til brugeren, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Sand mens et kald er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand når skærmen har hentet data mindst én gang.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Sand når listen er hentet uden fejl, men er tom.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Bookings.Count == 0;

    /// <summary>Sand når perioden har bookinger, men statusfilteret skjuler dem alle.</summary>
    public bool IsHiddenByStatusFilter => IsEmpty && _allBookings.Count > 0;

    /// <summary>Periodens første dato, inklusiv.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Periodens sidste dato, eksklusiv.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Fritekst på gæstenavn eller værelsesnummer (BR-23, BR-25).</summary>
    public string? SearchText { get; set; }

    /// <summary>Feltet der sorteres på (BR-32).</summary>
    public BookingSortField SortBy { get; private set; } = BookingSortField.StartDate;

    /// <summary>Sorteringsretningen.</summary>
    public SortDirection Direction { get; private set; } = SortDirection.Ascending;

    /// <summary>Om annullerede bookinger skal hentes med (BR-13's soft-delete).</summary>
    public bool IncludeCancelled { get; set; }

    /// <summary>Vis kun denne status. <c>null</c> viser alle hentede bookinger.</summary>
    public BookingStatus? StatusFilter { get; set; }

    /// <summary>Bookingen der venter på at brugeren bekræfter en annullering (BR-04).</summary>
    public BookingListItemDto? BookingPendingCancellation { get; private set; }

    /// <summary>
    /// Henter den første liste. Kaldes fra <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        SearchAsync(cancellationToken);

    /// <summary>
    /// Henter bookingerne der matcher den nuværende forespørgsel.
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

            // En række der er hentet igen er ikke den samme instans som den brugeren
            // klikkede på. Bekræftelsen ville pege på en forældet visning.
            BookingPendingCancellation = null;
        }
    }

    /// <summary>
    /// Rydder søgefelt, statusfilter og annullerede, og henter listen igen.
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
    /// Sætter statusfilteret og opdaterer den viste liste uden at hente igen.
    /// </summary>
    /// <param name="status">Statussen der skal vises, eller <c>null</c> for alle.</param>
    /// <remarks>
    /// Filtrerer i det allerede hentede resultat, fordi periodefilteret — det der
    /// bestemmer hvor meget der hentes — er uændret. Et servicekald ville give samme
    /// rækker igen.
    /// </remarks>
    public void FilterByStatus(BookingStatus? status)
    {
        StatusFilter = status;

        ApplyStatusFilter();
    }

    /// <summary>
    /// Sorterer på et felt. Klik på det felt der allerede sorteres på vender retningen.
    /// </summary>
    /// <param name="field">Feltet der skal sorteres på.</param>
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
    /// Fortæller om en kolonne er den der sorteres på lige nu.
    /// </summary>
    /// <param name="field">Kolonnens felt.</param>
    /// <returns>Sand hvis listen er sorteret på feltet.</returns>
    public bool IsSortedBy(BookingSortField field) => SortBy == field;

    /// <summary>
    /// Giver værdien til <c>aria-sort</c> på en kolonneoverskrift.
    /// </summary>
    /// <param name="field">Kolonnens felt.</param>
    /// <returns><c>"ascending"</c>, <c>"descending"</c> eller <c>"none"</c>.</returns>
    public string AriaSortFor(BookingSortField field)
    {
        if (SortBy != field)
        {
            return "none";
        }

        return Direction == SortDirection.Ascending ? "ascending" : "descending";
    }

    /// <summary>
    /// Beder om bekræftelse før en annullering (BR-04).
    /// </summary>
    /// <param name="booking">Bookingen brugeren vil annullere.</param>
    public void RequestCancel(BookingListItemDto booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        BookingPendingCancellation = booking;
    }

    /// <summary>
    /// Fortryder en annullering der endnu ikke er bekræftet.
    /// </summary>
    public void AbortCancel() => BookingPendingCancellation = null;

    /// <summary>
    /// Gennemfører den annullering brugeren har bekræftet (BR-04, BR-11).
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
    /// Udfører én af bookingens statusovergange og henter listen igen.
    /// </summary>
    /// <param name="action">Overgangen der skal udføres.</param>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Knappen vises kun når <c>Actions</c> siger den er lovlig, men servicen kontrollerer
    /// alligevel: mellem render og klik kan en anden bruger have flyttet bookingen videre.
    /// Sker det, kommer fejlkoden retur og bliver til en neutral dansk besked.
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
                .RunAsync((service, token) => Invoke(service, action, bookingId, token), cancellationToken)
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

        // Hentes igen, fordi overgangen kan flytte bookingen ud af filteret — fx en
        // annullering når "vis annullerede" er slået fra.
        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lægger statusfilteret ned over de hentede rækker.
    /// </summary>
    private void ApplyStatusFilter() =>
        Bookings = StatusFilter is null
            ? _allBookings
            : _allBookings.Where(booking => booking.Status == StatusFilter.Value).ToArray();

    private static Task<Result<BookingDetailsDto>> Invoke(
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