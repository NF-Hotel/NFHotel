namespace NFHotel.Application.Dto.Rooms;

/// <summary>Rooms that a housekeeper gives to a colleague, and the colleague's rooms asked for in return (MIL-060).</summary>
/// <param name="ToStaffId">The colleague.</param>
/// <param name="GiveRoomIds">The own rooms given; at least one.</param>
/// <param name="TakeRoomIds">The colleague's rooms asked for; none for a gift.</param>
public sealed record OfferRoomTradeRequest(int ToStaffId, IReadOnlyList<int> GiveRoomIds, IReadOnlyList<int>? TakeRoomIds);
