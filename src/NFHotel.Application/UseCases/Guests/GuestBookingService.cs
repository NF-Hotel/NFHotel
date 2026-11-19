using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Implementation of the signed-in guest's own booking (MIL-018, ADR-0049 decisions 3 and 7).
/// </summary>
/// <remarks>
/// The booking is the one in the guest token, so the guest cannot even name another booking. The rules stay in the
/// booking use cases, which let a guest read, cancel and move only the booking in the token (<see cref="GuestOwnership"/>);
/// this service names the booking for them and answers with the receipt alone.
/// </remarks>
public sealed class GuestBookingService : IGuestBookingService
{
    #region Fields

    private readonly IBookingService _bookings;
    private readonly IGuestCallContext _guestCall;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="bookings">The booking use cases.</param>
    /// <param name="guestCall">The signed-in guest of the call, if any.</param>
    public GuestBookingService(IBookingService bookings, IGuestCallContext guestCall)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(guestCall);

        _bookings = bookings;
        _guestCall = guestCall;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_guestCall.BookingId is not int bookingId)
        {
            return Refused();
        }

        return ToReceipt(await _bookings.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> CancelAsync(CancellationToken cancellationToken = default)
    {
        if (_guestCall.BookingId is not int bookingId)
        {
            return Refused();
        }

        return ToReceipt(await _bookings.CancelAsync(bookingId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> RescheduleAsync(
        GuestRescheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_guestCall.BookingId is not int bookingId)
        {
            return Refused();
        }

        // BR-C-07 (MIL-086): a guest moves a stay within the limit of nights of the public site; the dates against
        // today are checked by the move itself.
        if (PublicNightsError(request) is { } nightsError)
        {
            return Result<BookingReceiptDto>.Failure(nightsError);
        }

        // The guest keeps the room (MIL-018), so the move names the room that the booking has.
        Result<BookingDetailsDto> current = await _bookings.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure)
        {
            return ToReceipt(current);
        }

        RescheduleBookingRequest move = new(bookingId, request.CheckInDate, request.CheckOutDate, current.Value.Room.RoomId);

        return ToReceipt(await _bookings.RescheduleAsync(move, cancellationToken).ConfigureAwait(false));
    }

    #endregion Public methods

    #region Private methods

    private static Result<BookingReceiptDto> Refused() => Result<BookingReceiptDto>.Failure(ErrorCodes.Api.GuestUnauthorized);

    // The nights of the public site; a period that cannot be built is left to the move, which answers its own code.
    private static string? PublicNightsError(GuestRescheduleRequest request)
    {
        try
        {
            return Booking.CheckPublicNights(new DateRange(request.CheckInDate, request.CheckOutDate));
        }
        catch (DomainException)
        {
            return null;
        }
    }

    // A token whose booking is gone gets the one refusal of a guest, as a token that is no longer valid does; any other
    // failure keeps the code of the booking.
    private static Result<BookingReceiptDto> ToReceipt(Result<BookingDetailsDto> result)
    {
        if (result.IsSuccess)
        {
            return Result<BookingReceiptDto>.Success(BookingReceiptDto.From(result.Value));
        }

        return result.Errors.Any(static error => error.Code == ErrorCodes.Booking.NotFound)
            ? Refused()
            : Result<BookingReceiptDto>.Failure(result.Errors);
    }

    #endregion Private methods
}
