namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The states of a run, as the benchmark service names them.</summary>
public static class BenchmarkRunStates
{
    /// <summary>Waiting for the run before it.</summary>
    public const string Queued = "queued";

    /// <summary>The models are being run.</summary>
    public const string Running = "running";

    /// <summary>The run has finished.</summary>
    public const string Done = "done";

    /// <summary>The run failed; the state carries the reason.</summary>
    public const string Failed = "failed";
}
