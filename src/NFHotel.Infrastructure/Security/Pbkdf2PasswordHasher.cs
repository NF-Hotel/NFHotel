using System.Security.Cryptography;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 with a random salt (ADR-0005). The hash is one string,
/// <c>pbkdf2-sha256$iterations$salt$hash</c>, so the cost can be raised later and old hashes still verify.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha256";
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const char Separator = '$';

    private readonly int _iterations;
    private readonly string _dummyHash;

    /// <summary>Creates the hasher.</summary>
    /// <param name="options">The cost of a hash.</param>
    public Pbkdf2PasswordHasher(StaffSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _iterations = options.PasswordIterations;

        // A well-formed hash of a password nobody knows, so a missing user is checked at the same cost.
        _dummyHash = Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltLength)));
    }

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, HashLength);
        return string.Join(Separator, Scheme, _iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <inheritdoc />
    public bool Verify(string? hash, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var stored = hash ?? _dummyHash;
        var matches = TryVerify(stored, password);
        return hash is not null && matches;
    }

    private static bool TryVerify(string stored, string password)
    {
        var parts = stored.Split(Separator);
        if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out var iterations) || iterations < 1)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}