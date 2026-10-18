namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The role of a member of staff in the back office (Access-Control-Matrix.md, ADR-0005). The Guest role is
/// left to a later milestone.
/// </summary>
public enum StaffRole
{
    /// <summary>Handles guests and bookings.</summary>
    Receptionist = 1,

    /// <summary>Has extended administrative rights.</summary>
    Manager = 2,

    /// <summary>Handles cleaning and service of rooms.</summary>
    Housekeeping = 3
}