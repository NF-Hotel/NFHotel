using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// Præsentationslogik for at oprette og rette et rum.
/// </summary>
/// <remarks>
/// Uden denne formular kan rum kun oprettes med SQL: <c>CreateAsync</c> og
/// <c>UpdateAsync</c> havde ingen indgang i UI'et.
/// <para>
/// <b>Formularen har intet statusfelt.</b> <c>UpdateRoomRequest</c> bærer kun
/// værelsesnummer, etage, størrelse og kapacitet — driftsstatus og rengøringsstand har
/// deres egne use cases (A-08) og deres egne knapper på rumoversigten. Et statusfelt her
/// ville være en anden vej til de samme overgange, uden om tilstandsmaskinen.
/// </para>
/// <para>
/// B-10: klassen validerer ingenting. Værelsesnummerets længde, etagen, størrelsen og
/// kapaciteten afgøres af domænet gennem servicen (BR-97 til BR-100), og et
/// værelsesnummer der allerede findes, afvises af servicen (BR-N-01).
/// </para>
/// </remarks>
public sealed class RoomFormViewModel
{
    private const int DefaultFloor = 1;
    private const int DefaultCapacity = 1;

    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="rooms">Kører use cases på <see cref="IRoomService"/>.</param>
    public RoomFormViewModel(IUseCaseRunner<IRoomService> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms = rooms;
    }

    /// <summary>Sand når formularen er åben.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Id'et på rummet der rettes, eller <c>null</c> når et nyt oprettes.</summary>
    public int? EditingRoomId { get; private set; }

    /// <summary>Formularens overskrift.</summary>
    public string Title => EditingRoomId is null ? "New room" : $"Edit room {RoomNumber}";

    /// <summary>Formularfelt: værelsesnummer (BR-97, BR-N-01).</summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>Formularfelt: etage (BR-98).</summary>
    public int Floor { get; set; } = DefaultFloor;

    /// <summary>Formularfelt: rumstørrelse (BR-99).</summary>
    public RoomSize Size { get; set; } = RoomSize.Single;

    /// <summary>Formularfelt: kapacitet (BR-100).</summary>
    public int Capacity { get; set; } = DefaultCapacity;

    /// <summary>Alle rumstørrelser, til formularens dropdown.</summary>
    public IReadOnlyList<RoomSize> Sizes { get; } = Enum.GetValues<RoomSize>();

    /// <summary>Fejl fra formularen, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Kvittering efter et vellykket gem.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>Sand mens et kald er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand hvis det seneste gem lykkedes — bruges til at hente listen igen.</summary>
    public bool SavedSuccessfully { get; private set; }

    /// <summary>Sand når gemknappen må være aktiv.</summary>
    /// <remarks>
    /// Kun travlhed spærrer knappen. Feltvalideringen hører i domænet, og dens svar vises
    /// som fejltekster efter forsøget — så skærmen aldrig kan blive uenig med reglerne
    /// (BR-97 til BR-100).
    /// </remarks>
    public bool CanSave => !IsBusy;

    /// <summary>
    /// Åbner en tom formular til et nyt rum.
    /// </summary>
    public void BeginCreate()
    {
        EditingRoomId = null;
        RoomNumber = string.Empty;
        Floor = DefaultFloor;
        Size = RoomSize.Single;
        Capacity = DefaultCapacity;

        Errors = Array.Empty<string>();
        SuccessMessage = null;
        SavedSuccessfully = false;
        IsOpen = true;
    }

    /// <summary>
    /// Åbner formularen med et eksisterende rums felter.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Henter rummet igen frem for at bruge listens række, så formularen arbejder på den
    /// nyeste udgave og ikke på en visning der kan være minutter gammel.
    /// </remarks>
    public async Task BeginEditAsync(int roomId, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        SuccessMessage = null;
        SavedSuccessfully = false;

        try
        {
            var result = await _rooms
                .RunAsync((service, token) => service.GetByIdAsync(roomId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);

                return;
            }

            var room = result.Value;

            EditingRoomId = room.RoomId;
            RoomNumber = room.RoomNumber;
            Floor = room.Floor;
            Size = room.Size;
            Capacity = room.Capacity;

            Errors = Array.Empty<string>();
            IsOpen = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Lukker formularen uden at gemme.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        EditingRoomId = null;
        Errors = Array.Empty<string>();
        SavedSuccessfully = false;
    }

    /// <summary>
    /// Gemmer formularen — opretter et nyt rum eller retter det der redigeres.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        SavedSuccessfully = false;

        if (IsBusy)
        {
            return;
        }

        var roomId = EditingRoomId;
        var roomNumber = RoomNumber;
        var floor = Floor;
        var size = Size;
        var capacity = Capacity;

        IsBusy = true;

        try
        {
            var result = roomId is null
                ? await _rooms
                    .RunAsync(
                        (service, token) => service.CreateAsync(
                            new CreateRoomRequest(roomNumber, floor, size, capacity),
                            token),
                        cancellationToken)
                    .ConfigureAwait(false)
                : await _rooms
                    .RunAsync(
                        (service, token) => service.UpdateAsync(
                            new UpdateRoomRequest(roomId.Value, roomNumber, floor, size, capacity),
                            token),
                        cancellationToken)
                    .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);

                return;
            }

            Errors = Array.Empty<string>();
            SavedSuccessfully = true;
            SuccessMessage = roomId is null
                ? $"Room {result.Value.RoomNumber} was created."
                : $"Room {result.Value.RoomNumber} was updated.";

            IsOpen = false;
            EditingRoomId = null;
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
}