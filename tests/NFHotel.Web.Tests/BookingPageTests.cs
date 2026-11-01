using Bunit;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Booking;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The public booking page and the receipt page, rendered and clicked (MIL-054 task 3).</summary>
public sealed class BookingPageTests : IAsyncDisposable
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly PageHost _host = new();

    public BookingPageTests()
    {
        var bookings = new PassThroughRunner<IBookingService>(_bookings);
        _host.With(new BookingFlowViewModel(bookings, new PassThroughRunner<IGuestService>(_guests), new FakeClock { Today = Today }))
            .With(new BookingReceiptViewModel(bookings));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void RoomsAre(params AvailableRoomDto[] rooms) =>
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(rooms));

    #endregion

    #region Book

    /// <summary>Positive: a guest walks through dates, room and details, is sent to the receipt, and can book again.</summary>
    [Fact]
    public void Book_WholeFlow_EndsOnTheReceipt()
    {
        RoomsAre(FreeRoom(1), FreeRoom(2));
        _bookings.CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var page = _host.Render<Book>();

        page.Find("#check-in").Change("2026-03-12");
        page.Find("#check-out").Change("2026-03-15");
        Assert.Contains("3 nights", page.Markup, StringComparison.Ordinal);
        page.Button("Find available rooms").Click();
        Assert.Contains("Choose a room", page.Markup, StringComparison.Ordinal);
        page.Button("Choose room 102").Click();
        page.Find("#first-name").Change("Rita");
        page.Find("#last-name").Change("Hansen");
        page.Find("#email").Change("rita@example.com");
        page.Find("#phone").Change("+4512345678");
        page.Find("#country").Change("Denmark");
        page.Find("#passport").Change("P1");
        page.Button("Send booking request").Click();

        Assert.Contains("Your confirmation", page.Markup, StringComparison.Ordinal);
        Assert.Contains("B-0009", page.Markup, StringComparison.Ordinal);
        Assert.EndsWith("/booking/B-0009", _host.Navigation.Uri, StringComparison.Ordinal);
        _bookings.Received(1).CreatePublicAsync(
            Arg.Is<CreateBookingRequest>(request => request.RoomId == 2 && request.CheckInDate == new DateOnly(2026, 3, 12) && request.NewGuest!.PassportNumber == "P1"),
            Arg.Any<CancellationToken>());

        page.Button("Book another stay").Click();
        Assert.Contains("When would you like to stay?", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Dates

    /// <summary>Negative: invalid dates mark the fields invalid and keep the button off.</summary>
    [Fact]
    public void Dates_InThePast_AreMarkedInvalid()
    {
        var page = _host.Render<Book>();

        page.Find("#check-in").Change("2026-03-01");

        Assert.Equal("true", page.Find("#check-in").GetAttribute("aria-invalid"));
        Assert.True(page.Button("Find available rooms").HasAttribute("disabled"));
        Assert.Contains(Text(ErrorCodes.Booking.StartDateInPast), page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Rooms

    /// <summary>Edge: no free rooms offers other dates; the back buttons step back.</summary>
    [Fact]
    public void Rooms_NoneFree_OffersOtherDates()
    {
        RoomsAre();
        var page = _host.Render<Book>();

        page.Button("Find available rooms").Click();
        Assert.Equal(2, page.FindAll("button").Count(button => button.TextContent.Contains("dates", StringComparison.OrdinalIgnoreCase)));
        page.FindAll("button").First(button => button.TextContent.Contains("dates", StringComparison.OrdinalIgnoreCase)).Click();
        Assert.Contains("When would you like to stay?", page.Markup, StringComparison.Ordinal);

        RoomsAre(FreeRoom(1));
        page.Button("Find available rooms").Click();
        page.Button("Choose room 101").Click();
        page.Button("Back to rooms").Click();
        Assert.Contains("Choose a room", page.Markup, StringComparison.Ordinal);
        page.FindAll("button").Last(button => button.TextContent.Contains("dates", StringComparison.OrdinalIgnoreCase)).Click();
        Assert.Contains("When would you like to stay?", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Details

    /// <summary>Error handling: invalid details are marked on their fields and nothing is booked.</summary>
    [Fact]
    public void Details_Invalid_AreMarkedOnTheFields()
    {
        RoomsAre(FreeRoom(1));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Failure(ErrorCodes.Guest.EmailInvalid));
        var page = _host.Render<Book>();
        page.Button("Find available rooms").Click();
        page.Button("Choose room 101").Click();

        page.Button("Send booking request").Click();

        Assert.Equal("true", page.Find("#email").GetAttribute("aria-invalid"));
        Assert.Null(page.Find("#first-name").GetAttribute("aria-invalid"));
        Assert.Contains(Text(ErrorCodes.Guest.EmailInvalid), page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Receipt

    /// <summary>Positive: the receipt page shows the booking of a readable number.</summary>
    [Fact]
    public void Receipt_KnownNumber_ShowsTheBooking()
    {
        _bookings.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(42)));

        var page = _host.Render<BookingReceipt>(parameters => parameters.Add(receipt => receipt.BookingNumber, "FLZ-42"));

        Assert.Contains("B-0042", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Room 101", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Negative: the receipt page of an unknown number says so and links back to booking.</summary>
    [Fact]
    public void Receipt_UnknownNumber_SaysNotFound()
    {
        var page = _host.Render<BookingReceipt>(parameters => parameters.Add(receipt => receipt.BookingNumber, "nonsense"));

        Assert.Contains(Text(ErrorCodes.Booking.NotFound), page.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("a[href='/book']"));
    }

    #endregion

    #region ReceiptCard

    /// <summary>Edge: the receipt card renders nothing without a booking.</summary>
    [Fact]
    public void ReceiptCard_NoBooking_RendersNothing()
    {
        var card = _host.Render<BookingReceiptCard>(parameters => parameters.Add(receipt => receipt.Details, null));

        Assert.Equal(string.Empty, card.Markup.Trim());
    }

    #endregion
}
