
using System.Security.Cryptography;

using KeyRotationPrototype.Security;

// Run the demonstration when the console application starts.
Program.RunDemo();

public partial class Program
{
    public static void RunDemo()
    {
        // Generate two random AES-256 keys for demonstration purposes.
        // These keys are temporary and are not stored or persisted.
        byte[] keyV1 = RandomNumberGenerator.GetBytes(32);
        byte[] keyV2 = RandomNumberGenerator.GetBytes(32);

        // Create a key ring and register both encryption keys.
        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", keyV1);
        keyRing.AddKey("v2", keyV2);

        var encryptor = new VersionedAesGcmEncryptor(keyRing);

        // Use fictional data to demonstrate encryption.
        string passportNumber = "TEST123456";

        Console.WriteLine("=== NFHotel Key Rotation Prototype ===");

        // Step 1: Encrypt data using the original v1 key.
        string encryptedV1 = encryptor.Encrypt(passportNumber);

        Console.WriteLine("\nStep 1: Encrypt with v1");
        Console.WriteLine($"Key version: {keyRing.ActiveKeyId}");

        // Step 2: Rotate the active encryption key to v2.
        keyRing.SetActiveKey("v2");

        Console.WriteLine("\nStep 2: Rotate encryption key");
        Console.WriteLine($"Active key version: {keyRing.ActiveKeyId}");

        // Step 3: Verify that old data can still be decrypted.
        string decryptedV1 = encryptor.Decrypt(encryptedV1);

        Console.WriteLine("\nStep 3: Decrypt old v1 data");
        Console.WriteLine($"Successful: {decryptedV1 == passportNumber}");

        // Step 4: Re-encrypt the old ciphertext using v2.
        string encryptedV2 = encryptor.ReEncrypt(encryptedV1);

        Console.WriteLine("\nStep 4: Re-encrypt existing data");
        Console.WriteLine($"Uses v2: {encryptedV2.StartsWith("v2:")}");

        // Step 5: Verify that the migrated data is still readable.
        string decryptedV2 = encryptor.Decrypt(encryptedV2);

        Console.WriteLine("\nStep 5: Verify migrated data");
        Console.WriteLine($"Successful: {decryptedV2 == passportNumber}");
    }
}
