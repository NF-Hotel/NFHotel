using NFHotel.Web.Features.Admin.Bookings;
using NFHotel.Web.Features.Admin.Dashboard;
using NFHotel.Web.Features.Admin.Guests;
using NFHotel.Web.Features.Admin.Rooms;

namespace NFHotel.Web.Features.Admin;

/// <summary>
/// Registrerer admin-områdets viewmodels i containeren.
/// </summary>
/// <remarks>
/// Conventions.md holder <c>Program.cs</c> som composition root; denne klasse flytter ikke
/// beslutningen væk derfra, men samler admin-featurens egne typer ét sted, så en ny skærm
/// tilføjes i den mappe den hører til frem for i opstartsfilen.
/// <para>
/// Alle viewmodels er <b>Scoped</b>. I Blazor Server er en scope ét SignalR-kredsløb —
/// altså én brugers session i én fane — og det er præcis den levetid skærmens tilstand
/// (filtre, åbne formularer, valgt række) skal have. Databaseadgangen er kortere end det:
/// hvert use case-kald henter sin egen scope gennem <c>IUseCaseRunner</c>
/// (Architecture.md afsnit 8), så en lang session aldrig deler én <c>DbContext</c>.
/// </para>
/// </remarks>
public static class AdminFeatureRegistration
{
    /// <summary>
    /// Tilføjer admin-områdets viewmodels.
    /// </summary>
    /// <param name="services">Containeren fra <c>Program.cs</c>.</param>
    /// <returns>Samme container, så kaldet kan kædes.</returns>
    public static IServiceCollection AddAdminFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AdminDashboardViewModel>();

        services.AddScoped<BookingListViewModel>();
        services.AddScoped<BookingFormViewModel>();

        services.AddScoped<RoomListViewModel>();
        services.AddScoped<RoomFormViewModel>();

        services.AddScoped<GuestListViewModel>();

        return services;
    }
}