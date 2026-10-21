namespace NFHotel.Proxmox.Tests;

/// <summary>
/// Connection settings for the Proxmox VE API, read from <c>PVE_*</c>
/// environment variables (see <c>pve.env.example</c>).
/// </summary>
public sealed class PveSettings
{
    private PveSettings(
        string host,
        int port,
        string node,
        string tokenId,
        string tokenSecret,
        bool verifyTls,
        TimeSpan timeout)
    {
        Host = host;
        Port = port;
        Node = node;
        TokenId = tokenId;
        TokenSecret = tokenSecret;
        VerifyTls = verifyTls;
        Timeout = timeout;
    }

    public string Host { get; }

    public int Port { get; }

    public string Node { get; }

    public string TokenId { get; }

    public string TokenSecret { get; }

    public bool VerifyTls { get; }

    public TimeSpan Timeout { get; }

    public Uri BaseAddress => new($"https://{Host}:{Port}/");

    /// <summary>True when the host is configured, so live tests can run.</summary>
    public static bool IsConfigured(Func<string, string?> getVariable) =>
        !string.IsNullOrWhiteSpace(getVariable("PVE_HOST"));

    /// <summary>
    /// Reads the settings. Throws <see cref="InvalidOperationException"/>
    /// naming every missing or invalid variable; never includes the secret.
    /// </summary>
    public static PveSettings Load(Func<string, string?> getVariable)
    {
        var problems = new List<string>();

        string Required(string name)
        {
            var value = getVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                problems.Add($"{name} is missing");
                return string.Empty;
            }

            return value.Trim();
        }

        int Number(string name, int fallback)
        {
            var value = getVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (int.TryParse(value, out var number) && number > 0)
            {
                return number;
            }

            problems.Add($"{name} must be a positive whole number");
            return fallback;
        }

        var host = Required("PVE_HOST");
        var node = Required("PVE_NODE");
        var tokenId = Required("PVE_API_TOKEN_ID");
        var tokenSecret = Required("PVE_API_TOKEN_SECRET");
        var port = Number("PVE_PORT", 8006);
        var timeoutSeconds = Number("PVE_TIMEOUT_SECONDS", 15);
        var verifyTls = string.Equals(
            getVariable("PVE_VERIFY_TLS")?.Trim(),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Proxmox settings are incomplete (fill pve.env): "
                + string.Join("; ", problems));
        }

        return new PveSettings(
            host,
            port,
            node,
            tokenId,
            tokenSecret,
            verifyTls,
            TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>Settings from the process environment.</summary>
    public static PveSettings FromEnvironment() =>
        Load(Environment.GetEnvironmentVariable);

    /// <inheritdoc />
    public override string ToString() =>
        $"{TokenId}@{Host}:{Port} node {Node}";
}
