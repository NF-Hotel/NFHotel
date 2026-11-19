using System.Security.Claims;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The principal of the guest cookie (MIL-018, ADR-0047 decision 6, and since MIL-085 ADR-0049 decision 3): the guest
/// token and nothing else, no key of a booking and no role, so no staff policy can accept it; and the holder that hands
/// the token of a request to the runner.
/// </summary>
public sealed class GuestClaimsTests
{
    private static readonly GuestSessionDto Session = new("the-guest-token", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero));

    #region The principal

    /// <summary>
    /// Positive (ADR-0049 decision 3): the principal carries the token of the session and no other claim, and is signed
    /// in by the guest scheme.
    /// </summary>
    [Fact]
    public void CreatePrincipal_Session_CarriesTheTokenOnly()
    {
        // Act
        ClaimsPrincipal guest = GuestClaims.CreatePrincipal(Session);

        // Assert
        Assert.True(guest.Identity?.IsAuthenticated);
        Assert.Equal(GuestClaims.CookieScheme, guest.Identity?.AuthenticationType);
        Assert.Equal("the-guest-token", GuestClaims.TokenOf(guest));
        Assert.Equal([GuestClaims.TokenClaim], guest.Claims.Select(claim => claim.Type));
    }

    /// <summary>
    /// Security: the guest principal has no role, and holds no permission of the staff policies, so a guest never passes
    /// a staff page even if the principal reached one.
    /// </summary>
    [Fact]
    public void CreatePrincipal_Session_HasNoRoleAndHoldsNoStaffPermission()
    {
        // Act
        ClaimsPrincipal guest = GuestClaims.CreatePrincipal(Session);

        // Assert
        Assert.Null(StaffClaims.RoleOf(guest));
        Assert.All(Permissions.All, permission => Assert.False(StaffClaims.Holds(guest, permission), permission));
    }

    /// <summary>Negative: a missing session is a programming error.</summary>
    [Fact]
    public void CreatePrincipal_NullSession_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => GuestClaims.CreatePrincipal(null!));
    }

    /// <summary>
    /// Negative: no principal, a principal that is not signed in, one without a token and one with an empty token give no
    /// token, so the own booking page sends the visitor to the sign-in.
    /// </summary>
    /// <param name="kind">Which principal is read.</param>
    [Theory]
    [InlineData("none")]
    [InlineData("not signed in")]
    [InlineData("no token")]
    [InlineData("empty token")]
    public void TokenOf_NoValidGuest_IsNull(string kind)
    {
        // Arrange
        ClaimsPrincipal? principal = kind switch
        {
            "none" => null,
            "not signed in" => new ClaimsPrincipal(new ClaimsIdentity([new Claim(GuestClaims.TokenClaim, "t")])),
            "no token" => Principal(new Claim("guest_booking", "7")),
            _ => Principal(new Claim(GuestClaims.TokenClaim, string.Empty)),
        };

        // Act
        string? token = GuestClaims.TokenOf(principal);

        // Assert
        Assert.Null(token);
    }

    #endregion

    #region The holder

    /// <summary>Positive: the holder keeps the token of the signed-in guest for the runner of the request.</summary>
    [Fact]
    public void Hold_SignedInGuest_KeepsTheToken()
    {
        // Arrange
        GuestTokenHolder holder = new();

        // Act
        holder.Hold(GuestClaims.CreatePrincipal(Session));

        // Assert
        Assert.Equal("the-guest-token", holder.Token);
    }

    /// <summary>Negative: holding nobody forgets the token, so a request without a guest cookie carries no guest token.</summary>
    [Fact]
    public void Hold_Nobody_ForgetsTheToken()
    {
        // Arrange
        GuestTokenHolder holder = new();
        holder.Hold(GuestClaims.CreatePrincipal(Session));

        // Act
        holder.Hold(null);

        // Assert
        Assert.Null(holder.Token);
    }

    #endregion

    #region Helpers

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, GuestClaims.CookieScheme));

    #endregion
}
