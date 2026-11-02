using NFHotel.Application.Dto.Pricing;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// The price suggestions of the Manager (MIL-055): the open suggestions from the latest market analysis, the approval of one,
/// the revert of a price change, the list of the price changes and the effective prices of a day. The staff member who acts
/// is the caller of the use case.
/// </summary>
public interface IPriceSuggestionService
{
    /// <summary>
    /// Lists the suggestions that are open today: those the latest report, the base rates and the settings give, without the
    /// ones whose whole period a price change of the same room size already counts on.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The list; with the basis <see cref="PriceSuggestionBasis.NoBasis"/> and no items when there is no report or the report
    /// holds no usable occupancy by month.
    /// </returns>
    Task<Result<PriceSuggestionListDto>> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a suggestion. The percentage comes from the suggestion the key names, recomputed on the server; the dates may
    /// be changed. The same approval twice creates no second change.
    /// </summary>
    /// <param name="request">The key and the optional dates.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The change, or the codes that say what is wrong (the key is unknown, the dates, an overlap).</returns>
    Task<Result<ApprovedPriceChange>> ApproveAsync(PriceSuggestionApproveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reverts a price change.</summary>
    /// <param name="id">The key of the change.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The reverted change, or the codes that say what is wrong.</returns>
    Task<Result<PriceChangeDto>> RevertAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists every price change, the newest period first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The changes ordered by first day (latest first), then room size.</returns>
    Task<Result<IReadOnlyList<PriceChangeDto>>> GetChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gives the effective price of every room size on a day.</summary>
    /// <param name="day">The day, or <c>null</c> for today.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>One price per room size.</returns>
    Task<Result<IReadOnlyList<EffectivePriceDto>>> GetEffectivePricesAsync(DateOnly? day, CancellationToken cancellationToken = default);
}
