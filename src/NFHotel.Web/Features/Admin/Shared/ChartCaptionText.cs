using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The caption under one chart of the report (MIL-026 task 21).</summary>
/// <param name="What">What the chart shows.</param>
/// <param name="Why">Why this kind of chart was chosen.</param>
/// <param name="Limits">What the chart cannot say: the limits of its kind, then the limitations the analysis carries.</param>
/// <param name="Formula">How the figures were calculated, or <c>null</c> when the result has none.</param>
/// <param name="Observation">The observation the AI wrote about this chart, or <c>null</c> when there is none.</param>
/// <param name="NoObservation">The short text shown when there is no observation, saying why when the insight says so.</param>
public sealed record ChartCaptionModel(
    string What,
    string Why,
    IReadOnlyList<string> Limits,
    string? Formula,
    MarketAnalysisObservationDto? Observation,
    string NoObservation);

/// <summary>
/// Writes the captions. The text of what a chart shows, why its kind was chosen and what it cannot say is generic for each kind;
/// the limitations and the formula come from the analysis of the result, and the observation is matched to the chart by the
/// <see cref="MarketAnalysisSeriesDto.Id"/> of its series.
/// </summary>
public static class ChartCaptionText
{
    /// <summary>The first words of the text shown when a chart has no AI observation.</summary>
    public const string NoObservationText = "No AI observation for this chart";

    /// <summary>Writes the caption of a chart that draws a series.</summary>
    /// <param name="kind">The kind of chart, from <see cref="ChartKindSelector"/>.</param>
    /// <param name="series">The series.</param>
    /// <param name="section">The analysis the series belongs to.</param>
    /// <returns>The caption.</returns>
    public static ChartCaptionModel ForSeries(ChartKind kind, MarketAnalysisSeriesDto series, MarketAnalysisSectionDto section)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(section);

        var (what, why, limit) = Texts(kind, series);
        var limits = new List<string> { limit };
        if (series.Points.Any(point => point.SmallSample))
        {
            limits.Add("A figure marked with a star rests on few bookings and can change a lot with one more booking.");
        }

        return Build(what, why, limits, series.Id, section);
    }

    /// <summary>Writes the caption of the heat map of a matrix.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="section">The analysis the matrix belongs to.</param>
    /// <returns>The caption.</returns>
    public static ChartCaptionModel ForMatrix(MarketAnalysisMatrixDto matrix, MarketAnalysisSectionDto section)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(section);

        var method = string.IsNullOrWhiteSpace(matrix.Method) ? string.Empty : $" ({matrix.Method})";

        return Build(
            $"How strongly each pair of the {matrix.Fields.Count} booking fields moves together{method}, from -1 (one rises as the other falls) to +1 (both rise together).",
            "A heat map shows every pair at once. Blue is a negative correlation, red a positive one and white none; the value is written in each cell, and a dash is a pair that could not be calculated.",
            ["A correlation is an association and not a cause, and it sees only the order of the values, not how large the change is. A value near 0 can still hide a link that is not steady."],
            matrix.Id,
            section);
    }

    private static ChartCaptionModel Build(string what, string why, List<string> limits, string seriesId, MarketAnalysisSectionDto section)
    {
        limits.AddRange(section.Limitations ?? []);

        var observation = section.Insight?.Observations?.FirstOrDefault(found => string.Equals(found.Series, seriesId, StringComparison.Ordinal));

        return new ChartCaptionModel(what, why, limits, string.IsNullOrWhiteSpace(section.Formula) ? null : section.Formula, observation, NoObservation(section.Insight));
    }

    // Says why there is no observation: the analysis has no insight, the insight failed (for example GUARDRAIL_REJECTED or TIMEOUT),
    // or the insight is there and the AI wrote nothing about this chart.
    private static string NoObservation(MarketAnalysisInsightDto? insight) => insight switch
    {
        null => $"{NoObservationText}: no AI insight was asked for this analysis.",
        { Status: "available" } => $"{NoObservationText}.",
        _ => string.IsNullOrWhiteSpace(insight.Reason)
            ? $"{NoObservationText}: the AI insight of this analysis is not available."
            : $"{NoObservationText}: the AI insight of this analysis is not available ({insight.Reason}).",
    };

    private static (string What, string Why, string Limit) Texts(ChartKind kind, MarketAnalysisSeriesDto series) => kind switch
    {
        ChartKind.Tile => (
            $"{series.Title}: one figure.",
            "A single figure has nothing to be compared with, so it is shown as a tile and not as a chart.",
            "One figure says nothing about change over time or about spread between groups."),
        ChartKind.Donut => (
            $"{series.Title}: how one whole splits into {series.Points.Count} parts; the parts add up to 100 %.",
            "A donut shows parts of one whole, and is used only for two to four parts that add up to 100 %.",
            "The sizes of slices are hard to compare exactly, so read the values written on and beside them. Shares say nothing about how large the whole is."),
        ChartKind.RankedBars => (
            $"{series.Title}: {series.Points.Count} categories, the largest first.",
            "Many categories are easiest to compare as bars sorted by size.",
            "A category with few bookings gives an unsteady figure, and only the categories the analysis lists are drawn."),
        ChartKind.Columns when series.IsOrdered => (
            $"{series.Title}: the groups in their natural order.",
            "Columns keep the order of bands, buckets or numbers, so a rise or a fall from one group to the next can be seen.",
            "The borders of the bands are fixed; other borders could give another picture. Groups are compared, not single bookings."),
        ChartKind.Columns => (
            $"{series.Title}: {series.Points.Count} separate figures side by side.",
            "Separate rates are not parts of one whole and do not add up to 100 %, so they are drawn as columns to compare and never as a donut.",
            "Each figure has its own base of bookings or days; a gap between two columns is not proof of a cause."),
        ChartKind.Line => (
            $"{series.Title}: the course over time" + (series.ReferenceValue is null ? "." : $", with the {(series.ReferenceLabel ?? "overall").ToLowerInvariant()} level of the whole period as a dashed line."),
            "A line shows how a figure moves over time. Every point is a month or a week, evenly spaced and in order.",
            "A period with few bookings moves the line, and a part period at the start or the end can look like a fall. A pattern over months is not a forecast."),
        ChartKind.Heatmap => (
            $"{series.Title}: each month of each year.",
            "A long series of months is easier to read as months against years: a row is a year, a column is a month, so a season that comes back shows up as a dark column.",
            "A year with only some months has dashes for the others, and a shade is relative to the lowest and highest value of the chart."),
        ChartKind.Correlation => (
            $"{series.Title}: correlations between fields.",
            "A heat map shows every pair at once.",
            "A correlation is an association and not a cause."),
        _ => (
            $"{series.Title}: one bar for each group.",
            "No other kind of chart fits the shape of this series, so it keeps the plain bar chart.",
            "Bars show the groups side by side and say nothing about the order or the course between them."),
    };
}
