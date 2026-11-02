using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.MarketAnalysis;

namespace NFHotel.Web.Tests;

/// <summary>
/// The overall interpretation, the room revenue and the forecast of the market analysis report, through the view model and the
/// components, with a trimmed result of schema 1.2 (MIL-026 tasks 24 and 25). A 1.1 result must show none of them.
/// </summary>
public sealed class MarketAnalysisReportSectionsTests
{
    #region View model

    /// <summary>Positive: a 1.2 report gives the view model the overall interpretation, the forecast with a chart for each measure and the room revenue.</summary>
    [Fact]
    public async Task ViewModel_Version12_ExposesOverallForecastAndRoomRevenue()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = await LoadedAsync(MarketSample.Content12());

        // Act
        IReadOnlyList<MarketAnalysisForecastChart> charts = viewModel.ForecastCharts;

        // Assert
        Assert.NotNull(viewModel.Overall);
        Assert.NotNull(viewModel.Forecast);
        Assert.Equal(["arrivals", "occupancy", "room_revenue"], charts.Select(chart => chart.Id));
        Assert.Equal("Forecast of room revenue", charts[2].Title);
        Assert.Equal(["2027-01", "2027-02", "2027-03"], charts[0].Points.Select(point => point.Label));
        Assert.Equal(888.61, charts[0].Low[0]);
        Assert.Equal(1108.64, charts[0].High[0]);
        Assert.Contains("not by an AI", charts[0].Caption, StringComparison.Ordinal);
        Assert.Contains("102 months of history", charts[0].Caption, StringComparison.Ordinal);
        Assert.NotNull(viewModel.Sections.Single().Section.RoomRevenue);
        Assert.DoesNotContain(viewModel.Sections.Single().Charts, chart => chart.Series.Id == MarketAnalysisViewModel.RoomRevenueSeriesId);
    }

    /// <summary>Negative: a 1.1 report has no overall interpretation, no forecast and no room revenue.</summary>
    [Fact]
    public async Task ViewModel_Version11_HasNoneOfThem()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = MarketAnalysisViewModelTests.Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.Overall);
        Assert.Null(viewModel.Forecast);
        Assert.Empty(viewModel.ForecastCharts);
        Assert.All(viewModel.Sections, view => Assert.Null(view.Section.RoomRevenue));
    }

    /// <summary>Boundary: a month without a band end gives a chart that says the band is not drawn.</summary>
    [Fact]
    public async Task ViewModel_ForecastMonthWithoutBand_SaysTheBandIsNotDrawn()
    {
        // Arrange
        MarketAnalysisContentDto content = MarketSample.Content12();
        MarketAnalysisForecastMeasureDto measure = content.Forecast!.Measures[0];
        MarketAnalysisForecastMeasureDto open = measure with { Months = [measure.Months[0] with { Low = null }, .. measure.Months.Skip(1)] };
        MarketAnalysisViewModel viewModel = await LoadedAsync(content with { Forecast = content.Forecast with { Measures = [open] } });

        // Assert
        Assert.Contains("band could not be calculated", Assert.Single(viewModel.ForecastCharts).Caption, StringComparison.Ordinal);
    }

    #endregion

    #region Contents

    /// <summary>Positive: the contents list links to the overall interpretation and to the forecast when the report has them.</summary>
    [Fact]
    public async Task Contents_WithOverallAndForecast_LinksToBoth()
    {
        // Act
        string html = await RenderAsync<MarketAnalysisContents>(new Dictionary<string, object?>
        {
            [nameof(MarketAnalysisContents.HasFigures)] = true,
            [nameof(MarketAnalysisContents.HasOverall)] = true,
            [nameof(MarketAnalysisContents.HasForecast)] = true,
            [nameof(MarketAnalysisContents.Sections)] = new List<MarketAnalysisSectionView>(),
        });

        // Assert
        Assert.Contains("href=\"/admin/market-analysis#overall-title\">Overall interpretation</a>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/market-analysis#forecast-title\">Forecast</a>", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf("#figures-title", StringComparison.Ordinal) < html.IndexOf("#overall-title", StringComparison.Ordinal));
    }

    /// <summary>Negative: without them the contents list has neither link.</summary>
    [Fact]
    public async Task Contents_WithoutOverallAndForecast_HasNeitherLink()
    {
        // Act
        string html = await RenderAsync<MarketAnalysisContents>(new Dictionary<string, object?>
        {
            [nameof(MarketAnalysisContents.HasFigures)] = true,
            [nameof(MarketAnalysisContents.Sections)] = new List<MarketAnalysisSectionView>(),
        });

        // Assert
        Assert.DoesNotContain("overall-title", html, StringComparison.Ordinal);
        Assert.DoesNotContain("forecast-title", html, StringComparison.Ordinal);
    }

    #endregion

    #region Interpretation

    /// <summary>Positive: an AI text is drawn as Markdown with the AI label, its evidence and its recommendations.</summary>
    [Fact]
    public async Task Interpretation_AiText_ShowsMarkdownTheAiLabelAndTheRecommendations()
    {
        // Arrange
        MarketAnalysisOverallDto overall = MarketSample.Content12().Overall!;

        // Act
        string html = await RenderAsync<MarketAnalysisInterpretation>(Overall(overall));

        // Assert
        Assert.Contains("id=\"overall-title\"", html, StringComparison.Ordinal);
        Assert.Contains("AI-generated", html, StringComparison.Ordinal);
        Assert.DoesNotContain("not by an AI", html, StringComparison.Ordinal);
        Assert.Contains("<strong>same seasonal pattern</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<li>Arrivals peak in the summer.</li>", html, StringComparison.Ordinal);
        Assert.Contains("Evidence: 50769820.9, 447.1103, 113551", html, StringComparison.Ordinal);
        Assert.Contains("Recommendations", html, StringComparison.Ordinal);
        Assert.Contains("Check the <strong>summer</strong> prices", html, StringComparison.Ordinal);
        Assert.Contains("Compare the cancellations of the peak months.", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a text that the service calculated says that it is not by an AI and lists no recommendations.</summary>
    [Fact]
    public async Task Interpretation_FiguresFallback_SaysItIsNotByAnAi()
    {
        // Arrange
        MarketAnalysisOverallDto overall = new("figures", "Calculated", null, "The **largest** finding of each analysis.", null, []);

        // Act
        string html = await RenderAsync<MarketAnalysisInterpretation>(Overall(overall));

        // Assert
        Assert.Contains("Calculated from the figures, not by an AI", html, StringComparison.Ordinal);
        Assert.DoesNotContain("AI-generated", html, StringComparison.Ordinal);
        Assert.Contains("<strong>largest</strong>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Recommendations", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: an AI text with a script shows it as text.</summary>
    [Fact]
    public async Task Interpretation_ScriptInText_IsNotRun()
    {
        // Arrange
        MarketAnalysisOverallDto overall = new("ai", "AI-generated", null, "Hello <script>alert(1)</script>", null, []);

        // Act
        string html = await RenderAsync<MarketAnalysisInterpretation>(Overall(overall));

        // Assert
        Assert.DoesNotContain("<script", html, StringComparison.Ordinal);
    }

    #endregion

    #region Room revenue

    /// <summary>Positive: the room revenue has tiles for the total, the mean and the bookings left out, a column for each month and the caption about taxes and currency.</summary>
    [Fact]
    public async Task RoomRevenue_Version12_ShowsTilesColumnsAndCaption()
    {
        // Arrange
        MarketAnalysisSectionDto section = MarketSample.Content12().Sections.Single();
        MarketAnalysisRoomRevenueDto revenue = section.RoomRevenue!;

        // Act
        string html = await RenderAsync<RoomRevenuePanel>(RevenueParameters(revenue, section));

        // Assert
        Assert.Contains("Total room revenue", html, StringComparison.Ordinal);
        Assert.Contains("50,769,820.9", html, StringComparison.Ordinal);
        Assert.Contains("Mean per booking", html, StringComparison.Ordinal);
        Assert.Contains("447.11", html, StringComparison.Ordinal);
        Assert.Contains("Bookings left out", html, StringComparison.Ordinal);
        Assert.Equal(4, Count(html, "class=\"chart-bar"));
        Assert.Contains("before taxes, fees, discounts and extras", html, StringComparison.Ordinal);
        Assert.Contains("currency is not known", html, StringComparison.Ordinal);
        Assert.Contains("chart-ai-label\">AI-generated<", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: with no mean and no month the tile of the mean and the chart are left out.</summary>
    [Fact]
    public async Task RoomRevenue_NoMeanNoMonths_LeavesOutTileAndChart()
    {
        // Arrange
        MarketAnalysisRoomRevenueDto revenue = new("price units", "Before taxes.", 0, null, 0, 5, []);
        MarketAnalysisSectionDto section = MarketSample.Content12().Sections.Single();

        // Act
        string html = await RenderAsync<RoomRevenuePanel>(RevenueParameters(revenue, section));

        // Assert
        Assert.DoesNotContain("Mean per booking", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", html, StringComparison.Ordinal);
        Assert.Contains("Bookings left out", html, StringComparison.Ordinal);
    }

    #endregion

    #region Forecast

    /// <summary>Positive: the forecast has a line chart with a band for each measure, the months of history and the note that the service calculated it.</summary>
    [Fact]
    public async Task Forecast_Version12_ShowsLinesBandsAndHistory()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = await LoadedAsync(MarketSample.Content12());

        // Act
        string html = await RenderAsync<MarketAnalysisForecast>(Forecast(viewModel));

        // Assert
        Assert.Contains("id=\"forecast-title\"", html, StringComparison.Ordinal);
        Assert.Equal(3, Count(html, "class=\"chart-line\""));
        Assert.Equal(3, Count(html, "class=\"chart-band\""));
        Assert.Contains("102 complete months of history", html, StringComparison.Ordinal);
        Assert.Contains("Calculated by the service, not by an AI", html, StringComparison.Ordinal);
        Assert.Contains("Forecast of arrivals", html, StringComparison.Ordinal);
        Assert.Contains("Shaded area", html, StringComparison.Ordinal);
        Assert.Contains("What it cannot say.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Short history", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a history that is too short gets a note, and a month without a band end draws no band.</summary>
    [Fact]
    public async Task Forecast_HistoryTooShortAndOpenBand_SaysSoAndDrawsNoBand()
    {
        // Arrange
        MarketAnalysisContentDto content = MarketSample.Content12();
        MarketAnalysisForecastMeasureDto measure = content.Forecast!.Measures[0];
        MarketAnalysisForecastMeasureDto open = measure with { Months = [measure.Months[0] with { High = null }, .. measure.Months.Skip(1)] };
        MarketAnalysisViewModel viewModel = await LoadedAsync(content with
        {
            Forecast = content.Forecast with { HistoryTooShort = true, HistoryMonths = 10, Measures = [open] },
        });

        // Act
        string html = await RenderAsync<MarketAnalysisForecast>(Forecast(viewModel));

        // Assert
        Assert.Contains("Short history", html, StringComparison.Ordinal);
        Assert.Contains("fewer than the 24 months", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "class=\"chart-line\""));
        Assert.Equal(0, Count(html, "class=\"chart-band\""));
    }

    #endregion

    private static Dictionary<string, object?> RevenueParameters(MarketAnalysisRoomRevenueDto revenue, MarketAnalysisSectionDto section) =>
        new()
        {
            [nameof(RoomRevenuePanel.Revenue)] = revenue,
            [nameof(RoomRevenuePanel.Section)] = section,
        };

    private static Dictionary<string, object?> Overall(MarketAnalysisOverallDto overall) =>
        new() { [nameof(MarketAnalysisInterpretation.Overall)] = overall };

    private static Dictionary<string, object?> Forecast(MarketAnalysisViewModel viewModel) =>
        new()
        {
            [nameof(MarketAnalysisForecast.Forecast)] = viewModel.Forecast,
            [nameof(MarketAnalysisForecast.Charts)] = viewModel.ForecastCharts,
        };

    private static async Task<MarketAnalysisViewModel> LoadedAsync(MarketAnalysisContentDto content)
    {
        MarketAnalysisReportDto report = new(Guid.NewGuid(), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null, null, content);
        MarketAnalysisViewModel viewModel = MarketAnalysisViewModelTests.Build(out _, report);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        return viewModel;
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;

    private static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });
    }
}
