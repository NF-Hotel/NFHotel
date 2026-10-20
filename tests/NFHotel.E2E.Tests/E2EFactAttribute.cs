using System.Runtime.CompilerServices;

namespace NFHotel.E2E.Tests;

/// <summary>
/// A test that needs the running end-to-end stack (MIL-027). It is skipped when <c>E2E_BASE_URL</c> is not set, unless
/// <c>E2E_REQUIRE=true</c> (set by the runner script), in which case the missing settings make it fail instead of
/// hiding the problem.
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!E2ESettings.IsRequired && !E2ESettings.IsConfigured)
        {
            Skip = "E2E_BASE_URL is not set; run scripts/run-e2e-tests.sh of NF-Hotel/DevOps";
        }
    }
}
