namespace NFHotel.Application.Ports;

/// <summary>The answer of a limit of <see cref="ISenderLimits"/> to one request (ADR-0051 decision 2).</summary>
/// <param name="IsAllowed">Whether the request is counted and may go on.</param>
/// <param name="Limit">The name of the limit that refused the request, as the table of the limits names it; <see langword="null"/> when allowed.</param>
/// <param name="RetryAfter">The time until the window that refused has room again; zero when allowed.</param>
public readonly record struct SenderLimitDecision(bool IsAllowed, string? Limit, TimeSpan RetryAfter)
{
    #region Properties

    /// <summary>The answer to a request that is counted and may go on.</summary>
    public static SenderLimitDecision Allowed { get; } = new(true, null, TimeSpan.Zero);

    #endregion Properties

    #region Public methods

    /// <summary>The answer to a request that a limit refuses.</summary>
    /// <param name="limit">The name of the limit.</param>
    /// <param name="retryAfter">The time until its window has room again.</param>
    /// <returns>The refusal.</returns>
    public static SenderLimitDecision Refused(string limit, TimeSpan retryAfter) => new(false, limit, retryAfter);

    #endregion Public methods
}
