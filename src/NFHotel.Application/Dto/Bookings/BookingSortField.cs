namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Felt der kan sorteres på i bookingoversigten (BR-32).
/// </summary>
/// <remarks>
/// En enum og ikke en streng: en tastefejl i et kolonnenavn skal være en compilerfejl, ikke
/// en tavst usorteret liste.
/// </remarks>
public enum BookingSortField
{
    /// <summary>Bookingnummer — svarer til id-rækkefølgen (BR-109).</summary>
    BookingNumber = 0,

    /// <summary>Gæstens fulde navn (BR-25).</summary>
    GuestName = 1,

    /// <summary>Værelsesnummer.</summary>
    RoomNumber = 2,

    /// <summary>Ankomstdato.</summary>
    StartDate = 3,

    /// <summary>Booket afrejsedato.</summary>
    EndDate = 4,

    /// <summary>Bookingens status.</summary>
    Status = 5,

    /// <summary>Antal overnatninger (BR-117).</summary>
    Nights = 6
}