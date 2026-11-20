using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The mailbox of an email, as the limits per email count it (MIL-088, ADR-0052 decision 6): the forms of an address that
/// reach one mailbox give one form, and the count by mailbox reads the addresses of the domains that can reach it.
/// </summary>
public sealed class MailboxTests
{
    #region The mailbox form

    /// <summary>Equivalence: letter case, spaces around, a <c>+</c> tag and, for Gmail, dots and the older domain count once.</summary>
    [Theory]
    [InlineData("anna@example.com", "anna@example.com")]
    [InlineData(" ANNA@Example.com ", "anna@example.com")]
    [InlineData("anna+trip@example.com", "anna@example.com")]
    [InlineData("a.nna@example.com", "a.nna@example.com")]
    [InlineData("a.nna+1@gmail.com", "anna@gmail.com")]
    [InlineData("A.N.N.A@Gmail.com", "anna@gmail.com")]
    [InlineData("a.nna+trip@googlemail.com", "anna@gmail.com")]
    public void NormalFormOf_FormsOfOneMailbox_GiveOneForm(string email, string expected)
    {
        // Act
        string form = Mailbox.NormalFormOf(email);

        // Assert
        Assert.Equal(expected, form);
    }

    /// <summary>Boundary: a <c>+</c> as the first character is part of the name, and text without an <c>@</c> stays as it is.</summary>
    [Theory]
    [InlineData("+anna@example.com", "+anna@example.com")]
    [InlineData("no-at-sign", "no-at-sign")]
    [InlineData("@example.com", "@example.com")]
    public void NormalFormOf_NoNameToStrip_KeepsTheText(string email, string expected)
    {
        // Act
        string form = Mailbox.NormalFormOf(email);

        // Assert
        Assert.Equal(expected, form);
    }

    /// <summary>Negative: two different names of one domain stay two mailboxes.</summary>
    [Fact]
    public void NormalFormOf_DifferentNames_GiveDifferentForms()
    {
        // Act
        string first = Mailbox.NormalFormOf("anna@example.com");
        string second = Mailbox.NormalFormOf("anne@example.com");

        // Assert
        Assert.NotEqual(first, second);
    }

    #endregion

    #region The domains

    /// <summary>Positive: a Gmail address is read in both Gmail domains, another address in its own domain.</summary>
    [Theory]
    [InlineData("anna@gmail.com", "gmail.com googlemail.com")]
    [InlineData("anna@GoogleMail.com", "gmail.com googlemail.com")]
    [InlineData("anna@Example.com", "example.com")]
    public void DomainsOf_Address_GivesTheDomainsThatReachItsMailbox(string email, string expected)
    {
        // Act
        IReadOnlyList<string> domains = Mailbox.DomainsOf(email);

        // Assert
        Assert.Equal(expected.Split(' '), domains);
    }

    /// <summary>Negative: text without a domain gives no domain, so a count by mailbox finds nothing.</summary>
    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("anna@")]
    public void DomainsOf_NoDomain_GivesNone(string email)
    {
        // Act
        IReadOnlyList<string> domains = Mailbox.DomainsOf(email);

        // Assert
        Assert.Empty(domains);
    }

    #endregion
}
