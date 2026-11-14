using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>One analysis of the report as the page draws it.</summary>
/// <param name="Section">The analysis with its AI insight.</param>
/// <param name="Charts">One chart for each series that has points.</param>
public sealed record MarketAnalysisSectionView(MarketAnalysisSectionDto Section, IReadOnlyList<MarketAnalysisChart> Charts)
{
    /// <summary>The correlation matrices of the analysis, drawn as heat maps.</summary>
    public IReadOnlyList<MarketAnalysisMatrixDto> Matrices => Section.Matrices ?? [];

    /// <summary>True when the analysis has something to draw.</summary>
    public bool HasCharts => Charts.Count > 0 || Matrices.Count > 0;
}
