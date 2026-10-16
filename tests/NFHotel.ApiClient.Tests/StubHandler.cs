using System.Net;
using System.Text;

namespace NFHotel.ApiClient.Tests;

/// <summary>A fake HTTP handler: remembers the calls it got and answers what the test asked for.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;

    private StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        _respond = respond;
    }

    /// <summary>The method of the last call.</summary>
    public HttpMethod? Method { get; private set; }

    /// <summary>The address of the last call, relative to the base address.</summary>
    public string? PathAndQuery { get; private set; }

    /// <summary>The body of the last call, or <c>null</c> when it had none.</summary>
    public string? Body { get; private set; }

    /// <summary>The staff token header of the last call, or <c>null</c> when it had none.</summary>
    public string? StaffToken { get; private set; }

    /// <summary>The number of calls received.</summary>
    public int Calls { get; private set; }

    /// <summary>Answers with a status and a JSON body.</summary>
    /// <param name="status">The status.</param>
    /// <param name="json">The body.</param>
    /// <returns>The handler.</returns>
    public static StubHandler Json(HttpStatusCode status, string json) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));

    /// <summary>Answers with a status and a body that is not JSON.</summary>
    /// <param name="status">The status.</param>
    /// <param name="text">The body.</param>
    /// <returns>The handler.</returns>
    public static StubHandler Text(HttpStatusCode status, string text) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/html"),
        }));

    /// <summary>Throws instead of answering, as a broken network or a timeout does.</summary>
    /// <param name="exception">What to throw.</param>
    /// <returns>The handler.</returns>
    public static StubHandler Throws(Exception exception) => new(_ => throw exception);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Method = request.Method;
        PathAndQuery = request.RequestUri?.PathAndQuery;
        StaffToken = request.Headers.TryGetValues("X-Staff-Token", out var values) ? string.Join(",", values) : null;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await _respond(request).ConfigureAwait(false);
    }
}