using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Booking;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The presentation logic of the guest's own booking on <c>/my-booking</c> (MIL-018, UC-010): the receipt, the change of
/// dates in the same room, the cancellation, and the result after a change.
/// </summary>
public sealed class MyBookingViewModelTests
{
    private readonly IBookingService _bookings = Substitute.For<IBookingService>();

    #region Load

    /// <summary>Positive: the booking in the cookie is read, with its receipt and the actions it allows now.</summary>
    [Fact]
    public async Task LoadAsync_OwnBooking_ShowsTheBookingAndItsActions()
    {
        // Arrange
        _bookings.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.BookingDetails(7)));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(7, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(7, viewModel.Booking?.BookingId);
        Assert.Equal("B-0007", viewModel.Receipt?.BookingNumber);
        Assert.True(viewModel.CanCancel);
        Assert.True(viewModel.CanReschedule);
        Assert.Empty(viewModel.Errors);
        Assert.Null(viewModel.Notice);
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
        _bookings.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.BookingDetails(7)));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(7, result, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(notice, viewModel.Notice);
    }

    /// <summary>Negative: a refused read shows the text of the refusal, no booking, no actions and no notice.</summary>
    [Fact]
    public async Task LoadAsync_Refused_ShowsTheErrorAndNoBooking()
    {
        // Arrange
        _bookings.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<BookingDetailsDto>(ErrorCodes.Api.GuestUnauthorized));
        MyBookingViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(7, MyBookingViewModel.CancelledResult, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.Booking);
        Assert.Null(viewModel.Receipt);
        Assert.False(viewModel.CanCancel);
        Assert.False(viewModel.CanReschedule);
        Assert.Null(viewModel.Notice);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Api.GuestUnauthorized)], viewModel.Errors);
    }

    /// <summary>Negative: a key below one is a programming error, and the runner is required.</summary>
    [Fact]
    public async Task LoadAsync_InvalidKeyOrMissingRunner_Throws()
    {
        // Arrange
        MyBookingViewModel viewModel = Create();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => viewModel.LoadAsync(0, null, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => new MyBookingViewModel(null!));
    }

    #endregion

    #region Cancel

    /// <summary>Positive: the cancellation is of the booking that was read, and the page keeps the cancelled booking.</summary>
    [Fact]
    public async Task CancelAsync_LoadedBooking_CancelsItAndKeepsTheAnswer()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.CancelAsync(7, Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Ok(AdminSamples.BookingDetails(7, BookingStatus.Cancelled) with { Actions = AdminSamples.NoActions }));

        // Act
        bool cancelled = await viewModel.CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(cancelled);
        Assert.Equal(BookingStatus.Cancelled, viewModel.Booking?.Status);
        Assert.False(viewModel.CanCancel);
        await _bookings.Received(1).CancelAsync(7, Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a refused cancellation keeps the booking as it was and shows the text of the refusal.</summary>
    [Fact]
    public async Task CancelAsync_Refused_KeepsTheBookingAndShowsTheError()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.CancelAsync(7, Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<BookingDetailsDto>(ErrorCodes.Api.Forbidden));

        // Act
        bool cancelled = await viewModel.CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(cancelled);
        Assert.Equal(BookingStatus.Pending, viewModel.Booking?.Status);
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

    /// <summary>Positive: new dates are asked for the booking that was read, in the room it has (UC-010).</summary>
    [Fact]
    public async Task RescheduleAsync_LoadedBooking_AsksForTheNewDatesInTheSameRoom()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        DateOnly checkIn = AdminSamples.Today.AddDays(10);
        DateOnly checkOut = AdminSamples.Today.AddDays(12);
        RescheduleBookingRequest expected = new(7, checkIn, checkOut, AdminSamples.BookingDetails(7).Room.RoomId);
        _bookings.RescheduleAsync(expected, Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Ok(AdminSamples.BookingDetails(7) with { StartDate = checkIn, EndDate = checkOut }));

        // Act
        bool moved = await viewModel.RescheduleAsync(checkIn, checkOut, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(moved);
        Assert.Equal(checkIn, viewModel.Booking?.StartDate);
        await _bookings.Received(1).RescheduleAsync(expected, Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: new dates that are refused keep the booking and show the text of the refusal.</summary>
    [Fact]
    public async Task RescheduleAsync_Refused_KeepsTheBookingAndShowsTheError()
    {
        // Arrange
        MyBookingViewModel viewModel = await LoadedAsync();
        _bookings.RescheduleAsync(Arg.Any<RescheduleBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Fail<BookingDetailsDto>(ErrorCodes.Booking.RoomNotAvailable));

        // Act
        bool moved = await viewModel.RescheduleAsync(AdminSamples.Today.AddDays(10), AdminSamples.Today.AddDays(12), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(moved);
        Assert.Equal(AdminSamples.Today, viewModel.Booking?.StartDate);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Booking.RoomNotAvailable)], viewModel.Errors);
    }

    #endregion

    #region Helpers

    private MyBookingViewModel Create() => new(new PassThroughRunner<IBookingService>(_bookings));

    private async Task<MyBookingViewModel> LoadedAsync()
    {
        _bookings.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(AdminSamples.BookingDetails(7)));
        MyBookingViewModel viewModel = Create();
        await viewModel.LoadAsync(7, null, TestContext.Current.CancellationToken);
        return viewModel;
    }

    #endregion
}
