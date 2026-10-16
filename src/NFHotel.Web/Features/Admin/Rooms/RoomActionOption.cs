namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// En knap i rummets handlingspanel: overgangen og den danske tekst på knappen.
/// </summary>
/// <remarks>
/// Viewmodellen udleverer kun de overgange <c>RoomActionsDto</c> siger er lovlige lige nu,
/// så markupen kan løbe listen igennem uden at kende en eneste tilstandsovergang.
/// </remarks>
/// <param name="Action">Overgangen knappen udfører.</param>
/// <param name="Label">Knappens tekst.</param>
public sealed record RoomActionOption(RoomAction Action, string Label);