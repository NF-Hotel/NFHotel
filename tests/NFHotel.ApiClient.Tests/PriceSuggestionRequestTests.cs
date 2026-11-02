using System.Net;

using NFHotel.Application.Dto.Pricing;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application reads the suggestions, approves, reverts and lists the changes through the API (MIL-055).</summary>
public sealed class PriceSuggestionRequestTests
{
    private static readonly Uri Base = new("http://api.test/");

    private const string ListJson =
        """{"value":{"basis":"ok","generatedForDay":"2027-05-01","items":[{"key":"single|2027-06-01|2027-08-31|20.00","size":"Single","season":"High","firstDay":"2027-06-01","lastDay":"2027-08-31","percentage":20.0,"oldPrice":100.0,"newPrice":120.0,"occupancyPercent":90.0,"thresholdPercent":80.0,"wasLimited":false}],"aiExplanation":"Busy."},"errors":[]}""";

    private const string ChangeJson =
        """{"id":4,"size":"Single","percentage":20.0,"firstDay":"2027-06-01","lastDay":"2027-08-31","state":"Approved","approvedBy":"manager","approvedAt":"2027-05-01T10:00:00+00:00","revertedBy":null,"revertedAt":null,"endedOn":null}""";

    private static (StubHandler Handler, ApiPriceSuggestionService Service) Build(string json)
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, json);
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiPriceSuggestionService(gateway));
    }

    /// <summary>Positive: the open list is a GET of the suggestions path and is read.</summary>
    [Fact]
    public async Task GetOpenAsync_Call_GetsTheSuggestionsPath()
    {
        // Arrange
        var (handler, service) = Build(ListJson);

        // Act
        var result = await service.GetOpenAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("ok", "Busy.", 120m), (result.Value.Basis, result.Value.AiExplanation, result.Value.Items[0].NewPrice));
        Assert.Equal((HttpMethod.Get, "/api/price-suggestions"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: the approval is a POST of the key and the dates to the approve path.</summary>
    [Fact]
    public async Task ApproveAsync_Request_PostsTheKeyToTheApprovePath()
    {
        // Arrange
        var (handler, service) = Build($$"""{"value":{"change":{{ChangeJson}},"created":true},"errors":[]}""");

        // Act
        var result = await service.ApproveAsync(new PriceSuggestionApproveRequest("single|2027-06-01|2027-08-31|20.00", new DateOnly(2027, 6, 10)), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((4, true), (result.Value.Change.Id, result.Value.Created));
        Assert.Equal((HttpMethod.Post, "/api/price-suggestions/approve"), (handler.Method!, handler.PathAndQuery!));
        Assert.Contains("\"key\":\"single|2027-06-01|2027-08-31|20.00\"", handler.Body);
        Assert.Contains("\"firstDay\":\"2027-06-10\"", handler.Body);
    }

    /// <summary>Positive: the revert is a POST to the revert path of the change.</summary>
    [Fact]
    public async Task RevertAsync_Id_PostsTheRevertPath()
    {
        // Arrange
        var (handler, service) = Build($$"""{"value":{{ChangeJson}},"errors":[]}""");

        // Act
        var result = await service.RevertAsync(4, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((HttpMethod.Post, "/api/price-changes/4/revert"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: the list of changes is a GET of the changes path.</summary>
    [Fact]
    public async Task GetChangesAsync_Call_GetsTheChangesPath()
    {
        // Arrange
        var (handler, service) = Build($$"""{"value":[{{ChangeJson}}],"errors":[]}""");

        // Act
        var result = await service.GetChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, Assert.Single(result.Value).Id);
        Assert.Equal((HttpMethod.Get, "/api/price-changes"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Equivalence: the effective prices carry the day in the query when one is given, and no query otherwise.</summary>
    [Fact]
    public async Task GetEffectivePricesAsync_WithAndWithoutDay_BuildsTheQuery()
    {
        // Arrange
        var (handler, service) = Build("""{"value":[],"errors":[]}""");

        // Act
        await service.GetEffectivePricesAsync(new DateOnly(2027, 7, 1), TestContext.Current.CancellationToken);
        var withDay = handler.PathAndQuery;
        await service.GetEffectivePricesAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/api/price-changes/effective?day=2027-07-01", withDay);
        Assert.Equal("/api/price-changes/effective", handler.PathAndQuery);
    }

    /// <summary>Negative: a refusal of the API comes back as its codes.</summary>
    [Fact]
    public async Task ApproveAsync_Refused_ReturnsTheCodes()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.Conflict, """{"value":null,"errors":["price_change.overlap"]}""");
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });

        // Act
        var result = await new ApiPriceSuggestionService(gateway).ApproveAsync(new PriceSuggestionApproveRequest("k"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("price_change.overlap", result.Errors.Select(error => error.Code));
    }

    /// <summary>Negative: a request that is null is refused with an exception.</summary>
    [Fact]
    public async Task ApproveAsync_NullRequest_Throws()
    {
        // Arrange
        var (_, service) = Build(ListJson);

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ApproveAsync(null!, TestContext.Current.CancellationToken));
    }
}
