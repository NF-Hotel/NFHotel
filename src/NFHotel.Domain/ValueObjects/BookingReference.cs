using System.Globalization;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The reference of a booking (BR-109, ADR-0049): a version 4 UUID that names the booking to the guest and to staff,
/// apart from the key of the booking, which stays inside the system.
/// </summary>
/// <remarks>
/// <see cref="Guid.NewGuid"/> makes a version 4 UUID with the variant of RFC 9562, whose 122 random bits come from the
/// secure random generator of the operating system. The reference names a booking and does not open one: the guest
/// sign-in asks for the email of the booking as well (ADR-0049 decision 2). Only the usual text form is read back, so
/// a number of the old kind (<c>FLZ-000123</c>) and every other form are unreadable (ADR-0049 decisions 1 and 5).
/// </remarks>
public static class BookingReference
{
    #region Fields

    /// <summary>The length of the text form: 32 hexadecimal digits and 4 hyphens.</summary>
    public const int TextLength = 36;

    // The format of 32 digits in five groups with hyphens, which .NET writes in lower case.
    private const string TextFormat = "D";

    // The version of a random UUID (RFC 9562, section 5.4).
    private const int RandomVersion = 4;

    // The variant of RFC 9562 has the bits 10 at the top of its nibble (section 4.1).
    private const int VariantMask = 0b1100;
    private const int RfcVariant = 0b1000;

    #endregion Fields

    #region Public methods

    /// <summary>Makes a new reference.</summary>
    /// <returns>A version 4 UUID with the variant of RFC 9562.</returns>
    public static Guid New() => Guid.NewGuid();

    /// <summary>Writes a reference in its usual form, for example <c>3f2504e0-4f89-41d3-9a0c-0305e82c3301</c>.</summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The 36 characters of the reference in lower case.</returns>
    public static string Format(Guid reference) => reference.ToString(TextFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a reference that a guest or a member of staff typed: the usual form of 36 characters in any letter case,
    /// with the spaces around it trimmed, of version 4 and the variant of RFC 9562.
    /// </summary>
    /// <param name="text">The text as typed; <see langword="null"/> is unreadable.</param>
    /// <param name="reference">The reference that was read, or <see cref="Guid.Empty"/>.</param>
    /// <returns><see langword="true"/> when the text is a reference in the usual form.</returns>
    public static bool TryParse(string? text, out Guid reference)
    {
        reference = Guid.Empty;
        if (text is null)
        {
            return false;
        }

        ReadOnlySpan<char> trimmed = text.AsSpan().Trim();
        if (!HasTheUsualForm(trimmed) || !Guid.TryParseExact(trimmed, TextFormat, out Guid read) || !IsRandom(read))
        {
            return false;
        }

        reference = read;
        return true;
    }

    #endregion Public methods

    #region Private methods

    // Exactly 36 characters: hexadecimal digits in groups of 8, 4, 4, 4 and 12, with a hyphen between them. The check
    // comes before the parser, so a long text is refused by its length and no other form of the parser is accepted.
    private static bool HasTheUsualForm(ReadOnlySpan<char> text)
    {
        if (text.Length != TextLength)
        {
            return false;
        }

        for (int index = 0; index < text.Length; index++)
        {
            bool isHyphen = index is 8 or 13 or 18 or 23;
            if (isHyphen ? text[index] != '-' : !char.IsAsciiHexDigit(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRandom(Guid value) => value.Version == RandomVersion && (value.Variant & VariantMask) == RfcVariant;

    #endregion Private methods
}
