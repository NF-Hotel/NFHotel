using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>
/// The code behind the forecast: its labels, its charts and the texts about the history it rests on.
/// </summary>
public partial class MarketAnalysisForecast : ComponentBase
{
    /// <summary>The id of the heading, which the contents list links to.</summary>
    public const string SectionId = "forecast-title";

    /// <summary>The label that says who made the forecast.</summary>
    public const string CalculatedLabel = "Calculated by the service, not by an AI";

    /// <summary>The forecast to show.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisForecastDto Forecast { get; set; } = new(string.Empty, [], 0, 0, false, 0, []);

    /// <summary>The charts of the forecast, one for each measure that has months.</summary>
    [Parameter]
    public IReadOnlyList<MarketAnalysisForecastChart> Charts { get; set; } = [];

    private static string HistoryText(MarketAnalysisForecastDto forecast) =>
        $"It rests on {forecast.HistoryMonths} complete months of history and looks {forecast.HorizonMonths} months ahead. "
        + "It repeats the seasonal pattern of the history; it is not a promise.";

    private static string TooShortText(MarketAnalysisForecastDto forecast) =>
        $"The history has {forecast.HistoryMonths} complete months, fewer than the {forecast.MinimumHistoryMonths} months that the service wants, so the pattern may not be reliable.";
}
