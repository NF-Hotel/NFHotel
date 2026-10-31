using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Tests;

/// <summary>The charts, the heat maps and the captions of the market analysis report, rendered to HTML without a browser (MIL-026 tasks 18, 20 and 21).</summary>
public sealed class MarketChartComponentTests
{
    #region Donut

    /// <summary>Positive: a donut has one slice for each part, the values in a legend next to the colours, a title and a hidden table.</summary>
    [Fact]
    public async Task DonutChart_TwoParts_DrawsTheSlicesTheLegendAndTheTable()
    {
        // Act
        string html = await RenderAsync<DonutChart>(Points("Cancelled", 46, "Not cancelled", 54));

        // Assert
        Assert.Equal(2, Count(html, "<path "));
        Assert.Contains("Cancelled: <strong>46%</strong>", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("<caption>Split</caption>", html, StringComparison.Ordinal);
        Assert.Contains(">54%</text>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: a donut without points says there is no data and draws nothing.</summary>
    [Fact]
    public async Task DonutChart_NoPoint_SaysThereIsNoData()
    {
        // Act
        string html = await RenderAsync<DonutChart>(Points());

        // Assert
        Assert.Contains("no data", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
    }

    #endregion

    #region Ranked bars and columns

    /// <summary>Positive: ranked bars are written largest first whatever the order of the points, with a value at the end of each.</summary>
    [Fact]
    public async Task RankedBarChart_Points_AreLargestFirst()
    {
        // Act
        string html = await RenderAsync<RankedBarChart>(Points("small", 5, "large", 50, "middle", 20));

        // Assert
        Assert.True(html.IndexOf(">large<", StringComparison.Ordinal) < html.IndexOf(">middle<", StringComparison.Ordinal));
        Assert.True(html.IndexOf(">middle<", StringComparison.Ordinal) < html.IndexOf(">small<", StringComparison.Ordinal));
        Assert.Equal(3, Count(html, "<rect "));
        Assert.Contains(">50%</text>", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a label with markup is encoded in every chart and cannot inject an element.</summary>
    [Fact]
    public async Task Charts_LabelWithMarkup_IsEncoded()
    {
        // Arrange
        Dictionary<string, object?> parameters = Points("<script>alert(1)</script>", 5, "b", 50);

        // Act
        string ranked = await RenderAsync<RankedBarChart>(parameters);
        string columns = await RenderAsync<ColumnChart>(parameters);
        string donut = await RenderAsync<DonutChart>(parameters);

        // Assert
        Assert.DoesNotContain("<script>", ranked + columns + donut, StringComparison.Ordinal);
    }

    /// <summary>Positive: columns keep the order they are given, write the value above each and have a hidden table.</summary>
    [Fact]
    public async Task ColumnChart_OrderedBands_KeepTheirOrder()
    {
        // Act
        string html = await RenderAsync<ColumnChart>(Points("0-7", 12, "8-30", 30, "31-90", 33, "91+", 25));

        // Assert
        Assert.Equal(4, Count(html, "class=\"chart-bar"));
        Assert.True(html.IndexOf(">0-7<", StringComparison.Ordinal) < html.IndexOf(">8-30<", StringComparison.Ordinal));
        Assert.True(html.IndexOf(">31-90<", StringComparison.Ordinal) < html.IndexOf(">91+<", StringComparison.Ordinal));
        Assert.Contains(">33%</text>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">31-90</th>", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: one column is drawn, and a value of 0 is a column with no height that still has its label.</summary>
    [Fact]
    public async Task ColumnChart_OneColumnAndZero_AreDrawn()
    {
        // Act
        string one = await RenderAsync<ColumnChart>(Points("only", 7));
        string zero = await RenderAsync<ColumnChart>(Points("none", 0, "some", 4));

        // Assert
        Assert.Equal(1, Count(one, "class=\"chart-bar"));
        Assert.Contains(">0%</text>", zero, StringComparison.Ordinal);
    }

    /// <summary>Positive: a value that rests on few bookings has a star and a visible note, so that the star does not carry the meaning alone.</summary>
    [Fact]
    public async Task ColumnChart_SmallSample_IsMarkedAndExplained()
    {
        // Act
        string html = await RenderAsync<ColumnChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Split",
            ["Unit"] = "%",
            ["Points"] = new MarketAnalysisPointDto[] { new("a", 10, true), new("b", 20) },
        });

        // Assert
        Assert.Contains(">10%*</text>", html, StringComparison.Ordinal);
        Assert.Contains("Rests on few bookings", html, StringComparison.Ordinal);
    }

    #endregion

    #region Line

    /// <summary>Positive: a line of 30 months has a dot for each month and about a third of the labels, so that it stays readable.</summary>
    [Fact]
    public async Task LineChart_ThirtyMonths_ThinsTheLabels()
    {
        // Arrange
        MarketAnalysisPointDto[] months = Enumerable.Range(0, 30).Select(index => new MarketAnalysisPointDto($"{2016 + (index / 12)}-{(index % 12) + 1:00}", 20 + (index % 7))).ToArray();

        // Act
        string html = await RenderAsync<LineChart>(new Dictionary<string, object?> { ["Title"] = "Monthly", ["Unit"] = "%", ["Points"] = months });

        // Assert
        Assert.Equal(30, Count(html, "<circle "));
        Assert.Equal(10, Count(html, "class=\"chart-label\""));
        Assert.Contains("class=\"chart-line\"", html, StringComparison.Ordinal);
        Assert.Equal(30, Count(html, "<th scope=\"row\">20"));
    }

    /// <summary>Positive: the overall level is a dashed line with its own label and a note under the chart.</summary>
    [Fact]
    public async Task LineChart_ReferenceValue_IsADashedLineWithALabel()
    {
        // Act
        string html = await RenderAsync<LineChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Monthly",
            ["Unit"] = "%",
            ["Points"] = new MarketAnalysisPointDto[] { new("2018-07", 89.2), new("2018-08", 35), new("2018-09", 20) },
            ["ReferenceValue"] = 40.4,
            ["ReferenceLabel"] = "Overall",
        });

        // Assert
        Assert.Contains("class=\"chart-reference\"", html, StringComparison.Ordinal);
        Assert.Contains("Overall 40.4%", html, StringComparison.Ordinal);
        Assert.Contains("Dashed line: Overall 40.4%.", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: one point is a dot with its value and no line; no point is a message.</summary>
    [Fact]
    public async Task LineChart_OnePointAndNoPoint_AreHandled()
    {
        // Act
        string one = await RenderAsync<LineChart>(Points("2018-07", 10));
        string none = await RenderAsync<LineChart>(Points());

        // Assert
        Assert.Equal(1, Count(one, "<circle "));
        Assert.DoesNotContain("class=\"chart-line\"", one, StringComparison.Ordinal);
        Assert.Contains(">10%</text>", one, StringComparison.Ordinal);
        Assert.Contains("no data", none, StringComparison.Ordinal);
    }

    #endregion

    #region Heat map of months and tile

    /// <summary>Positive: the months of the real bookings by booking date are rows of years with a value in every cell that has one and a dash in the others.</summary>
    [Fact]
    public async Task HeatmapChart_RealMonths_AreYearsAgainstMonths()
    {
        // Arrange
        MarketAnalysisSeriesDto series = MarketSample.Section("seasonality").Series.Single(found => found.Id == "bookings.monthly.periods");

        // Act
        string html = await RenderAsync<HeatmapChart>(new Dictionary<string, object?> { ["Title"] = series.Title, ["Unit"] = series.Unit, ["Points"] = series.Points });

        // Assert
        Assert.Equal(4 * 12, Count(html, "<rect class=\"chart-cell"));
        Assert.Equal(series.Points.Count, Count(html, "<rect class=\"chart-cell\""));
        Assert.Equal((4 * 12) - series.Points.Count, Count(html, ">–</text>"));
        Assert.Contains(">2016</text>", html, StringComparison.Ordinal);
        Assert.Contains(">Jul</text>", html, StringComparison.Ordinal);
        Assert.Contains("linearGradient", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a tile writes the title, the figure with its unit and what it is the figure of.</summary>
    [Fact]
    public async Task FigureTile_OneFigure_ShowsTheFigure()
    {
        // Act
        string html = await RenderAsync<FigureTile>(new Dictionary<string, object?> { ["Title"] = "Repeated guest", ["Label"] = "not_repeated", ["Value"] = 100.0, ["Unit"] = "%" });

        // Assert
        Assert.Contains("<strong>100%</strong>", html, StringComparison.Ordinal);
        Assert.Contains("Repeated guest", html, StringComparison.Ordinal);
        Assert.Contains("not_repeated", html, StringComparison.Ordinal);
    }

    #endregion

    #region Correlation heat map

    /// <summary>Positive: the real matrix of 12 fields is 144 cells with the value in each (two decimals), a dash where it could not be calculated, a legend and a table.</summary>
    [Fact]
    public async Task CorrelationHeatmap_RealMatrix_DrawsEveryCellWithItsValue()
    {
        // Arrange
        MarketAnalysisMatrixDto matrix = Assert.Single(MarketSample.Section("correlations").Matrices!);
        List<double?> cells = matrix.Cells.SelectMany(row => row).ToList();

        // Act
        string html = await RenderAsync<CorrelationHeatmap>(new Dictionary<string, object?> { ["Matrix"] = matrix });

        // Assert
        Assert.Equal(12 * 12, cells.Count);
        Assert.Equal(cells.Count, Count(html, "<rect class=\"chart-cell"));
        Assert.Equal(cells.Count(cell => cell is null), Count(html, ">–</text>"));
        Assert.Equal(cells.Count(cell => cell is not null), Count(html, "class=\"chart-cell-text\""));
        Assert.Contains(">1.00</text>", html, StringComparison.Ordinal);
        Assert.Contains("#2166ac", html, StringComparison.Ordinal);
        Assert.Contains("#b2182b", html, StringComparison.Ordinal);
        Assert.Contains("linearGradient", html, StringComparison.Ordinal);
        Assert.Contains(">+1</text>", html, StringComparison.Ordinal);
        Assert.Contains("<th scope=\"row\">lead time</th>", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: long field names are shortened on the axis and kept in full in the tooltip and the table.</summary>
    [Fact]
    public async Task CorrelationHeatmap_LongFieldName_IsShortenedOnTheAxis()
    {
        // Arrange
        MarketAnalysisMatrixDto matrix = new MarketAnalysisMatrixDto("matrix", "Correlations", "spearman", ["required_car_parking_spaces", "adr"], [[1.0, -0.5], [-0.5, 1.0]]);

        // Act
        string html = await RenderAsync<CorrelationHeatmap>(new Dictionary<string, object?> { ["Matrix"] = matrix });

        // Assert
        Assert.Contains("required car parkin…", html, StringComparison.Ordinal);
        Assert.Contains("<title>required car parking spaces</title>", html, StringComparison.Ordinal);
        Assert.Contains(">-0.50</text>", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: a matrix without fields says there is no data.</summary>
    [Fact]
    public async Task CorrelationHeatmap_NoField_SaysThereIsNoData()
    {
        // Act
        string html = await RenderAsync<CorrelationHeatmap>(new Dictionary<string, object?> { ["Matrix"] = new MarketAnalysisMatrixDto("matrix", "Correlations", string.Empty, [], []) });

        // Assert
        Assert.Contains("no data", html, StringComparison.Ordinal);
    }

    #endregion

    #region Captions

    /// <summary>Positive: the caption says what the chart shows, why this kind and its limits, with the limitations and the formula of the analysis.</summary>
    [Fact]
    public void ForSeries_Occupancy_TakesTheLimitationsAndTheFormulaOfTheAnalysis()
    {
        // Arrange
        MarketAnalysisSectionDto section = MarketSample.Section("occupancy");
        MarketAnalysisSeriesDto series = Assert.Single(section.Series);

        // Act
        ChartCaptionModel caption = ChartCaptionText.ForSeries(ChartKindSelector.Select(series), series, section);

        // Assert
        Assert.Contains("dashed line", caption.What, StringComparison.Ordinal);
        Assert.Contains("line shows", caption.Why, StringComparison.Ordinal);
        Assert.NotEmpty(section.Limitations!);
        Assert.All(section.Limitations!, limit => Assert.Contains(limit, caption.Limits));
        Assert.Equal(section.Formula, caption.Formula);
    }

    /// <summary>Positive: the observation is matched to the chart by the id of its series, and another chart gets none.</summary>
    [Fact]
    public void ForSeries_ObservationOfTheSeries_IsMatchedById()
    {
        // Arrange
        MarketAnalysisSeriesDto matching = new MarketAnalysisSeriesDto("splits.by_deposit_type.groups", "By deposit type", "%", [new("a", 1), new("b", 2)]);
        MarketAnalysisSeriesDto other = new MarketAnalysisSeriesDto("splits.by_market_segment.groups", "By segment", "%", [new("a", 1), new("b", 2)]);
        MarketAnalysisSectionDto section = Section(new MarketAnalysisInsightDto("available", null, "Summary", [], [new("splits.by_deposit_type.groups", "Non refundable bookings cancel most.", "99.4%")]));

        // Act
        ChartCaptionModel found = ChartCaptionText.ForSeries(ChartKind.Columns, matching, section);
        ChartCaptionModel none = ChartCaptionText.ForSeries(ChartKind.Columns, other, section);

        // Assert
        Assert.Equal("Non refundable bookings cancel most.", found.Observation!.Text);
        Assert.Null(none.Observation);
        Assert.Equal("No AI observation for this chart.", none.NoObservation);
    }

    /// <summary>Negative: a chart without an observation says why when the insight was rejected, timed out or was not asked for.</summary>
    [Theory]
    [InlineData("unavailable", "GUARDRAIL_REJECTED", "No AI observation for this chart: the AI insight of this analysis is not available (GUARDRAIL_REJECTED).")]
    [InlineData("unavailable", "TIMEOUT", "No AI observation for this chart: the AI insight of this analysis is not available (TIMEOUT).")]
    [InlineData("unavailable", null, "No AI observation for this chart: the AI insight of this analysis is not available.")]
    public void ForSeries_InsightNotAvailable_SaysWhy(string status, string? reason, string expected)
    {
        // Arrange
        MarketAnalysisSectionDto section = Section(new MarketAnalysisInsightDto(status, reason, null, []));

        // Act
        ChartCaptionModel caption = ChartCaptionText.ForSeries(ChartKind.Columns, Plain(), section);

        // Assert
        Assert.Equal(expected, caption.NoObservation);
    }

    /// <summary>Negative: an analysis without an insight says that no insight was asked for.</summary>
    [Fact]
    public void ForSeries_NoInsight_SaysNoneWasAsked()
    {
        // Act
        ChartCaptionModel caption = ChartCaptionText.ForSeries(ChartKind.Columns, Plain(), Section(null));

        // Assert
        Assert.Equal("No AI observation for this chart: no AI insight was asked for this analysis.", caption.NoObservation);
    }

    /// <summary>Positive: the caption markup labels an observation as AI-generated and never shows an empty box for a chart without one.</summary>
    [Fact]
    public async Task ChartCaption_WithAndWithoutObservation_LabelsOrSaysNone()
    {
        // Arrange
        MarketAnalysisSectionDto section = Section(new MarketAnalysisInsightDto("available", null, "Summary", [], [new("s", "Text of the AI.", "12%")]));
        ChartCaptionModel with = ChartCaptionText.ForSeries(ChartKind.Columns, Plain(), section);
        ChartCaptionModel without = ChartCaptionText.ForSeries(ChartKind.Columns, Plain() with { Id = "other" }, section);

        // Act
        string withHtml = await RenderAsync<ChartCaption>(new Dictionary<string, object?> { ["Model"] = with });
        string withoutHtml = await RenderAsync<ChartCaption>(new Dictionary<string, object?> { ["Model"] = without });

        // Assert
        Assert.Contains("AI-generated", withHtml, StringComparison.Ordinal);
        Assert.Contains("Text of the AI.", withHtml, StringComparison.Ordinal);
        Assert.Contains("Evidence: 12%", withHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("AI-generated", withoutHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("chart-observation", withoutHtml, StringComparison.Ordinal);
        Assert.Contains("No AI observation for this chart.", withoutHtml, StringComparison.Ordinal);
        Assert.Contains("What it shows.", withoutHtml, StringComparison.Ordinal);
        Assert.Contains("Why this chart.", withoutHtml, StringComparison.Ordinal);
        Assert.Contains("What it cannot say.", withoutHtml, StringComparison.Ordinal);
    }

    /// <summary>Positive: the correlation caption says what the colours mean and that a correlation is not a cause.</summary>
    [Fact]
    public void ForMatrix_Correlations_ExplainsTheScale()
    {
        // Arrange
        MarketAnalysisSectionDto section = MarketSample.Section("correlations");
        MarketAnalysisMatrixDto matrix = Assert.Single(section.Matrices!);

        // Act
        ChartCaptionModel caption = ChartCaptionText.ForMatrix(matrix, section);

        // Assert
        Assert.Contains("Blue is a negative", caption.Why, StringComparison.Ordinal);
        Assert.Contains(caption.Limits, limit => limit.Contains("not a cause", StringComparison.Ordinal));
    }

    #endregion

    #region Dispatcher

    /// <summary>Positive: the occupancy of the sample is drawn as a line with the overall level, and its caption is under it.</summary>
    [Fact]
    public async Task MarketChart_RealOccupancy_IsALineWithTheOverallLevel()
    {
        // Arrange
        MarketAnalysisSectionDto section = MarketSample.Section("occupancy");

        // Act
        string html = await RenderAsync<MarketChart>(new Dictionary<string, object?> { ["Series"] = Assert.Single(section.Series), ["Section"] = section });

        // Assert
        Assert.Contains("class=\"chart-line\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"chart-reference\"", html, StringComparison.Ordinal);
        Assert.Contains("What it shows.", html, StringComparison.Ordinal);
        Assert.Contains("No AI observation for this chart", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: the occupancy on holidays and on other days are two columns and never a donut.</summary>
    [Fact]
    public async Task MarketChart_RealOccupancyOnHolidays_IsTwoColumnsAndNoDonut()
    {
        // Arrange
        MarketAnalysisSectionDto section = MarketSample.Section("occupancy_on_holidays");

        // Act
        string html = await RenderAsync<MarketChart>(new Dictionary<string, object?> { ["Series"] = Assert.Single(section.Series), ["Section"] = section });

        // Assert
        Assert.Equal(2, Count(html, "class=\"chart-bar"));
        Assert.DoesNotContain("chart-slice", html, StringComparison.Ordinal);
        Assert.Contains("never as a donut", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: every series of the real sample is drawn with an accessible SVG or tile and a caption, and no donut is drawn for more than four parts.</summary>
    [Fact]
    public async Task MarketChart_EverySeriesOfTheRealSample_HasATextAlternativeAndACaption()
    {
        foreach (MarketAnalysisSectionDto section in MarketSample.Sections())
        {
            foreach (MarketAnalysisSeriesDto series in section.Series)
            {
                // Act
                string html = await RenderAsync<MarketChart>(new Dictionary<string, object?> { ["Series"] = series, ["Section"] = section });

                // Assert
                ChartKind kind = ChartKindSelector.Select(series);
                Assert.True(
                    kind == ChartKind.Tile || html.Contains("role=\"img\"", StringComparison.Ordinal),
                    $"{section.Id} / {series.Id} has no text alternative");
                Assert.Contains("What it cannot say.", html, StringComparison.Ordinal);
                Assert.True(
                    html.Contains("No AI observation for this chart", StringComparison.Ordinal) || html.Contains("AI-generated", StringComparison.Ordinal),
                    $"{section.Id} / {series.Id} has neither an observation nor the text that says there is none");
                if (html.Contains("chart-slice", StringComparison.Ordinal))
                {
                    Assert.InRange(series.Points.Count, 2, 4);
                }
            }
        }
    }

    /// <summary>Boundary: a series with a negative value keeps the grouped bars of today.</summary>
    [Fact]
    public async Task MarketChart_NegativeValue_KeepsTheGroupedBars()
    {
        // Arrange
        MarketAnalysisSeriesDto series = new MarketAnalysisSeriesDto("s", "Change", "%", [new("a", 1), new("b", -2)]);

        // Act
        string html = await RenderAsync<MarketChart>(new Dictionary<string, object?> { ["Series"] = series, ["Section"] = Section(null) });

        // Assert
        Assert.Contains("chart-legend", html, StringComparison.Ordinal);
        Assert.Contains("plain bar chart", html, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static MarketAnalysisSeriesDto Plain() => new("s", "S", "%", [new("a", 1), new("b", 2)]);

    private static MarketAnalysisSectionDto Section(MarketAnalysisInsightDto? insight) =>
        new("x", "X", "available", null, [], insight);

    private static Dictionary<string, object?> Points(params object[] labelsAndValues)
    {
        List<MarketAnalysisPointDto> points = new List<MarketAnalysisPointDto>();
        for (int index = 0; index < labelsAndValues.Length; index += 2)
        {
            points.Add(new MarketAnalysisPointDto((string)labelsAndValues[index], Convert.ToDouble(labelsAndValues[index + 1], System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new Dictionary<string, object?> { ["Title"] = "Split", ["Unit"] = "%", ["Points"] = points };
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;

    private static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using HtmlRenderer renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });
    }

    #endregion
}
