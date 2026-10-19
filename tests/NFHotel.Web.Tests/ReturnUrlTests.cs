using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>The sign-in returns only to a page of the back office on this site, never to another address (ADR-0005).</summary>
public sealed class ReturnUrlTests
{
    private const string Fallback = "/admin";

    #region Accepted

    /// <summary>Equivalence: a path below the back office, with or without a query, is accepted as it is.</summary>
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/rooms")]
    [InlineData("/admin/bookings?search=anna")]
    [InlineData("/admin?x=1")]
    [InlineData("/ADMIN/rooms")]
    public void Safe_PathOfTheBackOffice_IsKept(string candidate)
    {
        // Arrange and act
        var result = ReturnUrl.Safe(candidate, Fallback);

        // Assert
        Assert.Equal(candidate, result);
    }

    #endregion

    #region Refused

    /// <summary>Negative: another site, a protocol-relative address, a public page, a path that only starts like the back office, and the login page itself are all replaced by the fallback.</summary>
    [Theory]
    [InlineData("https://evil.example/admin")]
    [InlineData("//evil.example/admin")]
    [InlineData("/admin//evil.example")]
    [InlineData("/book")]
    [InlineData("/administrator")]
    [InlineData("/admin/login")]
    [InlineData("/admin/login?returnUrl=/admin")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/admin\\evil")]
    [InlineData("/admin/rooms\r\nSet-Cookie: x=1")]
    public void Safe_AnythingElse_ReturnsTheFallback(string candidate)
    {
        // Arrange and act
        var result = ReturnUrl.Safe(candidate, Fallback);

        // Assert
        Assert.Equal(Fallback, result);
    }

    /// <summary>Negative: nothing at all gives the fallback.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Safe_MissingAddress_ReturnsTheFallback(string? candidate)
    {
        // Arrange and act
        var result = ReturnUrl.Safe(candidate, Fallback);

        // Assert
        Assert.Equal(Fallback, result);
    }

    /// <summary>Negative: a missing fallback is a programming error.</summary>
    [Fact]
    public void Safe_BlankFallback_Throws()
    {
        // Arrange and act
        var thrown = Record.Exception(() => ReturnUrl.Safe("/admin", " "));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(thrown);
    }

    #endregion
}
