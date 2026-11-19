using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>
/// The limit per email of the public booking from the settings (MIL-087 criterion 3, ADR-0051 decision 3): the number
/// that the use case compares with the count of the database, read through a port of its own (review RC-428).
/// </summary>
public sealed class ConfiguredPublicBookingEmailLimitTests
{
    /// <summary>Positive: the limit per email is the number of the settings.</summary>
    [Fact]
    public void ComingPublicBookingsPerEmail_FromTheSettings_IsTheirNumber()
    {
        // Arrange
        SenderLimitOptions options = new() { ComingPublicBookingsPerEmail = 7 };

        // Act
        ConfiguredPublicBookingEmailLimit limit = new(options);

        // Assert
        Assert.Equal(7, limit.ComingPublicBookingsPerEmail);
    }

    /// <summary>Boundary: the default of the settings is the number of the table, three.</summary>
    [Fact]
    public void ComingPublicBookingsPerEmail_DefaultSettings_IsThree()
    {
        // Arrange
        SenderLimitOptions options = new();

        // Act
        ConfiguredPublicBookingEmailLimit limit = new(options);

        // Assert
        Assert.Equal(3, limit.ComingPublicBookingsPerEmail);
    }

    /// <summary>Negative: a number below one is a mistake of the setup, which must not switch the limit off.</summary>
    /// <param name="number">The number of the setting.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NumberBelowOne_Throws(int number)
    {
        // Arrange
        SenderLimitOptions options = new() { ComingPublicBookingsPerEmail = number };

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfiguredPublicBookingEmailLimit(options));
    }

    /// <summary>Negative: the limit needs its settings.</summary>
    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new ConfiguredPublicBookingEmailLimit(null!));
    }
}
