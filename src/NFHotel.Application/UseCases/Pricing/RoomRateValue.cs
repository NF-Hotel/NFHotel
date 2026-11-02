using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The base rate of a room size, as the pricing rule reads it.</summary>
/// <param name="Size">The room size.</param>
/// <param name="NightlyPrice">The nightly price.</param>
public sealed record RoomRateValue(RoomSize Size, decimal NightlyPrice);
