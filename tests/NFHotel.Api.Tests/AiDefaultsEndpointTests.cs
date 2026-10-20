using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The default AI model endpoints (ADR-0014): every caller with the service token reads the defaults, and only a Manager
/// sets one.
/// </summary>
public sealed class AiDefaultsEndpointTests
{
    private const string Route = "/api/ai-defaults";
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    /// <summary>Positive: a Manager sets the default, the answer carries it, and the staff name comes from the token.</summary>
    [Fact]
    public async Task Put_Manager_SetsTheDefaultAndNamesTheStaffMember()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/website", new SetAiDefaultRequest("llama3.2:3b", RunId), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AiDefaultDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("website", "llama3.2:3b", RunId), (body!.Value!.Purpose, body.Value.Model, body.Value.SourceRunId));
        Assert.False(string.IsNullOrWhiteSpace(body.Value.SetBy));
        Assert.Single(factory.World.AiDefaults.Items);
    }

    /// <summary>Positive: a reader with only the service token lists the defaults, as the market analysis service does.</summary>
    [Fact]
    public async Task Get_ServiceTokenOnly_ListsTheDefaults()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var manager = factory.CreateStaffClient(StaffRole.Manager);
        await manager.PutAsJsonAsync($"{Route}/market_analysis", new SetAiDefaultRequest("phi3.5"), ApiJson.Options, TestContext.Current.CancellationToken);
        using var reader = factory.CreateServiceClient();

        // Act
        var body = await reader.GetFromJsonAsync<ApiResponse<List<AiDefaultDto>>>(Route, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        var value = Assert.Single(body!.Value!);
        Assert.Equal(("market_analysis", "phi3.5"), (value.Purpose, value.Model));
    }

    /// <summary>Negative: an unknown purpose is refused with its code and nothing is stored.</summary>
    [Fact]
    public async Task Put_UnknownPurpose_Returns422WithThePurposeCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/billing", new SetAiDefaultRequest("m1"), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AiDefaultDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(AiDefaultErrors.PurposeInvalid, body!.Errors);
        Assert.Empty(factory.World.AiDefaults.Items);
    }

    /// <summary>Negative: a blank model is refused with its code.</summary>
    [Fact]
    public async Task Put_BlankModel_Returns422WithTheModelCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/website", new SetAiDefaultRequest(" "), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AiDefaultDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(AiDefaultErrors.ModelInvalid, body!.Errors);
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
        using var response = await client.PutAsJsonAsync($"{Route}/website", new SetAiDefaultRequest("m1"), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.World.AiDefaults.Items);
    }

    /// <summary>Negative: the service token alone cannot set a default.</summary>
    [Fact]
    public async Task Put_ServiceTokenOnly_Returns401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/website", new SetAiDefaultRequest("m1"), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.World.AiDefaults.Items);
    }

    /// <summary>Negative: a call without any token is refused before the use case is called.</summary>
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
}
