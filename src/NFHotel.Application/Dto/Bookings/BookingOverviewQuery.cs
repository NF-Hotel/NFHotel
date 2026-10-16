namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Forespørgsel til bookingoversigten (BR-23, BR-24, BR-25, BR-32).
/// </summary>
/// <remarks>
/// Periodefilteret er <b>altid</b> aktivt. I det gamle system filtrerede uge- og
/// månedsvisningen slet ikke, så hele tabellen blev hentet og sorteret i hukommelsen (BR-111).
/// </remarks>
/// <param name="PeriodStart">Første dato i vinduet, inklusiv.</param>
/// <param name="PeriodEnd">Sidste dato i vinduet, eksklusiv.</param>
/// <param name="SearchText">Fritekst på gæstenavn eller værelsesnummer (BR-23, BR-25). <c>null</c> for alle.</param>
/// <param name="SortBy">Feltet der sorteres på (BR-32).</param>
/// <param name="Direction">Sorteringsretning.</param>
/// <param name="IncludeCancelled">Om annullerede bookinger skal med (BR-13's soft-delete).</param>
public sealed record BookingOverviewQuery(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? SearchText = null,
    BookingSortField SortBy = BookingSortField.StartDate,
    SortDirection Direction = SortDirection.Ascending,
    bool IncludeCancelled = false);