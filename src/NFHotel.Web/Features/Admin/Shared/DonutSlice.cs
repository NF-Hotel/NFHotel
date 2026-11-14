namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>One slice of a donut.</summary>
/// <param name="Index">The index of the value the slice draws.</param>
/// <param name="Share">The share of the whole from 0 to 1.</param>
/// <param name="Path">The SVG path of the slice, empty for a share of 0.</param>
/// <param name="LabelX">The x of the middle of the slice, for its label.</param>
/// <param name="LabelY">The y of the middle of the slice, for its label.</param>
public sealed record DonutSlice(int Index, double Share, string Path, double LabelX, double LabelY);
