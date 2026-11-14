using System.Globalization;
using System.Text;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the correlation heat map: the matrix, the cells and the SVG built from them.</summary>
public partial class CorrelationHeatmap : ComponentBase
{
    #region Fields

    private const int Left = 150;
    private const int Top = 128;
    private const int RightMargin = 96;
    private const int MinCell = 30;
    private const int MaxCell = 56;
    private const int PlotSize = 560;
    private const int LabelLength = 20;
    private const int LegendWidth = 240;
    private const int LegendHeight = 10;

    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    #endregion Fields

    #region Properties

    /// <summary>The matrix to draw.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisMatrixDto Matrix { get; set; } = new("matrix", "Correlations", string.Empty, [], []);

    private string Title => string.IsNullOrWhiteSpace(Matrix.Method) ? Matrix.Title : $"{Matrix.Title} ({Matrix.Method})";

    #endregion Properties

    #region Private methods

    private string CellText(int row, int column, string missing) =>
        Value(row, column) is { } value ? value.ToString("0.00", CultureInfo.InvariantCulture) : missing;

    private double? Value(int row, int column) =>
        row < Matrix.Cells.Count && column < Matrix.Cells[row].Count ? Matrix.Cells[row][column] : null;

    private MarkupString BuildSvg()
    {
        int count = Matrix.Fields.Count;
        int cell = Math.Clamp(PlotSize / count, MinCell, MaxCell);
        int width = Left + (count * cell) + RightMargin;
        int legendTop = Top + (count * cell) + 24;
        int height = legendTop + 48;
        string gradient = $"chart-scale-{_id}";
        string titleId = $"chart-title-{_id}";
        string descId = $"chart-desc-{_id}";
        StringBuilder svg = new StringBuilder();

        svg.Append(CultureInfo.InvariantCulture, $"<svg class=\"chart-svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-labelledby=\"{titleId} {descId}\" preserveAspectRatio=\"xMinYMin meet\">");
        svg.Append(CultureInfo.InvariantCulture, $"<title id=\"{titleId}\">{ChartFormat.Encode(Title)}</title><desc id=\"{descId}\">{ChartFormat.Encode($"Correlations between {count} fields from -1 to +1; the values are in the table below the chart.")}</desc>");
        svg.Append(CultureInfo.InvariantCulture, $"<defs><linearGradient id=\"{gradient}\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"0\"><stop offset=\"0\" stop-color=\"{HeatColor.Diverging(-1)}\"></stop><stop offset=\"0.5\" stop-color=\"{HeatColor.Diverging(0)}\"></stop><stop offset=\"1\" stop-color=\"{HeatColor.Diverging(1)}\"></stop></linearGradient></defs>");

        for (int index = 0; index < count; index++)
        {
            string name = ChartFormat.Encode(ChartFormat.Shorten(ChartFormat.Readable(Matrix.Fields[index]), LabelLength));
            string full = ChartFormat.Encode(ChartFormat.Readable(Matrix.Fields[index]));
            int cx = Left + (index * cell) + (cell / 2);
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-label\" x=\"{cx}\" y=\"{Top - 8}\" transform=\"rotate(-45 {cx} {Top - 8})\"><title>{full}</title>{name}</text>");
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-label\" x=\"{Left - 8}\" y=\"{Top + (index * cell) + (cell / 2) + 4}\" text-anchor=\"end\"><title>{full}</title>{name}</text>");
        }

        for (int row = 0; row < count; row++)
        {
            for (int column = 0; column < count; column++)
            {
                AppendCell(svg, row, column, Left + (column * cell), Top + (row * cell), cell);
            }
        }

        svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Left}\" y=\"{legendTop}\" width=\"{LegendWidth}\" height=\"{LegendHeight}\" fill=\"url(#{gradient})\"></rect>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{Left}\" y=\"{legendTop + 26}\">-1</text>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{Left + (LegendWidth / 2)}\" y=\"{legendTop + 26}\" text-anchor=\"middle\">0</text>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{Left + LegendWidth}\" y=\"{legendTop + 26}\" text-anchor=\"end\">+1</text>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{Left + LegendWidth + 12}\" y=\"{legendTop + 9}\">blue: opposite, white: none, red: together</text>");
        svg.Append("</svg>");

        return new MarkupString(svg.ToString());
    }

    private void AppendCell(StringBuilder svg, int row, int column, int x, int y, int cell)
    {
        string rowName = ChartFormat.Encode(ChartFormat.Readable(Matrix.Fields[row]));
        string columnName = ChartFormat.Encode(ChartFormat.Readable(Matrix.Fields[column]));
        int textX = x + (cell / 2);
        int textY = y + (cell / 2) + 4;

        if (Value(row, column) is not { } value)
        {
            svg.Append(CultureInfo.InvariantCulture, $"<rect class=\"chart-cell chart-cell-empty\" x=\"{x}\" y=\"{y}\" width=\"{cell}\" height=\"{cell}\"><title>{rowName} and {columnName}: not calculated</title></rect>");
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-cell-text chart-missing\" x=\"{textX}\" y=\"{textY}\" text-anchor=\"middle\">–</text>");
            return;
        }

        string fill = HeatColor.Diverging(value);
        string text = value.ToString("0.00", CultureInfo.InvariantCulture);
        svg.Append(CultureInfo.InvariantCulture, $"<rect class=\"chart-cell\" x=\"{x}\" y=\"{y}\" width=\"{cell}\" height=\"{cell}\" fill=\"{fill}\"><title>{rowName} and {columnName}: {text}</title></rect>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-cell-text\" x=\"{textX}\" y=\"{textY}\" text-anchor=\"middle\" fill=\"{HeatColor.TextOn(fill)}\">{text}</text>");
    }

    #endregion Private methods
}
