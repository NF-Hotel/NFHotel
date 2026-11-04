using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Assigns rooms to housekeepers and carries out the trades of rooms between them (MIL-060). Who holds which room is
/// checked when a trade is offered and again when it is carried out; a trade whose rooms changed hands in another way
/// meanwhile is called off.
/// </summary>
public sealed class RoomAssignmentService : IRoomAssignmentService
{
    private readonly IRoomAssignmentRepository _assignments;
    private readonly IRoomRepository _rooms;
    private readonly IRoomQueries _roomQueries;
    private readonly IStaffRepository _staff;
    private readonly IStaffAuthorizationService _authorization;
    private readonly IStaffCallContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="assignments">The assignments and the trades.</param>
    /// <param name="rooms">The rooms, to find one.</param>
    /// <param name="roomQueries">The rooms, to list them with their numbers.</param>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="authorization">Tells whether the caller is a Manager.</param>
    /// <param name="context">Who is calling.</param>
    /// <param name="unitOfWork">Saves a trade and the rooms it moves in one transaction.</param>
    /// <param name="clock">The source of the time.</param>
    public RoomAssignmentService(
        IRoomAssignmentRepository assignments,
        IRoomRepository rooms,
        IRoomQueries roomQueries,
        IStaffRepository staff,
        IStaffAuthorizationService authorization,
        IStaffCallContext context,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(roomQueries);
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _assignments = assignments;
        _rooms = rooms;
        _roomQueries = roomQueries;
        _staff = staff;
        _authorization = authorization;
        _context = context;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<RoomAssignmentOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _roomQueries.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var assignments = await _assignments.ListAsync(cancellationToken).ConfigureAwait(false);
        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);
        var holders = assignments.ToDictionary(assignment => assignment.RoomId, assignment => assignment.StaffId);

        IReadOnlyList<RoomAssignmentDto> roomRows =
        [
            .. rooms
                .OrderBy(room => room.Floor)
                .ThenBy(room => room.RoomNumber, StringComparer.Ordinal)
                .Select(room =>
                {
                    int? holder = holders.TryGetValue(room.RoomId, out var staffId) ? staffId : null;
                    return new RoomAssignmentDto(room.RoomId, room.RoomNumber, room.Floor, holder, StaffNames.Find(members, holder));
                }),
        ];
        IReadOnlyList<HousekeeperDto> housekeepers =
        [
            .. members.Values
                .Where(IsActiveHousekeeper)
                .Select(member => new HousekeeperDto(member.Id, StaffNames.Of(member), holders.Values.Count(id => id == member.Id)))
                .OrderBy(housekeeper => housekeeper.Name, StringComparer.CurrentCulture),
        ];

        return Result<RoomAssignmentOverviewDto>.Success(new RoomAssignmentOverviewDto(roomRows, housekeepers, RoomAssignment.MinimumRooms));
    }

    /// <inheritdoc />
    public async Task<Result> AssignAsync(int roomId, AssignRoomRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _rooms.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Failure(ErrorCodes.Room.NotFound);
        }

        if (request.StaffId is { } staffId
            && (await _staff.GetByIdAsync(staffId, cancellationToken).ConfigureAwait(false) is not { } member || !IsActiveHousekeeper(member)))
        {
            return Result.Failure(RoomTradeErrors.AssigneeInvalid);
        }

        var existing = await _assignments.FindByRoomAsync(roomId, cancellationToken).ConfigureAwait(false);
        switch (existing, request.StaffId)
        {
            case (null, null):
                return Result.Success();
            case (not null, null):
                _assignments.Remove(existing);
                break;
            case (null, { } newHolder):
                _assignments.Add(RoomAssignment.Create(roomId, newHolder));
                break;
            case (not null, { } newHolder):
                existing.MoveTo(newHolder);
                _assignments.Update(existing);
                break;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ConcurrencyConflictException or UniqueConstraintViolationException)
        {
            // Another Manager or a trade moved the room at the same moment.
            return Result.Failure(ErrorCodes.Room.ConcurrencyConflict);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default)
    {
        if (_context.StaffId is not { } caller)
        {
            return Result<IReadOnlyList<RoomTradeDto>>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var isManager = await _authorization.HasPermissionAsync(Permissions.RoomAssignmentsManage, cancellationToken).ConfigureAwait(false);
        var trades = await _assignments.ListTradesAsync(caller, isManager, cancellationToken).ConfigureAwait(false);
        var names = await NamesAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomTradeDto>>.Success([.. trades.Select(trade => names.ToDto(trade))]);
    }

    /// <inheritdoc />
    public async Task<Result<RoomTradeDto>> OfferTradeAsync(OfferRoomTradeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_context.StaffId is not { } caller)
        {
            return Result<RoomTradeDto>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        if (request.ToStaffId != caller
            && (await _staff.GetByIdAsync(request.ToStaffId, cancellationToken).ConfigureAwait(false) is not { } colleague || !IsActiveHousekeeper(colleague)))
        {
            return Result<RoomTradeDto>.Failure(RoomTradeErrors.NotHousekeeper);
        }

        RoomTrade trade;
        try
        {
            trade = RoomTrade.Offer(caller, request.ToStaffId, request.GiveRoomIds ?? [], request.TakeRoomIds ?? [], _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result<RoomTradeDto>.FromCodes(exception.Codes);
        }

        var holders = await HoldersAsync(cancellationToken).ConfigureAwait(false);
        if (!Holds(holders, trade.GiveRoomIds, caller))
        {
            return Result<RoomTradeDto>.Failure(RoomTradeErrors.RoomNotYours);
        }

        if (!Holds(holders, trade.TakeRoomIds, request.ToStaffId))
        {
            return Result<RoomTradeDto>.Failure(RoomTradeErrors.RoomNotTheirs);
        }

        _assignments.AddTrade(trade);
        return await SaveAsync(trade, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(
            id,
            (trade, caller, assignments) =>
            {
                if (caller != trade.ToStaffId)
                {
                    throw new DomainException(RoomTradeErrors.NotYours);
                }

                if (trade.State != TradeState.Open)
                {
                    throw new DomainException(RoomTradeErrors.NotOpen);
                }

                if (!StillHeld(trade, assignments))
                {
                    return false;
                }

                var needsApproval = trade.NeedsApproval(
                    assignments.Count(assignment => assignment.StaffId == trade.FromStaffId),
                    assignments.Count(assignment => assignment.StaffId == trade.ToStaffId));
                trade.Accept(caller, needsApproval, _clock.UtcNow);
                if (!needsApproval)
                {
                    HandOver(trade, assignments);
                }

                return true;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, caller, _) => Done(() => trade.Decline(caller, _clock.UtcNow)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, caller, _) => Done(() => trade.Withdraw(caller, _clock.UtcNow)), cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(
            id,
            (trade, caller, assignments) =>
            {
                if (trade.State != TradeState.AwaitingApproval)
                {
                    throw new DomainException(RoomTradeErrors.NotOpen);
                }

                if (!StillHeld(trade, assignments))
                {
                    return false;
                }

                trade.Approve(caller, _clock.UtcNow);
                HandOver(trade, assignments);
                return true;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, caller, _) => Done(() => trade.Reject(caller, _clock.UtcNow)), cancellationToken);

    private static bool IsActiveHousekeeper(StaffMember member) => member.IsActive && member.Role == StaffRole.Housekeeping;

    private static bool Holds(IReadOnlyDictionary<int, int> holders, IEnumerable<int> roomIds, int staffId) =>
        roomIds.All(roomId => holders.TryGetValue(roomId, out var holder) && holder == staffId);

    private static bool StillHeld(RoomTrade trade, IReadOnlyList<RoomAssignment> assignments)
    {
        var holders = assignments.ToDictionary(assignment => assignment.RoomId, assignment => assignment.StaffId);
        return Holds(holders, trade.GiveRoomIds, trade.FromStaffId) && Holds(holders, trade.TakeRoomIds, trade.ToStaffId);
    }

    private static bool Done(Action step)
    {
        step();
        return true;
    }

    private void HandOver(RoomTrade trade, IReadOnlyList<RoomAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (trade.GiveRoomIds.Contains(assignment.RoomId))
            {
                assignment.MoveTo(trade.ToStaffId);
                _assignments.Update(assignment);
            }
            else if (trade.TakeRoomIds.Contains(assignment.RoomId))
            {
                assignment.MoveTo(trade.FromStaffId);
                _assignments.Update(assignment);
            }
        }
    }

    /// <summary>
    /// Runs one step of a trade. The step returns false when the rooms changed hands meanwhile; the trade is then called
    /// off and saved, and the caller gets <see cref="RoomTradeErrors.RoomsChanged"/>.
    /// </summary>
    private async Task<Result<RoomTradeDto>> StepAsync(
        int id,
        Func<RoomTrade, int, IReadOnlyList<RoomAssignment>, bool> step,
        CancellationToken cancellationToken)
    {
        if (_context.StaffId is not { } caller)
        {
            return Result<RoomTradeDto>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var trade = await _assignments.GetTradeAsync(id, cancellationToken).ConfigureAwait(false);
        if (trade is null)
        {
            return Result<RoomTradeDto>.Failure(RoomTradeErrors.NotFound);
        }

        var assignments = await _assignments.ListAsync(cancellationToken).ConfigureAwait(false);
        bool carriedOut;
        try
        {
            carriedOut = step(trade, caller, assignments);
        }
        catch (DomainException exception)
        {
            return Result<RoomTradeDto>.FromCodes(exception.Codes);
        }

        if (!carriedOut)
        {
            trade.Cancel(_clock.UtcNow);
        }

        _assignments.UpdateTrade(trade);
        var saved = await SaveAsync(trade, cancellationToken).ConfigureAwait(false);
        return carriedOut || saved.IsFailure ? saved : Result<RoomTradeDto>.Failure(RoomTradeErrors.RoomsChanged);
    }

    private async Task<IReadOnlyDictionary<int, int>> HoldersAsync(CancellationToken cancellationToken) =>
        (await _assignments.ListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(assignment => assignment.RoomId, assignment => assignment.StaffId);

    private async Task<Result<RoomTradeDto>> SaveAsync(RoomTrade trade, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // Another trade or a Manager moved one of the rooms at the same moment.
            return Result<RoomTradeDto>.Failure(RoomTradeErrors.RoomsChanged);
        }

        var names = await NamesAsync(cancellationToken).ConfigureAwait(false);
        return Result<RoomTradeDto>.Success(names.ToDto(trade));
    }

    private async Task<TradeNames> NamesAsync(CancellationToken cancellationToken)
    {
        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);
        var rooms = (await _roomQueries.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(room => room.RoomId, room => room.RoomNumber);
        return new TradeNames(members, rooms);
    }

    /// <summary>The names of the staff and the numbers of the rooms that a trade shows.</summary>
    private sealed record TradeNames(IReadOnlyDictionary<int, StaffMember> Members, IReadOnlyDictionary<int, string> Rooms)
    {
        public RoomTradeDto ToDto(RoomTrade trade) =>
            new(
                trade.Id,
                trade.FromStaffId,
                StaffNames.Find(Members, trade.FromStaffId) ?? string.Empty,
                trade.ToStaffId,
                StaffNames.Find(Members, trade.ToStaffId) ?? string.Empty,
                [.. trade.GiveRoomIds.Select(Room)],
                [.. trade.TakeRoomIds.Select(Room)],
                trade.State,
                trade.OfferedAt);

        private TradedRoomDto Room(int roomId) => new(roomId, Rooms.TryGetValue(roomId, out var number) ? number : string.Empty);
    }
}
