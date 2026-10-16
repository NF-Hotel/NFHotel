using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// Standardimplementeringen af <see cref="IUseCaseRunner{TService}"/>.
/// </summary>
/// <typeparam name="TService">Application-servicen der skal opløses pr. kald.</typeparam>
/// <remarks>
/// Registreres som åben generisk singleton i <c>Program.cs</c>. Den er tilstandsløs, og
/// <see cref="IServiceScopeFactory"/> er selv en singleton — at gøre runneren scoped ville
/// binde den til kredsløbet uden at give noget.
/// </remarks>
public sealed class UseCaseRunner<TService> : IUseCaseRunner<TService>
    where TService : notnull
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UseCaseRunner<TService>> _logger;

    /// <summary>
    /// Opretter runneren.
    /// </summary>
    /// <param name="scopeFactory">Fabrikken der laver en frisk DI-scope pr. kald.</param>
    /// <param name="logger">Logger til de tekniske fejl brugeren aldrig får at se.</param>
    public UseCaseRunner(IServiceScopeFactory scopeFactory, ILogger<UseCaseRunner<TService>> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TValue>> RunAsync<TValue>(
        Func<TService, CancellationToken, Task<Result<TValue>>> useCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            // Scopen — og dermed DbContext'en — lever kun så længe use casen kører.
            await using var scope = _scopeFactory.CreateAsyncScope();

            var service = scope.ServiceProvider.GetRequiredService<TService>();

            return await useCase(service, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (ShouldHandle(exception, cancellationToken))
        {
            Log(exception);

            return Result<TValue>.Failure(WebErrorCodes.Unexpected);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RunAsync(
        Func<TService, CancellationToken, Task<Result>> useCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var service = scope.ServiceProvider.GetRequiredService<TService>();

            return await useCase(service, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (ShouldHandle(exception, cancellationToken))
        {
            Log(exception);

            return Result.Failure(WebErrorCodes.Unexpected);
        }
    }

    /// <inheritdoc />
    public Result Run(Func<TService, Result> useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<TService>();

            return useCase(service);
        }
        catch (Exception exception) when (ShouldHandle(exception, CancellationToken.None))
        {
            Log(exception);

            return Result.Failure(WebErrorCodes.Unexpected);
        }
    }

    /// <summary>
    /// Afgør om runneren skal svare med en fejlkode i stedet for at lade exceptionen løbe.
    /// </summary>
    /// <param name="exception">Den kastede exception.</param>
    /// <param name="cancellationToken">Kaldets annulleringstoken.</param>
    /// <returns>Sand hvis exceptionen skal blive til <see cref="WebErrorCodes.Unexpected"/>.</returns>
    /// <remarks>
    /// En annullering er ikke en fejl — den betyder at brugeren navigerede væk eller lukkede
    /// fanen. Den får lov at løbe videre, så Blazor kan rydde op uden at der bliver logget
    /// støj og uden at en død skærm får en fejlbesked.
    /// </remarks>
    private static bool ShouldHandle(Exception exception, CancellationToken cancellationToken) =>
        exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private void Log(Exception exception) =>
        _logger.LogError(
            exception,
            "Et use case på {Service} fejlede uventet. Brugeren fik en neutral besked.",
            typeof(TService).Name);
}