using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// The token of the link of a booking request and the keys that the database keeps instead of the token and the email
/// (MIL-088, ADR-0052 decisions 2 and 6).
/// </summary>
internal static class VerificationTokens
{
    #region Fields

    /// <summary>The random bytes of a token: 256 bits.</summary>
    public const int TokenBytes = 32;

    /// <summary>The characters of a token: 32 bytes in base64url without padding.</summary>
    public const int TokenLength = 43;

    // The characters of base64url, the form of base64 that is safe in an address.
    private static readonly SearchValues<char> Base64UrlCharacters =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

    #endregion Fields

    #region Public methods

    /// <summary>Makes a new token from the random generator of the operating system, and its hash.</summary>
    /// <returns>The token, 43 characters of base64url for the link, and its SHA-256 for the database.</returns>
    public static (string Token, string Hash) NewToken()
    {
        string token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        return (token, Sha256Hex(token));
    }

    /// <summary>The SHA-256 of a token as 64 lower-case hex digits, or <see langword="null"/> for a text that is no token.</summary>
    /// <param name="token">The text that came as a token.</param>
    /// <returns>The hash, or <see langword="null"/>, so a text of the wrong shape never reaches the database.</returns>
    public static string? HashOf(string? token) =>
        token is { Length: TokenLength } && !token.AsSpan().ContainsAnyExcept(Base64UrlCharacters) ? Sha256Hex(token) : null;

    /// <summary>The key of the mailbox of an email: the SHA-256 of <see cref="Mailbox.NormalFormOf"/>, as 64 lower-case hex digits.</summary>
    /// <param name="email">The email.</param>
    /// <returns>The key.</returns>
    public static string KeyOfEmail(string email) => Sha256Hex(Mailbox.NormalFormOf(email));

    #endregion Public methods

    #region Private methods

    private static string Sha256Hex(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    #endregion Private methods
}
