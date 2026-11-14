using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>A launcher that only records the run; the test decides when the work runs, so it can look at every state.</summary>
public sealed class RecordingMarketAnalysisLauncher : IMarketAnalysisRunLauncher
{
    public List<Guid> Launched { get; } = [];

    public void Launch(Guid runId) => Launched.Add(runId);
}
