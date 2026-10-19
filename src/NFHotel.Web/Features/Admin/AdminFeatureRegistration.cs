using NFHotel.Web.Features.Admin.Benchmark;
using NFHotel.Web.Features.Admin.Bookings;
using NFHotel.Web.Features.Admin.Dashboard;
using NFHotel.Web.Features.Admin.Guests;
using NFHotel.Web.Features.Admin.Login;
using NFHotel.Web.Features.Admin.Rooms;

namespace NFHotel.Web.Features.Admin;

/// <summary>
/// Registers the viewmodels of the admin area in the container.
/// </summary>
/// <remarks>
/// Conventions.md keeps <c>Program.cs</c> as the composition root; this class does not move
/// the decision away from there, but gathers the types of the admin feature itself in one place, so a new screen
/// is added in the folder it belongs to rather than in the start-up file.
/// <para>
/// All viewmodels are <b>Scoped</b>. In Blazor Server a scope is one SignalR circuit —
/// that is, one user's session in one tab — and that is exactly the lifetime the state of the screen
/// (filters, open forms, selected row) should have. Database access is shorter than that:
/// each use case call fetches its own scope through <c>IUseCaseRunner</c>
/// (Architecture.md section 8), so a long session never shares one <c>DbContext</c>.
/// </para>
/// </remarks>
public static class AdminFeatureRegistration
{
    /// <summary>
    /// Adds the viewmodels of the admin area.
    /// </summary>
    /// <param name="services">The container from <c>Program.cs</c>.</param>
    /// <returns>The same container, so the call can be chained.</returns>
    public static IServiceCollection AddAdminFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AdminDashboardViewModel>();

        services.AddScoped<BookingListViewModel>();
        services.AddScoped<BookingFormViewModel>();

        services.AddScoped<RoomListViewModel>();
        services.AddScoped<RoomFormViewModel>();

        services.AddScoped<GuestListViewModel>();

        services.AddScoped<BenchmarkRunListViewModel>();
        services.AddScoped<BenchmarkStartViewModel>();

        services.AddScoped<LoginViewModel>();

        return services;
    }
}
