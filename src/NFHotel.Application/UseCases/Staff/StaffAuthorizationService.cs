using NFHotel.Application.Ports;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Staff;


/// <summary>
///  Service responsible for handling staff authorization.
/// </summary>
public sealed class StaffAuthorizationService 
{
    private readonly IStaffCallContext _context;
    private readonly IStaffRepository _staff;


    /// <summary>
    /// Initializes a new instance of the <see cref="StaffAuthorizationService"/> class. 
    /// </summary>
    public StaffAuthorizationService(IStaffCallContext context, IStaffRepository staff)
    {
        // Assigns the provided context to the private field.
        // Throws an ArgumentNullException if context is null.
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Assigns the provided staff repository to the private field.
        _staff = staff ?? throw new ArgumentNullException(nameof(staff));
    }


    /// <summary>
    /// Checks asynchronously whether the current staff member
    /// has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>True if the staff member has permission; otherwise false.</returns>
    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {

        // Checks whether the current staff member has a valid integer ID.
        // If no valid staff ID is available, access is denied.
        if (_context.StaffId is not int staffId)
        {
            return false;
        }

        // Retrieves the staff member's details from the repository using the staff ID.
        var member = await _staff.GetByIdAsync(staffId, cancellationToken);

        // Checks if the staff member exists and is active.
        if (member is null || !member.IsActive)
        {
            return false;
        }

        // Checks whether the staff member must change their password
        // before accessing other functionality.
        if (member.MustChangePassword &&
            permission != Permissions.StaffChangeOwnPassword)
        {
            return false;
        }

        // Checks whether the staff member's role has the required permission.
        return RolePermissions.HasPermission(member.Role, permission);
    }


}
