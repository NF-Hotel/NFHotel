using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NFHotel.Proxmox.Tests;

/// <summary>A GET-only client for the Proxmox VE JSON API.</summary>
public sealed class PveClient : IDisposable
{
    private readonly HttpClient _http;

    public PveClient(PveSettings settings)
    {
        var inner = new HttpClientHandler();
        if (!settings.VerifyTls)
        {
            inner.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        _http = new HttpClient(new ReadOnlyHandler(inner))
        {
            BaseAddress = settings.BaseAddress,
            Timeout = settings.Timeout,
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "PVEAPIToken", $"{settings.TokenId}={settings.TokenSecret}");
    }

    /// <summary>GETs <c>api2/json/{path}</c> and returns its <c>data</c> element.</summary>
    public async Task<JsonElement> GetDataAsync(
        string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api2/json/{path}", ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return body.GetProperty("data");
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();
}
