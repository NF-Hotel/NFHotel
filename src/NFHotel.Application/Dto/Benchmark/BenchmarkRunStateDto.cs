namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The state of a run as the benchmark service reports it, and of the delivery of its result.</summary>
/// <param name="RunId">The id of the run.</param>
/// <param name="State">One of the values of <see cref="BenchmarkRunStates"/>.</param>
/// <param name="Delivery">One of the values of <see cref="BenchmarkDeliveryStates"/>.</param>
/// <param name="Models">The models of the run.</param>
/// <param name="QueuedAt">When the run was queued.</param>
/// <param name="StartedAt">When it started, or <c>null</c>.</param>
/// <param name="FinishedAt">When it ended, or <c>null</c>.</param>
/// <param name="Error">Why the run failed, or <c>null</c>.</param>
public sealed record BenchmarkRunStateDto(
    Guid RunId,
    string State,
    string Delivery,
    IReadOnlyList<string> Models,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Error)
{
    /// <summary>True when the run will not change state any more.</summary>
    public bool IsFinished => State is BenchmarkRunStates.Done or BenchmarkRunStates.Failed;
}