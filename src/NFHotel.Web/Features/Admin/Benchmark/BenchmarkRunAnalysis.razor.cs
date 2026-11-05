using System.Globalization;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// The code behind the analysis view of a run: the groups of the charts and the forwarding to <see
/// cref="BenchmarkAnalysisViewModel"/>.
/// </summary>
public partial class BenchmarkRunAnalysis : CancellableComponentBase
{
    private static readonly IReadOnlyList<string> PhaseNames = [BenchmarkResultAnalysis.Cold, BenchmarkResultAnalysis.Warm];
    private static readonly IReadOnlyList<string> MomentNames = ["before", "after", "peak"];

    /// <summary>The run that is shown.</summary>
    [Parameter, EditorRequired]
    public BenchmarkRunDetailsDto Run { get; set; } = default!;

    /// <summary>Starts the state of the view for the run that is shown.</summary>
    protected override void OnParametersSet() => ViewModel.ShowRun(Run.Header.RunId);

    private Task ToggleAsync(int benchmarkResultId) => ViewModel.ToggleCompareAsync(benchmarkResultId, ComponentToken);

    private static IReadOnlyList<ChartGroup> PhaseGroups(IReadOnlyList<PhaseStats> phases, Func<PhaseStats, double?> value) =>
        [
            .. phases
                .GroupBy(stats => stats.Model)
                .Select(group => new ChartGroup(
                    group.Key,
                    [.. PhaseNames.Select(name => new ChartBar(name, group.FirstOrDefault(stats => stats.Phase == name) is { } stats ? value(stats) : null))])),
        ];

    private static IReadOnlyList<ChartGroup> MomentGroups(
        IReadOnlyList<ResourceUsage> resources,
        Func<ResourceUsage, double?> before,
        Func<ResourceUsage, double?> after,
        Func<ResourceUsage, double?> peak) =>
        [
            .. resources.Select(usage => new ChartGroup(
                usage.Model,
                [new ChartBar("before", before(usage)), new ChartBar("after", after(usage)), new ChartBar("peak", peak(usage))])),
        ];

    private static string Number(double? value) =>
        value is { } number ? number.ToString("0.##", CultureInfo.InvariantCulture) : "—";
}
