using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IStaffRepository"/>.</summary>
public sealed class StaffRepository : Repository<StaffMember, int>, IStaffRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The context.</param>
    public StaffRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<StaffMember?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var key = StaffMember.NormalizeUserName(userName);
        return await Context.StaffMembers
            .FirstOrDefaultAsync(member => member.UserName == key, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        await Context.StaffMembers.AnyAsync(cancellationToken).ConfigureAwait(false);
}
