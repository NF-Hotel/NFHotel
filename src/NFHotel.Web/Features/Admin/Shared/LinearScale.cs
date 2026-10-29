namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>An axis with round limits and the ticks between them.</summary>
/// <param name="Min">The lower limit, at or below the lowest value.</param>
/// <param name="Max">The upper limit, at or above the highest value.</param>
/// <param name="Ticks">The tick values from <paramref name="Min"/> to <paramref name="Max"/>.</param>
public sealed record NiceAxis(double Min, double Max, IReadOnlyList<double> Ticks);

/// <summary>Maps values of a chart to pixels, and finds round axis limits.</summary>
public sealed class LinearScale
{
    private static readonly double[] StepFactors = [1, 2, 2.5, 5, 10];

    private readonly double _domainMin;
    private readonly double _domainMax;
    private readonly double _rangeStart;
    private readonly double _rangeEnd;

    /// <summary>Creates a scale.</summary>
    /// <param name="domainMin">The value at the start of the range.</param>
    /// <param name="domainMax">The value at the end of the range.</param>
    /// <param name="rangeStart">The pixel of <paramref name="domainMin"/>.</param>
    /// <param name="rangeEnd">The pixel of <paramref name="domainMax"/>; smaller than the start for a vertical axis.</param>
    public LinearScale(double domainMin, double domainMax, double rangeStart, double rangeEnd)
    {
        _domainMin = domainMin;
        _domainMax = domainMax;
        _rangeStart = rangeStart;
        _rangeEnd = rangeEnd;
    }

    /// <summary>Finds round limits and ticks that hold the values.</summary>
    /// <param name="min">The lowest value; use 0 to start at zero.</param>
    /// <param name="max">The highest value.</param>
    /// <param name="tickCount">About how many ticks are wanted; at least 2.</param>
    /// <returns>The axis.</returns>
    public static NiceAxis Nice(double min, double max, int tickCount = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tickCount, 2);

        var high = Math.Max(max, min);
        if (high - min <= 0)
        {
            // Every value is the same: give the axis a width so that the value is not on its edge.
            high = min + (min == 0 ? 1 : Math.Abs(min));
        }

        var rough = (high - min) / (tickCount - 1);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var step = StepFactors.Select(factor => factor * magnitude).First(candidate => candidate >= rough);
        var niceMin = Math.Floor(min / step) * step;
        var niceMax = Math.Ceiling(high / step) * step;

        var ticks = new List<double>();
        var count = (int)Math.Round((niceMax - niceMin) / step);
        for (var index = 0; index <= count; index++)
        {
            ticks.Add(Math.Round(niceMin + (index * step), 10));
        }

        return new NiceAxis(Math.Round(niceMin, 10), Math.Round(niceMax, 10), ticks);
    }

    /// <summary>Maps a value to a pixel; a domain with no width maps everything to the middle of the range.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The pixel.</returns>
    public double Map(double value) =>
        _domainMax <= _domainMin
            ? (_rangeStart + _rangeEnd) / 2
            : _rangeStart + ((value - _domainMin) / (_domainMax - _domainMin) * (_rangeEnd - _rangeStart));
}
