using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.ApiClient;
using NFHotel.Application.UseCases;
using NFHotel.Web.Common;

namespace NFHotel.Web.Tests;

/// <summary>The runner gives each call its own scope and puts the staff token of the circuit into it (ADR-0005).</summary>
public sealed class UseCaseRunnerTests
{
    #region The staff token

    /// <summary>Positive: the service of the call sees the staff token of the signed-in user.</summary>
    [Fact]
    public async Task RunAsync_SignedInUser_GivesTheTokenToTheScopeOfTheCall()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, "the-staff-token");

        // Act
        var result = await runner.RunAsync(
            (service, _) => Task.FromResult(Result<string?>.Success(service.TokenSeen)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("the-staff-token", result.Value);
    }

    /// <summary>Equivalence: a call of the public pages, where nobody is signed in, carries no token.</summary>
    [Fact]
    public async Task RunAsync_NobodySignedIn_GivesNoToken()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, null);

        // Act
        var result = await runner.RunAsync(
            (service, _) => Task.FromResult(Result<string?>.Success(service.TokenSeen)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result.Value);
    }

    /// <summary>Concurrency: two calls at once each get their own scope, so the token of one never leaks into the other.</summary>
    [Fact]
    public async Task RunAsync_TwoCalls_GetTheirOwnScopes()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, "the-staff-token");

        // Act
        var calls = await Task.WhenAll(
            runner.RunAsync((service, _) => Task.FromResult(Result<Guid>.Success(service.ScopeId)), TestContext.Current.CancellationToken),
            runner.RunAsync((service, _) => Task.FromResult(Result<Guid>.Success(service.ScopeId)), TestContext.Current.CancellationToken));

        // Assert
        Assert.NotEqual(calls[0].Value, calls[1].Value);
    }

    #endregion

    #region Failures

    /// <summary>Negative: an unexpected exception becomes the neutral code, and the screen never sees the exception.</summary>
    [Fact]
    public async Task RunAsync_ServiceThrows_ReturnsTheUnexpectedCode()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, null);

        // Act
        var result = await runner.RunAsync<string>(
            (_, _) => throw new InvalidOperationException("database password is hunter2"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(WebErrorCodes.Unexpected, result.FirstError?.Code);
    }

    /// <summary>Edge: a cancelled call propagates the cancellation instead of becoming an error.</summary>
    [Fact]
    public async Task RunAsync_CancelledByTheCaller_PropagatesTheCancellation()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, null);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // Act and assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync<string>(
            (_, token) => Task.FromCanceled<Result<string>>(token),
            cancelled.Token));
    }

    /// <summary>Positive and negative: the call without a value runs in its own scope, and a throw becomes the neutral code.</summary>
    [Fact]
    public async Task RunAsync_WithoutValue_RunsOrReturnsTheUnexpectedCode()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, "the-staff-token");

        // Act
        var success = await runner.RunAsync(
            (service, _) => Task.FromResult(service.TokenSeen == "the-staff-token" ? Result.Success() : Result.Failure("wrong")),
            TestContext.Current.CancellationToken);
        var failure = await runner.RunAsync(
            (_, _) => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success.IsSuccess);
        Assert.Equal(WebErrorCodes.Unexpected, failure.FirstError?.Code);
    }

    /// <summary>Positive and negative: the synchronous call runs in a scope, and a throw becomes the neutral code.</summary>
    [Fact]
    public void Run_Synchronous_RunsOrReturnsTheUnexpectedCode()
    {
        // Arrange
        using var provider = Provider();
        var runner = Runner(provider, null);

        // Act
        var success = runner.Run(service => service.ScopeId == Guid.Empty ? Result.Failure("no scope") : Result.Success());
        var failure = runner.Run(_ => throw new InvalidOperationException("boom"));

        // Assert
        Assert.True(success.IsSuccess);
        Assert.Equal(WebErrorCodes.Unexpected, failure.FirstError?.Code);
    }

    /// <summary>Negative: every dependency and every use case is required.</summary>
    [Fact]
    public async Task Constructor_AndCalls_MissingArguments_Throw()
    {
        // Arrange
        using var provider = Provider();
        var scopes = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<UseCaseRunner<Probe>>.Instance;
        var tokens = new FixedTokenSource(null);
        var runner = Runner(provider, null);

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => new UseCaseRunner<Probe>(null!, logger, tokens));
        Assert.Throws<ArgumentNullException>(() => new UseCaseRunner<Probe>(scopes, null!, tokens));
        Assert.Throws<ArgumentNullException>(() => new UseCaseRunner<Probe>(scopes, logger, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync<string>(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync(null!, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => runner.Run(null!));
    }

    #endregion

    #region The address of the browser

    /// <summary>
    /// Positive (MIL-087 criterion 4, ADR-0051 decision 4): a call carries the address that the source of addresses gives,
    /// so the API counts and audits the browser and not the web application.
    /// </summary>
    [Fact]
    public async Task RunAsync_SourceWithAnAddress_GivesItToTheCall()
    {
        // Arrange
        using ServiceProvider provider = Provider();
        UseCaseRunner<Probe> runner = Runner(provider, null, new FixedAddressSource("203.0.113.7"));

        // Act
        Result<string?> result = await runner.RunAsync(
            (service, _) => Task.FromResult(Result<string?>.Success(service.AddressSeen)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("203.0.113.7", result.Value);
    }

    /// <summary>Positive: a call without an answer carries the address too, so a refused guest sign-in is counted per browser.</summary>
    [Fact]
    public async Task RunAsync_CallWithoutAnAnswer_GivesTheAddressToTheCall()
    {
        // Arrange
        using ServiceProvider provider = Provider();
        UseCaseRunner<Probe> runner = Runner(provider, null, new FixedAddressSource("203.0.113.8"));
        string? seen = null;

        // Act
        await runner.RunAsync(
            (service, _) =>
            {
                seen = service.AddressSeen;
                return Task.FromResult(Result.Success());
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("203.0.113.8", seen);
    }

    /// <summary>
    /// Equivalence: a source without an address, and a host without a source, give the call no address; the runner reads
    /// no header of its own, so a header that a browser sends never reaches the API as its address.
    /// </summary>
    [Fact]
    public async Task RunAsync_SourceWithoutAnAddressOrNoSource_GivesNone()
    {
        // Arrange
        using ServiceProvider provider = Provider();

        // Act
        Result<string?> fromEmptySource = await Runner(provider, null, new FixedAddressSource(null)).RunAsync(
            (service, _) => Task.FromResult(Result<string?>.Success(service.AddressSeen)),
            TestContext.Current.CancellationToken);
        Result<string?> withoutSource = await Runner(provider, null).RunAsync(
            (service, _) => Task.FromResult(Result<string?>.Success(service.AddressSeen)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(fromEmptySource.Value);
        Assert.Null(withoutSource.Value);
    }

    #endregion

    #region Helpers

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddScoped<StaffCallContext>();
        services.AddScoped<Probe>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static UseCaseRunner<Probe> Runner(ServiceProvider provider, string? token, IClientAddressSource? addresses = null) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UseCaseRunner<Probe>>.Instance,
            new FixedTokenSource(token),
            addresses);

    /// <summary>A service that reports what the scope it lives in holds.</summary>
    private sealed class Probe
    {
        private readonly StaffCallContext _context;

        public Probe(StaffCallContext context)
        {
            _context = context;
        }

        public string? TokenSeen => _context.Token;

        public string? AddressSeen => _context.ClientAddress;

        public Guid ScopeId { get; } = Guid.NewGuid();
    }

    private sealed class FixedTokenSource : IStaffTokenSource
    {
        private readonly string? _token;

        public FixedTokenSource(string? token)
        {
            _token = token;
        }

        public ValueTask<string?> GetTokenAsync() => ValueTask.FromResult(_token);
    }

    private sealed class FixedAddressSource : IClientAddressSource
    {
        private readonly string? _address;

        public FixedAddressSource(string? address)
        {
            _address = address;
        }

        public ValueTask<string?> GetAddressAsync() => ValueTask.FromResult(_address);
    }

    #endregion
}
