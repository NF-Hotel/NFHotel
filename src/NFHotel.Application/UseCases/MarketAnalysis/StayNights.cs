namespace NFHotel.Application.UseCases.MarketAnalysis;

/// <summary>Splits a stay into weekend nights and week nights, as the input contract of the markets API wants it (MIL-026).</summary>
public static class StayNights
{
    /// <summary>
    /// Counts the nights of <c>[start, end)</c>. The night of a date belongs to that date, so a Friday night is a week night
    /// and a Saturday night and a Sunday night are weekend nights.
    /// </summary>
    /// <param name="start">The arrival date.</param>
    /// <param name="end">The departure date; the night before it is the last one.</param>
    /// <returns>The weekend nights and the week nights; both 0 when <paramref name="end"/> is not after <paramref name="start"/>.</returns>
    public static (int Weekend, int Week) Split(DateOnly start, DateOnly end)
    {
        var weekend = 0;
        var week = 0;
        for (var night = start; night < end; night = night.AddDays(1))
        {
            if (night.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                weekend++;
            }
            else
            {
                week++;
            }
        }

        return (weekend, week);
    }
}
