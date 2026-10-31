using System.Globalization;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>
/// The colours of the heat maps. Every cell carries its own fill and the colour of its text, so that a map reads the same on the
/// light page and on a dark theme. The diverging scale is the ColorBrewer RdBu ends with a near white middle: blue for negative,
/// red for positive.
/// </summary>
public static class HeatColor
{
    private const double LightLuminance = 0.45;

    private static readonly (int R, int G, int B) Negative = (0x21, 0x66, 0xac);
    private static readonly (int R, int G, int B) Neutral = (0xf7, 0xf7, 0xf7);
    private static readonly (int R, int G, int B) Positive = (0xb2, 0x18, 0x2b);
    private static readonly (int R, int G, int B) Low = (0xef, 0xf3, 0xff);
    private static readonly (int R, int G, int B) High = (0x08, 0x51, 0x9c);

    /// <summary>The colour of a value on the diverging scale from -1 to +1.</summary>
    /// <param name="value">The value; outside -1 to +1 it is cut to the nearest end.</param>
    /// <returns>A colour as <c>#rrggbb</c>.</returns>
    public static string Diverging(double value)
    {
        var clamped = Math.Clamp(value, -1d, 1d);

        return Hex(Mix(Neutral, clamped < 0 ? Negative : Positive, Math.Abs(clamped)));
    }

    /// <summary>The colour of a position on the sequential scale from light to dark blue.</summary>
    /// <param name="position">The position from 0 (lowest value) to 1 (highest value); cut to that range.</param>
    /// <returns>A colour as <c>#rrggbb</c>.</returns>
    public static string Sequential(double position) => Hex(Mix(Low, High, Math.Clamp(position, 0d, 1d)));

    /// <summary>The colour of a text on a fill that is readable on it: dark on a light fill, white on a dark fill.</summary>
    /// <param name="fill">The fill as <c>#rrggbb</c>.</param>
    /// <returns>The text colour as <c>#rrggbb</c>.</returns>
    public static string TextOn(string fill)
    {
        var red = Channel(fill, 1);
        var green = Channel(fill, 3);
        var blue = Channel(fill, 5);
        var luminance = (0.2126 * Linear(red)) + (0.7152 * Linear(green)) + (0.0722 * Linear(blue));

        return luminance > LightLuminance ? "#14171f" : "#ffffff";
    }

    private static (int R, int G, int B) Mix((int R, int G, int B) from, (int R, int G, int B) to, double amount) =>
        (Lerp(from.R, to.R, amount), Lerp(from.G, to.G, amount), Lerp(from.B, to.B, amount));

    private static int Lerp(int from, int to, double amount) => (int)Math.Round(from + ((to - from) * amount));

    private static string Hex((int R, int G, int B) color) => string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}");

    private static double Channel(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;

    private static double Linear(double channel) =>
        channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
