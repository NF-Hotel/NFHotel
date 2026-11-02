using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.RoomRates;

/// <summary>Reads and sets the nightly base rate per room size (MIL-055).</summary>
public sealed class RoomRateService : IRoomRateService
{
    private const string NoneText = "(none)";

    private readonly IRoomRateRepository _rates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="rates">The storage of the rates.</param>
    /// <param name="audit">The audit trail the change is written to.</param>
    /// <param name="unitOfWork">Saves the rate and its audit event in one transaction.</param>
    /// <param name="clock">The clock that says when a rate was set.</param>
    /// <param name="context">Who is calling.</param>
    public RoomRateService(
        IRoomRateRepository rates,
        IStaffAuditRepository audit,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffCallContext context)
    {
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(context);

        _rates = rates;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoomRateDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomRate> rates = await _rates.ListAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomRateDto>>.Success(rates.Select(ToDto).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result<RoomRateDto>> SetAsync(
        string size,
        SetRoomRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!RoomSizeText.TryParse(size, out RoomSize parsed))
        {
            return Result<RoomRateDto>.FromCodes([RoomRateErrors.SizeInvalid]);
        }

        DateTimeOffset now = _clock.UtcNow;
        string setBy = _context.StaffName ?? string.Empty;
        RoomRate stored;
        string oldText;
        try
        {
            RoomRate? existing = await _rates.FindBySizeAsync(parsed, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                oldText = NoneText;
                stored = RoomRate.Create(parsed, request.NightlyPrice, setBy, now);
                _rates.Add(stored);
            }
            else
            {
                oldText = MoneyText.Format(existing.NightlyPrice);
                existing.Change(request.NightlyPrice, setBy, now);
                stored = existing;
            }
        }
        catch (DomainException exception)
        {
            return Result<RoomRateDto>.FromCodes(exception.Codes);
        }

        _recorder.ByCallerWithDetail(
            StaffAuditAction.RoomRateChanged,
            StaffAuditRecorder.Success,
            $"{parsed}: {oldText} -> {MoneyText.Format(stored.NightlyPrice)}");

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // The race safeguard behind the check above: a simultaneous first save hit the unique index.
            return Result<RoomRateDto>.FromCodes([RoomRateErrors.ConcurrencyConflict]);
        }

        return Result<RoomRateDto>.Success(ToDto(stored));
    }

    private static RoomRateDto ToDto(RoomRate value) =>
        new(value.Size.ToString(), value.NightlyPrice, value.SetBy, value.SetAt);
}
