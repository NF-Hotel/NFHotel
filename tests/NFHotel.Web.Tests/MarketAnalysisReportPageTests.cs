using Bunit;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.MarketAnalysis;
using NFHotel.Web.Security;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The market analysis report page, rendered and clicked (MIL-054 task 6).</summary>
public sealed class MarketAnalysisReportPageTests : IAsyncDisposable
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly IMarketAnalysisService _service = Substitute.For<IMarketAnalysisService>();
    private readonly PageHost _host = new();

    public MarketAnalysisReportPageTests()
    {
        _host.With(new MarketAnalysisViewModel(new PassThroughRunner<IMarketAnalysisService>(_service), new FakeClock { Today = Today }));
        _service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisReportDto>.Success(Report()));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private static MarketAnalysisReportDto Report()
    {
        MarketAnalysisSuggestionDto[] suggestions = [new("Raise the weekend rate", "Weekends are full", 412)];
        MarketAnalysisSectionDto[] extra =
        [
            new("unavailable", "Unavailable analysis", "unavailable", "too few bookings", [], null),
            new("unavailable-silent", "Silent analysis", "unavailable", null, [], null),
            new("summarised", "Summarised analysis", "available", null, [], new MarketAnalysisInsightDto("available", null, "Bookings rise in summer.", suggestions)),
            new("unsummarised", "Unsummarised analysis", "available", null, [], new MarketAnalysisInsightDto("unavailable", "the model timed out", null, [])),
            new("unsummarised-silent", "Quiet analysis", "available", null, [], new MarketAnalysisInsightDto("unavailable", null, null, []))
        ];

        return new MarketAnalysisReportDto(
            RunId,
            Start,
            Start.AddMinutes(4),
            "ollama",
            "phi4-mini",
            new MarketAnalysisContentDto([new MarketAnalysisFigureDto("Bookings", 5000, "bookings")], [.. MarketSample.Sections(), .. extra]));
    }

    private IRenderedComponent<MarketAnalysisReport> Render(params string[] policies)
    {
        _host.SignedInWith(policies);
        return _host.Render<MarketAnalysisReport>();
    }

    /// <summary>Positive: the stored report shows its figures, the charts of the sample and each kind of AI summary.</summary>
    [Fact]
    public void Render_StoredReport_ShowsEveryPart()
    {
        var page = Render();

        foreach (var text in new[]
        {
            "Run of 2026-10-19 10:00:00 UTC", "Analysis by phi4-mini.", "Key figures", "5,000",
            "This analysis could not be made: too few bookings", "This analysis could not be made.",
            "Bookings rise in summer.", "Raise the weekend rate", "412 bookings",
            "No AI summary for this analysis: the model timed out", "No AI summary for this analysis.",
            "This analysis has no chart data."
        })
        {
            Assert.Contains(text, page.Markup, StringComparison.Ordinal);
        }

        Assert.NotEmpty(page.FindAll("svg"));
        Assert.Empty(page.FindAll("#analysis-from"));
    }

    /// <summary>Edge: without a stored run the page says so instead of showing an empty report.</summary>
    [Fact]
    public void Render_NoStoredRun_SaysSo()
    {
        _service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Fail<MarketAnalysisReportDto>(ErrorCodes.MarketAnalysis.NotFound));

        var page = Render();

        Assert.Contains("No market analysis yet", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Equivalence: each state of a started run is shown in its own way, and the chosen period is sent.</summary>
    [Theory]
    [InlineData(MarketAnalysisRunStates.Running, "since 2026-10-19 10:00:00 UTC")]
    [InlineData(MarketAnalysisRunStates.Queued, "queued")]
    [InlineData(MarketAnalysisRunStates.Completed, "The analysis is done")]
    [InlineData(MarketAnalysisRunStates.Failed, "The analysis failed.")]
    public void Start_EachState_IsShown(string state, string expected)
    {
        DateTimeOffset? started = state == MarketAnalysisRunStates.Queued ? null : Start;
        _service.StartRunAsync(Arg.Any<StartMarketAnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<MarketAnalysisRunStateDto>.Success(new MarketAnalysisRunStateDto(RunId, state, started, null, null)));
        var page = Render(StaffPolicies.MarketAnalysisStart);

        page.Button("Last 3 years").Click();
        page.Button("Last year").Click();
        page.Find("#analysis-to").Change("2026-03-01");
        page.Button("Start a new analysis").Click();

        Assert.Contains(expected, page.Markup, StringComparison.Ordinal);
        _service.Received(1).StartRunAsync(
            Arg.Is<StartMarketAnalysisRequest>(request => request.To == new DateOnly(2026, 3, 1)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused start shows its text and the period can be cleared for all bookings.</summary>
    [Fact]
    public void Start_Refused_ShowsTheError()
    {
        _service.StartRunAsync(Arg.Any<StartMarketAnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<MarketAnalysisRunStateDto>(ErrorCodes.MarketAnalysis.NotFound));
        var page = Render(StaffPolicies.MarketAnalysisStart);

        page.Button("All bookings").Click();
        page.Button("Start a new analysis").Click();

        Assert.Contains(Text(ErrorCodes.MarketAnalysis.NotFound), page.Markup, StringComparison.Ordinal);
        _service.Received(1).StartRunAsync(new StartMarketAnalysisRequest(null, null), Arg.Any<CancellationToken>());
    }
}
