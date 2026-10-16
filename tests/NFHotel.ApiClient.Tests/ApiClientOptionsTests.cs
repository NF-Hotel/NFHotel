using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;

namespace NFHotel.ApiClient.Tests;

/// <summary>The settings of the client and its registration: a setup mistake stops the start-up, it is not found on the first page.</summary>
public sealed class ApiClientOptionsTests
{
    #region Options

    /// <summary>Positive: a valid address and token are read, and a missing slash is added so relative paths resolve below it.</summary>
    [Fact]
    public void Read_ValidSettings_ReturnsTheOptionsWithATrailingSlash()
    {
        // Arrange
        var configuration = Configuration(("Api:BaseUrl", "http://nfhotel-api:8080"), ("Api:Token", "a-token"));

        // Act
        var options = ApiClientOptions.Read(configuration);

        // Assert
        Assert.Equal("http://nfhotel-api:8080/", options.BaseUri.ToString());
        Assert.Equal("a-token", options.Token);
        Assert.Equal(ApiClientOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
    }

    /// <summary>Equivalence: a missing, a relative and a non-HTTP address are all a setup mistake.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nfhotel-api:8080")]
    [InlineData("/relative/path")]
    [InlineData("ftp://nfhotel-api/")]
    public void Read_BadAddress_Throws(string? baseUrl)
    {
        // Arrange
        var configuration = baseUrl is null
            ? Configuration(("Api:Token", "a-token"))
            : Configuration(("Api:BaseUrl", baseUrl), ("Api:Token", "a-token"));

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => ApiClientOptions.Read(configuration));
    }

    /// <summary>Negative: no token is a setup mistake.</summary>
    [Fact]
    public void Read_NoToken_Throws()
    {
        // Arrange
        var configuration = Configuration(("Api:BaseUrl", "http://nfhotel-api:8080"));

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => ApiClientOptions.Read(configuration));
    }

    /// <summary>Boundary: a timeout of zero, a negative one and text fall back to the default; a positive number is used.</summary>
    [Theory]
    [InlineData("0", ApiClientOptions.DefaultTimeoutSeconds)]
    [InlineData("-5", ApiClientOptions.DefaultTimeoutSeconds)]
    [InlineData("soon", ApiClientOptions.DefaultTimeoutSeconds)]
    [InlineData("1", 1)]
    [InlineData("90", 90)]
    public void Read_TimeoutSetting_UsesItOnlyWhenPositive(string setting, int expected)
    {
        // Arrange
        var configuration = Configuration(("Api:BaseUrl", "http://nfhotel-api:8080"), ("Api:Token", "a-token"), ("Api:TimeoutSeconds", setting));

        // Act
        var options = ApiClientOptions.Read(configuration);

        // Assert
        Assert.Equal(expected, options.TimeoutSeconds);
    }

    #endregion

    #region Registration

    /// <summary>Positive: the three use-case interfaces resolve to the clients.</summary>
    [Fact]
    public void AddApiClient_ValidSettings_RegistersTheThreeServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiClient(Configuration(("Api:BaseUrl", "http://nfhotel-api:8080"), ("Api:Token", "a-token")));

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<ApiBookingService>(scope.ServiceProvider.GetRequiredService<IBookingService>());
        Assert.IsType<ApiGuestService>(scope.ServiceProvider.GetRequiredService<IGuestService>());
        Assert.IsType<ApiRoomService>(scope.ServiceProvider.GetRequiredService<IRoomService>());
    }

    /// <summary>Negative: without settings the registration itself fails, so the start-up stops.</summary>
    [Fact]
    public void AddApiClient_NoSettings_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => services.AddApiClient(Configuration()));
    }

    #endregion

    #region Helpers

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();

    #endregion
}