using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// Hosts the real web application with the guest substitutes of <see cref="GuestWebFactory"/> and a substitute for the
/// booking verified by email (MIL-088, ADR-0052): the verification is on, and the token of the link decides the answer of
/// the button, so the tests of one class can share the host.
/// </summary>
public sealed class BookingRequestWebFactory : GuestWebFactory
{
    #region Fields

    /// <summary>The token of a link that books the room: 43 characters of base64url, as the API makes them.</summary>
    public const string BookingToken = "q8Zr3m0X7c1V2b4N6m8Q0w2E4r6T8y0U2i4O6p8A0s2";

    /// <summary>The token of a link whose room went between the mail and the button.</summary>
    public const string RoomGoneToken = "R7oOmGoNe0b4N6m8Q0w2E4r6T8y0U2i4O6p8A0s2xYz";

    /// <summary>Any other token is unknown, used or lapsed: the API gives the three one answer.</summary>
    public const string UsedToken = "U5eDl1nK0b4N6m8Q0w2E4r6T8y0U2i4O6p8A0s2xYzQ";

    #endregion Fields

    #region Constructors

    /// <summary>Creates the factory and gives the substitute its answers.</summary>
    public BookingRequestWebFactory()
    {
        GuestSessionDto session = new(GuestToken, ExpiresAt);
        BookingReceiptDto receipt = new(
            Guid.Parse(Reference), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 12), 2, RoomSize.Double, BookingStatus.Pending);

        Requests.GetModeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<PublicBookingModeDto>.Success(new PublicBookingModeDto(true))));
        Requests.VerifyAsync(Arg.Any<VerifyBookingRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(call.Arg<VerifyBookingRequest>().Token switch
            {
                BookingToken => Result<PublicBookingDto>.Success(new PublicBookingDto(receipt, session)),
                RoomGoneToken => Result<PublicBookingDto>.Failure(ErrorCodes.Booking.RoomTypeNotAvailable),
                _ => Result<PublicBookingDto>.Failure(ErrorCodes.Booking.VerificationLinkInvalid),
            }));
    }

    #endregion Constructors

    #region Properties

    /// <summary>The booking verified by email that the pages call instead of the API.</summary>
    public IBookingRequestService Requests { get; } = Substitute.For<IBookingRequestService>();

    #endregion Properties

    #region Protected methods

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBookingRequestService>();
            services.AddScoped(_ => Requests);
        });
    }

    #endregion Protected methods
}
