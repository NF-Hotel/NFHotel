namespace NFHotel.Application.Ports;

/// <summary>Counts failed sign-ins per user name so that guessing a password is stopped (ADR-0005).</summary>
public interface ILoginThrottle
{
    /// <summary>Tells whether the key has used up its attempts in the current window.</summary>
    /// <param name="key">The normalised user name.</param>
    /// <returns>True when the next attempt must be refused whatever the password.</returns>
    bool IsBlocked(string key);

    /// <summary>Counts one failed attempt.</summary>
    /// <param name="key">The normalised user name.</param>
    void RecordFailure(string key);

    /// <summary>
    /// Counts one attempt unless the key has used up its attempts in the current window, as one atomic step, so that
    /// attempts sent at the same time cannot all pass the check before any of them is counted (MIL-018). The attempt
    /// counts as a failure until <see cref="Reset"/> clears the key after a success.
    /// </summary>
    /// <param name="key">The key the attempts are counted for.</param>
    /// <returns>True when the attempt is counted and may go on; false when it must be refused, and nothing is counted.</returns>
    bool TryCountAttempt(string key);

    /// <summary>Forgets the failures after a successful sign-in.</summary>
    /// <param name="key">The normalised user name.</param>
    void Reset(string key);
}
