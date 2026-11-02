using System.Globalization;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Errors;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// State of the price suggestions page (MIL-051): the open suggestions with the figures they rest on, the price changes, the
/// effective prices of today and the AI explanation. It calls only <see cref="IPriceSuggestionService"/>; the page holds only
/// markup and binding. The API checks the role and every rule again, and the percentages come from the server, never from
/// the AI text.
/// </summary>
public sealed class PriceSuggestionsViewModel
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string StampFormat = "yyyy-MM-dd HH:mm";
    private const string RevertedState = "Reverted";

    private readonly IUseCaseRunner<IPriceSuggestionService> _service;
    private readonly IClock _clock;
    private readonly Dictionary<string, string> _firstDays = [];
    private readonly Dictionary<string, string> _lastDays = [];
    private List<PriceSuggestionItemDto> _items = [];
    private bool _openLoaded;

    /// <summary>Creates the view model.</summary>
    /// <param name="service">Runs the price suggestion use cases.</param>
    /// <param name="clock">Gives today's date.</param>
    public PriceSuggestionsViewModel(IUseCaseRunner<IPriceSuggestionService> service, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(clock);

        _service = service;
        _clock = clock;
    }

    /// <summary>The state of a price change, as the Manager sees it.</summary>
    public enum ChangeStatus
    {
        /// <summary>Approved, and today is within the period.</summary>
        InForce = 0,

        /// <summary>Approved, and the period has not begun.</summary>
        Coming = 1,

        /// <summary>Approved, and the period is over.</summary>
        Finished = 2,

        /// <summary>Reverted while it was in force.</summary>
        Reverted = 3,

        /// <summary>Reverted before it began, so it never counted for a day.</summary>
        Withdrawn = 4
    }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the lists have been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the market analysis gives no basis for suggestions.</summary>
    public bool HasNoBasis { get; private set; }

    /// <summary>True when there is a basis but no price change is suggested.</summary>
    public bool HasNoSuggestions => _openLoaded && !HasNoBasis && Suggestions.Count == 0;

    /// <summary>The AI text that explains the suggestions, or <c>null</c>. It is never used for a price.</summary>
    public string? AiExplanation { get; private set; }

    /// <summary>The open suggestions.</summary>
    public IReadOnlyList<SuggestionRow> Suggestions { get; private set; } = Array.Empty<SuggestionRow>();

    /// <summary>Every price change, the newest first.</summary>
    public IReadOnlyList<ChangeRow> Changes { get; private set; } = Array.Empty<ChangeRow>();

    /// <summary>The effective price of each room size today.</summary>
    public IReadOnlyList<EffectiveRow> EffectivePrices { get; private set; } = Array.Empty<EffectiveRow>();

    /// <summary>Derives the state of a price change.</summary>
    /// <param name="change">The change.</param>
    /// <param name="today">Today's date.</param>
    /// <returns>The state to show.</returns>
    public static ChangeStatus StatusOf(PriceChangeDto change, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (string.Equals(change.State, RevertedState, StringComparison.Ordinal))
        {
            return change.EndedOn is null ? ChangeStatus.Withdrawn : ChangeStatus.Reverted;
        }

        if (change.LastDay < today)
        {
            return ChangeStatus.Finished;
        }

        return change.FirstDay > today ? ChangeStatus.Coming : ChangeStatus.InForce;
    }

    /// <summary>Gives the text of a state.</summary>
    /// <param name="status">The state.</param>
    /// <returns>The text shown.</returns>
    public static string Describe(ChangeStatus status) => status switch
    {
        ChangeStatus.InForce => "In force",
        ChangeStatus.Coming => "Coming",
        ChangeStatus.Finished => "Finished",
        ChangeStatus.Reverted => "Reverted",
        ChangeStatus.Withdrawn => "Withdrawn",
        _ => status.ToString()
    };

    /// <summary>Formats a change in percent with a sign, for example <c>+20 %</c> or <c>−15 %</c>.</summary>
    /// <param name="percentage">The signed change.</param>
    /// <returns>The text.</returns>
    public static string FormatChange(decimal percentage) =>
        (percentage < 0m ? "−" : "+") + Math.Abs(percentage).ToString("0.##", CultureInfo.InvariantCulture) + " %";

    /// <summary>Formats the move of a price, for example <c>100.00 → 120.00</c>.</summary>
    /// <param name="oldPrice">The price before.</param>
    /// <param name="newPrice">The price after.</param>
    /// <returns>The text.</returns>
    public static string FormatMove(decimal oldPrice, decimal newPrice) =>
        $"{NumberInput.Format(oldPrice)} → {NumberInput.Format(newPrice)}";

    /// <summary>Gets the text in the first-day field of a suggestion.</summary>
    /// <param name="key">The key of the suggestion.</param>
    /// <returns>The date as <c>yyyy-MM-dd</c>.</returns>
    public string FirstDayOf(string key) => _firstDays.GetValueOrDefault(key, string.Empty);

    /// <summary>Gets the text in the last-day field of a suggestion.</summary>
    /// <param name="key">The key of the suggestion.</param>
    /// <returns>The date as <c>yyyy-MM-dd</c>.</returns>
    public string LastDayOf(string key) => _lastDays.GetValueOrDefault(key, string.Empty);

    /// <summary>Sets the text in the first-day field of a suggestion.</summary>
    /// <param name="key">The key of the suggestion.</param>
    /// <param name="text">The text typed.</param>
    public void SetFirstDay(string key, string? text) => _firstDays[key] = text ?? string.Empty;

    /// <summary>Sets the text in the last-day field of a suggestion.</summary>
    /// <param name="key">The key of the suggestion.</param>
    /// <param name="text">The text typed.</param>
    public void SetLastDay(string key, string? text) => _lastDays[key] = text ?? string.Empty;

    /// <summary>Loads the suggestions, the changes and the effective prices.</summary>
    /// <param name="cancellationToken">Cancels the calls when the page is closed.</param>
    /// <returns>A task that ends when the lists are loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        SuccessMessage = null;
        IsBusy = true;
        try
        {
            await FillAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Approves a suggestion with the dates in its fields; a date is sent only when it was changed.</summary>
    /// <param name="key">The key of the suggestion.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the call and the reload are done.</returns>
    public async Task ApproveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (IsBusy)
        {
            return;
        }

        SuccessMessage = null;
        var item = _items.Find(candidate => string.Equals(candidate.Key, key, StringComparison.Ordinal));
        if (item is null)
        {
            Errors = ErrorMessages.Describe(Result.Failure(PriceSuggestionErrors.NotFound));
            return;
        }

        if (!TryReadDate(FirstDayOf(key), out var first) || !TryReadDate(LastDayOf(key), out var last))
        {
            Errors = ErrorMessages.Describe(Result.Failure(PriceChangeErrors.PeriodInvalid));
            return;
        }

        var request = new PriceSuggestionApproveRequest(
            key,
            first == item.FirstDay ? null : first,
            last == item.LastDay ? null : last);

        IsBusy = true;
        try
        {
            var result = await _service.RunAsync((service, token) => service.ApproveAsync(request, token), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            var change = result.Value.Change;
            SuccessMessage =
                $"{(result.Value.Created ? "Approved" : "Already approved")}: {change.Size} {FormatChange(change.Percentage)} " +
                $"from {Date(change.FirstDay)} to {Date(change.LastDay)}";
            await FillAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Reverts a price change.</summary>
    /// <param name="id">The key of the change.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the call and the reload are done.</returns>
    public async Task RevertAsync(int id, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        SuccessMessage = null;
        IsBusy = true;
        try
        {
            var result = await _service.RunAsync((service, token) => service.RevertAsync(id, token), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            var change = result.Value;
            var label = $"{change.Size} {FormatChange(change.Percentage)}";
            SuccessMessage = StatusOf(change, _clock.Today) == ChangeStatus.Withdrawn
                ? $"Withdrawn: {label} had not begun, so it never counted."
                : $"Reverted: {label} counts no more.";
            await FillAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string Date(DateOnly day) => day.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static bool TryReadDate(string text, out DateOnly day) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

    private static SuggestionRow ToRow(PriceSuggestionItemDto item)
    {
        var high = string.Equals(item.Season, "High", StringComparison.Ordinal);
        var occupancy = item.OccupancyPercent.ToString("0.0", CultureInfo.InvariantCulture);
        var threshold = item.ThresholdPercent.ToString("0.##", CultureInfo.InvariantCulture);
        return new SuggestionRow(
            item.Key,
            item.Size,
            high ? "High season" : "Low season",
            Date(item.FirstDay),
            Date(item.LastDay),
            FormatChange(item.Percentage),
            FormatMove(item.OldPrice, item.NewPrice),
            $"Occupancy {occupancy} % {(high ? "at or above" : "at or below")} {threshold} %",
            item.WasLimited);
    }

    private static ChangeRow ToRow(PriceChangeDto change, DateOnly today)
    {
        var status = StatusOf(change, today);
        return new ChangeRow(
            change.Id,
            change.Size,
            FormatChange(change.Percentage),
            Date(change.FirstDay),
            Date(change.LastDay),
            Describe(status),
            change.ApprovedBy,
            change.ApprovedAt.UtcDateTime.ToString(StampFormat, CultureInfo.InvariantCulture),
            change.RevertedBy,
            change.RevertedAt?.UtcDateTime.ToString(StampFormat, CultureInfo.InvariantCulture),
            status is ChangeStatus.InForce or ChangeStatus.Coming);
    }

    private static EffectiveRow ToRow(EffectivePriceDto price) =>
        new(
            price.Size,
            price.BaseRate is { } rate ? NumberInput.Format(rate) : null,
            price.Percentage is { } percentage ? FormatChange(percentage) : "-",
            price.Price is { } now ? NumberInput.Format(now) : null);

    private async Task FillAsync(CancellationToken cancellationToken)
    {
        var today = _clock.Today;
        var errors = new List<string>();

        var open = await _service.RunAsync((service, token) => service.GetOpenAsync(token), cancellationToken).ConfigureAwait(false);
        if (open.IsFailure)
        {
            errors.AddRange(ErrorMessages.Describe(open));
        }
        else
        {
            ShowOpen(open.Value);
        }

        var changes = await _service.RunAsync((service, token) => service.GetChangesAsync(token), cancellationToken).ConfigureAwait(false);
        if (changes.IsFailure)
        {
            errors.AddRange(ErrorMessages.Describe(changes));
        }
        else
        {
            Changes = [.. changes.Value.Select(change => ToRow(change, today))];
        }

        var effective = await _service
            .RunAsync((service, token) => service.GetEffectivePricesAsync(today, token), cancellationToken)
            .ConfigureAwait(false);
        if (effective.IsFailure)
        {
            errors.AddRange(ErrorMessages.Describe(effective));
        }
        else
        {
            EffectivePrices = [.. effective.Value.Select(ToRow)];
        }

        Errors = [.. errors.Distinct()];
    }

    private void ShowOpen(PriceSuggestionListDto list)
    {
        _openLoaded = true;
        HasNoBasis = string.Equals(list.Basis, PriceSuggestionBasis.NoBasis, StringComparison.Ordinal);
        AiExplanation = string.IsNullOrWhiteSpace(list.AiExplanation) ? null : list.AiExplanation;
        _items = [.. list.Items];
        Suggestions = [.. _items.Select(ToRow)];
        _firstDays.Clear();
        _lastDays.Clear();
        foreach (var item in _items)
        {
            _firstDays[item.Key] = Date(item.FirstDay);
            _lastDays[item.Key] = Date(item.LastDay);
        }
    }

    /// <summary>One open suggestion, with its texts ready to show.</summary>
    /// <param name="Key">The key to send when it is approved.</param>
    /// <param name="Size">The room size.</param>
    /// <param name="Season">High season or Low season.</param>
    /// <param name="FirstDay">The first day, <c>yyyy-MM-dd</c>.</param>
    /// <param name="LastDay">The last day, <c>yyyy-MM-dd</c>.</param>
    /// <param name="Change">The signed change in percent.</param>
    /// <param name="Move">The old and the new price a night.</param>
    /// <param name="Basis">The occupancy and the threshold it rests on.</param>
    /// <param name="WasLimited">True when the cap or the minimum price limited the price.</param>
    public sealed record SuggestionRow(
        string Key,
        string Size,
        string Season,
        string FirstDay,
        string LastDay,
        string Change,
        string Move,
        string Basis,
        bool WasLimited);

    /// <summary>One price change, with its texts ready to show.</summary>
    /// <param name="Id">The key of the change.</param>
    /// <param name="Size">The room size.</param>
    /// <param name="Change">The signed change in percent.</param>
    /// <param name="FirstDay">The first day.</param>
    /// <param name="LastDay">The last day.</param>
    /// <param name="State">The state text.</param>
    /// <param name="ApprovedBy">Who approved it.</param>
    /// <param name="ApprovedAt">When it was approved (UTC).</param>
    /// <param name="RevertedBy">Who reverted it, or <c>null</c>.</param>
    /// <param name="RevertedAt">When it was reverted (UTC), or <c>null</c>.</param>
    /// <param name="CanRevert">True when the change is in force or coming.</param>
    public sealed record ChangeRow(
        int Id,
        string Size,
        string Change,
        string FirstDay,
        string LastDay,
        string State,
        string ApprovedBy,
        string ApprovedAt,
        string? RevertedBy,
        string? RevertedAt,
        bool CanRevert);

    /// <summary>The effective price of one room size today.</summary>
    /// <param name="Size">The room size.</param>
    /// <param name="BaseRate">The base rate, or <c>null</c> when none is set.</param>
    /// <param name="Change">The change that counts today, or a dash.</param>
    /// <param name="Price">The price today, or <c>null</c> when no base rate is set.</param>
    public sealed record EffectiveRow(string Size, string? BaseRate, string Change, string? Price);
}
