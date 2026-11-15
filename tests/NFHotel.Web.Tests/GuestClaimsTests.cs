using System.Security.Claims;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The principal of the guest cookie (MIL-018, ADR-0047 decision 6): the booking and the guest token, no role, so no staff
/// policy can accept it; and the holder that hands the token of a request to the runner.
/// </summary>
public sealed class GuestClaimsTests
{
    private static readonly GuestSessionDto Session = new("the-guest-token", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero), 7);

    #region The principal

    /// <summary>Positive: the principal carries the booking and the token of the session, and is signed in by the guest scheme.</summary>
    [Fact]
    public void CreatePrincipal_Session_CarriesTheBookingAndTheToken()
    {
        // Act
        ClaimsPrincipal guest = GuestClaims.CreatePrincipal(Session);

        // Assert
        Assert.True(guest.Identity?.IsAuthenticated);
        Assert.Equal(GuestClaims.CookieScheme, guest.Identity?.AuthenticationType);
        Assert.Equal(7, GuestClaims.BookingIdOf(guest));
        Assert.Equal("the-guest-token", GuestClaims.TokenOf(guest));
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
    /// Negative: no principal, a principal that is not signed in, one without a token and one with an unreadable booking
    /// give no booking, so the own booking page sends the visitor to the sign-in.
    /// </summary>
    /// <param name="kind">Which principal is read.</param>
    [Theory]
    [InlineData("none")]
    [InlineData("not signed in")]
    [InlineData("no token")]
    [InlineData("booking 0")]
    [InlineData("booking not a number")]
    public void BookingIdOf_NoValidGuest_IsNull(string kind)
    {
        // Arrange
        ClaimsPrincipal? principal = kind switch
        {
            "none" => null,
            "not signed in" => new ClaimsPrincipal(new ClaimsIdentity([new Claim(GuestClaims.BookingClaim, "7"), new Claim(GuestClaims.TokenClaim, "t")])),
            "no token" => Principal(new Claim(GuestClaims.BookingClaim, "7")),
            "booking 0" => Principal(new Claim(GuestClaims.BookingClaim, "0"), new Claim(GuestClaims.TokenClaim, "t")),
            _ => Principal(new Claim(GuestClaims.BookingClaim, "7a"), new Claim(GuestClaims.TokenClaim, "t")),
        };

        // Act
        int? bookingId = GuestClaims.BookingIdOf(principal);

        // Assert
        Assert.Null(bookingId);
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
