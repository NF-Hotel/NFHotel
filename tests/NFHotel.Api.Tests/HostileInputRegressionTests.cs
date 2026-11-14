using System.Net;
using System.Net.Http.Json;
using System.Text;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The defects that the hostile-input test found (MIL-077), each with a request that showed it: before the fix the
/// API took the value, or refused it with a body that named a type of the server.
/// </summary>
public sealed class HostileInputRegressionTests : IClassFixture<ApiFactory>
{
    private const string Period = "\"periodStart\":\"2030-06-10\",\"periodEnd\":\"2030-06-12\"";

    private readonly ApiFactory _factory;

    public HostileInputRegressionTests(ApiFactory factory) => _factory = factory;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    #region Unreadable requests

    /// <summary>Negative: a field of the wrong JSON type is refused with the code of the API alone; the body named System.Int32 and the request record.</summary>
    [Fact]
    public async Task CreateRoom_WrongJsonType_AnswersOnlyTheCode()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.PostAsync(ApiRoutes.Rooms, Json("{\"roomNumber\":\"101\",\"floor\":\"text\",\"size\":0,\"capacity\":2}"), Cancellation);
        var body = await response.Content.ReadAsStringAsync(Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("{\"errors\":[\"api.invalid_request\"]}", body);
    }

    #endregion

    #region Text

    /// <summary>Negative: a NUL or a right-to-left override in a room number is refused; the room was created, and PostgreSQL would have failed the save of the NUL with a fault.</summary>
    /// <param name="roomNumber">The room number.</param>
    [Theory]
    [InlineData("1\u00000")]
    [InlineData("\u202E101")]
    public async Task CreateRoom_RefusedCharacter_IsRefused(string roomNumber)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.PostAsJsonAsync(ApiRoutes.Rooms, new { roomNumber, floor = 1, size = 0, capacity = 2 }, Cancellation);

        // Assert
        await AssertRefusedAsync(response);
    }

    /// <summary>Negative: a NUL in the search text of the guest list is refused; PostgreSQL would have failed the search with a fault.</summary>
    [Fact]
    public async Task SearchGuests_NulCharacter_IsRefused()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Guests}?searchText=a%00b", Cancellation);

        // Assert
        await AssertRefusedAsync(response);
    }

    /// <summary>Boundary: the guest list searches a text of the longest length and refuses one character more; it searched a text of one megabyte.</summary>
    /// <param name="length">The length of the search text.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(SearchLimits.MaxTextLength, HttpStatusCode.OK)]
    [InlineData(SearchLimits.MaxTextLength + 1, HttpStatusCode.BadRequest)]
    public async Task SearchGuests_TextLength_IsLimited(int length, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Guests}?searchText={new string('a', length)}", Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Boundary: the booking overview searches a text of the longest length and refuses one character more; it searched a text of one megabyte.</summary>
    /// <param name="length">The length of the search text.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(SearchLimits.MaxTextLength, HttpStatusCode.OK)]
    [InlineData(SearchLimits.MaxTextLength + 1, HttpStatusCode.BadRequest)]
    public async Task BookingOverview_SearchTextLength_IsLimited(int length, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();
        var query = $"{{{Period},\"searchText\":\"{new string('a', length)}\"}}";

        // Act
        using var response = await client.PostAsync($"{ApiRoutes.Bookings}/{ApiRoutes.Overview}", Json(query), Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    #endregion

    #region Enums

    /// <summary>Negative: a sort field that the enum does not define is refused in the booking overview; it was taken.</summary>
    [Fact]
    public async Task BookingOverview_UndefinedSortField_IsRefused()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.PostAsync($"{ApiRoutes.Bookings}/{ApiRoutes.Overview}", Json($"{{{Period},\"sortBy\":-1}}"), Cancellation);

        // Assert
        await AssertRefusedAsync(response);
    }

    /// <summary>Negative: a room size that the enum does not define is refused in the room search; it was taken.</summary>
    [Fact]
    public async Task SearchRooms_UndefinedSize_IsRefused()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.PostAsync($"{ApiRoutes.Rooms}/{ApiRoutes.Search}", Json("{\"size\":-1}"), Cancellation);

        // Assert
        await AssertRefusedAsync(response);
    }

    #endregion

    #region Numbers, keys and dates

    /// <summary>Boundary: the room search takes floor 1 and refuses floor 0, since floors start at 1 (BR-98); it took floor -1.</summary>
    /// <param name="floor">The floor.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    public async Task SearchRooms_Floor_StartsAtOne(int floor, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.PostAsJsonAsync($"{ApiRoutes.Rooms}/{ApiRoutes.Search}", new { floor }, Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Boundary: a day from 1900-01-01 to 2199-12-31 is taken and a day outside is refused; the effective prices took 0001-01-01 and 9999-12-31.</summary>
    /// <param name="day">The day.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData("1900-01-01", HttpStatusCode.OK)]
    [InlineData("1899-12-31", HttpStatusCode.BadRequest)]
    [InlineData("2199-12-31", HttpStatusCode.OK)]
    [InlineData("2200-01-01", HttpStatusCode.BadRequest)]
    public async Task EffectivePrices_Day_LiesInTheRange(string day, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.PriceChanges}/{ApiRoutes.PriceChangesEffective}?day={day}", Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Boundary: the audit trail takes page 1 with 1 to 200 lines and refuses page 0, 0 lines and 201 lines; it turned them into page 1 and 1 or 200 lines.</summary>
    /// <param name="page">The page.</param>
    /// <param name="pageSize">The lines per page.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(1, 1, HttpStatusCode.OK)]
    [InlineData(1, 200, HttpStatusCode.OK)]
    [InlineData(0, 50, HttpStatusCode.BadRequest)]
    [InlineData(1, 0, HttpStatusCode.BadRequest)]
    [InlineData(1, 201, HttpStatusCode.BadRequest)]
    public async Task StaffAudit_Paging_IsInTheRange(int page, int pageSize, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync($"{ApiRoutes.Staff}/{ApiRoutes.StaffAudit}?page={page}&pageSize={pageSize}", Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Boundary: the available rooms take an excluded booking key from 1 and refuse 0, as every key below one is refused; 0 was taken and excluded nothing.</summary>
    /// <param name="excludeBookingId">The key of the booking to leave out.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    public async Task AvailableRooms_ExcludedBookingKey_StartsAtOne(int excludeBookingId, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();
        var path = $"{ApiRoutes.Bookings}/{ApiRoutes.AvailableRooms}?checkInDate=2030-06-10&checkOutDate=2030-06-12&excludeBookingId={excludeBookingId}";

        // Act
        using var response = await client.GetAsync(path, Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Negative: a booking with a guest key below one and a new guest is refused; the key was read as no guest and the booking was made.</summary>
    [Fact]
    public async Task CreateBooking_GuestKeyBelowOne_IsRefused()
    {
        // Arrange
        var room = _factory.World.AddRoom("hostile-201");
        using var client = _factory.CreateAuthorizedClient();
        var request = new { checkInDate = "2030-07-01", checkOutDate = "2030-07-03", roomId = room.Id, guestId = 0, newGuest = ServiceWorld.ValidGuestFields() };

        // Act
        using var response = await client.PostAsJsonAsync(ApiRoutes.Bookings, request, Cancellation);

        // Assert
        await AssertRefusedAsync(response);
    }

    /// <summary>Boundary: a public booking for one guest is made and a booking for no guest is refused; it counted as one and was made.</summary>
    /// <param name="guests">The number of guests.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    public async Task CreatePublicBooking_Guests_StartsAtOne(int guests, HttpStatusCode expected)
    {
        // Arrange
        _factory.World.AddRoom($"hostile-30{guests}", size: RoomSize.Double);
        using var client = _factory.CreateAuthorizedClient();
        var day = new DateOnly(2030, 8, 1).AddDays(10 * guests);
        var request = new { checkInDate = day, checkOutDate = day.AddDays(2), roomSize = RoomSize.Double, guests, newGuest = ServiceWorld.ValidGuestFields() };

        // Act
        using var response = await client.PostAsJsonAsync($"{ApiRoutes.Bookings}/{ApiRoutes.PublicBooking}", request, Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Boundary: the benchmark intake takes a result with a session of 200 characters and record version 1, and refuses a session of 201 characters and version 0; it took a session of one megabyte and version -1, and dropped them.</summary>
    /// <param name="sessionLength">The length of the session of the result.</param>
    /// <param name="version">The version of the record of the result.</param>
    /// <param name="expected">The expected status.</param>
    [Theory]
    [InlineData(200, 1, HttpStatusCode.Created)]
    [InlineData(201, 2, HttpStatusCode.BadRequest)]
    [InlineData(1, 0, HttpStatusCode.BadRequest)]
    public async Task SubmitBenchmarkRun_ResultSessionAndVersion_AreBounded(int sessionLength, int version, HttpStatusCode expected)
    {
        // Arrange
        using var client = _factory.CreateServiceClient();
        var start = new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
        var result = new BenchmarkRequestResultDto(
            version, start, new string('s', sessionLength), "ollama", "m1", "hotel-cancellation", "analysis", "hotel_cancellation", 0, true, 0.5, 2.0,
            10, 20, 5.0, false, "stop", null, null, 1000, null, null, null, null, true, null, "{}", null);
        var run = new SubmitBenchmarkRunRequest(
            2, Guid.NewGuid(), start, start.AddMinutes(5), "hotel-v1", new BenchmarkDatasetDto("d.csv", new string('a', 64), 10),
            new BenchmarkMachineDto("Test CPU", 8, 16384, null, "Linux 6.8"), new BenchmarkBackendDto("ollama", "0.5.1"), [result]);

        // Act
        using var response = await client.PostAsJsonAsync($"{ApiRoutes.Benchmark}/{ApiRoutes.BenchmarkResults}", run, Cancellation);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    #endregion

    #region Helpers

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static async Task AssertRefusedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, Cancellation);
        Assert.Equal([ApiErrorCodes.InvalidRequest], body!.Errors);
    }

    #endregion
}
