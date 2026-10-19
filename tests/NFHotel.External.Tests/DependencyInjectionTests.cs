using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;

namespace NFHotel.External.Tests;

/// <summary>The registrations of the external adapters: the hotel clock and the holiday calendar client.</summary>
public sealed class DependencyInjectionTests
{
    #region Hotel clock

    /// <summary>Positive: the clock is registered and tells "today" in the configured time zone (A-05).</summary>
    [Fact]
    public void AddHotelClock_ValidTimeZone_RegistersTheClock()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHotelClock(Configuration(("HotelTime:TimeZoneId", "Europe/Copenhagen")));

        // Act
        using var provider = services.BuildServiceProvider();
        var clock = provider.GetRequiredService<IClock>();

        // Assert
        Assert.True(clock.UtcNow > new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Edge: without a setting the clock uses the default time zone.</summary>
    [Fact]
    public void AddHotelClock_NoTimeZoneSetting_UsesTheDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHotelClock(Configuration());

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetRequiredService<IClock>());
    }

    /// <summary>Negative: an unknown time zone stops the start-up, not the first check-in.</summary>
    [Fact]
    public void AddHotelClock_UnknownTimeZone_ThrowsAtStartUp()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act and assert
        Assert.ThrowsAny<Exception>(() => services.AddHotelClock(Configuration(("HotelTime:TimeZoneId", "Nowhere/Land"))));
    }

    #endregion

    #region Holiday client

    /// <summary>Positive: the typed client is registered even when no address is configured (it then answers not configured).</summary>
    [Fact]
    public void AddHolidayApi_NoSettings_RegistersTheClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHolidayApi(Configuration());

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetRequiredService<IHolidayCalendarClient>());
    }

    #endregion

    #region Helpers

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();

    #endregion
}
