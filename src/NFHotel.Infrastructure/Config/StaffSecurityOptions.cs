namespace NFHotel.Infrastructure.Config;

/// <summary>
/// The settings of the staff sign-in (ADR-0005), read from the section <c>Staff</c>. The signing key is a secret
/// and comes from the environment file of the API, never from git.
/// </summary>
public sealed class StaffSecurityOptions
{
    /// <summary>The configuration section.</summary>
    public const string SectionName = "Staff";

    /// <summary>The shortest signing key that is accepted.</summary>
    public const int MinimumSigningKeyLength = 32;

    /// <summary>The OWASP figure for PBKDF2-HMAC-SHA256.</summary>
    public const int DefaultPasswordIterations = 600_000;

    /// <summary>The issuer claim of the tokens.</summary>
    public const string Issuer = "nfhotel-api";

    /// <summary>The audience claim of the tokens.</summary>
    public const string Audience = "nfhotel-staff";

    /// <summary>The secret that signs the tokens (<c>Staff:Token:SigningKey</c>).</summary>
    public required string SigningKey { get; init; }

    /// <summary>How long a token is valid, in minutes (<c>Staff:Token:LifetimeMinutes</c>); one shift by default.</summary>
    public int TokenLifetimeMinutes { get; init; } = 480;

    /// <summary>The cost of a password hash (<c>Staff:PasswordIterations</c>).</summary>
    public int PasswordIterations { get; init; } = DefaultPasswordIterations;

    /// <summary>Failed sign-ins of a user name that are allowed in the window (<c>Staff:Throttle:MaxFailures</c>).</summary>
    public int MaxFailures { get; init; } = 5;

    /// <summary>The length of the window in minutes (<c>Staff:Throttle:WindowMinutes</c>).</summary>
    public int ThrottleWindowMinutes { get; init; } = 15;
}