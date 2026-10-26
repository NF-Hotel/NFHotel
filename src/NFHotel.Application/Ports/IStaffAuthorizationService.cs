namespace NFHotel.Application.Ports;

/// <summary>
/// Defines operations for checking staff permissions.
/// </summary>
public interface IStaffAuthorizationService
{
    /// <summary>
    /// Checks asynchronously whether the current staff member has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>True if the staff member has permission; otherwise false.</returns>
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);
}
