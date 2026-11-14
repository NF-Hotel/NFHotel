namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>An axis with round limits and the ticks between them.</summary>
/// <param name="Min">The lower limit, at or below the lowest value.</param>
/// <param name="Max">The upper limit, at or above the highest value.</param>
/// <param name="Ticks">The tick values from <paramref name="Min"/> to <paramref name="Max"/>.</param>
public sealed record NiceAxis(double Min, double Max, IReadOnlyList<double> Ticks);
