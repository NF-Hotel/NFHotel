using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Api.Startup;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NFHotel.Api.Tests;

/// <summary>
/// The timed job of the API that deletes the lapsed booking requests (MIL-088 criterion 5, ADR-0052 decision 7): it runs
/// at the start and then every ten minutes, each run in a scope of its own, and a run that fails never stops the API.
/// </summary>
public sealed class BookingRequestCleanupWorkerTests
{
    private readonly ServiceWorld _world = new();

    /// <summary>Positive (criterion 5): a run deletes the request whose link lapsed and keeps the one whose link works.</summary>
    [Fact]
    public async Task RunOnceAsync_LapsedAndOpenRequests_DeletesTheLapsedOne()
    {
        // Arrange
        DateTimeOffset now = _world.Clock.UtcNow;
        BookingRequest lapsed = Request("anna@example.com", now.AddMinutes(-40));
        BookingRequest open = Request("eve@example.com", now.AddMinutes(-10));
        _world.BookingRequests.Add(lapsed);
        _world.BookingRequests.Add(open);
        BookingRequestCleanupWorker worker = Worker(services => services.AddScoped<IBookingRequestCleanup>(_ => _world.BookingRequestCleanup()));

        // Act
        await worker.RunOnceAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(open, Assert.Single(_world.BookingRequests.Items));
    }

    /// <summary>Negative: a run whose clean-up fails, as when the database is down, is logged and does not throw.</summary>
    [Fact]
    public async Task RunOnceAsync_CleanupFails_DoesNotThrow()
    {
        // Arrange
        IBookingRequestCleanup cleanup = Substitute.For<IBookingRequestCleanup>();
        cleanup.RunAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("The database is down."));
        BookingRequestCleanupWorker worker = Worker(services => services.AddScoped(_ => cleanup));

        // Act
        Exception? thrown = await Record.ExceptionAsync(() => worker.RunOnceAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(thrown);
        await cleanup.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a run that is cancelled because the API stops throws the cancellation, so the worker ends.</summary>
    [Fact]
    public async Task RunOnceAsync_ApiStops_ThrowsTheCancellation()
    {
        // Arrange
        using CancellationTokenSource stopping = new();
        await stopping.CancelAsync();
        IBookingRequestCleanup cleanup = Substitute.For<IBookingRequestCleanup>();
        cleanup.RunAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(stopping.Token));
        BookingRequestCleanupWorker worker = Worker(services => services.AddScoped(_ => cleanup));

        // Act
        Exception? thrown = await Record.ExceptionAsync(() => worker.RunOnceAsync(stopping.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(thrown, exactMatch: false);
    }

    /// <summary>
    /// Negative: a cancellation that does not come from the stop of the API, as a timeout of the database, is logged like
    /// any other failure and does not throw, so the next run tries again.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_CancelledWhileTheApiRuns_DoesNotThrow()
    {
        // Arrange
        IBookingRequestCleanup cleanup = Substitute.For<IBookingRequestCleanup>();
        cleanup.RunAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException("A timeout of the database."));
        BookingRequestCleanupWorker worker = Worker(services => services.AddScoped(_ => cleanup));

        // Act
        Exception? thrown = await Record.ExceptionAsync(() => worker.RunOnceAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(thrown);
    }

    /// <summary>Positive (ADR-0052 decision 7): the worker runs the clean-up at its start, without waiting ten minutes.</summary>
    [Fact]
    public async Task StartAsync_Start_RunsTheCleanupAtOnce()
    {
        // Arrange
        TaskCompletionSource ran = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IBookingRequestCleanup cleanup = Substitute.For<IBookingRequestCleanup>();
        cleanup.RunAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            ran.TrySetResult();
            return Task.FromResult((0, 0));
        });
        BookingRequestCleanupWorker worker = Worker(services => services.AddScoped(_ => cleanup));

        // Act
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await ran.Task.WaitAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        await cleanup.Received(1).RunAsync(Arg.Any<CancellationToken>());
        Assert.Equal(TimeSpan.FromMinutes(10), BookingRequestCleanupWorker.Interval);
    }

    private static BookingRequestCleanupWorker Worker(Action<IServiceCollection> register)
    {
        ServiceCollection services = new();
        register(services);
        return new BookingRequestCleanupWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingRequestCleanupWorker>.Instance);
    }

    // A request made at a time, whose link lapses 30 minutes later.
    private static BookingRequest Request(string email, DateTimeOffset madeAt)
    {
        Guest guest = Guest.Create("Anna", "Jensen", email, "+4512345678", "Denmark", null);
        return BookingRequest.Create(
            guest,
            new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3)),
            RoomSize.Double,
            1,
            BookingRequestTestData.HashOf("token of " + email),
            BookingRequestTestData.KeyOf(email),
            madeAt);
    }
}
