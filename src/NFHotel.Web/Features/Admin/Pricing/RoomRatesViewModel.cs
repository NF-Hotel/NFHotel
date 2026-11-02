using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Pricing;

/// <summary>
/// State of the room rates page (MIL-055): the nightly base rate of each room size, who set it and when, and one price
/// field per size. The page holds only markup and binding; the API checks the role and every rule again.
/// </summary>
public sealed class RoomRatesViewModel
{
    private readonly IUseCaseRunner<IRoomRateService> _rates;
    private readonly Dictionary<RoomSize, RoomRateDto> _current = [];
    private readonly Dictionary<RoomSize, string> _inputs = [];

    /// <summary>Creates the view model.</summary>
    /// <param name="rates">Runs the room rate use cases.</param>
    public RoomRatesViewModel(IUseCaseRunner<IRoomRateService> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        _rates = rates;
    }

    /// <summary>The room sizes that have a rate, in the order they are shown.</summary>
    public static IReadOnlyList<RoomSize> Sizes { get; } = Enum.GetValues<RoomSize>();

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the rates have been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Gets the rate that is set for a size.</summary>
    /// <param name="size">The room size.</param>
    /// <returns>The rate, or <c>null</c> when none is set.</returns>
    public RoomRateDto? RateOf(RoomSize size) => _current.GetValueOrDefault(size);

    /// <summary>Gets the text in the price field of a size.</summary>
    /// <param name="size">The room size.</param>
    /// <returns>The text in the field: the current price after a load or a save, or what was typed.</returns>
    public string InputOf(RoomSize size) =>
        _inputs.TryGetValue(size, out string? text) ? text : string.Empty;

    /// <summary>Sets the text in the price field of a size.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="text">The text typed.</param>
    public void SetInput(RoomSize size, string? text) => _inputs[size] = text ?? string.Empty;

    /// <summary>Loads the rates.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the rates are loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            Result<IReadOnlyList<RoomRateDto>> result = await _rates.RunAsync((service, token) => service.GetAllAsync(token), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = Array.Empty<string>();
            _current.Clear();
            foreach (RoomRateDto? rate in result.Value.Where(rate => Enum.IsDefined(typeof(RoomSize), rate.Size)))
            {
                RoomSize size = Enum.Parse<RoomSize>(rate.Size);
                _current[size] = rate;
                _inputs[size] = Format(rate.NightlyPrice);
            }
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Saves the price in the field of a size as its new rate.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the call is done.</returns>
    public async Task SaveAsync(RoomSize size, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        SuccessMessage = null;
        if (!NumberInput.TryParseDecimal(InputOf(size), out decimal price))
        {
            Errors = ErrorMessages.Describe(Result.Failure(RoomRateErrors.PriceInvalid));
            return;
        }

        IsBusy = true;
        try
        {
            Result<RoomRateDto> result = await _rates
                .RunAsync((service, token) => service.SetAsync(size.ToString(), new SetRoomRateRequest(price), token), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = Array.Empty<string>();
            _current[size] = result.Value;
            _inputs[size] = Format(result.Value.NightlyPrice);
            SuccessMessage = $"The rate of {size} is now {Format(result.Value.NightlyPrice)} a night.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string Format(decimal price) => NumberInput.Format(price);
}
