using NFHotel.Domain.Attributes;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Guests;

/// <summary>
/// The formats of the fields of a guest and the normal form of a typed text (MIL-086 decision 2, ADR-0050): one
/// attribute per format, which <c>Guest</c> carries and whose method <c>Accepts</c> anyone can call. Each format is
/// tested at its limits and with one value of each kind that it refuses. The lengths are the rules of the attributes
/// <c>StringLength</c> and are tested with the guest.
/// </summary>
public class GuestFieldFormatTests
{
    #region Normal form

    /// <summary>Positive: white space at both ends goes, the text inside stays, and the text is in normal form C.</summary>
    [Theory]
    [InlineData("  Anna  ", "Anna")]
    [InlineData("\tAnna\r\n", "Anna")]
    [InlineData("Anna Marie", "Anna Marie")]
    [InlineData("José", "José")]
    [InlineData("Ångström", "Ångström")]
    public void Normalize_Text_TrimsAndComposes(string text, string expected)
    {
        // Act
        var normalized = TextNormalForm.Normalize(text);

        // Assert
        Assert.Equal(expected, normalized);
    }

    /// <summary>Boundary: no text stays no text, and white space alone becomes empty, which the rule of a required field refuses.</summary>
    [Fact]
    public void Normalize_NullOrWhiteSpace_GivesNullOrEmpty()
    {
        // Act & Assert
        Assert.Null(TextNormalForm.Normalize(null));
        Assert.Equal(string.Empty, TextNormalForm.Normalize(" \t\r\n "));
    }

    /// <summary>Negative: a text with a lone surrogate cannot be normalised; it is left as it is, and its rule refuses it.</summary>
    [Fact]
    public void Normalize_LoneSurrogate_LeavesTheTextForTheRuleToRefuse()
    {
        // Arrange
        const string text = "An\uD800na";

        // Act
        var normalized = TextNormalForm.Normalize(text);

        // Assert
        Assert.Equal(text, normalized);
        Assert.False(PersonNameAttribute.Accepts(normalized));
    }

    /// <summary>Equivalence: an optional text that is empty after the normal form is no value.</summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t ", null)]
    [InlineData(" AB123 ", "AB123")]
    public void NormalizeOptional_Text_GivesNullForNothing(string? text, string? expected)
    {
        // Act
        var normalized = TextNormalForm.NormalizeOptional(text);

        // Assert
        Assert.Equal(expected, normalized);
    }

    #endregion

    #region Names

    /// <summary>
    /// Positive: letters and marks of any script, the space, both apostrophes, the hyphen-minus and the full stop. SQL
    /// text made of those characters is a name like any other, and stays data.
    /// </summary>
    [Theory]
    [InlineData("A")]
    [InlineData("Anna")]
    [InlineData("Anna Marie")]
    [InlineData("O'Brien")]
    [InlineData("O’Brien")]
    [InlineData("Jean-Luc")]
    [InlineData("St. John")]
    [InlineData("Ægir")]
    [InlineData("Zoë")]
    [InlineData("王")]
    [InlineData("محمد")]
    [InlineData("सुनील")]
    [InlineData("x' OR 'x")]
    public void PersonName_LettersMarksAndTheAllowedSigns_IsAccepted(string name)
    {
        // Act & Assert
        Assert.True(PersonNameAttribute.Accepts(name));
    }

    /// <summary>Negative: no letter at all, a digit, a sign outside the list, markup, a line break, a tab, a no-break space and an emoji.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("'.")]
    [InlineData("Anna1")]
    [InlineData("Anna_")]
    [InlineData("Anna%")]
    [InlineData("Anna;")]
    [InlineData("Anna\"")]
    [InlineData("{0}")]
    [InlineData("a@b")]
    [InlineData("<script>")]
    [InlineData("Anna\nJensen")]
    [InlineData("Anna\tJensen")]
    [InlineData("Anna Jensen")]
    [InlineData("😀")]
    public void PersonName_OtherCharacters_IsRefused(string? name)
    {
        // Act & Assert
        Assert.False(PersonNameAttribute.Accepts(name));
    }

    #endregion

    #region Email

    /// <summary>Positive: one plain address with a domain of two or more labels, at the limits of the local part and of a label.</summary>
    [Theory]
    [InlineData("anna@example.com")]
    [InlineData("a@b.dk")]
    [InlineData("anna.jensen@example.co.uk")]
    [InlineData("anna+booking@example.com")]
    [InlineData("ANNA@EXAMPLE.COM")]
    [InlineData("o'brien@example.ie")]
    [InlineData("anna@xn--bcher-kva.example")]
    [InlineData("anna@mail-1.example.com")]
    public void EmailFormat_PlainAddress_IsAccepted(string email)
    {
        // Act & Assert
        Assert.True(EmailFormatAttribute.Accepts(email));
    }

    /// <summary>Boundary: 64 characters before the at sign and a label of 63 are accepted; 65 and 64 are refused.</summary>
    [Fact]
    public void EmailFormat_LocalPartAndLabelAtTheirLimits_AreAcceptedAndOneMoreIsRefused()
    {
        // Arrange
        var local64 = new string('a', 64);
        var label63 = new string('b', 63);

        // Act & Assert
        Assert.True(EmailFormatAttribute.Accepts($"{local64}@example.com"));
        Assert.False(EmailFormatAttribute.Accepts($"{local64}a@example.com"));
        Assert.True(EmailFormatAttribute.Accepts($"anna@{label63}.com"));
        Assert.False(EmailFormatAttribute.Accepts($"anna@{label63}b.com"));
    }

    /// <summary>
    /// Negative: no at sign, one label, an empty label, a label with a hyphen at an end or a sign outside the list, white
    /// space, a quotation mark, a display name, angle brackets, a backslash, a backquote, a comment, two at signs, two
    /// addresses, an address literal and a line break that would add a mail header.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("anna")]
    [InlineData("anna@example")]
    [InlineData("anna@example.")]
    [InlineData("anna@.example.com")]
    [InlineData("anna@example..com")]
    [InlineData("anna@-example.com")]
    [InlineData("anna@example-.com")]
    [InlineData("anna@exa_mple.com")]
    [InlineData("anna jensen@example.com")]
    [InlineData("anna@exam ple.com")]
    [InlineData("\"anna\"@example.com")]
    [InlineData("Anna <anna@example.com>")]
    [InlineData("<anna@example.com>")]
    [InlineData("an\\na@example.com")]
    [InlineData("`anna`@example.com")]
    [InlineData("anna(comment)@example.com")]
    [InlineData("anna@@example.com")]
    [InlineData("@example.com")]
    [InlineData("anna@")]
    [InlineData("anna@example.com, eve@example.com")]
    [InlineData("anna@[127.0.0.1]")]
    [InlineData("anna@example.com\r\nBcc: eve@example.com")]
    public void EmailFormat_NotOnePlainAddress_IsRefused(string? email)
    {
        // Act & Assert
        Assert.False(EmailFormatAttribute.Accepts(email));
    }

    #endregion

    #region Phone number

    /// <summary>Positive: an optional plus, then 7 to 15 digits with spaces, hyphens and round brackets between them.</summary>
    [Theory]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("+4512345678")]
    [InlineData("+45 12 34 56 78")]
    [InlineData("+123456789012345")]
    [InlineData("(030) 1234567")]
    [InlineData("+44 (0) 20 7946 0958")]
    [InlineData("+1 (555) 123-4567")]
    public void PhoneNumber_DigitsWithSeparators_IsAccepted(string phoneNumber)
    {
        // Act & Assert
        Assert.True(PhoneNumberAttribute.Accepts(phoneNumber));
    }

    /// <summary>
    /// Negative: 6 and 16 digits, a plus that is not first or not followed by a digit or a bracket, a separator at an end,
    /// a letter, a dot, a slash, digits of another script, a line break and markup.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("1 2 3 4 5 6")]
    [InlineData("+1234567890123456")]
    [InlineData("+")]
    [InlineData("++4512345678")]
    [InlineData("4512345678+")]
    [InlineData("+ 4512345678")]
    [InlineData("-12345678")]
    [InlineData("12345678-")]
    [InlineData("1234567a")]
    [InlineData("12.34.56.78")]
    [InlineData("12/345678")]
    [InlineData("١٢٣٤٥٦٧")]
    [InlineData("12345678\nBcc")]
    [InlineData("<script>")]
    public void PhoneNumber_OtherText_IsRefused(string? phoneNumber)
    {
        // Act & Assert
        Assert.False(PhoneNumberAttribute.Accepts(phoneNumber));
    }

    #endregion

    #region Country

    /// <summary>Positive: a name of the list, the long forms of the data of the hotel and the eight corrected names included.</summary>
    [Theory]
    [InlineData("Denmark")]
    [InlineData("Sweden")]
    [InlineData("Portugal")]
    [InlineData("United Kingdom of Great Britain and Northern Ireland (the)")]
    [InlineData("United States of America (the)")]
    [InlineData("Netherlands (Kingdom of the)")]
    [InlineData("Türkiye")]
    [InlineData("Côte d'Ivoire")]
    [InlineData("Åland Islands")]
    public void CountryName_NameOfTheList_IsAccepted(string country)
    {
        // Act & Assert
        Assert.True(CountryNameAttribute.Accepts(country));
    }

    /// <summary>
    /// Negative: another letter case, a code, a name in another language, an old or a short name, a name that is not
    /// normalised, the stand-in of the data and markup.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("denmark")]
    [InlineData("DENMARK")]
    [InlineData("DK")]
    [InlineData("DNK")]
    [InlineData("Danmark")]
    [InlineData("Testland")]
    [InlineData("Unknown")]
    [InlineData("Turkey")]
    [InlineData("United Kingdom")]
    [InlineData(" Denmark")]
    [InlineData("Côte d'Ivoire")]
    [InlineData("Denmark<script>")]
    public void CountryName_AnythingElse_IsRefused(string? country)
    {
        // Act & Assert
        Assert.False(CountryNameAttribute.Accepts(country));
    }

    /// <summary>Positive: a name that is not normalised is accepted after the normal form, which the guest applies first.</summary>
    [Fact]
    public void CountryName_DecomposedName_IsAcceptedAfterTheNormalForm()
    {
        // Act & Assert
        Assert.True(CountryNameAttribute.Accepts(TextNormalForm.Normalize(" Côte d'Ivoire ")));
    }

    #endregion

    #region Passport number

    /// <summary>Positive: ASCII letters and digits in either letter case.</summary>
    [Theory]
    [InlineData("A")]
    [InlineData("ab12")]
    [InlineData("AB1234567")]
    [InlineData("12345678901234567890")]
    public void PassportNumber_LettersAndDigits_IsAccepted(string passportNumber)
    {
        // Act & Assert
        Assert.True(PassportNumberAttribute.Accepts(passportNumber));
    }

    /// <summary>Negative: a hyphen, a space, a letter outside ASCII, digits of full width, a line break, markup and the wildcards of a search.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB-1234567")]
    [InlineData("AB 1234567")]
    [InlineData("ÆB123")]
    [InlineData("１２３")]
    [InlineData("AB12345\n")]
    [InlineData("<script>")]
    [InlineData("%_")]
    public void PassportNumber_OtherText_IsRefused(string? passportNumber)
    {
        // Act & Assert
        Assert.False(PassportNumberAttribute.Accepts(passportNumber));
    }

    #endregion
}
