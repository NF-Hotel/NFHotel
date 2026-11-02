using System.Net;

using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.UseCases;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application reads and sets the nightly base rates through the API (MIL-055).</summary>
public sealed class RoomRateRequestTests
{
    private static readonly Uri Base = new("http://api.test/");

    /// <summary>Positive: the list is a GET of the room rates path.</summary>
    [Fact]
    public async Task GetAllAsync_Call_GetsTheRoomRatesPath()
    {
        // Arrange
        (StubHandler? handler, ApiRoomRateService? service) = Build("""{"value":[],"errors":[]}""");

        // Act
        Result<IReadOnlyList<RoomRateDto>> result = await service.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((HttpMethod.Get, "/api/room-rates"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: setting a rate is a PUT of the size below the room rates path.</summary>
    [Fact]
    public async Task SetAsync_Size_PutsThePathOfThatSize()
    {
        // Arrange
        (StubHandler? handler, ApiRoomRateService? service) = Build("""{"value":{"size":"Single","nightlyPrice":90.0,"setBy":"manager","setAt":"2026-11-01T10:00:00+00:00"},"errors":[]}""");

        // Act
        Result<RoomRateDto> result = await service.SetAsync("Single", new SetRoomRateRequest(90m), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(90m, result.Value.NightlyPrice);
        Assert.Equal((HttpMethod.Put, "/api/room-rates/Single"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Negative: a size with characters that need escaping is escaped in the path.</summary>
    [Fact]
    public async Task SetAsync_SizeWithASlash_EscapesItInThePath()
    {
        // Arrange
        (StubHandler? handler, ApiRoomRateService? service) = Build("""{"value":null,"errors":["room_rate.size_invalid"]}""");

        // Act
        await service.SetAsync("a/b", new SetRoomRateRequest(90m), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/api/room-rates/a%2Fb", handler.PathAndQuery);
    }

    private static (StubHandler Handler, ApiRoomRateService Service) Build(string json)
    {
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, json);
        ApiGateway gateway = new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiRoomRateService(gateway));
    }
}
