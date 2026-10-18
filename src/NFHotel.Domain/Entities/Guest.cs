// Copyright (c) 2026 Team6. All rights reserved. 
//  No warranty, explicit or implicit, provided.

using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A guest. Aggregate root.
/// </summary>
/// <remarks>
/// A pure domain entity — deliberately NOT a login or account entity.
/// The invariants are enforced in <see cref="Create"/> and <see cref="UpdateDetails"/>, so a
/// persisted, invalid guest cannot occur (BR-84 becomes structurally impossible).
/// </remarks>
public sealed class Guest : Entity<int>
{
    /// <summary>Longest allowed first and last name (A-10). Mirrored by the EF configuration.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Longest allowed email (A-10).</summary>
    public const int MaxEmailLength = 100;

    /// <summary>Longest allowed phone number (A-10).</summary>
    public const int MaxPhoneNumberLength = 50;

    /// <summary>Longest allowed country name (A-10).</summary>
    public const int MaxCountryLength = 100;

    /// <summary>Longest allowed passport number (A-10).</summary>
    public const int MaxPassportNumberLength = 50;

    /// <summary>An email has an at sign and a dot, as in BR-52 to BR-56. Stricter formats are not required.</summary>
    public const string EmailPattern = @"^(?=[\s\S]*@)(?=[\s\S]*\.)[\s\S]*$";

    /// <summary>For EF Core. Use <see cref="Create"/> in code.</summary>
    private Guest()
    {
    }

    /// <summary>First name. Never empty or only whitespace (BR-91).</summary>
    [Required(ErrorMessage = GuestErrors.FirstNameRequired)]
    [StringLength(MaxNameLength, ErrorMessage = GuestErrors.FirstNameTooLong)]
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Last name. Never empty or only whitespace (BR-92).</summary>
    [Required(ErrorMessage = GuestErrors.LastNameRequired)]
    [StringLength(MaxNameLength, ErrorMessage = GuestErrors.LastNameTooLong)]
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Email. Always contains both '@' and '.' (BR-93).</summary>
    [Required(ErrorMessage = GuestErrors.EmailRequired)]
    [RegularExpression(EmailPattern, ErrorMessage = GuestErrors.EmailInvalid)]
    [StringLength(MaxEmailLength, ErrorMessage = GuestErrors.EmailTooLong)]
    public string Email { get; private set; } = string.Empty;

    /// <summary>Phone number. Never empty; no format requirement (BR-94).</summary>
    [Required(ErrorMessage = GuestErrors.PhoneNumberRequired)]
    [StringLength(MaxPhoneNumberLength, ErrorMessage = GuestErrors.PhoneNumberTooLong)]
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Country. Never empty (BR-95).</summary>
    [Required(ErrorMessage = GuestErrors.CountryRequired)]
    [StringLength(MaxCountryLength, ErrorMessage = GuestErrors.CountryTooLong)]
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Passport number. Optional and not validated beyond its length (BR-57).
    /// </summary>
    /// <remarks>
    /// Sensitive: must not be used as a search or sort key (B-09, D-04). The domain does not know
    /// the encryption — that is the responsibility of the infrastructure.
    /// </remarks>
    [StringLength(MaxPassportNumberLength, ErrorMessage = GuestErrors.PassportNumberTooLong)]
    public string? PassportNumber { get; private set; }

    /// <summary>
    /// Full name, "First name Last name". Used by the search and sorting of the Application layer
    /// (BR-25, BR-68, BR-69). A domain identity, not a display string: no culture,
    /// no format, no truncation.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Creates a new guest.
    /// </summary>
    /// <param name="firstName">Fornavn.</param>
    /// <param name="lastName">Efternavn.</param>
    /// <param name="email">Email. Must contain both '@' and '.'.</param>
    /// <param name="phoneNumber">Telefonnummer.</param>
    /// <param name="country">Land.</param>
    /// <param name="passportNumber">Pasnummer. Valgfrit.</param>
    /// <returns>A new, valid guest that is not yet persisted.</returns>
    /// <exception cref="DomainException">
    /// On a breach of BR-91 to BR-95 or a length limit (A-10). The error code is the first
    /// code; all codes are in <see cref="DomainException.Codes"/>.
    /// </exception>
    public static Guest Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber)
    {
        Guest guest = new();
        guest.Apply(firstName, lastName, email, phoneNumber, country, passportNumber);

        return guest;
    }

    /// <summary>
    /// Overwrites exactly the six editable fields. <see cref="Entity{TKey}.Id"/> is never changed (BR-72).
    /// </summary>
    /// <param name="firstName">Fornavn.</param>
    /// <param name="lastName">Efternavn.</param>
    /// <param name="email">E-mail.</param>
    /// <param name="phoneNumber">Telefonnummer.</param>
    /// <param name="country">Land.</param>
    /// <param name="passportNumber">Pasnummer. Valgfrit.</param>
    /// <exception cref="DomainException">The same invariants as <see cref="Create"/>.</exception>
    public void UpdateDetails(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber) =>
        Apply(firstName, lastName, email, phoneNumber, country, passportNumber);

    /// <summary>
    /// Enforces the invariants and writes the values. Shared by <see cref="Create"/> and
    /// <see cref="UpdateDetails"/>, so the rule exists in one place only.
    /// </summary>
    private void Apply(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber)
    {
        Guest candidate = new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            Country = country,
            PassportNumber = passportNumber,
        };

        EntityValidation.ThrowIfInvalid(candidate);

        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        Country = country;
        PassportNumber = passportNumber;
    }
}