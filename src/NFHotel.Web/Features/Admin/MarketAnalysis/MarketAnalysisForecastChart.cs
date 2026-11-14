using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>One chart of the forecast as the page draws it: the expected value of each month with the band of the history.</summary>
/// <param name="Id">The measure: <c>arrivals</c>, <c>occupancy</c> or <c>room_revenue</c>.</param>
/// <param name="Title">The title of the chart.</param>
/// <param name="Unit">The unit of the values.</param>
/// <param name="Points">The expected value of each month.</param>
/// <param name="Low">The low end of the band for each month; <c>null</c> where it cannot be known.</param>
/// <param name="High">The high end of the band for each month; <c>null</c> where it cannot be known.</param>
/// <param name="Caption">What the chart shows and who calculated it.</param>
public sealed record MarketAnalysisForecastChart(
    string Id,
    string Title,
    string Unit,
    IReadOnlyList<MarketAnalysisPointDto> Points,
    IReadOnlyList<double?> Low,
    IReadOnlyList<double?> High,
    string Caption)
{
    /// <summary>Builds the chart of one measure.</summary>
    /// <param name="measure">The measure with its months.</param>
    /// <param name="forecast">The forecast the measure belongs to, for the months of history.</param>
    /// <returns>The chart.</returns>
    public static MarketAnalysisForecastChart From(MarketAnalysisForecastMeasureDto measure, MarketAnalysisForecastDto forecast)
    {
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentNullException.ThrowIfNull(forecast);

        string name = measure.Id switch
        {
            "arrivals" => "arrivals",
            "occupancy" => "occupancy",
            "room_revenue" => "room revenue",
            _ => ChartFormat.Readable(measure.Id),
        };
        bool hasBand = measure.Months.All(month => month.Low.HasValue && month.High.HasValue);
        string band = hasBand
            ? "The shaded area is the band from the low to the high value of the same calendar month in the history; the real value can lie outside it."
            : "The band could not be calculated for every month, so it is not drawn.";

        return new MarketAnalysisForecastChart(
            measure.Id,
            $"Forecast of {name}",
            measure.Unit,
            [.. measure.Months.Select(month => new MarketAnalysisPointDto(month.Month, month.Value))],
            [.. measure.Months.Select(month => month.Low)],
            [.. measure.Months.Select(month => month.High)],
            $"Forecast of {name} for the next {measure.Months.Count} months, calculated by the service from {forecast.HistoryMonths} months of history, not by an AI. {band}");
    }
}
