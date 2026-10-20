using System.Net;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>
/// Counts failed sign-ins per client address for the calls that arrive without the service token (ADR-0015), so one
/// address cannot try many user names. The limit is a multiple of the limit per user name, because several members of
/// staff can share one address. The web application, which signs everyone in from one address, is not counted.
/// </summary>
public sealed class AddressLoginThrottle
{
    /// <summary>How many times the limit per user name one address may fail.</summary>
    public const int Multiplier = 4;

    private const string Unknown = "unknown";

    private readonly InMemoryLoginThrottle _throttle;

    /// <summary>Creates the throttle.</summary>
    /// <param name="options">The settings of the sign-in; the limit per user name is multiplied.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    public AddressLoginThrottle(StaffSecurityOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        _throttle = new InMemoryLoginThrottle(
            new StaffSecurityOptions
            {
                SigningKey = options.SigningKey,
                MaxFailures = options.MaxFailures * Multiplier,
                ThrottleWindowMinutes = options.ThrottleWindowMinutes,
            },
            clock);
    }

    /// <summary>Tells whether the address has used up its attempts in the current window.</summary>
    /// <param name="address">The client address; null when the server does not know it.</param>
    /// <returns>True when the next attempt must be refused.</returns>
    public bool IsBlocked(IPAddress? address) => _throttle.IsBlocked(KeyOf(address));

    /// <summary>Counts one failed attempt.</summary>
    /// <param name="address">The client address; null when the server does not know it.</param>
    public void RecordFailure(IPAddress? address) => _throttle.RecordFailure(KeyOf(address));

    private static string KeyOf(IPAddress? address) =>
        address is null ? Unknown : (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
}
