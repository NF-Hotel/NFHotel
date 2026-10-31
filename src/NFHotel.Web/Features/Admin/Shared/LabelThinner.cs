namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>Decides which labels of a crowded axis are written, so that a series of 30 months stays readable.</summary>
public static class LabelThinner
{
    /// <summary>Finds the gap between written labels.</summary>
    /// <param name="count">How many labels the axis has.</param>
    /// <param name="maxLabels">The most labels that fit; at least 1.</param>
    /// <returns>1 when all fit, otherwise the N of "every Nth label".</returns>
    public static int Step(int count, int maxLabels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLabels, 1);

        return count <= maxLabels ? 1 : (int)Math.Ceiling(count / (double)maxLabels);
    }

    /// <summary>Tells whether the label at an index is written.</summary>
    /// <param name="index">The index from 0.</param>
    /// <param name="step">The gap from <see cref="Step"/>.</param>
    /// <returns>True for every Nth label, counted from the first.</returns>
    public static bool IsShown(int index, int step) => index % step == 0;
}
