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

    /// <summary>A Manager set or changed the nightly base rate of a room size (MIL-055).</summary>
    RoomRateChanged = 10,

    /// <summary>A Manager saved the pricing settings (MIL-055).</summary>
    PricingSettingsChanged = 11,

    /// <summary>A Manager approved a price change of a room size (MIL-055).</summary>
    PriceChangeApproved = 12,

    /// <summary>A Manager reverted a price change of a room size (MIL-055).</summary>
    PriceChangeReverted = 13,

    /// <summary>A Manager took a service note of a room back from the history.</summary>
    RoomServiceNoteReopened = 14,
}
