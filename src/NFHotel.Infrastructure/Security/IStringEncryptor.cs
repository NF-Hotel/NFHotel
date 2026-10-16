namespace NFHotel.Infrastructure.Security;

/// <summary>
/// Symmetric encryption of single fields at rest (B-09, D-04).
/// </summary>
/// <remarks>
/// The interface lives in Infrastructure and not in Application: encryption is a
/// persistence detail. The domain knows only plain text — <c>Guest.PassportNumber</c> does not know
/// that the field is encrypted, and should not.
/// <para>
/// The implementation is randomised (a new nonce per encryption), so the same plain text gives
/// different ciphertext every time. The consequence is absolute: the field can be neither indexed,
/// sorted nor searched — not even with equality.
/// </para>
/// </remarks>
public interface IStringEncryptor
{
    /// <summary>
    /// Encrypts a plain text value.
    /// </summary>
    /// <param name="plainText">Klarteksten.</param>
    /// <returns>Ciphertext in a self-describing, base64-encoded format.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a ciphertext produced by <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="cipherText">Cifferteksten.</param>
    /// <returns>The original plain text.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the value is damaged, truncated or encrypted with a different key.
    /// </exception>
    string Decrypt(string cipherText);
}