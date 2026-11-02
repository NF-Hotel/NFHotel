using System.Globalization;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Creates and reverts price changes and gives the effective price of a room size on a day (MIL-055). An approval and a
/// revert write their audit event in the same save as the change.
/// </summary>
public sealed class PriceChangeService : IPriceChangeService
{
    private readonly IPriceChangeRepository _changes;
    private readonly IRoomRateRepository _rates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="changes">The storage of the price changes.</param>
    /// <param name="rates">The storage of the base rates.</param>
    /// <param name="audit">The audit trail an approval and a revert are written to.</param>
    /// <param name="unitOfWork">Saves an approval or a revert in one call.</param>
    /// <param name="clock">The clock that says when a change was approved or reverted, and what today is.</param>
    /// <param name="context">Who is calling.</param>
    public PriceChangeService(
        IPriceChangeRepository changes,
        IRoomRateRepository rates,
        IStaffAuditRepository audit,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffCallContext context)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(context);

        _changes = changes;
        _rates = rates;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    /// <inheritdoc />
    public async Task<Result<ApprovedPriceChange>> ApproveAsync(
        string size,
        decimal percentage,
        DateOnly firstDay,
        DateOnly lastDay,
        CancellationToken cancellationToken = default)
    {
        if (!RoomSizeText.TryParse(size, out RoomSize parsed))
        {
            return Result<ApprovedPriceChange>.FromCodes([PriceChangeErrors.SizeInvalid]);
        }

        PriceChange candidate;
        try
        {
            // The domain is the authority: the candidate is built first, so an invalid request changes and saves nothing.
            candidate = PriceChange.Approve(parsed, percentage, firstDay, lastDay, _context.StaffName ?? string.Empty, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result<ApprovedPriceChange>.FromCodes(exception.Codes);
        }

        // A period wholly in the past counts for no future booking and could not be reverted afterwards (UC-007 3b).
        if (lastDay < _clock.Today)
        {
            return Result<ApprovedPriceChange>.FromCodes([PriceChangeErrors.PeriodInvalid]);
        }

        IReadOnlyList<PriceChange> neighbours = await _changes.ListForSizeAsync(parsed, firstDay, lastDay, cancellationToken).ConfigureAwait(false);
        PriceChange[] counting = neighbours.Where(item => item.CountsWithin(candidate.FirstDay, candidate.LastDay)).ToArray();

        // A repeated click: the same approved change already exists, so nothing is created.
        PriceChange? same = counting.FirstOrDefault(item => IsSameChange(item, candidate));
        if (same is not null)
        {
            return Result<ApprovedPriceChange>.Success(new ApprovedPriceChange(same.ToDto(), Created: false));
        }

        if (counting.Length > 0)
        {
            return Result<ApprovedPriceChange>.FromCodes([PriceChangeErrors.Overlap]);
        }

        _changes.Add(candidate);

        // The audit event is added before the one save, so the change and its record are stored together or not at all.
        RoomRate? rate = await _rates.FindBySizeAsync(parsed, cancellationToken).ConfigureAwait(false);
        _recorder.ByCallerWithDetail(
            StaffAuditAction.PriceChangeApproved,
            StaffAuditRecorder.Success,
            ApprovedDetail(candidate, rate?.NightlyPrice));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ApprovedPriceChange>.Success(new ApprovedPriceChange(candidate.ToDto(), Created: true));
    }

    /// <inheritdoc />
    public async Task<Result<PriceChangeDto>> RevertAsync(int id, CancellationToken cancellationToken = default)
    {
        PriceChange? change = await _changes.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (change is null)
        {
            return Result<PriceChangeDto>.FromCodes([PriceChangeErrors.NotFound]);
        }

        try
        {
            change.Revert(_context.StaffName ?? string.Empty, _clock.UtcNow, _clock.Today);
        }
        catch (DomainException exception)
        {
            return Result<PriceChangeDto>.FromCodes(exception.Codes);
        }

        _recorder.ByCallerWithDetail(
            StaffAuditAction.PriceChangeReverted,
            StaffAuditRecorder.Success,
            RevertedDetail(change));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PriceChangeDto>.Success(change.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<EffectivePriceDto>> GetEffectivePriceAsync(
        string size,
        DateOnly day,
        CancellationToken cancellationToken = default)
    {
        if (!RoomSizeText.TryParse(size, out RoomSize parsed))
        {
            return Result<EffectivePriceDto>.FromCodes([PriceChangeErrors.SizeInvalid]);
        }

        RoomRate? rate = await _rates.FindBySizeAsync(parsed, cancellationToken).ConfigureAwait(false);
        EffectivePriceDto price = await EffectiveAsync(parsed, rate?.NightlyPrice, day, cancellationToken).ConfigureAwait(false);

        return Result<EffectivePriceDto>.Success(price);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EffectivePriceDto>>> GetEffectivePricesAsync(
        DateOnly day,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomRate> rates = await _rates.ListAsync(cancellationToken).ConfigureAwait(false);
        List<EffectivePriceDto> prices = new();
        foreach (RoomSize size in Enum.GetValues<RoomSize>())
        {
            RoomRate? rate = rates.FirstOrDefault(item => item.Size == size);
            prices.Add(await EffectiveAsync(size, rate?.NightlyPrice, day, cancellationToken).ConfigureAwait(false));
        }

        return Result<IReadOnlyList<EffectivePriceDto>>.Success(prices);
    }

    private static string Describe(PriceChange change) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{change.Size} {change.Percentage:+0.00;-0.00}% {change.FirstDay:yyyy-MM-dd}..{change.LastDay:yyyy-MM-dd}");

    private static string ApprovedDetail(PriceChange change, decimal? baseRate)
    {
        string text = Describe(change);

        return baseRate is null
            ? text
            : $"{text} ({MoneyText.Format(baseRate.Value)} -> {MoneyText.Format(PriceMath.ApplyPercentage(baseRate.Value, change.Percentage))})";
    }

    private static string RevertedDetail(PriceChange change) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"#{change.Id} {Describe(change)}; {(change.EndedOn is { } ended ? $"ended {ended:yyyy-MM-dd}" : "withdrawn")}");

    private static bool IsSameChange(PriceChange existing, PriceChange candidate) =>
        existing.State == PriceChangeState.Approved
        && existing.Percentage == candidate.Percentage
        && existing.FirstDay == candidate.FirstDay
        && existing.LastDay == candidate.LastDay;

    private async Task<EffectivePriceDto> EffectiveAsync(
        RoomSize size,
        decimal? baseRate,
        DateOnly day,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PriceChange> candidates = await _changes.ListForSizeAsync(size, day, day, cancellationToken).ConfigureAwait(false);

        // The overlap rule leaves at most one; should the data hold two, the one approved last wins and nothing throws.
        PriceChange? counting = candidates
            .Where(item => item.CountsOn(day))
            .OrderByDescending(item => item.ApprovedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        if (baseRate is null)
        {
            return new EffectivePriceDto(size.ToString(), day, null, null, counting?.Percentage);
        }

        decimal price = counting is null
            ? baseRate.Value
            : PriceMath.ApplyPercentage(baseRate.Value, counting.Percentage);

        return new EffectivePriceDto(size.ToString(), day, baseRate, price, counting?.Percentage);
    }
}
