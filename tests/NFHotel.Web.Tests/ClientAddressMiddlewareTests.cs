using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The address of the browser on the web application (MIL-087 criterion 4, ADR-0051 decision 4): the header that nginx
/// sets is read only from a peer in the networks of the stack and only when it is one valid address; otherwise the
/// address of the connection counts. The address goes on the user as the one claim of an identity without
/// authentication, so a signed-in user stays who they were and an anonymous user stays anonymous.
/// </summary>
public sealed class ClientAddressMiddlewareTests
{
    /// <summary>The name on the wire, written out, so the test pins it and not only the constant.</summary>
    private const string Header = "X-NFHotel-Client-Address";

    #region The header from nginx

    /// <summary>Positive: from a peer in a network of the stack, the address in the header is the address of the browser.</summary>
    /// <param name="peer">The address of the connection: nginx in the Docker network, or loopback in development.</param>
    [Theory]
    [InlineData("172.18.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("10.1.2.3")]
    [InlineData("192.168.1.10")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:172.18.0.5")]
    public async Task UseClientAddress_StackPeerWithTheHeader_TakesTheHeader(string peer)
    {
        // Arrange
        DefaultHttpContext request = Request(peer, "203.0.113.7");

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal("203.0.113.7", ClientAddressClaims.Of(request.User));
        Assert.Equal("203.0.113.7", ClientAddressMiddleware.AddressOf(request));
    }

    /// <summary>
    /// Security: a caller outside the networks of the stack that writes the header itself gets the address of its
    /// connection, so the header cannot name another browser; just outside 172.16.0.0/12 is outside.
    /// </summary>
    /// <param name="peer">The address of the connection.</param>
    /// <param name="expected">The address in its one form.</param>
    [Theory]
    [InlineData("198.51.100.4", "198.51.100.4")]
    [InlineData("172.32.0.1", "172.32.0.1")]
    [InlineData("172.15.255.255", "172.15.255.255")]
    [InlineData("2001:db8::4", "2001:db8::4")]
    [InlineData("::ffff:198.51.100.4", "198.51.100.4")]
    public async Task UseClientAddress_PeerOutsideTheStackWithTheHeader_TakesTheConnection(string peer, string expected)
    {
        // Arrange
        DefaultHttpContext request = Request(peer, "203.0.113.7");

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal(expected, ClientAddressClaims.Of(request.User));
    }

    /// <summary>
    /// Negative: from a peer in the stack, a header that is not one valid address (a list, a name, a short or octal form
    /// of IPv4, an address with a port, an empty value) is not taken, and the address of the connection counts.
    /// </summary>
    /// <param name="header">The value of the header.</param>
    [Theory]
    [InlineData("203.0.113.7, 203.0.113.8")]
    [InlineData("example.com")]
    [InlineData("127.1")]
    [InlineData("010.0.0.1")]
    [InlineData("203.0.113.7:443")]
    [InlineData("[2001:db8::7]")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UseClientAddress_StackPeerWithAnInvalidHeader_TakesTheConnection(string header)
    {
        // Arrange
        DefaultHttpContext request = Request("172.18.0.5", header);

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal("172.18.0.5", ClientAddressClaims.Of(request.User));
    }

    /// <summary>Negative: from a peer in the stack, two headers are not one address, and the address of the connection counts.</summary>
    [Fact]
    public async Task UseClientAddress_StackPeerWithTwoHeaders_TakesTheConnection()
    {
        // Arrange
        DefaultHttpContext request = Request("172.18.0.5", null);
        request.Request.Headers[Header] = new StringValues(["203.0.113.7", "203.0.113.8"]);

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal("172.18.0.5", ClientAddressClaims.Of(request.User));
    }

    /// <summary>Equivalence: an IPv4 address that the header writes as IPv6 is kept in its IPv4 form, and spaces around it go.</summary>
    /// <param name="header">The value of the header.</param>
    [Theory]
    [InlineData("::ffff:203.0.113.7")]
    [InlineData(" 203.0.113.7 ")]
    public async Task UseClientAddress_HeaderInAnotherForm_IsKeptInItsOneForm(string header)
    {
        // Arrange
        DefaultHttpContext request = Request("172.18.0.5", header);

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal("203.0.113.7", ClientAddressClaims.Of(request.User));
    }

    /// <summary>Boundary: without nginx and without the header, as in development, the address of the connection counts.</summary>
    [Fact]
    public async Task UseClientAddress_NoHeader_TakesTheConnection()
    {
        // Arrange
        DefaultHttpContext request = Request("127.0.0.1", null);

        // Act
        await RunAsync(request);

        // Assert
        Assert.Equal("127.0.0.1", ClientAddressClaims.Of(request.User));
    }

    /// <summary>
    /// Boundary: a request without the address of a connection, as on the in-memory test server, gets no address, also
    /// with the header, because no peer can be trusted.
    /// </summary>
    [Fact]
    public async Task UseClientAddress_NoConnectionAddress_AddsNone()
    {
        // Arrange
        DefaultHttpContext request = new();
        request.Request.Headers[Header] = "203.0.113.7";

        // Act
        await RunAsync(request);

        // Assert
        Assert.Null(ClientAddressClaims.Of(request.User));
        Assert.Null(ClientAddressMiddleware.AddressOf(request));
        Assert.DoesNotContain(request.User.Claims, claim => claim.Type == ClientAddressClaims.AddressClaim);
    }

    #endregion

    #region The user

    /// <summary>
    /// Security: a signed-in user stays who they were: the first identity is still the one of the sign-in, the address is
    /// on a second identity without authentication, and an anonymous user stays anonymous.
    /// </summary>
    [Fact]
    public async Task UseClientAddress_SignedInAndAnonymousUsers_StayWhoTheyWere()
    {
        // Arrange
        ClaimsIdentity staff = new([new Claim(ClaimTypes.Name, "manager")], "Cookies");
        DefaultHttpContext signedIn = Request("172.18.0.5", "203.0.113.7");
        signedIn.User = new ClaimsPrincipal(staff);
        DefaultHttpContext anonymous = Request("172.18.0.5", "203.0.113.8");

        // Act
        await RunAsync(signedIn);
        await RunAsync(anonymous);

        // Assert
        Assert.Same(staff, signedIn.User.Identity);
        Assert.Equal([true, false], signedIn.User.Identities.Select(identity => identity.IsAuthenticated));
        Assert.Equal("203.0.113.7", ClientAddressClaims.Of(signedIn.User));
        Assert.All(anonymous.User.Identities, identity => Assert.False(identity.IsAuthenticated));
        Assert.Equal("203.0.113.8", ClientAddressClaims.Of(anonymous.User));
    }

    /// <summary>Positive: the identity of an address holds the one claim and has no authentication.</summary>
    [Fact]
    public void IdentityFor_Address_HoldsTheOneClaimWithoutAuthentication()
    {
        // Act
        ClaimsIdentity identity = ClientAddressClaims.IdentityFor("203.0.113.7");

        // Assert
        Assert.False(identity.IsAuthenticated);
        Assert.Equal([(ClientAddressClaims.AddressClaim, "203.0.113.7")], identity.Claims.Select(claim => (claim.Type, claim.Value)));
    }

    /// <summary>Boundary: no user, and a user without the claim, have no address.</summary>
    [Fact]
    public void Of_NoUserOrNoClaim_IsNull()
    {
        // Act
        string? noUser = ClientAddressClaims.Of(null);
        string? noClaim = ClientAddressClaims.Of(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "manager")], "Cookies")));

        // Assert
        Assert.Null(noUser);
        Assert.Null(noClaim);
    }

    /// <summary>Negative: the step needs the application, and the reader a request.</summary>
    [Fact]
    public void UseClientAddressAndAddressOf_Null_Throw()
    {
        // Act and assert
        Assert.Throws<ArgumentNullException>(() => ClientAddressMiddleware.UseClientAddress(null!));
        Assert.Throws<ArgumentNullException>(() => ClientAddressMiddleware.AddressOf(null!));
        Assert.Throws<ArgumentNullException>(() => ClientAddressClaims.IdentityFor(null!));
    }

    #endregion

    #region Helpers

    private static DefaultHttpContext Request(string peer, string? header)
    {
        DefaultHttpContext request = new();
        request.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        if (header is not null)
        {
            request.Request.Headers[Header] = header;
        }

        return request;
    }

    // The step alone in a pipeline that ends after it, as it runs after the authentication of the web application.
    private static async Task RunAsync(HttpContext request)
    {
        ApplicationBuilder app = new(new ServiceCollection().BuildServiceProvider());
        app.UseClientAddress();
        app.Run(_ => Task.CompletedTask);

        await app.Build()(request);
    }

    #endregion
}
