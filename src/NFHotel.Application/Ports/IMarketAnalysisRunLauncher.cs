namespace NFHotel.Application.Ports;

/// <summary>Starts a market analysis run in the background of the API, so that a start returns at once (MIL-026).</summary>
public interface IMarketAnalysisRunLauncher
{
    /// <summary>Queues the work of a run. The run must already be known to the tracker; failures end the run, they are not thrown.</summary>
    /// <param name="runId">The id of the run.</param>
    void Launch(Guid runId);
}
