namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// A button in the action panel of the room: the transition and the Danish text on the button.
/// </summary>
/// <remarks>
/// The viewmodel hands out only the transitions <c>RoomActionsDto</c> says are legal right now,
/// so the markup can run through the list without knowing a single state transition.
/// </remarks>
/// <param name="Action">The transition the button performs.</param>
/// <param name="Label">Knappens tekst.</param>
public sealed record RoomActionOption(RoomAction Action, string Label);
