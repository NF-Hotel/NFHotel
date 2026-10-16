using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// Præsentationslogik for rumoversigten: filtrering, rengørings- og driftsovergange samt
/// sletning.
/// </summary>
/// <remarks>
/// B-10: klassen indeholder <b>ingen</b> forretningsregler. Hvilke overgange der er lovlige
/// på et rum, kommer fra <c>RoomListItemDto.Actions</c>, som Application beregner ud fra
/// domænets tilstandsmaskine — nøjagtig som <c>BookingActionsDto</c> gør for bookinger
/// (BR-22). <see cref="AvailableActions"/> oversætter kun de sande felter til knaptekster.
/// <para>
/// Servicen kontrollerer alligevel ved klik: mellem render og klik kan en anden bruger have
/// flyttet rummet videre. Sker det, kommer fejlkoden retur og bliver til en dansk besked.
/// </para>
/// <para>
/// Hvert kald går gennem <see cref="IUseCaseRunner{TService}"/> og får dermed sin egen
/// DI-scope og sin egen <c>DbContext</c> (Architecture.md afsnit 8).
/// </para>
/// </remarks>
public sealed class RoomListViewModel
{
    /// <summary>Antal mulige knapper — kun til listens startkapacitet.</summary>
    private const int RoomActionCount = 11;

    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="rooms">Kører use cases på <see cref="IRoomService"/>.</param>
    public RoomListViewModel(IUseCaseRunner<IRoomService> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms = rooms;
    }

    /// <summary>Rummene der vises lige nu.</summary>
    public IReadOnlyList<RoomListItemDto> Rooms { get; private set; } = Array.Empty<RoomListItemDto>();

    /// <summary>Valgmulighederne i filterets dropdowns, hentet fra servicen.</summary>
    public RoomFilterOptionsDto? FilterOptions { get; private set; }

    /// <summary>Fejl der skal vises til brugeren, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Kvittering efter en vellykket sletning.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>Sand mens et kald er i gang, så knapper kan deaktiveres.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand når skærmen har hentet data mindst én gang.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Sand når listen er hentet uden fejl, men er tom.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Rooms.Count == 0;

    /// <summary>Sand når mindst ét filterfelt er sat.</summary>
    public bool HasFilter =>
        FilterFloor is not null ||
        FilterSize is not null ||
        FilterStatus is not null ||
        FilterHousekeepingStatus is not null;

    /// <summary>Det rum handlingspanelet arbejder på, eller <c>null</c>.</summary>
    public RoomListItemDto? SelectedRoom { get; private set; }

    /// <summary>Rummet der venter på at brugeren bekræfter en sletning.</summary>
    public RoomListItemDto? RoomPendingDeletion { get; private set; }

    /// <summary>Filter: etage. <c>null</c> betyder alle.</summary>
    public int? FilterFloor { get; set; }

    /// <summary>Filter: rumstørrelse. <c>null</c> betyder alle.</summary>
    public RoomSize? FilterSize { get; set; }

    /// <summary>Filter: rumstatus. <c>null</c> betyder alle.</summary>
    public RoomStatus? FilterStatus { get; set; }

    /// <summary>Filter: rengøringsstatus. <c>null</c> betyder alle.</summary>
    public HousekeepingStatus? FilterHousekeepingStatus { get; set; }

    /// <summary>
    /// De overgange der er lovlige på det valgte rum lige nu, med dansk knaptekst.
    /// </summary>
    /// <remarks>
    /// Listen er tom når intet rum er valgt. Rækkefølgen følger arbejdsgangen: drift først,
    /// derefter rengøringsforløbet, derefter service.
    /// </remarks>
    public IReadOnlyList<RoomActionOption> AvailableActions => BuildActions(SelectedRoom?.Actions);

    /// <summary>
    /// Henter filtermuligheder og den første liste. Kaldes fra <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var optionsLoaded = await LoadFilterOptionsAsync(cancellationToken).ConfigureAwait(false);

        if (!optionsLoaded)
        {
            // Kan filtermulighederne ikke hentes, er datakilden nede. At forsøge listen
            // bagefter ville blot lade brugeren vente på den samme fejl to gange.
            IsLoaded = true;

            return;
        }

        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Henter rummene der matcher det nuværende filter.
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
    /// Nulstiller alle filterfelter og henter listen igen.
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
    /// Vælger eller fravælger rummet handlingspanelet arbejder på.
    /// </summary>
    /// <param name="room">Rummet der blev klikket på.</param>
    public void ToggleSelection(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        SelectedRoom = SelectedRoom?.RoomId == room.RoomId ? null : room;
        RoomPendingDeletion = null;
    }

    /// <summary>
    /// Fortæller om et rum er det valgte.
    /// </summary>
    /// <param name="room">Rummet i rækken.</param>
    /// <returns>Sand hvis rummet er valgt.</returns>
    public bool IsSelected(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return SelectedRoom?.RoomId == room.RoomId;
    }

    /// <summary>
    /// Udfører én af rummets overgange på det valgte rum og henter listen igen.
    /// </summary>
    /// <param name="action">Overgangen der skal udføres.</param>
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
                .RunAsync((service, token) => Invoke(service, action, room.RoomId, token), cancellationToken)
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

        // Listen hentes igen, fordi en overgang kan flytte rummet ud af det aktive filter.
        await SearchAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Beder om bekræftelse før et rum slettes.
    /// </summary>
    /// <param name="room">Rummet brugeren vil slette.</param>
    public void RequestDelete(RoomListItemDto room)
    {
        ArgumentNullException.ThrowIfNull(room);

        RoomPendingDeletion = room;
    }

    /// <summary>
    /// Fortryder en sletning der endnu ikke er bekræftet.
    /// </summary>
    public void AbortDelete() => RoomPendingDeletion = null;

    /// <summary>
    /// Sletter det rum brugeren har bekræftet (BR-78, BR-112).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Servicen afviser rum med bookinger; skærmen forsøger og viser svaret.
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
    /// Rydder kvitteringen fra sidste handling.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;

    /// <summary>
    /// Henter filterets valgmuligheder.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Sand hvis mulighederne blev hentet.</returns>
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

        // Rummet skal genfindes i den nye liste, ellers viser handlingspanelet en forældet status.
        return rooms.FirstOrDefault(room => room.RoomId == selectedId.Value);
    }

    /// <summary>
    /// Oversætter de sande felter på <paramref name="actions"/> til knapper.
    /// </summary>
    /// <param name="actions">Rummets lovlige overgange, eller <c>null</c> hvis intet rum er valgt.</param>
    /// <returns>Knapperne i den rækkefølge de skal stå i panelet.</returns>
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

    private static Task<Result<RoomDetailsDto>> Invoke(
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