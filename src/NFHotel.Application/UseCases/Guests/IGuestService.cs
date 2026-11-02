using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Use cases for guests.
/// </summary>
public interface IGuestService
{
    /// <summary>
    /// Fetches one guest (BR-35, BR-115).
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the guest, or <see cref="ErrorCodes.Guest.NotFound"/>.</returns>
    /// <remarks>Also used to prefill a new booking for a known guest.</remarks>
    Task<Result<GuestDetailsDto>> GetByIdAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches guests (BR-68, BR-69, BR-70, BR-116, BR-N-05).
    /// </summary>
    /// <param name="searchText">
    /// Free text. Split into words; every word must be found in first name, last name, country or email.
    /// Empty or only whitespace gives all guests (BR-70).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching guests, sorted on "first name last name" in lowercase (BR-69).</returns>
    Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a guest (BR-52 to BR-57, BR-60, BR-91 to BR-96).
    /// </summary>
    /// <param name="request">The fields of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created guest, or all validation errors at once (BR-60, BR-96).</returns>
    Task<Result<GuestDetailsDto>> CreateAsync(
        CreateGuestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a guest (BR-62, BR-66, BR-72, BR-91 to BR-96).
    /// </summary>
    /// <param name="request">The new field values plus the id of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated guest, or all validation errors at once.</returns>
    Task<Result<GuestDetailsDto>> UpdateAsync(
        UpdateGuestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a guest (BR-N-02, follows from D-05).
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or <see cref="ErrorCodes.Guest.HasBookings"/> if the guest has bookings.</returns>
    /// <remarks>
    /// The rule is not among the 126 ported rules and is registered as new (BR-N-02).
    /// </remarks>
    Task<Result> DeleteAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the guest fields without saving (BR-58, BR-59).
    /// </summary>
    /// <param name="fields">The fields to validate.</param>
    /// <returns>Success, or all broken rules at once.</returns>
    /// <remarks>
    /// <b>Deliberately not async</b> — there is no IO, and a method without real async work
    /// should not be async just for show.
    /// <para>
    /// The method lets the UI give feedback per keystroke and enable the save button without
    /// duplicating a single validation rule. It is the method that means BR-52 to BR-56 never
    /// have to exist in the Web layer.
    /// </para>
    /// </remarks>
    Result ValidateFields(GuestFields fields);
}
