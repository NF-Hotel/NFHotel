using NFHotel.Application.Dto.Guests;
using NFHotel.TestSupport;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The one-time tickets that carry the session of a booking just made from the booking flow to the guest cookie
/// (MIL-018, ADR-0047 decision 2): once, for ten minutes, and never longer than the session.
/// </summary>
public sealed class GuestSessionTicketsTests
{
    private readonly FakeClock _clock = new() { Today = new DateOnly(2030, 1, 1), TimeOfDay = TimeSpan.FromHours(8) };

    #region Redeem

    /// <summary>Positive: a ticket just issued gives its session, and only once.</summary>
    [Fact]
    public void Redeem_TicketJustIssued_GivesTheSessionOnce()
    {
        // Arrange
        GuestSessionTickets tickets = new(_clock);
        GuestSessionDto session = Session(hoursLeft: 2);
        string ticket = tickets.Issue(session);

        // Act
        GuestSessionDto? first = tickets.Redeem(ticket);
        GuestSessionDto? second = tickets.Redeem(ticket);

        // Assert
        Assert.Equal(session, first);
        Assert.Null(second);
    }

    /// <summary>Boundary: a ticket is redeemed until just before ten minutes, and not at ten minutes.</summary>
    /// <param name="secondsLater">How long after the issue the ticket is redeemed.</param>
    /// <param name="redeemed">Whether it still gives the session.</param>
    [Theory]
    [InlineData(599, true)]
    [InlineData(600, false)]
    public void Redeem_AroundTheLifetime_GivesTheSessionOnlyWithinIt(int secondsLater, bool redeemed)
    {
        // Arrange
        GuestSessionTickets tickets = new(_clock);
        string ticket = tickets.Issue(Session(hoursLeft: 2));
        _clock.TimeOfDay += TimeSpan.FromSeconds(secondsLater);

        // Act
        GuestSessionDto? session = tickets.Redeem(ticket);

        // Assert
        Assert.Equal(redeemed, session is not null);
    }

    /// <summary>Negative: a ticket whose session has ended gives nothing, even within the ten minutes.</summary>
    [Fact]
    public void Redeem_SessionThatHasEnded_GivesNothing()
    {
        // Arrange
        GuestSessionTickets tickets = new(_clock);
        string ticket = tickets.Issue(Session(hoursLeft: 0));

        // Act
        GuestSessionDto? session = tickets.Redeem(ticket);

        // Assert
        Assert.Null(session);
    }

    /// <summary>Negative: a missing, empty or unknown ticket gives nothing.</summary>
    /// <param name="ticket">The ticket from the form.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void Redeem_UnknownTicket_GivesNothing(string? ticket)
    {
        // Arrange
        GuestSessionTickets tickets = new(_clock);
        tickets.Issue(Session(hoursLeft: 2));

        // Act
        GuestSessionDto? session = tickets.Redeem(ticket);

        // Assert
        Assert.Null(session);
    }

    /// <summary>
    /// Race condition: many threads that redeem one ticket at the same moment, released at once by a latch, get its
    /// session once between them. The rounds repeat only to widen the interleaving.
    /// </summary>
    [Fact]
    public void Redeem_ManyThreadsAtOnce_GivesTheSessionOnce()
    {
        for (int round = 0; round < 10; round++)
        {
            // Arrange
            GuestSessionTickets tickets = new(_clock);
            GuestSessionDto session = Session(hoursLeft: 2);
            string ticket = tickets.Issue(session);
            using ManualResetEventSlim release = new(initialState: false);
            GuestSessionDto?[] redeemed = new GuestSessionDto?[16];
            Thread[] threads = [.. Enumerable.Range(0, redeemed.Length).Select(index => new Thread(() =>
            {
                release.Wait();
                redeemed[index] = tickets.Redeem(ticket);
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
            Assert.Equal(session, Assert.Single(redeemed, value => value is not null));
        }
    }

    #endregion

    #region Issue

    /// <summary>Security: each ticket is 64 hexadecimal characters of its own, so a ticket cannot be guessed from another.</summary>
    [Fact]
    public void Issue_TwoSessions_GivesTwoDifferentRandomTickets()
    {
        // Arrange
        GuestSessionTickets tickets = new(_clock);

        // Act
        string first = tickets.Issue(Session(hoursLeft: 2));
        string second = tickets.Issue(Session(hoursLeft: 2));

        // Assert
        Assert.NotEqual(first, second);
        Assert.All([first, second], ticket => Assert.Matches("^[0-9A-F]{64}$", ticket));
    }

    /// <summary>Negative: the clock and the session are required.</summary>
    [Fact]
    public void Constructor_MissingClockOrSession_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new GuestSessionTickets(null!));
        Assert.Throws<ArgumentNullException>(() => new GuestSessionTickets(_clock).Issue(null!));
    }

    #endregion

    #region Helpers

    private GuestSessionDto Session(int hoursLeft) => new("guest-token", _clock.UtcNow.AddHours(hoursLeft));

    #endregion
}
