
namespace NFHotel.Application.UseCases.Staff;


/// <summary>
/// Defines the four roles of the system from the approved RBAC matrix.
/// </summary>
public static class Roles
{

    // defines a fixed role name that can be reused elsewhere in the system.
    /// <summary>
    /// The role of a guest.
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// The role of a receptionist.
    /// </summary>
    public const string Receptionist = "Receptionist";

    /// <summary>
    /// The role of a manager.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// The role of the cleaning staff.
    /// </summary>
    public const string Housekeeping = "Housekeeping";
}
