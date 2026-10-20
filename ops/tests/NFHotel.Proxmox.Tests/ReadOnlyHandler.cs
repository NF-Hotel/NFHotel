namespace NFHotel.Proxmox.Tests;

/// <summary>
/// Refuses every request that is not a GET, so a test can never change the
/// Proxmox server even by mistake.
/// </summary>
public sealed class ReadOnlyHandler : DelegatingHandler
{
    public ReadOnlyHandler(HttpMessageHandler inner)
        : base(inner)
    {
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
        {
            throw new InvalidOperationException(
                $"Read-only guard: {request.Method} is not allowed.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
