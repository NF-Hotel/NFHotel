using System.Collections.Concurrent;
using System.Security.Cryptography;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;

namespace NFHotel.Web.Security;

/// <summary>
/// One-time tickets that carry the guest session of a booking just made from the booking flow to the guest cookie
/// (MIL-018, ADR-0047 decision 2).
/// </summary>
/// <remarks>
/// The booking flow runs in an interactive circuit, and a circuit cannot set a cookie: only the answer to a request can.
/// The flow therefore keeps the session here under a random ticket, and the confirmation posts the ticket to
/// <see cref="GuestSignInRegistration.SessionPath"/>, whose answer sets the cookie. A ticket opens one session once,
/// for a few minutes; the session itself stays on the server, so the page never holds the guest token.
/// </remarks>
public sealed class GuestSessionTickets
{
    #region Fields

    /// <summary>How long a ticket can be redeemed after it was issued.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private const int TicketBytes = 32;

    private readonly ConcurrentDictionary<string, Ticket> _tickets = new(StringComparer.Ordinal);
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the store.</summary>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    public GuestSessionTickets(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
    }

    #endregion Constructors

    #region Public methods

    /// <summary>Keeps a session under a new ticket, and drops the tickets that have run out.</summary>
    /// <param name="session">The guest session of the booking just made.</param>
    /// <returns>The ticket: 64 hexadecimal characters from a cryptographic random source.</returns>
    public string Issue(GuestSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        DateTimeOffset now = _clock.UtcNow;
        foreach (KeyValuePair<string, Ticket> stale in _tickets.Where(entry => !entry.Value.IsValidAt(now)))
        {
            _tickets.TryRemove(stale.Key, out Ticket? _);
        }

        string ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(TicketBytes));
        _tickets[ticket] = new Ticket(session, now.Add(Lifetime));
        return ticket;
    }

    /// <summary>Gives the session of a ticket and forgets the ticket, so it works once.</summary>
    /// <param name="ticket">The ticket from the form, or <c>null</c>.</param>
    /// <returns>The session, or <c>null</c> for an unknown, used or expired ticket, or a session that has ended.</returns>
    public GuestSessionDto? Redeem(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket) || !_tickets.TryRemove(ticket, out Ticket? found))
        {
            return null;
        }

        return found.IsValidAt(_clock.UtcNow) ? found.Session : null;
    }

    #endregion Public methods

    #region Nested types

    /// <summary>A session and the time until which its ticket can be redeemed.</summary>
    /// <param name="Session">The guest session.</param>
    /// <param name="RedeemBefore">The end of the ticket's few minutes.</param>
    private sealed record Ticket(GuestSessionDto Session, DateTimeOffset RedeemBefore)
    {
        /// <summary>Tells whether the ticket and its session are both still valid.</summary>
        /// <param name="now">The current time.</param>
        /// <returns>True before the end of the ticket and of the session.</returns>
        public bool IsValidAt(DateTimeOffset now) => now < RedeemBefore && now < Session.ExpiresAt;
    }

    #endregion Nested types
}
