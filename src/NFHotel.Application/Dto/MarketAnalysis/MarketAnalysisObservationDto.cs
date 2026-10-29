namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// What the AI sees in one chart of an analysis, written in the same request as the insight of that analysis. The page shows it
/// under the chart whose <see cref="MarketAnalysisSeriesDto.Id"/> equals <see cref="Series"/>, labelled as AI-generated.
/// </summary>
/// <param name="Series">The path inside the findings of the group figures that the observation reads.</param>
/// <param name="Text">The observation.</param>
/// <param name="Evidence">The numbers that the text rests on; they are found in the series.</param>
public sealed record MarketAnalysisObservationDto(string Series, string Text, string Evidence);
