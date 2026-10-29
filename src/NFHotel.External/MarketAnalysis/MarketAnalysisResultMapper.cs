using System.Globalization;
using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.External.MarketAnalysis;

/// <summary>
/// Reads the result document of the market analysis service (result schema 1.0 and 1.1, <c>RunResult</c> of
/// <c>openapi/market-contract.yaml</c> of <c>notebook-market-analysis</c>) into <see cref="MarketAnalysisResultDto"/>. The
/// document is read as JSON and not as a fixed type, because the <c>findings</c> of every analysis have their own shape and a
/// new MINOR version adds fields. It reads the nine analyses of 1.1 (lead time, holidays, seasonality, cancellations, room
/// value, guest mix, occupancy, occupancy on holidays and correlations), the <c>formula</c> and <c>limitations</c> of each, and
/// the <c>observations</c> of each insight; a document without them (1.0) is read with those fields empty.
/// </summary>
public static class MarketAnalysisResultMapper
{
    private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        ["lead_time"] = "Lead time",
        ["holidays"] = "Cambodian holidays",
        ["seasonality"] = "Seasonality and booking pace",
        ["cancellations"] = "Cancellations",
        ["room_value"] = "Room value and stay",
        ["guest_mix"] = "Guest and booking mix",
        ["occupancy"] = "Occupancy",
        ["occupancy_on_holidays"] = "Occupancy on holidays",
        ["correlations"] = "Correlations between booking fields",
    };

    /// <summary>Reads a result document.</summary>
    /// <param name="document">The root of the answer to <c>POST /analyze</c>.</param>
    /// <returns>The result, or <c>null</c> when the document is not a result: no schema version, or a failed run.</returns>
    public static MarketAnalysisResultDto? Map(JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object
            || Text(document, "schema_version") is not { } version
            || MajorOf(version) is not { } major
            || Text(document, "status") == "failed")
        {
            return null;
        }

        var generatedAt = Text(document, "generated_at") is { } text
            && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : default;

        var provider = default(string);
        var model = default(string);
        if (document.TryGetProperty("insights", out var insights) && insights.ValueKind == JsonValueKind.Object)
        {
            provider = Text(insights, "provider");
            model = Text(insights, "model");
        }

        var sections = new List<MarketAnalysisSectionDto>();
        if (document.TryGetProperty("analyses", out var analyses) && analyses.ValueKind == JsonValueKind.Object)
        {
            sections.AddRange(analyses.EnumerateObject().Select(analysis => Section(analysis.Name, analysis.Value)));
        }

        return new MarketAnalysisResultDto(
            Guid.TryParse(Text(document, "result_id"), out var resultId) ? resultId : Guid.Empty,
            major,
            generatedAt,
            generatedAt,
            provider,
            model,
            new MarketAnalysisContentDto(Figures(document), sections));
    }

    private static IReadOnlyList<MarketAnalysisFigureDto> Figures(JsonElement document)
    {
        var figures = new List<MarketAnalysisFigureDto>();
        if (document.TryGetProperty("data_quality", out var quality) && quality.ValueKind == JsonValueKind.Object)
        {
            Add(figures, quality, "record_count", "Bookings analysed");
            Add(figures, quality, "zero_price_count", "Bookings with a price of 0");
            Add(figures, quality, "duplicate_booking_id_count", "Duplicate booking ids");
        }

        return figures;
    }

    private static void Add(List<MarketAnalysisFigureDto> figures, JsonElement source, string name, string label)
    {
        if (source.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            figures.Add(new MarketAnalysisFigureDto(label, value.GetDouble(), string.Empty));
        }
    }

    private static MarketAnalysisSectionDto Section(string name, JsonElement analysis)
    {
        var findings = analysis.TryGetProperty("findings", out var found) ? found : default;

        return new MarketAnalysisSectionDto(
            name,
            Titles.TryGetValue(name, out var title) ? title : Humanize(name),
            Text(analysis, "status") ?? "unavailable",
            Text(analysis, "reason"),
            findings.ValueKind == JsonValueKind.Undefined ? [] : FindingsSeries.Extract(findings),
            analysis.TryGetProperty("insight", out var insight) && insight.ValueKind == JsonValueKind.Object ? Insight(insight) : null,
            Text(analysis, "formula"),
            Strings(analysis, "limitations"),
            findings.ValueKind == JsonValueKind.Undefined ? null : FindingsSeries.ExtractMatrices(findings));
    }

    private static MarketAnalysisInsightDto Insight(JsonElement insight)
    {
        var suggestions = new List<MarketAnalysisSuggestionDto>();
        if (insight.TryGetProperty("improvement_suggestions", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                if (Text(item, "suggestion") is { } suggestion)
                {
                    suggestions.Add(new MarketAnalysisSuggestionDto(
                        suggestion,
                        Text(item, "evidence") ?? string.Empty,
                        item.TryGetProperty("sample_size", out var size) && size.ValueKind == JsonValueKind.Number ? size.GetInt32() : 0));
                }
            }
        }

        return new MarketAnalysisInsightDto(
            Text(insight, "status") ?? "unavailable",
            Text(insight, "reason"),
            Text(insight, "executive_summary"),
            suggestions,
            Observations(insight));
    }

    // One observation per chart; an entry without text or without the path of its series cannot be shown under a chart.
    private static List<MarketAnalysisObservationDto> Observations(JsonElement insight)
    {
        var observations = new List<MarketAnalysisObservationDto>();
        if (insight.TryGetProperty("observations", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                if (Text(item, "series") is { Length: > 0 } series && Text(item, "text") is { Length: > 0 } text)
                {
                    observations.Add(new MarketAnalysisObservationDto(series, text, Text(item, "evidence") ?? string.Empty));
                }
            }
        }

        return observations;
    }

    private static List<string>? Strings(JsonElement source, string name) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : null;

    private static string? Text(JsonElement source, string name) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? MajorOf(string version) =>
        int.TryParse(version.Split('.')[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major) ? major : null;

    private static string Humanize(string name)
    {
        var text = name.Replace('_', ' ');
        return text.Length == 0 ? name : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
