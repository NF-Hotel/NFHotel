namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// De statusovergange bookingoversigten kan udføre på én booking.
/// </summary>
/// <remarks>
/// Svarer én-til-én til <c>BookingActionsDto</c>'s fem felter minus <c>CanReschedule</c>,
/// der kræver en formular med datoer og rum og derfor hører til omlægningsskærmen.
/// <para>
/// Enum og ikke en streng: en knap der peger på en overgang der ikke findes, skal være en
/// compilerfejl, ikke et tavst ingenting.
/// </para>
/// </remarks>
public enum BookingAction
{
    /// <summary>Bekræft bookingen (BR-02).</summary>
    Confirm = 0,

    /// <summary>Tjek gæsten ind (BR-06).</summary>
    CheckIn = 1,

    /// <summary>Tjek gæsten ud (BR-09).</summary>
    CheckOut = 2,

    /// <summary>Annullér bookingen (BR-11). Kræver en bekræftelse først (BR-04).</summary>
    Cancel = 3
}