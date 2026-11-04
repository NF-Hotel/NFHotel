using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A shift that a Manager plans: when it is, which role it needs, and who works it (MIL-060). A shift without a member
/// of staff is open for the Manager to fill.
/// </summary>
public sealed class Shift : Entity<int>
{
    /// <summary>The longest shift.</summary>
    public static readonly TimeSpan MaxLength = TimeSpan.FromHours(16);

    private Shift()
    {
    }

    /// <summary>When it starts.</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>When it ends.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>The role it needs.</summary>
    public StaffRole Role { get; private set; }

    /// <summary>The member of staff who works it, or <c>null</c> while nobody does.</summary>
    public int? AssigneeId { get; private set; }

    /// <summary>Plans a shift.</summary>
    /// <param name="startsAt">When it starts.</param>
    /// <param name="endsAt">When it ends.</param>
    /// <param name="role">The role it needs.</param>
    /// <param name="assigneeId">Who works it, or <c>null</c>.</param>
    /// <returns>The shift.</returns>
    /// <exception cref="DomainException">The times or the role are not valid.</exception>
    public static Shift Plan(DateTimeOffset startsAt, DateTimeOffset endsAt, StaffRole role, int? assigneeId)
    {
        var shift = new Shift();
        shift.Change(startsAt, endsAt, role, assigneeId);
        return shift;
    }

    /// <summary>Changes the times, the role or the member of staff.</summary>
    /// <param name="startsAt">When it starts.</param>
    /// <param name="endsAt">When it ends.</param>
    /// <param name="role">The role it needs.</param>
    /// <param name="assigneeId">Who works it, or <c>null</c>.</param>
    /// <exception cref="DomainException">The times or the role are not valid; nothing changes then.</exception>
    public void Change(DateTimeOffset startsAt, DateTimeOffset endsAt, StaffRole role, int? assigneeId)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException(ShiftErrors.EndBeforeStart);
        }

        if (endsAt - startsAt > MaxLength)
        {
            throw new DomainException(ShiftErrors.TooLong);
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException(ShiftErrors.RoleInvalid);
        }

        if (assigneeId is <= 0)
        {
            throw new DomainException(ShiftErrors.AssigneeInvalid);
        }

        (StartsAt, EndsAt, Role, AssigneeId) = (startsAt, endsAt, role, assigneeId);
    }

    /// <summary>Gives the shift to another member of staff, after a trade.</summary>
    /// <param name="assigneeId">The member of staff.</param>
    /// <exception cref="DomainException">The key is not valid.</exception>
    public void HandTo(int assigneeId)
    {
        if (assigneeId <= 0)
        {
            throw new DomainException(ShiftErrors.AssigneeInvalid);
        }

        AssigneeId = assigneeId;
    }

    /// <summary>Tells whether the shift and the time from <paramref name="startsAt"/> to <paramref name="endsAt"/> share a moment.</summary>
    /// <param name="startsAt">The start of the other time.</param>
    /// <param name="endsAt">The end of the other time.</param>
    /// <returns>True when they overlap; a shift that ends when the other starts does not.</returns>
    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) => StartsAt < endsAt && startsAt < EndsAt;
}
