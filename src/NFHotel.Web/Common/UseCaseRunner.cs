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
/// On a guest page it gives the guest token instead, and then no staff token (MIL-018).
/// </remarks>
public sealed class UseCaseRunner<TService> : IUseCaseRunner<TService>
    where TService : notnull
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UseCaseRunner<TService>> _logger;
    private readonly IStaffTokenSource _tokenSource;
    private readonly IClientAddressSource? _clientAddresses;
    private readonly IGuestTokenSource? _guestTokens;

    /// <summary>
    /// Creates the runner.
    /// </summary>
    /// <param name="scopeFactory">The factory that makes a fresh DI scope per call.</param>
    /// <param name="logger">Logger for the technical errors the user never gets to see.</param>
    /// <param name="tokenSource">Gives the staff token of the signed-in user, if any.</param>
    /// <param name="clientAddresses">
    /// Gives the address of the browser, which goes to the API for the audit trail (ADR-0017) and the limits of the booking
    /// flow (MIL-087, ADR-0051 decision 4).
    /// </param>
    /// <param name="guestTokens">Gives the guest token of a guest page, if any (MIL-018).</param>
    public UseCaseRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<UseCaseRunner<TService>> logger,
        IStaffTokenSource tokenSource,
        IClientAddressSource? clientAddresses = null,
        IGuestTokenSource? guestTokens = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(tokenSource);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _tokenSource = tokenSource;
        _clientAddresses = clientAddresses;
        _guestTokens = guestTokens;
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
            await GiveCallerTokenAsync(scope).ConfigureAwait(false);

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
            await GiveCallerTokenAsync(scope).ConfigureAwait(false);

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
    /// <param name="cancellationToken">The cancellation token of the call.</param>
    /// <returns>True if the exception should become <see cref="WebErrorCodes.Unexpected"/>.</returns>
    /// <remarks>
    /// A cancellation is not an error — it means the user navigated away or closed
    /// the tab. It is allowed to propagate, so Blazor can clean up without noise being logged
    /// and without a dead screen getting an error message.
    /// </remarks>
    private static bool ShouldHandle(Exception exception, CancellationToken cancellationToken) =>
        exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private async Task GiveCallerTokenAsync(AsyncServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<StaffCallContext>();
        context.ClientAddress = _clientAddresses is null ? null : await _clientAddresses.GetAddressAsync().ConfigureAwait(false);

        // A guest page speaks as the guest and only as the guest (MIL-018, ADR-0047 decision 6): with a guest token the
        // call carries no staff token, even when the same browser is signed in to the back office. A host without the
        // guest context cannot send the guest token, so the call goes without a caller token rather than as staff.
        if (_guestTokens?.Token is string guestToken)
        {
            if (scope.ServiceProvider.GetService<GuestCallContext>() is GuestCallContext guest)
            {
                guest.Token = guestToken;
            }

            return;
        }

        context.Token = await _tokenSource.GetTokenAsync().ConfigureAwait(false);
    }

    private void Log(Exception exception) =>
        _logger.LogError(
            exception,
            "A use case on {Service} failed unexpectedly. The user got a neutral message.",
            typeof(TService).Name);
}
