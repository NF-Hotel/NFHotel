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
using NFHotel.Web.Security;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

using GuestEntity = NFHotel.Domain.Entities.Guest;

namespace NFHotel.Web.Tests;

/// <summary>
/// The public booking page, rendered and clicked (MIL-054 task 3). The guest sign-in and the guest's own booking are
/// static pages with a cookie; <see cref="GuestPagesTests"/> drives them through the real pipeline (MIL-018).
/// </summary>
public sealed class BookingPageTests : IAsyncDisposable
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly IGuestAccessService _access = Substitute.For<IGuestAccessService>();
    private readonly GuestSessionTickets _tickets = new(new FakeClock { Today = Today });
    private readonly PageHost _host = new();

    public BookingPageTests()
    {
        var bookings = new PassThroughRunner<IBookingService>(_bookings);
        _host.With(new BookingFlowViewModel(
            bookings,
            new PassThroughRunner<IGuestService>(_guests),
            new PassThroughRunner<IGuestAccessService>(_access),
            _tickets,
            new FakeClock { Today = Today }));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private IRenderedComponent<Book> AtTheDetails()
    {
        RoomsAre(FreeRoom(1));
        var page = _host.Render<Book>();
        page.Button("Find available rooms").Click();
        page.Button("Choose Double room").Click();
        return page;
    }

    private void RoomsAre(params AvailableRoomDto[] rooms) =>
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(rooms));

    #endregion

    #region Book

    /// <summary>
    /// Positive: a guest walks through dates, room type and details, sees the receipt on the same page with a link to
    /// find it again, and can book again. The page does not move on to the sign-in, which asks for the email (MIL-079);
    /// instead "Manage your booking" posts the one-time ticket of the guest's session, which opens it once (MIL-018).
    /// </summary>
    [Fact]
    public void Book_WholeFlow_EndsOnTheReceiptWithTheSessionTicket()
    {
        RoomsAre(FreeRoom(1), FreeRoom(2));
        GuestSessionDto session = new("guest-token-9", new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero));
        _access.BookAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new PublicBookingDto(Receipt(9), session)));
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
        Assert.Contains(ReferenceOf(9).ToString(), page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("rita@example.com", page.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("a[href='/booking']"));
        Assert.Empty(page.FindAll($"a[href*='{ReferenceOf(9)}']"));
        Assert.DoesNotContain("/booking/", _host.Navigation.Uri, StringComparison.Ordinal);
        Assert.DoesNotContain(session.Token, page.Markup, StringComparison.Ordinal);
        Assert.Equal(GuestSignInRegistration.SessionPath, page.Find("form[method='post']").GetAttribute("action"));
        Assert.True(page.HasButton("Manage your booking"));
        string? ticket = page.Find($"input[name='{GuestSignInRegistration.TicketField}']").GetAttribute("value");
        Assert.Equal(session, _tickets.Redeem(ticket));
        _access.Received(1).BookAsync(
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

    /// <summary>
    /// Boundary (MIL-086 decision 3): the date fields offer the days that the rules allow: the arrival from today to the
    /// day before the last departure, the departure up to five years ahead, and up to 30 nights after an arrival.
    /// </summary>
    [Fact]
    public void Dates_OnThePage_OfferTheDaysThatTheRulesAllow()
    {
        var page = _host.Render<Book>();

        page.Find("#check-in").Change("2026-03-12");

        Assert.Equal("2026-03-10", page.Find("#check-in").GetAttribute("min"));
        Assert.Equal("2031-03-09", page.Find("#check-in").GetAttribute("max"));
        Assert.Equal("2026-03-13", page.Find("#check-out").GetAttribute("min"));
        Assert.Equal("2026-04-11", page.Find("#check-out").GetAttribute("max"));
    }

    /// <summary>Negative: 31 nights are marked at the departure with the text of the rule, and the button stays off.</summary>
    [Fact]
    public void Dates_ThirtyOneNights_AreMarkedInvalid()
    {
        var page = _host.Render<Book>();

        page.Find("#check-in").Change("2026-03-12");
        page.Find("#check-out").Change("2026-04-12");

        Assert.Equal("true", page.Find("#check-out").GetAttribute("aria-invalid"));
        Assert.True(page.Button("Find available rooms").HasAttribute("disabled"));
        Assert.Contains(Text(ErrorCodes.Booking.StayTooLong), page.Markup, StringComparison.Ordinal);
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

    /// <summary>
    /// Boundary (MIL-086 decision 2): each text field takes no more characters than its rule, and the country is chosen
    /// from the list of the rules, in the order of the list, after an empty choice that asks for one.
    /// </summary>
    [Fact]
    public void Details_OnThePage_FieldsHaveTheLengthsAndTheListOfTheRules()
    {
        var page = AtTheDetails();

        var options = page.FindAll("#country option").Select(option => option.GetAttribute("value")).ToList();

        Assert.Equal($"{GuestEntity.MaxNameLength}", page.Find("#first-name").GetAttribute("maxlength"));
        Assert.Equal($"{GuestEntity.MaxNameLength}", page.Find("#last-name").GetAttribute("maxlength"));
        Assert.Equal($"{GuestEntity.MaxEmailLength}", page.Find("#email").GetAttribute("maxlength"));
        Assert.Equal($"{GuestEntity.MaxPhoneNumberLength}", page.Find("#phone").GetAttribute("maxlength"));
        Assert.Equal($"{GuestEntity.MaxPassportNumberLength}", page.Find("#passport").GetAttribute("maxlength"));
        Assert.Equal("SELECT", page.Find("#country").TagName);
        Assert.Equal(string.Empty, options[0]);
        Assert.Equal(Countries.All.Select(country => country.Name), options.Skip(1));
    }

    /// <summary>
    /// Negative (MIL-086 decision 5): each hostile string typed into a text field stays the value of that field, written
    /// as text: the page holds no element that the string would make. The parser of the test reads a line break of the
    /// markup as a line feed, as every HTML parser does, so the value is compared with its line breaks as line feeds.
    /// </summary>
    [Fact]
    public void Details_HostileStrings_StayTextInTheFields()
    {
        var page = AtTheDetails();

        foreach (var (kind, value) in HostileStrings.All)
        {
            foreach (var id in new[] { "#first-name", "#last-name", "#email", "#phone", "#passport" })
            {
                page.Find(id).Change(value);

                Assert.True(value.ReplaceLineEndings("\n") == page.Find(id).GetAttribute("value"), $"{kind} in {id}");
            }

            Assert.Empty(page.FindAll("script"));
            Assert.Empty(page.FindAll("img"));
            Assert.Empty(page.FindAll("[onmouseover], [onerror]"));
        }
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
