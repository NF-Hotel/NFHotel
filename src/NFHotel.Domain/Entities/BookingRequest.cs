using System.Buffers;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A booking asked for on the public site and not yet verified (MIL-088, ADR-0052): the stay, the room size, the party
/// and the fields of the guest, kept until the guest presses the button of the link in the mail or the link lapses.
/// </summary>
/// <remarks>
/// A request holds no room and is not a booking: staff never see it, and no report counts it (decision 9). The token of
/// the link is never stored, only its SHA-256 hash (decision 2), and the key of the mailbox makes one mailbox hold one
/// open request (decision 6). The fields of the guest are the normal forms that <see cref="Guest.Create"/> checked, and
/// the booking checks them again when the guest verifies.
/// </remarks>
public sealed class BookingRequest : Entity<int>
{
    #region Fields

    /// <summary>The number of characters of a SHA-256 hash written as lower-case hex.</summary>
    public const int HashLength = 64;

    // The digits of a hash; a search value, so the check makes no lambda, which would be a class of its own here.
    private static readonly SearchValues<char> LowerHexDigits = SearchValues.Create("0123456789abcdef");

    #endregion Fields

    #region Constructors

    /// <summary>For EF Core. Use <see cref="Create"/> in code.</summary>
    private BookingRequest()
    {
    }

    #endregion Constructors

    #region Properties

    /// <summary>How long the link of a request works (ADR-0052 decision 2).</summary>
    public static TimeSpan LinkLifetime { get; } = TimeSpan.FromMinutes(30);

    /// <summary>The SHA-256 of the token of the link, as 64 lower-case hex digits; unique.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// The key of the mailbox of the email: the SHA-256 of <see cref="Mailbox.NormalFormOf"/>, as 64 lower-case hex digits;
    /// unique, so a mailbox has one open request.
    /// </summary>
    public string EmailKey { get; private set; } = string.Empty;

    /// <summary>The arrival date.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>The departure date, exclusive.</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>The room size that the guest chose.</summary>
    public RoomSize RoomSize { get; private set; }

    /// <summary>The number of guests of the party.</summary>
    public int Guests { get; private set; }

    /// <summary>The first name of the guest, in its normal form.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>The last name of the guest, in its normal form.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>The email of the guest, in its normal form; the mail of the request went to it.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>The phone number of the guest, in its normal form.</summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>The country of the guest, a name of <see cref="Countries"/>.</summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>The passport number of the guest, if given; encrypted at rest as a guest's is (decision 7).</summary>
    public string? PassportNumber { get; private set; }

    /// <summary>When the guest asked.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the link lapses: <see cref="LinkLifetime"/> after <see cref="CreatedAt"/>.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>The asked stay as a value object.</summary>
    public DateRange Period => new(StartDate, EndDate);

    #endregion Properties

    #region Public methods

    /// <summary>Tells whether the link still works at a moment.</summary>
    /// <param name="now">The moment.</param>
    /// <returns><see langword="true"/> before <see cref="ExpiresAt"/>.</returns>
    public bool IsOpenAt(DateTimeOffset now) => now < ExpiresAt;

    /// <summary>Creates a request from a guest whose fields the rules of <see cref="Guest.Create"/> checked.</summary>
    /// <param name="guest">The guest of the request, not stored.</param>
    /// <param name="stay">The stay.</param>
    /// <param name="roomSize">The room size.</param>
    /// <param name="guests">The number of guests of the party.</param>
    /// <param name="tokenHash">The SHA-256 of the token of the link.</param>
    /// <param name="emailKey">The key of the mailbox of the guest.</param>
    /// <param name="now">The moment of the request.</param>
    /// <returns>The request, not yet stored.</returns>
    /// <exception cref="ArgumentNullException">The guest is missing.</exception>
    /// <exception cref="ArgumentException">A hash is not 64 lower-case hex digits.</exception>
    public static BookingRequest Create(
        Guest guest,
        DateRange stay,
        RoomSize roomSize,
        int guests,
        string tokenHash,
        string emailKey,
        DateTimeOffset now)
    {
        GuardHash(emailKey, nameof(emailKey));

        BookingRequest request = new() { EmailKey = emailKey };
        request.Renew(guest, stay, roomSize, guests, tokenHash, now);
        return request;
    }

    /// <summary>
    /// Replaces the request of the mailbox with a new one (ADR-0052 decision 6): new answers, a new token and a new time,
    /// so the old link stops working.
    /// </summary>
    /// <param name="guest">The guest of the new request, not stored.</param>
    /// <param name="stay">The stay.</param>
    /// <param name="roomSize">The room size.</param>
    /// <param name="guests">The number of guests of the party.</param>
    /// <param name="tokenHash">The SHA-256 of the token of the new link.</param>
    /// <param name="now">The moment of the new request.</param>
    /// <exception cref="ArgumentNullException">The guest is missing.</exception>
    /// <exception cref="ArgumentException">The hash is not 64 lower-case hex digits.</exception>
    public void Renew(Guest guest, DateRange stay, RoomSize roomSize, int guests, string tokenHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(guest);
        GuardHash(tokenHash, nameof(tokenHash));

        TokenHash = tokenHash;
        StartDate = stay.Start;
        EndDate = stay.End;
        RoomSize = roomSize;
        Guests = guests;
        FirstName = guest.FirstName;
        LastName = guest.LastName;
        Email = guest.Email;
        PhoneNumber = guest.PhoneNumber;
        Country = guest.Country;
        PassportNumber = guest.PassportNumber;
        CreatedAt = now;
        ExpiresAt = now + LinkLifetime;
    }

    #endregion Public methods

    #region Internal methods

    /// <summary>Refuses a value that is not a SHA-256 hash written as 64 lower-case hex digits.</summary>
    /// <param name="hash">The value.</param>
    /// <param name="name">The name of the parameter, for the message.</param>
    /// <exception cref="ArgumentException">The value is not 64 lower-case hex digits.</exception>
    internal static void GuardHash(string hash, string name)
    {
        if (hash is not { Length: HashLength } || hash.AsSpan().ContainsAnyExcept(LowerHexDigits))
        {
            throw new ArgumentException("The value must be a SHA-256 hash written as 64 lower-case hex digits.", name);
        }
    }

    #endregion Internal methods
}
