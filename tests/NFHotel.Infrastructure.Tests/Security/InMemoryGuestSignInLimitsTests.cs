using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>
/// The limits of the guest sign-in (MIL-085, ADR-0049 decision 6): at most 5 failed tries per reference and 20 per caller
/// address in a fixed window of 15 minutes, counted before the booking is read as one step with the check, in two
/// stores of their own that never hold more than their bound.
/// </summary>
public sealed class InMemoryGuestSignInLimitsTests
{
    private const string Address = "203.0.113.10";

    private static readonly Guid Reference = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

    #region Per reference

    /// <summary>Boundary: five tries for one reference are counted, the sixth is refused, even from other addresses.</summary>
    [Fact]
    public void TryCount_SixTriesForOneReference_CountsFiveAndRefusesTheSixth()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());

        // Act
        bool[] counted = [.. Enumerable.Range(1, 6).Select(number => limits.TryCount(Reference, AddressNumber(number)))];

        // Assert
        Assert.Equal([true, true, true, true, true, false], counted);
    }

    /// <summary>State transition: a success clears the window of the reference, so the next five tries are counted again.</summary>
    [Fact]
    public void Succeeded_AfterFourTries_ClearsTheWindowOfTheReference()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        for (int attempt = 0; attempt < 4; attempt++)
        {
            limits.TryCount(Reference, Address);
        }

        // Act
        limits.Succeeded(Reference, Address);
        bool[] counted = [.. Enumerable.Range(0, 6).Select(_ => limits.TryCount(Reference, AddressNumber(9)))];

        // Assert
        Assert.Equal([true, true, true, true, true, false], counted);
    }

    /// <summary>
    /// Boundary: the window is fixed from the first try. A minute before its end the reference is still refused, at the
    /// end it is counted in a new window, and a refused try does not push the end away.
    /// </summary>
    [Fact]
    public void TryCount_RefusedThenAtTheEndOfTheWindow_CountsInANewWindow()
    {
        // Arrange
        FakeClock clock = new() { TimeOfDay = TimeSpan.FromHours(8) };
        InMemoryGuestSignInLimits limits = Build(clock);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            limits.TryCount(Reference, AddressNumber(attempt));
        }

        // Act
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(14);
        bool beforeTheEnd = limits.TryCount(Reference, Address);
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(15);
        bool atTheEnd = limits.TryCount(Reference, Address);

        // Assert
        Assert.False(beforeTheEnd);
        Assert.True(atTheEnd);
    }

    #endregion

    #region Per address

    /// <summary>
    /// Boundary: one address that tries twenty references is counted twenty times and refused at the twenty-first, while
    /// another address still gets in for the same reference.
    /// </summary>
    [Fact]
    public void TryCount_TwentyFirstTryFromOneAddress_IsRefused()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        bool[] counted = [.. Enumerable.Range(0, 20).Select(_ => limits.TryCount(Guid.NewGuid(), Address))];

        // Act
        bool twentyFirst = limits.TryCount(Reference, Address);
        bool otherAddress = limits.TryCount(Reference, AddressNumber(2));

        // Assert
        Assert.All(counted, Assert.True);
        Assert.False(twentyFirst);
        Assert.True(otherAddress);
    }

    /// <summary>Equivalence (ADR-0049 decision 6): an unreadable reference counts against the address alone.</summary>
    [Fact]
    public void TryCount_UnreadableReference_CountsTheAddressOnly()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        bool[] counted = [.. Enumerable.Range(0, 20).Select(_ => limits.TryCount(null, Address))];

        // Act
        bool twentyFirst = limits.TryCount(null, Address);
        bool referenceElsewhere = limits.TryCount(Reference, AddressNumber(2));

        // Assert
        Assert.All(counted, Assert.True);
        Assert.False(twentyFirst);
        Assert.True(referenceElsewhere);
        Assert.Equal(1, limits.TrackedReferenceCount);
    }

    /// <summary>
    /// State transition (ADR-0049 decision 6): a success gives the address its try back but keeps the window, so the own
    /// booking cannot be used to reset the count of an address.
    /// </summary>
    [Fact]
    public void Succeeded_AfterNineteenFailures_GivesOneTryBackAndKeepsTheWindow()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        for (int attempt = 0; attempt < 19; attempt++)
        {
            limits.TryCount(Guid.NewGuid(), Address);
        }

        limits.TryCount(Reference, Address);

        // Act
        limits.Succeeded(Reference, Address);
        bool twentieth = limits.TryCount(Guid.NewGuid(), Address);
        bool twentyFirst = limits.TryCount(Guid.NewGuid(), Address);

        // Assert
        Assert.True(twentieth);
        Assert.False(twentyFirst);
    }

    #endregion

    #region One step for both counts

    /// <summary>Security: a try over the limit of the address counts nothing for the reference.</summary>
    [Fact]
    public void TryCount_AddressOverItsLimit_CountsNothingForTheReference()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        for (int attempt = 0; attempt < 20; attempt++)
        {
            limits.TryCount(null, Address);
        }

        // Act
        bool[] refused = [.. Enumerable.Range(0, 10).Select(_ => limits.TryCount(Reference, Address))];
        bool[] elsewhere = [.. Enumerable.Range(1, 6).Select(number => limits.TryCount(Reference, AddressNumber(number)))];

        // Assert
        Assert.All(refused, Assert.False);
        Assert.Equal([true, true, true, true, true, false], elsewhere);
    }

    /// <summary>Security: a try over the limit of the reference counts nothing for the address.</summary>
    [Fact]
    public void TryCount_ReferenceOverItsLimit_CountsNothingForTheAddress()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());
        for (int attempt = 0; attempt < 5; attempt++)
        {
            limits.TryCount(Reference, AddressNumber(attempt));
        }

        // Act
        bool[] refused = [.. Enumerable.Range(0, 30).Select(_ => limits.TryCount(Reference, Address))];
        bool[] others = [.. Enumerable.Range(0, 21).Select(_ => limits.TryCount(Guid.NewGuid(), Address))];

        // Assert
        Assert.All(refused, Assert.False);
        Assert.Equal(20, others.Count(counted => counted));
        Assert.False(others[^1]);
    }

    /// <summary>
    /// Concurrency: tries for one reference from many threads, released at once, are counted only up to the limit, because
    /// the check and the count are one step. The rounds repeat only to widen the interleaving.
    /// </summary>
    [Fact]
    public void TryCount_ManyThreadsAtOnce_CountsNoMoreThanTheLimits()
    {
        for (int round = 0; round < 10; round++)
        {
            // Arrange
            InMemoryGuestSignInLimits limits = Build(new FakeClock());
            using ManualResetEventSlim release = new(initialState: false);
            int perReference = 0;
            int perAddress = 0;
            Thread[] threads = [.. Enumerable.Range(0, 32).Select(number => new Thread(() =>
            {
                release.Wait();
                if (limits.TryCount(Reference, AddressNumber(number)))
                {
                    Interlocked.Increment(ref perReference);
                }

                if (limits.TryCount(Guid.NewGuid(), Address))
                {
                    Interlocked.Increment(ref perAddress);
                }
            }))];

            // Act
            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            release.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Equal(5, perReference);
            Assert.Equal(20, perAddress);
        }
    }

    #endregion

    #region The bound of the stores

    /// <summary>
    /// Boundary (ADR-0049 decision 6): 100,000 failed tries for as many references from as many addresses leave at most
    /// 10,000 windows in each store, so a flood cannot grow the memory without a bound.
    /// </summary>
    [Fact]
    public void TryCount_HundredThousandKeys_KeepsEachStoreWithinItsBound()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());

        // Act
        for (int number = 0; number < 100_000; number++)
        {
            limits.TryCount(Guid.NewGuid(), AddressNumber(number));
        }

        // Assert
        Assert.InRange(limits.TrackedReferenceCount, 1, 10_000);
        Assert.InRange(limits.TrackedAddressCount, 1, 10_000);
    }

    /// <summary>Boundary: a full store drops the windows that have run out before any other.</summary>
    [Fact]
    public void TryCount_FullStore_DropsTheRunOutWindowsFirst()
    {
        // Arrange
        FakeClock clock = new() { TimeOfDay = TimeSpan.FromHours(8) };
        InMemoryGuestSignInLimits limits = Build(clock, maxTrackedKeys: 3);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();
        limits.TryCount(first, AddressNumber(1));
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(10);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            limits.TryCount(second, AddressNumber(2));
        }

        limits.TryCount(third, AddressNumber(3));

        // Act
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(16);
        limits.TryCount(Guid.NewGuid(), AddressNumber(4));

        // Assert: the window of the first reference had run out; the second one is still refused.
        Assert.Equal(3, limits.TrackedReferenceCount);
        Assert.False(limits.TryCount(second, AddressNumber(5)));
    }

    /// <summary>
    /// Boundary: a full store with no window that has run out drops the window that started first, which is the nearest to
    /// its end, so the least protection is lost.
    /// </summary>
    [Fact]
    public void TryCount_FullStoreWithoutRunOutWindows_DropsTheWindowThatStartedFirst()
    {
        // Arrange
        FakeClock clock = new() { TimeOfDay = TimeSpan.FromHours(8) };
        InMemoryGuestSignInLimits limits = Build(clock, maxTrackedKeys: 3);
        Guid oldest = Guid.NewGuid();
        Guid younger = Guid.NewGuid();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            limits.TryCount(oldest, AddressNumber(attempt));
        }

        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            limits.TryCount(younger, AddressNumber(attempt + 10));
        }

        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(2);
        limits.TryCount(Guid.NewGuid(), AddressNumber(20));

        // Act
        clock.TimeOfDay = TimeSpan.FromHours(8) + TimeSpan.FromMinutes(3);
        limits.TryCount(Guid.NewGuid(), AddressNumber(21));

        // Assert
        Assert.Equal(3, limits.TrackedReferenceCount);
        Assert.False(limits.TryCount(younger, AddressNumber(30)));
        Assert.True(limits.TryCount(oldest, AddressNumber(31)));
    }

    #endregion

    #region Bad input

    /// <summary>Negative: a missing address is a programming error; the caller gives the bucket of unknown addresses instead.</summary>
    [Fact]
    public void TryCountAndSucceeded_NullAddress_Throw()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Build(new FakeClock());

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => limits.TryCount(Reference, null!));
        Assert.Throws<ArgumentNullException>(() => limits.Succeeded(Reference, null!));
    }

    /// <summary>Negative: settings below one stop the start-up, so a typing mistake cannot switch a limit off.</summary>
    /// <param name="setting">The setting that is set to zero.</param>
    [Theory]
    [InlineData(nameof(GuestLookupOptions.MaxFailuresPerReference))]
    [InlineData(nameof(GuestLookupOptions.MaxFailuresPerAddress))]
    [InlineData(nameof(GuestLookupOptions.WindowMinutes))]
    [InlineData(nameof(GuestLookupOptions.MaxTrackedKeys))]
    public void Constructor_SettingBelowOne_Throws(string setting)
    {
        // Arrange
        GuestLookupOptions options = setting switch
        {
            nameof(GuestLookupOptions.MaxFailuresPerReference) => new GuestLookupOptions { MaxFailuresPerReference = 0 },
            nameof(GuestLookupOptions.MaxFailuresPerAddress) => new GuestLookupOptions { MaxFailuresPerAddress = 0 },
            nameof(GuestLookupOptions.WindowMinutes) => new GuestLookupOptions { WindowMinutes = 0 },
            _ => new GuestLookupOptions { MaxTrackedKeys = 0 },
        };

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryGuestSignInLimits(options, new FakeClock()));
    }

    /// <summary>Positive: the defaults are the numbers of ADR-0049 decision 6.</summary>
    [Fact]
    public void Options_Defaults_AreTheNumbersOfTheDecision()
    {
        // Act
        GuestLookupOptions options = new();

        // Assert
        Assert.Equal((5, 20, 15, 10_000), (options.MaxFailuresPerReference, options.MaxFailuresPerAddress, options.WindowMinutes, options.MaxTrackedKeys));
        Assert.Equal("GuestLookup", GuestLookupOptions.SectionName);
    }

    #endregion

    #region Helpers

    private static InMemoryGuestSignInLimits Build(FakeClock clock, int maxTrackedKeys = 10_000) =>
        new(new GuestLookupOptions { MaxTrackedKeys = maxTrackedKeys }, clock);

    /// <summary>An address of the documentation ranges for a number (RFC 3849), so many callers can be told apart.</summary>
    private static string AddressNumber(int number) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"2001:db8::{number:x}");

    #endregion
}
