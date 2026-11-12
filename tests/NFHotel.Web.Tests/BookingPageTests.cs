using Bunit;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
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

    /// <summary>
    /// Positive: a guest walks through dates, room type and details, sees the receipt on the same page with a link to
    /// find it again, and can book again. The page does not move on to the lookup page, which asks for the email (MIL-079).
    /// </summary>
    [Fact]
    public void Book_WholeFlow_EndsOnTheReceipt()
    {
        RoomsAre(FreeRoom(1), FreeRoom(2));
        _bookings.CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var page = _host.Render<Book>();

        page.Find("#check-in").Change("2026-03-12");
        page.Find("#check-out").Change("2026-03-15");
        Assert.Contains("3 nights", page.Markup, StringComparison.Ordinal);
        page.Button("Find available rooms").Click();
        Assert.Contains("Choose a room", page.Markup, StringComparison.Ordinal);
        page.Button("Choose Double room").Click();
        page.Find("#first-name").Change("Rita");
        page.Find("#last-name").Change("Hansen");
        page.Find("#email").Change("rita@example.com");
        page.Find("#phone").Change("+4512345678");
        page.Find("#country").Change("Denmark");
        page.Find("#passport").Change("P1");
        page.Button("Send booking request").Click();

        Assert.Contains("Your confirmation", page.Markup, StringComparison.Ordinal);
        Assert.Contains("B-0009", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("rita@example.com", page.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("a[href='/booking/B-0009']"));
        Assert.DoesNotContain("/booking/", _host.Navigation.Uri, StringComparison.Ordinal);
        _bookings.Received(1).CreatePublicAsync(
            Arg.Is<CreatePublicBookingRequest>(request => request.RoomSize == RoomSize.Double && request.CheckInDate == new DateOnly(2026, 3, 12) && request.NewGuest!.PassportNumber == "P1"),
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
        page.Button("Choose Double room").Click();
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
        page.Button("Choose Double room").Click();

        page.Button("Send booking request").Click();

        Assert.Equal("true", page.Find("#email").GetAttribute("aria-invalid"));
        Assert.Null(page.Find("#first-name").GetAttribute("aria-invalid"));
        Assert.Contains(Text(ErrorCodes.Guest.EmailInvalid), page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Receipt

    /// <summary>
    /// Security: a number in the address without the email shows no booking and looks nothing up, in the first render
    /// as well, which is what the server sends as the first HTML (MIL-079 criterion 3).
    /// </summary>
    [Fact]
    public void Receipt_NumberInTheAddressWithoutTheEmail_ShowsNoBooking()
    {
        var page = _host.Render<BookingReceipt>(parameters => parameters.Add(receipt => receipt.BookingNumber, "FLZ-000001"));

        Assert.Empty(page.FindAll(".pub-receipt"));
        Assert.Equal("FLZ-000001", page.Find("#booking-number").GetAttribute("value"));
        Assert.Empty(_bookings.ReceivedCalls());
    }

    /// <summary>
    /// Positive: the number from the address and the email that the guest types show the receipt, and the lookup sends
    /// the number as the key of the booking and the email as typed (MIL-079).
    /// </summary>
    [Fact]
    public void Receipt_NumberFromTheAddressAndEmail_ShowsTheReceipt()
    {
        _bookings.LookUpReceiptAsync(new BookingReceiptRequest(42, "rita@example.com"), Arg.Any<CancellationToken>())
            .Returns(Ok(BookingReceiptDto.From(BookingDetails(42))));
        var page = _host.Render<BookingReceipt>(parameters => parameters.Add(receipt => receipt.BookingNumber, "FLZ-42"));

        Assert.Equal("FLZ-42", page.Find("#booking-number").GetAttribute("value"));
        page.Find("#booking-email").Change("rita@example.com");
        page.Find("form").Submit();

        Assert.Contains("B-0042", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Double room · the room number is given at check-in", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Room 101", page.Markup, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#booking-email"));
    }

    /// <summary>Security: a wrong email says not found, keeps the form and shows no receipt.</summary>
    [Fact]
    public void Receipt_WrongEmail_SaysNotFoundAndKeepsTheForm()
    {
        _bookings.LookUpReceiptAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingReceiptDto>(ErrorCodes.Booking.NotFound));
        var page = _host.Render<BookingReceipt>(parameters => parameters.Add(receipt => receipt.BookingNumber, "FLZ-42"));

        page.Find("#booking-email").Change("someone@example.com");
        page.Find("form").Submit();

        Assert.Contains(Text(ErrorCodes.Booking.NotFound), page.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#booking-email"));
        Assert.Empty(page.FindAll(".pub-receipt"));
    }

    /// <summary>Edge: the page without a number in the address shows an empty form and looks nothing up.</summary>
    [Fact]
    public void Receipt_NoNumberInTheAddress_ShowsAnEmptyFormWithoutALookup()
    {
        var page = _host.Render<BookingReceipt>();

        Assert.Equal(string.Empty, page.Find("#booking-number").GetAttribute("value") ?? string.Empty);
        Assert.Empty(page.FindAll(".pub-receipt"));
        Assert.Empty(_bookings.ReceivedCalls());
    }

    #endregion

    #region ReceiptCard

    /// <summary>Edge: the receipt card renders nothing without a booking.</summary>
    [Fact]
    public void ReceiptCard_NoBooking_RendersNothing()
    {
        var card = _host.Render<BookingReceiptCard>(parameters => parameters.Add(component => component.Receipt, null));

        Assert.Equal(string.Empty, card.Markup.Trim());
    }

    #endregion
}
