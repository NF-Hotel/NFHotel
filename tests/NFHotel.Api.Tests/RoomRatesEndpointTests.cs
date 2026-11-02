using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.Dto.Staff;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>The nightly base rate endpoints (MIL-055): staff read the rates, only a Manager sets one, and the audit page shows the change.</summary>
public sealed class RoomRatesEndpointTests
{
    private const string Route = "/api/room-rates";

    #region Allowed calls

    /// <summary>Positive: a Manager sets a rate, the answer carries it, and the staff name comes from the token.</summary>
    [Fact]
    public async Task Put_Manager_SetsTheRateAndNamesTheStaffMember()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/single", new SetRoomRateRequest(89.5m), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RoomRateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("Single", 89.5m, "tester-manager"), (body!.Value!.Size, body.Value.NightlyPrice, body.Value.SetBy));
        Assert.Single(factory.World.RoomRates.Items);
    }

    /// <summary>Positive: staff who may see rooms list the rates.</summary>
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task Get_StaffWithRoomsView_ListsTheRates(StaffRole role)
    {
        // Arrange
        using var factory = new ApiFactory();
        using var manager = factory.CreateStaffClient(StaffRole.Manager);
        await manager.PutAsJsonAsync($"{Route}/suite", new SetRoomRateRequest(300m), ApiJson.Options, TestContext.Current.CancellationToken);
        using var reader = factory.CreateStaffClient(role);

        // Act
        var body = await reader.GetFromJsonAsync<ApiResponse<List<RoomRateDto>>>(Route, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        var value = Assert.Single(body!.Value!);
        Assert.Equal(("Suite", 300m), (value.Size, value.NightlyPrice));
    }

    /// <summary>Positive: the audit page returns the event of the change with the old and the new price.</summary>
    [Fact]
    public async Task Audit_AfterTwoChanges_ShowsTheOldAndTheNewPrice()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PutAsJsonAsync($"{Route}/double", new SetRoomRateRequest(100m), ApiJson.Options, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"{Route}/Double", new SetRoomRateRequest(110m), ApiJson.Options, TestContext.Current.CancellationToken);

        // Act
        var body = await client.GetFromJsonAsync<ApiResponse<StaffAuditPageDto>>("/api/staff/audit?page=1&pageSize=50", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        var rateEvents = body!.Value!.Items.Where(item => item.Action == StaffAuditAction.RoomRateChanged).ToArray();
        Assert.Equal(["Double: 100.00 -> 110.00", "Double: (none) -> 100.00"], rateEvents.Select(item => item.Detail));
        Assert.All(rateEvents, item => Assert.Equal("tester-manager", item.ActorName));
    }

    #endregion

    #region Refused calls

    /// <summary>Negative: an unknown size is refused with its code and nothing is stored.</summary>
    [Fact]
    public async Task Put_UnknownSize_Returns422WithTheSizeCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/penthouse", new SetRoomRateRequest(100m), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RoomRateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(RoomRateErrors.SizeInvalid, body!.Errors);
        Assert.Empty(factory.World.RoomRates.Items);
    }

    /// <summary>Negative: a price of 0 is refused with its code.</summary>
    [Fact]
    public async Task Put_PriceZero_Returns422WithThePriceCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/single", new SetRoomRateRequest(0m), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RoomRateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(RoomRateErrors.PriceInvalid, body!.Errors);
    }

    /// <summary>Negative: a role without the permission is 403 and nothing is stored.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task Put_RoleWithoutThePermission_Returns403(StaffRole role)
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(role);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/single", new SetRoomRateRequest(100m), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.World.RoomRates.Items);
        Assert.Empty(factory.World.StaffAudit.Events);
    }

    /// <summary>Negative: the service token alone cannot set or read the rates.</summary>
    [Fact]
    public async Task PutAndGet_ServiceTokenOnly_Return401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var put = await client.PutAsJsonAsync($"{Route}/single", new SetRoomRateRequest(100m), ApiJson.Options, TestContext.Current.CancellationToken);
        using var get = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Empty(factory.World.RoomRates.Items);
    }

    /// <summary>Negative: a call without any token is refused.</summary>
    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
