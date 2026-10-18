namespace NFHotel.Application.Ports;

/// <summary>Hashes and checks passwords with a slow, salted algorithm (ADR-0005). Implemented in Infrastructure.</summary>
public interface IPasswordHasher
{
    /// <summary>Makes the self-describing hash of a password.</summary>
    /// <param name="password">The password.</param>
    /// <returns>The hash, with the algorithm, the cost and the salt in it.</returns>
    string Hash(string password);

    /// <summary>
    /// Checks a password against a hash in constant time. A missing hash is checked against a fixed dummy, so an
    /// unknown user costs the same time as a wrong password.
    /// </summary>
    /// <param name="hash">The stored hash, or <c>null</c> when the user does not exist.</param>
    /// <param name="password">The password that was typed.</param>
    /// <returns>True when the password matches; always false for a missing hash.</returns>
    bool Verify(string? hash, string password);
}