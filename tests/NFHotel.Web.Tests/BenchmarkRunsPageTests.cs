using Bunit;

using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Domain.Entities;
using NFHotel.Web.Features.Admin.Benchmark;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The page of the stored benchmark runs, rendered and clicked (MIL-054 task 6).</summary>
public sealed class BenchmarkRunsPageTests : IAsyncDisposable
{
    #region Setup

    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly IBenchmarkService _service = Substitute.For<IBenchmarkService>();
    private readonly IAiDefaultService _defaults = Substitute.For<IAiDefaultService>();
    private readonly PageHost _host = new();

    public BenchmarkRunsPageTests()
    {
        var header = new BenchmarkRunListItemDto(RunId, Start, Start.AddMinutes(5), "d.csv", "Test CPU", 16384, "RTX", "ollama", "0.5.1", ["m1"], 4);
        BenchmarkResultDto[] results =
        [
            Sample(1, passed: true),
            Sample(2, passed: false, cold: true, estimated: true),
            Sample(3, valid: true),
            Sample(4, valid: false),
            Sample(5, error: true),
            Sample(6) with { VramPeakMb = 2048 },
            Sample(7, valid: true) with { AnalysisType = BenchmarkResultAnalysis.HotelAnalysisType, TaskId = null }
        ];
        _service.GetRunsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([header, header with { RunId = Guid.NewGuid(), Gpu = null }]));
        _service.GetRunAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunDetailsDto>.Success(new BenchmarkRunDetailsDto(header, new string('a', 64), 10, "hotel-v1", 8, "Linux", results)));
        _service.GetOutputAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BenchmarkOutputDto>.Success(new BenchmarkOutputDto(call.Arg<int>(), "m1", "{\"answer\":1}", null, null)));
        _service.GetOutputAsync(5, Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkOutputDto>.Success(new BenchmarkOutputDto(5, "m1", "{}", "not json", "timed out")));
        _service.DeleteRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        _defaults.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([new AiDefaultDto(AiDefault.Website, "m1", "manager", Start, RunId)]));
        _defaults.SetAsync(Arg.Any<string>(), Arg.Any<SetAiDefaultRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<AiDefaultDto>.Success(new AiDefaultDto(call.ArgAt<string>(0), call.ArgAt<SetAiDefaultRequest>(1).Model, "manager", Start, null)));

        var started = new[]
        {
            BenchmarkRequestDoubles.Record(Guid.NewGuid(), BenchmarkRunStates.Running),
            BenchmarkRequestDoubles.Record(Guid.NewGuid(), BenchmarkRunStates.Failed, "out of memory")
        };
        var client = Substitute.For<IBenchmarkServiceClient>();
        client.GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BenchmarkRunStateDto>.Success(new BenchmarkRunStateDto(
                call.Arg<Guid>(), BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady, ["m1"], Start, Start, null, null)));
        var tracker = new BenchmarkRunTracker(
            new PassThroughRunner<IBenchmarkServiceClient>(client),
            new PassThroughRunner<IBenchmarkRequestService>(BenchmarkRequestDoubles.Requests(started)),
            new PassThroughRunner<IBenchmarkService>(BenchmarkRequestDoubles.Stored()));
        _host.With(new BenchmarkAnalysisViewModel(new PassThroughRunner<IBenchmarkService>(_service)));
        _host.With(new BenchmarkRunListViewModel(
            new PassThroughRunner<IBenchmarkService>(_service),
            new PassThroughRunner<IAiDefaultService>(_defaults),
            tracker));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private static BenchmarkResultDto Sample(int id, bool? passed = null, bool? valid = null, bool error = false, bool cold = false, bool estimated = false) =>
        new(id, Start, "m1", "hotel", $"task-{id}", 0, cold, 0.5, 2.25, 100, 44.4, estimated, 1024, null, passed, valid, error);

    #endregion

    #region Render 1

    /// <summary>Positive: a manager sees the defaults, the started runs and the stored runs with their machines.</summary>
    [Fact]
    public void Render_Manager_SeesDefaultsStartedAndStoredRuns()
    {
        _host.SignedInWith(StaffPolicies.AiDefaultsManage, StaffPolicies.BenchmarkRun, StaffPolicies.BenchmarkDelete);

        var page = _host.Render<BenchmarkRuns>();

        Assert.Contains("m1, set by manager", page.Markup, StringComparison.Ordinal);
        Assert.Contains("no default set", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Follow", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Start again", page.Markup, StringComparison.Ordinal);
        Assert.Contains("out of memory", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Test CPU, 16 GiB, RTX", page.Markup, StringComparison.Ordinal);
        Assert.Contains("CPU only", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Open

    /// <summary>Positive: a run opens with every outcome, an answer is shown and hidden, and the run closes again.</summary>
    [Fact]
    public void Open_Run_ShowsResultsAndAnswers()
    {
        _host.SignedInWith(StaffPolicies.AiDefaultsManage);
        var page = _host.Render<BenchmarkRuns>();

        page.Button("Open").Click();

        Assert.Contains($"Run {RunId}", page.Markup, StringComparison.Ordinal);
        foreach (var outcome in new[] { "passed", "failed", "valid answer", "invalid answer", "error", "cold", "(est.)", "2.25" })
        {
            Assert.Contains(outcome, page.Markup, StringComparison.Ordinal);
        }

        page.FindAll("button").Where(button => button.TextContent.Trim() == "Show answer").ElementAt(4).Click();
        Assert.Contains("Answer of m1", page.Markup, StringComparison.Ordinal);
        Assert.Contains("timed out", page.Markup, StringComparison.Ordinal);
        Assert.Contains("not json", page.Markup, StringComparison.Ordinal);
        page.Button("Hide the answer").Click();
        Assert.DoesNotContain("Answer of m1", page.Markup, StringComparison.Ordinal);

        page.FindAll("button").First(button => button.TextContent.Trim().StartsWith("Use for", StringComparison.Ordinal)).Click();
        _defaults.Received(1).SetAsync(AiDefault.Website, Arg.Any<SetAiDefaultRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        page.Button("Close").Click();
        Assert.DoesNotContain($"Run {RunId}", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Compare

    /// <summary>Positive: a checked answer is compared in a card, and unchecked again.</summary>
    [Fact]
    public void Compare_CheckedAnswer_ShowsItsCard()
    {
        _host.SignedInWith();
        var page = _host.Render<BenchmarkRuns>();
        page.Button("Open").Click();

        page.FindAll("input[type=checkbox][aria-label^='Compare']")[0].Change(true);
        Assert.Contains("Compare up to", page.Markup, StringComparison.Ordinal);
        Assert.Contains("GPU memory (MB)", page.Markup, StringComparison.Ordinal);

        page.FindAll("input[type=checkbox][aria-label^='Compare']")[0].Change(false);
        Assert.DoesNotContain("Compare up to", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Default

    /// <summary>Positive and edge: a typed default is saved; a name no run used gets a note.</summary>
    [Fact]
    public void Default_TypedName_IsSavedWithANoteWhenUnknown()
    {
        _host.SignedInWith(StaffPolicies.AiDefaultsManage);
        var page = _host.Render<BenchmarkRuns>();

        page.Find($"#default-ai-{AiDefault.MarketAnalysis}").Change("phi3.5:latest");
        Assert.Contains("No stored run used this name", page.Markup, StringComparison.Ordinal);
        page.FindAll("button").Where(button => button.TextContent.Trim() == "Save").Last().Click();

        _defaults.Received(1).SetAsync(
            AiDefault.MarketAnalysis,
            Arg.Is<SetAiDefaultRequest>(request => request.Model == "phi3.5:latest"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Delete

    /// <summary>State transition: deleting a run asks first, can be aborted, and deletes on confirm.</summary>
    [Fact]
    public void Delete_AskThenAbortThenConfirm_DeletesOnce()
    {
        _host.SignedInWith(StaffPolicies.BenchmarkDelete);
        var page = _host.Render<BenchmarkRuns>();

        page.Button("Delete").Click();
        Assert.Contains("Delete the run of 2026-10-19 10:00 UTC?", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        page.Button("Delete").Click();
        page.Button("Delete run").Click();

        _service.Received(1).DeleteRunAsync(RunId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Render 2

    /// <summary>Edge: with no stored run the page says so.</summary>
    [Fact]
    public void Render_NoRuns_SaysSo()
    {
        _service.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([]));
        _host.SignedInWith();

        var page = _host.Render<BenchmarkRuns>();

        Assert.Contains("No benchmark run has been stored yet", page.Markup, StringComparison.Ordinal);
    }

    #endregion
}
