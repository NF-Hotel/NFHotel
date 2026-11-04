using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Shifts;

/// <summary>
/// Plans the shifts and carries out their trades (MIL-060). Before a shift changes hands the service checks that the
/// member who offered it still works it, that the colleague has its role, and that nobody ends up with two shifts at
/// the same time; a trade whose shifts changed hands in another way meanwhile is called off.
/// </summary>
public sealed class ShiftService : IShiftService
{
    /// <summary>The longest period that one list of shifts covers.</summary>
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(62);

    /// <summary>How long a closed trade stays in the list.</summary>
    public static readonly TimeSpan ClosedTradesShown = TimeSpan.FromDays(14);

    private readonly IShiftRepository _shifts;
    private readonly IStaffRepository _staff;
    private readonly IStaffAuthorizationService _authorization;
    private readonly IStaffCallContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="shifts">The shifts and their trades.</param>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="authorization">Tells whether the caller is a Manager.</param>
    /// <param name="context">Who is calling.</param>
    /// <param name="unitOfWork">Saves a trade and the shifts it hands over in one transaction.</param>
    /// <param name="clock">The source of the time.</param>
    public ShiftService(
        IShiftRepository shifts,
        IStaffRepository staff,
        IStaffAuthorizationService authorization,
        IStaffCallContext context,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _shifts = shifts;
        _staff = staff;
        _authorization = authorization;
        _context = context;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShiftDto>>> ListAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (to <= from || to - from > MaxPeriod)
        {
            return Result<IReadOnlyList<ShiftDto>>.Failure(ShiftErrors.PeriodInvalid);
        }

        if (await CallerAsync(cancellationToken).ConfigureAwait(false) is not { } caller)
        {
            return Result<IReadOnlyList<ShiftDto>>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var isManager = await IsManagerAsync(cancellationToken).ConfigureAwait(false);
        var shifts = await _shifts.ListAsync(from, to, isManager ? null : caller.Role, cancellationToken).ConfigureAwait(false);
        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<ShiftDto>>.Success([.. shifts.Select(shift => ToDto(shift, members))]);
    }

    /// <inheritdoc />
    public async Task<Result<ShiftDto>> CreateAsync(SaveShiftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Shift shift;
        try
        {
            shift = Shift.Plan(request.StartsAt, request.EndsAt, request.Role, request.AssigneeId);
        }
        catch (DomainException exception)
        {
            return Result<ShiftDto>.FromCodes(exception.Codes);
        }

        if (await AssigneeProblemAsync(shift, cancellationToken).ConfigureAwait(false) is { } problem)
        {
            return Result<ShiftDto>.Failure(problem);
        }

        _shifts.Add(shift);
        return await SaveShiftAsync(shift, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<ShiftDto>> UpdateAsync(int id, SaveShiftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shift = await _shifts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (shift is null)
        {
            return Result<ShiftDto>.Failure(ShiftErrors.NotFound);
        }

        try
        {
            shift.Change(request.StartsAt, request.EndsAt, request.Role, request.AssigneeId);
        }
        catch (DomainException exception)
        {
            return Result<ShiftDto>.FromCodes(exception.Codes);
        }

        if (await AssigneeProblemAsync(shift, cancellationToken).ConfigureAwait(false) is { } problem)
        {
            return Result<ShiftDto>.Failure(problem);
        }

        // A trade of the shift that is still pending is checked again when it is taken, accepted or approved.
        _shifts.Update(shift);
        return await SaveShiftAsync(shift, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var shift = await _shifts.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (shift is null)
        {
            return Result.Failure(ShiftErrors.NotFound);
        }

        _shifts.Remove(shift);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShiftTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default)
    {
        if (await CallerAsync(cancellationToken).ConfigureAwait(false) is not { } caller)
        {
            return Result<IReadOnlyList<ShiftTradeDto>>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var now = _clock.UtcNow;
        var isManager = await IsManagerAsync(cancellationToken).ConfigureAwait(false);
        var trades = await _shifts.ListTradesAsync(now - ClosedTradesShown, cancellationToken).ConfigureAwait(false);
        int[] shiftIds = [.. trades.Select(trade => trade.ShiftId), .. trades.Select(trade => trade.SwapShiftId ?? 0).Where(id => id > 0)];
        var shifts = (await _shifts.ListByIdsAsync([.. shiftIds.Distinct()], cancellationToken).ConfigureAwait(false))
            .ToDictionary(shift => shift.Id);
        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<ShiftTradeDto> visible =
        [
            .. trades
                .Where(trade => shifts.ContainsKey(trade.ShiftId))
                .Where(trade => isManager
                    || trade.OfferedById == caller.Id
                    || trade.TakenById == caller.Id
                    || (trade.State == TradeState.Open && !trade.IsSwap && shifts[trade.ShiftId].Role == caller.Role))
                .Select(trade => ToDto(trade, shifts, members, now)),
        ];
        return Result<IReadOnlyList<ShiftTradeDto>>.Success(visible);
    }

    /// <inheritdoc />
    public async Task<Result<ShiftTradeDto>> OfferTradeAsync(OfferShiftTradeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_context.StaffId is not { } caller)
        {
            return Result<ShiftTradeDto>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var now = _clock.UtcNow;
        var shift = await _shifts.GetByIdAsync(request.ShiftId, cancellationToken).ConfigureAwait(false);
        if (shift is null)
        {
            return Result<ShiftTradeDto>.Failure(ShiftErrors.NotFound);
        }

        if (shift.AssigneeId != caller)
        {
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeNotYours);
        }

        if (ShiftTrade.IsClosed(shift.StartsAt, now))
        {
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeExpired);
        }

        if (await _shifts.HasPendingTradeAsync(shift.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeAlreadyOffered);
        }

        ShiftTrade trade;
        Shift? swap = null;
        if (request.SwapShiftId is { } swapId)
        {
            swap = await _shifts.GetByIdAsync(swapId, cancellationToken).ConfigureAwait(false);
            if (swap is null)
            {
                return Result<ShiftTradeDto>.Failure(ShiftErrors.NotFound);
            }

            if (swap.AssigneeId is not { } colleague || colleague == caller || swap.Role != shift.Role)
            {
                return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeSwapInvalid);
            }

            if (ShiftTrade.IsClosed(swap.StartsAt, now))
            {
                return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeExpired);
            }

            if (await _shifts.HasPendingTradeAsync(swap.Id, cancellationToken).ConfigureAwait(false))
            {
                return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeAlreadyOffered);
            }

            trade = ShiftTrade.Swap(shift.Id, caller, swap.Id, colleague, now);
            if (await OverlapProblemAsync(trade, shift, swap, cancellationToken).ConfigureAwait(false) is { } overlap)
            {
                return Result<ShiftTradeDto>.Failure(overlap);
            }
        }
        else
        {
            trade = ShiftTrade.GiveAway(shift.Id, caller, now);
        }

        _shifts.AddTrade(trade);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ShiftTradeDto>.Success(await ToDtoAsync(trade, shift, swap, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> TakeTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(
            id,
            async (trade, shift, swap, caller, now) =>
            {
                if (trade.IsSwap)
                {
                    return ShiftErrors.TradeNotYours;
                }

                var member = await _staff.GetByIdAsync(caller, cancellationToken).ConfigureAwait(false);
                if (member is null || member.Role != shift.Role)
                {
                    return ShiftErrors.TradeWrongRole;
                }

                if (ShiftTrade.IsClosed(shift.StartsAt, now))
                {
                    return ShiftErrors.TradeExpired;
                }

                if (await OverlapsAsync(caller, shift, null, cancellationToken).ConfigureAwait(false))
                {
                    return ShiftErrors.Overlap;
                }

                trade.Take(caller, now);
                return null;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(
            id,
            async (trade, shift, swap, caller, now) =>
            {
                if (!trade.IsSwap || caller != trade.TakenById)
                {
                    return ShiftErrors.TradeNotYours;
                }

                if (ShiftTrade.IsClosed(shift.StartsAt, now) || (swap is not null && ShiftTrade.IsClosed(swap.StartsAt, now)))
                {
                    return ShiftErrors.TradeExpired;
                }

                if (await OverlapProblemAsync(trade, shift, swap, cancellationToken).ConfigureAwait(false) is { } overlap)
                {
                    return overlap;
                }

                trade.Accept(caller, now);
                return null;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, _, _, caller, now) => RunAsync(() => trade.Decline(caller, now)), cancellationToken, checkHolders: false);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, _, _, caller, now) => RunAsync(() => trade.Withdraw(caller, now)), cancellationToken, checkHolders: false);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(
            id,
            async (trade, shift, swap, caller, now) =>
            {
                if (trade.State != TradeState.AwaitingApproval)
                {
                    return ShiftErrors.TradeNotOpen;
                }

                // A Manager may still approve within the cut-off, but not once a shift has begun.
                if (now >= shift.StartsAt || (swap is not null && now >= swap.StartsAt))
                {
                    return ShiftErrors.TradeExpired;
                }

                if (await OverlapProblemAsync(trade, shift, swap, cancellationToken).ConfigureAwait(false) is { } overlap)
                {
                    return overlap;
                }

                trade.Approve(caller, now);
                shift.HandTo(trade.TakenById!.Value);
                _shifts.Update(shift);
                if (swap is not null)
                {
                    swap.HandTo(trade.OfferedById);
                    _shifts.Update(swap);
                }

                return null;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default) =>
        StepAsync(id, (trade, _, _, caller, now) => RunAsync(() => trade.Reject(caller, now)), cancellationToken, checkHolders: false);

    private static Task<string?> RunAsync(Action step)
    {
        step();
        return Task.FromResult<string?>(null);
    }

    private static ShiftDto ToDto(Shift shift, IReadOnlyDictionary<int, StaffMember> members) =>
        new(shift.Id, shift.StartsAt, shift.EndsAt, shift.Role, shift.AssigneeId, StaffNames.Find(members, shift.AssigneeId));

    private static ShiftTradeDto ToDto(
        ShiftTrade trade,
        IReadOnlyDictionary<int, Shift> shifts,
        IReadOnlyDictionary<int, StaffMember> members,
        DateTimeOffset now)
    {
        var shift = shifts[trade.ShiftId];
        var swap = trade.SwapShiftId is { } swapId && shifts.TryGetValue(swapId, out var found) ? found : null;
        var closed = ShiftTrade.IsClosed(shift.StartsAt, now) || (swap is not null && ShiftTrade.IsClosed(swap.StartsAt, now));
        return new ShiftTradeDto(
            trade.Id,
            ToDto(shift, members),
            swap is null ? null : ToDto(swap, members),
            trade.OfferedById,
            StaffNames.Find(members, trade.OfferedById) ?? string.Empty,
            trade.TakenById,
            StaffNames.Find(members, trade.TakenById),
            trade.State,
            closed,
            trade.OfferedAt);
    }

    private async Task<StaffMember?> CallerAsync(CancellationToken cancellationToken) =>
        _context.StaffId is { } id ? await _staff.GetByIdAsync(id, cancellationToken).ConfigureAwait(false) : null;

    private Task<bool> IsManagerAsync(CancellationToken cancellationToken) =>
        _authorization.HasPermissionAsync(Permissions.ShiftsManage, cancellationToken);

    /// <summary>The member of a planned shift must be active, have its role, and have no other shift at that time.</summary>
    private async Task<string?> AssigneeProblemAsync(Shift shift, CancellationToken cancellationToken)
    {
        if (shift.AssigneeId is not { } assigneeId)
        {
            return null;
        }

        if (await _staff.GetByIdAsync(assigneeId, cancellationToken).ConfigureAwait(false) is not { IsActive: true } member
            || member.Role != shift.Role)
        {
            return ShiftErrors.AssigneeInvalid;
        }

        return await OverlapsAsync(assigneeId, shift, null, cancellationToken).ConfigureAwait(false) ? ShiftErrors.Overlap : null;
    }

    /// <summary>After the trade, the colleague works the shift and, in a swap, the member who offered it the other one.</summary>
    private async Task<string?> OverlapProblemAsync(ShiftTrade trade, Shift shift, Shift? swap, CancellationToken cancellationToken)
    {
        if (trade.TakenById is { } colleague
            && await OverlapsAsync(colleague, shift, swap?.Id, cancellationToken).ConfigureAwait(false))
        {
            return ShiftErrors.Overlap;
        }

        return swap is not null && await OverlapsAsync(trade.OfferedById, swap, shift.Id, cancellationToken).ConfigureAwait(false)
            ? ShiftErrors.Overlap
            : null;
    }

    private async Task<bool> OverlapsAsync(int staffId, Shift target, int? leaving, CancellationToken cancellationToken)
    {
        var shifts = await _shifts.ListForStaffAsync(staffId, target.StartsAt, target.EndsAt, cancellationToken).ConfigureAwait(false);
        return shifts.Any(other => other.Id != target.Id && other.Id != leaving && other.Overlaps(target.StartsAt, target.EndsAt));
    }

    /// <summary>
    /// Runs one step of a trade. When <paramref name="checkHolders"/> is set and a shift of the trade changed hands
    /// meanwhile, the trade is called off and saved, and the caller gets <see cref="ShiftErrors.TradeShiftChanged"/>.
    /// The step returns <c>null</c> on success or the code that refuses it.
    /// </summary>
    private async Task<Result<ShiftTradeDto>> StepAsync(
        int id,
        Func<ShiftTrade, Shift, Shift?, int, DateTimeOffset, Task<string?>> step,
        CancellationToken cancellationToken,
        bool checkHolders = true)
    {
        if (_context.StaffId is not { } caller)
        {
            return Result<ShiftTradeDto>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var trade = await _shifts.GetTradeAsync(id, cancellationToken).ConfigureAwait(false);
        var shift = trade is null ? null : await _shifts.GetByIdAsync(trade.ShiftId, cancellationToken).ConfigureAwait(false);
        if (trade is null || shift is null)
        {
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeNotFound);
        }

        var swap = trade.SwapShiftId is { } swapId ? await _shifts.GetByIdAsync(swapId, cancellationToken).ConfigureAwait(false) : null;
        var now = _clock.UtcNow;
        if (checkHolders && trade.IsPending && !StillHeld(trade, shift, swap))
        {
            trade.Cancel(now);
            _shifts.UpdateTrade(trade);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeShiftChanged);
        }

        string? problem;
        try
        {
            problem = await step(trade, shift, swap, caller, now).ConfigureAwait(false);
        }
        catch (DomainException exception)
        {
            return Result<ShiftTradeDto>.FromCodes(exception.Codes);
        }

        if (problem is not null)
        {
            return Result<ShiftTradeDto>.Failure(problem);
        }

        _shifts.UpdateTrade(trade);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // Another step moved one of the shifts at the same moment.
            return Result<ShiftTradeDto>.Failure(ShiftErrors.TradeShiftChanged);
        }

        return Result<ShiftTradeDto>.Success(await ToDtoAsync(trade, shift, swap, cancellationToken).ConfigureAwait(false));
    }

    private static bool StillHeld(ShiftTrade trade, Shift shift, Shift? swap) =>
        shift.AssigneeId == trade.OfferedById && (swap is null || swap.AssigneeId == trade.TakenById);

    private async Task<Result<ShiftDto>> SaveShiftAsync(Shift shift, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<ShiftDto>.Failure(ShiftErrors.TradeShiftChanged);
        }

        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);
        return Result<ShiftDto>.Success(ToDto(shift, members));
    }

    private async Task<ShiftTradeDto> ToDtoAsync(ShiftTrade trade, Shift shift, Shift? swap, CancellationToken cancellationToken)
    {
        var members = await StaffNames.ReadAsync(_staff, cancellationToken).ConfigureAwait(false);
        var shifts = new Dictionary<int, Shift> { [shift.Id] = shift };
        if (swap is not null)
        {
            shifts[swap.Id] = swap;
        }

        return ToDto(trade, shifts, members, _clock.UtcNow);
    }
}
