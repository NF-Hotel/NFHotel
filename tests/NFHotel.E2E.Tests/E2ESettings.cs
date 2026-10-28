namespace NFHotel.E2E.Tests;

/// <summary>A staff role of the back office (Access Control Matrix), as the test accounts of the stack hold them.</summary>
public enum StaffRole
{
    Manager,
    Receptionist,
    Housekeeping,
}

/// <summary>
/// The settings of the end-to-end stack, read from the environment that scripts/run-e2e-tests.sh passes in: the base
/// address of nginx and one throwaway account per role. Nothing here is a real secret; the stack makes new ones each run.
/// </summary>
public static class E2ESettings
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<StaffRole, string> ChosenPasswords = new();

    /// <summary>True when the runner asks the tests to fail rather than skip without settings.</summary>
    public static bool IsRequired =>
        string.Equals(Environment.GetEnvironmentVariable("E2E_REQUIRE"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the base address of the stack is set.</summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL"));

    /// <summary>The address of nginx in the stack, for example <c>https://nginx</c>.</summary>
    public static Uri BaseUrl => new(Required("E2E_BASE_URL").TrimEnd('/') + "/");

    /// <summary>The folder for traces and screenshots (default <c>e2e-results</c> beside the test binaries).</summary>
    public static string ResultsDirectory =>
        Environment.GetEnvironmentVariable("E2E_RESULTS") is { Length: > 0 } folder
            ? folder
            : Path.Combine(AppContext.BaseDirectory, "e2e-results");

    /// <summary>The user name of the test account of a role.</summary>
    public static string UserName(StaffRole role) => Required($"E2E_{role.ToString().ToUpperInvariant()}_USER");

    /// <summary>
    /// The password of the test account of a role. An account that had to choose a new password at its first sign-in
    /// (the first Manager, ADR-0017) answers with the one it chose.
    /// </summary>
    public static string Password(StaffRole role) =>
        ChosenPasswords.TryGetValue(role, out var chosen) ? chosen : Required($"E2E_{role.ToString().ToUpperInvariant()}_PASSWORD");

    /// <summary>Remembers the password that a test account chose, so every later sign-in of the run uses it.</summary>
    /// <param name="role">The role of the account.</param>
    /// <param name="password">The new password.</param>
    public static void RememberChosenPassword(StaffRole role, string password) => ChosenPasswords[role] = password;

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"{name} is not set; run scripts/run-e2e-tests.sh of NF-Hotel/DevOps");
}
