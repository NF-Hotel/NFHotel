using System.Globalization;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Presents the price suggestions that the market analysis service made in the latest report and approves one (MIL-055, MIL-071).
/// The service decides the dates, the change and the percentage; this class applies the limits of the settings and works out the
/// new price from the room rate. The AI texts are passed on as texts and never read as a number.
/// </summary>
public sealed class PriceSuggestionService : IPriceSuggestionService
{
    // Two approvals at the same moment could both pass the overlap check; one API instance is assumed, so a lock in the
    // process serialises the check and the save.
    private static readonly SemaphoreSlim ApprovalLock = new(1, 1);

    // The largest discount a price change takes (99.99 percent): a change never takes the whole price away.
    private static readonly decimal LargestDiscount = -(decimal)PriceChange.MinPercentage;

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

        return Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(computed.Basis, computed.Today, open, computed.Explanation, computed.Ideas));
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

    // The service decides the dates, the change and the percentage; the website applies its own limits (the largest change and the
    // minimum price of MIL-055) and works out the new price from the room rate, which the service never sees (MIL-071).
    private async Task<Computed> ComputeAsync(CancellationToken cancellationToken)
    {
        DateOnly today = _clock.Today;
        Result<MarketAnalysisReportDto> report = await _reports.GetLatestReportAsync(cancellationToken).ConfigureAwait(false);

        // No stored report, or one without price suggestions (result schema 1.2), is the case 2a of the use case: nothing to show.
        MarketAnalysisPriceSuggestionsDto? prices = report.IsSuccess ? report.Value.Content?.PriceSuggestions : null;
        if (prices is null)
        {
            return new Computed(PriceSuggestionBasis.NoBasis, today, [], null, []);
        }

        PricingSetting? stored = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        PricingSettings settings = stored is null ? PricingSettings.Defaults : stored.ToRule();
        IReadOnlyList<RoomRate> rates = await _rates.ListAsync(cancellationToken).ConfigureAwait(false);
        PriceSuggestionItemDto[] items =
        [
            .. prices.Suggestions
                .SelectMany(suggestion => ToItems(suggestion, rates, settings))
                .OrderBy(item => item.FirstDay)
                .ThenBy(item => Enum.Parse<RoomSize>(item.Size)),
        ];
        PriceSuggestionTextDto[] ideas = [.. prices.Ideas.Select(idea => new PriceSuggestionTextDto(idea.Text, idea.AiWritten))];

        return new Computed(PriceSuggestionBasis.Ok, today, items, OccupancyObservationReader.ReadExplanation(report.Value.Content), ideas);
    }

    private static IEnumerable<PriceSuggestionItemDto> ToItems(
        MarketAnalysisPriceSuggestionDto suggestion,
        IReadOnlyList<RoomRate> rates,
        PricingSettings settings)
    {
        bool isHigh = string.Equals(suggestion.Change, "surcharge", StringComparison.Ordinal);
        bool forAll = string.Equals(suggestion.RoomSize, "all", StringComparison.OrdinalIgnoreCase);
        IEnumerable<RoomRate> matching = rates
            .Where(rate => forAll || string.Equals(rate.Size.ToString(), suggestion.RoomSize, StringComparison.OrdinalIgnoreCase))
            .GroupBy(rate => rate.Size)
            .Select(group => group.First());

        foreach (RoomRate rate in matching)
        {
            decimal percentage = Math.Min(suggestion.Percent, settings.MaxChangePercent);
            if (!isHigh)
            {
                // The settings allow a discount and a largest change of 100 percent, which an approval would refuse (RC-266 N1).
                percentage = Math.Min(percentage, LargestDiscount);
            }

            bool limited = percentage < suggestion.Percent;
            decimal newPrice = PriceMath.ApplyPercentage(rate.NightlyPrice, isHigh ? percentage : -percentage);
            if (!isHigh && newPrice < settings.MinimumNightlyPrice)
            {
                // The percentage is what is approved and stored, so it must give the cut price: it is the largest discount that
                // keeps the minimum, and the price is the one that percentage gives (never below the minimum).
                percentage = PriceMath.LargestDiscountKeeping(rate.NightlyPrice, settings.MinimumNightlyPrice);
                limited = true;
                if (percentage <= 0m)
                {
                    continue;
                }

                newPrice = PriceMath.ApplyPercentage(rate.NightlyPrice, -percentage);
            }

            decimal signed = isHigh ? percentage : -percentage;
            string size = rate.Size.ToString();
            PriceSuggestionHistoryDto[] history =
            [
                .. suggestion.History.Select(year => new PriceSuggestionHistoryDto(year.Year, Percent(year.Occupancy))),
            ];

            yield return new PriceSuggestionItemDto(
                KeyOf(size, suggestion.FirstDate, suggestion.LastDate, signed),
                size,
                isHigh ? "High" : "Low",
                suggestion.FirstDate,
                suggestion.LastDate,
                signed,
                rate.NightlyPrice,
                newPrice,
                history.Length == 0 ? 0m : Math.Round(history.Average(year => year.OccupancyPercent), 1, MidpointRounding.AwayFromZero),
                Percent(suggestion.Threshold),
                limited,
                suggestion.Reason,
                suggestion.Holiday,
                history,
                new PriceSuggestionTextDto(suggestion.Explanation.Text, suggestion.Explanation.AiWritten));
        }
    }

    // A share between 0 and 1 as percent with one decimal.
    private static decimal Percent(double share) => Math.Round((decimal)share * 100m, 1, MidpointRounding.AwayFromZero);

    private sealed record Computed(
        string Basis,
        DateOnly Today,
        IReadOnlyList<PriceSuggestionItemDto> Items,
        string? Explanation,
        IReadOnlyList<PriceSuggestionTextDto> Ideas);
}
