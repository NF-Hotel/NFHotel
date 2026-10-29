namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// A square matrix of one analysis, for example the Spearman correlations between the numeric booking fields. It is not a list
/// of group figures, so it is not a <see cref="MarketAnalysisSeriesDto"/>; the page draws it as a heat map.
/// </summary>
/// <param name="Id">A stable key of the matrix, the path of the matrix in the findings.</param>
/// <param name="Title">The title of the chart.</param>
/// <param name="Method">How the values were calculated, for example <c>spearman</c>.</param>
/// <param name="Fields">The name of each row and column, in order.</param>
/// <param name="Cells">One row per field; a value is <c>null</c> when it could not be calculated.</param>
public sealed record MarketAnalysisMatrixDto(
    string Id,
    string Title,
    string Method,
    IReadOnlyList<string> Fields,
    IReadOnlyList<IReadOnlyList<double?>> Cells);
