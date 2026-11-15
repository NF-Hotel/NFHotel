using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Implementation of the guest sign-in and the public booking that signs its guest in (MIL-018, ADR-0047, UC-010).
/// </summary>
/// <remarks>
/// The sign-in is the lookup of MIL-079 with a token at the end: <see cref="IBookingService.LookUpReceiptAsync"/>
/// compares the email in a time that does not depend on the text, reads the booking also when the number is unknown,
/// and counts every attempt against the number in the throttle of the staff sign-in before it reads, clearing the
/// count only when the email is right and the booking is not cancelled. This service only turns the lookup's "not
/// found" into the one refusal of the sign-in, refuses a cancelled booking, and issues the token.
/// </remarks>
public sealed class GuestAccessService : IGuestAccessService
{
    #region Fields

    private readonly IBookingService _bookings;
    private readonly IGuestTokenIssuer _tokens;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="bookings">The booking use cases: the lookup by number and email, and the public booking.</param>
    /// <param name="tokens">Issues the guest token.</param>
    public GuestAccessService(IBookingService bookings, IGuestTokenIssuer tokens)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(tokens);

        _bookings = bookings;
        _tokens = tokens;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    /// <remarks>
    /// A cancelled booking gets the same refusal as a wrong email (ADR-0047 decision 7). The decision refuses a booking
    /// that was cancelled more than 30 days ago; the booking stores no time of its cancellation, and this milestone leaves
    /// the database as it is, so every cancelled booking is refused. That holds the decision and is stricter in the first
    /// 30 days: the guest who cancels keeps the session that cancelled, and nobody signs in to a cancelled booking later.
    /// </remarks>
    public async Task<Result<GuestSessionDto>> SignInAsync(
        BookingReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<BookingReceiptDto> lookup = await _bookings
            .LookUpReceiptAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (lookup.IsFailure)
        {
            return lookup.Errors.Any(static error => error.Code == ErrorCodes.Booking.NotFound)
                ? Result<GuestSessionDto>.Failure(ErrorCodes.Api.GuestUnauthorized)
                : Result<GuestSessionDto>.Failure(lookup.Errors);
        }

        return lookup.Value.Status == BookingStatus.Cancelled
            ? Result<GuestSessionDto>.Failure(ErrorCodes.Api.GuestUnauthorized)
            : Result<GuestSessionDto>.Success(_tokens.Issue(request.BookingId));
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
            ? Result<PublicBookingDto>.Success(new PublicBookingDto(created.Value, _tokens.Issue(created.Value.BookingId)))
            : Result<PublicBookingDto>.Failure(created.Errors);
    }

    #endregion Public methods
}
