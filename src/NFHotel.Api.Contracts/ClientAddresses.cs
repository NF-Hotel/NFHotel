using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace NFHotel.Api.Contracts;

/// <summary>
/// The address of a caller as the hops of the stack pass it on (MIL-087, ADR-0051 decision 4): nginx writes the address
/// it saw into <see cref="ApiAuthentication.ClientAddressHeader"/>, the web application reads it only from a peer in
/// <see cref="StackNetworks"/>, and the API reads it only on a call with the service token. One address is written in
/// one form, so that one caller is one key of the limits.
/// </summary>
public static class ClientAddresses
{
    #region Fields

    /// <summary>The longest text of one address: an IPv6 address with an IPv4 part, as <c>ffff:...:255.255.255.255</c>.</summary>
    private const int MaxLength = 45;

    #endregion Fields

    #region Properties

    /// <summary>
    /// The networks of the stack, from which the web application takes the header of nginx and the API takes forwarded
    /// headers: loopback, and the private ranges from which Docker gives its networks their addresses.
    /// </summary>
    public static IReadOnlyList<IPNetwork> StackNetworks { get; } =
    [
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
    ];

    #endregion Properties

    #region Public methods

    /// <summary>Tells whether an address is in one of the <see cref="StackNetworks"/>.</summary>
    /// <param name="address">The address of a peer.</param>
    /// <returns><see langword="true"/> for a peer of the stack.</returns>
    public static bool IsInStack(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        IPAddress peer = OneForm(address);
        return StackNetworks.Any(network => network.Contains(peer));
    }

    /// <summary>
    /// Reads one address from the value of a header and writes it in its one form. Only the characters of an address
    /// are accepted, so a list, a port, a zone or a name is refused, and an IPv4 address only in its four decimal parts,
    /// so the short and octal forms that the parser of .NET also reads (<c>127.1</c>, <c>010.0.0.1</c>) are refused.
    /// </summary>
    /// <param name="value">The value, such as <c>203.0.113.7</c>.</param>
    /// <param name="address">The address in its one form, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the value is one address.</returns>
    public static bool TryNormalize(string? value, [NotNullWhen(true)] out string? address)
    {
        address = null;
        string text = value?.Trim() ?? string.Empty;
        if (text.Length is 0 or > MaxLength || !text.All(IsAddressCharacter) || !IPAddress.TryParse(text, out IPAddress? parsed))
        {
            return false;
        }

        if (parsed.AddressFamily == AddressFamily.InterNetwork && parsed.ToString() != text)
        {
            return false;
        }

        address = Normalize(parsed);
        return true;
    }

    /// <summary>Writes an address in its one form: an IPv4 address in IPv6 form as IPv4.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The text of the address.</returns>
    public static string Normalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return OneForm(address).ToString();
    }

    #endregion Public methods

    #region Private methods

    private static IPAddress OneForm(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private static bool IsAddressCharacter(char character) => char.IsAsciiHexDigit(character) || character is '.' or ':';

    #endregion Private methods
}
