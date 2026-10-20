using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Web.Features.Admin.Benchmark;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Tests;

/// <summary>The chart and the answer card of the analysis view, rendered to HTML without a browser.</summary>
public sealed class AnalysisComponentTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region Chart

    /// <summary>Positive: the chart draws one rectangle per measured bar, names the groups and carries a title and a hidden table.</summary>
    [Fact]
    public async Task GroupedBarChart_TwoModels_DrawsTheBarsTheLabelsAndTheTable()
    {
        // Arrange and act
        var html = await RenderAsync<GroupedBarChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Total time (s)",
            ["AxisTitle"] = "seconds, lower is faster",
            ["SeriesNames"] = new[] { "cold", "warm" },
            ["Groups"] = new[]
            {
                new ChartGroup("m1", [new ChartBar("cold", 10), new ChartBar("warm", 5)]),
                new ChartGroup("m2", [new ChartBar("cold", 2), new ChartBar("warm", null)]),
            },
        });

        // Assert
        Assert.Equal(3, Count(html, "<rect "));
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("<title id=\"chart-title-", html, StringComparison.Ordinal);
        Assert.Contains(">m1</text>", html, StringComparison.Ordinal);
        Assert.Contains(">n/a</text>", html, StringComparison.Ordinal);
        Assert.Contains("<caption>Total time (s)</caption>", html, StringComparison.Ordinal);
        Assert.Contains("chart-series-1", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a label with markup is encoded and cannot inject an element.</summary>
    [Fact]
    public async Task GroupedBarChart_LabelWithMarkup_IsEncoded()
    {
        // Arrange and act
        var html = await RenderAsync<GroupedBarChart>(new Dictionary<string, object?>
        {
            ["Title"] = "T",
            ["AxisTitle"] = "A",
            ["SeriesNames"] = new[] { "cold" },
            ["Groups"] = new[] { new ChartGroup("<script>alert(1)</script>", [new ChartBar("cold", 1)]) },
        });

        // Assert
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: no group draws nothing at all.</summary>
    [Fact]
    public async Task GroupedBarChart_NoGroups_RendersNothing()
    {
        // Arrange and act
        var html = await RenderAsync<GroupedBarChart>(new Dictionary<string, object?>
        {
            ["Title"] = "T",
            ["AxisTitle"] = "A",
            ["SeriesNames"] = new[] { "cold" },
            ["Groups"] = Array.Empty<ChartGroup>(),
        });

        // Assert
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
    }

    #endregion

    #region Answer card

    /// <summary>Positive: a valid answer shows its summary, the rates of the model with n/a for the calculated ones, and the findings.</summary>
    [Fact]
    public async Task AnswerCard_ValidAnswer_ShowsSummaryRatesAndFindings()
    {
        // Arrange
        const string answer = """{"summary":"S","overall_cancellation":{"rate":0.37,"interpretation":"I1"},"direct_customers":{"rate":0.3,"interpretation":"I2"},"agent_bookings":{"rate":0.4,"interpretation":"I3"},"key_findings":["F1"],"limitations":["L1"]}""";

        // Act
        var html = await RenderAsync<AnswerCard>(new Dictionary<string, object?>
        {
            ["Result"] = Result(valid: true),
            ["Output"] = new BenchmarkOutputDto(1, "m1", answer, null, null),
        });

        // Assert
        Assert.Contains("37.0%", html, StringComparison.Ordinal);
        Assert.Contains("n/a", html, StringComparison.Ordinal);
        Assert.Contains("F1", html, StringComparison.Ordinal);
        Assert.Contains("L1", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Invalid answer", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: an invalid answer shows why it was refused and the raw text, and no parts.</summary>
    [Fact]
    public async Task AnswerCard_InvalidAnswer_ShowsTheReasonAndTheRawText()
    {
        // Arrange and act
        var html = await RenderAsync<AnswerCard>(new Dictionary<string, object?>
        {
            ["Result"] = Result(valid: false),
            ["Output"] = new BenchmarkOutputDto(1, "m1", "```{\"summary\": \"x\"}```", "overall_cancellation.rate must be a number", null),
        });

        // Assert
        Assert.Contains("Invalid answer: overall_cancellation.rate must be a number", html, StringComparison.Ordinal);
        Assert.Contains("Raw answer", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Key findings", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: a raw answer longer than the limit is cut and marked.</summary>
    [Fact]
    public async Task AnswerCard_VeryLongRawAnswer_IsCut()
    {
        // Arrange and act
        var html = await RenderAsync<AnswerCard>(new Dictionary<string, object?>
        {
            ["Result"] = Result(valid: false),
            ["Output"] = new BenchmarkOutputDto(1, "m1", new string('x', 7000), "bad", null),
        });

        // Assert
        Assert.Contains(new string('x', 6000), html, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 6001), html, StringComparison.Ordinal);
    }

    #endregion

    #region Whole view

    /// <summary>Positive: the whole view of a run renders its sections, tables, charts and downloads without a browser.</summary>
    [Fact]
    public async Task RunAnalysis_RunWithBothWorkloads_RendersEverySection()
    {
        // Arrange
        var results = new[]
        {
            new BenchmarkResultDto(1, At, "m1", "hotel_cancellation", "hotel-cancellation", 0, true, 1, 20, 100, 5, false, 2000, null, null, true, false, 1000, 1100, null, null, "stop"),
            new BenchmarkResultDto(2, At, "m1", "deterministic_task", "math-1", 0, false, 0.5, 1, 3, 20, false, 2100, null, true, null, false, 1000, 1100, null, null, "stop"),
        };
        var header = new BenchmarkRunListItemDto(Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001"), At, At.AddMinutes(1), "d.csv", "CPU", 16384, null, "ollama", "0.5", ["m1"], 2);
        var run = new BenchmarkRunDetailsDto(header, new string('a', 64), 10, "hotel-v1", 8, "Linux", results);
        var viewModel = new BenchmarkAnalysisViewModel(NSubstitute.Substitute.For<NFHotel.Web.Common.IUseCaseRunner<NFHotel.Application.UseCases.Benchmark.IBenchmarkService>>())
        {
            Workload = BenchmarkWorkload.Both,
        };

        // Act
        var html = await RenderAsync<BenchmarkRunAnalysis>(new Dictionary<string, object?> { ["Run"] = run }, viewModel);

        // Assert
        Assert.Contains("Mean per model and phase", html, StringComparison.Ordinal);
        Assert.Contains("Deterministic task outcomes", html, StringComparison.Ordinal);
        Assert.Contains("pass (1/1)", html, StringComparison.Ordinal);
        Assert.Contains("LLM analysis comparison", html, StringComparison.Ordinal);
        Assert.Contains("System memory (MB)", html, StringComparison.Ordinal);
        Assert.Contains("GPU memory was not measured", html, StringComparison.Ordinal);
        Assert.Contains("download=\"benchmark-results-5b3f0e1a-0000-4000-8000-000000000001.csv\"", html, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static BenchmarkResultDto Result(bool valid) =>
        new(1, At, "m1", "hotel_cancellation", "hotel-cancellation", 0, true, 1, 2, 10, 5, false, null, null, null, valid, false);

    private static int Count(string text, string part) => text.Split(part).Length - 1;

    private static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters, BenchmarkAnalysisViewModel? viewModel = null)
        where TComponent : IComponent
    {
        var collection = new ServiceCollection().AddLogging();
        if (viewModel is not null)
        {
            collection.AddSingleton(viewModel);
        }

        await using var services = collection.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });
    }

    #endregion
}
