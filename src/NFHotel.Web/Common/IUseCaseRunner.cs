using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// Kører ét use case-kald på <typeparamref name="TService"/> i sin egen DI-scope og sikrer
/// at kaldet altid ender i et <see cref="Result"/>.
/// </summary>
/// <typeparam name="TService">Application-servicen, fx <c>IBookingService</c>.</typeparam>
/// <remarks>
/// Typen løser to ting på én gang, og begge er krav til Fase 2.
/// <para>
/// <b>1. Én scope pr. use case.</b> Architecture.md afsnit 8: <i>"Blazor Server skal give
/// hver side sin egen DI-scope. Gør den ikke det, får en bruger én <c>DbContext</c> for
/// hele sin session, og to samtidige komponenthændelser kaster."</i> I Blazor Server lever
/// en DI-scope lige så længe som SignalR-kredsløbet — potentielt timer.
/// <c>AddInfrastructure</c> registrerer derfor <c>HotelDbContext</c> som <c>Scoped</c> oven
/// på en <c>IDbContextFactory</c>, netop for at Web kan hente en frisk kontekst pr. use
/// case. Denne type er det sted Web faktisk gør det: ét kald = én scope = én
/// <c>DbContext</c>, som repositories og <c>IUnitOfWork</c> deler indbyrdes, men ikke deler
/// med et andet igangværende kald. Uden den ville to hurtige klik på samme side ramme samme
/// kontekst midt i et <c>await</c>.
/// </para>
/// <para>
/// <b>2. Ét sted at fange det uventede.</b> Forventede regelbrud kommer allerede tilbage
/// som <see cref="Result"/>. En nede database gør det ikke — den kaster. Runneren fanger
/// den, logger den tekniske detalje centralt og returnerer
/// <see cref="WebErrorCodes.Unexpected"/>, så skærmen kan vise en neutral besked med den
/// samme kode-sti som alt andet i stedet for at rive kredsløbet ned.
/// </para>
/// <para>
/// ViewModels afhænger af denne type frem for af <c>IServiceScopeFactory</c>, så de kun
/// nævner Application-interfaces og ikke selv kender til DI-mekanikken (B-10).
/// </para>
/// </remarks>
public interface IUseCaseRunner<TService>
    where TService : notnull
{
    /// <summary>
    /// Kører et asynkront use case der returnerer en værdi.
    /// </summary>
    /// <typeparam name="TValue">Use casets værditype.</typeparam>
    /// <param name="useCase">Kaldet der skal udføres på servicen.</param>
    /// <param name="cancellationToken">Annulleringstoken, sendes videre til servicen.</param>
    /// <returns>Servicens resultat, eller en fejl med <see cref="WebErrorCodes.Unexpected"/>.</returns>
    Task<Result<TValue>> RunAsync<TValue>(
        Func<TService, CancellationToken, Task<Result<TValue>>> useCase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kører et asynkront use case uden returværdi.
    /// </summary>
    /// <param name="useCase">Kaldet der skal udføres på servicen.</param>
    /// <param name="cancellationToken">Annulleringstoken, sendes videre til servicen.</param>
    /// <returns>Servicens resultat, eller en fejl med <see cref="WebErrorCodes.Unexpected"/>.</returns>
    Task<Result> RunAsync(
        Func<TService, CancellationToken, Task<Result>> useCase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kører et synkront use case.
    /// </summary>
    /// <param name="useCase">Kaldet der skal udføres på servicen.</param>
    /// <returns>Servicens resultat, eller en fejl med <see cref="WebErrorCodes.Unexpected"/>.</returns>
    /// <remarks>
    /// Findes udelukkende til de use cases der ikke laver IO — <c>IGuestService.ValidateFields</c>
    /// er den eneste i dag. Conventions.md: en metode uden reelt async arbejde skal ikke være
    /// async bare for syns skyld.
    /// </remarks>
    Result Run(Func<TService, Result> useCase);
}