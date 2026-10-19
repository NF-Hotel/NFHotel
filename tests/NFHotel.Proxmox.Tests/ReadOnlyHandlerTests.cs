namespace NFHotel.Proxmox.Tests;

/// <summary>Proves the HTTP guard refuses every method except GET.</summary>
public sealed class ReadOnlyHandlerTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task SendAsync_NonGetMethod_ThrowsInvalidOperation(string method)
    {
        using var client = new HttpClient(new ReadOnlyHandler(new OkHandler()));
        using var request = new HttpRequestMessage(
            new HttpMethod(method), "https://pve.invalid/api2/json/version");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAsync_GetMethod_PassesThrough()
    {
        using var client = new HttpClient(new ReadOnlyHandler(new OkHandler()));

        using var response = await client.GetAsync("https://pve.invalid/api2/json/version", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
