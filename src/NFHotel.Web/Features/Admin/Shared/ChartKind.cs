namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The kind of chart that draws one series of the market analysis report (MIL-026, criterion 18).</summary>
public enum ChartKind
{
    /// <summary>The plain grouped bars of the benchmark, for a series that fits no other kind.</summary>
    GroupedBars,

    /// <summary>One figure, drawn as a tile.</summary>
    Tile,

    /// <summary>Two to four parts of one whole that add up to 100 %.</summary>
    Donut,

    /// <summary>More than four categories, largest first.</summary>
    RankedBars,

    /// <summary>Ordered bands, numbers or a few separate rates, in their order.</summary>
    Columns,

    /// <summary>A series of months.</summary>
    Line,

    /// <summary>A long series of months, laid out as months against years.</summary>
    Heatmap,

    /// <summary>
    /// The correlation matrix of an analysis. The selector never returns it: a matrix is not a series, so the page draws it
    /// with <c>CorrelationHeatmap</c> and only the captions use this value.
    /// </summary>
    Correlation,
}
