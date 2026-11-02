namespace NFHotel.Application.Dto.RoomRates;

/// <summary>The nightly base rate of one room size (MIL-055).</summary>
/// <param name="Size">The room size: <c>Single</c>, <c>Double</c> or <c>Suite</c>.</param>
/// <param name="NightlyPrice">The price of one night.</param>
/// <param name="SetBy">The user name of the staff member who set it.</param>
/// <param name="SetAt">When it was set.</param>
public sealed record RoomRateDto(string Size, decimal NightlyPrice, string SetBy, DateTimeOffset SetAt);
