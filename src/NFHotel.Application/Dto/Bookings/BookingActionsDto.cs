namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Which actions are legal on a booking right now (BR-22).
/// </summary>
/// <remarks>
/// The most important single part of the whole DTO design. Computed in Application from the state
/// rules of the domain, so Web can draw buttons without knowing a single status transition. That is
/// how BR-02, BR-11, BR-14 and BR-22 (ViewModel) and BR-121, BR-122, BR-123 (XAML triggers)
/// become <b>one</b> implementation instead of six.
/// <para>
/// The name is <c>CanReschedule</c> and not <c>CanEdit</c>, so the DTO and the domain use the same word
/// om samme regel (A-06).
/// </para>
/// </remarks>
/// <param name="CanConfirm">Whether the booking may be confirmed (BR-02, BR-121).</param>
/// <param name="CanCheckIn">Whether the guest may be checked in today (BR-06). Walk-in is allowed (B-01).</param>
/// <param name="CanCheckOut">Whether the guest may be checked out (BR-09).</param>
/// <param name="CanCancel">Whether the booking may be cancelled (BR-11, BR-122).</param>
/// <param name="CanReschedule">Whether dates and room may be changed (BR-14, BR-123).</param>
public sealed record BookingActionsDto(
    bool CanConfirm,
    bool CanCheckIn,
    bool CanCheckOut,
    bool CanCancel,
    bool CanReschedule);
