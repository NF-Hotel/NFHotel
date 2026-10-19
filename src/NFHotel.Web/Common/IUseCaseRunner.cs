using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// Runs one use case call on <typeparamref name="TService"/> in its own DI scope and ensures
/// that the call always ends in a <see cref="Result"/>.
/// </summary>
/// <typeparam name="TService">The Application service, for example <c>IBookingService</c>.</typeparam>
/// <remarks>
/// The type solves two things at once, and both are requirements for Phase 2.
/// <para>
/// <b>1. One scope per use case.</b> Architecture.md section 8: <i>"Blazor Server must give
/// each page its own DI scope. If it does not, a user gets one <c>DbContext</c> for
/// their whole session, and two simultaneous component events throw."</i> In Blazor Server
/// a DI scope lives as long as the SignalR circuit — potentially hours.
/// <c>AddInfrastructure</c> therefore registers <c>HotelDbContext</c> as <c>Scoped</c> on
/// top of an <c>IDbContextFactory</c>, precisely so Web can fetch a fresh context per use
/// case. This type is where Web actually does it: one call = one scope = one
/// <c>DbContext</c>, which repositories and <c>IUnitOfWork</c> share among themselves, but do not share
/// with another call in progress. Without it, two quick clicks on the same page would hit the same
/// context in the middle of an <c>await</c>.
/// </para>
/// <para>
/// <b>2. One place to catch the unexpected.</b> Expected rule violations already come back
/// as <see cref="Result"/>. A database that is down does not — it throws. The runner catches
/// it, logs the technical detail centrally and returns
/// <see cref="WebErrorCodes.Unexpected"/>, so the screen can show a neutral message through the
/// same code path as everything else instead of tearing down the circuit.
/// </para>
/// <para>
/// ViewModels depend on this type rather than on <c>IServiceScopeFactory</c>, so they only
/// mention Application interfaces and do not themselves know the DI mechanics (B-10).
/// </para>
/// </remarks>
public interface IUseCaseRunner<TService>
    where TService : notnull
{
    /// <summary>
    /// Runs an asynchronous use case that returns a value.
    /// </summary>
    /// <typeparam name="TValue">The value type of the use case.</typeparam>
    /// <param name="useCase">The call to be made on the service.</param>
    /// <param name="cancellationToken">Cancellation token, passed on to the service.</param>
    /// <returns>The result of the service, or an error with <see cref="WebErrorCodes.Unexpected"/>.</returns>
    Task<Result<TValue>> RunAsync<TValue>(
        Func<TService, CancellationToken, Task<Result<TValue>>> useCase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an asynchronous use case without a return value.
    /// </summary>
    /// <param name="useCase">The call to be made on the service.</param>
    /// <param name="cancellationToken">Cancellation token, passed on to the service.</param>
    /// <returns>The result of the service, or an error with <see cref="WebErrorCodes.Unexpected"/>.</returns>
    Task<Result> RunAsync(
        Func<TService, CancellationToken, Task<Result>> useCase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a synchronous use case.
    /// </summary>
    /// <param name="useCase">The call to be made on the service.</param>
    /// <returns>The result of the service, or an error with <see cref="WebErrorCodes.Unexpected"/>.</returns>
    /// <remarks>
    /// Exists solely for the use cases that do no IO — <c>IGuestService.ValidateFields</c>
    /// is the only one today. Conventions.md: a method without real async work should not be
    /// async bare for syns skyld.
    /// </remarks>
    Result Run(Func<TService, Result> useCase);
}
