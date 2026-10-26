using NFHotel.Application.UseCases;
using NFHotel.Web.Common;

namespace NFHotel.Web.Tests;

/// <summary>A use-case runner that calls the service it was given, with no token: for view models whose calls are faked.</summary>
/// <typeparam name="TService">The service.</typeparam>
/// <param name="service">The fake service.</param>
public sealed class PassThroughRunner<TService>(TService service) : IUseCaseRunner<TService>
    where TService : notnull
{
    /// <inheritdoc />
    public Task<Result<TValue>> RunAsync<TValue>(
        Func<TService, CancellationToken, Task<Result<TValue>>> useCase,
        CancellationToken cancellationToken = default) =>
        useCase(service, cancellationToken);

    /// <inheritdoc />
    public Task<Result> RunAsync(Func<TService, CancellationToken, Task<Result>> useCase, CancellationToken cancellationToken = default) =>
        useCase(service, cancellationToken);

    /// <inheritdoc />
    public Result Run(Func<TService, Result> useCase) => useCase(service);
}
