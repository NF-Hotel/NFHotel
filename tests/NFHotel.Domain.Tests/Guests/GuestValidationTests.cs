using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Tests.Guests;

/// <summary>
/// Gæstevalidering. Dækker BR-46, BR-52 til BR-57, BR-60, BR-72, BR-84 og BR-91 til BR-96
/// samt længdegrænserne fra A-10.
/// </summary>
/// <remarks>
/// Reglerne testes gennem <see cref="NFHotel.Domain.Entities.Guest.Validate"/> (som samler alle fejl, BR-96)
/// og gennem <see cref="Guest.Create"/> (som fejler på den første, BR-46). Begge veje deler
/// prædikater og skal derfor være enige.
/// </remarks>
public class GuestValidationTests
{
    private const string ValidFirstName = "Valdemar";
    private const string ValidLastName = "Holm";
    private const string ValidEmail = "valdemar@nfhotel.dk";
    private const string ValidPhoneNumber = "+45 12 34 56 78";
    private const string ValidCountry = "Danmark";
    private const string ValidPassportNumber = "123456789";

    // --------------------------------------------------------
    // PÅKRÆVEDE FELTER (BR-91 til BR-95)
    // --------------------------------------------------------

    /// <summary>BR-52, BR-91: fornavn må hverken være null, tomt eller kun whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Validate_FirstNameIsMissing_ReturnsFirstNameRequired(string? firstName)
    {
        // Arrange & Act
        var errors = Guest.Validate(firstName, ValidLastName, ValidEmail, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.FirstNameRequired, Assert.Single(errors));
    }

    /// <summary>BR-53, BR-92: efternavn må hverken være null, tomt eller kun whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_LastNameIsMissing_ReturnsLastNameRequired(string? lastName)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, lastName, ValidEmail, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.LastNameRequired, Assert.Single(errors));
    }

    /// <summary>BR-93: e-mail må hverken være null, tom eller kun whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailIsMissing_ReturnsEmailRequired(string? email)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.EmailRequired, Assert.Single(errors));
    }

    /// <summary>BR-54, BR-94: telefonnummer må ikke være tomt.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_PhoneNumberIsMissing_ReturnsPhoneNumberRequired(string? phoneNumber)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, ValidEmail, phoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.PhoneNumberRequired, Assert.Single(errors));
    }

    /// <summary>
    /// BR-94: telefonnummeret har bevidst intet formatkrav — kun at der står noget.
    /// Reglen må ikke strammes uden en beslutning; udenlandske gæster har alle formater.
    /// </summary>
    [Theory]
    [InlineData("12345678")]
    [InlineData("+45 12 34 56 78")]
    [InlineData("ikke et nummer")]
    public void Validate_PhoneNumberHasAnyNonBlankValue_ReturnsNoErrors(string phoneNumber)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, ValidEmail, phoneNumber, ValidCountry);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>BR-56, BR-95: land må ikke være tomt.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_CountryIsMissing_ReturnsCountryRequired(string? country)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, ValidEmail, ValidPhoneNumber, country);

        // Assert
        Assert.Equal(GuestErrors.CountryRequired, Assert.Single(errors));
    }

    // --------------------------------------------------------
    // E-MAIL (BR-55, BR-93)
    // --------------------------------------------------------

    /// <summary>
    /// BR-55, BR-93: e-mailen skal indeholde både '@' og '.'. Positionen kontrolleres
    /// bevidst ikke — en strammere regel ville bryde paritet med det gamle system.
    /// </summary>
    [Theory]
    [InlineData("valdemar@nfhotel.dk", true)]
    [InlineData("a@b.c", true)]
    [InlineData(".@", true)] // absurd, men opfylder reglen præcis som den er formuleret
    [InlineData("valdemar.nfhotel.dk", false)] // intet '@'
    [InlineData("valdemar@nfhoteldk", false)] // intet '.'
    [InlineData("valdemar", false)]
    public void IsValidEmail_RequiresBothAtSignAndDot(string email, bool expected)
    {
        // Arrange & Act
        var isValid = Guest.IsValidEmail(email);

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-55, BR-93: en udfyldt, men forkert formateret e-mail giver <c>email_invalid</c>.</summary>
    [Theory]
    [InlineData("valdemar.nfhotel.dk")]
    [InlineData("valdemar@nfhoteldk")]
    public void Validate_EmailIsMalformed_ReturnsEmailInvalid(string email)
    {
        // Arrange & Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Equal(GuestErrors.EmailInvalid, Assert.Single(errors));
    }

    /// <summary>
    /// BR-96, A-10: e-mailfeltet er det eneste med to uafhængige krav, så en adresse der
    /// både er for lang og forkert formateret giver begge koder. De øvrige felter kan kun
    /// give én kode ad gangen.
    /// </summary>
    [Fact]
    public void Validate_EmailIsBothMalformedAndTooLong_ReturnsBothCodes()
    {
        // Arrange
        var email = new string('a', Guest.MaxEmailLength + 1);

        // Act
        var errors = Guest.Validate(ValidFirstName, ValidLastName, email, ValidPhoneNumber, ValidCountry);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(GuestErrors.EmailInvalid, error),
            error => Assert.Equal(GuestErrors.EmailTooLong, error));
    }

    // --------------------------------------------------------
    // LÆNGDEGRÆNSER (A-10)
    // --------------------------------------------------------

    /// <summary>
    /// A-10: grænsen selv er tilladt. Testen bevogter den klassiske off-by-one, hvor
    /// EF-konfigurationen og domænet er uenige om ét tegn.
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
        var errors = Guest.Validate(firstName, lastName, email, phoneNumber, country, passportNumber);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>
    /// A-10: ét tegn over grænsen afvises af Domain — ikke først af databasen som en
    /// <c>DbUpdateException</c>. Alle seks felter har hver sin kode.
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
        var errors = Guest.Validate(firstName, lastName, email, phoneNumber, country, passportNumber);

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
    /// BR-57: pasnummeret er valgfrit og uvalideret ud over længden — hverken null eller
    /// en tom streng er en fejl.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(ValidPassportNumber)]
    public void Validate_PassportNumberIsOptional_ReturnsNoErrors(string? passportNumber)
    {
        // Arrange & Act
        var errors = Guest.Validate(
            ValidFirstName,
            ValidLastName,
            ValidEmail,
            ValidPhoneNumber,
            ValidCountry,
            passportNumber);

        // Assert
        Assert.Empty(errors);
    }

    // --------------------------------------------------------
    // ALLE FEJL PÅ ÉN GANG (BR-60, BR-96) OG FAIL-FAST (BR-46)
    // --------------------------------------------------------

    /// <summary>
    /// BR-60, BR-96: <c>Validate</c> samler alle fejl i feltrækkefølge, så en formular kan
    /// vise dem på én gang i stedet for at lade brugeren rette ét felt ad gangen.
    /// </summary>
    [Fact]
    public void Validate_EveryRequiredFieldIsBlank_ReturnsAllErrorsInFieldOrder()
    {
        // Arrange & Act
        var errors = Guest.Validate("  ", null, "", "\t", null);

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
    /// BR-46, BR-84: <c>Create</c> fejler på den FØRSTE fejl, så en ugyldig gæst aldrig
    /// kan opstå som objekt — det gamle system ignorerede valideringsresultatet og gemte
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

    /// <summary>BR-46: gyldige oplysninger skrives igennem uændret.</summary>
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
    /// BR-84: en afvist opdatering må ikke efterlade gæsten halvt overskrevet —
    /// invarianterne tjekkes FØR felterne skrives.
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

    /// <summary>BR-72: en gyldig opdatering overskriver præcis de seks redigérbare felter.</summary>
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
    /// Bygger en formatgyldig e-mail af en præcis længde, så længdereglen kan testes
    /// uden samtidig at udløse formatfejlen (BR-55).
    /// </summary>
    private static string EmailOfLength(int length) => $"a@b.{new string('c', length - 4)}";
}