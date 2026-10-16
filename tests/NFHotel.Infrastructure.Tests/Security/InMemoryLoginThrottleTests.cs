using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>The sign-in throttle of ADR-0005: five failures per user name per fixed window, then refusal until the window has passed.</summary>
public sealed class InMemoryLoginThrottleTests
{
    #region Counting

    /// <summary>Boundary: four failures do not block, the fifth does.</summary>
    [Fact]
    public void IsBlocked_AfterFourAndFiveFailures_BlocksOnTheFifth()
    {
        // Arrange
        var throttle = Build(new FakeClock());

        // Act
        for (var failure = 0; failure < 4; failure++)
        {
            throttle.RecordFailure("rita");
        }

        var afterFour = throttle.IsBlocked("rita");
        throttle.RecordFailure("rita");
        var afterFive = throttle.IsBlocked("rita");

        // Assert
        Assert.False(afterFour);
        Assert.True(afterFive);
    }

    /// <summary>Equivalence: a name that never failed is not blocked, and failures of one name do not block another.</summary>
    [Fact]
    public void IsBlocked_OtherName_IsNotBlocked()
    {
        // Arrange
        var throttle = Build(new FakeClock());
        for (var failure = 0; failure < 5; failure++)
        {
            throttle.RecordFailure("rita");
        }

        // Act
        var other = throttle.IsBlocked("mona");

        // Assert
        Assert.False(other);
    }

    /// <summary>State transition: a success clears the failures of the name.</summary>
    [Fact]
    public void Reset_AfterFailures_ClearsTheCount()
    {
        // Arrange
        var throttle = Build(new FakeClock());
        for (var failure = 0; failure < 5; failure++)
        {
            throttle.RecordFailure("rita");
        }

        // Act
        throttle.Reset("rita");

        // Assert
        Assert.False(throttle.IsBlocked("rita"));
    }

    #endregion

    #region The window

    /// <summary>Boundary: one minute before the window ends the name is still blocked, at the end it is free.</summary>
    [Fact]
    public void IsBlocked_AtTheEndOfTheWindow_IsFree()
    {
        // Arrange
        var clock = new FakeClock { TimeOfDay = TimeSpan.FromHours(8) };
        var throttle = Build(clock);
        for (var failure = 0; failure < 5; failure++)
        {
            throttle.RecordFailure("rita");
        }

        // Act
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(14);
        var beforeTheEnd = throttle.IsBlocked("rita");
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(15);
        var atTheEnd = throttle.IsBlocked("rita");

        // Assert
        Assert.True(beforeTheEnd);
        Assert.False(atTheEnd);
    }

    /// <summary>The window is fixed: a refused attempt that is not counted does not push its end away, and a failure after the end starts a new window.</summary>
    [Fact]
    public void RecordFailure_AfterTheWindow_StartsANewWindowWithOneFailure()
    {
        // Arrange
        var clock = new FakeClock { TimeOfDay = TimeSpan.FromHours(8) };
        var throttle = Build(clock);
        for (var failure = 0; failure < 5; failure++)
        {
            throttle.RecordFailure("rita");
        }

        // Act
        clock.TimeOfDay = TimeSpan.FromHours(9);
        throttle.RecordFailure("rita");

        // Assert
        Assert.False(throttle.IsBlocked("rita"));
    }

    #endregion

    #region Bad input

    /// <summary>Negative: a missing key is a programming error.</summary>
    [Fact]
    public void IsBlocked_NullKey_Throws()
    {
        // Arrange
        var throttle = Build(new FakeClock());

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => throttle.IsBlocked(null!));
    }

    #endregion

    #region Helpers

    private static InMemoryLoginThrottle Build(FakeClock clock) =>
        new(new StaffSecurityOptions { SigningKey = "unused", MaxFailures = 5, ThrottleWindowMinutes = 15 }, clock);

    #endregion
}