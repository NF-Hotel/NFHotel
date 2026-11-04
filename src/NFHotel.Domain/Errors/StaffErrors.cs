namespace NFHotel.Domain.Errors;

/// <summary>Error codes of the staff account. The text belongs to the screen (A-09).</summary>
public static class StaffErrors
{
    /// <summary>The user name is empty.</summary>
    public const string UserNameRequired = "staff.user_name_required";

    /// <summary>The user name is longer than <see cref="Entities.StaffMember.MaxUserNameLength"/>.</summary>
    public const string UserNameTooLong = "staff.user_name_too_long";

    /// <summary>The password hash is empty.</summary>
    public const string PasswordHashRequired = "staff.password_hash_required";

    /// <summary>The role is not one of <see cref="ValueObjects.StaffRole"/>.</summary>
    public const string RoleInvalid = "staff.role_invalid";

    /// <summary>The first name is empty.</summary>
    public const string FirstNameRequired = "staff.first_name_required";

    /// <summary>The first name is longer than <see cref="Entities.StaffMember.MaxNameLength"/>.</summary>
    public const string FirstNameTooLong = "staff.first_name_too_long";

    /// <summary>The last name is empty.</summary>
    public const string LastNameRequired = "staff.last_name_required";

    /// <summary>The last name is longer than <see cref="Entities.StaffMember.MaxNameLength"/>.</summary>
    public const string LastNameTooLong = "staff.last_name_too_long";

    /// <summary>The e-mail address is not a valid address.</summary>
    public const string EmailInvalid = "staff.email_invalid";

    /// <summary>The e-mail address is longer than <see cref="Entities.StaffMember.MaxEmailLength"/>.</summary>
    public const string EmailTooLong = "staff.email_too_long";
}
