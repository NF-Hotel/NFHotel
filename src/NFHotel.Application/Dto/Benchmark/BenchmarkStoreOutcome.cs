namespace NFHotel.Application.Dto.Benchmark;

/// <summary>What storing a delivered run came to (ADR-0013).</summary>
public enum BenchmarkStoreOutcome
{
    /// <summary>The run id was new and the run is now stored.</summary>
    Created,

    /// <summary>The run id was already stored; nothing changed.</summary>
    AlreadyStored,
}
