using System.Globalization;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Makes the open price suggestions from the latest market analysis and approves one (MIL-055). The figures come from
/// <see cref="PriceSuggestionRule"/> alone; the AI text of the report is passed on as an explanation and never read as a number.
/// </summary>
public sealed class PriceSuggestionService : IPriceSuggestionService
{
    // Two approvals at the same moment could both pass the overlap check; one API instance is assumed, so a lock in the
    // process serialises the check and the save.
    private static readonly SemaphoreSlim ApprovalLock = new(1, 1);

    private readonly IMarketAnalysisService _reports;
    private readonly IRoomRateRepository _rates;
    private readonly IPricingSettingRepository _settings;
    private readonly IPriceChangeRepository _changes;
    private readonly IPriceChangeService _changeService;
    private readonly IClock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="reports">The market analysis, which gives the latest report.</param>
    /// <param name="rates">The storage of the base rates.</param>
    /// <param name="settings">The storage of the pricing settings.</param>
    /// <param name="changes">The storage of the price changes.</param>
    /// <param name="changeService">Approves and reverts the changes, with their audit events.</param>
    /// <param name="clock">The clock that says what today is.</param>
    public PriceSuggestionService(
        IMarketAnalysisService reports,
        IRoomRateRepository rates,
        IPricingSettingRepository settings,
        IPriceChangeRepository changes,
        IPriceChangeService changeService,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(rates);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(changeService);
        ArgumentNullException.ThrowIfNull(clock);

        _reports = reports;
        _rates = rates;
        _settings = settings;
        _changes = changes;
        _changeService = changeService;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<PriceSuggestionListDto>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        Computed computed = await ComputeAsync(cancellationToken).ConfigureAwait(false);
        if (computed.Basis == PriceSuggestionBasis.NoBasis)
        {
            return Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(computed.Basis, computed.Today, [], null));
        }

        IReadOnlyList<PriceChange> existing = await _changes.ListAsync(cancellationToken).ConfigureAwait(false);
        PriceSuggestionItemDto[] open = computed.Items.Where(item => !IsCovered(item, existing)).ToArray();

        return Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(computed.Basis, computed.Today, open, computed.Explanation));
    }

    /// <inheritdoc />
    public async Task<Result<ApprovedPriceChange>> ApproveAsync(
        PriceSuggestionApproveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ApprovalLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Not the list of open suggestions: a repeated click on a suggestion that is approved by now must still find it
            // so that the change service can answer that the change exists.
            Computed computed = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            PriceSuggestionItemDto? match = computed.Items.FirstOrDefault(item => string.Equals(item.Key, request.Key, StringComparison.Ordinal));
            if (match is null)
            {
                return Result<ApprovedPriceChange>.FromCodes([PriceSuggestionErrors.NotFound]);
            }

            return await _changeService
                .ApproveAsync(match.Size, match.Percentage, request.FirstDay ?? match.FirstDay, request.LastDay ?? match.LastDay, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ApprovalLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<Result<PriceChangeDto>> RevertAsync(int id, CancellationToken cancellationToken = default) =>
        _changeService.RevertAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PriceChangeDto>>> GetChangesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PriceChange> changes = await _changes.ListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<PriceChangeDto> list =
        [
            .. changes
                .OrderByDescending(change => change.FirstDay)
                .ThenBy(change => change.Size)
                .ThenByDescending(change => change.Id)
                .Select(change => change.ToDto()),
        ];

        return Result<IReadOnlyList<PriceChangeDto>>.Success(list);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<EffectivePriceDto>>> GetEffectivePricesAsync(
        DateOnly? day,
        CancellationToken cancellationToken = default) =>
        _changeService.GetEffectivePricesAsync(day ?? _clock.Today, cancellationToken);

    /// <summary>The key of a suggestion, for example <c>single|2027-06-01|2027-08-31|20.00</c>.</summary>
    /// <param name="size">The room size name.</param>
    /// <param name="firstDay">The first day.</param>
    /// <param name="lastDay">The last day.</param>
    /// <param name="percentage">The signed percentage.</param>
    /// <returns>The key.</returns>
    internal static string KeyOf(string size, DateOnly firstDay, DateOnly lastDay, decimal percentage) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{size.ToLowerInvariant()}|{firstDay:yyyy-MM-dd}|{lastDay:yyyy-MM-dd}|{percentage:0.00}");

    // A suggestion is covered when the days that changes of its size count on leave no day of its period free.
    private static bool IsCovered(PriceSuggestionItemDto item, IReadOnlyList<PriceChange> existing)
    {
        PriceChange[] counting = existing
            .Where(change => string.Equals(change.Size.ToString(), item.Size, StringComparison.Ordinal)
                && change.CountsWithin(item.FirstDay, item.LastDay))
            .OrderBy(change => change.FirstDay)
            .ToArray();

        DateOnly next = item.FirstDay;
        foreach (PriceChange? change in counting)
        {
            if (change.FirstDay > next)
            {
                break;
            }

            // CountsWithin is true, so the last counted day exists.
            DateOnly last = change.CountedLastDay!.Value;
            if (last >= next)
            {
                next = last.AddDays(1);
            }
        }

        return next > item.LastDay;
    }

    private async Task<Computed> ComputeAsync(CancellationToken cancellationToken)
    {
        DateOnly today = _clock.Today;
        Result<MarketAnalysisReportDto> report = await _reports.GetLatestReportAsync(cancellationToken).ConfigureAwait(false);

        // No stored report (or one that cannot be read) is the case 2a of the use case: nothing to suggest from.
        IReadOnlyList<OccupancyObservation> observations = report.IsSuccess ? OccupancyObservationReader.Read(report.Value.Content) : [];

        PricingSetting? stored = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        PricingSettings settings = stored is null ? PricingSettings.Defaults : stored.ToRule();
        if (!SeasonRule.ByCalendarMonth(observations, settings).HasBasis)
        {
            return new Computed(PriceSuggestionBasis.NoBasis, today, [], null);
        }

        IReadOnlyList<RoomRate> rates = await _rates.ListAsync(cancellationToken).ConfigureAwait(false);
        RoomRateValue[] values = rates.Select(rate => new RoomRateValue(rate.Size, rate.NightlyPrice)).ToArray();
        PriceSuggestionItemDto[] items = PriceSuggestionRule.Suggest(observations, values, settings, today).Select(ToItem).ToArray();

        return new Computed(PriceSuggestionBasis.Ok, today, items, OccupancyObservationReader.ReadExplanation(report.Value.Content));
    }

    private static PriceSuggestionItemDto ToItem(PriceSuggestion suggestion)
    {
        decimal signed = suggestion.Season == Season.High ? suggestion.Percentage : -suggestion.Percentage;
        string size = suggestion.Size.ToString();

        return new PriceSuggestionItemDto(
            KeyOf(size, suggestion.FirstDay, suggestion.LastDay, signed),
            size,
            suggestion.Season.ToString(),
            suggestion.FirstDay,
            suggestion.LastDay,
            signed,
            suggestion.OldPrice,
            suggestion.NewPrice,
            suggestion.OccupancyPercent,
            suggestion.ThresholdPercent,
            suggestion.WasLimited);
    }

    private sealed record Computed(string Basis, DateOnly Today, IReadOnlyList<PriceSuggestionItemDto> Items, string? Explanation);
}
