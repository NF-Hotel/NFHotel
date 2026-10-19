namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The registration of the customer's booking flow, so <c>Program.cs</c> has only one call to know.
/// </summary>
/// <remarks>
/// Conventions.md: <c>Program.cs</c> is the composition root, and no component news up a
/// service. The feature therefore describes its own dependencies here, but <i>registers</i>
/// them only when the composition root calls <see cref="AddBookingFeature"/> — the file makes
/// no decision about the setup of the host.
/// </remarks>
public static class BookingFeatureRegistration
{
    /// <summary>
    /// Registers the viewmodels of the booking flow.
    /// </summary>
    /// <param name="services">The service collection from the composition root.</param>
    /// <returns>The same collection, so the call can be chained.</returns>
    /// <remarks>
    /// <c>Scoped</c>, like the other viewmodels: in Blazor Server a scope is one
    /// SignalR circuit — that is, one customer's flow in one tab. That is exactly the lifetime a
    /// half-filled booking should have, and it is also the reason the state survives
    /// when the customer goes back a step.
    /// </remarks>
    public static IServiceCollection AddBookingFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<BookingFlowViewModel>();
        services.AddScoped<BookingReceiptViewModel>();

        return services;
    }
}
