using NFHotel.Web.Features.Home.Neighbourhood;

namespace NFHotel.Web.Features.Home;

/// <summary>
/// The registration of the front page, so <c>Program.cs</c> has only one call to know.
/// </summary>
/// <remarks>
/// Conventions.md: <c>Program.cs</c> is the composition root, and no component news up a service. The
/// feature describes its own dependencies here, but <i>registers</i> them only when the composition root
/// calls <see cref="AddHomeFeature"/>.
/// </remarks>
public static class HomeFeatureRegistration
{
    /// <summary>
    /// Registers the viewmodel of the map of the neighbourhood.
    /// </summary>
    /// <param name="services">The service collection from the composition root.</param>
    /// <returns>The same collection, so the call can be chained.</returns>
    /// <remarks>
    /// <c>Scoped</c>, like the other viewmodels: one circuit, one guest looking at the map.
    /// </remarks>
    public static IServiceCollection AddHomeFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<NeighbourhoodMapViewModel>();

        return services;
    }
}
