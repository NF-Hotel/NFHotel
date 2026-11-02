namespace NFHotel.Application.Dto.RoomRates;

/// <summary>The body that sets the nightly base rate of a room size (MIL-051).</summary>
/// <param name="NightlyPrice">The price of one night, above 0 and at most 1,000,000, with two decimals.</param>
public sealed record SetRoomRateRequest(decimal NightlyPrice);
