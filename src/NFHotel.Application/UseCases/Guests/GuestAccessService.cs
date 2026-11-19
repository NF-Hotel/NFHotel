using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Implementation of the guest sign-in and the public booking that signs its guest in (MIL-018, ADR-0047, ADR-0049,
/// UC-010).
/// </summary>
/// <remarks>
/// The sign-in is the one lookup of a booking by a guest (ADR-0049 decision 8). Every try is counted against the
/// reference and the address of the caller before the booking is read, in one step with the check, and a try that
/// succeeds is taken back (decision 6). An unknown reference, an unreadable one, a wrong email and a cancelled booking
/// get the same refusal (decision 2); a reference that no booking has is read and its email compared like one that a
/// booking has, so the cost of the answer does not tell either.
/// </remarks>
public sealed class GuestAccessService : IGuestAccessService
{
    #region Fields

    /// <summary>The name under which the tries of the callers whose address the API does not know are counted together.</summary>
    public const string UnknownAddress = "unknown";

    // A reference that no booking has is compared with this text, so that its answer costs what a wrong email costs. The
    // comparison gives the time only: without a booking there is no session, whatever the email.
    private const string StandInEmail = "no-booking@example.invalid";

    private readonly IPublicBookingService _bookings;
    private readonly IBookingQueries _bookingQueries;
    private readonly IGuestTokenIssuer _tokens;
    private readonly IGuestSignInLimits _limits;
    private readonly ICallerAddress _caller;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="bookings">The public booking.</param>
    /// <param name="bookingQueries">Reads the booking of a reference.</param>
    /// <param name="tokens">Issues the guest token.</param>
    /// <param name="limits">Counts the tries per reference and per address (ADR-0049 decision 6).</param>
    /// <param name="caller">The address of the caller.</param>
    public GuestAccessService(
        IPublicBookingService bookings,
        IBookingQueries bookingQueries,
        IGuestTokenIssuer tokens,
        IGuestSignInLimits limits,
        ICallerAddress caller)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(bookingQueries);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(caller);

        _bookings = bookings;
        _bookingQueries = bookingQueries;
        _tokens = tokens;
        _limits = limits;
        _caller = caller;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    /// <remarks>
    /// A cancelled booking gets the same refusal as a wrong email and keeps its count (ADR-0047 decision 7, ADR-0049
    /// decision 6): the booking stores no time of its cancellation, so every cancelled booking is refused, and the count
    /// does not tell a guesser that the email was right.
    /// </remarks>
    public async Task<Result<GuestSessionDto>> SignInAsync(
        GuestSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<GuestSessionDto>.Failure(ErrorCodes.Booking.ReceiptEmailRequired);
        }

        Guid? reference = BookingReference.TryParse(request.Reference, out Guid read) ? read : null;
        string address = AddressKey(_caller.Address);

        // The try counts as a failure from here until it succeeds.
        if (!_limits.TryCount(reference, address))
        {
            return Result<GuestSessionDto>.Failure(ErrorCodes.Booking.TooManyLookups);
        }

        if (reference is not Guid known)
        {
            return Refused();
        }

        BookingReadModel? booking = await _bookingQueries
            .GetReadModelByReferenceAsync(known, cancellationToken)
            .ConfigureAwait(false);
        bool isRightEmail = IsSameEmail(booking?.Guest.Email ?? StandInEmail, request.Email);
        if (booking is null || !isRightEmail || booking.Status == BookingStatus.Cancelled)
        {
            return Refused();
        }

        _limits.Succeeded(known, address);

        return Result<GuestSessionDto>.Success(_tokens.Issue(booking.BookingId));
    }

    /// <inheritdoc />
    public async Task<Result<PublicBookingDto>> BookAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<BookingDetailsDto> created = await _bookings
            .CreatePublicAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return created.IsSuccess
            ? Result<PublicBookingDto>.Success(new PublicBookingDto(BookingReceiptDto.From(created.Value), _tokens.Issue(created.Value.BookingId)))
            : Result<PublicBookingDto>.Failure(created.Errors);
    }

    #endregion Public methods

    #region Private methods

    private static Result<GuestSessionDto> Refused() => Result<GuestSessionDto>.Failure(ErrorCodes.Api.GuestUnauthorized);

    /// <summary>
    /// The key of the caller, so that one caller is one key (ADR-0049 decision 6): an IPv4 address, also in IPv6 form,
    /// counts as itself; an IPv6 address counts by its first 64 bits, the prefix of its link, because the last 64 bits
    /// are its interface identifier (RFC 4291 section 2.5.1), which a host forms itself and may change for each try
    /// (RFC 8981). An address that is missing or that is no address counts under <see cref="UnknownAddress"/>.
    /// </summary>
    private static string AddressKey(string? address)
    {
        if (!IPAddress.TryParse(address, out IPAddress? parsed))
        {
            return UnknownAddress;
        }

        if (parsed.IsIPv4MappedToIPv6)
        {
            return parsed.MapToIPv4().ToString();
        }

        if (parsed.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return parsed.ToString();
        }

        byte[] network = parsed.GetAddressBytes();
        Array.Clear(network, 8, 8);
        return string.Create(CultureInfo.InvariantCulture, $"{new IPAddress(network)}/64");
    }

    /// <summary>
    /// Compares two emails in their normal form (trimmed, Unicode normal form C; MIL-086 decision 4) and without letter
    /// case, in a time that does not depend on where they first differ.
    /// </summary>
    private static bool IsSameEmail(string stored, string given) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(TextNormalForm.Normalize(stored).ToUpperInvariant()),
            Encoding.UTF8.GetBytes(TextNormalForm.Normalize(given).ToUpperInvariant()));

    #endregion Private methods
}
