
using KeyRotationPrototype.Security;

using Xunit;

namespace KeyRotationPrototype.Tests;

public class EncryptionKeyRingTests
{
    [Fact]
    public void AddKey_InvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange
        var keyRing = new EncryptionKeyRing("v1");
        byte[] invalidKey = new byte[10];

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => keyRing.AddKey("v1", invalidKey));

        // Assert
        Assert.Equal("key", exception.ParamName);
    }


    [Fact]
    public void AddKey_DuplicateKeyId_ThrowsArgumentException()
    {
        // Arrange
        var keyRing = new EncryptionKeyRing("v1");
        byte[] firstKey = new byte[32];
        byte[] secondKey = new byte[32];

        keyRing.AddKey("v1", firstKey);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => keyRing.AddKey("v1", secondKey));

        // Assert
        Assert.Equal("keyId", exception.ParamName);
    }


    [Fact]
    public void GetKey_UnknownKeyId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", new byte[32]);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(
            () => keyRing.GetKey("v2"));
    }

    [Fact]
    public void SetActiveKey_UnknownKeyId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var keyRing = new EncryptionKeyRing("v1");
        keyRing.AddKey("v1", new byte[32]);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(
            () => keyRing.SetActiveKey("v2"));

        // Verify that the active key remains unchanged.
        Assert.Equal("v1", keyRing.ActiveKeyId);
    }

    [Fact]
    public void GetKey_ModifyingReturnedKey_DoesNotChangeStoredKey()
    {
        // Arrange
        var keyRing = new EncryptionKeyRing("v1");
        byte[] originalKey = new byte[32];
        originalKey[0] = 42;

        keyRing.AddKey("v1", originalKey);

        // Act
        byte[] retrievedKey = keyRing.GetKey("v1");
        retrievedKey[0] = 99;

        // Assert
        byte[] storedKey = keyRing.GetKey("v1");

        Assert.Equal(42, storedKey[0]);
    }

}
