using System.Net;
using System.Text;
using System.Text.Json;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// A fake of the market analysis service that behaves like the three run paths of its contract: <c>POST .../runs</c> checks the
/// body against the <c>RunRequest</c> and <c>Booking</c> schemas of <c>market-contract.yaml</c> (read from the copy of the
/// contract, so the fake follows it), answers 202 with a run id, or 409 <c>run_active</c>; <c>GET .../runs/{id}</c> walks the
/// states of <see cref="States"/>; <c>GET .../runs/{id}/result</c> answers the result when the run is completed, 409
/// <c>result_not_ready</c> or <c>run_failed</c> otherwise, and 404 <c>run_not_found</c> for an id that is not known. Every call
/// needs the header <c>X-API-Key</c> when <see cref="RequiredKey"/> is set.
/// </summary>
internal sealed class FakeMarketAnalysisService : HttpMessageHandler
{
    private const string RunsPath = "/api/v1/market-analysis/runs";

    private const string MinimalResult =
        """{"schema_version":"1.1","result_id":"5b3f0e1a-0000-4000-8000-0000000000a1","generated_at":"2026-10-19T10:04:00.000Z","status":"completed","analyses":{},"notices":[]}""";

    private readonly MarketContractFile _contract = MarketContractFile.Load();
    private readonly List<string> _requests = [];
    private readonly List<string> _states = [];
    private Guid? _runId;
    private int _stateReads;

    /// <summary>The states that the successive asks for the state answer; the last one is repeated.</summary>
    public IReadOnlyList<string> States { get; init; } = ["queued", "running", "completed"];

    /// <summary>The <c>error_code</c> of a failed state.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>The result document that the result path answers.</summary>
    public string Result { get; init; } = MinimalResult;

    /// <summary>The key that every call must carry, or <c>null</c> when none is needed.</summary>
    public string? RequiredKey { get; init; }

    /// <summary>A status that the start answers instead of working, with <see cref="StartProblem"/> as its code.</summary>
    public HttpStatusCode? StartStatus { get; init; }

    /// <summary>The problem code of <see cref="StartStatus"/>.</summary>
    public string StartProblem { get; init; } = "invalid_request";

    /// <summary>The raw body that the start answers with a 202 instead of a state.</summary>
    public string? StartBody { get; init; }

    /// <summary>The raw body that the state path answers instead of a state.</summary>
    public string? StateBody { get; init; }

    /// <summary>A status that the state path answers instead of a state.</summary>
    public HttpStatusCode? StateStatus { get; init; }

    /// <summary>True when the result path answers a body that is not JSON.</summary>
    public bool ResultIsNotJson { get; init; }

    /// <summary>Another run is already queued or running, so a start answers 409.</summary>
    public bool Busy { get; init; }

    /// <summary>The state reads (1-based) that throw a connection error instead of answering.</summary>
    public IReadOnlySet<int> ThrowOnStateRead { get; init; } = new HashSet<int>();

    /// <summary>True when every call throws a connection error.</summary>
    public bool Throw { get; init; }

    /// <summary>True when every call throws a timeout (a cancellation that the caller did not ask for).</summary>
    public bool Timeout { get; init; }

    /// <summary>Every request as <c>METHOD path?query</c>, in order.</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <summary>The body of the last start, or <c>null</c> when none came.</summary>
    public string? StartedBody { get; private set; }

    /// <summary>The value of <c>X-API-Key</c> of the last request, or <c>null</c> when it had none.</summary>
    public string? LastKey { get; private set; }

    /// <summary>The host and port of the last request.</summary>
    public string LastAuthority { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        _requests.Add($"{request.Method.Method} {uri.AbsolutePath}{uri.Query}");
        LastAuthority = uri.Authority;
        LastKey = request.Headers.TryGetValues("X-API-Key", out var keys) ? keys.Single() : null;

        if (Timeout)
        {
            throw new TaskCanceledException("the call timed out");
        }

        if (Throw)
        {
            throw new HttpRequestException("connection refused");
        }

        if (RequiredKey is not null && LastKey != RequiredKey)
        {
            return Problem(HttpStatusCode.Unauthorized, "unauthorized");
        }

        var path = uri.AbsolutePath;
        if (request.Method == HttpMethod.Post && path == RunsPath)
        {
            return await StartAsync(request, cancellationToken);
        }

        if (request.Method == HttpMethod.Get && path.StartsWith(RunsPath + "/", StringComparison.Ordinal))
        {
            var rest = path[(RunsPath.Length + 1)..].Split('/');
            if (Guid.TryParse(rest[0], out var id))
            {
                return rest.Length switch
                {
                    1 => State(id),
                    2 when rest[1] == "result" => ResultOf(id),
                    _ => Problem(HttpStatusCode.NotFound, "not_found"),
                };
            }
        }

        return Problem(HttpStatusCode.NotFound, "not_found");
    }

    private async Task<HttpResponseMessage> StartAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        StartedBody = await request.Content!.ReadAsStringAsync(cancellationToken);

        if (StartStatus is { } status)
        {
            return Problem(status, StartProblem);
        }

        if (Busy)
        {
            return Problem(HttpStatusCode.Conflict, "run_active");
        }

        if (!BodyIsValid(StartedBody) || request.RequestUri!.Query != "?insights=true")
        {
            return Problem(HttpStatusCode.BadRequest, "invalid_request");
        }

        _runId = Guid.NewGuid();
        _stateReads = 0;

        return Json(HttpStatusCode.Accepted, StartBody ?? StateJson(_runId.Value, "queued", null));
    }

    private HttpResponseMessage State(Guid id)
    {
        if (id != _runId)
        {
            return Problem(HttpStatusCode.NotFound, "run_not_found");
        }

        var read = ++_stateReads;
        if (ThrowOnStateRead.Contains(read))
        {
            throw new HttpRequestException("connection reset");
        }

        if (StateStatus is { } status)
        {
            return Problem(status, "problem");
        }

        var state = States[Math.Min(read - 1, States.Count - 1)];
        _states.Add(state);

        return Json(HttpStatusCode.OK, StateBody ?? StateJson(id, state, state == "failed" ? ErrorCode : null));
    }

    private HttpResponseMessage ResultOf(Guid id)
    {
        if (id != _runId)
        {
            return Problem(HttpStatusCode.NotFound, "run_not_found");
        }

        return _states.LastOrDefault() switch
        {
            "completed" => ResultIsNotJson ? Json(HttpStatusCode.OK, "<html>") : Json(HttpStatusCode.OK, Result),
            "failed" => Problem(HttpStatusCode.Conflict, "run_failed"),
            _ => Problem(HttpStatusCode.Conflict, "result_not_ready"),
        };
    }

    // The body must be a RunRequest of the contract: only the properties of the schema, the required ones present, at least
    // one room and one booking, and every booking a Booking: only its properties, its required fields present.
    private bool BodyIsValid(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var allowed = _contract.Properties("RunRequest");
        var bookingFields = _contract.Properties("Booking");

        return root.ValueKind == JsonValueKind.Object
            && root.EnumerateObject().All(property => allowed.Contains(property.Name))
            && _contract.Required("RunRequest").All(name => root.TryGetProperty(name, out _))
            && root.GetProperty("room_count").GetInt32() >= 1
            && root.GetProperty("bookings").GetArrayLength() >= 1
            && root.GetProperty("bookings").EnumerateArray().All(booking =>
                booking.EnumerateObject().All(property => bookingFields.Contains(property.Name))
                && _contract.Required("Booking").All(name => booking.TryGetProperty(name, out _)))
            && (!root.TryGetProperty("period", out var period)
                || (period.TryGetProperty("first_date", out _) && period.TryGetProperty("last_date", out _)));
    }

    private static string StateJson(Guid id, string state, string? errorCode)
    {
        var fields = new Dictionary<string, object>
        {
            ["run_id"] = id,
            ["state"] = state,
            ["created_at"] = "2026-10-29T10:00:00Z",
        };
        if (errorCode is not null)
        {
            fields["error_code"] = errorCode;
        }

        return JsonSerializer.Serialize(fields);
    }

    private static HttpResponseMessage Problem(HttpStatusCode status, string code) =>
        Json(status, JsonSerializer.Serialize(new { code, message = "a message that is not read" }));

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
