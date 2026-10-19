using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Ports;

/// <summary>
/// Reading guests as projections (A-11). Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// Entities are fetched only on command paths, via <c>IGuestRepository</c> in Domain.
/// </remarks>
public interface IGuestQueries
{
    /// <summary>
    /// Fetches the details of a guest as a projection.
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details, or <c>null</c> if the guest does not exist (BR-115).</returns>
    Task<GuestDetailsDto?> GetDetailsAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches guests (BR-68, BR-N-05).
    /// </summary>
    /// <param name="searchTerms">
    /// The search words. <b>EVERY</b> word must be found (case-insensitively) in either first name,
    /// last name, country or email — that is, AND between words, OR between fields. An empty set
    /// returns all guests (BR-70).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The matching guests as list rows.</returns>
    /// <remarks>
    /// The semantics are consolidated from two disagreeing searches in the old system: the repository searched
    /// only first and last name (BR-116), while the ViewModel split the text into words and required
    /// that all words were found in name, country or email (BR-68). BR-68 won (BR-N-05).
    /// The tokenisation is the rule and happens in the service; the matching is data access and happens here.
    /// The contract must be pinned by a test in Infrastructure.Tests, otherwise they drift apart.
    /// </remarks>
    Task<IReadOnlyList<GuestListItemDto>> SearchAsync(
        IReadOnlyCollection<string> searchTerms,
        CancellationToken cancellationToken = default);
}
