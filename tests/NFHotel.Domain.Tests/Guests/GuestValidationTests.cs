using System.Text;

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
    private const string ValidCountry = "Denmark";
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
    /// BR-94, MIL-086: a phone number is an optional plus and 7 to 15 digits with spaces, hyphens and round brackets
    /// between them; text that is not a number gives <c>phone_number_invalid</c>.
    /// </summary>
    [Theory]
    [InlineData("12345678", true)]
    [InlineData("+45 12 34 56 78", true)]
    [InlineData("+1 (555) 123-4567", true)]
    [InlineData("ikke et nummer", false)]
    [InlineData("123456", false)]
    public void Validate_PhoneNumberFormat_FollowsTheRuleOfTheField(string phoneNumber, bool expected)
    {
        // Arrange & Act
        var errors = ErrorsOf(ValidFirstName, ValidLastName, ValidEmail, phoneNumber, ValidCountry);

        // Assert
        string[] expectedErrors = expected ? [] : [GuestErrors.PhoneNumberInvalid];
        Assert.Equal(expectedErrors, errors);
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
    /// BR-55, BR-93, MIL-086: the email is one plain address that a parser reads, with a domain of two or more labels.
    /// The shapes that the old rule let through (an at sign and a dot anywhere) are refused.
    /// </summary>
    [Theory]
    [InlineData("valdemar@nfhotel.dk", true)]
    [InlineData("a@b.dk", true)]
    [InlineData("a@b.c", true)]
    [InlineData(".@", false)]
    [InlineData("valdemar.nfhotel.dk", false)]
    [InlineData("valdemar@nfhoteldk", false)]
    [InlineData("valdemar", false)]
    [InlineData("Valdemar <valdemar@nfhotel.dk>", false)]
    [InlineData("valdemar@nfhotel.dk\r\nBcc: eve@example.com", false)]
    public void Email_AnyShape_MustBeOnePlainAddress(string email, bool expected)
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
    [InlineData("valdemar@@nfhotel.dk")]
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
    /// A-10: the limit itself is allowed. The test guards against the classic off-by-one, where the EF configuration
    /// and the domain disagree by one character. Each value meets the rule of its field, so only the length is tested;
    /// a country has no length of its own, because it is a name of the list (MIL-086).
    /// </summary>
    [Fact]
    public void Validate_EveryFieldIsExactlyAtItsMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var firstName = new string('a', Guest.MaxNameLength);
        var lastName = new string('b', Guest.MaxNameLength);
        var email = EmailOfLength(Guest.MaxEmailLength);
        string phoneNumber = PhoneNumberOfLength(Guest.MaxPhoneNumberLength);
        var passportNumber = new string('9', Guest.MaxPassportNumberLength);

        // Act
        IReadOnlyList<string> errors = ErrorsOf(firstName, lastName, email, phoneNumber, ValidCountry, passportNumber);

        // Assert
        Assert.Empty(errors);
        Assert.Equal(20, Guest.MaxPassportNumberLength);
    }

    /// <summary>
    /// A-10: one character over the limit is rejected by Domain, not first by the database as a
    /// <c>DbUpdateException</c>. Each field has its own code of length.
    /// </summary>
    [Fact]
    public void Validate_EveryFieldExceedsItsMaxLength_ReturnsOneLengthErrorPerField()
    {
        // Arrange
        var firstName = new string('a', Guest.MaxNameLength + 1);
        var lastName = new string('b', Guest.MaxNameLength + 1);
        var email = EmailOfLength(Guest.MaxEmailLength + 1);
        string phoneNumber = PhoneNumberOfLength(Guest.MaxPhoneNumberLength + 1);
        var passportNumber = new string('9', Guest.MaxPassportNumberLength + 1);

        // Act
        IReadOnlyList<string> errors = ErrorsOf(firstName, lastName, email, phoneNumber, ValidCountry, passportNumber);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(GuestErrors.FirstNameTooLong, error),
            error => Assert.Equal(GuestErrors.LastNameTooLong, error),
            error => Assert.Equal(GuestErrors.EmailTooLong, error),
            error => Assert.Equal(GuestErrors.PhoneNumberTooLong, error),
            error => Assert.Equal(GuestErrors.PassportNumberTooLong, error));
    }

    /// <summary>A-10, MIL-086: a country over its length is also no name of the list, so it gets both codes.</summary>
    [Fact]
    public void Validate_CountryExceedsItsMaxLength_ReturnsTheLengthAndTheListCode()
    {
        // Arrange
        string country = new('c', Guest.MaxCountryLength + 1);

        // Act
        IReadOnlyList<string> errors = ErrorsOf(ValidFirstName, ValidLastName, ValidEmail, ValidPhoneNumber, country);

        // Assert
        Assert.Equal(new[] { GuestErrors.CountryTooLong, GuestErrors.CountryInvalid }, errors);
    }

    /// <summary>
    /// BR-57: the passport number is optional and not validated beyond its length — neither null nor
    /// an empty string is an error.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
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

    #region The rules of the fields (MIL-086, ADR-0050)

    /// <summary>MIL-086: each field that has a rule of its characters answers its own code, all at once (BR-96).</summary>
    [Fact]
    public void Validate_EveryFieldBreaksTheRuleOfItsCharacters_ReturnsEachCode()
    {
        // Arrange & Act
        IReadOnlyList<string> errors = ErrorsOf("Anna1", "<b>", "anna@example", "12 ab", "Danmark", "AB-123");

        // Assert
        Assert.Equal(
            new[]
            {
                GuestErrors.FirstNameInvalid,
                GuestErrors.LastNameInvalid,
                GuestErrors.EmailInvalid,
                GuestErrors.PhoneNumberInvalid,
                GuestErrors.CountryInvalid,
                GuestErrors.PassportNumberInvalid,
            },
            errors);
    }

    /// <summary>MIL-086: every text is trimmed and composed before its rule, and the stored text is the normalised one.</summary>
    [Fact]
    public void Create_TextsWithWhiteSpaceAndDecomposedLetters_StoresTheNormalForm()
    {
        // Arrange & Act
        Guest guest = Guest.Create(" Jose\u0301 ", "\tHolm\n", " valdemar@nfhotel.dk ", " +45 12 34 56 78 ", " Co\u0302te d'Ivoire ", " AB123 ");

        // Assert
        Assert.Equal("Jos\u00E9", guest.FirstName);
        Assert.Equal("Holm", guest.LastName);
        Assert.Equal("valdemar@nfhotel.dk", guest.Email);
        Assert.Equal("+45 12 34 56 78", guest.PhoneNumber);
        Assert.Equal("C\u00F4te d'Ivoire", guest.Country);
        Assert.Equal("AB123", guest.PassportNumber);
    }

    /// <summary>MIL-086: a passport number of white space only is no passport number.</summary>
    [Fact]
    public void Create_PassportNumberOfWhiteSpace_StoresNone()
    {
        // Arrange & Act
        Guest guest = Guest.Create(ValidFirstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry, "   ");

        // Assert
        Assert.Null(guest.PassportNumber);
    }

    /// <summary>
    /// MIL-086 decision 5: markup and query text are data. A name made of letters, spaces and apostrophes is stored as
    /// it is; anything with other signs is refused with the code of the field, never stripped.
    /// </summary>
    [Theory]
    [InlineData("x' OR 'x", true)]
    [InlineData("Robert'); DROP TABLE guests;--", false)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("<img src=x onerror=alert(1)>", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("%s%s%n", false)]
    [InlineData("{{7*7}}", false)]
    public void Create_HostileFirstName_IsStoredUnchangedOrRefused(string firstName, bool stored)
    {
        // Arrange & Act
        IReadOnlyList<string> errors = ErrorsOf(firstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry);

        // Assert
        if (stored)
        {
            Assert.Empty(errors);
            Assert.Equal(firstName, Guest.Create(firstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry, null).FirstName);
        }
        else
        {
            Assert.Equal(GuestErrors.FirstNameInvalid, Assert.Single(errors));
        }
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
            () => guest.UpdateDetails("Ny", "Person", "ugyldig-email", "87654321", "Sweden", null));

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
        guest.UpdateDetails("Ny", "Person", "ny@person.dk", "87654321", "Sweden", null);

        // Assert
        Assert.Equal("Ny", guest.FirstName);
        Assert.Equal("Person", guest.LastName);
        Assert.Equal("ny@person.dk", guest.Email);
        Assert.Equal("87654321", guest.PhoneNumber);
        Assert.Equal("Sweden", guest.Country);
        Assert.Null(guest.PassportNumber);
    }

    /// <summary>
    /// Builds a format-valid email of an exact length, so the length rule can be tested
    /// without at the same time triggering the format error (BR-55).
    /// </summary>
    private static string EmailOfLength(int length)
    {
        const string Domain = "example.com";
        int local = Math.Min(64, length - Domain.Length - 1);
        int rest = length - Domain.Length - 1 - local;

        // What does not fit before the at sign becomes labels of up to 60 letters in front of the domain.
        StringBuilder text = new StringBuilder(new string('a', local)).Append('@');
        while (rest > 0)
        {
            int label = Math.Min(60, rest - 1);
            text.Append('b', label).Append('.');
            rest -= label + 1;
        }

        return text.Append(Domain).ToString();
    }

    /// <summary>
    /// Builds a phone number of an exact length that meets the rule of the field: eight digits with spaces between the
    /// first two and the rest, so the length can be tested without the format error (MIL-086).
    /// </summary>
    private static string PhoneNumberOfLength(int length) => $"12{new string(' ', length - 8)}345678";

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
