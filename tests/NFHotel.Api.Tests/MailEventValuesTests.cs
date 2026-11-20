using NFHotel.Api.Security;
using NFHotel.Application.Ports;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The values that an event of a mail holds (MIL-088, ADR-0053 decision 5): the kind of the mail by its name and the
/// reason in a word, never a text of the relay.
/// </summary>
public sealed class MailEventValuesTests
{
    /// <summary>Positive: each kind of mail has the name that the events, the counter and the alerts use.</summary>
    /// <param name="kind">The kind of the mail.</param>
    /// <param name="expected">Its name.</param>
    [Theory]
    [InlineData(MailKind.VerificationLink, "verification_link")]
    [InlineData(MailKind.BookingReceipt, "booking_receipt")]
    public void MailNameOf_EachKind_IsItsName(MailKind kind, string expected)
    {
        // Act and assert
        Assert.Equal(expected, SecurityEvents.MailNameOf(kind));
        Assert.Contains(expected, SecurityEvents.MailNames);
    }

    /// <summary>Negative: a number that is no kind of mail is refused, so no event holds a name that no alert knows.</summary>
    [Fact]
    public void MailNameOf_NoKind_IsRefused()
    {
        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => SecurityEvents.MailNameOf((MailKind)7));
    }

    /// <summary>Positive: every reason of the port is written as it is.</summary>
    [Fact]
    public void MailReasonOf_EveryReasonOfThePort_IsKept()
    {
        // Act and assert
        Assert.All(MailFailureReasons.All, reason => Assert.Equal(reason, SecurityEvents.MailReasonOf(reason)));
    }

    /// <summary>
    /// Security (ASVS 16.2.5): any other text, a text with a line break and no text at all are written as
    /// <c>other</c>, so no text of a relay and no second field reaches the log.
    /// </summary>
    /// <param name="reason">The reason that reached the event.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Connect")]
    [InlineData("connect\nkind=mail_fuse_blown")]
    [InlineData("535 5.7.8 Username and Password not accepted")]
    public void MailReasonOf_OtherText_IsOther(string? reason)
    {
        // Act and assert
        Assert.Equal(MailFailureReasons.Other, SecurityEvents.MailReasonOf(reason));
    }
}
