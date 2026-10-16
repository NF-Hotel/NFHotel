using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Api.Startup;

/// <summary>
/// What the API does before it serves a call (ADR-0005): apply the migrations when asked, create the first Manager
/// from the environment file when no account exists, and run the <c>create-staff</c> command.
/// </summary>
public static class ApiStartup
{
    /// <summary>The setting that makes the API apply pending migrations at start (<c>Database:MigrateOnStart</c>).</summary>
    public const string MigrateOnStartSetting = "Database:MigrateOnStart";

    /// <summary>The settings of the first Manager.</summary>
    public const string InitialManagerSection = "Staff:InitialManager";

    /// <summary>The command that creates a staff account.</summary>
    public const string CreateStaffCommand = "create-staff";

    /// <summary>The environment variable that holds the password for <see cref="CreateStaffCommand"/>.</summary>
    public const string PasswordVariable = "STAFF_PASSWORD";

    /// <summary>Applies pending migrations when <see cref="MigrateOnStartSetting"/> is true. Running it again changes nothing.</summary>
    /// <param name="app">The application.</param>
    /// <param name="cancellationToken">Cancels the start-up.</param>
    /// <returns>A task that completes when the schema is current.</returns>
    public static async Task MigrateIfRequestedAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!bool.TryParse(app.Configuration[MigrateOnStartSetting], out var migrate) || !migrate)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the first Manager from <c>Staff:InitialManager:UserName</c> and <c>:Password</c> when both are set and no
    /// account exists. Running it again changes nothing.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="cancellationToken">Cancels the start-up.</param>
    /// <returns>A task that completes when the account exists or the settings were empty.</returns>
    /// <exception cref="InvalidOperationException">The settings are set but the account was refused (for example a short password).</exception>
    public static async Task EnsureFirstManagerAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var userName = app.Configuration[$"{InitialManagerSection}:UserName"];
        var password = app.Configuration[$"{InitialManagerSection}:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IStaffAccountService>();
        var result = await accounts.EnsureFirstManagerAsync(userName, password, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            // The codes only: the password is never part of the message.
            throw new InvalidOperationException(
                $"The first Manager was refused: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
    }

    /// <summary>Tells whether the arguments ask for the <c>create-staff</c> command.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>True for <c>create-staff</c>.</returns>
    public static bool IsCreateStaff(string[] args) =>
        args is { Length: > 0 } && string.Equals(args[0], CreateStaffCommand, StringComparison.Ordinal);

    /// <summary>Runs <c>create-staff &lt;user name&gt; &lt;role&gt;</c>; the password comes from <see cref="PasswordVariable"/>.</summary>
    /// <param name="services">The services of the application.</param>
    /// <param name="args">The command line.</param>
    /// <param name="password">The password, read by the caller from <see cref="PasswordVariable"/> so that it never appears on a command line.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>0 when the account was created, 1 when it was refused, 2 for a wrong command line.</returns>
    public static async Task<int> RunCreateStaffAsync(IServiceProvider services, string[] args, string? password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != 3 || !StaffRoleParser.TryParse(RoleNameIgnoringCase(args[2]), out var role) || string.IsNullOrEmpty(password))
        {
            await Console.Error.WriteLineAsync(
                $"Usage: {CreateStaffCommand} <user name> <Receptionist|Manager|Housekeeping> (the password in {PasswordVariable})").ConfigureAwait(false);
            return 2;
        }

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IStaffAccountService>();
        var result = await accounts.CreateAsync(new CreateStaffRequest(args[1], password, role), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            await Console.Error.WriteLineAsync(
                $"Refused: {string.Join(", ", result.Errors.Select(error => error.Code))}").ConfigureAwait(false);
            return 1;
        }

        await Console.Out.WriteLineAsync($"Created {args[1].Trim().ToLowerInvariant()} as {role}.").ConfigureAwait(false);
        return 0;
    }

    // The command line may say "manager"; the claims of a token may not, so the parser itself stays exact.
    private static string? RoleNameIgnoringCase(string text) =>
        Enum.GetNames<StaffRole>().FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
}