using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace NFHotel.Application.UseCases;

/// <summary>
/// The key under which the limits count a caller, so that one caller is one key (ADR-0049 decision 6, ADR-0051
/// decision 2): an IPv4 address, also in IPv6 form, counts as itself; an IPv6 address counts by its first 64 bits, the
/// prefix of its link, because the last 64 bits are its interface identifier (RFC 4291 section 2.5.1), which a host
/// forms itself and may change for each try (RFC 8981). An address that is missing or that is no address counts under
/// <see cref="Unknown"/>.
/// </summary>
public static class CallerAddressKey
{
    #region Fields

    /// <summary>The key of the callers whose address is not known: only a host without the address of a connection.</summary>
    public const string Unknown = "unknown";

    #endregion Fields

    #region Public methods

    /// <summary>The key of an address.</summary>
    /// <param name="address">The address, or <see langword="null"/>.</param>
    /// <returns>The key.</returns>
    public static string Of(string? address)
    {
        if (!IPAddress.TryParse(address, out IPAddress? parsed))
        {
            return Unknown;
        }

        if (parsed.IsIPv4MappedToIPv6)
        {
            return parsed.MapToIPv4().ToString();
        }

        if (parsed.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return parsed.ToString();
        }

        byte[] network = parsed.GetAddressBytes();
        Array.Clear(network, 8, 8);
        return string.Create(CultureInfo.InvariantCulture, $"{new IPAddress(network)}/64");
    }

    #endregion Public methods
}
