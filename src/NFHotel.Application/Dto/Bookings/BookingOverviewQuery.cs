namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Query for the booking overview (BR-23, BR-24, BR-25, BR-32).
/// </summary>
/// <remarks>
/// The period filter is <b>always</b> active. In the old system the week and
/// month views did not filter at all, so the whole table was fetched and sorted in memory (BR-111).
/// </remarks>
/// <param name="PeriodStart">First date of the window, inclusive.</param>
/// <param name="PeriodEnd">Last date of the window, exclusive.</param>
/// <param name="SearchText">Free text on guest name or room number (BR-23, BR-25). <c>null</c> for all.</param>
/// <param name="SortBy">The field to sort on (BR-32).</param>
/// <param name="Direction">Sorteringsretning.</param>
/// <param name="IncludeCancelled">Whether cancelled bookings are included (the soft delete of BR-13).</param>
public sealed record BookingOverviewQuery(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? SearchText = null,
    BookingSortField SortBy = BookingSortField.StartDate,
    SortDirection Direction = SortDirection.Ascending,
    bool IncludeCancelled = false);
