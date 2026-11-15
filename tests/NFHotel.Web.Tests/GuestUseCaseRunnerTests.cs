using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.ApiClient;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The runner on a guest page (MIL-018, ADR-0047 decision 6): the call speaks as the guest and only as the guest, even
/// when the same browser is signed in to the back office; every other page speaks as the staff, as before.
/// </summary>
public sealed class GuestUseCaseRunnerTests
{
    private static readonly GuestSessionDto Session = new("the-guest-token", new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero), 7);

    #region The guest token

    /// <summary>Security: on a guest page the call carries the guest token and no staff token, even with a staff sign-in.</summary>
    [Fact]
    public async Task RunAsync_GuestPageWithAStaffSignIn_GivesOnlyTheGuestToken()
    {
        // Arrange
        using ServiceProvider provider = Provider(withGuestContext: true);
        UseCaseRunner<CallerProbe> runner = Runner(provider, "the-staff-token", Holder(signedIn: true));

        // Act
        Result<(string? Staff, string? Guest)> seen = await runner.RunAsync(
            (probe, _) => Task.FromResult(Result<(string? Staff, string? Guest)>.Success((probe.StaffTokenSeen, probe.GuestTokenSeen))),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((null, "the-guest-token"), seen.Value);
    }

    /// <summary>Equivalence: a page where no guest is held speaks as the staff, with no guest token, as before MIL-018.</summary>
    [Fact]
    public async Task RunAsync_NoGuestHeld_GivesOnlyTheStaffToken()
    {
        // Arrange
        using ServiceProvider provider = Provider(withGuestContext: true);
        UseCaseRunner<CallerProbe> runner = Runner(provider, "the-staff-token", Holder(signedIn: false));

        // Act
        Result<(string? Staff, string? Guest)> seen = await runner.RunAsync(
            (probe, _) => Task.FromResult(Result<(string? Staff, string? Guest)>.Success((probe.StaffTokenSeen, probe.GuestTokenSeen))),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(("the-staff-token", null), seen.Value);
    }

    /// <summary>
    /// Security: a host without the guest context cannot send the guest token, so a call with a guest held goes without
    /// any caller token, never with the staff token of the same browser (ADR-0047 decision 6).
    /// </summary>
    [Fact]
    public async Task RunAsync_HostWithoutTheGuestContext_GivesNoCallerToken()
    {
        // Arrange
        using ServiceProvider provider = Provider(withGuestContext: false);
        UseCaseRunner<CallerProbe> runner = Runner(provider, "the-staff-token", Holder(signedIn: true));

        // Act
        Result<string?> seen = await runner.RunAsync(
            (probe, _) => Task.FromResult(Result<string?>.Success(probe.StaffTokenSeen)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(seen.Value);
    }

    #endregion

    #region Helpers

    private static ServiceProvider Provider(bool withGuestContext)
    {
        ServiceCollection services = new();
        services.AddScoped<StaffCallContext>();
        if (withGuestContext)
        {
            services.AddScoped<GuestCallContext>();
        }

        services.AddScoped<CallerProbe>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static GuestTokenHolder Holder(bool signedIn)
    {
        GuestTokenHolder holder = new();
        holder.Hold(signedIn ? GuestClaims.CreatePrincipal(Session) : null);
        return holder;
    }

    private static UseCaseRunner<CallerProbe> Runner(ServiceProvider provider, string? staffToken, IGuestTokenSource guestTokens) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<UseCaseRunner<CallerProbe>>.Instance,
            new FixedStaffTokenSource(staffToken),
            httpContext: null,
            guestTokens);

    /// <summary>A service that reports the tokens the scope it lives in holds.</summary>
    private sealed class CallerProbe(StaffCallContext staff, IServiceProvider services)
    {
        public string? StaffTokenSeen => staff.Token;

        public string? GuestTokenSeen => services.GetService<GuestCallContext>()?.Token;
    }

    private sealed class FixedStaffTokenSource(string? token) : IStaffTokenSource
    {
        public ValueTask<string?> GetTokenAsync() => ValueTask.FromResult(token);
    }

    #endregion
}
