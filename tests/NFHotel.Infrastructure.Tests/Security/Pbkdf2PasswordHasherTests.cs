using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>The password hash of ADR-0005: salted, self-describing, and checked in constant time.</summary>
public sealed class Pbkdf2PasswordHasherTests
{
    private const string Password = "correct horse battery staple";

    #region Hash and verify

    /// <summary>Positive: the hash of a password verifies with that password.</summary>
    [Fact]
    public void Verify_RightPassword_ReturnsTrue()
    {
        // Arrange
        var hasher = Build();

        // Act
        var matches = hasher.Verify(hasher.Hash(Password), Password);

        // Assert
        Assert.True(matches);
    }

    /// <summary>Negative: another password, another case, and an extra character do not verify.</summary>
    [Theory]
    [InlineData("correct horse battery stapl")]
    [InlineData("Correct horse battery staple")]
    [InlineData("correct horse battery staple ")]
    [InlineData("")]
    public void Verify_WrongPassword_ReturnsFalse(string typed)
    {
        // Arrange
        var hasher = Build();
        var hash = hasher.Hash(Password);

        // Act
        var matches = hasher.Verify(hash, typed);

        // Assert
        Assert.False(matches);
    }

    /// <summary>The hash is salted: the same password gives two different hashes, and both verify.</summary>
    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashesThatBothVerify()
    {
        // Arrange
        var hasher = Build();

        // Act
        var first = hasher.Hash(Password);
        var second = hasher.Hash(Password);

        // Assert
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify(first, Password));
        Assert.True(hasher.Verify(second, Password));
    }

    /// <summary>The hash names its algorithm and cost and does not contain the password.</summary>
    [Fact]
    public void Hash_Result_NamesTheSchemeAndTheCostAndHidesThePassword()
    {
        // Arrange
        var hasher = Build(iterations: 1234);

        // Act
        var hash = hasher.Hash(Password);

        // Assert
        Assert.StartsWith("pbkdf2-sha256$1234$", hash, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
        Assert.True(hash.Length <= 200, "The hash must fit the column.");
    }

    /// <summary>Equivalence: a hash made at another cost still verifies, so the cost can be raised later without locking anyone out.</summary>
    [Fact]
    public void Verify_HashMadeAtAnotherCost_ReturnsTrue()
    {
        // Arrange
        var oldHash = Build(iterations: 500).Hash(Password);

        // Act
        var matches = Build(iterations: 2000).Verify(oldHash, Password);

        // Assert
        Assert.True(matches);
    }

    #endregion

    #region Bad input

    /// <summary>Negative: no hash (the user does not exist) never verifies, even for an empty password.</summary>
    [Theory]
    [InlineData(Password)]
    [InlineData("")]
    public void Verify_MissingHash_ReturnsFalse(string typed)
    {
        // Arrange
        var hasher = Build();

        // Act
        var matches = hasher.Verify(null, typed);

        // Assert
        Assert.False(matches);
    }

    /// <summary>Negative: a stored value that is not a hash of this scheme is refused, not thrown at.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("pbkdf2-sha256$abc$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$not base64!$AAAA")]
    [InlineData("md5$1000$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$AAAA")]
    public void Verify_MalformedHash_ReturnsFalse(string stored)
    {
        // Arrange
        var hasher = Build();

        // Act
        var matches = hasher.Verify(stored, Password);

        // Assert
        Assert.False(matches);
    }

    /// <summary>Negative: a hash with one changed character does not verify.</summary>
    [Fact]
    public void Verify_TamperedHash_ReturnsFalse()
    {
        // Arrange
        var hasher = Build();
        var hash = hasher.Hash(Password);
        var tampered = hash[..^3] + (hash[^3] == 'A' ? "B" : "A") + hash[^2..];

        // Act
        var matches = hasher.Verify(tampered, Password);

        // Assert
        Assert.False(matches);
    }

    /// <summary>Negative: a missing password is a programming error.</summary>
    [Fact]
    public void Hash_NullPassword_Throws()
    {
        // Arrange
        var hasher = Build();

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => hasher.Hash(null!));
    }

    #endregion

    #region Helpers

    private static Pbkdf2PasswordHasher Build(int iterations = 1000) =>
        new(new StaffSecurityOptions { SigningKey = "unused", PasswordIterations = iterations });

    #endregion
}
