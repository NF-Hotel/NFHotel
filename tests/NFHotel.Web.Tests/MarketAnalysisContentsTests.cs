using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Web.Features.Admin.MarketAnalysis;

namespace NFHotel.Web.Tests;

/// <summary>The contents of the market analysis page lists its sections in a contents list that links to each one (MIL-026).</summary>
public sealed class MarketAnalysisContentsTests
{
    private const string PagePath = "/admin/market-analysis";

    /// <summary>Positive: the contents list links to the key figures and to every analysis with the full page path.</summary>
    [Fact]
    public async Task Render_ReportStored_ListsAContentsLinkForEachSectionWithThePagePath()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = MarketAnalysisViewModelTests.Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        string html = await RenderAsync(hasFigures: true, viewModel.Sections);

        // Assert
        Assert.Contains("Contents", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{PagePath}#figures-title\"", html, StringComparison.Ordinal);
        foreach (MarketAnalysisSectionView view in viewModel.Sections)
        {
            Assert.Contains($"href=\"{PagePath}#analysis-{view.Section.Id}\"", html, StringComparison.Ordinal);
        }
    }

    /// <summary>Negative: a report without key figures has no link to them.</summary>
    [Fact]
    public async Task Render_NoKeyFigures_HasNoKeyFiguresLink()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = MarketAnalysisViewModelTests.Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        string html = await RenderAsync(hasFigures: false, viewModel.Sections);

        // Assert
        Assert.DoesNotContain("#figures-title", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(bool hasFigures, IReadOnlyList<MarketAnalysisSectionView> sections)
    {
        await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await renderer.RenderComponentAsync<MarketAnalysisContents>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(MarketAnalysisContents.HasFigures)] = hasFigures,
                [nameof(MarketAnalysisContents.Sections)] = sections,
            }));
            return output.ToHtmlString();
        });
    }
}
