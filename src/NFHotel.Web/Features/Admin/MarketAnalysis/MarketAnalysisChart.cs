using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>One chart of an analysis as the page draws it.</summary>
/// <param name="Series">The series to draw.</param>
/// <param name="Kind">The kind of chart that the rule of <see cref="ChartKindSelector"/> chose for the series.</param>
public sealed record MarketAnalysisChart(MarketAnalysisSeriesDto Series, ChartKind Kind);
