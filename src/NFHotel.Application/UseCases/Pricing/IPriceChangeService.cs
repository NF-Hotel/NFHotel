using NFHotel.Application.Dto.Pricing;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Creates and reverts price changes and gives the effective price of a room size on a day (MIL-055). The staff member
/// who approves or reverts is the caller of the use case.
/// </summary>
public interface IPriceChangeService
{
    /// <summary>
    /// Approves a price change. The same change twice (size, percentage and period) creates no second change: the first
    /// one is returned with <see cref="ApprovedPriceChange.Created"/> set to <c>false</c>.
    /// </summary>
    /// <param name="size">The room size: <c>single</c>, <c>double</c> or <c>suite</c>, in any letter case.</param>
    /// <param name="percentage">The change in percent, below 0 for a discount.</param>
    /// <param name="firstDay">The first day of the period.</param>
    /// <param name="lastDay">The last day of the period.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The change, or the codes that say what is wrong (for example an overlap with another change).</returns>
    Task<Result<ApprovedPriceChange>> ApproveAsync(
        string size,
        decimal percentage,
        DateOnly firstDay,
        DateOnly lastDay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverts a price change: a change that has not begun is withdrawn, a change in force ends and the base rate is back
    /// from today. The change stays in the storage in the state reverted.
    /// </summary>
    /// <param name="id">The key of the change.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The reverted change, or the codes that say what is wrong.</returns>
    Task<Result<PriceChangeDto>> RevertAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gives the effective price of a room size on a day.</summary>
    /// <param name="size">The room size, in any letter case.</param>
    /// <param name="day">The day.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The price, or the code of an unknown size.</returns>
    Task<Result<EffectivePriceDto>> GetEffectivePriceAsync(string size, DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>Gives the effective price of every room size on a day.</summary>
    /// <param name="day">The day.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>One price per room size, in the order of the sizes.</returns>
    Task<Result<IReadOnlyList<EffectivePriceDto>>> GetEffectivePricesAsync(DateOnly day, CancellationToken cancellationToken = default);
}
