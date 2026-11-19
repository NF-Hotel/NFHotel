namespace NFHotel.Application.Ports;

/// <summary>
/// The number of coming bookings made on the public site that one email may hold (MIL-087, ADR-0051 decision 3). The
/// bookings are counted in the database; the use case reads only this number, apart from the counts per caller address
/// of <see cref="ISenderLimits"/> (review RC-428).
/// </summary>
public interface IPublicBookingEmailLimit
{
    #region Properties

    /// <summary>The number of coming bookings made on the public site that one email may hold.</summary>
    int ComingPublicBookingsPerEmail { get; }

    #endregion Properties
}
