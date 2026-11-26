using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NFHotel.Application.UseCases;
using NFHotel.External.Telemetry;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The limit of the open circuits of one caller (ADR-0051 decision 8; SAR-002 finding BF-02 and abuse case 11): the web
/// application counts the open requests to <c>/_blazor</c> per caller, an IPv6 address by its first 64 bits, and answers
/// the request over <see cref="CircuitLimit.OpenPerCaller"/> with 429 and the security event <c>limit_refused</c> of the
/// limit <c>circuits</c>. Another caller and every other path pass, and a request that ends, also with a fault, gives
/// its place back.
/// </summary>
public sealed class CircuitLimitTests
{
    /// <summary>nginx in the Docker network, which writes the address of the browser into the header.</summary>
    private const string Nginx = "172.18.0.5";

    /// <summary>The name on the wire, written out, so the test pins it and not only the constant.</summary>
    private const string Header = "X-NFHotel-Client-Address";

    /// <summary>
    /// The meter of the security events, written out, so a change of the constant of the web application fails here,
    /// as one of the API fails in <c>SenderLimitApiTests</c>, and not only in the alert <c>BookingFlowRefusals</c>.
    /// </summary>
    private const string SecurityMeter = "NFHotel.Security";

    /// <summary>The counter of the security events of the API, <c>nfhotel_security_events_total</c> in Prometheus.</summary>
    private const string EventsCounter = "nfhotel.security.events";

    private const string Guest = "203.0.113.7";

    #region The count

    /// <summary>Boundary: a caller gets 50 open requests and not the 51st, the number of ADR-0051 decision 8.</summary>
    [Fact]
    public void TryOpen_UpToTheLimit_CountsFiftyAndRefusesTheNext()
    {
        // Arrange
        CircuitLimit limit = new();

        // Act
        bool[] opened = [.. Enumerable.Range(0, 51).Select(_ => limit.TryOpen(Guest))];

        // Assert
        Assert.Equal(50, CircuitLimit.OpenPerCaller);
        Assert.Equal(50, opened.Count(open => open));
        Assert.False(opened[^1]);
        Assert.Equal(50, limit.OpenOf(Guest));
    }

    /// <summary>Positive: a closed request gives its place back, and a caller without an open request is forgotten.</summary>
    [Fact]
    public void Close_OpenRequests_GiveTheirPlacesBackAndForgetTheCaller()
    {
        // Arrange
        CircuitLimit limit = new();
        Open(limit, Guest, CircuitLimit.OpenPerCaller);

        // Act
        limit.Close(Guest);
        bool reopened = limit.TryOpen(Guest);
        for (int open = 0; open < CircuitLimit.OpenPerCaller; open++)
        {
            limit.Close(Guest);
        }

        // Assert
        Assert.True(reopened);
        Assert.Equal(0, limit.OpenOf(Guest));
        Assert.Equal(0, limit.CallerCount);
    }

    /// <summary>Security: many requests at once still get 50 places per caller, and each caller has a count of its own.</summary>
    [Fact]
    public void TryOpen_ManyAtOnce_CountsExactlyTheLimitOfEachCaller()
    {
        // Arrange
        CircuitLimit limit = new();
        string[] callers = [Guest, "2001:db8:1:2::/64"];
        ConcurrentQueue<string> opened = new();

        // Act
        Parallel.For(0, 400, attempt =>
        {
            string caller = callers[attempt % callers.Length];
            if (limit.TryOpen(caller))
            {
                opened.Enqueue(caller);
            }
        });

        // Assert
        Assert.Equal(50, opened.Count(caller => caller == callers[0]));
        Assert.Equal(50, opened.Count(caller => caller == callers[1]));
        Assert.Equal(2, limit.CallerCount);
    }

    /// <summary>Negative: closing a request that was never counted is a fault, and every member refuses a null.</summary>
    [Fact]
    public void CloseAndNull_RequestNeverCountedOrNull_Throw()
    {
        // Arrange
        CircuitLimit limit = new();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<InvalidOperationException>(() => limit.Close(Guest));
        Assert.Throws<ArgumentNullException>(() => limit.TryOpen(null!));
        Assert.Throws<ArgumentNullException>(() => limit.Close(null!));
        Assert.Throws<ArgumentNullException>(() => limit.OpenOf(null!));
        Assert.Throws<ArgumentNullException>(() => CircuitLimitMiddleware.UseCircuitLimit(null!));
    }

    #endregion

    #region The step of the pipeline

    /// <summary>
    /// Security: with 50 circuits of one caller open, the next request to <c>/_blazor</c> gets 429 and never reaches the
    /// hub; the event names the limit and the address that nginx saw, and the counter counts it once.
    /// </summary>
    [Fact]
    public async Task UseCircuitLimit_CallerWithFiftyOpen_RefusesTheNextWith429AndTheEvent()
    {
        // Arrange
        await using Pipeline pipeline = new();
        Task[] circuits = pipeline.OpenCircuits(Guest, CircuitLimit.OpenPerCaller);

        // Act
        DefaultHttpContext refused = await pipeline.SendAsync("/_blazor?id=over", Guest);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, refused.Response.StatusCode);
        Assert.Equal(CircuitLimit.OpenPerCaller, pipeline.Reached);
        LogEntry entry = Assert.Single(pipeline.Entries);
        Assert.Equal(
            ("NFHotel.Security", LogLevel.Warning, 8604, "limit_refused circuits 203.0.113.7"),
            (entry.Category, entry.Level, entry.EventId, entry.Message));
        Assert.Equal(["limit_refused/circuits"], pipeline.Counted);
        await pipeline.EndCircuitsAsync(circuits);
    }

    /// <summary>
    /// Equivalence: the addresses of one IPv6 network of /64 are one caller, and the path counts in any letter case, as
    /// the routes of the application match it; another network has its own places.
    /// </summary>
    [Fact]
    public async Task UseCircuitLimit_OneIpv6NetworkInAnyLetterCase_SharesTheCount()
    {
        // Arrange
        await using Pipeline pipeline = new();
        Task[] circuits = pipeline.OpenCircuits("2001:db8:1:2::a", CircuitLimit.OpenPerCaller);

        // Act
        DefaultHttpContext sameNetwork = await pipeline.SendAsync("/_blazor/negotiate", "2001:db8:1:2:ffff::b");
        DefaultHttpContext upperCase = await pipeline.SendAsync("/_BLAZOR/negotiate", "2001:db8:1:2::c");
        DefaultHttpContext otherNetwork = await pipeline.SendAsync("/_blazor/negotiate", "2001:db8:1:3::a");

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, sameNetwork.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, upperCase.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, otherNetwork.Response.StatusCode);
        await pipeline.EndCircuitsAsync(circuits);
    }

    /// <summary>
    /// Positive: while a caller has its 50 circuits, its other requests pass, a path that only begins like the one of the
    /// circuits is another path, and another caller opens a circuit; none of them is counted.
    /// </summary>
    /// <param name="target">The path and query of the request.</param>
    /// <param name="address">The address that nginx saw.</param>
    [Theory]
    [InlineData("/book", Guest)]
    [InlineData("/_framework/blazor.web.js", Guest)]
    [InlineData("/_blazorx", Guest)]
    [InlineData("/_blazor/negotiate", "203.0.113.8")]
    public async Task UseCircuitLimit_OtherPathOrOtherCaller_Passes(string target, string address)
    {
        // Arrange
        await using Pipeline pipeline = new();
        Task[] circuits = pipeline.OpenCircuits(Guest, CircuitLimit.OpenPerCaller);

        // Act
        DefaultHttpContext passed = await pipeline.SendAsync(target, address);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, passed.Response.StatusCode);
        Assert.Equal(CircuitLimit.OpenPerCaller + 1, pipeline.Reached);
        Assert.Equal(CircuitLimit.OpenPerCaller, pipeline.Limit.OpenOf(Guest));
        Assert.Empty(pipeline.Entries);
        await pipeline.EndCircuitsAsync(circuits);
    }

    /// <summary>
    /// Positive: a request that ends, at once as the negotiation does or with a fault of the hub, gives its place back,
    /// and once the circuits end the caller is forgotten.
    /// </summary>
    [Fact]
    public async Task UseCircuitLimit_RequestThatEndsOrFails_GivesItsPlaceBack()
    {
        // Arrange
        await using Pipeline pipeline = new();
        Task[] circuits = pipeline.OpenCircuits(Guest, CircuitLimit.OpenPerCaller - 1);

        // Act
        DefaultHttpContext negotiated = await pipeline.SendAsync("/_blazor/negotiate", Guest);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.SendAsync("/_blazor/negotiate?fault=1", Guest));
        DefaultHttpContext last = await pipeline.SendAsync("/_blazor/negotiate", Guest);
        int openAfterTheRequests = pipeline.Limit.OpenOf(Guest);
        await pipeline.EndCircuitsAsync(circuits);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, negotiated.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, last.Response.StatusCode);
        Assert.Equal(CircuitLimit.OpenPerCaller - 1, openAfterTheRequests);
        Assert.Equal(0, pipeline.Limit.CallerCount);
        Assert.Empty(pipeline.Entries);
    }

    #endregion

    #region The web application

    /// <summary>
    /// Security: the step is in the pipeline of the web application, before the hub. With every place of a caller taken,
    /// the negotiation of a circuit gets 429 while a page still opens, and once a place is free the negotiation passes. On
    /// the in-memory test server a request has no address, so its caller is <see cref="CallerAddressKey.Unknown"/>.
    /// </summary>
    [Fact]
    public async Task WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage()
    {
        // Arrange
        using WebFactory factory = new();
        using HttpClient browser = factory.CreateBrowser();
        CircuitLimit limit = factory.Services.GetRequiredService<CircuitLimit>();
        Open(limit, CallerAddressKey.Unknown, CircuitLimit.OpenPerCaller);
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        // Act
        using HttpResponseMessage refused = await browser.PostAsync("/_blazor/negotiate?negotiateVersion=1", null, cancellation);
        using HttpResponseMessage page = await browser.GetAsync("/book", cancellation);
        limit.Close(CallerAddressKey.Unknown);
        using HttpResponseMessage negotiated = await browser.PostAsync("/_blazor/negotiate?negotiateVersion=1", null, cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, negotiated.StatusCode);
    }

    /// <summary>
    /// Positive (ADR-0051 decision 8): the meter of the refusals is one that the telemetry of the services sends on, so
    /// the alert <c>BookingFlowRefusals</c> counts them with the refusals of the API.
    /// </summary>
    [Fact]
    public void Telemetry_Meters_IncludeTheMeterOfTheRefusals()
    {
        // Act and assert
        Assert.Contains(CircuitLimitMiddleware.MeterName, TelemetryRegistration.Meters);
    }

    #endregion

    #region Helpers

    private static void Open(CircuitLimit limit, string caller, int count)
    {
        for (int open = 0; open < count; open++)
        {
            Assert.True(limit.TryOpen(caller));
        }
    }

    /// <summary>One entry of the log: its category, level, event id and message.</summary>
    /// <param name="Category">The category of the logger.</param>
    /// <param name="Level">The level.</param>
    /// <param name="EventId">The id of the event.</param>
    /// <param name="Message">The formatted message.</param>
    private sealed record LogEntry(string Category, LogLevel Level, int EventId, string Message);

    /// <summary>
    /// The step alone in a pipeline whose end stands in for the hub: a request with an <c>id</c>, the web socket of a
    /// circuit, stays open until <see cref="EndCircuitsAsync"/>; a request with <c>fault</c> fails; any other ends at
    /// once. The log and the counter of the security events are recorded.
    /// </summary>
    private sealed class Pipeline : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly MeterListener _listener;
        private readonly RequestDelegate _app;
        private readonly TaskCompletionSource _end = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<LogEntry> _entries = new();
        private readonly ConcurrentQueue<string> _counted = new();
        private int _reached;

        public Pipeline()
        {
            _services = new ServiceCollection()
                .AddSingleton<CircuitLimit>()
                .AddLogging(logging => logging.AddProvider(new Recorder(_entries)))
                .AddMetrics()
                .BuildServiceProvider();
            _listener = Listen(_services.GetRequiredService<IMeterFactory>(), _counted);

            ApplicationBuilder app = new(_services);
            app.UseCircuitLimit();
            app.Run(async context =>
            {
                Interlocked.Increment(ref _reached);
                if (context.Request.Query.ContainsKey("fault"))
                {
                    throw new InvalidOperationException("The hub failed.");
                }

                if (context.Request.Query.ContainsKey("id"))
                {
                    await _end.Task;
                }
            });
            _app = app.Build();
        }

        public CircuitLimit Limit => _services.GetRequiredService<CircuitLimit>();

        public int Reached => Volatile.Read(ref _reached);

        public IReadOnlyList<LogEntry> Entries => [.. _entries];

        public IReadOnlyList<string> Counted => [.. _counted];

        // Each call runs the step up to the open web socket, so the count is taken when this returns.
        public Task[] OpenCircuits(string address, int count) =>
            [.. Enumerable.Range(0, count).Select(circuit => _app(Request($"{CircuitLimitMiddleware.CircuitPath}?id={circuit}", address)))];

        public async Task<DefaultHttpContext> SendAsync(string target, string address)
        {
            DefaultHttpContext context = Request(target, address);
            await _app(context);
            return context;
        }

        public async Task EndCircuitsAsync(Task[] circuits)
        {
            _end.TrySetResult();
            await Task.WhenAll(circuits);
        }

        public async ValueTask DisposeAsync()
        {
            _end.TrySetResult();
            _listener.Dispose();
            await _services.DisposeAsync();
        }

        private static DefaultHttpContext Request(string target, string address)
        {
            string[] parts = target.Split('?', 2);
            DefaultHttpContext context = new();
            context.Connection.RemoteIpAddress = IPAddress.Parse(Nginx);
            context.Request.Headers[Header] = address;
            context.Request.Path = parts[0];
            context.Request.QueryString = parts.Length == 2 ? new QueryString("?" + parts[1]) : QueryString.Empty;
            return context;
        }

        private static MeterListener Listen(IMeterFactory meters, ConcurrentQueue<string> counted)
        {
            MeterListener listener = new()
            {
                InstrumentPublished = (instrument, owner) =>
                {
                    if (instrument.Meter.Scope == meters && instrument.Meter.Name == SecurityMeter && instrument.Name == EventsCounter)
                    {
                        owner.EnableMeasurementEvents(instrument);
                    }
                },
            };
            listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                Dictionary<string, string?> labels = [];
                foreach (KeyValuePair<string, object?> tag in tags)
                {
                    labels[tag.Key] = tag.Value?.ToString();
                }

                for (long count = 0; count < value; count++)
                {
                    counted.Enqueue($"{labels.GetValueOrDefault("kind")}/{labels.GetValueOrDefault("limit")}");
                }
            });
            listener.Start();
            return listener;
        }
    }

    /// <summary>Keeps every entry of the log of the pipeline.</summary>
    /// <param name="entries">Where the entries go.</param>
    private sealed class Recorder(ConcurrentQueue<LogEntry> entries) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Logger(categoryName, entries);

        public void Dispose()
        {
            // Nothing to release.
        }

        private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                entries.Enqueue(new LogEntry(category, logLevel, eventId.Id, formatter(state, exception)));
            }
        }
    }

    #endregion
}
