using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the staff accounts (ADR-0005).</summary>
public interface IStaffRepository : IRepository<StaffMember, int>
{
    /// <summary>Finds a member by the user name, in any case.</summary>
    /// <param name="userName">The name as typed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The member, or <c>null</c>.</returns>
    Task<StaffMember?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>Tells whether any member exists, so the first account can be created once.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>True when the table has a row.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
