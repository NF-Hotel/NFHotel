namespace NFHotel.Application.Dto.Pricing;

/// <summary>A price change of one room size (MIL-051).</summary>
/// <param name="Id">The key of the change.</param>
/// <param name="Size">The room size: <c>Single</c>, <c>Double</c> or <c>Suite</c>.</param>
/// <param name="Percentage">The change in percent: above 0 for a surcharge, below 0 for a discount.</param>
/// <param name="FirstDay">The first day of the period.</param>
/// <param name="LastDay">The last day of the period.</param>
/// <param name="State">The state: <c>Approved</c> or <c>Reverted</c>.</param>
/// <param name="ApprovedBy">The user name of the staff member who approved it.</param>
/// <param name="ApprovedAt">When it was approved.</param>
/// <param name="RevertedBy">The user name of the staff member who reverted it, or <c>null</c>.</param>
/// <param name="RevertedAt">When it was reverted, or <c>null</c>.</param>
/// <param name="EndedOn">The last day a reverted change in force counted, or <c>null</c>.</param>
public sealed record PriceChangeDto(
    int Id,
    string Size,
    decimal Percentage,
    DateOnly FirstDay,
    DateOnly LastDay,
    string State,
    string ApprovedBy,
    DateTimeOffset ApprovedAt,
    string? RevertedBy,
    DateTimeOffset? RevertedAt,
    DateOnly? EndedOn);
