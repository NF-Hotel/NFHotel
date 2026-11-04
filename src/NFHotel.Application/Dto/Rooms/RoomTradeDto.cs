using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>A trade of rooms between two housekeepers (MIL-060).</summary>
/// <param name="Id">The trade.</param>
/// <param name="FromStaffId">The housekeeper who offered it.</param>
/// <param name="FromName">The name of that housekeeper.</param>
/// <param name="ToStaffId">The colleague it is offered to.</param>
/// <param name="ToName">The name of the colleague.</param>
/// <param name="Give">The rooms that go to the colleague.</param>
/// <param name="Take">The rooms that come back; empty for a gift.</param>
/// <param name="State">Where the trade is.</param>
/// <param name="OfferedAt">When it was offered.</param>
public sealed record RoomTradeDto(
    int Id,
    int FromStaffId,
    string FromName,
    int ToStaffId,
    string ToName,
    IReadOnlyList<TradedRoomDto> Give,
    IReadOnlyList<TradedRoomDto> Take,
    TradeState State,
    DateTimeOffset OfferedAt);
