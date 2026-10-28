namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The states of a run, as the data analysis service names them.</summary>
public static class MarketAnalysisRunStates
{
    /// <summary>Waiting to start.</summary>
    public const string Queued = "queued";

    /// <summary>The notebook is running.</summary>
    public const string Running = "running";

    /// <summary>The run has finished and its result is valid.</summary>
    public const string Completed = "completed";

    /// <summary>The run failed; the state carries a sanitised reason.</summary>
    public const string Failed = "failed";
}
