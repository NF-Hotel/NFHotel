using System.Globalization;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The arithmetic of a donut chart: the slices as SVG paths, starting at twelve o'clock and going clockwise.</summary>
public static class DonutGeometry
{
    #region Fields

    // A slice that is the whole circle cannot be drawn as one arc, so it stops a hair short of it.
    private const double FullTurn = 360d;
    private const double MaxSweep = 359.99d;
    private const double StartAngle = -90d;
    private const double HalfTurn = 180d;

    #endregion Fields

    #region Public methods

    /// <summary>Works out the slices.</summary>
    /// <param name="values">The values; negative values count as 0.</param>
    /// <param name="centerX">The x of the centre.</param>
    /// <param name="centerY">The y of the centre.</param>
    /// <param name="outerRadius">The outer radius.</param>
    /// <param name="innerRadius">The radius of the hole, smaller than the outer radius.</param>
    /// <returns>One slice for each value, in order; empty when the values add up to 0.</returns>
    public static IReadOnlyList<DonutSlice> Slices(IReadOnlyList<double> values, double centerX, double centerY, double outerRadius, double innerRadius)
    {
        double total = values.Sum(value => Math.Max(0, value));
        if (total <= 0)
        {
            return [];
        }

        List<DonutSlice> slices = new List<DonutSlice>();
        double start = StartAngle;
        for (int index = 0; index < values.Count; index++)
        {
            double share = Math.Max(0, values[index]) / total;
            double sweep = Math.Min(share * FullTurn, MaxSweep);
            double end = start + sweep;
            (double X, double Y) middle = Point(centerX, centerY, (outerRadius + innerRadius) / 2, start + (sweep / 2));
            slices.Add(new DonutSlice(index, share, share <= 0 ? string.Empty : Path(centerX, centerY, outerRadius, innerRadius, start, end), middle.X, middle.Y));
            start += share * FullTurn;
        }

        return slices;
    }

    #endregion Public methods

    #region Private methods

    private static string Path(double centerX, double centerY, double outerRadius, double innerRadius, double start, double end)
    {
        (double X, double Y) outerStart = Point(centerX, centerY, outerRadius, start);
        (double X, double Y) outerEnd = Point(centerX, centerY, outerRadius, end);
        (double X, double Y) innerEnd = Point(centerX, centerY, innerRadius, end);
        (double X, double Y) innerStart = Point(centerX, centerY, innerRadius, start);
        int large = end - start > HalfTurn ? 1 : 0;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"M {Num(outerStart.X)} {Num(outerStart.Y)} A {Num(outerRadius)} {Num(outerRadius)} 0 {large} 1 {Num(outerEnd.X)} {Num(outerEnd.Y)} L {Num(innerEnd.X)} {Num(innerEnd.Y)} A {Num(innerRadius)} {Num(innerRadius)} 0 {large} 0 {Num(innerStart.X)} {Num(innerStart.Y)} Z");
    }

    private static (double X, double Y) Point(double centerX, double centerY, double radius, double degrees)
    {
        double radians = degrees * Math.PI / HalfTurn;

        return (centerX + (radius * Math.Cos(radians)), centerY + (radius * Math.Sin(radians)));
    }

    private static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    #endregion Private methods
}
