namespace NFHotel.Domain.ValueObjects;

/// <summary>What an audit event of the staff accounts records (ADR-0017).</summary>
public enum StaffAuditAction
{
    /// <summary>A member of staff signed in.</summary>
    SignedIn = 1,

    /// <summary>A sign-in for an existing account failed.</summary>
    SignInFailed = 2,

    /// <summary>The sign-in of an account was blocked because its attempts were used up.</summary>
    LockedOut = 3,

    /// <summary>A member of staff changed their own password.</summary>
    PasswordChanged = 4,

    /// <summary>A change of the own password was refused (wrong current password or a rule).</summary>
    PasswordChangeFailed = 5,

    /// <summary>A Manager set a temporary password.</summary>
    PasswordReset = 6,

    /// <summary>An account was created.</summary>
    AccountCreated = 7,

    /// <summary>An account was disabled.</summary>
    AccountDisabled = 8,

    /// <summary>An account was enabled again.</summary>
    AccountEnabled = 9,
}
