using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// The ownership rule of the guest sign-in (MIL-018, ADR-0047 decision 5, UC-010): a signed-in guest reads, cancels and
/// moves only the booking in the guest token and keeps the room; since MIL-085 a guest reads no guest by its key at all
/// (ADR-0049 decision 3). Every guest operation is stated allowed or refused, for the own booking and another one.
/// </summary>
public sealed class GuestOwnershipTests
{
    private const string ReadBooking = "read the booking";
    private const string ReadGuest = "read the guest of the booking";
    private const string Cancel = "cancel the booking";
    private const string Reschedule = "move the booking to other dates";

    #region The rule

    /// <summary>Positive: the guest of a booking may use each permission of the Guest role on that booking.</summary>
    /// <param name="permission">A permission that the Guest role holds.</param>
    [Theory]
    [InlineData(Permissions.CreateBooking)]
    [InlineData(Permissions.EditOwnBooking)]
    [InlineData(Permissions.ViewOwnGuestData)]
    public void Allows_OwnBookingAndAPermissionOfTheGuestRole_IsTrue(string permission)
    {
        // Arrange
        FakeGuestCallContext guest = new() { BookingId = 7 };

        // Act
        bool allowed = GuestOwnership.Allows(guest, 7, permission);

        // Assert
        Assert.True(allowed);
    }

    /// <summary>Negative: another booking, or no guest at all, is never allowed.</summary>
    /// <param name="guestBooking">The booking in the guest token, or <c>null</c> without a guest.</param>
    [Theory]
    [InlineData(8)]
    [InlineData(null)]
    public void Allows_AnotherBookingOrNoGuest_IsFalse(int? guestBooking)
    {
        // Arrange
        FakeGuestCallContext guest = new() { BookingId = guestBooking };

        // Act
        bool allowed = GuestOwnership.Allows(guest, 7, Permissions.EditOwnBooking);

        // Assert
        Assert.False(allowed);
    }

    /// <summary>Negative: a permission that the Guest role lacks is refused even on the own booking (least privilege).</summary>
    /// <param name="permission">A permission outside the Guest role.</param>
    [Theory]
    [InlineData(Permissions.EditOtherBookings)]
    [InlineData(Permissions.ViewOtherGuestData)]
    [InlineData(Permissions.ViewOwnPassportData)]
    [InlineData(Permissions.ViewGuestPassportData)]
    [InlineData(Permissions.BookingsView)]
    [InlineData(Permissions.GuestsManage)]
    public void Allows_PermissionTheGuestRoleLacks_IsFalse(string permission)
    {
        // Arrange
        FakeGuestCallContext guest = new() { BookingId = 7 };

        // Act
        bool allowed = GuestOwnership.Allows(guest, 7, permission);

        // Assert
        Assert.False(allowed);
    }

    /// <summary>Negative: the guest and the permission are required.</summary>
    [Fact]
    public void Allows_MissingArgument_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => GuestOwnership.Allows(null!, 7, Permissions.EditOwnBooking));
        Assert.Throws<ArgumentException>(() => GuestOwnership.Allows(new FakeGuestCallContext(), 7, " "));
    }

    #endregion

    #region Every guest operation

    /// <summary>Positive: on the booking in the guest token, each guest operation is allowed (MIL-018 criterion 2).</summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_OwnBooking_IsAllowed(string operation)
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);
        world.GuestCall.BookingId = own.Id;

        // Act
        Result result = await RunAsync(world, operation, own);

        // Assert
        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(error => error.Code)));
    }

    /// <summary>
    /// Security: on another booking, each guest operation is refused with 403 before anything changes, so the answer does
    /// not tell whether that booking exists (MIL-018 criterion 2).
    /// </summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(ReadGuest)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_AnotherBooking_IsForbidden(string operation)
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking other) = TwoBookings(world);
        world.GuestCall.BookingId = own.Id;
        DateOnly startBefore = other.StartDate;
        DateOnly endBefore = other.EndDate;

        // Act
        Result result = await RunAsync(world, operation, other);

        // Assert
        Assert.Equal([ErrorCodes.Api.Forbidden], result.Errors.Select(error => error.Code));
        Assert.Equal(BookingStatus.Pending, other.Status);
        Assert.Equal(startBefore, other.StartDate);
        Assert.Equal(endBefore, other.EndDate);
    }

    /// <summary>Negative: without a guest token and without a staff permission, each operation is refused.</summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(ReadGuest)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_NoGuestAndNoStaff_IsForbidden(string operation)
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);

        // Act
        Result result = await RunAsync(world, operation, own);

        // Assert
        Assert.Equal([ErrorCodes.Api.Forbidden], result.Errors.Select(error => error.Code));
    }

    /// <summary>Security: a guest moves the own booking to other dates but never to another room (UC-010).</summary>
    [Fact]
    public async Task RescheduleAsync_GuestToAnotherRoom_IsForbiddenAndChangesNothing()
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);
        Room otherRoom = world.AddRoom("301", 3, RoomSize.Double, 2);
        world.GuestCall.BookingId = own.Id;
        int roomBefore = own.RoomId;

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().RescheduleAsync(
            new RescheduleBookingRequest(own.Id, own.StartDate, own.EndDate, otherRoom.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Equal(roomBefore, own.RoomId);
    }

    /// <summary>
    /// Security (ADR-0049 decision 3): a guest token reads no guest by its key, not even the guest of the own booking,
    /// because no page of a guest needs it; the receipt is all a guest sees.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_GuestOfTheOwnBooking_IsForbidden()
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);
        world.GuestCall.BookingId = own.Id;

        // Act
        Result<GuestDetailsDto> result = await world.GuestService().GetByIdAsync(own.GuestId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    /// <summary>Negative: a guest token for a booking that no longer exists reads no guest.</summary>
    [Fact]
    public async Task GetByIdAsync_GuestOfAnUnknownBooking_IsForbidden()
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);
        world.GuestCall.BookingId = own.Id + 100;

        // Act
        Result<GuestDetailsDto> result = await world.GuestService().GetByIdAsync(own.GuestId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    /// <summary>Equivalence: staff with the permission are not limited by the rule, with or without a guest token around.</summary>
    [Fact]
    public async Task RescheduleAsync_StaffToAnotherRoom_IsAllowedAsBefore()
    {
        // Arrange
        ServiceWorld world = new();
        (Booking own, Booking _) = TwoBookings(world);
        Room otherRoom = world.AddRoom("301", 3, RoomSize.Double, 2);
        IStaffAuthorizationService staff = Substitute.For<IStaffAuthorizationService>();
        staff.HasPermissionAsync(Permissions.EditOtherBookings, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(staff).RescheduleAsync(
            new RescheduleBookingRequest(own.Id, own.StartDate, own.EndDate, otherRoom.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(otherRoom.Id, own.RoomId);
    }

    #endregion

    #region Helpers

    /// <summary>Two pending bookings in two rooms, each with its own guest; the first guest has a passport number.</summary>
    private static (Booking Own, Booking Other) TwoBookings(ServiceWorld world)
    {
        Room first = world.AddRoom("101", 1, RoomSize.Double, 2);
        Room second = world.AddRoom("102", 1, RoomSize.Double, 2);
        Guest anna = world.AddGuest("Anna", "Jensen", "anna@example.com", "P1234567");
        Guest bo = world.AddGuest("Bo", "Hansen", "bo@example.com", "P7654321");
        Booking own = world.AddBooking(first.Id, anna.Id, startOffsetDays: 5, nights: 2);
        Booking other = world.AddBooking(second.Id, bo.Id, startOffsetDays: 5, nights: 2);
        return (own, other);
    }

    /// <summary>Runs one guest operation on a booking through the real services, without any staff permission.</summary>
    private static async Task<Result> RunAsync(ServiceWorld world, string operation, Booking booking)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        return operation switch
        {
            ReadBooking => await world.BookingService().GetByIdAsync(booking.Id, token),
            ReadGuest => await world.GuestService().GetByIdAsync(booking.GuestId, token),
            Cancel => await world.BookingService().CancelAsync(booking.Id, token),
            Reschedule => await world.BookingService().RescheduleAsync(
                new RescheduleBookingRequest(booking.Id, booking.StartDate.AddDays(10), booking.EndDate.AddDays(10), booking.RoomId),
                token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation."),
        };
    }

    #endregion
}
