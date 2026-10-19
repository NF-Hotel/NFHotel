using System.Net;

using NFHotel.Application.Errors;

namespace NFHotel.ApiClient.Tests;

/// <summary>How the gateway turns an answer of the API, or the lack of one, into the result of a use case.</summary>
public sealed class ApiGatewayTests
{
    private static readonly Uri Base = new("http://api.test/");

    #region Answers

    /// <summary>Positive: a 200 with a value is a success with that value.</summary>
    [Fact]
    public async Task GetAsync_OkWithValue_ReturnsTheValue()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":7},"errors":[]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(7, result.Value.N);
    }

    /// <summary>Negative: a 404 with a code is a failure with that code.</summary>
    [Fact]
    public async Task GetAsync_NotFoundWithCode_ReturnsThatCode()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.NotFound, """{"value":null,"errors":["booking.not_found"]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("booking.not_found", result.FirstError?.Code);
    }

    /// <summary>Edge: several codes arrive in the order the API sent them.</summary>
    [Fact]
    public async Task PostAsync_UnprocessableWithSeveralCodes_ReturnsThemInOrder()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.UnprocessableEntity, """{"value":null,"errors":["a.one","b.two","c.three"]}"""));

        // Act
        var result = await gateway.PostAsync<Sample>("api/x", new Sample(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["a.one", "b.two", "c.three"], result.Errors.Select(error => error.Code).ToArray());
    }

    /// <summary>Positive: a delete that answers 200 is a plain success.</summary>
    [Fact]
    public async Task DeleteAsync_Ok_ReturnsSuccess()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.OK, """{"errors":[]}"""));

        // Act
        var result = await gateway.DeleteAsync("api/x/1", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative: a delete that is refused with a conflict carries the code.</summary>
    [Fact]
    public async Task DeleteAsync_Conflict_ReturnsTheCode()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.Conflict, """{"errors":["room.has_bookings"]}"""));

        // Act
        var result = await gateway.DeleteAsync("api/x/1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("room.has_bookings", result.FirstError?.Code);
    }

    #endregion

    #region Failures of the connection

    /// <summary>Negative: 401 is the refused token, whatever the body says.</summary>
    [Fact]
    public async Task GetAsync_Unauthorized_ReturnsApiUnauthorized()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Text(HttpStatusCode.Unauthorized, "nope"));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unauthorized, result.FirstError?.Code);
    }

    /// <summary>Equivalence: every 5xx is the API being unavailable.</summary>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetAsync_ServerError_ReturnsApiUnavailable(HttpStatusCode status)
    {
        // Arrange
        var gateway = Gateway(StubHandler.Text(status, "boom"));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    /// <summary>Error handling: a broken network is the API being unavailable.</summary>
    [Fact]
    public async Task GetAsync_NetworkFailure_ReturnsApiUnavailable()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Throws(new HttpRequestException("no route")));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    /// <summary>Timeout: the timeout of the HttpClient, which is not a cancel by the caller, is the API being unavailable.</summary>
    [Fact]
    public async Task GetAsync_TimeoutOfTheClient_ReturnsApiUnavailable()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Throws(new TaskCanceledException("timed out")));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    /// <summary>Asynchronous behaviour: when the caller cancels, the cancel is passed on and not turned into an error.</summary>
    [Fact]
    public async Task GetAsync_CallerCancels_ThrowsOperationCanceled()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}"""));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // Act and assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gateway.GetAsync<Sample>("api/x", cancelled.Token));
    }

    #endregion

    #region Answers that cannot be used

    /// <summary>Negative: an answer that is not JSON is unexpected.</summary>
    [Fact]
    public async Task GetAsync_OkButNotJson_ReturnsUnexpectedResponse()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Text(HttpStatusCode.OK, "<html>proxy error page</html>"));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.UnexpectedResponse, result.FirstError?.Code);
    }

    /// <summary>Negative: broken JSON is unexpected.</summary>
    [Fact]
    public async Task GetAsync_OkButBrokenJson_ReturnsUnexpectedResponse()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.UnexpectedResponse, result.FirstError?.Code);
    }

    /// <summary>Edge: a success without a value is unexpected, not a null result.</summary>
    [Fact]
    public async Task GetAsync_OkWithoutValue_ReturnsUnexpectedResponse()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.OK, """{"value":null,"errors":[]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.UnexpectedResponse, result.FirstError?.Code);
    }

    /// <summary>Edge: a failure status without any code is unexpected.</summary>
    [Fact]
    public async Task GetAsync_NotFoundWithoutCodes_ReturnsUnexpectedResponse()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.NotFound, """{"value":null,"errors":[]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.UnexpectedResponse, result.FirstError?.Code);
    }

    #endregion

    #region Request

    /// <summary>Positive: a body goes out as camelCase JSON with the right method and path.</summary>
    [Fact]
    public async Task PostAsync_Body_IsSentAsCamelCaseJson()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":2},"errors":[]}""");
        var gateway = Gateway(handler);

        // Act
        await gateway.PostAsync<Sample>("api/x", new Sample(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/x", handler.PathAndQuery);
        Assert.Equal("""{"n":2}""", handler.Body);
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler) => new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });

    private sealed record Sample(int N);

    #endregion
}
