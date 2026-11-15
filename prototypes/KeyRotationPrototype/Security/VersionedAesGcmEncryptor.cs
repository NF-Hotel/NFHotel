using System.Security.Cryptography;
using System.Text;

namespace KeyRotationPrototype.Security;

/// <summary>
/// Encrypts data using AES-GCM and includes the encryption
/// key version in the encrypted output.
/// </summary>
/// <remarks>
/// Prototype implementation for testing encryption key rotation.
/// Uses the existing NFHotel AES-GCM binary layout:
/// [Nonce][Authentication Tag][Ciphertext].
/// </remarks>
public sealed class VersionedAesGcmEncryptor
{
    // AES-GCM uses a 12-byte nonce and a 16-byte authentication tag.
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private readonly EncryptionKeyRing _keyRing;

    /// <summary>
    /// Initializes the encryptor with a key ring.
    /// </summary>
    /// <param name="keyRing">
    /// Key ring containing the available AES encryption keys.
    /// </param>
    public VersionedAesGcmEncryptor(EncryptionKeyRing keyRing)
    {
        _keyRing = keyRing
            ?? throw new ArgumentNullException(nameof(keyRing));
    }

    /// <summary>
    /// Encrypts plaintext using the currently active AES key.
    /// </summary>
    /// <param name="plainText">Text to encrypt.</param>
    /// <returns>
    /// A versioned encrypted value in the format:
    /// keyId:Base64(nonce + tag + ciphertext).
    /// </returns>
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        // Get the active key ID and its corresponding AES key.
        string keyId = _keyRing.ActiveKeyId;
        byte[] key = _keyRing.GetKey(keyId);

        // Convert plaintext into bytes before encryption.
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Allocate space for nonce, authentication tag and ciphertext.
        byte[] output = new byte[
            NonceLength + TagLength + plainBytes.Length];

        Span<byte> nonce = output.AsSpan(0, NonceLength);
        Span<byte> tag = output.AsSpan(NonceLength, TagLength);
        Span<byte> cipherText = output.AsSpan(NonceLength + TagLength);

        // Generate a fresh random nonce for every encryption.
        RandomNumberGenerator.Fill(nonce);

        // Encrypt the plaintext and generate an authentication tag.
        using var aes = new AesGcm(key, TagLength);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        // Prefix the encrypted data with the key version.
        return $"{keyId}:{Convert.ToBase64String(output)}";
    }


    /// <summary>
    /// Decrypts AES-GCM encrypted data using its key version.
    /// Supports legacy NFHotel values without a key ID.
    /// </summary>
    /// <param name="encryptedValue">
    /// Versioned ciphertext or legacy Base64 ciphertext.
    /// </param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="FormatException">
    /// Thrown if the encrypted value has an invalid format.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown if authentication fails during decryption.
    /// </exception>
    public string Decrypt(string encryptedValue)
    {
        ArgumentNullException.ThrowIfNull(encryptedValue);

        string keyId;
        string base64Data;

        int separatorIndex = encryptedValue.IndexOf(':');

        if (separatorIndex == -1)
        {
            // Legacy NFHotel values do not contain a key ID.
            // Use the original encryption key registered as "v1".
            keyId = "v1";
            base64Data = encryptedValue;
        }
        else
        {
            // Versioned values use the format "keyId:Base64".
            keyId = encryptedValue[..separatorIndex];
            base64Data = encryptedValue[(separatorIndex + 1)..];

            if (string.IsNullOrWhiteSpace(keyId))
            {
                throw new FormatException("Missing encryption key ID.");
            }
        }

        // Retrieve the AES key associated with this ciphertext.
        byte[] key = _keyRing.GetKey(keyId);

        byte[] input = Convert.FromBase64String(base64Data);

        // Validate the minimum size: nonce + authentication tag.
        if (input.Length < NonceLength + TagLength)
        {
            throw new FormatException("Encrypted value is too short.");
        }

        ReadOnlySpan<byte> nonce = input.AsSpan(0, NonceLength);
        ReadOnlySpan<byte> tag = input.AsSpan(NonceLength, TagLength);
        ReadOnlySpan<byte> cipherText =
            input.AsSpan(NonceLength + TagLength);

        byte[] plainBytes = new byte[cipherText.Length];

        // AES-GCM verifies the authentication tag during decryption.
        // A wrong key or modified ciphertext causes decryption to fail.
        using var aes = new AesGcm(key, TagLength);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Re-encrypts an existing encrypted value using
    /// the currently active encryption key.
    /// </summary>
    /// <param name="encryptedValue">
    /// Existing versioned or legacy encrypted value.
    /// </param>
    /// <returns>
    /// A new encrypted value using the active key version.
    /// </returns>
    /// <remarks>
    /// This prototype method performs in-memory re-encryption.
    /// It does not update database records or retire old keys.
    /// </remarks>
    public string ReEncrypt(string encryptedValue)
    {
        ArgumentNullException.ThrowIfNull(encryptedValue);

        // Decrypt the existing value using its original key.
        string plainText = Decrypt(encryptedValue);

        // Encrypt the plaintext using the currently active key.
        // AES-GCM generates a fresh nonce for the new ciphertext.
        return Encrypt(plainText);
    }

}
