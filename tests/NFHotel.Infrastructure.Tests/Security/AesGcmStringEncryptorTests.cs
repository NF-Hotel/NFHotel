using System.Security.Cryptography;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Security;


/// <summary>
/// Tests for <see cref="AesGcmStringEncryptor"/> class.
/// </summary>
public class AesGcmStringEncryptorTests
{

    // Is a round-trip test that ensures encryption and decryption work correctly.
    // decrypts back to the original value.
    // [Fact] marks the method as an xUnit test.
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlainText()
    {
        // Creates a byte array of 32.
        var key = new byte[32];

        // Creates the settings for the encryption.
        var options = new EncryptionOptions
        {
            // Converts the AES key from byte[] to a Base64 string,
            // because PassportKey is stored/expected as text.
            PassportKey = Convert.ToBase64String(key)
        };

        // Creates the class we want to test,
        // gives it the encryption settings with the AES key.
        var encryptor = new AesGcmStringEncryptor(options);


        // Testdata
        const string plainText = "abcd1234";

        // Krypterer testdataene.
        var cipherText = encryptor.Encrypt(plainText);
        var decryptedText = encryptor.Decrypt(cipherText);

        // Asserts
        Assert.Equal(plainText, decryptedText);
    }


    // Test to ensure that encrypting the same text twice gives different results.
    [Fact]
    public void Encrypt_SamePlainTextTwice_ReturnsDifferentCipherTexts()
    {
        // Arrange 
        var key = new byte[32];

        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Creates the class we want to test,
        // gives it the encryption settings with the AES key.
        var encryptor = new AesGcmStringEncryptor(options);

        const string plainText = "abcd1234";

        // Act
        var cipherText1 = encryptor.Encrypt(plainText);
        var cipherText2 = encryptor.Encrypt(plainText);


        // Asserts
        Assert.NotEqual(cipherText1, cipherText2);

    }


    // Test to ensure that tampered encrypted data cannot be decrypted.
    // This is important to ensure data integrity and security.
    [Fact]
    public void Decrypt_TamperedCipherText_ThrowsCryptographicException()
    {
        // Arrange
        var key = new byte[32];

        // Creates the settings for the encryption.
        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Creates the class we want to test,
        var encryptor = new AesGcmStringEncryptor(options);

        const string plainText = "abcd1234";


        var cipherText = encryptor.Encrypt(plainText);

        // Tampers with the encrypted text by changing a single byte.
        // Converts the encrypted Base64 string to bytes
        var tamperedBytes = Convert.FromBase64String(cipherText);

        // Changes one bit in the last byte to deliberately tamper with
        // the encrypted data and test whether AES-GCM detects the change.
        tamperedBytes[^1] ^= 1;


        // Converts the tampered bytes back to a Base64 string,
        // so the changed ciphertext can be used in the test.
        var tamperedCipherText = Convert.ToBase64String(tamperedBytes);



        // Act and Assert
        // Expects a CryptographicException to be thrown when we try to decrypt the tampered encrypted text.
        Assert.ThrowsAny<CryptographicException>(
            () => encryptor.Decrypt(tamperedCipherText));
    }






    // Test to ensure that a missing encryption key is rejected.
    [Fact]
    public void Constructor_MissingPassportKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            PassportKey = string.Empty
        };



        // Act and Assert
        Assert.Throws<InvalidOperationException>(
            () => new AesGcmStringEncryptor(options));
    }




    // Test to ensure that an invalid Base64 encryption key is rejected.
    [Fact]
    public void Constructor_InvalidBase64Key_ThrowsInvalidOperationException()
    {


        // Arrange
        var options = new EncryptionOptions
        {
            PassportKey = "not-valid-base64!"
        };


        // Act and Assert
        Assert.Throws<InvalidOperationException>(
            () => new AesGcmStringEncryptor(options));
    }




    // Test to ensure that a key with an invalid length is rejected.
    [Fact]
    public void Constructor_InvalidKeyLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var key = new byte[10];


        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Act and Assert
        Assert.Throws<InvalidOperationException>(
            () => new AesGcmStringEncryptor(options));
    }



    // Test for at sikre, at ugyldig Base64-ciphertext afvises.
    [Fact]
    public void Decrypt_InvalidBase64_ThrowsCryptographicException()
    {
        // Arrange
        var key = new byte[32];

        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        var encryptor = new AesGcmStringEncryptor(options);

        const string invalidCipherText = "not-valid-base64!";

        // Act and Assert
        Assert.Throws<CryptographicException>(
            () => encryptor.Decrypt(invalidCipherText));
    }



}