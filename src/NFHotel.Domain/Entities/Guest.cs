// Copyright (c) 2026 Team6. All rights reserved. 
//  No warranty, explicit or implicit, provided.

using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Attributes;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A guest. Aggregate root.
/// </summary>
/// <remarks>
/// A pure domain entity — deliberately NOT a login or account entity.
/// The invariants are enforced in <see cref="Create"/> and <see cref="UpdateDetails"/>, so a
/// persisted, invalid guest cannot occur (BR-84 becomes structurally impossible). Each text is brought to its normal
/// form before the rules run, and the normal form is what is stored (MIL-086, ADR-0050).
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

    /// <summary>
    /// Longest allowed passport number (A-10, MIL-086): a document number of the machine readable zone has nine
    /// characters, and twenty leave room for the national forms without letting free text in.
    /// </summary>
    public const int MaxPassportNumberLength = 20;

    /// <summary>For EF Core. Use <see cref="Create"/> in code.</summary>
    private Guest()
    {
    }

    /// <summary>First name. Never empty; letters, marks and the signs of a name (BR-91, MIL-086).</summary>
    [Required(ErrorMessage = GuestErrors.FirstNameRequired)]
    [StringLength(MaxNameLength, ErrorMessage = GuestErrors.FirstNameTooLong)]
    [PersonName(ErrorMessage = GuestErrors.FirstNameInvalid)]
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Last name. Never empty; letters, marks and the signs of a name (BR-92, MIL-086).</summary>
    [Required(ErrorMessage = GuestErrors.LastNameRequired)]
    [StringLength(MaxNameLength, ErrorMessage = GuestErrors.LastNameTooLong)]
    [PersonName(ErrorMessage = GuestErrors.LastNameInvalid)]
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Email. One plain address (BR-93, MIL-086).</summary>
    [Required(ErrorMessage = GuestErrors.EmailRequired)]
    [EmailFormat(ErrorMessage = GuestErrors.EmailInvalid)]
    [StringLength(MaxEmailLength, ErrorMessage = GuestErrors.EmailTooLong)]
    public string Email { get; private set; } = string.Empty;

    /// <summary>Phone number. Never empty; 7 to 15 digits with separators (BR-94, MIL-086).</summary>
    [Required(ErrorMessage = GuestErrors.PhoneNumberRequired)]
    [StringLength(MaxPhoneNumberLength, ErrorMessage = GuestErrors.PhoneNumberTooLong)]
    [PhoneNumber(ErrorMessage = GuestErrors.PhoneNumberInvalid)]
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Country. Never empty; a name of <see cref="Countries"/> (BR-95, MIL-086).</summary>
    [Required(ErrorMessage = GuestErrors.CountryRequired)]
    [StringLength(MaxCountryLength, ErrorMessage = GuestErrors.CountryTooLong)]
    [CountryName(ErrorMessage = GuestErrors.CountryInvalid)]
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Passport number. Optional; when given, ASCII letters and digits (BR-57, MIL-086).
    /// </summary>
    /// <remarks>
    /// Sensitive: must not be used as a search or sort key (B-09, D-04). The domain does not know
    /// the encryption — that is the responsibility of the infrastructure.
    /// </remarks>
    [StringLength(MaxPassportNumberLength, ErrorMessage = GuestErrors.PassportNumberTooLong)]
    [PassportNumber(ErrorMessage = GuestErrors.PassportNumberInvalid)]
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
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="email">Email. One plain address.</param>
    /// <param name="phoneNumber">Phone number.</param>
    /// <param name="country">Country, a name of <see cref="Countries"/>.</param>
    /// <param name="passportNumber">Passport number. Optional; white space only means none.</param>
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
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="email">Email.</param>
    /// <param name="phoneNumber">Phone number.</param>
    /// <param name="country">Country.</param>
    /// <param name="passportNumber">Passport number. Optional.</param>
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
    /// Brings each text to its normal form, enforces the invariants and writes the values. Shared by
    /// <see cref="Create"/> and <see cref="UpdateDetails"/>, so the rule exists in one place only.
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
            FirstName = TextNormalForm.Normalize(firstName),
            LastName = TextNormalForm.Normalize(lastName),
            Email = TextNormalForm.Normalize(email),
            PhoneNumber = TextNormalForm.Normalize(phoneNumber),
            Country = TextNormalForm.Normalize(country),
            PassportNumber = TextNormalForm.NormalizeOptional(passportNumber),
        };

        EntityValidation.ThrowIfInvalid(candidate);

        FirstName = candidate.FirstName;
        LastName = candidate.LastName;
        Email = candidate.Email;
        PhoneNumber = candidate.PhoneNumber;
        Country = candidate.Country;
        PassportNumber = candidate.PassportNumber;
    }
}
