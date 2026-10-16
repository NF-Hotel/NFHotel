using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A member of staff who may sign in to the back office. The entity holds the hash of the password, never the
/// password; making the hash is the job of an infrastructure port (ADR-0005).
/// </summary>
public sealed class StaffMember : Entity<int>
{
    /// <summary>The longest user name.</summary>
    public const int MaxUserNameLength = 50;

    /// <summary>The longest password hash that the column holds.</summary>
    public const int MaxPasswordHashLength = 200;

    private StaffMember()
    {
    }

    /// <summary>The name used to sign in, always in lower case so that the sign-in does not depend on case.</summary>
    [Required(ErrorMessage = StaffErrors.UserNameRequired)]
    [StringLength(MaxUserNameLength, ErrorMessage = StaffErrors.UserNameTooLong)]
    public string UserName { get; private set; } = string.Empty;

    /// <summary>The self-describing hash of the password.</summary>
    [Required(ErrorMessage = StaffErrors.PasswordHashRequired)]
    [StringLength(MaxPasswordHashLength, ErrorMessage = StaffErrors.PasswordHashRequired)]
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>What the member may do.</summary>
    [EnumDataType(typeof(StaffRole), ErrorMessage = StaffErrors.RoleInvalid)]
    public StaffRole Role { get; private set; }

    /// <summary>False when the member may no longer sign in.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Normalises a user name the way the entity stores it: trimmed and in lower case.</summary>
    /// <param name="userName">The name as typed.</param>
    /// <returns>The stored form; empty for a missing name.</returns>
    public static string NormalizeUserName(string? userName) => (userName ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Creates an active member.</summary>
    /// <param name="userName">The user name; trimmed and lower-cased.</param>
    /// <param name="passwordHash">The hash of the password.</param>
    /// <param name="role">The role.</param>
    /// <returns>The new member.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static StaffMember Create(string userName, string passwordHash, StaffRole role)
    {
        var member = new StaffMember
        {
            UserName = NormalizeUserName(userName),
            PasswordHash = passwordHash ?? string.Empty,
            Role = role,
            IsActive = true,
        };

        EntityValidation.ThrowIfInvalid(member);
        return member;
    }

    /// <summary>Stops the member from signing in. Calling it twice changes nothing.</summary>
    public void Deactivate() => IsActive = false;
}