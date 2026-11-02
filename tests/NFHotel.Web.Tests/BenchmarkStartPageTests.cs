using Bunit;

using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Domain.Entities;
using NFHotel.Web.Features.Admin.Benchmark;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The page that starts a benchmark run, with its panel of installed models, rendered and clicked (MIL-054 task 6).</summary>
public sealed class BenchmarkStartPageTests : IAsyncDisposable
{
    #region Setup

    private static readonly DateTimeOffset Queued = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly IBenchmarkServiceClient _client = Substitute.For<IBenchmarkServiceClient>();
    private readonly IAiDefaultService _defaults = Substitute.For<IAiDefaultService>();
    private readonly PageHost _host = new();
    private BenchmarkRequestDto[] _recent = [];

    public BenchmarkStartPageTests()
    {
        _client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["m1", "m2"]));
        _client.ListModelPullsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ModelPullDto>>.Success([]));
        _client.StartModelPullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ModelPullDto>.Success(new ModelPullDto("pull-1", call.ArgAt<string>(0), ModelPullStates.Pulling, 40, null)));
        _client.DeleteModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _client.StartRunAsync(Arg.Any<StartBenchmarkRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunStateDto>.Success(State(Guid.NewGuid(), BenchmarkRunStates.Queued, BenchmarkDeliveryStates.NotReady)));
        RunsAre();
        _defaults.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([new AiDefaultDto(AiDefault.Website, "m1", "manager", Queued, null)]));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private static BenchmarkRunStateDto State(Guid runId, string state, string delivery, string? error = null) =>
        new(runId, state, delivery, ["m1"], Queued, state == BenchmarkRunStates.Queued ? null : Queued.AddMinutes(1), null, error);

    private void RunsAre(params BenchmarkRunStateDto[] runs) =>
        _client.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Success(runs));

    private IRenderedComponent<BenchmarkStart> Render(params string[] policies)
    {
        var client = new PassThroughRunner<IBenchmarkServiceClient>(_client);
        var tracker = new BenchmarkRunTracker(
            client,
            new PassThroughRunner<IBenchmarkRequestService>(BenchmarkRequestDoubles.Requests(_recent)),
            new PassThroughRunner<IBenchmarkService>(BenchmarkRequestDoubles.Stored()));
        _host.With(new BenchmarkStartViewModel(client, tracker))
            .With(new BenchmarkModelsViewModel(client, new PassThroughRunner<IAiDefaultService>(_defaults)))
            .SignedInWith(policies);
        return _host.Render<BenchmarkStart>();
    }

    #endregion

    #region Start

    /// <summary>Positive: models are chosen, the options set and the run started; the known runs show their state.</summary>
    [Fact]
    public void Start_ChosenModels_StartsTheRun()
    {
        RunsAre(
            State(Guid.NewGuid(), BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady),
            State(Guid.NewGuid(), BenchmarkRunStates.Done, BenchmarkDeliveryStates.Delivered),
            State(Guid.NewGuid(), BenchmarkRunStates.Done, BenchmarkDeliveryStates.Pending),
            State(Guid.NewGuid(), BenchmarkRunStates.Done, BenchmarkDeliveryStates.Failed),
            State(Guid.NewGuid(), BenchmarkRunStates.Failed, BenchmarkDeliveryStates.NotReady, "out of memory"),
            State(Guid.NewGuid(), BenchmarkRunStates.Failed, BenchmarkDeliveryStates.NotReady));
        var page = Render();

        Assert.Contains("6 runs, newest first", page.Markup, StringComparison.Ordinal);
        foreach (var text in new[] { "since 2026-10-19 10:01:00 UTC", "stored", "waiting to be stored", "could not be stored", "out of memory", "The run failed.", "not ready" })
        {
            Assert.Contains(text, page.Markup, StringComparison.Ordinal);
        }

        page.FindAll("fieldset input[type=checkbox]")[0].Change(true);
        page.FindAll(".field-row input[type=checkbox]")[1].Change(true);
        page.Find("#benchmark-runs").Change(2);
        page.Button("Start the run").Click();

        _client.Received(1).StartRunAsync(
            Arg.Is<StartBenchmarkRunRequest>(request => request.Models.SequenceEqual(new[] { "m1" }) && request.Runs == 2),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region StoppedRun

    /// <summary>State transition: a run that a restart stopped is shown, and its choice can be taken again.</summary>
    [Fact]
    public void StoppedRun_UseTheSameChoice_TakesItsModels()
    {
        _recent = [BenchmarkRequestDoubles.Record(Guid.NewGuid(), BenchmarkRunStates.Failed, "the service restarted")];
        var page = Render();

        Assert.Contains("The last run did not finish", page.Markup, StringComparison.Ordinal);
        Assert.Contains("the service restarted", page.Markup, StringComparison.Ordinal);
        page.Button("Use the same choice").Click();

        Assert.DoesNotContain("The last run did not finish", page.Markup, StringComparison.Ordinal);
        Assert.All(page.FindAll("fieldset input[type=checkbox]"), box => Assert.True(box.HasAttribute("checked")));
    }

    #endregion

    #region Render

    /// <summary>Edge: no installed model and no known run each say so, and a service that cannot be reached pauses the live list.</summary>
    [Fact]
    public void Render_NoModelsAndServiceDown_SaysSo()
    {
        _client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success([]));
        _client.ListRunsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        var page = Render();

        Assert.Contains("No model is installed", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Live updates are paused", page.Markup, StringComparison.Ordinal);
        Assert.Contains("No run is queued or running", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region ModelsPanel

    /// <summary>Positive: with the right to manage models, a model is added and its download is shown.</summary>
    [Fact]
    public void ModelsPanel_AddModel_StartsTheDownload()
    {
        var page = Render(StaffPolicies.BenchmarkModelsManage);

        page.Find("#new-model-name").Input("phi4-mini");
        page.Button("Add model").Click();

        _client.Received(1).StartModelPullAsync("phi4-mini", Arg.Any<CancellationToken>());
        Assert.Contains("40 %", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>State transition: deleting a model asks first, can be aborted, deletes on confirm and reloads the models of the page.</summary>
    [Fact]
    public void ModelsPanel_Delete_AskThenAbortThenConfirm()
    {
        var page = Render(StaffPolicies.BenchmarkModelsManage);

        page.Button("Delete").Click();
        Assert.Contains("Delete the model m1?", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        Assert.DoesNotContain("Delete the model m1?", page.Markup, StringComparison.Ordinal);
        page.Button("Delete").Click();
        page.Button("Delete model").Click();

        _client.Received(1).DeleteModelAsync("m1", Arg.Any<CancellationToken>());
        Assert.DoesNotContain("Delete the model m1?", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Edge: a panel with no installed model says so.</summary>
    [Fact]
    public void ModelsPanel_NoModels_SaysSo()
    {
        _client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success([]));

        var page = Render(StaffPolicies.BenchmarkModelsManage);

        Assert.Contains("No model is installed.", page.Markup, StringComparison.Ordinal);
    }

    #endregion
}
