namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The state of the download of a model into Ollama, as the benchmark service reports it (ADR-0005 of that service).</summary>
/// <param name="PullId">The id of the download.</param>
/// <param name="Model">The model name that was asked for.</param>
/// <param name="State">One of the values of <see cref="ModelPullStates"/>.</param>
/// <param name="Percent">The share of the bytes that is downloaded, or <c>null</c> before the size is known.</param>
/// <param name="Error">The reason Ollama gave when the download failed, or <c>null</c>.</param>
public sealed record ModelPullDto(string PullId, string Model, string State, int? Percent, string? Error)
{
    /// <summary>True when the download will not change state any more.</summary>
    public bool IsFinished => State is ModelPullStates.Done or ModelPullStates.Failed;
}
