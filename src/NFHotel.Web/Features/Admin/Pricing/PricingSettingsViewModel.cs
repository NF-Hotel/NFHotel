using System.Globalization;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// State of the pricing settings page (MIL-055): the six numbers, the room sizes that get a discount, and who saved them
/// last. The page holds only markup and binding; the API checks the role and every rule again.
/// </summary>
public sealed class PricingSettingsViewModel
{
    private readonly IUseCaseRunner<IPricingSettingsService> _settings;
    private readonly Dictionary<string, string> _inputs = [];
    private readonly HashSet<RoomSize> _sizes = [];

    /// <summary>Creates the view model.</summary>
    /// <param name="settings">Runs the pricing settings use cases.</param>
    public PricingSettingsViewModel(IUseCaseRunner<IPricingSettingsService> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <summary>The names of the number fields.</summary>
    public static class Fields
    {
        /// <summary>The surcharge of a high season.</summary>
        public const string Surcharge = "surcharge";

        /// <summary>The discount of a low season.</summary>
        public const string Discount = "discount";

        /// <summary>The high threshold.</summary>
        public const string HighThreshold = "high-threshold";

        /// <summary>The low threshold.</summary>
        public const string LowThreshold = "low-threshold";

        /// <summary>The largest change.</summary>
        public const string MaxChange = "max-change";

        /// <summary>The minimum nightly price.</summary>
        public const string MinimumPrice = "minimum-price";
    }

    /// <summary>The room sizes that can get a discount, in the order they are shown.</summary>
    public static IReadOnlyList<RoomSize> Sizes { get; } = Enum.GetValues<RoomSize>();

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the settings have been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>The settings as they are saved, or the defaults; <c>null</c> before they are loaded.</summary>
    public PricingSettingsDto? Current { get; private set; }

    /// <summary>True when the settings shown are the defaults, because none are saved yet.</summary>
    public bool AreDefaults => Current is not null && Current.SetAt is null;

    /// <summary>Says who saved the settings and when, or that they are the defaults.</summary>
    public string SavedText =>
        Current is null
            ? string.Empty
            : Current.SetAt is { } at
                ? $"Last saved by {Current.SetBy} at {at.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC."
                : "Defaults (not saved yet).";

    /// <summary>Gets the text in a number field.</summary>
    /// <param name="field">One of <see cref="Fields"/>.</param>
    /// <returns>The text in the field.</returns>
    public string InputOf(string field) => _inputs.TryGetValue(field, out var text) ? text : string.Empty;

    /// <summary>Sets the text in a number field.</summary>
    /// <param name="field">One of <see cref="Fields"/>.</param>
    /// <param name="text">The text typed.</param>
    public void SetInput(string field, string? text) => _inputs[field] = text ?? string.Empty;

    /// <summary>Tells whether a room size is checked for the discount.</summary>
    /// <param name="size">The room size.</param>
    /// <returns><c>true</c> when it is checked.</returns>
    public bool IsDiscounted(RoomSize size) => _sizes.Contains(size);

    /// <summary>Checks or unchecks a room size for the discount.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="isChecked">Whether it is checked.</param>
    public void SetDiscounted(RoomSize size, bool isChecked)
    {
        if (isChecked)
        {
            _sizes.Add(size);
        }
        else
        {
            _sizes.Remove(size);
        }
    }

    /// <summary>Loads the settings.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the settings are loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _settings.RunAsync((service, token) => service.GetAsync(token), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = Array.Empty<string>();
            Show(result.Value);
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Saves what is in the fields as the new settings.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the call is done.</returns>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        SuccessMessage = null;
        var codes = new List<string>();
        var surcharge = Read(Fields.Surcharge, PricingSettingErrors.SurchargeInvalid, codes);
        var discount = Read(Fields.Discount, PricingSettingErrors.DiscountInvalid, codes);
        var high = Read(Fields.HighThreshold, PricingSettingErrors.HighThresholdInvalid, codes);
        var low = Read(Fields.LowThreshold, PricingSettingErrors.LowThresholdInvalid, codes);
        var maxChange = Read(Fields.MaxChange, PricingSettingErrors.MaxChangeInvalid, codes);
        var minimum = Read(Fields.MinimumPrice, PricingSettingErrors.MinimumPriceInvalid, codes);
        if (codes.Count > 0)
        {
            Errors = ErrorMessages.Describe(Result.FromCodes(codes));
            return;
        }

        var request = new SetPricingSettingsRequest(
            surcharge,
            discount,
            high,
            low,
            maxChange,
            minimum,
            [.. Sizes.Where(_sizes.Contains).Select(size => size.ToString())]);

        IsBusy = true;
        try
        {
            var result = await _settings
                .RunAsync((service, token) => service.SetAsync(request, token), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = Array.Empty<string>();
            Show(result.Value);
            SuccessMessage = "The pricing settings are saved.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private decimal Read(string field, string code, List<string> codes)
    {
        if (NumberInput.TryParseDecimal(InputOf(field), out var value))
        {
            return value;
        }

        codes.Add(code);
        return 0m;
    }

    private void Show(PricingSettingsDto value)
    {
        Current = value;
        _inputs[Fields.Surcharge] = NumberInput.Format(value.SurchargePercent);
        _inputs[Fields.Discount] = NumberInput.Format(value.DiscountPercent);
        _inputs[Fields.HighThreshold] = NumberInput.Format(value.HighThresholdPercent);
        _inputs[Fields.LowThreshold] = NumberInput.Format(value.LowThresholdPercent);
        _inputs[Fields.MaxChange] = NumberInput.Format(value.MaxChangePercent);
        _inputs[Fields.MinimumPrice] = NumberInput.Format(value.MinimumNightlyPrice);
        _sizes.Clear();
        foreach (var name in value.DiscountSizes.Where(name => Enum.IsDefined(typeof(RoomSize), name)))
        {
            _sizes.Add(Enum.Parse<RoomSize>(name));
        }
    }
}
