namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Registreringen af kundens bookingflow, så <c>Program.cs</c> kun har ét kald at kende.
/// </summary>
/// <remarks>
/// Conventions.md: <c>Program.cs</c> er composition root, og ingen komponent new'er en
/// service. Featuren beskriver derfor selv sine afhængigheder her, men <i>registrerer</i>
/// dem først når composition root kalder <see cref="AddBookingFeature"/> — filen tager
/// ingen beslutning om værtens opsætning.
/// </remarks>
public static class BookingFeatureRegistration
{
    /// <summary>
    /// Registrerer bookingflowets viewmodels.
    /// </summary>
    /// <param name="services">Servicesamlingen fra composition root.</param>
    /// <returns>Samme samling, så kaldet kan kædes.</returns>
    /// <remarks>
    /// <c>Scoped</c>, som de øvrige viewmodels: i Blazor Server er en scope ét
    /// SignalR-kredsløb — altså én kundes flow i én fane. Det er præcis den levetid en
    /// halvudfyldt booking skal have, og det er også grunden til at tilstanden overlever,
    /// når kunden går et trin tilbage.
    /// </remarks>
    public static IServiceCollection AddBookingFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<BookingFlowViewModel>();
        services.AddScoped<BookingReceiptViewModel>();

        return services;
    }
}