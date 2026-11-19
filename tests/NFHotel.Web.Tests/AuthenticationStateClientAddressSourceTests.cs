using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;

using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The source of the address that the runner gives to the API (MIL-087 criterion 4, ADR-0051 decision 4): the claim of the
/// authentication state, on a static page and in a circuit alike, and the claim of the request only where no
/// authentication state is set up.
/// </summary>
public sealed class AuthenticationStateClientAddressSourceTests
{
    /// <summary>Positive: a page or a circuit whose authentication state has the address gives it.</summary>
    [Fact]
    public async Task GetAddressAsync_StateWithTheAddress_GivesIt()
    {
        // Arrange
        ServerAuthenticationStateProvider provider = StateOf(UserAt("203.0.113.7"));
        AuthenticationStateClientAddressSource source = new(provider, new HttpContextAccessor());

        // Act
        string? address = await source.GetAddressAsync();

        // Assert
        Assert.Equal("203.0.113.7", address);
    }

    /// <summary>Positive: where no authentication state is set up, as in an endpoint outside a page, the request gives the address.</summary>
    [Fact]
    public async Task GetAddressAsync_NoStateSetUp_TakesTheRequest()
    {
        // Arrange
        HttpContextAccessor accessor = new() { HttpContext = new DefaultHttpContext { User = UserAt("198.51.100.4") } };
        AuthenticationStateClientAddressSource source = new(new ServerAuthenticationStateProvider(), accessor);

        // Act
        string? address = await source.GetAddressAsync();

        // Assert
        Assert.Equal("198.51.100.4", address);
    }

    /// <summary>
    /// Equivalence: a state without the address gives none and does not read the request, which in a circuit is not the
    /// browser's; neither a state nor a request gives none.
    /// </summary>
    [Fact]
    public async Task GetAddressAsync_StateWithoutTheAddressOrNothing_GivesNone()
    {
        // Arrange
        HttpContextAccessor accessor = new() { HttpContext = new DefaultHttpContext { User = UserAt("198.51.100.4") } };
        AuthenticationStateClientAddressSource stateWithout = new(StateOf(new ClaimsPrincipal(new ClaimsIdentity())), accessor);
        AuthenticationStateClientAddressSource nothing = new(new ServerAuthenticationStateProvider(), new NoRequest());

        // Act
        string? fromStateWithout = await stateWithout.GetAddressAsync();
        string? fromNothing = await nothing.GetAddressAsync();

        // Assert
        Assert.Null(fromStateWithout);
        Assert.Null(fromNothing);
    }

    /// <summary>Negative: the source needs the authentication state and the request accessor.</summary>
    [Fact]
    public void Constructor_MissingDependency_Throws()
    {
        // Act and assert
        Assert.Throws<ArgumentNullException>(() => new AuthenticationStateClientAddressSource(null!, new HttpContextAccessor()));
        Assert.Throws<ArgumentNullException>(() => new AuthenticationStateClientAddressSource(new ServerAuthenticationStateProvider(), null!));
    }

    private static ClaimsPrincipal UserAt(string address)
    {
        ClaimsPrincipal user = new(new ClaimsIdentity());
        user.AddIdentity(ClientAddressClaims.IdentityFor(address));
        return user;
    }

    // An accessor without a request. Every HttpContextAccessor shares one AsyncLocal, so a new one would see the request
    // that another accessor of the test set.
    private sealed class NoRequest : IHttpContextAccessor
    {
        public HttpContext? HttpContext
        {
            get => null;
            set => _ = value;
        }
    }

    private static ServerAuthenticationStateProvider StateOf(ClaimsPrincipal user)
    {
        ServerAuthenticationStateProvider provider = new();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));
        return provider;
    }
}
