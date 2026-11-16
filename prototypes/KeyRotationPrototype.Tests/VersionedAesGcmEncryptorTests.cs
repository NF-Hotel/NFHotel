using System.Security.Cryptography;

using KeyRotationPrototype.Security;

using Xunit;

namespace KeyRotationPrototype.Tests;

/// <summary>
/// Unit tests for AES-GCM encryption and decryption
/// using versioned encryption keys.
/// </summary>
public sealed class VersionedAesGcmEncryptorTests
{
    /// <summary>
    /// Verifies that encrypted data can be decrypted
    /// back to its original plaintext.
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        // Arrange: Generate a random AES-256 test key.
        byte[] key = RandomNumberGenerator.GetBytes(32);

        // Register the test key as the activee encryption key.
        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", key);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        // Use a fictional passport number.
        string originalPassportNumber = "TEST123456";

        // Act: Encrypt and then decrypt the passport number.
        string encrypted = encryptor.Encrypt(originalPassportNumber);
        string decrypted = encryptor.Decrypt(encrypted);

        // Assert: The decrypted value must match the original.
        Assert.Equal(originalPassportNumber, decrypted);
    }


    /// <summary>
    /// Verifies that rotating the active encryption key
    /// preserves access to data encrypted with older keys.
    /// </summary>
    [Fact]
    public void RotateKey_OldAndNewCiphertextsCanBeDecrypted()
    {
        // Arrange: Generate two independent AES-256 test keys.
        byte[] keyV1 = RandomNumberGenerator.GetBytes(32);
        byte[] keyV2 = RandomNumberGenerator.GetBytes(32);

        var keyRing = new EncryptionKeyRing("v1");

        // Register both key versions.
        keyRing.AddKey("v1", keyV1);
        keyRing.AddKey("v2", keyV2);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        // Encrypt data using the original active key (v1).
        string oldPassport = "OLD123456";
        string encryptedV1 = encryptor.Encrypt(oldPassport);

        // Act: Rotate the active encryption key to v2.
        keyRing.SetActiveKey("v2");

        // New data must now be encrypted using v2.
        string newPassport = "NEW654321";
        string encryptedV2 = encryptor.Encrypt(newPassport);

        // Decrypt both values after the rotation.
        string decryptedV1 = encryptor.Decrypt(encryptedV1);
        string decryptedV2 = encryptor.Decrypt(encryptedV2);

        // Assert: Verify that each ciphertext uses the correct key version.
        Assert.StartsWith("v1:", encryptedV1);
        Assert.StartsWith("v2:", encryptedV2);

        // Verify that both old and new data remain accessible.
        Assert.Equal(oldPassport, decryptedV1);
        Assert.Equal(newPassport, decryptedV2);
    }


    /// <summary>
    /// Verifies that legacy AES-GCM ciphertext without a key
    /// version prefix can still be decrypted after key rotation.
    /// </summary>
    [Fact]
    public void Decrypt_LegacyCiphertext_ReturnsOriginalPlaintext()
    {
        // Arrange: Generate separate AES-256 test keys.
        byte[] keyV1 = RandomNumberGenerator.GetBytes(32);
        byte[] keyV2 = RandomNumberGenerator.GetBytes(32);

        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", keyV1);
        keyRing.AddKey("v2", keyV2);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        string originalPassport = "LEGACY123456";

        // Simulate the legacy NFHotel format by removing
        // the key version prefix from a v1 encrypted value.
        string encryptedV1 = encryptor.Encrypt(originalPassport);
        string legacyCiphertext = encryptedV1["v1:".Length..];

        // Act: Activate v2 and decrypt the legacy ciphertext.
        keyRing.SetActiveKey("v2");

        string decrypted = encryptor.Decrypt(legacyCiphertext);

        // Assert: Legacy data must remain readable after rotation.
        Assert.DoesNotContain(":", legacyCiphertext);
        Assert.Equal(originalPassport, decrypted);
    }


    /// <summary>
    /// Verifies that AES-GCM rejects ciphertext that has
    /// been modified after encryption.
    /// </summary>
    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        // Arrange: Generate a random AES-256 test key.
        byte[] key = RandomNumberGenerator.GetBytes(32);

        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", key);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        // Encrypt a fictional passport number.
        string encrypted = encryptor.Encrypt("TEST123456");

        // Separate the key version from the encrypted data.
        string[] parts = encrypted.Split(':', 2);

        byte[] encryptedBytes = Convert.FromBase64String(parts[1]);

        // Modify one byte of the ciphertext.
        // The first 12 bytes contain the nonce and the next
        // 16 bytes contain the authentication tag.
        int ciphertextIndex = 12 + 16;
        encryptedBytes[ciphertextIndex] ^= 0x01;

        // Reconstruct the encrypted value with modified data.
        string tamperedValue =
            $"{parts[0]}:{Convert.ToBase64String(encryptedBytes)}";

        // Act & Assert: AES-GCM must reject the modified ciphertext.
        Assert.Throws<AuthenticationTagMismatchException>(
            () => encryptor.Decrypt(tamperedValue));
    }


    /// <summary>
    /// Verifies that decryption fails when the key registered
    /// for a ciphertext version is not the original AES key.
    /// </summary>
    [Fact]
    public void Decrypt_WithWrongKey_ThrowsAuthenticationTagMismatchException()
    {
        // Arrange: Generate two independent AES-256 test keys.
        byte[] correctKey = RandomNumberGenerator.GetBytes(32);
        byte[] wrongKey = RandomNumberGenerator.GetBytes(32);

        // Encrypt data using the correct v1 key.
        var originalKeyRing = new EncryptionKeyRing("v1");
        originalKeyRing.AddKey("v1", correctKey);

        var originalEncryptor =
            new VersionedAesGcmEncryptor(originalKeyRing);

        string encrypted = originalEncryptor.Encrypt("TEST123456");

        // Create a separate key ring with the wrong v1 key.
        var wrongKeyRing = new EncryptionKeyRing("v1");
        wrongKeyRing.AddKey("v1", wrongKey);

        var wrongEncryptor =
            new VersionedAesGcmEncryptor(wrongKeyRing);

        // Act & Assert: Decryption must fail authentication.
        Assert.Throws<AuthenticationTagMismatchException>(
            () => wrongEncryptor.Decrypt(encrypted));
    }


    /// <summary>
    /// Verifies that ciphertext encrypted with v1 can be
    /// re-encrypted using v2 without losing the original data.
    /// </summary>
    [Fact]
    public void ReEncrypt_V1Ciphertext_UsesV2KeyAndPreservesPlaintext()
    {
        // Arrange: Generate two independent AES-256 test keys.
        byte[] keyV1 = RandomNumberGenerator.GetBytes(32);
        byte[] keyV2 = RandomNumberGenerator.GetBytes(32);

        var keyRing = new EncryptionKeyRing("v1");

        // Register both encryption keys.
        keyRing.AddKey("v1", keyV1);
        keyRing.AddKey("v2", keyV2);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        string originalPassport = "TEST123456";

        // Encrypt the passport number using the original v1 key.
        string encryptedV1 = encryptor.Encrypt(originalPassport);

        // Act: Activate v2 and re-encrypt the existing ciphertext.
        keyRing.SetActiveKey("v2");

        string encryptedV2 = encryptor.ReEncrypt(encryptedV1);

        // Decrypt the newly encrypted value.
        string decrypted = encryptor.Decrypt(encryptedV2);

        // Assert: Verify that the encryption key version changed.
        Assert.StartsWith("v1:", encryptedV1);
        Assert.StartsWith("v2:", encryptedV2);

        // Verify that the new ciphertext differs from the original.
        Assert.NotEqual(encryptedV1, encryptedV2);

        // Verify that the original passport number is preserved.
        Assert.Equal(originalPassport, decrypted);
    }


}
