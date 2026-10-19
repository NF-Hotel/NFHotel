using System.Text.Json;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>One rate that the model gave, with its own words.</summary>
/// <param name="Rate">The rate between 0 and 1, or <c>null</c> when the model gave none.</param>
/// <param name="Interpretation">What the model says about the rate.</param>
public sealed record HotelRate(double? Rate, string Interpretation);

/// <summary>The parts of the answer of a model to the hotel cancellation analysis, read for display.</summary>
/// <param name="Summary">The summary.</param>
/// <param name="Overall">The overall cancellation rate.</param>
/// <param name="Direct">The rate of the direct customers.</param>
/// <param name="Agent">The rate of the agent bookings.</param>
/// <param name="KeyFindings">The key findings.</param>
/// <param name="Limitations">The limitations the model names.</param>
public sealed record HotelAnswer(
    string Summary,
    HotelRate Overall,
    HotelRate Direct,
    HotelRate Agent,
    IReadOnlyList<string> KeyFindings,
    IReadOnlyList<string> Limitations)
{
    /// <summary>
    /// Reads the answer text of a request. Whether the answer is valid is decided by the benchmark service and stored;
    /// this only picks the parts out for display, and answers <c>null</c> when the text is not a JSON object.
    /// </summary>
    /// <param name="output">The answer text, possibly inside a code fence.</param>
    /// <returns>The parts, or <c>null</c> when the text cannot be read.</returns>
    public static HotelAnswer? TryParse(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(StripFence(output));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new HotelAnswer(
                Text(root, "summary"),
                Rate(root, "overall_cancellation"),
                Rate(root, "direct_customers"),
                Rate(root, "agent_bookings"),
                List(root, "key_findings"),
                List(root, "limitations"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string StripFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLine = trimmed.IndexOf('\n', StringComparison.Ordinal);
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLine >= 0 && lastFence > firstLine ? trimmed[(firstLine + 1)..lastFence].Trim() : trimmed;
    }

    private static string Text(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static HotelRate Rate(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return new HotelRate(null, string.Empty);
        }

        double? rate = value.TryGetProperty("rate", out var number) && number.ValueKind == JsonValueKind.Number ? number.GetDouble() : null;

        return new HotelRate(rate, Text(value, "interpretation"));
    }

    private static string[] List(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString() ?? string.Empty)]
            : [];
}
