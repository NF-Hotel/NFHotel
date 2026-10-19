using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// Every endpoint of the API through HTTP with the real services over in-memory repositories: the route
/// exists, the body and the query are read, and the status says what kind of result it is.
/// </summary>
public sealed class ApiEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Status of a result

    /// <summary>Positive: a booking that exists comes back as 200 with the value and no errors.</summary>
    [Fact]
    public async Task GetBooking_Existing_ReturnsOkWithTheDetails()
    {
        // Arrange
        var (_, _, bookingId) = Seed();
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Bookings}/{bookingId}", TestContext.Current.CancellationToken);
        var body = await Read<BookingDetailsDto>(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(bookingId, body.Value!.BookingId);
        Assert.Empty(body.Errors);
    }

    /// <summary>Negative: an unknown booking is 404 with the not-found code and no value.</summary>
    [Fact]
    public async Task GetBooking_Unknown_ReturnsNotFoundWithTheCode()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Bookings}/9999", TestContext.Current.CancellationToken);
        var body = await Read<BookingDetailsDto>(response);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.NotFound], body.Errors);
        Assert.Null(body.Value);
    }

    /// <summary>Negative: a double booking is a conflict, 409.</summary>
    [Fact]
    public async Task CreateBooking_RoomTaken_ReturnsConflict()
    {
        // Arrange
        var (roomId, guestId, _) = Seed();
        using var client = _factory.CreateAuthorizedClient();
        var request = new CreateBookingRequest(Today(), Today().AddDays(2), roomId, guestId, null);

        // Act
        using var response = await client.PostAsJsonAsync(ApiRoutes.Bookings, request, ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await Read<BookingDetailsDto>(response);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.RoomNotAvailable], body.Errors);
    }

    /// <summary>Negative: a rule error such as an inverted period is 422 with the code of the rule.</summary>
    [Fact]
    public async Task CreateBooking_EndBeforeStart_ReturnsUnprocessableEntityWithTheCode()
    {
        // Arrange
        var (roomId, guestId, _) = Seed();
        using var client = _factory.CreateAuthorizedClient();
        var request = new CreateBookingRequest(Today().AddDays(5), Today().AddDays(2), roomId, guestId, null);

        // Act
        using var response = await client.PostAsJsonAsync(ApiRoutes.Bookings, request, ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await Read<BookingDetailsDto>(response);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.EndNotAfterStart], body.Errors);
    }

    /// <summary>Boundary: a key below one is a programming error of the caller, 400, not a missing resource.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRoom_KeyBelowOne_ReturnsBadRequest(int roomId)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Rooms}/{roomId}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["api.invalid_request"], body!.Errors);
    }

    /// <summary>Negative: a room that has bookings is not deleted and the answer is a conflict.</summary>
    [Fact]
    public async Task DeleteRoom_WithBookings_ReturnsConflict()
    {
        // Arrange
        var (roomId, _, _) = Seed();
        _factory.World.Rooms.RoomIdsWithBookings.Add(roomId);
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.DeleteAsync($"{ApiRoutes.Rooms}/{roomId}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal([ErrorCodes.Room.HasBookings], body!.Errors);
    }

    /// <summary>Negative: a request without a body is refused by the framework, 400, before the use case runs.</summary>
    [Fact]
    public async Task CreateBooking_NoBody_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();
        using var empty = new StringContent(string.Empty);

        // Act
        using var response = await client.PostAsync(ApiRoutes.Bookings, empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Booking routes

    /// <summary>Positive: the overview, the free rooms and every transition of a booking are reachable.</summary>
    [Fact]
    public async Task BookingRoutes_EveryEndpoint_IsReachable()
    {
        // Arrange
        var (roomId, _, bookingId) = Seed();
        using var client = _factory.CreateAuthorizedClient();
        var query = new BookingOverviewQuery(Today(), Today().AddDays(30));

        // Act
        using var overview = await client.PostAsJsonAsync($"{ApiRoutes.Bookings}/{ApiRoutes.Overview}", query, ApiJson.Options, TestContext.Current.CancellationToken);
        using var free = await client.GetAsync($"{ApiRoutes.Bookings}/{ApiRoutes.AvailableRooms}?checkInDate={Today().AddDays(20):yyyy-MM-dd}&checkOutDate={Today().AddDays(22):yyyy-MM-dd}&excludeBookingId={bookingId}", TestContext.Current.CancellationToken);
        using var confirm = await client.PostAsync($"{ApiRoutes.Bookings}/{bookingId}/confirm", null, TestContext.Current.CancellationToken);
        using var checkIn = await client.PostAsync($"{ApiRoutes.Bookings}/{bookingId}/check-in", null, TestContext.Current.CancellationToken);
        _factory.World.Clock.TimeOfDay = TimeSpan.FromHours(18);
        using var checkOut = await client.PostAsync($"{ApiRoutes.Bookings}/{bookingId}/check-out", null, TestContext.Current.CancellationToken);
        _factory.World.Clock.TimeOfDay = TimeSpan.FromHours(12);

        // Assert
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
        Assert.Equal(HttpStatusCode.OK, free.StatusCode);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, checkOut.StatusCode);
        Assert.NotEqual(0, roomId);
    }

    /// <summary>Positive: cancelling and moving a booking are reachable.</summary>
    [Fact]
    public async Task BookingRoutes_CancelAndReschedule_AreReachable()
    {
        // Arrange
        var (roomId, _, bookingId) = Seed();
        var (_, _, otherBookingId) = Seed();
        using var client = _factory.CreateAuthorizedClient();
        var move = new RescheduleBookingRequest(bookingId, Today().AddDays(40), Today().AddDays(42), roomId);

        // Act
        using var reschedule = await client.PostAsJsonAsync($"{ApiRoutes.Bookings}/{ApiRoutes.Reschedule}", move, ApiJson.Options, TestContext.Current.CancellationToken);
        using var cancel = await client.PostAsync($"{ApiRoutes.Bookings}/{otherBookingId}/cancel", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, reschedule.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    #endregion

    #region Guest routes

    /// <summary>Positive: create, read, search, update and delete a guest; invalid fields are 422.</summary>
    [Fact]
    public async Task GuestRoutes_EveryEndpoint_IsReachable()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();
        var fields = new GuestFields("Maja", "Holm", "maja@example.com", "+4511223344", "Denmark", null);

        // Act
        using var create = await client.PostAsJsonAsync(ApiRoutes.Guests, new CreateGuestRequest(fields), ApiJson.Options, TestContext.Current.CancellationToken);
        var created = await Read<GuestDetailsDto>(create);
        var id = created.Value!.GuestId;
        using var get = await client.GetAsync($"{ApiRoutes.Guests}/{id}", TestContext.Current.CancellationToken);
        using var search = await client.GetAsync($"{ApiRoutes.Guests}?searchText=maja", TestContext.Current.CancellationToken);
        using var update = await client.PutAsJsonAsync(ApiRoutes.Guests, new UpdateGuestRequest(id, fields with { LastName = "Nielsen" }), ApiJson.Options, TestContext.Current.CancellationToken);
        using var invalid = await client.PostAsJsonAsync(ApiRoutes.Guests, new CreateGuestRequest(fields with { FirstName = " " }), ApiJson.Options, TestContext.Current.CancellationToken);
        using var delete = await client.DeleteAsync($"{ApiRoutes.Guests}/{id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    #endregion

    #region Room routes

    /// <summary>Positive: create, list, search, filter options, update and delete a room.</summary>
    [Fact]
    public async Task RoomRoutes_Crud_IsReachable()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();
        var number = $"R{_factory.World.Rooms.Items.Count + 100}";

        // Act
        using var create = await client.PostAsJsonAsync(ApiRoutes.Rooms, new CreateRoomRequest(number, 3, RoomSize.Double, 2), ApiJson.Options, TestContext.Current.CancellationToken);
        var id = (await Read<RoomDetailsDto>(create)).Value!.RoomId;
        using var all = await client.GetAsync(ApiRoutes.Rooms, TestContext.Current.CancellationToken);
        using var search = await client.PostAsJsonAsync($"{ApiRoutes.Rooms}/{ApiRoutes.Search}", new RoomFilter(Floor: 3), ApiJson.Options, TestContext.Current.CancellationToken);
        using var options = await client.GetAsync($"{ApiRoutes.Rooms}/{ApiRoutes.FilterOptions}", TestContext.Current.CancellationToken);
        using var update = await client.PutAsJsonAsync(ApiRoutes.Rooms, new UpdateRoomRequest(id, number, 4, RoomSize.Suite, 3), ApiJson.Options, TestContext.Current.CancellationToken);
        using var delete = await client.DeleteAsync($"{ApiRoutes.Rooms}/{id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Equal(HttpStatusCode.OK, options.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    /// <summary>Positive: the room transitions of bookability and of housekeeping are reachable.</summary>
    [Fact]
    public async Task RoomRoutes_Transitions_AreReachable()
    {
        // Arrange
        var (roomId, _, _) = Seed();
        using var client = _factory.CreateAuthorizedClient();
        var steps = new[]
        {
            "take-out-of-service", "return-to-service", "send-to-maintenance", "return-to-service",
            "mark-daily-cleaning-due", "start-cleaning", "complete-cleaning",
            "report-service-needed", "start-service", "complete-service?requiresCleaning=true",
            "mark-departure-cleaning-due",
        };

        // Act and assert
        foreach (var step in steps)
        {
            using var response = await client.PostAsync($"{ApiRoutes.Rooms}/{roomId}/{step}", null, TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{step} answered {(int)response.StatusCode}");
        }
    }

    #endregion

    #region Helpers

    private (int RoomId, int GuestId, int BookingId) Seed()
    {
        var world = _factory.World;
        var room = world.AddRoom($"{world.Rooms.Items.Count + 1}0{world.Rooms.Items.Count}");
        var guest = world.AddGuest();
        var booking = world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 3);
        return (room.Id, guest.Id, booking.Id);
    }

    private DateOnly Today() => _factory.World.Clock.Today;

    private static async Task<ApiResponse<T>> Read<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ApiJson.Options, TestContext.Current.CancellationToken))!;

    #endregion
}
