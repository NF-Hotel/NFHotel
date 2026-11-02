using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;

namespace NFHotel.ApiClient;

/// <summary>
/// Writes, marks and lists the service notes of the rooms through the API for the web application. The API takes the
/// names of the staff members from the staff token.
/// </summary>
public sealed class ApiRoomServiceNoteService : IRoomServiceNoteService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiRoomServiceNoteService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<RoomServiceNoteDto>>(ApiRoutes.RoomServiceNotes, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetHistoryAsync(int roomId, CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<RoomServiceNoteDto>>($"{ApiRoutes.RoomServiceNotes}/history/{roomId}", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> CreateAsync(CreateRoomServiceNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PostAsync<RoomServiceNoteDto>(ApiRoutes.RoomServiceNotes, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> MarkDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
        ActionAsync(noteId, "done", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> UndoDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
        ActionAsync(noteId, "undo-done", cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomServiceNoteDto>> ReopenAsync(int noteId, CancellationToken cancellationToken = default) =>
        ActionAsync(noteId, "reopen", cancellationToken);

    private Task<Result<RoomServiceNoteDto>> ActionAsync(int noteId, string action, CancellationToken cancellationToken) =>
        _api.PostAsync<RoomServiceNoteDto>($"{ApiRoutes.RoomServiceNotes}/{noteId}/{action}", null, cancellationToken);
}
