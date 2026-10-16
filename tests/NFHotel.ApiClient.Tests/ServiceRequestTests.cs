using System.Net;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.ApiClient.Tests;

/// <summary>
/// What each method of the three services sends: the HTTP method, the path and the body, and the exceptions that
/// the real services throw for a key below one or a missing request, which the client throws before it calls.
/// </summary>
public sealed class ServiceRequestTests
{
    private static readonly Uri Base = new("http://api.test/");

    #region Bookings

    /// <summary>Positive: reading a booking is a GET of its path.</summary>
    [Fact]
    public async Task BookingGetByIdAsync_Key_GetsTheBookingPath()
    {
        // Arrange
        var (handler, bookings, _, _) = Services();

        // Act
        await bookings.GetByIdAsync(5, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, "/api/bookings/5"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Equivalence: each transition is a POST of its own path.</summary>
    [Fact]
    public async Task BookingTransitions_Key_PostToTheirOwnPaths()
    {
        // Arrange
        var (handler, bookings, _, _) = Services();
        var calls = new (Func<Task> Call, string Path)[]
        {
            (() => bookings.ConfirmAsync(5, TestContext.Current.CancellationToken), "/api/bookings/5/confirm"),
            (() => bookings.CheckInAsync(5, TestContext.Current.CancellationToken), "/api/bookings/5/check-in"),
            (() => bookings.CheckOutAsync(5, TestContext.Current.CancellationToken), "/api/bookings/5/check-out"),
            (() => bookings.CancelAsync(5, TestContext.Current.CancellationToken), "/api/bookings/5/cancel"),
        };

        // Act and assert
        foreach (var (call, path) in calls)
        {
            await call();
            Assert.Equal((HttpMethod.Post, path), (handler.Method!, handler.PathAndQuery!));
        }
    }

    /// <summary>Edge: the free-rooms query carries both dates in ISO form and the excluded booking only when there is one.</summary>
    [Fact]
    public async Task BookingGetAvailableRoomsAsync_WithAndWithoutExcludedBooking_BuildsTheQuery()
    {
        // Arrange
        var (handler, bookings, _, _) = Services();
        var start = new DateOnly(2026, 3, 1);
        var end = new DateOnly(2026, 3, 4);

        // Act
        await bookings.GetAvailableRoomsAsync(start, end, null, TestContext.Current.CancellationToken);
        var without = handler.PathAndQuery;
        await bookings.GetAvailableRoomsAsync(start, end, 9, TestContext.Current.CancellationToken);
        var with = handler.PathAndQuery;

        // Assert
        Assert.Equal("/api/bookings/available-rooms?checkInDate=2026-03-01&checkOutDate=2026-03-04", without);
        Assert.Equal("/api/bookings/available-rooms?checkInDate=2026-03-01&checkOutDate=2026-03-04&excludeBookingId=9", with);
    }

    /// <summary>Positive: the overview posts its query as the body.</summary>
    [Fact]
    public async Task BookingGetOverviewAsync_Query_PostsTheQueryBody()
    {
        // Arrange
        var (handler, bookings, _, _) = Services();
        var query = new BookingOverviewQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "anna");

        // Act
        await bookings.GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/bookings/overview"), (handler.Method!, handler.PathAndQuery!));
        Assert.Contains("\"periodStart\":\"2026-01-01\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"searchText\":\"anna\"", handler.Body, StringComparison.Ordinal);
    }

    #endregion

    #region Guests and rooms

    /// <summary>Edge: the search text is escaped in the query, and no text means no query at all.</summary>
    [Fact]
    public async Task GuestSearchAsync_TextAndNoText_BuildsTheQuery()
    {
        // Arrange
        var (handler, _, guests, _) = Services();

        // Act
        await guests.SearchAsync("anna & maja", TestContext.Current.CancellationToken);
        var withText = handler.PathAndQuery;
        await guests.SearchAsync("  ", TestContext.Current.CancellationToken);
        var withoutText = handler.PathAndQuery;

        // Assert
        Assert.Equal("/api/guests?searchText=anna%20%26%20maja", withText);
        Assert.Equal("/api/guests", withoutText);
    }

    /// <summary>Positive: update is a PUT with the request as the body, delete is a DELETE of the key.</summary>
    [Fact]
    public async Task GuestUpdateAndDeleteAsync_Requests_UsePutAndDelete()
    {
        // Arrange
        var (handler, _, guests, _) = Services();
        var fields = new GuestFields("A", "B", "a@b.dk", "1", "DK", null);

        // Act
        await guests.UpdateAsync(new UpdateGuestRequest(3, fields), TestContext.Current.CancellationToken);
        var update = (handler.Method, handler.PathAndQuery);
        await guests.DeleteAsync(3, TestContext.Current.CancellationToken);
        var delete = (handler.Method, handler.PathAndQuery);

        // Assert
        Assert.Equal((HttpMethod.Put, "/api/guests"), update);
        Assert.Equal((HttpMethod.Delete, "/api/guests/3"), delete);
    }

    /// <summary>Positive: validating guest fields calls nothing, the rule runs locally and gives the same answer.</summary>
    [Fact]
    public void GuestValidateFields_Fields_MakesNoCallAndGivesTheCodes()
    {
        // Arrange
        var (handler, _, guests, _) = Services();

        // Act
        var result = guests.ValidateFields(new GuestFields(" ", "B", "a@b.dk", "1", "DK", null));

        // Assert
        Assert.Equal(0, handler.Calls);
        Assert.Equal("guest.first_name_required", result.FirstError?.Code);
    }

    /// <summary>Equivalence: each room action is a POST of its own path, and service completion says whether cleaning follows.</summary>
    [Fact]
    public async Task RoomActions_Key_PostToTheirOwnPaths()
    {
        // Arrange
        var (handler, _, _, rooms) = Services();
        var token = TestContext.Current.CancellationToken;
        var calls = new (Func<Task> Call, string Path)[]
        {
            (() => rooms.TakeOutOfServiceAsync(4, token), "/api/rooms/4/take-out-of-service"),
            (() => rooms.SendToMaintenanceAsync(4, token), "/api/rooms/4/send-to-maintenance"),
            (() => rooms.ReturnToServiceAsync(4, token), "/api/rooms/4/return-to-service"),
            (() => rooms.MarkDailyCleaningDueAsync(4, token), "/api/rooms/4/mark-daily-cleaning-due"),
            (() => rooms.MarkDepartureCleaningDueAsync(4, token), "/api/rooms/4/mark-departure-cleaning-due"),
            (() => rooms.StartCleaningAsync(4, token), "/api/rooms/4/start-cleaning"),
            (() => rooms.CompleteCleaningAsync(4, token), "/api/rooms/4/complete-cleaning"),
            (() => rooms.ReportServiceNeededAsync(4, token), "/api/rooms/4/report-service-needed"),
            (() => rooms.StartServiceAsync(4, token), "/api/rooms/4/start-service"),
            (() => rooms.CompleteServiceAsync(4, true, token), "/api/rooms/4/complete-service?requiresCleaning=true"),
            (() => rooms.CompleteServiceAsync(4, false, token), "/api/rooms/4/complete-service?requiresCleaning=false"),
        };

        // Act and assert
        foreach (var (call, path) in calls)
        {
            await call();
            Assert.Equal((HttpMethod.Post, path), (handler.Method!, handler.PathAndQuery!));
        }
    }

    /// <summary>Positive: searching and creating rooms post their bodies.</summary>
    [Fact]
    public async Task RoomSearchAndCreateAsync_Requests_PostTheirBodies()
    {
        // Arrange
        var (handler, _, _, rooms) = Services();

        // Act
        await rooms.SearchAsync(new RoomFilter(Floor: 2), TestContext.Current.CancellationToken);
        var search = (handler.Method, handler.PathAndQuery, handler.Body);
        await rooms.CreateAsync(new CreateRoomRequest("101", 1, RoomSize.Double, 2), TestContext.Current.CancellationToken);
        var create = (handler.Method, handler.PathAndQuery);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/rooms/search", """{"floor":2,"size":null,"status":null,"housekeepingStatus":null}"""), search);
        Assert.Equal((HttpMethod.Post, "/api/rooms"), create);
    }

    #endregion

    #region Arguments

    /// <summary>Boundary: a key below one throws the exception of the real service and makes no call.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Services_KeyBelowOne_ThrowArgumentOutOfRangeWithoutCalling(int key)
    {
        // Arrange
        var (handler, bookings, guests, rooms) = Services();
        var token = TestContext.Current.CancellationToken;
        Func<Task>[] calls =
        [
            () => bookings.GetByIdAsync(key, token),
            () => bookings.ConfirmAsync(key, token),
            () => bookings.CheckInAsync(key, token),
            () => bookings.CheckOutAsync(key, token),
            () => bookings.CancelAsync(key, token),
            () => bookings.RescheduleAsync(new RescheduleBookingRequest(key, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), 1), token),
            () => guests.GetByIdAsync(key, token),
            () => guests.DeleteAsync(key, token),
            () => guests.UpdateAsync(new UpdateGuestRequest(key, new GuestFields("A", "B", "a@b.dk", "1", "DK", null)), token),
            () => rooms.GetByIdAsync(key, token),
            () => rooms.DeleteAsync(key, token),
            () => rooms.TakeOutOfServiceAsync(key, token),
            () => rooms.CompleteServiceAsync(key, false, token),
            () => rooms.UpdateAsync(new UpdateRoomRequest(key, "1", 1, RoomSize.Single, 1), token),
        ];

        // Act and assert
        foreach (var call in calls)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(call);
        }

        Assert.Equal(0, handler.Calls);
    }

    /// <summary>Negative: a missing request or query throws the exception of the real service and makes no call.</summary>
    [Fact]
    public async Task Services_MissingRequest_ThrowArgumentNullWithoutCalling()
    {
        // Arrange
        var (handler, bookings, guests, rooms) = Services();
        var token = TestContext.Current.CancellationToken;
        Func<Task>[] calls =
        [
            () => bookings.GetOverviewAsync(null!, token),
            () => bookings.CreateAsync(null!, token),
            () => bookings.RescheduleAsync(null!, token),
            () => guests.CreateAsync(null!, token),
            () => guests.UpdateAsync(null!, token),
            () => rooms.SearchAsync(null!, token),
            () => rooms.CreateAsync(null!, token),
            () => rooms.UpdateAsync(null!, token),
        ];

        // Act and assert
        foreach (var call in calls)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(call);
        }

        Assert.Equal(0, handler.Calls);
    }

    #endregion

    #region Helpers

    private static (StubHandler Handler, ApiBookingService Bookings, ApiGuestService Guests, ApiRoomService Rooms) Services()
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":[],"errors":[]}""");
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiBookingService(gateway), new ApiGuestService(gateway), new ApiRoomService(gateway));
    }

    #endregion
}