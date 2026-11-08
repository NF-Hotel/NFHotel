namespace NFHotel.Web.Components.Shared;

/// <summary>One tab of <see cref="AdminTabs"/>: where it leads and what it says.</summary>
/// <param name="Href">The path of the page, for example <c>/admin/pricing/rates</c>.</param>
/// <param name="Label">The text on the tab.</param>
public sealed record AdminTab(string Href, string Label);
