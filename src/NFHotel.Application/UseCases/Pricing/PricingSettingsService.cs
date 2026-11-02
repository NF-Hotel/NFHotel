using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>Reads and saves the pricing settings of the Manager (MIL-051).</summary>
public sealed class PricingSettingsService : IPricingSettingsService
{
    private readonly IPricingSettingRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="settings">The storage of the settings.</param>
    /// <param name="audit">The audit trail the change is written to.</param>
    /// <param name="unitOfWork">Saves the settings and their audit event in one transaction.</param>
    /// <param name="clock">The clock that says when the settings were saved.</param>
    /// <param name="context">Who is calling.</param>
    public PricingSettingsService(
        IPricingSettingRepository settings,
        IStaffAuditRepository audit,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffCallContext context)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(context);

        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    /// <inheritdoc />
    public async Task<Result<PricingSettingsDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);

        return Result<PricingSettingsDto>.Success(stored is null ? DefaultsDto() : ToDto(stored));
    }

    /// <inheritdoc />
    public async Task<Result<PricingSettingsDto>> SetAsync(
        SetPricingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseSizes(request.DiscountSizes, out var sizes))
        {
            return Result<PricingSettingsDto>.FromCodes([PricingSettingErrors.DiscountSizesInvalid]);
        }

        var now = _clock.UtcNow;
        var setBy = _context.StaffName ?? string.Empty;
        PricingSetting candidate;
        try
        {
            // The domain is the authority: a candidate is built first, so an invalid request changes and saves nothing.
            candidate = PricingSetting.Create(
                request.SurchargePercent,
                request.DiscountPercent,
                request.HighThresholdPercent,
                request.LowThresholdPercent,
                request.MaxChangePercent,
                request.MinimumNightlyPrice,
                sizes,
                setBy,
                now);
        }
        catch (DomainException exception)
        {
            return Result<PricingSettingsDto>.FromCodes(exception.Codes);
        }

        var existing = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        var before = existing is null ? PricingSettings.Defaults : existing.ToRule();
        var changes = Differences(before, candidate.ToRule());
        if (changes.Count == 0)
        {
            return Result<PricingSettingsDto>.Success(existing is null ? DefaultsDto() : ToDto(existing));
        }

        PricingSetting stored;
        if (existing is null)
        {
            _settings.Add(candidate);
            stored = candidate;
        }
        else
        {
            existing.Change(
                candidate.SurchargePercent,
                candidate.DiscountPercent,
                candidate.HighThresholdPercent,
                candidate.LowThresholdPercent,
                candidate.MaxChangePercent,
                candidate.MinimumNightlyPrice,
                candidate.DiscountSizes,
                setBy,
                now);
            stored = existing;
        }

        _recorder.ByCallerWithDetail(
            StaffAuditAction.PricingSettingsChanged,
            StaffAuditRecorder.Success,
            string.Join("; ", changes));

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // The race safeguard behind the check above: a simultaneous first save hit the unique index.
            return Result<PricingSettingsDto>.FromCodes([PricingSettingErrors.ConcurrencyConflict]);
        }

        return Result<PricingSettingsDto>.Success(ToDto(stored));
    }

    private static bool TryParseSizes(IReadOnlyList<string>? names, out List<RoomSize> sizes)
    {
        sizes = [];
        foreach (var name in names ?? [])
        {
            if (!RoomSizeText.TryParse(name, out var size))
            {
                return false;
            }

            sizes.Add(size);
        }

        return true;
    }

    private static List<string> Differences(PricingSettings before, PricingSettings after)
    {
        var changes = new List<string>();
        AddNumber(changes, "Surcharge", before.SurchargePercent, after.SurchargePercent);
        AddNumber(changes, "Discount", before.DiscountPercent, after.DiscountPercent);
        AddNumber(changes, "HighThreshold", before.HighThreshold, after.HighThreshold);
        AddNumber(changes, "LowThreshold", before.LowThreshold, after.LowThreshold);
        AddNumber(changes, "MaxChange", before.MaxChangePercent, after.MaxChangePercent);
        AddNumber(changes, "MinimumPrice", before.MinimumNightlyPrice, after.MinimumNightlyPrice);

        var oldSizes = SizesText(before.DiscountSizes);
        var newSizes = SizesText(after.DiscountSizes);
        if (oldSizes != newSizes)
        {
            changes.Add($"DiscountSizes: {oldSizes} -> {newSizes}");
        }

        return changes;
    }

    private static void AddNumber(List<string> changes, string name, decimal before, decimal after)
    {
        if (before != after)
        {
            changes.Add($"{name}: {MoneyText.Format(before)} -> {MoneyText.Format(after)}");
        }
    }

    private static string SizesText(IEnumerable<RoomSize> sizes) =>
        string.Join(",", sizes.Distinct().OrderBy(size => size));

    private static PricingSettingsDto DefaultsDto()
    {
        var defaults = PricingSettings.Defaults;

        return new PricingSettingsDto(
            defaults.SurchargePercent,
            defaults.DiscountPercent,
            defaults.HighThreshold,
            defaults.LowThreshold,
            defaults.MaxChangePercent,
            defaults.MinimumNightlyPrice,
            [.. defaults.DiscountSizes.Select(size => size.ToString())],
            null,
            null);
    }

    private static PricingSettingsDto ToDto(PricingSetting value) =>
        new(
            value.SurchargePercent,
            value.DiscountPercent,
            value.HighThresholdPercent,
            value.LowThresholdPercent,
            value.MaxChangePercent,
            value.MinimumNightlyPrice,
            [.. value.DiscountSizes.Select(size => size.ToString())],
            value.SetBy,
            value.SetAt);
}
