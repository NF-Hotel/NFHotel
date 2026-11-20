using System.Globalization;

using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The table "Limits of the booking flow" of the rule ledger against the code (MIL-087 criterion 2, ADR-0051 decision 1):
/// a number, a window, a setting or a code that differs from the table fails here, so the table and the API cannot drift
/// apart.
/// </summary>
public sealed class LimitsTableTests
{
    // The setting cell of a limit whose numbers are constants of the code.
    private const string NoSetting = "None";

    /// <summary>Positive: the table names the limits of the security events, in their order, and no other.</summary>
    [Fact]
    public void Table_Names_AreTheLimitsOfTheSecurityEvents()
    {
        // Act
        string[] names = [.. LimitsTable.Read().Select(row => row.Limit).Distinct()];

        // Assert
        Assert.Equal(LimitNames.All, names);
    }

    /// <summary>
    /// Positive: each number of the table is the default of the setting that the row names, and a limit without a setting
    /// has the number of its constant in the code (ADR-0052 decision 6).
    /// </summary>
    [Fact]
    public void Table_Numbers_AreTheDefaultsOfTheirSettings()
    {
        // Arrange
        Dictionary<string, object> defaults = new()
        {
            [SenderLimitOptions.SectionName] = new SenderLimitOptions(),
            [GuestLookupOptions.SectionName] = new GuestLookupOptions(),
        };
        Dictionary<(string Limit, string Window), int> constants = new()
        {
            [(LimitNames.VerificationMail, "1 hour")] = BookingRequestService.MailsPerHour,
            [(LimitNames.VerificationMail, "1 day")] = BookingRequestService.MailsPerDay,
        };

        // Act
        LimitRule[] rows = [.. LimitsTable.Read()];

        // Assert
        Assert.All(rows, row =>
        {
            int value = row.Setting == NoSetting ? constants[(row.Limit, row.Window)] : DefaultOf(defaults, row.Setting);
            Assert.Equal((row.Limit, row.Setting, row.Number), (row.Limit, row.Setting, value));
        });
    }

    /// <summary>Positive (criterion 5): each store of counts per address holds at most the 10,000 keys that the ledger names.</summary>
    [Fact]
    public void Settings_MaxTrackedKeys_BoundEachStoreAtTenThousand()
    {
        // Act and assert
        Assert.Equal(10_000, new SenderLimitOptions().MaxTrackedKeys);
        Assert.Equal(10_000, new GuestLookupOptions().MaxTrackedKeys);
    }

    /// <summary>
    /// Positive: each window of the table is a window of its limit, the limit per email has none, and the limit per mailbox
    /// has the hour and the day of its two constants.
    /// </summary>
    [Fact]
    public void Table_Windows_AreTheWindowsOfTheLimits()
    {
        // Arrange
        Dictionary<string, TimeSpan?[]> windows = new()
        {
            [LimitNames.Search] = [InMemorySenderLimits.SearchWindow],
            [LimitNames.BookingHour] = [InMemorySenderLimits.BookingHourWindow],
            [LimitNames.BookingDay] = [InMemorySenderLimits.BookingDayWindow],
            [LimitNames.Email] = [null],
            [LimitNames.GuestSignIn] = [TimeSpan.FromMinutes(new GuestLookupOptions().WindowMinutes)],
            [LimitNames.VerificationMail] = [TimeSpan.FromHours(1), TimeSpan.FromDays(1)],
        };

        // Act
        (string Limit, TimeSpan? Window)[] rows = [.. LimitsTable.Read().Select(row => (row.Limit, WindowOf(row.Window)))];

        // Assert
        Assert.All(rows, row => Assert.Contains(row.Window, windows[row.Limit]));
    }

    /// <summary>
    /// Positive: every refusal of the table is 429 with a code of the API, and only a limit of the booking requests and the
    /// searches names Retry-After: the guest sign-in keeps the text of ADR-0049, and the limit per mailbox tells nothing
    /// about the mailbox of somebody else (ADR-0052 decision 6).
    /// </summary>
    [Fact]
    public void Table_Refusals_Are429WithACodeOfTheApi()
    {
        // Arrange
        string[] codes = [ErrorCodes.Booking.TooManyRequests, ErrorCodes.Booking.TooManyLookups, ErrorCodes.Booking.TooManyMails];
        string[] withoutRetryAfter = [LimitNames.GuestSignIn, LimitNames.VerificationMail];

        // Act
        LimitRule[] rows = [.. LimitsTable.Read()];

        // Assert
        Assert.All(rows, row =>
        {
            Assert.StartsWith("429, ", row.Refusal, StringComparison.Ordinal);
            Assert.Contains(row.Refusal.Split('`')[1], codes);
            Assert.Equal(row.Window != "None" && !withoutRetryAfter.Contains(row.Limit), row.Refusal.Contains("Retry-After", StringComparison.Ordinal));
        });
    }

    // "SenderLimits:SearchesPerMinute": the default of a property of the options of its section.
    private static int DefaultOf(Dictionary<string, object> defaults, string setting)
    {
        string[] parts = setting.Split(':');
        object options = defaults[parts[0]];
        return Assert.IsType<int>(options.GetType().GetProperty(parts[1])?.GetValue(options));
    }

    // "1 minute", "15 minutes", "1 hour", "1 day" or "None".
    private static TimeSpan? WindowOf(string text)
    {
        if (text == "None")
        {
            return null;
        }

        string[] parts = text.Split(' ');
        int count = int.Parse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
        return parts[1].TrimEnd('s') switch
        {
            "minute" => TimeSpan.FromMinutes(count),
            "hour" => TimeSpan.FromHours(count),
            "day" => TimeSpan.FromDays(count),
            _ => throw new InvalidOperationException($"The window \"{text}\" is not a number of minutes, hours or days."),
        };
    }
}
