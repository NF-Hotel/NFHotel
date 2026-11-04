namespace NFHotel.Application.Dto.Shifts;

/// <summary>An own shift given away, or offered in a swap (MIL-060).</summary>
/// <param name="ShiftId">The own shift.</param>
/// <param name="SwapShiftId">The colleague's shift asked for in return, or <c>null</c> to give the shift away.</param>
public sealed record OfferShiftTradeRequest(int ShiftId, int? SwapShiftId);
