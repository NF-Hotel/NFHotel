using System.Runtime.CompilerServices;

namespace NFHotel.Proxmox.Tests;

/// <summary>
/// A test that needs the real Proxmox server. It is skipped when no host is
/// configured, unless <c>PVE_REQUIRE_LIVE=true</c> (set by the runner), in
/// which case missing settings make it fail instead of hiding the problem.
/// </summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        var required = string.Equals(
            Environment.GetEnvironmentVariable("PVE_REQUIRE_LIVE"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!required && !PveSettings.IsConfigured(Environment.GetEnvironmentVariable))
        {
            Skip = "PVE_HOST is not set; run scripts/run-proxmox-tests.sh";
        }
    }
}
