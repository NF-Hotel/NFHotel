using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>The names of the staff accounts as the screens show them (MIL-060).</summary>
internal static class StaffNames
{
    /// <summary>The first and the last name of an account, or its user name while it has none.</summary>
    /// <param name="member">The account.</param>
    /// <returns>The name.</returns>
    public static string Of(StaffMember member) =>
        member.HasName ? $"{member.FirstName} {member.LastName}" : member.UserName;

    /// <summary>Reads the name of every account, by key.</summary>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The accounts by key.</returns>
    public static async Task<IReadOnlyDictionary<int, StaffMember>> ReadAsync(IStaffRepository staff, CancellationToken cancellationToken) =>
        (await staff.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(member => member.Id);

    /// <summary>The name of the account with the key, or <c>null</c> for no key or an account that is gone.</summary>
    /// <param name="members">The accounts by key.</param>
    /// <param name="id">The key.</param>
    /// <returns>The name, or <c>null</c>.</returns>
    public static string? Find(IReadOnlyDictionary<int, StaffMember> members, int? id) =>
        id is { } key && members.TryGetValue(key, out var member) ? Of(member) : null;
}
