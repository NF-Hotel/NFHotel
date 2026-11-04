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

    /// <summary>The longest first name, and the longest last name.</summary>
    public const int MaxNameLength = 50;

    /// <summary>The longest e-mail address (RFC 5321).</summary>
    public const int MaxEmailLength = 254;

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

    /// <summary>The first name, or <c>null</c> for an account that has no name yet. It is set together with <see cref="LastName"/>.</summary>
    [StringLength(MaxNameLength, ErrorMessage = StaffErrors.FirstNameTooLong)]
    public string? FirstName { get; private set; }

    /// <summary>The last name, or <c>null</c> for an account that has no name yet. It is set together with <see cref="FirstName"/>.</summary>
    [StringLength(MaxNameLength, ErrorMessage = StaffErrors.LastNameTooLong)]
    public string? LastName { get; private set; }

    /// <summary>
    /// The e-mail address to reach the member, in lower case, or <c>null</c>. It is contact data only: the member signs in
    /// with <see cref="UserName"/>.
    /// </summary>
    [EmailAddress(ErrorMessage = StaffErrors.EmailInvalid)]
    [StringLength(MaxEmailLength, ErrorMessage = StaffErrors.EmailTooLong)]
    public string? Email { get; private set; }

    /// <summary>True when the account has a first and a last name.</summary>
    public bool HasName => FirstName is not null && LastName is not null;

    /// <summary>What the member may do.</summary>
    [EnumDataType(typeof(StaffRole), ErrorMessage = StaffErrors.RoleInvalid)]
    public StaffRole Role { get; private set; }

    /// <summary>False when the member may no longer sign in.</summary>
    public bool IsActive { get; private set; }

    /// <summary>True when the member has to choose a new password before anything else (ADR-0017).</summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>
    /// Rises whenever the password changes or the member is disabled or enabled. A token carries the value it was
    /// issued with, so an older token no longer matches and its session has ended (ADR-0017).
    /// </summary>
    public int SecurityStamp { get; private set; } = 1;

    /// <summary>Normalises a user name the way the entity stores it: trimmed and in lower case.</summary>
    /// <param name="userName">The name as typed.</param>
    /// <returns>The stored form; empty for a missing name.</returns>
    public static string NormalizeUserName(string? userName) => (userName ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Normalises an e-mail address the way the entity stores it: trimmed and in lower case.</summary>
    /// <param name="email">The address as typed.</param>
    /// <returns>The stored form; <c>null</c> for a missing or blank address.</returns>
    public static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }

    /// <summary>Creates an active member.</summary>
    /// <param name="userName">The user name; trimmed and lower-cased.</param>
    /// <param name="passwordHash">The hash of the password.</param>
    /// <param name="role">The role.</param>
    /// <param name="mustChangePassword">True when the member must choose a new password at the first sign-in.</param>
    /// <param name="firstName">The first name, or <c>null</c>; given together with <paramref name="lastName"/>.</param>
    /// <param name="lastName">The last name, or <c>null</c>; given together with <paramref name="firstName"/>.</param>
    /// <param name="email">The e-mail address, or <c>null</c>.</param>
    /// <returns>The new member.</returns>
    /// <exception cref="DomainException">A field breaks an invariant, or only one of the two names is given.</exception>
    public static StaffMember Create(
        string userName,
        string passwordHash,
        StaffRole role,
        bool mustChangePassword = false,
        string? firstName = null,
        string? lastName = null,
        string? email = null)
    {
        var (first, last) = (Trimmed(firstName), Trimmed(lastName));
        if (first is not null || last is not null)
        {
            ThrowIfNameMissing(first, last);
        }

        var member = new StaffMember
        {
            UserName = NormalizeUserName(userName),
            PasswordHash = passwordHash ?? string.Empty,
            Role = role,
            IsActive = true,
            MustChangePassword = mustChangePassword,
            FirstName = first,
            LastName = last,
            Email = NormalizeEmail(email),
        };

        EntityValidation.ThrowIfInvalid(member);
        return member;
    }

    /// <summary>Sets the first and the last name. Both are required, because a name is shown as the two together.</summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <exception cref="DomainException">A name is empty or too long; the names stay as they were.</exception>
    public void Rename(string? firstName, string? lastName)
    {
        var (first, last) = (Trimmed(firstName), Trimmed(lastName));
        ThrowIfNameMissing(first, last);
        SetContact(first, last, Email);
    }

    /// <summary>Sets the e-mail address, or clears it with an empty one.</summary>
    /// <param name="email">The address as typed.</param>
    /// <exception cref="DomainException">The address is not valid or too long; it stays as it was.</exception>
    public void ChangeEmail(string? email) => SetContact(FirstName, LastName, NormalizeEmail(email));

    /// <summary>
    /// Gives the member another role and ends the open sessions, because a token carries the role it was issued with.
    /// Giving the same role changes nothing.
    /// </summary>
    /// <param name="role">The new role.</param>
    /// <exception cref="DomainException">The role is not one of <see cref="StaffRole"/>.</exception>
    public void ChangeRole(StaffRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(StaffErrors.RoleInvalid);
        }

        if (role == Role)
        {
            return;
        }

        Role = role;
        SecurityStamp++;
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

    private static string? Trimmed(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static void ThrowIfNameMissing(string? first, string? last)
    {
        string[] codes =
        [
            .. first is null ? [StaffErrors.FirstNameRequired] : Array.Empty<string>(),
            .. last is null ? [StaffErrors.LastNameRequired] : Array.Empty<string>(),
        ];
        if (codes.Length > 0)
        {
            throw new DomainException(codes);
        }
    }

    // Sets the contact data and puts it back when the result breaks an invariant.
    private void SetContact(string? firstName, string? lastName, string? email)
    {
        var previous = (FirstName, LastName, Email);
        (FirstName, LastName, Email) = (firstName, lastName, email);
        try
        {
            EntityValidation.ThrowIfInvalid(this);
        }
        catch (DomainException)
        {
            (FirstName, LastName, Email) = previous;
            throw;
        }
    }

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
