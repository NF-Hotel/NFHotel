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

    /// <summary>True when the member has to choose a new password before anything else (ADR-0015).</summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>
    /// Rises whenever the password changes or the member is disabled or enabled. A token carries the value it was
    /// issued with, so an older token no longer matches and its session has ended (ADR-0015).
    /// </summary>
    public int SecurityStamp { get; private set; } = 1;

    /// <summary>Normalises a user name the way the entity stores it: trimmed and in lower case.</summary>
    /// <param name="userName">The name as typed.</param>
    /// <returns>The stored form; empty for a missing name.</returns>
    public static string NormalizeUserName(string? userName) => (userName ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Creates an active member.</summary>
    /// <param name="userName">The user name; trimmed and lower-cased.</param>
    /// <param name="passwordHash">The hash of the password.</param>
    /// <param name="role">The role.</param>
    /// <param name="mustChangePassword">True when the member must choose a new password at the first sign-in.</param>
    /// <returns>The new member.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static StaffMember Create(string userName, string passwordHash, StaffRole role, bool mustChangePassword = false)
    {
        var member = new StaffMember
        {
            UserName = NormalizeUserName(userName),
            PasswordHash = passwordHash ?? string.Empty,
            Role = role,
            IsActive = true,
            MustChangePassword = mustChangePassword,
        };

        EntityValidation.ThrowIfInvalid(member);
        return member;
    }

    /// <summary>Stops the member from signing in and ends the open sessions. Calling it twice changes nothing.</summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SecurityStamp++;
    }

    /// <summary>Lets a disabled member sign in again, with a new password to choose. Calling it twice changes nothing.</summary>
    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        MustChangePassword = true;
        SecurityStamp++;
    }

    /// <summary>The member has chosen a new password: stores its hash, clears the duty to change it, ends older sessions.</summary>
    /// <param name="passwordHash">The hash of the new password.</param>
    /// <exception cref="DomainException">The hash is empty or too long.</exception>
    public void ChangePassword(string passwordHash) => SetPassword(passwordHash, mustChange: false);

    /// <summary>A Manager sets a temporary password: stores its hash, requires a change at the next sign-in, ends older sessions.</summary>
    /// <param name="passwordHash">The hash of the temporary password.</param>
    /// <exception cref="DomainException">The hash is empty or too long.</exception>
    public void ResetPassword(string passwordHash) => SetPassword(passwordHash, mustChange: true);

    private void SetPassword(string passwordHash, bool mustChange)
    {
        var previous = PasswordHash;
        PasswordHash = passwordHash ?? string.Empty;
        try
        {
            EntityValidation.ThrowIfInvalid(this);
        }
        catch (DomainException)
        {
            PasswordHash = previous;
            throw;
        }

        MustChangePassword = mustChange;
        SecurityStamp++;
    }
}
