using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Common;

/// <summary>
/// A component whose asynchronous calls stop when the component goes away (MIL-014). A call to a use case
/// that waits for the database or an HTTP service receives <see cref="ComponentToken"/>, so a closed page
/// or a lost circuit does not leave work running for nobody.
/// </summary>
public abstract class CancellableComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _disposed = new();

    /// <summary>A token that is cancelled when the component is disposed.</summary>
    protected CancellationToken ComponentToken => _disposed.Token;

    /// <summary>Cancels the pending calls of the component and releases the token source.</summary>
    public void Dispose()
    {
        _disposed.Cancel();
        _disposed.Dispose();
        GC.SuppressFinalize(this);
    }
}