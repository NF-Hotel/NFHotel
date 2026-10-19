using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// Presentation logic for creating and changing a room.
/// </summary>
/// <remarks>
/// Without this form rooms can only be created with SQL: <c>CreateAsync</c> and
/// <c>UpdateAsync</c> had no entry in the UI.
/// <para>
/// <b>The form has no status field.</b> <c>UpdateRoomRequest</c> carries only
/// room number, floor, size and capacity — operating status and cleaning state have
/// their own use cases (A-08) and their own buttons on the room overview. A status field here
/// would be another way to the same transitions, bypassing the state machine.
/// </para>
/// <para>
/// B-10: the class validates nothing. The length of the room number, the floor, the size and
/// the capacity are decided by the domain through the service (BR-97 to BR-100), and a
/// room number that already exists is rejected by the service (BR-N-01).
/// </para>
/// </remarks>
public sealed class RoomFormViewModel
{
    private const int DefaultFloor = 1;
    private const int DefaultCapacity = 1;

    private readonly IUseCaseRunner<IRoomService> _rooms;

    /// <summary>
    /// Creates the view model.
    /// </summary>
    /// <param name="rooms">Runs use cases on <see cref="IRoomService"/>.</param>
    public RoomFormViewModel(IUseCaseRunner<IRoomService> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms = rooms;
    }

    /// <summary>True when the form is open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The id of the room being changed, or <c>null</c> when a new one is created.</summary>
    public int? EditingRoomId { get; private set; }

    /// <summary>Formularens overskrift.</summary>
    public string Title => EditingRoomId is null ? "New room" : $"Edit room {RoomNumber}";

    /// <summary>Form field: room number (BR-97, BR-N-01).</summary>
    public string RoomNumber { get; set; } = string.Empty;

    /// <summary>Form field: floor (BR-98).</summary>
    public int Floor { get; set; } = DefaultFloor;

    /// <summary>Form field: room size (BR-99).</summary>
    public RoomSize Size { get; set; } = RoomSize.Single;

    /// <summary>Form field: capacity (BR-100).</summary>
    public int Capacity { get; set; } = DefaultCapacity;

    /// <summary>All room sizes, for the dropdown of the form.</summary>
    public IReadOnlyList<RoomSize> Sizes { get; } = Enum.GetValues<RoomSize>();

    /// <summary>Errors from the form, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Receipt after a successful save.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True if the latest save succeeded — used to fetch the list again.</summary>
    public bool SavedSuccessfully { get; private set; }

    /// <summary>True when the save button may be active.</summary>
    /// <remarks>
    /// Only busyness blocks the button. The field validation belongs in the domain, and its answer is shown
    /// as error texts after the attempt — so the screen can never disagree with the rules
    /// (BR-97 to BR-100).
    /// </remarks>
    public bool CanSave => !IsBusy;

    /// <summary>
    /// Opens an empty form for a new room.
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
    /// Opens the form with the fields of an existing room.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Fetches the room again rather than using the row of the list, so the form works on the
    /// latest version and not on a view that may be minutes old.
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
    /// Closes the form without saving.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        EditingRoomId = null;
        Errors = Array.Empty<string>();
        SavedSuccessfully = false;
    }

    /// <summary>
    /// Saves the form — creates a new room or changes the one being edited.
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
    /// Clears the receipt from the last save.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;
}
