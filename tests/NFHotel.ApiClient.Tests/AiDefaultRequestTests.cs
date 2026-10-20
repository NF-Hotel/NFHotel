using System.Net;

using NFHotel.Application.Dto.AiDefaults;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application reads and sets the default AI models through the API (ADR-0014).</summary>
public sealed class AiDefaultRequestTests
{
    private static readonly Uri Base = new("http://api.test/");

    /// <summary>Positive: the list is a GET of the defaults path.</summary>
    [Fact]
    public async Task GetAllAsync_Call_GetsTheDefaultsPath()
    {
        // Arrange
        var (handler, service) = Build("""{"value":[],"errors":[]}""");

        // Act
        var result = await service.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((HttpMethod.Get, "/api/ai-defaults"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: setting a default is a PUT of the purpose below the defaults path.</summary>
    [Fact]
    public async Task SetAsync_Purpose_PutsThePathOfThatPurpose()
    {
        // Arrange
        var (handler, service) = Build("""{"value":{"purpose":"website","model":"m1","setBy":"manager","setAt":"2026-10-19T10:00:00+00:00","sourceRunId":null},"errors":[]}""");

        // Act
        var result = await service.SetAsync("website", new SetAiDefaultRequest("m1"), string.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("m1", result.Value.Model);
        Assert.Equal((HttpMethod.Put, "/api/ai-defaults/website"), (handler.Method!, handler.PathAndQuery!));
    }

    private static (StubHandler Handler, ApiAiDefaultService Service) Build(string json)
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, json);
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiAiDefaultService(gateway));
    }
}
