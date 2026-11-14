using System.Globalization;

using NFHotel.Api.Security;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>The input rules that every value bound from a request meets (MIL-077).</summary>
/// <remarks>
/// The refused characters are built from their codes and never written into the source: a bidirectional control
/// character in the source is the attack that the rules refuse, and theory data cannot carry a lone surrogate.
/// </remarks>
public sealed class InputRulesTests
{
    private const char NulCharacter = (char)0x0000;
    private const char RightToLeftOverride = (char)0x202E;
    private const char HighSurrogate = (char)0xD83D;
    private const char LowSurrogate = (char)0xDE00;

    #region Text

    /// <summary>Positive: letters, digits, Danish letters, Arabic script, a tab, a line feed, a carriage return and empty text are accepted.</summary>
    /// <param name="text">The text.</param>
    [Theory]
    [InlineData("Anna Jensen 101")]
    [InlineData("Ærø Østerby Åbo")]
    [InlineData("مرحبا")]
    [InlineData("line one\nline two\r\n\tindented")]
    [InlineData("")]
    public void IsAcceptedText_OrdinaryText_IsAccepted(string text)
    {
        // Act
        var isAccepted = InputRules.IsAcceptedText(text);

        // Assert
        Assert.True(isAccepted);
    }

    /// <summary>Boundary: each refused range of characters starts and ends where it should, and the character just outside it is accepted.</summary>
    /// <param name="code">The UTF-16 code of the character.</param>
    /// <param name="expected">Whether the character is accepted.</param>
    [Theory]
    [InlineData(0x0000, false)]
    [InlineData(0x0008, false)]
    [InlineData(0x0009, true)]
    [InlineData(0x000A, true)]
    [InlineData(0x000B, false)]
    [InlineData(0x000C, false)]
    [InlineData(0x000D, true)]
    [InlineData(0x000E, false)]
    [InlineData(0x001F, false)]
    [InlineData(0x0020, true)]
    [InlineData(0x007E, true)]
    [InlineData(0x007F, false)]
    [InlineData(0x0085, false)]
    [InlineData(0x009F, false)]
    [InlineData(0x00A0, true)]
    [InlineData(0x2029, true)]
    [InlineData(0x202A, false)]
    [InlineData(0x202E, false)]
    [InlineData(0x202F, true)]
    [InlineData(0x2065, true)]
    [InlineData(0x2066, false)]
    [InlineData(0x2069, false)]
    [InlineData(0x206A, true)]
    [InlineData(0xD800, false)]
    [InlineData(0xDBFF, false)]
    [InlineData(0xDC00, false)]
    [InlineData(0xDFFF, false)]
    public void IsAcceptedText_Character_IsJudgedByItsRange(int code, bool expected)
    {
        // Arrange
        var text = $"a{(char)code}b";

        // Act
        var isAccepted = InputRules.IsAcceptedText(text);

        // Assert
        Assert.Equal(expected, isAccepted);
    }

    /// <summary>Edge case: a high surrogate followed by a low one is one character, here an emoji, and is accepted.</summary>
    [Fact]
    public void IsAcceptedText_SurrogatePair_IsAccepted()
    {
        // Arrange
        var text = $"a{HighSurrogate}{LowSurrogate}b";

        // Act
        var isAccepted = InputRules.IsAcceptedText(text);

        // Assert
        Assert.True(isAccepted);
    }

    /// <summary>Negative: a low surrogate before a high one makes two lone surrogates, and the text is refused.</summary>
    [Fact]
    public void IsAcceptedText_SurrogatesInTheWrongOrder_AreRefused()
    {
        // Arrange
        var text = $"a{LowSurrogate}{HighSurrogate}b";

        // Act
        var isAccepted = InputRules.IsAcceptedText(text);

        // Assert
        Assert.False(isAccepted);
    }

    /// <summary>Boundary: a high surrogate as the last character has no low surrogate after it, and the text is refused.</summary>
    [Fact]
    public void IsAcceptedText_HighSurrogateAtTheEnd_IsRefused()
    {
        // Arrange
        var text = $"ab{HighSurrogate}";

        // Act
        var isAccepted = InputRules.IsAcceptedText(text);

        // Assert
        Assert.False(isAccepted);
    }

    #endregion

    #region Dates

    /// <summary>Boundary: a day from 1900-01-01 to 2199-12-31 is accepted and the day outside each end is refused.</summary>
    /// <param name="day">The day.</param>
    /// <param name="expected">Whether the day is accepted.</param>
    [Theory]
    [InlineData("1899-12-31", false)]
    [InlineData("1900-01-01", true)]
    [InlineData("2199-12-31", true)]
    [InlineData("2200-01-01", false)]
    public void IsAcceptedDate_Day_LiesInTheRange(string day, bool expected)
    {
        // Act
        var isAccepted = InputRules.IsAcceptedDate(DateOnly.Parse(day, CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expected, isAccepted);
    }

    #endregion

    #region Request values

    /// <summary>Positive: a request record of plausible values, a missing value and a number are accepted.</summary>
    [Fact]
    public void IsAccepted_PlausibleValues_AreAccepted()
    {
        // Act
        var areAccepted = InputRules.IsAccepted(new RoomFilter(1, RoomSize.Double)) && InputRules.IsAccepted(null) && InputRules.IsAccepted(42);

        // Assert
        Assert.True(areAccepted);
    }

    /// <summary>Negative: an enum value that the enum does not define is refused.</summary>
    [Fact]
    public void IsAccepted_UndefinedEnumValue_IsRefused()
    {
        // Act
        var isAccepted = InputRules.IsAccepted(new RoomFilter(Size: (RoomSize)(-1)));

        // Assert
        Assert.False(isAccepted);
    }

    /// <summary>Negative: a refused character in a record inside a record is found.</summary>
    [Fact]
    public void IsAccepted_RefusedCharacterInANestedRecord_IsRefused()
    {
        // Arrange
        var request = new CreateGuestRequest(ServiceWorld.ValidGuestFields() with { FirstName = $"a{NulCharacter}b" });

        // Act
        var isAccepted = InputRules.IsAccepted(request);

        // Assert
        Assert.False(isAccepted);
    }

    /// <summary>Negative: a refused character in an item of a list is found.</summary>
    [Fact]
    public void IsAccepted_RefusedCharacterInAList_IsRefused()
    {
        // Act
        var isAccepted = InputRules.IsAccepted(new[] { "Double", $"{RightToLeftOverride}Single" });

        // Assert
        Assert.False(isAccepted);
    }

    /// <summary>Boundary: a moment on a day outside the range is refused, as a date and time and as one with an offset.</summary>
    [Fact]
    public void IsAccepted_MomentOutsideTheRange_IsRefused()
    {
        // Act
        var areRefused = !InputRules.IsAccepted(DateTime.MinValue) && !InputRules.IsAccepted(DateTimeOffset.MaxValue);

        // Assert
        Assert.True(areRefused);
    }

    #endregion
}
