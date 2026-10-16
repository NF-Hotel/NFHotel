using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Tests.Guests;

/// <summary>
/// Guest validation. Covers BR-46, BR-52 to BR-57, BR-60, BR-72, BR-84 and BR-91 to BR-96
/// as well as the length limits from A-10.
/// </summary>
/// <remarks>
/// The rules are tested through the helper <c>ErrorsOf</c> (which collects every error code, BR-96)
/// and through <see cref="Guest.Create"/> (whose exception code is the first, BR-46). Both ways share
/// the attributes of <c>Guest</c> (ADR-0002) and must therefore agree.
/// </remarks>
public class GuestValidationTests
{
    private const string ValidFirstName = "Valdemar";
    private const string ValidLastName = "Holm";
    private const string ValidEmail = "valdemar@nfhotel.dk";
    private const string ValidPhoneNumber = "+45 12 34 56 78";
    private const string ValidCountry = "Danmark";
    private const string ValidPassportNumber = "123456789";

    #region Required fields (BR-91 to BR-95)

    /// <summary>BR-52, BR-91: first name must be neither null, empty nor only whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Validate_FirstNameIsMissing_ReturnsFirstNameRequired(string? firstName)
    {
        // Arrange & Act
        var errors = ErrorsOf(firstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.FirstNameRequired, Assert.Single(errors));
    }

    /// <summary>BR-53, BR-92: last name must be neither null, empty nor only whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_LastNameIsMissing_ReturnsLastNameRequired(string? lastName)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, lastName, ValidEmail, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.LastNameRequired, Assert.Single(errors));
    }

    /// <summary>BR-93: email must be neither null, empty nor only whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailIsMissing_ReturnsEmailRequired(string? email)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.EmailRequired, Assert.Single(errors));
    }

    /// <summary>BR-54, BR-94: phone number must not be empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_PhoneNumberIsMissing_ReturnsPhoneNumberRequired(string? phoneNumber)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, ValidEmail, phoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.PhoneNumberRequired, Assert.Single(errors));
    }

    /// <summary>
    /// BR-94: the phone number deliberately has no format requirement — only that something is there.
    /// The rule must not be tightened without a decision; foreign guests have all formats.
    /// </summary>
    [Theory]
    [InlineData("12345678")]
    [InlineData("+45 12 34 56 78")]
    [InlineData("ikke et nummer")]
    public void Validate_PhoneNumberHasAnyNonBlankValue_ReturnsNoErrors(string phoneNumber)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, ValidEmail, phoneNumber, ValidCountry);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>BR-56, BR-95: country must not be empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_CountryIsMissing_ReturnsCountryRequired(string? country)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, ValidEmail, ValidPhoneNumber, country);

        // Assert
        Assert.Equal(GuestErrors.CountryRequired, Assert.Single(errors));
    }

    #endregion

    #region Email (BR-55, BR-93)

    /// <summary>
    /// BR-55, BR-93: the email must contain both '@' and '.'. The position is deliberately
    /// not checked — a stricter rule would break parity with the old system.
    /// </summary>
    [Theory]
    [InlineData("valdemar@nfhotel.dk", true)]
    [InlineData("a@b.c", true)]
    [InlineData(".@", true)] // absurd, but meets the rule exactly as it is formulated
    [InlineData("valdemar.nfhotel.dk", false)] // intet '@'
    [InlineData("valdemar@nfhoteldk", false)] // intet '.'
    [InlineData("valdemar", false)]
    public void Email_RequiresBothAtSignAndDot(string email, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-55, BR-93: a filled-in but wrongly formatted email gives <c>email_invalid</c>.</summary>
    [Theory]
    [InlineData("valdemar.nfhotel.dk")]
    [InlineData("valdemar@nfhoteldk")]
    public void Validate_EmailIsMalformed_ReturnsEmailInvalid(string email)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.EmailInvalid, Assert.Single(errors));
    }

    /// <summary>
    /// BR-96, A-10: the email field is the only one with two independent requirements, so an address that
    /// is both too long and wrongly formatted gives both codes. The other fields can only
    /// give one code at a time.
    /// </summary>
    [Fact]
    public void Validate_EmailIsBothMalformedAndTooLong_ReturnsBothCodes()
    {
        // Arrange
        var email = new string('a', Guest.MaxEmailLength + 1);

        // Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(GuestErrors.EmailInvalid, error),
            error => Assert.Equal(GuestErrors.EmailTooLong, error));
    }

    #endregion

    #region Length limits (A-10)

    /// <summary>
    /// A-10: the limit itself is allowed. The test guards against the classic off-by-one, where
    /// the EF configuration and the domain disagree by one character.
    /// </summary>
    [Fact]
    public void Validate_EveryFieldIsExactlyAtItsMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var firstName = new string('a', Guest.MaxNameLength);
        var lastName = new string('b', Guest.MaxNameLength);
        var email = EmailOfLength(Guest.MaxEmailLength);
        var phoneNumber = new string('1', Guest.MaxPhoneNumberLength);
        var country = new string('c', Guest.MaxCountryLength);
        var passportNumber = new string('9', Guest.MaxPassportNumberLength);

        // Act
        var errors = ErrorsOf(firstName, lastName, email, phoneNumber, country, passportNumber);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>
    /// A-10: one character over the limit is rejected by Domain — not first by the database as a
    /// <c>DbUpdateException</c>. All six fields each have their own code.
    /// </summary>
    [Fact]
    public void Validate_EveryFieldExceedsItsMaxLength_ReturnsOneLengthErrorPerField()
    {
        // Arrange
        var firstName = new string('a', Guest.MaxNameLength + 1);
        var lastName = new string('b', Guest.MaxNameLength + 1);
        var email = EmailOfLength(Guest.MaxEmailLength + 1);
        var phoneNumber = new string('1', Guest.MaxPhoneNumberLength + 1);
        var country = new string('c', Guest.MaxCountryLength + 1);
        var passportNumber = new string('9', Guest.MaxPassportNumberLength + 1);

        // Act
        var errors = ErrorsOf(firstName, lastName, email, phoneNumber, country, passportNumber);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(GuestErrors.FirstNameTooLong, error),
            error => Assert.Equal(GuestErrors.LastNameTooLong, error),
            error => Assert.Equal(GuestErrors.EmailTooLong, error),
            error => Assert.Equal(GuestErrors.PhoneNumberTooLong, error),
            error => Assert.Equal(GuestErrors.CountryTooLong, error),
            error => Assert.Equal(GuestErrors.PassportNumberTooLong, error));
    }

    /// <summary>
    /// BR-57: the passport number is optional and not validated beyond its length — neither null nor
    /// an empty string is an error.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(ValidPassportNumber)]
    public void Validate_PassportNumberIsOptional_ReturnsNoErrors(string? passportNumber)
    {
        // Arrange & Act
        var errors = ErrorsOf(
            ValidFirstName,
            ValidLastName,
            ValidEmail,
            ValidPhoneNumber,
            ValidCountry,
            passportNumber);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region All errors at once (BR-60, BR-96) and fail-fast (BR-46)

    /// <summary>
    /// BR-60, BR-96: <c>ErrorsOf</c> collects all errors in field order, so a form can
    /// show them at once instead of letting the user correct one field at a time.
    /// </summary>
    [Fact]
    public void Validate_EveryRequiredFieldIsBlank_ReturnsAllErrorsInFieldOrder()
    {
        // Arrange & Act
        var errors = ErrorsOf("  ", null, "", "\t", null);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(GuestErrors.FirstNameRequired, error),
            error => Assert.Equal(GuestErrors.LastNameRequired, error),
            error => Assert.Equal(GuestErrors.EmailRequired, error),
            error => Assert.Equal(GuestErrors.PhoneNumberRequired, error),
            error => Assert.Equal(GuestErrors.CountryRequired, error));
    }

    /// <summary>
    /// BR-46, BR-84: <c>Create</c> fails with the FIRST error as its code, so an invalid guest can never
    /// arise as an object — the old system ignored the validation result and saved
    /// alligevel.
    /// </summary>
    [Fact]
    public void Create_MultipleFieldsAreInvalid_ThrowsWithTheFirstErrorCode()
    {
        // Arrange & Act
        var exception = Assert.Throws<DomainException>(
            () => Guest.Create("  ", "  ", "ugyldig", "  ", "  ", null));

        // Assert
        Assert.Equal(GuestErrors.FirstNameRequired, exception.Code);
    }

    /// <summary>BR-46: valid details are written through unchanged.</summary>
    [Fact]
    public void Create_ValidInput_StoresEveryField()
    {
        // Arrange & Act
        var guest = Guest.Create(
            ValidFirstName,
            ValidLastName,
            ValidEmail,
            ValidPhoneNumber,
            ValidCountry,
            ValidPassportNumber);

        // Assert
        Assert.Equal(ValidFirstName, guest.FirstName);
        Assert.Equal(ValidLastName, guest.LastName);
        Assert.Equal(ValidEmail, guest.Email);
        Assert.Equal(ValidPhoneNumber, guest.PhoneNumber);
        Assert.Equal(ValidCountry, guest.Country);
        Assert.Equal(ValidPassportNumber, guest.PassportNumber);
    }

    /// <summary>
    /// BR-84: a rejected update must not leave the guest half overwritten —
    /// the invariants are checked BEFORE the fields are written.
    /// </summary>
    [Fact]
    public void UpdateDetails_InvalidInput_ThrowsAndLeavesGuestUnchanged()
    {
        // Arrange
        var guest = Guest.Create(ValidFirstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry, null);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => guest.UpdateDetails("Ny", "Person", "ugyldig-email", "87654321", "Sverige", null));

        // Assert
        Assert.Equal(GuestErrors.EmailInvalid, exception.Code);
        Assert.Equal(ValidFirstName, guest.FirstName);
        Assert.Equal(ValidEmail, guest.Email);
        Assert.Equal(ValidCountry, guest.Country);
    }

    /// <summary>BR-72: a valid update overwrites exactly the six editable fields.</summary>
    [Fact]
    public void UpdateDetails_ValidInput_OverwritesEditableFields()
    {
        // Arrange
        var guest = Guest.Create(
            ValidFirstName,
            ValidLastName,
            ValidEmail,
            ValidPhoneNumber,
            ValidCountry,
            ValidPassportNumber);

        // Act
        guest.UpdateDetails("Ny", "Person", "ny@person.dk", "87654321", "Sverige", null);

        // Assert
        Assert.Equal("Ny", guest.FirstName);
        Assert.Equal("Person", guest.LastName);
        Assert.Equal("ny@person.dk", guest.Email);
        Assert.Equal("87654321", guest.PhoneNumber);
        Assert.Equal("Sverige", guest.Country);
        Assert.Null(guest.PassportNumber);
    }

    /// <summary>
    /// Builds a format-valid email of an exact length, so the length rule can be tested
    /// without at the same time triggering the format error (BR-55).
    /// </summary>
    private static string EmailOfLength(int length) => $"a@b.{new string('c', length - 4)}";

    /// <summary>
    /// Builds a guest and returns every error code, or an empty list when the guest is valid.
    /// The rules are attributes on <see cref="Guest"/> (ADR-0002).
    /// </summary>
    private static IReadOnlyList<string> ErrorsOf(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        string? country,
        string? passportNumber = null)
    {
        try
        {
            _ = Guest.Create(firstName!, lastName!, email!, phoneNumber!, country!, passportNumber);

            return [];
        }
        catch (DomainException exception)
        {
            return exception.Codes;
        }
    }

    #endregion
}