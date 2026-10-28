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

    /// <summary>Lists all accounts, ordered by user name (ADR-0017).</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The accounts.</returns>
    Task<IReadOnlyList<StaffMember>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Tells whether an active Manager other than the given account exists, so the last one is never lost.</summary>
    /// <param name="excludingId">The id of the account that is about to be disabled.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>True when another active Manager exists.</returns>
    Task<bool> AnotherActiveManagerExistsAsync(int excludingId, CancellationToken cancellationToken = default);
}
