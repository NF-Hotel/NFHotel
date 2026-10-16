using System.Text.Json;

namespace NFHotel.Proxmox.Tests;

/// <summary>Read-only checks against the real Proxmox VE server.</summary>
public sealed class ProxmoxApiTests : IDisposable
{
    private readonly PveSettings? _settings;
    private readonly PveClient? _client;

    public ProxmoxApiTests()
    {
        // Skipped runs (no PVE_HOST, not required) must not throw here.
        if (PveSettings.IsConfigured(Environment.GetEnvironmentVariable)
            || Environment.GetEnvironmentVariable("PVE_REQUIRE_LIVE") == "true")
        {
            _settings = PveSettings.FromEnvironment();
            _client = new PveClient(_settings);
        }
    }

    private PveClient Client => _client
        ?? throw new InvalidOperationException("Proxmox client was not created.");

    private PveSettings Settings => _settings
        ?? throw new InvalidOperationException("Proxmox settings were not loaded.");

    [LiveFact]
    public async Task GetVersion_WithValidToken_ReturnsVersion()
    {
        var data = await Client.GetDataAsync("version");

        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("version").GetString()));
    }

    [LiveFact]
    public async Task GetNodes_ConfiguredNode_IsListed()
    {
        var nodes = await Client.GetDataAsync("nodes");

        var names = nodes.EnumerateArray()
            .Select(node => node.GetProperty("node").GetString())
            .ToList();
        Assert.Contains(Settings.Node, names);
    }

    [LiveFact]
    public async Task GetNodeStatus_ConfiguredNode_ReportsUptime()
    {
        var data = await Client.GetDataAsync($"nodes/{Settings.Node}/status");

        var uptime = data.GetProperty("uptime");
        Assert.Equal(JsonValueKind.Number, uptime.ValueKind);
        Assert.True(uptime.GetInt64() > 0);
    }

    /// <inheritdoc />
    public void Dispose() => _client?.Dispose();
}