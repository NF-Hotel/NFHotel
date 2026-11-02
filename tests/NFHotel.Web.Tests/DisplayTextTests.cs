using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin;

namespace NFHotel.Web.Tests;

/// <summary>The texts and badge classes every screen uses for the states of the domain (MIL-054 task 7).</summary>
public sealed class DisplayTextTests
{
    /// <summary>Equivalence: every booking status has its own text and badge.</summary>
    [Theory]
    [InlineData(BookingStatus.Pending, "Pending", "badge badge-pending")]
    [InlineData(BookingStatus.Confirmed, "Confirmed", "badge badge-confirmed")]
    [InlineData(BookingStatus.CheckedIn, "Checked in", "badge badge-checkedin")]
    [InlineData(BookingStatus.CheckedOut, "Checked out", "badge badge-checkedout")]
    [InlineData(BookingStatus.Cancelled, "Cancelled", "badge badge-cancelled")]
    public void Describe_BookingStatus_HasItsTextAndBadge(BookingStatus status, string text, string badge)
    {
        Assert.Equal(text, DisplayText.Describe(status));
        Assert.Equal(badge, BadgeClass.For(status));
    }

    /// <summary>Equivalence: every room status has its own text and badge.</summary>
    [Theory]
    [InlineData(RoomStatus.Available, "In service", "badge badge-confirmed")]
    [InlineData(RoomStatus.OutOfService, "Out of service", "badge badge-blocked")]
    [InlineData(RoomStatus.Maintenance, "Maintenance", "badge badge-service")]
    public void Describe_RoomStatus_HasItsTextAndBadge(RoomStatus status, string text, string badge)
    {
        Assert.Equal(text, DisplayText.Describe(status));
        Assert.Equal(badge, BadgeClass.For(status));
    }

    /// <summary>Equivalence: every housekeeping status has its own text and badge.</summary>
    [Theory]
    [InlineData(HousekeepingStatus.Clean, "Clean", "badge badge-clean")]
    [InlineData(HousekeepingStatus.DailyCleaningDue, "Daily cleaning due", "badge badge-cleaning")]
    [InlineData(HousekeepingStatus.DepartureCleaningDue, "Departure cleaning due", "badge badge-cleaning")]
    [InlineData(HousekeepingStatus.CleaningInProgress, "Cleaning in progress", "badge badge-cleaning")]
    [InlineData(HousekeepingStatus.ServiceRequired, "Service needed", "badge badge-service")]
    [InlineData(HousekeepingStatus.ServiceInProgress, "Service in progress", "badge badge-service")]
    public void Describe_HousekeepingStatus_HasItsTextAndBadge(HousekeepingStatus status, string text, string badge)
    {
        Assert.Equal(text, DisplayText.Describe(status));
        Assert.Equal(badge, BadgeClass.For(status));
    }

    /// <summary>Equivalence: every room size has its text.</summary>
    [Theory]
    [InlineData(RoomSize.Single, "Single room")]
    [InlineData(RoomSize.Double, "Double room")]
    [InlineData(RoomSize.Suite, "Suite")]
    public void Describe_RoomSize_HasItsText(RoomSize size, string text)
    {
        Assert.Equal(text, DisplayText.Describe(size));
    }

    /// <summary>Edge: a value the screen does not know falls back to its number and the plain badge, never an exception.</summary>
    [Fact]
    public void Describe_UnknownValue_FallsBack()
    {
        Assert.Equal("99", DisplayText.Describe((BookingStatus)99));
        Assert.Equal("99", DisplayText.Describe((RoomStatus)99));
        Assert.Equal("99", DisplayText.Describe((HousekeepingStatus)99));
        Assert.Equal("99", DisplayText.Describe((RoomSize)99));
        Assert.Equal("badge", BadgeClass.For((BookingStatus)99));
        Assert.Equal("badge", BadgeClass.For((RoomStatus)99));
        Assert.Equal("badge", BadgeClass.For((HousekeepingStatus)99));
    }

    /// <summary>Equivalence: each alert kind has its class; an unknown kind is shown as information.</summary>
    [Theory]
    [InlineData(AlertKind.Error, "alert alert-error")]
    [InlineData(AlertKind.Ok, "alert alert-ok")]
    [InlineData(AlertKind.Info, "alert alert-info")]
    [InlineData((AlertKind)99, "alert alert-info")]
    public void For_AlertKind_HasItsClass(AlertKind kind, string expected)
    {
        Assert.Equal(expected, BadgeClass.For(kind));
    }

    /// <summary>Edge: the back office writes dates in one format and a missing date as a dash.</summary>
    [Fact]
    public void AdminFormat_DatesAndMissingValues_AreWrittenOneWay()
    {
        Assert.Equal("10 Mar 2026", AdminFormat.Date(new DateOnly(2026, 3, 10)));
        Assert.Equal("10 Mar 2026", AdminFormat.Date((DateOnly?)new DateOnly(2026, 3, 10)));
        Assert.Equal("—", AdminFormat.Date((DateOnly?)null));
        Assert.Equal("10 Mar 2026 14:05", AdminFormat.DateAndTime(new DateTimeOffset(2026, 3, 10, 14, 5, 0, TimeSpan.Zero)));
        Assert.Equal("—", AdminFormat.DateAndTime(null));
    }
}
