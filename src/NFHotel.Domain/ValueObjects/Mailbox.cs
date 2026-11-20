namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The mailbox that an email reaches, as the limits per email count it (MIL-088, ADR-0052 decision 6): the forms of an
/// address that reach one mailbox count once.
/// </summary>
/// <remarks>
/// The normal form of <see cref="TextNormalForm"/> without letter case, without a <c>+</c> and what follows it before the
/// <c>@</c>, and for <c>gmail.com</c> and <c>googlemail.com</c> without the dots before the <c>@</c> and with the domain
/// <c>gmail.com</c>. Gmail delivers these forms to one mailbox, and many providers deliver the <c>+</c> form there too;
/// where two forms reach two mailboxes, counting them as one only makes a limit stricter. The form is a key for
/// counting, never an address to send to.
/// </remarks>
public static class Mailbox
{
    #region Fields

    /// <summary>The domain of Gmail, whose mailboxes ignore the dots before the <c>@</c>.</summary>
    public const string GmailDomain = "gmail.com";

    /// <summary>The older domain of Gmail, counted as <see cref="GmailDomain"/>.</summary>
    public const string GooglemailDomain = "googlemail.com";

    private static readonly IReadOnlyList<string> GmailDomains = [GmailDomain, GooglemailDomain];

    #endregion Fields

    #region Public methods

    /// <summary>The form of an email that names its mailbox.</summary>
    /// <param name="email">The email.</param>
    /// <returns>The mailbox form, such as <c>anna@gmail.com</c> for <c>A.nna+trip@Googlemail.com</c>.</returns>
    public static string NormalFormOf(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        string text = TextNormalForm.Normalize(email).ToLowerInvariant();
        int at = text.LastIndexOf('@');
        if (at <= 0)
        {
            return text;
        }

        string local = text[..at];
        string domain = text[(at + 1)..];
        int plus = local.IndexOf('+', StringComparison.Ordinal);
        if (plus > 0)
        {
            local = local[..plus];
        }

        if (IsGmail(domain))
        {
            local = local.Replace(".", string.Empty, StringComparison.Ordinal);
            domain = GmailDomain;
        }

        return $"{local}@{domain}";
    }

    /// <summary>
    /// The domains whose addresses can reach the mailbox of an email: both domains of Gmail for a Gmail address, otherwise
    /// the domain of the email alone. A count by mailbox reads the addresses of these domains and compares their forms.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns>The domains in lower case; none when the email has no <c>@</c> with text before it.</returns>
    public static IReadOnlyList<string> DomainsOf(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        string text = TextNormalForm.Normalize(email).ToLowerInvariant();
        int at = text.LastIndexOf('@');
        if (at <= 0 || at == text.Length - 1)
        {
            return [];
        }

        string domain = text[(at + 1)..];
        return IsGmail(domain) ? GmailDomains : [domain];
    }

    #endregion Public methods

    #region Private methods

    private static bool IsGmail(string domain) =>
        string.Equals(domain, GmailDomain, StringComparison.Ordinal)
        || string.Equals(domain, GooglemailDomain, StringComparison.Ordinal);

    #endregion Private methods
}
