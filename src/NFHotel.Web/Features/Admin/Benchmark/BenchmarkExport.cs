using System.Globalization;
using System.Text;
using System.Text.Json;

using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// The rows of a stored run as CSV and as JSON, for download. The answer texts are not in the export: they are long, and
/// the page reads each of them on its own.
/// </summary>
public static class BenchmarkExport
{
    private static readonly string[] Columns =
    [
        "model", "task", "run", "phase", "first_token_s", "total_s", "completion_tokens", "tokens_per_s", "tokens_estimated",
        "ram_before_mb", "ram_after_mb", "ram_peak_mb", "vram_before_mb", "vram_after_mb", "vram_peak_mb",
        "passed", "response_valid", "error", "finish_reason",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>Writes the requests as CSV with a header row; a text with a comma, a quote or a line break is quoted.</summary>
    /// <param name="results">The requests.</param>
    /// <returns>The CSV text.</returns>
    public static string ToCsv(IEnumerable<BenchmarkResultDto> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var builder = new StringBuilder();
        builder.Append(string.Join(',', Columns)).Append("\r\n");
        foreach (var result in results)
        {
            string[] cells =
            [
                result.Model,
                BenchmarkResultAnalysis.TaskName(result),
                (result.RunNumber + 1).ToString(CultureInfo.InvariantCulture),
                result.Cold ? BenchmarkResultAnalysis.Cold : BenchmarkResultAnalysis.Warm,
                Number(result.TimeToFirstTokenSeconds),
                Number(result.TotalSeconds),
                result.CompletionTokens.ToString(CultureInfo.InvariantCulture),
                Number(result.TokensPerSecond),
                Flag(result.TokensEstimated),
                Number(result.RamBeforeMb),
                Number(result.RamAfterMb),
                Number(result.RamPeakMb),
                Number(result.VramBeforeMb),
                Number(result.VramAfterMb),
                Number(result.VramPeakMb),
                result.Passed is { } passed ? Flag(passed) : string.Empty,
                result.ResponseValid is { } valid ? Flag(valid) : string.Empty,
                Flag(result.HasError),
                result.FinishReason ?? string.Empty,
            ];
            builder.Append(string.Join(',', cells.Select(Quote))).Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>Writes the requests as indented JSON with snake_case names.</summary>
    /// <param name="results">The requests.</param>
    /// <returns>The JSON text.</returns>
    public static string ToJson(IEnumerable<BenchmarkResultDto> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return JsonSerializer.Serialize(results, JsonOptions);
    }

    private static string Number(double? value) =>
        value is { } number ? number.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;

    private static string Flag(bool value) => value ? "true" : "false";

    private static string Quote(string text) =>
        text.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
}
