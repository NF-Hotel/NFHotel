namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The state of a market analysis run as the data analysis service reports it.</summary>
/// <param name="RunId">The id of the run.</param>
/// <param name="State">One of the values of <see cref="MarketAnalysisRunStates"/>.</param>
/// <param name="StartedAt">When the run started, or <c>null</c> while it is queued.</param>
/// <param name="FinishedAt">When the run ended, or <c>null</c>.</param>
/// <param name="Error">Why the run failed, or <c>null</c>.</param>
public sealed record MarketAnalysisRunStateDto(
    Guid RunId,
    string State,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Error)
{
    /// <summary>True when the run will not change state any more.</summary>
    public bool IsFinished => State is MarketAnalysisRunStates.Completed or MarketAnalysisRunStates.Failed;
}
