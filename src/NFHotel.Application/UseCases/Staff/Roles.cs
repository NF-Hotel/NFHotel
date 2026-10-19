
namespace NFHotel.Application.UseCases.Staff;


/// <summary>
/// Definerer systemets fire roller ud fra den godkendte RBAC-matrix.
/// </summary>
public static class Roles
{

    // definerer et fast rollenavn som kan genbruges andre steder i systemet.
    /// <summary>
    /// Rollen for en gæst.
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// Rollen for en receptionist.
    /// </summary>
    public const string Receptionist = "Receptionist";

    /// <summary>
    /// Rollen for en manager.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Rollen for rengøringspersonale.
    /// </summary>
    public const string Housekeeping = "Housekeeping";
}
