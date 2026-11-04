using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Shifts;

/// <summary>A shift given away, or a swap of shifts (MIL-060).</summary>
/// <param name="Id">The trade.</param>
/// <param name="Shift">The shift offered.</param>
/// <param name="SwapShift">The shift asked for in a swap, or <c>null</c> when the shift is given away.</param>
/// <param name="OfferedById">The member of staff who offered it.</param>
/// <param name="OfferedByName">The name of that member of staff.</param>
/// <param name="TakenById">The colleague who took or is asked to accept it, or <c>null</c>.</param>
/// <param name="TakenByName">The name of the colleague, or <c>null</c>.</param>
/// <param name="State">Where the trade is.</param>
/// <param name="IsClosed">True when a shift of the trade starts within 24 hours, so it can no longer be taken or accepted.</param>
/// <param name="OfferedAt">When it was offered.</param>
public sealed record ShiftTradeDto(
    int Id,
    ShiftDto Shift,
    ShiftDto? SwapShift,
    int OfferedById,
    string OfferedByName,
    int? TakenById,
    string? TakenByName,
    TradeState State,
    bool IsClosed,
    DateTimeOffset OfferedAt);
