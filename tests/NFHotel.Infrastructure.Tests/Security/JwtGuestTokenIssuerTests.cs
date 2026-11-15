using System.Globalization;
using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Application.Dto.Guests;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>
/// The guest token of ADR-0047 decision 2 (MIL-018): signed with the key of the staff tokens, the role Guest and one
/// booking as claims, two hours from the clock, and an audience of its own so it never passes for a staff token.
/// </summary>
public sealed class JwtGuestTokenIssuerTests
{
    private const string SigningKey = "staff-signing-key-0123456789-staff-signing-key";

    #region Claims

    /// <summary>Positive: the token carries the role Guest, the booking, the issuer and the guest audience.</summary>
    [Fact]
    public async Task Issue_Booking_ProducesATokenWithTheGuestRoleAndTheBooking()
    {
        // Act
        GuestSessionDto session = Build(new FakeClock()).Issue(7);
        TokenValidationResult result = await ValidateAsync(session.Token, SigningKey, JwtGuestTokenIssuer.Audience);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Guest", result.Claims["role"]);
        Assert.Equal("7", result.Claims[JwtGuestTokenIssuer.BookingClaim]);
        Assert.Equal(StaffSecurityOptions.Issuer, result.Claims["iss"]);
        Assert.Equal(7, session.BookingId);
    }

    /// <summary>
    /// Security: the token names no staff account (no subject, no name), so the API can never read it as a member of
    /// staff.
    /// </summary>
    [Fact]
    public async Task Issue_Booking_CarriesNoSubjectAndNoName()
    {
        // Act
        GuestSessionDto session = Build(new FakeClock()).Issue(7);
        TokenValidationResult result = await ValidateAsync(session.Token, SigningKey, JwtGuestTokenIssuer.Audience);

        // Assert
        Assert.False(result.Claims.ContainsKey("sub"));
        Assert.False(result.Claims.ContainsKey(JwtStaffTokenIssuer.NameClaim));
    }

    /// <summary>Boundary: the token expires two hours after the instant of the clock, and the session says the same instant.</summary>
    [Fact]
    public async Task Issue_Booking_ExpiresTwoHoursAfterTheClock()
    {
        // Arrange
        FakeClock clock = new() { Today = new DateOnly(2030, 5, 1), TimeOfDay = TimeSpan.FromHours(8) };
        DateTimeOffset expected = new(2030, 5, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        GuestSessionDto session = Build(clock).Issue(7);
        TokenValidationResult result = await ValidateAsync(session.Token, SigningKey, JwtGuestTokenIssuer.Audience);

        // Assert
        Assert.Equal(expected, session.ExpiresAt);
        Assert.Equal(expected.ToUnixTimeSeconds(), Convert.ToInt64(result.Claims["exp"], CultureInfo.InvariantCulture));
    }

    #endregion

    #region Refusals

    /// <summary>Security: a guest token is not a staff token; validated with the staff audience it is refused.</summary>
    [Fact]
    public async Task Issue_Booking_IsNotValidAsAStaffToken()
    {
        // Act
        GuestSessionDto session = Build(new FakeClock()).Issue(7);
        TokenValidationResult result = await ValidateAsync(session.Token, SigningKey, StaffSecurityOptions.Audience);

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>Security: the token is refused with another key, so only the key holder can make a valid one.</summary>
    [Fact]
    public async Task Issue_Booking_IsNotValidWithAnotherKey()
    {
        // Act
        GuestSessionDto session = Build(new FakeClock()).Issue(7);
        TokenValidationResult result = await ValidateAsync(session.Token, "another-signing-key-0123456789-another-key", JwtGuestTokenIssuer.Audience);

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>Negative: a booking key of 0 or below is a programming error, so no token opens no booking.</summary>
    /// <param name="bookingId">The invalid key.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_ZeroOrNegativeBooking_Throws(int bookingId)
    {
        // Arrange
        JwtGuestTokenIssuer issuer = Build(new FakeClock());

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => issuer.Issue(bookingId));
    }

    /// <summary>Negative: the options and the clock are required.</summary>
    [Fact]
    public void Constructor_MissingOptionsOrClock_Throws()
    {
        // Arrange
        StaffSecurityOptions options = new() { SigningKey = SigningKey };

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new JwtGuestTokenIssuer(null!, new FakeClock()));
        Assert.Throws<ArgumentNullException>(() => new JwtGuestTokenIssuer(options, null!));
    }

    #endregion

    #region Helpers

    private static JwtGuestTokenIssuer Build(FakeClock clock) =>
        new(new StaffSecurityOptions { SigningKey = SigningKey }, clock);

    private static async Task<TokenValidationResult> ValidateAsync(string token, string key, string audience)
    {
        TokenValidationParameters parameters = new()
        {
            ValidIssuer = StaffSecurityOptions.Issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false,
        };
        return await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    #endregion
}
