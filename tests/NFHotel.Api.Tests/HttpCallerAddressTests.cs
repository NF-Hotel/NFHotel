using System.Net;

using Microsoft.AspNetCore.Http;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;

namespace NFHotel.Api.Tests;

/// <summary>
/// The address of the caller of a request, as the audit trail, the limits of the guest sign-in and the limits of the
/// booking flow read it (MIL-087, ADR-0051 decision 4): the address that the web application names in
/// <see cref="ApiAuthentication.ClientAddressHeader"/>, only on a call that has shown the service token; otherwise the
/// address of the connection, after the forwarded headers of the stack.
/// </summary>
public sealed class HttpCallerAddressTests
{
    #region Of

    /// <summary>Positive: a call with the service token names its caller with the address in the header.</summary>
    /// <param name="header">The header of the request.</param>
    /// <param name="expected">The address in its one form.</param>
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData(" 203.0.113.7 ", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:DB8::7", "2001:db8::7")]
    public void Of_ServiceCallWithAnAddress_IsThatAddress(string header, string expected)
    {
        // Arrange
        DefaultHttpContext request = ServiceCallFrom("172.18.0.5");
        request.Request.Headers[ApiAuthentication.ClientAddressHeader] = header;

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal(expected, address);
    }

    /// <summary>
    /// Security (MIL-087 criterion 4): a call without the service token cannot name its caller; the header is ignored and
    /// the address of the connection names the caller.
    /// </summary>
    [Fact]
    public void Of_CallWithoutTheServiceToken_IgnoresTheHeader()
    {
        // Arrange
        DefaultHttpContext request = new();
        request.Request.Headers[ApiAuthentication.ClientAddressHeader] = "198.51.100.66";
        request.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.20");

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal("203.0.113.20", address);
    }

    /// <summary>
    /// Negative: a header that is not one address, as a list that a caller could have written or a value with a port,
    /// names no caller; the address of the connection does.
    /// </summary>
    /// <param name="header">The header of the request, or none.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("203.0.113.7, 10.0.0.2")]
    [InlineData("203.0.113.7:443")]
    [InlineData("[2001:db8::7]:443")]
    [InlineData("127.1")]
    [InlineData("not-an-address")]
    public void Of_ServiceCallWithoutOneAddress_IsTheAddressOfTheConnection(string? header)
    {
        // Arrange
        DefaultHttpContext request = ServiceCallFrom("198.51.100.4");
        if (header is not null)
        {
            request.Request.Headers[ApiAuthentication.ClientAddressHeader] = header;
        }

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal("198.51.100.4", address);
    }

    /// <summary>Equivalence: an IPv4 connection that the server holds in its IPv6 form names the caller in its IPv4 form.</summary>
    [Fact]
    public void Of_MappedConnectionAddress_IsTheIPv4Form()
    {
        // Arrange
        DefaultHttpContext request = new();
        request.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.9");

        // Act
        string? address = HttpCallerAddress.Of(request);

        // Assert
        Assert.Equal("203.0.113.9", address);
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
        DefaultHttpContext request = ServiceCallFrom("172.18.0.5");
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

    #region The mark of a service call

    /// <summary>Positive: a call is a service call only after <see cref="ServiceCall.Mark"/>, and the mark stays with that call.</summary>
    [Fact]
    public void IsMarked_BeforeAndAfterTheMark_IsTrueOnlyAfterIt()
    {
        // Arrange
        DefaultHttpContext marked = new();
        DefaultHttpContext other = new();

        // Act
        bool before = ServiceCall.IsMarked(marked);
        ServiceCall.Mark(marked);

        // Assert
        Assert.False(before);
        Assert.True(ServiceCall.IsMarked(marked));
        Assert.False(ServiceCall.IsMarked(other));
    }

    #endregion

    #region Helpers

    // A call from a peer that has shown the service token, as ApiTokenMiddleware marks it.
    private static DefaultHttpContext ServiceCallFrom(string connection)
    {
        DefaultHttpContext request = new();
        request.Connection.RemoteIpAddress = IPAddress.Parse(connection);
        ServiceCall.Mark(request);
        return request;
    }

    #endregion
}
