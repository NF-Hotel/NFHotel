namespace NFHotel.Proxmox.Tests;

/// <summary>Checks that settings load with defaults, name what is missing and never expose the secret.</summary>
public sealed class PveSettingsTests
{
    private const string Secret = "top-secret-value";

    private static readonly Dictionary<string, string> Complete = new()
    {
        ["PVE_HOST"] = "pve.test",
        ["PVE_NODE"] = "pve",
        ["PVE_API_TOKEN_ID"] = "tester@pve!ci",
        ["PVE_API_TOKEN_SECRET"] = Secret,
    };

    private static string? Get(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out var value) ? value : null;

    [Fact]
    public void Load_OnlyRequiredValues_AppliesDefaults()
    {
        var settings = PveSettings.Load(name => Get(Complete, name));

        Assert.Equal(8006, settings.Port);
        Assert.Equal(TimeSpan.FromSeconds(15), settings.Timeout);
        Assert.False(settings.VerifyTls);
        Assert.Equal("https://pve.test:8006/", settings.BaseAddress.ToString());
    }

    [Fact]
    public void Load_MissingValues_NamesThemWithoutSecret()
    {
        var partial = new Dictionary<string, string> { ["PVE_API_TOKEN_SECRET"] = Secret };

        var error = Assert.Throws<InvalidOperationException>(
            () => PveSettings.Load(name => Get(partial, name)));

        Assert.Contains("PVE_HOST", error.Message);
        Assert.Contains("PVE_NODE", error.Message);
        Assert.Contains("PVE_API_TOKEN_ID", error.Message);
        Assert.DoesNotContain(Secret, error.Message);
    }

    [Fact]
    public void Load_InvalidPort_NamesTheVariable()
    {
        var values = new Dictionary<string, string>(Complete) { ["PVE_PORT"] = "abc" };

        var error = Assert.Throws<InvalidOperationException>(
            () => PveSettings.Load(name => Get(values, name)));

        Assert.Contains("PVE_PORT", error.Message);
    }

    [Fact]
    public void ToString_Always_OmitsSecret()
    {
        var settings = PveSettings.Load(name => Get(Complete, name));

        Assert.DoesNotContain(Secret, settings.ToString());
    }
}
