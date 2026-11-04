using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The mobile app holds no token of the web application (ADR-0015): the sign-in, the rooms and the service notes routes
/// accept the staff token alone, every other route still needs the service token, and the sign-in is limited per address.
/// </summary>
public sealed class MobileAccessTests : IClassFixture<ApiFactory>
{
    private const string Login = "/api/auth/login";
    private const string Rooms = "/api/rooms";
    private const string Notes = "/api/room-service-notes";
    private const string Password = "correct horse battery staple";

    private readonly ApiFactory _factory;

    public MobileAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Sign-in without the service token

    /// <summary>Positive: the right password gives 200 and a token with no service token at all.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_RightPassword_ReturnsTheSession()
    {
        // Arrange
        _factory.AddStaff("mobile-rita", Password, StaffRole.Housekeeping);
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-rita", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StaffRole.Housekeeping, body!.Value!.Role);
    }

    /// <summary>Negative: a wrong password is 401 without the service token too.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        _factory.AddStaff("mobile-walt", Password, StaffRole.Manager);
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-walt", "not the password"), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Boundary: the sixth failed sign-in of a user name gives 429 without the service token.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_SixthFailure_ReturnsTooManyRequests()
    {
        // Arrange
        _factory.AddStaff("mobile-trudy", Password, StaffRole.Manager);
        using var client = _factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-trudy", $"wrong {attempt}"), ApiJson.Options, TestContext.Current.CancellationToken);
        }

        // Act
        using var sixth = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-trudy", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
    }

    /// <summary>Boundary: many user names tried from one address are stopped after 20 failures, whatever the user name.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_ManyUserNamesFromOneAddress_ReturnsTooManyRequests()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("honest", Password, StaffRole.Manager);
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest($"guess-{attempt}", Password), ApiJson.Options, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        // Act
        using var next = await client.PostAsJsonAsync(Login, new SignInRequest("honest", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, next.StatusCode);
    }

    /// <summary>Equivalence: behind the proxy the address is the one in X-Forwarded-For, so one phone does not stop another.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_OtherForwardedAddress_IsNotStopped()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("other-phone", Password, StaffRole.Manager);
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var request = Request($"phone-guess-{attempt}", "203.0.113.7");
            using var failed = await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        // Act
        using var stoppedRequest = Request("other-phone", "203.0.113.7");
        using var otherRequest = Request("other-phone", "203.0.113.8");
        using var stopped = await client.SendAsync(stoppedRequest, TestContext.Current.CancellationToken);
        using var other = await client.SendAsync(otherRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, stopped.StatusCode);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    /// <summary>Equivalence: the web application, which signs everyone in from one address with the service token, is not counted per address.</summary>
    [Fact]
    public async Task Login_WithServiceToken_ManyFailuresOfOtherUserNames_StillSignsInTheRightOne()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("web-user", Password, StaffRole.Manager);
        using var client = factory.CreateServiceClient();
        for (var attempt = 0; attempt < 25; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest($"web-guess-{attempt}", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        }

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("web-user", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage Request(string userName, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Login)
        {
            Content = JsonContent.Create(new SignInRequest(userName, Password), options: ApiJson.Options),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return request;
    }

    #endregion

    #region Rooms with the staff token alone

    /// <summary>Positive: a valid staff token alone reads the rooms.</summary>
    [Theory]
    [InlineData(StaffRole.Housekeeping)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    public async Task Get_Rooms_StaffTokenAlone_ReturnsOk(StaffRole role)
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(role));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: neither token is 401 with the service code.</summary>
    [Fact]
    public async Task Get_Rooms_NoToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a tampered staff token alone is 401.</summary>
    [Fact]
    public async Task Get_Rooms_TamperedStaffTokenAlone_ReturnsUnauthorized()
    {
        // Arrange
        var token = _factory.IssueStaffToken(StaffRole.Manager);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, token[..^3] + (token[^3] == 'A' ? "BBB" : "AAA"));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Negative: a wrong bearer token is not rescued by a valid staff token; the service token is checked when it is sent.</summary>
    [Fact]
    public async Task Get_Rooms_WrongServiceTokenAndValidStaffToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "wrong-token-0123456789-wrong-token-0123456789");
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Positive: Housekeeping starts and completes cleaning with the staff token alone.</summary>
    [Fact]
    public async Task Post_CleaningActions_HousekeepingStaffTokenAlone_ReturnsOk()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var room = factory.World.AddRoom("701", 7);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(StaffRole.Housekeeping));

        // Act
        using var due = await client.PostAsync($"{Rooms}/{room.Id}/mark-daily-cleaning-due", null, TestContext.Current.CancellationToken);
        using var start = await client.PostAsync($"{Rooms}/{room.Id}/start-cleaning", null, TestContext.Current.CancellationToken);
        using var complete = await client.PostAsync($"{Rooms}/{room.Id}/complete-cleaning", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, due.StatusCode);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    /// <summary>Negative: Housekeeping gets 403 on delete and on taking a room out of service.</summary>
    [Fact]
    public async Task Delete_Room_HousekeepingStaffTokenAlone_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var room = factory.World.AddRoom("702", 7);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(StaffRole.Housekeeping));

        // Act
        using var delete = await client.DeleteAsync($"{Rooms}/{room.Id}", TestContext.Current.CancellationToken);
        using var outOfService = await client.PostAsync($"{Rooms}/{room.Id}/take-out-of-service", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, outOfService.StatusCode);
    }

    #endregion

    #region Service notes with the staff token alone

    /// <summary>Positive: every role reads the notes and the history with a valid staff token alone.</summary>
    [Theory]
    [InlineData(StaffRole.Housekeeping)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    public async Task Get_Notes_StaffTokenAlone_ReturnsOk(StaffRole role)
    {
        // Arrange
        await using var factory = new NotesFactory();
        using var client = StaffTokenClient(factory, role);

        // Act
        using var current = await client.GetAsync(Notes, TestContext.Current.CancellationToken);
        using var history = await client.GetAsync($"{Notes}/history/1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
    }

    /// <summary>Positive: every role marks a note as done and undoes the mark with a valid staff token alone.</summary>
    [Theory]
    [InlineData(StaffRole.Housekeeping)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    public async Task Post_DoneAndUndo_StaffTokenAlone_ReturnsOk(StaffRole role)
    {
        // Arrange
        await using var factory = new NotesFactory();
        using var client = StaffTokenClient(factory, role);

        // Act
        using var done = await client.PostAsync($"{Notes}/1/done", null, TestContext.Current.CancellationToken);
        using var undo = await client.PostAsync($"{Notes}/1/undo-done", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal(HttpStatusCode.OK, undo.StatusCode);
    }

    /// <summary>Equivalence: the staff token alone does not widen who may write a note.</summary>
    [Theory]
    [InlineData(StaffRole.Housekeeping, HttpStatusCode.OK)]
    [InlineData(StaffRole.Manager, HttpStatusCode.OK)]
    [InlineData(StaffRole.Receptionist, HttpStatusCode.Forbidden)]
    public async Task Post_Note_StaffTokenAlone_FollowsThePermission(StaffRole role, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = new NotesFactory();
        using var client = StaffTokenClient(factory, role);
        var request = new CreateRoomServiceNoteRequest(1, "The tap drips");

        // Act
        using var response = await client.PostAsJsonAsync(Notes, request, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Equivalence: the staff token alone does not widen who may take a note back from the history.</summary>
    [Theory]
    [InlineData(StaffRole.Manager, HttpStatusCode.OK)]
    [InlineData(StaffRole.Housekeeping, HttpStatusCode.Forbidden)]
    [InlineData(StaffRole.Receptionist, HttpStatusCode.Forbidden)]
    public async Task Post_Reopen_StaffTokenAlone_IsForAManagerOnly(StaffRole role, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = new NotesFactory();
        using var client = StaffTokenClient(factory, role);

        // Act
        using var response = await client.PostAsync($"{Notes}/1/reopen", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Negative: neither token is 401 with the service code.</summary>
    [Fact]
    public async Task Get_Notes_NoToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(Notes, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a tampered staff token alone is 401.</summary>
    [Fact]
    public async Task Get_Notes_TamperedStaffTokenAlone_ReturnsUnauthorized()
    {
        // Arrange
        var token = _factory.IssueStaffToken(StaffRole.Manager);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, token[..^3] + (token[^3] == 'A' ? "BBB" : "AAA"));

        // Act
        using var response = await client.GetAsync(Notes, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient StaffTokenClient(ApiFactory factory, StaffRole role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(role));
        return client;
    }

    /// <summary>The API with notes that always succeed: these tests are about who gets through, not about the notes.</summary>
    private sealed class NotesFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRoomServiceNoteService>();
                services.AddSingleton<IRoomServiceNoteService, StubNotes>();
            });
        }
    }

    private sealed class StubNotes : IRoomServiceNoteService
    {
        private static readonly RoomServiceNoteDto Note = new(1, 1, "The tap drips", "tester", DateTimeOffset.UnixEpoch, null, null, null);

        public Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<RoomServiceNoteDto>>.Success([Note]));

        public Task<Result<IReadOnlyList<RoomServiceNoteDto>>> GetHistoryAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<RoomServiceNoteDto>>.Success([]));

        public Task<Result<RoomServiceNoteDto>> CreateAsync(CreateRoomServiceNoteRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<RoomServiceNoteDto>.Success(Note));

        public Task<Result<RoomServiceNoteDto>> MarkDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<RoomServiceNoteDto>.Success(Note));

        public Task<Result<RoomServiceNoteDto>> UndoDoneAsync(int noteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<RoomServiceNoteDto>.Success(Note));

        public Task<Result<RoomServiceNoteDto>> ReopenAsync(int noteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<RoomServiceNoteDto>.Success(Note));
    }

    #endregion

    #region The staff accounts (MIL-059)

    /// <summary>Equivalence: the staff token alone reaches the staff accounts, and the permission still decides.</summary>
    [Theory]
    [InlineData(StaffRole.Manager, HttpStatusCode.OK)]
    [InlineData(StaffRole.Housekeeping, HttpStatusCode.Forbidden)]
    [InlineData(StaffRole.Receptionist, HttpStatusCode.Forbidden)]
    public async Task Get_Staff_StaffTokenAlone_FollowsThePermission(StaffRole role, HttpStatusCode expected)
    {
        // Arrange
        using var client = StaffTokenClient(_factory, role);

        // Act
        using var response = await client.GetAsync("/api/staff", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>Negative: neither token is 401 with the service code on the staff accounts too.</summary>
    [Fact]
    public async Task Get_Staff_NoToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/staff", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    #endregion

    #region Everything else still needs the service token

    /// <summary>Negative: a Manager staff token alone is 401 on every route group outside the list.</summary>
    [Theory]
    [InlineData("/api/bookings/1")]
    [InlineData("/api/bookings/available-rooms?checkInDate=2030-01-02&checkOutDate=2030-01-04")]
    [InlineData("/api/guests")]
    [InlineData("/api/benchmark/runs")]
    [InlineData("/api/market-analysis/latest")]
    [InlineData("/api/market-analysis/runs/current")]
    [InlineData("/api/ai-defaults")]
    [InlineData("/api/auth/other")]
    [InlineData("/api/room-service-notesXYZ")]
    [InlineData("/api/staffXYZ")]
    public async Task Get_OtherRoutes_StaffTokenAlone_ReturnsUnauthorizedWithTheServiceCode(string path)
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a route that merely starts with the letters of a listed route is not listed.</summary>
    [Fact]
    public async Task Get_RouteWithListedPrefixInTheName_StaffTokenAlone_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync("/api/roomsXYZ", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
