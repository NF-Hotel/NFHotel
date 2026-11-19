using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Booking;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The presentation logic of the guest's own booking on <c>/my-booking</c> (MIL-018, UC-010, and since MIL-085 ADR-0049):
/// the receipt, the change of dates in the same room, the cancellation, and the result after a change. No call names a
/// booking: the API answers for the booking in the guest token.
/// </summary>
public sealed class MyBookingViewModelTests
{
    private readonly IGuestBookingService _bookings = Substitute.For<IGuestBookingService>();

    #region Load

    /// <summary>Positive: the booking of the guest is read, with its receipt and the actions its status allows.</summary>
    [Fact]
    public async Task LoadAsync_OwnBooking_ShowsTheReceiptAndItsActions()
    {
        // Arrange
        _bookings.GetAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.Receipt(7)));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(AdminSamples.Receipt(7), viewModel.Receipt);
        Assert.Equal(AdminSamples.ReferenceOf(7), viewModel.Receipt?.Reference);
        Assert.True(viewModel.CanCancel);
        Assert.True(viewModel.CanReschedule);
        Assert.Empty(viewModel.Errors);
        Assert.Null(viewModel.Notice);
    }

    /// <summary>
    /// Equivalence (ADR-0049 decision 7): the page offers the move and the cancellation for an open booking only, by the
    /// rule of the domain, so a guest is not shown a button the API would refuse.
    /// </summary>
    /// <param name="status">The status of the booking.</param>
    /// <param name="open">Whether the booking may still be moved and cancelled.</param>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, false)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public async Task LoadAsync_AnyStatus_OffersTheChangesOfAnOpenBookingOnly(BookingStatus status, bool open)
    {
        // Arrange
        _bookings.GetAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.Receipt(7, status)));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(open, viewModel.CanCancel);
        Assert.Equal(open, viewModel.CanReschedule);
    }

    /// <summary>Equivalence: the word in the address after a change gives its notice; any other word gives none.</summary>
    /// <param name="result">The word in the address.</param>
    /// <param name="notice">The notice shown.</param>
    [Theory]
    [InlineData(MyBookingViewModel.CancelledResult, "Your booking is cancelled.")]
    [InlineData(MyBookingViewModel.RescheduledResult, "Your new dates are saved.")]
    [InlineData("<script>", null)]
    [InlineData(null, null)]
    public async Task LoadAsync_ResultInTheAddress_ShowsItsNoticeOnly(string? result, string? notice)
    {
        // Arrange
        _bookings.GetAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.Receipt(7)));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(result, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(notice, viewModel.Notice);
    }

    /// <summary>Negative: a refused read shows the text of the refusal, no receipt, no actions and no notice.</summary>
    [Fact]
    public async Task LoadAsync_Refused_ShowsTheErrorAndNoReceipt()
    {
        // Arrange
        _bookings.GetAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<BookingReceiptDto>(ErrorCodes.Api.GuestUnauthorized));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(MyBookingViewModel.CancelledResult, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.Receipt);
        Assert.False(viewModel.CanCancel);
        Assert.False(viewModel.CanReschedule);
        Assert.Null(viewModel.Notice);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Api.GuestUnauthorized)], viewModel.Errors);
    }

    /// <summary>Negative: the runner is required.</summary>
    [Fact]
    public void Constructor_MissingRunner_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new MyBookingViewModel(null!));
    }

    #endregion

    #region Cancel

    /// <summary>Positive: the cancellation is of the booking of the guest, and the page keeps the cancelled receipt.</summary>
    [Fact]
    public async Task CancelAsync_LoadedBooking_CancelsItAndKeepsTheAnswer()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.CancelAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.Receipt(7, BookingStatus.Cancelled)));

        // Act
        bool cancelled = await viewModel.CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(cancelled);
        Assert.Equal(BookingStatus.Cancelled, viewModel.Receipt?.Status);
        Assert.False(viewModel.CanCancel);
        await _bookings.Received(1).CancelAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a refused cancellation keeps the receipt as it was and shows the text of the refusal.</summary>
    [Fact]
    public async Task CancelAsync_Refused_KeepsTheReceiptAndShowsTheError()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.CancelAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<BookingReceiptDto>(ErrorCodes.Api.Forbidden));

        // Act
        bool cancelled = await viewModel.CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(cancelled);
        Assert.Equal(BookingStatus.Pending, viewModel.Receipt?.Status);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Api.Forbidden)], viewModel.Errors);
    }

    /// <summary>Negative: with no booking read there is nothing to cancel or move, and nothing is called.</summary>
    [Fact]
    public async Task ChangeAsync_NothingLoaded_ReturnsFalseWithoutACall()
    {
        // Arrange
        MyBookingViewModel viewModel = Create();

        // Act
        bool cancelled = await viewModel.CancelAsync(TestContext.Current.CancellationToken);
        bool moved = await viewModel.RescheduleAsync(AdminSamples.Today, AdminSamples.Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(cancelled);
        Assert.False(moved);
        Assert.Empty(_bookings.ReceivedCalls());
    }

    #endregion

    #region Reschedule

    /// <summary>Positive: new dates are asked for the booking of the guest; the request names no booking and no room (UC-010).</summary>
    [Fact]
    public async Task RescheduleAsync_LoadedBooking_AsksForTheNewDatesOnly()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        DateOnly checkIn = AdminSamples.Today.AddDays(10);
        DateOnly checkOut = AdminSamples.Today.AddDays(12);
        GuestRescheduleRequest expected = new(checkIn, checkOut);
        _bookings.RescheduleAsync(expected, Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Ok(AdminSamples.Receipt(7) with { StartDate = checkIn, EndDate = checkOut }));

        // Act
        bool moved = await viewModel.RescheduleAsync(checkIn, checkOut, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(moved);
        Assert.Equal(checkIn, viewModel.Receipt?.StartDate);
        await _bookings.Received(1).RescheduleAsync(expected, Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: new dates that are refused keep the receipt and show the text of the refusal.</summary>
    [Fact]
    public async Task RescheduleAsync_Refused_KeepsTheReceiptAndShowsTheError()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.RescheduleAsync(Arg.Any<GuestRescheduleRequest>(), Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Fail<BookingReceiptDto>(ErrorCodes.Booking.RoomNotAvailable));

        // Act
        bool moved = await viewModel.RescheduleAsync(AdminSamples.Today.AddDays(10), AdminSamples.Today.AddDays(12), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(moved);
        Assert.Equal(AdminSamples.Today, viewModel.Receipt?.StartDate);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Booking.RoomNotAvailable)], viewModel.Errors);
    }

    #endregion

    #region Helpers

    private MyBookingViewModel Create() => new(new PassThroughRunner<IGuestBookingService>(_bookings));

    private async Task<MyBookingViewModel> LoadedAsync()
    {
        _bookings.GetAsync(Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.Receipt(7)));
        MyBookingViewModel viewModel = Create();
        await viewModel.LoadAsync(null, TestContext.Current.CancellationToken);
        return viewModel;
    }

    #endregion
}
