using System.Security.Cryptography;
using System.Text;

using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// AES-GCM encryption of single fields (B-09).
/// </summary>
/// <remarks>
/// The format is <c>base64(nonce ‖ tag ‖ ciphertext)</c> with fixed lengths for the first two
/// parts, so a value can be decrypted without side-channel metadata.
/// <para>
/// AEAD is chosen over AES-CBC, because the tag turns changed ciphertext into a
/// <see cref="CryptographicException"/> instead of into nonsense plain text. The nonce is drawn
/// at random per encryption, which is exactly why the field must never be indexed:
/// the same passport number gives different ciphertext every time.
/// </para>
/// <para>
/// The type is thread-safe: the key is immutable, and <see cref="AesGcm"/> is instantiated per
/// operation. It can therefore be registered as a singleton.
/// </para>
/// </remarks>
public sealed class AesGcmStringEncryptor : IStringEncryptor
{
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private static readonly int[] ValidKeyLengths = [16, 24, 32];

    private readonly byte[] _key;

    /// <summary>
    /// Creates the encryption service.
    /// </summary>
    /// <param name="options">The encryption settings. The key must be base64-encoded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the key is missing, is not valid base64 or does not decode to 16, 24
    /// or 32 bytes. The error is thrown at start-up, not on first use (fail fast).
    /// </exception>
    public AesGcmStringEncryptor(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _key = DecodeKey(options.PassportKey);
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var output = new byte[NonceLength + TagLength + plainBytes.Length];

        var nonce = output.AsSpan(0, NonceLength);
        var tag = output.AsSpan(NonceLength, TagLength);
        var cipherText = output.AsSpan(NonceLength + TagLength);

        RandomNumberGenerator.Fill(nonce);

        using var aesGcm = new AesGcm(_key, TagLength);
        aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

        return Convert.ToBase64String(output);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var input = DecodeCipherText(cipherText);

        var nonce = input.AsSpan(0, NonceLength);
        var tag = input.AsSpan(NonceLength, TagLength);
        var payload = input.AsSpan(NonceLength + TagLength);
        var plainBytes = new byte[payload.Length];

        using var aesGcm = new AesGcm(_key, TagLength);
        aesGcm.Decrypt(nonce, payload, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>Decodes and validates the key. Fails at start-up rather than on first use.</summary>
    private static byte[] DecodeKey(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                $"The encryption key '{EncryptionOptions.SectionName}:{EncryptionOptions.PassportKeyName}' is missing. "
                + "The app refuses to start without it, so passport numbers cannot end up in plain text (B-09).");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"The encryption key '{EncryptionOptions.SectionName}:{EncryptionOptions.PassportKeyName}' "
                + "is not valid base64.",
                exception);
        }

        if (!ValidKeyLengths.Contains(key.Length))
        {
            throw new InvalidOperationException(
                $"The encryption key must decode to 16, 24 or 32 bytes, but was {key.Length}.");
        }

        return key;
    }

    /// <summary>Decodes the ciphertext and rejects values that are too short to carry nonce and tag.</summary>
    private static byte[] DecodeCipherText(string cipherText)
    {
        byte[] input;

        try
        {
            input = Convert.FromBase64String(cipherText);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The encrypted value is not valid base64.", exception);
        }

        if (input.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("The encrypted value is too short to hold the nonce and tag.");
        }

        return input;
    }
}
