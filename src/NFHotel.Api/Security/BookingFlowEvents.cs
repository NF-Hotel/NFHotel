namespace NFHotel.Api.Security;

/// <summary>
/// Writes the security events of the booking flow (MIL-087, ADR-0051 decision 6): each one once in the log of
/// <see cref="SecurityEvents.Category"/>, with the address of the caller and a masked email and without another typed
/// value, and once in the counter of <see cref="SecurityMetrics"/>.
/// </summary>
/// <param name="loggerFactory">The factory of the logger of <see cref="SecurityEvents.Category"/>.</param>
/// <param name="metrics">The counter of the events.</param>
public sealed class BookingFlowEvents(ILoggerFactory loggerFactory, SecurityMetrics metrics)
{
    #region Fields

    private readonly ILogger _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
        .CreateLogger(SecurityEvents.Category);

    private readonly SecurityMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    #endregion Fields

    #region Public methods

    /// <summary>Writes the event of a booking made on the public site.</summary>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The email of the booking, unmasked; the event holds it masked.</param>
    public void PublicBookingMade(string? address, string? email)
    {
        _logger.PublicBookingMade(address ?? SecurityEvents.None, SecurityEvents.MaskEmail(email));
        _metrics.Count(SecurityEvents.PublicBookingMadeKind, null);
    }

    /// <summary>Writes the event of a guest sign-in refused for a wrong reference or email.</summary>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The email that was tried, unmasked; the event holds it masked.</param>
    public void GuestLookupFailed(string? address, string? email)
    {
        _logger.GuestLookupFailed(address ?? SecurityEvents.None, SecurityEvents.MaskEmail(email));
        _metrics.Count(SecurityEvents.GuestLookupFailedKind, null);
    }

    /// <summary>Writes the event of a refusal by a limit.</summary>
    /// <param name="limit">The name of the limit, from <see cref="Application.Ports.LimitNames"/>.</param>
    /// <param name="address">The address of the caller.</param>
    /// <param name="email">The email of the request, unmasked, or <see langword="null"/>.</param>
    public void LimitRefused(string limit, string? address, string? email)
    {
        ArgumentNullException.ThrowIfNull(limit);

        _logger.LimitRefused(limit, address ?? SecurityEvents.None, SecurityEvents.MaskEmail(email));
        _metrics.Count(SecurityEvents.LimitRefusedKind, limit);
    }

    #endregion Public methods
}
