using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>
/// The code behind the room revenue panel: the caption, the title of the monthly chart and its caption model.
/// </summary>
public partial class RoomRevenuePanel : ComponentBase
{
    /// <summary>What the figures are not, in the words of the page; the service adds its own limitation under it.</summary>
    public const string Caption =
        "Room revenue is the price per night times the nights of the bookings that are not cancelled, before taxes, fees, discounts and extras. "
        + "The currency is not known, so the amounts are in the price units of the input. Bookings without a price are left out of the figures.";

    // The chart draws every month the service sends, while the AI observation under it reads the latest 30, so the title says how many.
    private string MonthlyTitle => $"Room revenue by month of arrival (all {Revenue.Monthly.Count} months)";

    /// <summary>The analysis the room revenue belongs to, for the limitations, the formula and the AI observation of the chart.</summary>
    [Parameter]
    public MarketAnalysisSectionDto Section { get; set; } = new(string.Empty, string.Empty, string.Empty, null, [], null);

    private ChartCaptionModel MonthlyCaption =>
        ChartCaptionText.ForSeries(
            ChartKind.Columns,
            new MarketAnalysisSeriesDto(MarketAnalysisViewModel.RoomRevenueSeriesId, MonthlyTitle, Revenue.Unit, Revenue.Monthly, IsOrdered: true),
            Section);

    /// <summary>The room revenue to show.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisRoomRevenueDto Revenue { get; set; } = new(string.Empty, string.Empty, 0, null, 0, 0, []);
}
