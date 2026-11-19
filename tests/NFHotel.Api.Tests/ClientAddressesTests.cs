using System.Net;

using NFHotel.Api.Contracts;

namespace NFHotel.Api.Tests;

/// <summary>
/// The address of a caller as the hops of the stack pass it on (MIL-087, ADR-0051 decision 4): the networks of the stack,
/// from which a header of nginx or a forwarded header is read, and the one form of an address, so that one caller is one
/// key of the limits.
/// </summary>
public sealed class ClientAddressesTests
{
    #region IsInStack

    /// <summary>Positive: loopback and the private ranges that Docker gives its networks are the stack, also in IPv6 form.</summary>
    /// <param name="peer">The address of a peer.</param>
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.0.1")]
    [InlineData("::ffff:172.18.0.4")]
    public void IsInStack_LoopbackAndPrivateRanges_IsTrue(string peer)
    {
        // Act and assert
        Assert.True(ClientAddresses.IsInStack(IPAddress.Parse(peer)));
    }

    /// <summary>Security: a public address, and the addresses right outside the private ranges, are not the stack.</summary>
    /// <param name="peer">The address of a peer.</param>
    [Theory]
    [InlineData("203.0.113.7")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("11.0.0.1")]
    [InlineData("192.169.0.1")]
    [InlineData("2001:db8::1")]
    [InlineData("::ffff:203.0.113.7")]
    public void IsInStack_OtherAddresses_IsFalse(string peer)
    {
        // Act and assert
        Assert.False(ClientAddresses.IsInStack(IPAddress.Parse(peer)));
    }

    /// <summary>Positive: the list names the five networks of the stack, so the API and the web application trust the same peers.</summary>
    [Fact]
    public void StackNetworks_InTheirOrder_AreLoopbackAndThePrivateRanges()
    {
        // Act
        string[] networks = [.. ClientAddresses.StackNetworks.Select(network => network.ToString())];

        // Assert
        Assert.Equal(["127.0.0.0/8", "::1/128", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"], networks);
    }

    #endregion

    #region TryNormalize

    /// <summary>Positive: one address, with spaces around it or in another form of the same address, is read in its one form.</summary>
    /// <param name="value">The value of the header.</param>
    /// <param name="expected">The one form.</param>
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData(" 203.0.113.7 ", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:DB8:0:0:0:0:0:7", "2001:db8::7")]
    public void TryNormalize_OneAddress_IsItsOneForm(string value, string expected)
    {
        // Act
        bool read = ClientAddresses.TryNormalize(value, out string? address);

        // Assert
        Assert.True(read);
        Assert.Equal(expected, address);
    }

    /// <summary>
    /// Negative: no value, a list, a port, a zone, a short or octal form of IPv4 and text are not one address, so a
    /// header that a caller could shape names nobody.
    /// </summary>
    /// <param name="value">The value of the header.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("203.0.113.7, 10.0.0.2")]
    [InlineData("203.0.113.7:443")]
    [InlineData("[2001:db8::7]:443")]
    [InlineData("fe80::1%eth0")]
    [InlineData("127.1")]
    [InlineData("010.0.0.1")]
    [InlineData("0x7f.0.0.1")]
    [InlineData("unknown")]
    public void TryNormalize_NotOneAddress_IsFalse(string? value)
    {
        // Act
        bool read = ClientAddresses.TryNormalize(value, out string? address);

        // Assert
        Assert.False(read);
        Assert.Null(address);
    }

    /// <summary>Equivalence: an IPv4 address in IPv6 form is written in its IPv4 form, and any other address as itself.</summary>
    [Fact]
    public void Normalize_MappedAndOtherAddresses_AreWrittenInTheirOneForm()
    {
        // Act and assert
        Assert.Equal("203.0.113.7", ClientAddresses.Normalize(IPAddress.Parse("::ffff:203.0.113.7")));
        Assert.Equal("2001:db8::7", ClientAddresses.Normalize(IPAddress.Parse("2001:db8::7")));
        Assert.Equal("198.51.100.4", ClientAddresses.Normalize(IPAddress.Parse("198.51.100.4")));
    }

    #endregion
}
