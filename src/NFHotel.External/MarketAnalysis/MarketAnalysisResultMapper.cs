using System.Globalization;
using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.External.MarketAnalysis;

/// <summary>
/// Reads the result document of the market analysis service (result schema 1.0, 1.1 and 1.2, <c>RunResult</c> of
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
            new MarketAnalysisContentDto(
                Figures(document),
                sections,
                Overall(document),
                document.TryGetProperty("forecast", out JsonElement forecast) ? Forecast(forecast) : null));
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
            findings.ValueKind == JsonValueKind.Undefined ? null : FindingsSeries.ExtractMatrices(findings),
            name == "room_value" ? RoomRevenue(findings) : null);
    }

    // Room revenue sits in the findings of the room value analysis (result schema 1.2); amounts are decimal numbers as text.
    private static MarketAnalysisRoomRevenueDto? RoomRevenue(JsonElement findings)
    {
        if (findings.ValueKind != JsonValueKind.Object
            || !findings.TryGetProperty("room_revenue", out JsonElement revenue)
            || revenue.ValueKind != JsonValueKind.Object
            || Text(revenue, "status") != "available"
            || Decimal(revenue, "total") is not { } total)
        {
            return null;
        }

        List<MarketAnalysisPointDto> monthly = [];
        if (revenue.TryGetProperty("monthly", out JsonElement months) && months.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement month in months.EnumerateArray())
            {
                if (Text(month, "group") is { Length: > 0 } group && Decimal(month, "value") is { } value)
                {
                    monthly.Add(new MarketAnalysisPointDto(group, value));
                }
            }
        }

        return new MarketAnalysisRoomRevenueDto(
            Text(revenue, "unit") ?? string.Empty,
            Text(revenue, "limitation") ?? string.Empty,
            total,
            Decimal(revenue, "mean"),
            Integer(revenue, "records_with_price"),
            Integer(revenue, "records_without_price"),
            monthly);
    }

    // The overall interpretation of insights.overall (result schema 1.2); without a status of available and a text there is nothing to show.
    private static MarketAnalysisOverallDto? Overall(JsonElement document)
    {
        if (!document.TryGetProperty("insights", out JsonElement insights)
            || insights.ValueKind != JsonValueKind.Object
            || !insights.TryGetProperty("overall", out JsonElement overall)
            || overall.ValueKind != JsonValueKind.Object
            || Text(overall, "status") != "available"
            || Text(overall, "text") is not { Length: > 0 } text)
        {
            return null;
        }

        return new MarketAnalysisOverallDto(
            Text(overall, "source") ?? string.Empty,
            Text(overall, "label") ?? string.Empty,
            Text(overall, "generated_at"),
            text,
            Text(overall, "evidence"),
            Suggestions(overall, "recommendations"));
    }

    // The forecast (result schema 1.2): the months of each measure with the band; a measure without months is left out.
    private static MarketAnalysisForecastDto? Forecast(JsonElement forecast)
    {
        if (forecast.ValueKind != JsonValueKind.Object
            || !forecast.TryGetProperty("measures", out JsonElement measures)
            || measures.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        List<MarketAnalysisForecastMeasureDto> list = [];
        foreach (JsonProperty measure in measures.EnumerateObject())
        {
            List<MarketAnalysisForecastMonthDto> months = [];
            if (measure.Value.ValueKind == JsonValueKind.Object
                && measure.Value.TryGetProperty("months", out JsonElement found)
                && found.ValueKind == JsonValueKind.Array)
            {
                months.AddRange(found.EnumerateArray().Select(ForecastMonth).OfType<MarketAnalysisForecastMonthDto>());
            }

            if (months.Count > 0)
            {
                list.Add(new MarketAnalysisForecastMeasureDto(measure.Name, Text(measure.Value, "unit") ?? string.Empty, months));
            }
        }

        return new MarketAnalysisForecastDto(
            Text(forecast, "formula") ?? string.Empty,
            Strings(forecast, "limitations") ?? [],
            Integer(forecast, "history_months"),
            Integer(forecast, "minimum_history_months"),
            forecast.TryGetProperty("history_too_short", out JsonElement tooShort) && tooShort.ValueKind == JsonValueKind.True,
            Integer(forecast, "horizon_months"),
            list);
    }

    private static int Integer(JsonElement source, string name) =>
        source.ValueKind == JsonValueKind.Object && source.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
            ? number
            : 0;

    // An amount is a decimal number as text; a plain JSON number is read too. Anything else is no amount.
    private static double? Decimal(JsonElement source, string name)
    {
        if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && double.IsFinite(parsed) => parsed,
            JsonValueKind.Number => value.GetDouble(),
            _ => null,
        };
    }

    private static MarketAnalysisInsightDto Insight(JsonElement insight) =>
        new(
            Text(insight, "status") ?? "unavailable",
            Text(insight, "reason"),
            Text(insight, "executive_summary"),
            Suggestions(insight, "improvement_suggestions"),
            Observations(insight));

    private static List<MarketAnalysisSuggestionDto> Suggestions(JsonElement source, string listName)
    {
        List<MarketAnalysisSuggestionDto> suggestions = [];
        if (source.TryGetProperty(listName, out JsonElement list) && list.ValueKind == JsonValueKind.Array)
        {
            suggestions.AddRange(list.EnumerateArray().Select(Suggestion).OfType<MarketAnalysisSuggestionDto>());
        }

        return suggestions;
    }

    private static MarketAnalysisSuggestionDto? Suggestion(JsonElement item) =>
        Text(item, "suggestion") is { } suggestion
            ? new MarketAnalysisSuggestionDto(suggestion, Text(item, "evidence") ?? string.Empty, Integer(item, "sample_size"), Text(item, "series"))
            : null;

    private static MarketAnalysisForecastMonthDto? ForecastMonth(JsonElement month) =>
        Text(month, "group") is { Length: > 0 } group && Decimal(month, "value") is { } value
            ? new MarketAnalysisForecastMonthDto(group, value, Decimal(month, "low"), Decimal(month, "high"))
            : null;

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
