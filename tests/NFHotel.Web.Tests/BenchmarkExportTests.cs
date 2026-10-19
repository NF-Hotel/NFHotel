using System.Text.Json;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Web.Features.Admin.Benchmark;

namespace NFHotel.Web.Tests;

/// <summary>The rows of a stored run as CSV and JSON for download.</summary>
public sealed class BenchmarkExportTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Positive: the CSV has a header and one row per request with the 1-based run number and invariant numbers.</summary>
    [Fact]
    public void ToCsv_OneRequest_WritesTheHeaderAndTheRow()
    {
        // Arrange
        var result = Result("llama3.2:3b", "hotel_cancellation", "hotel-cancellation", true, 1234.5);

        // Act
        var lines = BenchmarkExport.ToCsv([result]).TrimEnd().Split("\r\n");

        // Assert
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("model,task,run,phase,first_token_s,total_s", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("llama3.2:3b,Hotel cancellation analysis,1,cold,0.5,2,10,5,false,1234.5,", lines[1], StringComparison.Ordinal);
        Assert.EndsWith(",false,stop", lines[1], StringComparison.Ordinal);
    }

    /// <summary>Boundary: a text with a comma, a quote or a line break is quoted and the quote is doubled.</summary>
    [Fact]
    public void ToCsv_ModelWithCommaAndQuote_IsQuoted()
    {
        // Arrange
        var result = Result("a,\"b\"", "deterministic_task", "math-1", false, null);

        // Act
        var row = BenchmarkExport.ToCsv([result]).Split("\r\n")[1];

        // Assert
        Assert.StartsWith("\"a,\"\"b\"\"\",math-1,1,warm,", row, StringComparison.Ordinal);
    }

    /// <summary>Boundary: no request gives only the header.</summary>
    [Fact]
    public void ToCsv_NoRequests_WritesOnlyTheHeader() =>
        Assert.Single(BenchmarkExport.ToCsv([]).TrimEnd().Split("\r\n"));

    /// <summary>Positive: the JSON is an array with snake_case names and no answer text.</summary>
    [Fact]
    public void ToJson_OneRequest_WritesSnakeCaseNamesWithoutTheAnswerText()
    {
        // Arrange
        var result = Result("m1", "hotel_cancellation", "hotel-cancellation", true, 100);

        // Act
        using var document = JsonDocument.Parse(BenchmarkExport.ToJson([result]));

        // Assert
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("m1", item.GetProperty("model").GetString());
        Assert.Equal(10, item.GetProperty("completion_tokens").GetInt32());
        Assert.False(item.TryGetProperty("output", out _));
    }

    private static BenchmarkResultDto Result(string model, string analysisType, string task, bool cold, double? ramBefore) =>
        new(1, At, model, analysisType, task, 0, cold, 0.5, 2, 10, 5, false, 2000, null, null, null, false, ramBefore, null, null, null, "stop");
}
