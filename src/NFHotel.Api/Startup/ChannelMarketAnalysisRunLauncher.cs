using System.Threading.Channels;

using NFHotel.Application.Ports;

namespace NFHotel.Api.Startup;

/// <summary>
/// Hands a market analysis run to the <see cref="MarketAnalysisRunWorker"/> through a queue, so that a start returns at once
/// (MIL-026). The queue is unbounded because the tracker lets only one run be active, so it never holds more than one.
/// </summary>
public sealed class ChannelMarketAnalysisRunLauncher : IMarketAnalysisRunLauncher
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>The runs that wait to be worked on; read by the worker.</summary>
    public ChannelReader<Guid> Reader => _queue.Reader;

    /// <inheritdoc />
    public void Launch(Guid runId) => _queue.Writer.TryWrite(runId);
}
