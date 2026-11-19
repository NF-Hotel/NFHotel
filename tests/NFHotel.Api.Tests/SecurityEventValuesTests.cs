using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The values that a security event of the booking flow holds (MIL-087, ADR-0051 decision 6): an email masked to its
/// first character and its domain, and the email of a request of the booking flow and of nothing else.
/// </summary>
public sealed class SecurityEventValuesTests
{
    #region MaskEmail

    /// <summary>Positive: an email keeps its first character and its domain, so an event tells two senders apart.</summary>
    /// <param name="email">The email as the request has it.</param>
    /// <param name="expected">The masked email.</param>
    [Theory]
    [InlineData("anna@example.com", "a***@example.com")]
    [InlineData(" Anna@Example.com ", "A***@Example.com")]
    [InlineData("a@b.dk", "a***@b.dk")]
    [InlineData("anna+tag@sub.example.co.uk", "a***@sub.example.co.uk")]
    public void MaskEmail_Email_KeepsTheFirstCharacterAndTheDomain(string email, string expected)
    {
        // Act and assert
        Assert.Equal(expected, SecurityEvents.MaskEmail(email));
    }

    /// <summary>
    /// Security (ASVS 16.2.5): a value that is no plain email keeps nothing that the caller could use to write into the
    /// log, such as a line break or a second field, and no value at all is written as <see cref="SecurityEvents.None"/>.
    /// </summary>
    /// <param name="email">The email as the request has it.</param>
    /// <param name="expected">The masked email.</param>
    [Theory]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    [InlineData("   ", "-")]
    [InlineData("anna", "a***")]
    [InlineData("@example.com", "***")]
    [InlineData("anna@", "a***")]
    [InlineData("anna@exa mple.com", "a***")]
    [InlineData("anna@example.com\r\nkind=limit_refused", "a***")]
    [InlineData("<anna@example.com", "***@example.com")]
    public void MaskEmail_NoPlainEmail_KeepsNoTypedText(string? email, string expected)
    {
        // Act and assert
        Assert.Equal(expected, SecurityEvents.MaskEmail(email));
    }

    #endregion

    #region EmailOf

    /// <summary>Positive: the email of the new guest of a public booking and the email of a guest sign-in are found among the arguments.</summary>
    [Fact]
    public void EmailOf_RequestsOfTheBookingFlow_IsTheirEmail()
    {
        // Arrange
        CreatePublicBookingRequest booking = new(
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 3),
            RoomSize.Double,
            1,
            new GuestFields("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null));
        GuestSignInRequest signIn = new("3f2504e0-4f89-41d3-9a0c-0305e82c3301", "eve@example.org");

        // Act
        string? fromBooking = SecurityEvents.EmailOf([booking, CancellationToken.None]);
        string? fromSignIn = SecurityEvents.EmailOf([signIn, CancellationToken.None]);

        // Assert
        Assert.Equal("anna@example.com", fromBooking);
        Assert.Equal("eve@example.org", fromSignIn);
    }

    /// <summary>Equivalence: a request without a new guest, other arguments and none at all give no email.</summary>
    [Fact]
    public void EmailOf_NoEmailAmongTheArguments_IsNull()
    {
        // Arrange
        CreatePublicBookingRequest withoutGuest = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3), RoomSize.Double, 1, null);

        // Act and assert
        Assert.Null(SecurityEvents.EmailOf([withoutGuest]));
        Assert.Null(SecurityEvents.EmailOf([new DateOnly(2026, 3, 1), null, "anna@example.com"]));
        Assert.Null(SecurityEvents.EmailOf([]));
    }

    #endregion
}
