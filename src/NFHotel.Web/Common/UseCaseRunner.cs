using NFHotel.ApiClient;
using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// The standard implementation of <see cref="IUseCaseRunner{TService}"/>.
/// </summary>
/// <typeparam name="TService">The Application service to be resolved per call.</typeparam>
/// <remarks>
/// Registered as an open generic scoped service in <c>Program.cs</c> (ADR-0005). It is stateless, but it reads the
/// staff token of the circuit it belongs to and gives it to the scope of each call, so the API sees who is calling.
/// </remarks>
public sealed class UseCaseRunner<TService> : IUseCaseRunner<TService>
    where TService : notnull
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UseCaseRunner<TService>> _logger;
    private readonly IStaffTokenSource _tokenSource;
    private readonly IHttpContextAccessor? _httpContext;

    /// <summary>
    /// Creates the runner.
    /// </summary>
    /// <param name="scopeFactory">The factory that makes a fresh DI scope per call.</param>
    /// <param name="logger">Logger for the technical errors the user never gets to see.</param>
    /// <param name="tokenSource">Gives the staff token of the signed-in user, if any.</param>
    /// <param name="httpContext">Gives the request of a static page, whose address goes to the API for the audit trail (ADR-0015).</param>
    public UseCaseRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<UseCaseRunner<TService>> logger,
        IStaffTokenSource tokenSource,
        IHttpContextAccessor? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(tokenSource);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _tokenSource = tokenSource;
        _httpContext = httpContext;
    }

    /// <inheritdoc />
    public async Task<Result<TValue>> RunAsync<TValue>(
        Func<TService, CancellationToken, Task<Result<TValue>>> useCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            // The scope — and thereby the DbContext — lives only as long as the use case runs.
            await using var scope = _scopeFactory.CreateAsyncScope();
            await GiveStaffTokenAsync(scope).ConfigureAwait(false);

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
            await GiveStaffTokenAsync(scope).ConfigureAwait(false);

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
    /// Decides whether the runner should answer with an error code instead of letting the exception propagate.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Kaldets annulleringstoken.</param>
    /// <returns>True if the exception should become <see cref="WebErrorCodes.Unexpected"/>.</returns>
    /// <remarks>
    /// A cancellation is not an error — it means the user navigated away or closed
    /// the tab. It is allowed to propagate, so Blazor can clean up without noise being logged
    /// and without a dead screen getting an error message.
    /// </remarks>
    private static bool ShouldHandle(Exception exception, CancellationToken cancellationToken) =>
        exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private async Task GiveStaffTokenAsync(AsyncServiceScope scope)
    {
        var token = await _tokenSource.GetTokenAsync().ConfigureAwait(false);
        var context = scope.ServiceProvider.GetRequiredService<StaffCallContext>();
        context.Token = token;
        context.ClientAddress = ClientAddressOf(_httpContext?.HttpContext);
    }

    // The first address of X-Forwarded-For (set by the proxy), or the address of the connection. A circuit has no request.
    private static string? ClientAddressOf(HttpContext? request)
    {
        if (request is null)
        {
            return null;
        }

        var forwarded = request.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        return forwarded.Length > 0 ? forwarded : request.Connection.RemoteIpAddress?.ToString();
    }

    private void Log(Exception exception) =>
        _logger.LogError(
            exception,
            "A use case on {Service} failed unexpectedly. The user got a neutral message.",
            typeof(TService).Name);
}
