using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>The staff token of ADR-0005: signed, with the id, the name and the role as claims, and an expiry from the clock.</summary>
public sealed class JwtStaffTokenIssuerTests
{
    private const string SigningKey = "staff-signing-key-0123456789-staff-signing-key";

    #region Claims

    /// <summary>Positive: the token carries the name, the role, the issuer and the audience.</summary>
    [Fact]
    public async Task Issue_Member_ProducesATokenWithTheClaims()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash", StaffRole.Receptionist);

        // Act
        var issued = Build(new FakeClock()).Issue(member);
        var result = await Validate(issued.Token, SigningKey);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("rita", result.Claims["name"]);
        Assert.Equal("Receptionist", result.Claims["role"]);
        Assert.Equal("nfhotel-api", result.Claims["iss"]);
    }

    /// <summary>Positive: the token carries the stamp of the account and the must-change flag, so the API can compare them (ADR-0015).</summary>
    [Fact]
    public async Task Issue_Member_CarriesTheStampAndTheMustChangeFlag()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash", StaffRole.Manager, mustChangePassword: true);
        member.ChangePassword("hash-2");
        member.ResetPassword("hash-3");

        // Act
        var result = await Validate(Build(new FakeClock()).Issue(member).Token, SigningKey);

        // Assert
        Assert.Equal(3, Convert.ToInt32(result.Claims["stamp"], System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(Convert.ToBoolean(result.Claims["must_change"], System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Boundary: the token expires after the lifetime of the options from the instant of the clock, not before.</summary>
    [Fact]
    public void Issue_Member_ExpiresAfterTheConfiguredLifetime()
    {
        // Arrange
        var clock = new FakeClock { Today = new DateOnly(2030, 5, 1), TimeOfDay = TimeSpan.FromHours(8) };
        var member = StaffMember.Create("rita", "hash", StaffRole.Manager);

        // Act
        var issued = Build(clock, lifetimeMinutes: 90).Issue(member);

        // Assert
        Assert.Equal(new DateTimeOffset(2030, 5, 1, 9, 30, 0, TimeSpan.Zero), issued.ExpiresAt);
    }

    /// <summary>The token is refused with another key, so only the key holder can make a valid one.</summary>
    [Fact]
    public async Task Issue_Member_IsNotValidWithAnotherKey()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash", StaffRole.Manager);

        // Act
        var issued = Build(new FakeClock()).Issue(member);
        var result = await Validate(issued.Token, "another-signing-key-0123456789-another-key");

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>Negative: a missing member is a programming error.</summary>
    [Fact]
    public void Issue_NullMember_Throws()
    {
        // Arrange
        var issuer = Build(new FakeClock());

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => issuer.Issue(null!));
    }

    #endregion

    #region Helpers

    private static JwtStaffTokenIssuer Build(FakeClock clock, int lifetimeMinutes = 480) =>
        new(new StaffSecurityOptions { SigningKey = SigningKey, TokenLifetimeMinutes = lifetimeMinutes }, clock);

    private static async Task<TokenValidationResult> Validate(string token, string key)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "nfhotel-api",
            ValidAudience = "nfhotel-staff",
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false,
        };
        return await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    #endregion
}
