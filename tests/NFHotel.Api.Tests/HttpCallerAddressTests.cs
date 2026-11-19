using System.Net;

using Microsoft.AspNetCore.Http;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;

namespace NFHotel.Api.Tests;

/// <summary>
/// The address of the caller of a request, as the limits of the guest sign-in and the audit trail read it (ADR-0049
/// decision 6, ADR-0017): the first address that the web application forwards, or else the address of the connection.
/// </summary>
public sealed class HttpCallerAddressTests
{
    #region Of

    /// <summary>Positive: the first forwarded address, without spaces around it, names the caller.</summary>
    /// <param name="header">The forwarded-for header of the request.</param>
    [Theory]
    [InlineData("203.0.113.7")]
    [InlineData(" 203.0.113.7 , 10.0.0.2")]
    [InlineData("203.0.113.7,10.0.0.2,10.0.0.3")]
    public void Of_ForwardedAddresses_IsTheFirstOne(string header)
    {
        // Arrange
        DefaultHttpContext request = new();
        request.Request.Headers[ApiAuthentication.ClientAddressHeader] = header;
        request.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.9");

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal("203.0.113.7", address);
    }

    /// <summary>
    /// Equivalence: without a forwarded address, as after the forwarded-headers middleware of the API has taken the one
    /// address that the web application sent, the address of the connection names the caller.
    /// </summary>
    /// <param name="header">The forwarded-for header of the request, or none.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Of_NoForwardedAddress_IsTheAddressOfTheConnection(string? header)
    {
        // Arrange
        DefaultHttpContext request = new();
        if (header is not null)
        {
            request.Request.Headers[ApiAuthentication.ClientAddressHeader] = header;
        }

        request.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.4");

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal("198.51.100.4", address);
    }

    /// <summary>Boundary: outside a request, and for a request that has no address at all, there is no address.</summary>
    [Fact]
    public void Of_NoRequestOrNoAddress_IsNull()
    {
        // Act
        string? outside = HttpCallerAddress.Of(null);
        string? withoutAddress = HttpCallerAddress.Of(new DefaultHttpContext());

        // Assert
        Assert.Null(outside);
        Assert.Null(withoutAddress);
    }

    #endregion

    #region Address

    /// <summary>Positive: the port reads the address of the current request through the accessor.</summary>
    [Fact]
    public void Address_CurrentRequest_IsTheAddressOfThatRequest()
    {
        // Arrange
        DefaultHttpContext request = new();
        request.Request.Headers[ApiAuthentication.ClientAddressHeader] = "203.0.113.8";
        HttpCallerAddress caller = new(new HttpContextAccessor { HttpContext = request });

        // Act
        string? address = caller.Address;

        // Assert
        Assert.Equal("203.0.113.8", address);
    }

    /// <summary>Negative: a missing accessor is a programming error.</summary>
    [Fact]
    public void Constructor_NoAccessor_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new HttpCallerAddress(null!));
    }

    #endregion
}
