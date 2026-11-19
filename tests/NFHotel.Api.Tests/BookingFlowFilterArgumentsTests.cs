using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Api.Security;
using NFHotel.Application.Ports;

using NSubstitute;

namespace NFHotel.Api.Tests;

/// <summary>
/// The dependencies of the filters of the booking flow (MIL-087, review RC-428 F7): each filter refuses a missing one
/// when it is made, as <see cref="BookingFlowEvents"/> does, instead of failing on the first request it filters.
/// </summary>
public sealed class BookingFlowFilterArgumentsTests
{
    /// <summary>Negative: the filter of the limits needs the limits, the address of the caller and the events.</summary>
    [Fact]
    public void SenderLimitFilter_MissingDependency_Throws()
    {
        // Arrange
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        ISenderLimits limits = Substitute.For<ISenderLimits>();
        ICallerAddress caller = Substitute.For<ICallerAddress>();
        BookingFlowEvents events = Events(provider);

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new SenderLimitFilter(SenderLimitKind.Search, null!, caller, events));
        Assert.Throws<ArgumentNullException>(() => new SenderLimitFilter(SenderLimitKind.Search, limits, null!, events));
        Assert.Throws<ArgumentNullException>(() => new SenderLimitFilter(SenderLimitKind.Search, limits, caller, null!));
    }

    /// <summary>Negative: the filter of the events needs the address of the caller and the events.</summary>
    [Fact]
    public void BookingFlowEventFilter_MissingDependency_Throws()
    {
        // Arrange
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        ICallerAddress caller = Substitute.For<ICallerAddress>();
        BookingFlowEvents events = Events(provider);

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new BookingFlowEventFilter(BookingFlowStep.PublicBooking, null!, events));
        Assert.Throws<ArgumentNullException>(() => new BookingFlowEventFilter(BookingFlowStep.PublicBooking, caller, null!));
    }

    private static BookingFlowEvents Events(IServiceProvider provider) =>
        new(NullLoggerFactory.Instance, new SecurityMetrics(provider.GetRequiredService<IMeterFactory>()));
}
