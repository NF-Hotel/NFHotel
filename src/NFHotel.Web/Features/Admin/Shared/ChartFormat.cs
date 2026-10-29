using System.Globalization;
using System.Net;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>Text helpers shared by the SVG charts: numbers, labels and encoding. The charts are built as text, so every label is encoded here.</summary>
public static class ChartFormat
{
    private const string Percent = "%";

    /// <summary>Writes a value as the charts show it: a rate as <c>42.5%</c>, a count with a thousands separator.</summary>
    /// <param name="value">The value.</param>
    /// <param name="unit">The unit of the series; <c>%</c> for a rate.</param>
    /// <returns>The text.</returns>
    public static string Value(double value, string unit) =>
        unit == Percent
            ? value.ToString("0.#", CultureInfo.InvariantCulture) + Percent
            : value.ToString("#,0.##", CultureInfo.InvariantCulture);

    /// <summary>Writes a number for an attribute of the SVG.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The text with at most one decimal and a dot.</returns>
    public static string Num(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>Encodes text for the markup of an SVG built as text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The encoded text.</returns>
    public static string Encode(string text) => WebUtility.HtmlEncode(text);

    /// <summary>Shortens a label to fit, with an ellipsis; the full text goes to a tooltip.</summary>
    /// <param name="label">The label.</param>
    /// <param name="maxLength">The most characters to keep; at least 2.</param>
    /// <returns>The label, or its start followed by an ellipsis.</returns>
    public static string Shorten(string label, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 2);

        return label.Length <= maxLength ? label : label[..(maxLength - 1)] + "…";
    }

    /// <summary>Makes a field name readable: underscores become spaces.</summary>
    /// <param name="name">The name, for example <c>lead_time</c>.</param>
    /// <returns>The name with spaces.</returns>
    public static string Readable(string name) => name.Replace('_', ' ');
}
