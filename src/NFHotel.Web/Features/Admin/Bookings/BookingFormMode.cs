namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// Hvad bookingformularen er åbnet for.
/// </summary>
/// <remarks>
/// De to tilstande deler datovalg og rumsøgning, men rammer hver sit use case:
/// <c>CreateAsync</c> og <c>RescheduleAsync</c>. De er bevidst ikke slået sammen — en
/// omlægning må ikke kunne skifte gæst, og en oprettelse har ingen booking at udelade fra
/// tilgængelighedsopslaget.
/// </remarks>
public enum BookingFormMode
{
    /// <summary>Opret en ny booking (BR-01).</summary>
    Create = 0,

    /// <summary>Flyt en eksisterende booking til nye datoer eller et nyt rum (BR-14).</summary>
    Reschedule = 1
}

/// <summary>
/// Om bookingen skal knyttes til en eksisterende gæst eller til en der oprettes nu.
/// </summary>
/// <remarks>
/// Modsvarer BR-47: præcis én af <c>GuestId</c> og <c>NewGuest</c> må være udfyldt på
/// <c>CreateBookingRequest</c>. Valget her afgør hvilken af de to der sendes med, så
/// formularen aldrig kan sende begge.
/// </remarks>
public enum BookingGuestMode
{
    /// <summary>Vælg en gæst der allerede findes.</summary>
    Existing = 0,

    /// <summary>Opret gæsten som en del af bookingen.</summary>
    New = 1
}