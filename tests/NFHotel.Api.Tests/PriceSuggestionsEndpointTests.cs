using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Dto.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The price suggestion and price change endpoints (MIL-055): a Manager lists the suggestions, approves one, reads the
/// changes and the effective prices, and reverts; every other caller is refused; the audit page shows both acts.
/// </summary>
public sealed class PriceSuggestionsEndpointTests
{
    private const string Suggestions = "/api/price-suggestions";
    private const string Approve = "/api/price-suggestions/approve";
    private const string Changes = "/api/price-changes";
    private const string HighSingleKey = "single|2027-06-01|2027-08-31|20.00";

    private static ApiFactory FactoryWithReport()
    {
        var factory = new ApiFactory();
        var world = factory.World;
        world.Clock.Today = new DateOnly(2027, 5, 1);
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 100m, "manager", world.Clock.UtcNow));
        world.RoomRates.Add(RoomRate.Create(RoomSize.Double, 150m, "manager", world.Clock.UtcNow));
        PriceSuggestionTestData.StoreLatestReport(world, PriceSuggestionTestData.Content(explanation: "Summer is busy."));
        return factory;
    }

    private static async Task<decimal?> PriceAsync(HttpClient client, string day, string size)
    {
        var body = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EffectivePriceDto>>>($"{Changes}/effective?day={day}", ApiJson.Options, TestContext.Current.CancellationToken);
        return body!.Value!.Single(price => price.Size == size).Price;
    }

    #region Allowed calls

    /// <summary>Positive: the whole flow as a Manager: suggestions, approve, list, effective prices on day 1, last day and after, revert, and the audit page.</summary>
    [Fact]
    public async Task Flow_Manager_ApprovesRevertsAndSeesBothInTheAudit()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        var open = await client.GetFromJsonAsync<ApiResponse<PriceSuggestionListDto>>(Suggestions, ApiJson.Options, TestContext.Current.CancellationToken);
        using var approveResponse = await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken);
        var approved = await approveResponse.Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);
        var list = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<PriceChangeDto>>>(Changes, ApiJson.Options, TestContext.Current.CancellationToken);
        var first = await PriceAsync(client, "2027-06-01", "Single");
        var last = await PriceAsync(client, "2027-08-31", "Single");
        var after = await PriceAsync(client, "2027-09-01", "Single");
        factory.World.Clock.Today = new DateOnly(2027, 7, 10);
        using var revertResponse = await client.PostAsync($"{Changes}/{approved!.Value!.Change.Id}/revert", null, TestContext.Current.CancellationToken);
        var reverted = await revertResponse.Content.ReadFromJsonAsync<ApiResponse<PriceChangeDto>>(ApiJson.Options, TestContext.Current.CancellationToken);
        var afterRevert = await PriceAsync(client, "2027-07-10", "Single");
        var beforeRevert = await PriceAsync(client, "2027-07-09", "Single");
        var audit = await client.GetFromJsonAsync<ApiResponse<StaffAuditPageDto>>("/api/staff/audit?page=1&pageSize=50", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceSuggestionBasis.Ok, open!.Value!.Basis);
        Assert.Equal("Summer is busy.", open.Value.AiExplanation);
        Assert.Equal(HighSingleKey, open.Value.Items[0].Key);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        Assert.True(approved.Value.Created);
        Assert.Equal(("Single", 20m, "tester-manager"), (approved.Value.Change.Size, approved.Value.Change.Percentage, approved.Value.Change.ApprovedBy));
        Assert.Equal([approved.Value.Change.Id], list!.Value!.Select(change => change.Id));
        Assert.Equal((120m, 120m, 100m), (first, last, after));
        Assert.Equal(HttpStatusCode.OK, revertResponse.StatusCode);
        Assert.Equal(("Reverted", "tester-manager"), (reverted!.Value!.State, reverted.Value.RevertedBy));
        Assert.Equal((100m, 120m), (afterRevert, beforeRevert));
        var details = audit!.Value!.Items
            .Where(item => item.Action is StaffAuditAction.PriceChangeApproved or StaffAuditAction.PriceChangeReverted)
            .OrderBy(item => item.Action)
            .Select(item => (item.Action, item.Detail, item.ActorName))
            .ToArray();
        Assert.Equal(
            [
                (StaffAuditAction.PriceChangeApproved, "Single +20.00% 2027-06-01..2027-08-31 (100.00 -> 120.00)", "tester-manager"),
                (StaffAuditAction.PriceChangeReverted, $"#{approved.Value.Change.Id} Single +20.00% 2027-06-01..2027-08-31; ended 2027-07-09", "tester-manager"),
            ],
            details);
    }

    /// <summary>Positive: the effective prices without a day are those of today.</summary>
    [Fact]
    public async Task GetEffective_NoDay_UsesToday()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken);
        factory.World.Clock.Today = new DateOnly(2027, 7, 1);

        // Act
        var body = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EffectivePriceDto>>>($"{Changes}/effective", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2027, 7, 1), body!.Value![0].Day);
        Assert.Equal(120m, body.Value.Single(price => price.Size == "Single").Price);
    }

    /// <summary>Positive: the same approval twice answers 200 both times; the second says it was not created and no second audit event exists.</summary>
    [Fact]
    public async Task Approve_Twice_CreatesOneChangeAndOneAuditEvent()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        var request = new PriceSuggestionApproveRequest(HighSingleKey);

        // Act
        using var first = await client.PostAsJsonAsync(Approve, request, ApiJson.Options, TestContext.Current.CancellationToken);
        using var second = await client.PostAsJsonAsync(Approve, request, ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await second.Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (first.StatusCode, second.StatusCode));
        Assert.False(body!.Value!.Created);
        Assert.Single(factory.World.PriceChanges.Items);
        Assert.Single(factory.World.StaffAudit.Events, item => item.Action == StaffAuditAction.PriceChangeApproved);
    }

    /// <summary>Positive: without a report the list has the basis no_basis and no items.</summary>
    [Fact]
    public async Task Get_NoReport_ReturnsNoBasis()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        var body = await client.GetFromJsonAsync<ApiResponse<PriceSuggestionListDto>>(Suggestions, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceSuggestionBasis.NoBasis, body!.Value!.Basis);
        Assert.Empty(body.Value.Items);
    }

    #endregion

    #region Refused calls

    /// <summary>Negative: an unknown key is 404 with the suggestion code and nothing is stored.</summary>
    [Fact]
    public async Task Approve_UnknownKey_Returns404()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest("single|2027-06-01|2027-08-31|99.00"), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(PriceSuggestionErrors.NotFound, body!.Errors);
        Assert.Empty(factory.World.PriceChanges.Items);
        Assert.Empty(factory.World.StaffAudit.Events);
    }

    /// <summary>Negative: dates the wrong way round are 422 with the period code; a period that overlaps a change is 409 with the overlap code.</summary>
    [Fact]
    public async Task Approve_BadDatesAndOverlap_Return422And409()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        factory.World.PriceChanges.Add(PriceChange.Approve(RoomSize.Single, -10m, new DateOnly(2027, 7, 1), new DateOnly(2027, 7, 31), "manager", factory.World.Clock.UtcNow));

        // Act
        using var backwards = await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey, new DateOnly(2027, 8, 31), new DateOnly(2027, 6, 1)), ApiJson.Options, TestContext.Current.CancellationToken);
        var backwardsBody = await backwards.Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);
        using var overlap = await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken);
        var overlapBody = await overlap.Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, backwards.StatusCode);
        Assert.Contains(PriceChangeErrors.PeriodInvalid, backwardsBody!.Errors);
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Contains(PriceChangeErrors.Overlap, overlapBody!.Errors);
        Assert.Single(factory.World.PriceChanges.Items);
        Assert.Empty(factory.World.StaffAudit.Events);
    }

    /// <summary>Negative: a revert of an unknown id is 404, of a reverted change is 422, and neither writes an audit event.</summary>
    [Fact]
    public async Task Revert_UnknownAndAlreadyReverted_Return404And422()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        var approved = await (await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<ApiResponse<ApprovedPriceChange>>(ApiJson.Options, TestContext.Current.CancellationToken);
        await client.PostAsync($"{Changes}/{approved!.Value!.Change.Id}/revert", null, TestContext.Current.CancellationToken);
        var auditBefore = factory.World.StaffAudit.Events.Count;

        // Act
        using var unknown = await client.PostAsync($"{Changes}/999/revert", null, TestContext.Current.CancellationToken);
        using var again = await client.PostAsync($"{Changes}/{approved.Value.Change.Id}/revert", null, TestContext.Current.CancellationToken);
        var againBody = await again.Content.ReadFromJsonAsync<ApiResponse<PriceChangeDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Contains(PriceChangeErrors.AlreadyReverted, againBody!.Errors);
        Assert.Equal(auditBefore, factory.World.StaffAudit.Events.Count);
    }

    /// <summary>Negative: a role without the permission is 403 on every route and nothing is stored.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task EveryRoute_RoleWithoutThePermission_Returns403(StaffRole role)
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var client = factory.CreateStaffClient(role);

        // Act
        var responses = new List<HttpResponseMessage>
        {
            await client.GetAsync(Suggestions, TestContext.Current.CancellationToken),
            await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken),
            await client.GetAsync(Changes, TestContext.Current.CancellationToken),
            await client.GetAsync($"{Changes}/effective", TestContext.Current.CancellationToken),
            await client.PostAsync($"{Changes}/1/revert", null, TestContext.Current.CancellationToken),
        };

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Empty(factory.World.PriceChanges.Items);
        Assert.Empty(factory.World.StaffAudit.Events);
        responses.ForEach(response => response.Dispose());
    }

    /// <summary>Negative: the service token alone, or no token at all, is 401 on every route.</summary>
    [Fact]
    public async Task EveryRoute_NoStaffToken_Returns401()
    {
        // Arrange
        using var factory = FactoryWithReport();
        using var service = factory.CreateServiceClient();
        using var anonymous = factory.CreateClient();
        var responses = new List<HttpResponseMessage>();

        // Act
        foreach (var client in new[] { service, anonymous })
        {
            responses.Add(await client.GetAsync(Suggestions, TestContext.Current.CancellationToken));
            responses.Add(await client.PostAsJsonAsync(Approve, new PriceSuggestionApproveRequest(HighSingleKey), ApiJson.Options, TestContext.Current.CancellationToken));
            responses.Add(await client.GetAsync(Changes, TestContext.Current.CancellationToken));
            responses.Add(await client.GetAsync($"{Changes}/effective", TestContext.Current.CancellationToken));
            responses.Add(await client.PostAsync($"{Changes}/1/revert", null, TestContext.Current.CancellationToken));
        }

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.Empty(factory.World.PriceChanges.Items);
        responses.ForEach(response => response.Dispose());
    }

    #endregion
}
