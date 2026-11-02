using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// Presentation logic for the service notes under each room: the current notes, the drafts, the done marks and the
/// history of one room at a time.
/// </summary>
/// <remarks>
/// The class holds no rule about the five minutes. Which notes are current comes from the service, which also refuses an
/// undo that came too late; that answer becomes a message here, and the list is fetched again.
/// </remarks>
public sealed class RoomServiceNotesViewModel
{
    private readonly IUseCaseRunner<IRoomServiceNoteService> _notes;
    private readonly Dictionary<int, string> _drafts = [];
    private ILookup<int, RoomServiceNoteDto> _current = Array.Empty<RoomServiceNoteDto>().ToLookup(note => note.RoomId);

    /// <summary>Creates the view model.</summary>
    /// <param name="notes">Runs use cases on <see cref="IRoomServiceNoteService"/>.</param>
    public RoomServiceNotesViewModel(IUseCaseRunner<IRoomServiceNoteService> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        _notes = notes;
    }

    /// <summary>Errors to show to the user.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>True while a call is in progress, so the fields can be disabled.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>The room whose history is open, or <c>null</c>.</summary>
    public int? HistoryRoomId { get; private set; }

    /// <summary>The finished service of the room in <see cref="HistoryRoomId"/>.</summary>
    public IReadOnlyList<RoomServiceNoteDto> History { get; private set; } = Array.Empty<RoomServiceNoteDto>();

    /// <summary>
    /// Goes up each time a mark was refused. The page keys its checkboxes on it: the browser has already moved the box the
    /// user clicked, and only a new element puts it back when the state of the note did not change.
    /// </summary>
    public int RefusedMarks { get; private set; }

    /// <summary>Gives the notes of a room that are not in the history.</summary>
    /// <param name="roomId">The room.</param>
    /// <returns>The notes, oldest first.</returns>
    public IEnumerable<RoomServiceNoteDto> NotesOf(int roomId) => _current[roomId];

    /// <summary>Gives what the user has written so far for a room.</summary>
    /// <param name="roomId">The room.</param>
    /// <returns>The draft, or an empty text.</returns>
    public string DraftOf(int roomId) => _drafts.GetValueOrDefault(roomId, string.Empty);

    /// <summary>Keeps what the user has written for a room.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="text">The text.</param>
    public void SetDraft(int roomId, string? text) => _drafts[roomId] = text ?? string.Empty;

    /// <summary>Tells whether a room has a draft that can be sent.</summary>
    /// <param name="roomId">The room.</param>
    /// <returns>True if the draft holds text.</returns>
    public bool HasDraft(int roomId) => !string.IsNullOrWhiteSpace(DraftOf(roomId));

    /// <summary>Fetches the current notes and shows a failure. Called when the page opens.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var result = await FetchCurrentAsync(cancellationToken).ConfigureAwait(false);

        Errors = result.IsFailure ? ErrorMessages.Describe(result) : Array.Empty<string>();
    }

    /// <summary>
    /// Fetches the notes again while the page stays open, so a mark moves to the history on time and the notes of the
    /// others show up. It leaves the messages alone: a failed round tries again by itself, and the answer to a click must
    /// stay on the screen. It stands aside while another call is in progress.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!IsBusy)
        {
            await FetchCurrentAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Sends the draft of a room as a note, which also reports that the room needs service.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the note was written, so the state of the room has to be fetched again.</returns>
    public async Task<bool> ReportAsync(int roomId, CancellationToken cancellationToken = default)
    {
        if (IsBusy || !HasDraft(roomId))
        {
            return false;
        }

        var request = new CreateRoomServiceNoteRequest(roomId, DraftOf(roomId));
        var result = await RunAsync((service, token) => service.CreateAsync(request, token), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            _drafts.Remove(roomId);
        }

        return result.IsSuccess;
    }

    /// <summary>Marks a current note as done, or undoes the mark.</summary>
    /// <param name="note">The note whose box was clicked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ToggleDoneAsync(RoomServiceNoteDto note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (IsBusy)
        {
            return;
        }

        await RunAsync(
            (service, token) => note.IsDone ? service.UndoDoneAsync(note.Id, token) : service.MarkDoneAsync(note.Id, token),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Takes a note back from the history.</summary>
    /// <param name="note">The note whose box was clicked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the note is open again, so the state of the room has to be fetched again.</returns>
    public async Task<bool> ReopenAsync(RoomServiceNoteDto note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (IsBusy)
        {
            return false;
        }

        var result = await RunAsync((service, token) => service.ReopenAsync(note.Id, token), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess;
    }

    /// <summary>Opens the history of a room, or closes it when it is the one already open.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ToggleHistoryAsync(int roomId, CancellationToken cancellationToken = default)
    {
        if (HistoryRoomId == roomId)
        {
            HistoryRoomId = null;
            History = Array.Empty<RoomServiceNoteDto>();

            return;
        }

        HistoryRoomId = roomId;
        History = Array.Empty<RoomServiceNoteDto>();

        var result = await FetchHistoryAsync(roomId, cancellationToken).ConfigureAwait(false);

        Errors = result.IsFailure ? ErrorMessages.Describe(result) : Array.Empty<string>();
    }

    /// <summary>
    /// Runs one change, shows its answer, and fetches the notes and an open history again, because a refused mark means
    /// that the screen is out of date.
    /// </summary>
    private async Task<Result<RoomServiceNoteDto>> RunAsync(
        Func<IRoomServiceNoteService, CancellationToken, Task<Result<RoomServiceNoteDto>>> change,
        CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            var result = await _notes.RunAsync(change, cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                RefusedMarks++;
            }
            else
            {
                Errors = Array.Empty<string>();
            }

            await FetchCurrentAsync(cancellationToken).ConfigureAwait(false);

            if (HistoryRoomId is { } roomId)
            {
                await FetchHistoryAsync(roomId, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<Result<IReadOnlyList<RoomServiceNoteDto>>> FetchCurrentAsync(CancellationToken cancellationToken)
    {
        var result = await _notes
            .RunAsync((service, token) => service.GetCurrentAsync(token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            _current = result.Value.ToLookup(note => note.RoomId);
        }

        return result;
    }

    private async Task<Result<IReadOnlyList<RoomServiceNoteDto>>> FetchHistoryAsync(int roomId, CancellationToken cancellationToken)
    {
        var result = await _notes
            .RunAsync((service, token) => service.GetHistoryAsync(roomId, token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            History = result.Value;
        }

        return result;
    }
}
