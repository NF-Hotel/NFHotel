using System;
using System.Security.Cryptography;

using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Security;


/// <summary>
/// Tests for <see cref="AesGcmStringEncryptor"/> class.
/// </summary>
public class AesGcmStringEncryptorTests
{

    // Er en roundtrip-test, der sikrer at kryptering og dekryptering fungerer korrekt.
    // dekrypteres tilbage til den oprindelige værdi.
    // [Fact] markerer metoden som en xUnit-test.
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlainText()
    {
        // Opretter et byte-array på 32.
        var key = new byte[32];

        // Opretter indstillingerne til krypteringen.
        var options = new EncryptionOptions
        {
            // Konverterer AES-nøglen fra byte[] til en Base64-string,
            // fordi PassportKey gemmes/forventes som tekst.
            PassportKey = Convert.ToBase64String(key)
        };

        // Opretter den klasse, som vi vil teste,
        // giver den krypteringsindstillingerne med AES-nøglen.
        var encryptor = new AesGcmStringEncryptor(options);


        // Testdata
        const string plainText = "abcd1234";

        // Krypterer testdataene.
        var cipherText = encryptor.Encrypt(plainText);
        var decryptedText = encryptor.Decrypt(cipherText);

        // Asserts
        Assert.Equal(plainText, decryptedText);
    }


    // Test for at sikre, at kryptering af samme tekst to gange giver forskellige resultater.
    [Fact]
    public void Encrypt_SamePlainTextTwice_ReturnsDifferentCipherTexts()
    {
       // Arrange 
       var key = new byte[32];

        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Opretter den klasse, som vi vil teste,
        // giver den krypteringsindstillingerne med AES-nøglen.
        var encryptor = new AesGcmStringEncryptor(options);

        const string plainText = "abcd1234";

        // Act
        var cipherText1 = encryptor.Encrypt(plainText);
        var cipherText2 = encryptor.Encrypt(plainText);


        // Asserts
        Assert.NotEqual(cipherText1, cipherText2);

    }


    // Test for at sikre, at manipuleret krypteret data ikke kan dekrypteres.
    // Dette er vigtigt for at sikre dataintegritet og sikkerhed.
    [Fact]
    public void Decrypt_TamperedCipherText_ThrowsCryptographicException()
    {
        // Arrange
        var key = new byte[32];

        // Opretter indstillingerne til krypteringen.
        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Opretter den klasse, som vi vil teste,
        var encryptor = new AesGcmStringEncryptor(options);

        const string plainText = "abcd1234";


        var cipherText = encryptor.Encrypt(plainText);

        // Manipulerer den krypterede tekst ved at ændre et enkelt byte.
        // Konverterer den krypterede Base64-string til bytes
        var tamperedBytes = Convert.FromBase64String(cipherText);

        // Ændrer én bit i den sidste byte for med vilje at manipulere
        // den krypterede data og teste om AES-GCM opdager ændringen.
        tamperedBytes[^1] ^= 1;


        // Konverterer de manipulerede bytes tilbage til en Base64-string,
        // så den ændrede ciphertext kan bruges i testen.
        var tamperedCipherText = Convert.ToBase64String(tamperedBytes);



        // Act og Assert
        // Forventer, at en CryptographicException kastes, når vi forsøger at dekryptere den manipulerede krypterede tekst.
          Assert.ThrowsAny<CryptographicException>(
              () => encryptor.Decrypt(tamperedCipherText));
    }






    // Test for at sikre, at en manglende krypteringsnøgle afvises.
    [Fact]
    public void Constructor_MissingPassportKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            PassportKey = string.Empty
        };



        // Act og Assert
        Assert.Throws<InvalidOperationException>(
            () => new AesGcmStringEncryptor(options));
    }




    // Test for at sikre, at en ugyldig Base64-krypteringsnøgle afvises.
    [Fact]
    public void Constructor_InvalidBase64Key_ThrowsInvalidOperationException()
    {


        // Arrange
        var options = new EncryptionOptions
        {
            PassportKey = "not-valid-base64!"
        };


        // Act og Assert
        Assert.Throws<InvalidOperationException>(
            () => new AesGcmStringEncryptor(options));
    }




    // Test for at sikre, at en nøgle med ugyldig længde afvises.
    [Fact]
    public void Constructor_InvalidKeyLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var key = new byte[10];

        
        var options = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(key)
        };

        // Act og Assert
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

        // Act og Assert
        Assert.Throws<CryptographicException>(
            () => encryptor.Decrypt(invalidCipherText));
    }



}



