namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>One bar of a grouped bar chart.</summary>
/// <param name="Series">The series the bar belongs to, for example cold or warm; its colour comes from the order of the series.</param>
/// <param name="Value">The value, or <c>null</c> when it could not be measured.</param>
public sealed record ChartBar(string Series, double? Value);

/// <summary>One group of a grouped bar chart: a label and one bar per series.</summary>
/// <param name="Label">The label of the group, for example a model name.</param>
/// <param name="Bars">The bars of the group.</param>
public sealed record ChartGroup(string Label, IReadOnlyList<ChartBar> Bars);
