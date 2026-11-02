using NFHotel.Application.Dto.Pricing;
using NFHotel.Domain.Entities;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The one mapping from a stored price change to its DTO (MIL-051).</summary>
internal static class PriceChangeMapping
{
    /// <summary>Maps a price change to its DTO.</summary>
    /// <param name="value">The change.</param>
    /// <returns>The DTO.</returns>
    public static PriceChangeDto ToDto(this PriceChange value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PriceChangeDto(
            value.Id,
            value.Size.ToString(),
            value.Percentage,
            value.FirstDay,
            value.LastDay,
            value.State.ToString(),
            value.ApprovedBy,
            value.ApprovedAt,
            value.RevertedBy,
            value.RevertedAt,
            value.EndedOn);
    }
}
