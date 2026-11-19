using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of an email address (MIL-086 decision 2, ADR-0050): one plain address that <see cref="MailAddress"/>
/// reads as it is, with no display name; a local part of at most 64 characters without white space, control
/// characters, quotes, back quotes, angle brackets or backslashes; and a domain of two or more labels of letters,
/// digits and hyphens. The length of the whole address is the rule of <see cref="StringLengthAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EmailFormatAttribute : ValidationAttribute
{
    /// <summary>The most characters of the part before the at sign (RFC 5321, 4.5.3.1.1).</summary>
    public const int MaxLocalPartLength = 64;

    /// <summary>The most characters of one label of the domain (RFC 1035, 2.3.4).</summary>
    public const int MaxLabelLength = 63;

    /// <summary>Tells whether a text is one plain email address.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/> when the text is one address of the allowed shape.</returns>
    public static bool Accepts(string? value)
    {
        if (string.IsNullOrEmpty(value)
            || !MailAddress.TryCreate(value, out var address)
            || !string.Equals(address.Address, value, StringComparison.Ordinal)
            || !string.IsNullOrEmpty(address.DisplayName))
        {
            return false;
        }

        return IsLocalPart(address.User) && IsDomain(address.Host);
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is null || (value is string text && Accepts(text));

    private static bool IsLocalPart(string local) =>
        local.Length is > 0 and <= MaxLocalPartLength
        && local.All(character => !char.IsWhiteSpace(character)
            && !char.IsControl(character)
            && character is not ('"' or '`' or '<' or '>' or '\\'));

    private static bool IsDomain(string host)
    {
        var labels = host.Split('.');

        return labels.Length >= 2 && labels.All(IsLabel);
    }

    private static bool IsLabel(string label) =>
        label.Length is > 0 and <= MaxLabelLength
        && label[0] != '-'
        && label[^1] != '-'
        && label.All(character => char.IsLetter(character) || char.IsAsciiDigit(character) || character == '-');
}
