using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NFHotel.Api.Contracts;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// Every endpoint that the API maps, taken from the API itself, is sent hostile input (MIL-077 task 1): a body that
/// is not JSON, JSON of the wrong type, no body, null fields, a string of one megabyte, numbers above
/// <see cref="int.MaxValue"/> and below zero, dates out of range, and a NUL character, a right-to-left override and a
/// lone surrogate in text, in the body, the route and the query. An endpoint refuses each hostile value that it reads
/// with an answer in the 400 range, and no answer is a fault (500) or shows a detail of the server.
/// </summary>
/// <remarks>
/// A new endpoint is covered without a change here: the requests are built from the routes, the parameters and the
/// JSON shape of the body that the API itself describes. Each request carries the API token and the staff token of
/// a new Manager, who holds every permission, so the permission checks let the input through and the write limit
/// per member of staff (30 a minute) never answers in place of the endpoint. Each hostile request changes one value
/// of a plausible request that the endpoint accepts, so that the hostile value is what the endpoint refuses; an
/// endpoint that reads a body or a query and refuses its plausible request is listed in
/// <see cref="RefusedPlausibleRequests"/> with the reason.
/// </remarks>
public sealed class HostileInputTests : IClassFixture<ApiFactory>
{
    private const int OneMegabyte = 1024 * 1024;
    private const int MaxDepth = 4;
    private const string PlausibleValues = "plausible values";
    private const string IgnoredInput = "input it does not read";
    private const string PlausibleGuid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string KnownPassword = "Known-staff-passphrase-1";
    private const string NewPassword = "Hostile-input-passphrase-2";
    private const string NulCharacter = "a NUL character";

    // A lone surrogate cannot be written by the JSON writer, so the body carries this marker and the marker is
    // replaced by the escape \ud800 in the text that is sent.
    private const string LoneSurrogateMarker = "hostile-lone-surrogate-marker";

    private static readonly string Megabyte = new('a', OneMegabyte);

    // The first day of the plausible stays; each request books its own three days after it, so that no two overlap.
    private static readonly DateOnly FirstDay = new(2030, 6, 10);

    private static readonly (string Kind, string Value)[] HostileText =
    [
        (NulCharacter, "a\u0000b"),
        ("a right-to-left override", "\u202Egnp.exe"),
        ("a lone surrogate", LoneSurrogateMarker),
        ("a string of one megabyte", Megabyte),
    ];

    // The same text as it arrives in a URL, percent-encoded. A URL cannot carry a lone surrogate: UTF-8 has no form
    // for one, and the server keeps bytes that are not UTF-8 (such as ED A0 80) percent-encoded, so the value is the
    // plain text "%ED%A0%80". The lone surrogate goes in the body, as the JSON escape \ud800.
    private static readonly (string Kind, string Encoded)[] HostileUrlText =
    [
        (NulCharacter, "a%00b"),
        ("a right-to-left override", "%E2%80%AEgnp.exe"),
        ("a string of one megabyte", Megabyte),
    ];

    // The endpoints that read a body or a query and refuse their plausible request, with the reason. The input rules
    // and the binding run before the action, so they hold for these endpoints too; only a value that the use case
    // itself would refuse is then refused for the listed reason instead.
    private static readonly Dictionary<string, string> RefusedPlausibleRequests = new(StringComparer.Ordinal)
    {
        ["POST api/auth/login"] = "the plausible user name has no account: 401",
        ["POST api/bookings/public/receipt"] = "a receipt opens only with the email of the guest of the booking: 404",
        ["POST api/price-suggestions/approve"] = "the key names a suggestion of the latest market analysis, and the test world holds none: 404",
        ["PUT api/benchmark/requests/{runId:guid}/state"] = "the state is set on a request that the benchmark tool took, and the test world holds none: 404",
        ["POST api/rooms/{id:int}/complete-service"] = "a service is completed only on a room in service, and the seeded room is not: 422",
    };

    // What a body must not show: an exception, a frame of a stack trace, a .NET type, a setting or a secret.
    private static readonly Regex Leak = new(
        @"exception|stack ?trace|\bat [A-Za-z_][\w.`]*\(|\bsystem\.|\bmicrosoft\.|\bnfhotel\.|npgsql|connectionstring|host=|password=|"
            + Regex.Escape(ApiFactory.Token) + "|" + Regex.Escape(ApiFactory.SigningKey),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5));

    private readonly ApiFactory _factory;
    private readonly JsonSerializerOptions _json;
    private int _unique;
    private int _window;

    public HostileInputTests(ApiFactory factory)
    {
        _factory = factory;
        _ = _factory.Server;
        _json = new JsonSerializerOptions(_factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

        // A room of each size, one guest and one booking; the room, the guest and the booking have the key 1 that a
        // plausible request names.
        if (_factory.World.Rooms.Items.Count == 0)
        {
            var room = _factory.World.AddRoom("101", size: RoomSize.Double);
            _factory.World.AddRoom("102", size: RoomSize.Single);
            _factory.World.AddRoom("103", size: RoomSize.Suite);
            var guest = _factory.World.AddGuest();
            _factory.World.AddBooking(room.Id, guest.Id, startOffsetDays: 30);
        }
    }

    #region Every endpoint

    /// <summary>Positive: the endpoints that the test attacks are the endpoints of the API, every controller action and the minimal endpoints.</summary>
    [Fact]
    public void Endpoints_TakenFromTheApi_HoldEveryControllerAction()
    {
        // Arrange
        var declared = typeof(Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Count(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());

        // Act
        var endpoints = Endpoints();

        // Assert
        Assert.Equal(declared, endpoints.Count(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null));
        Assert.Contains(endpoints, endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is null);
    }

    /// <summary>Negative: every endpoint refuses each hostile value that it reads in the 400 range, never with a fault or a detail of the server.</summary>
    [Fact]
    public async Task EveryEndpoint_HostileInput_IsRefusedWithoutAFaultOrALeak()
    {
        // Arrange
        var endpoints = Endpoints();
        var attacked = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<string>();
        var sent = 0;

        // Act
        foreach (var endpoint in endpoints)
        {
            foreach (var attempt in Attempts(endpoint))
            {
                var (status, body) = await SendAsync(attempt).ConfigureAwait(true);
                sent++;
                attacked.Add(Key(endpoint));
                var failure = attempt.Description switch
                {
                    PlausibleValues => JudgePlausible(endpoint, status, body),
                    IgnoredInput => JudgeHarmless(status, body),
                    _ => Judge(status, body),
                };
                if (failure is not null)
                {
                    failures.Add($"{Key(endpoint)}: {attempt.Description} -> {failure}");
                }
            }
        }

        // Assert
        Assert.True(failures.Count == 0, $"{failures.Count} of {sent} requests failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        Assert.Equal(endpoints.Count, attacked.Count);
    }

    #endregion

    #region Judging

    private static string? Judge(int status, string body) =>
        JudgeHarmless(status, body) ?? (status is < 400 or > 499 ? $"{status}, not refused" : null);

    // No fault, and nothing of the server in the body.
    private static string? JudgeHarmless(int status, string body) =>
        status >= 500 ? $"{status}, a fault"
        : Leak.IsMatch(body) ? $"{status}, the body shows a detail of the server"
        : null;

    // The plausible request is no fault and shows nothing of the server; an endpoint that reads a body or a query
    // accepts it, unless it is listed with the reason why it cannot.
    private static string? JudgePlausible(RouteEndpoint endpoint, int status, string body)
    {
        if (JudgeHarmless(status, body) is { } harm)
        {
            return harm;
        }

        var isListed = RefusedPlausibleRequests.ContainsKey(Key(endpoint));
        var isAccepted = status is >= 200 and <= 299;
        if (!HasInput(endpoint) || isAccepted != isListed)
        {
            return null;
        }

        return isListed
            ? $"{status}, accepted although it is listed as refused"
            : $"{status}, refused ({Shorten(body)}); give the field a plausible value or list the endpoint with the reason";
    }

    private static string Shorten(string body) => body.Length > 160 ? body[..160] : body;

    #endregion

    #region Endpoints and attempts

    private static string Name(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText ?? endpoint.DisplayName ?? "?";

    private static string Key(RouteEndpoint endpoint) =>
        $"{endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single()} {Name(endpoint)}";

    private static bool HasInput(RouteEndpoint endpoint)
    {
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        return action is not null && (BodyParameter(action) is not null || QueryParameters(action, RouteNames(endpoint)).Any());
    }

    private static IEnumerable<string> RouteNames(RouteEndpoint endpoint) => endpoint.RoutePattern.Parameters.Select(part => part.Name);

    private static ParameterDescriptor? BodyParameter(ControllerActionDescriptor? action) =>
        action?.Parameters.FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body);

    // The endpoints that read a body or a query first, so that their plausible requests meet the seeded room, guest
    // and booking before an endpoint that reads only a key changes them; the deletions last.
    private List<RouteEndpoint> Endpoints() =>
        _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is not null)
            .OrderBy(endpoint => HasInput(endpoint) ? 0 : 1)
            .ThenBy(endpoint => Key(endpoint).StartsWith("DELETE ", StringComparison.Ordinal) ? 1 : 0)
            .ToList();

    private IEnumerable<Attempt> Attempts(RouteEndpoint endpoint)
    {
        var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single();
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        var routeValues = endpoint.RoutePattern.Parameters.ToDictionary(part => part.Name, PlausibleRouteValue, StringComparer.OrdinalIgnoreCase);
        var queries = QueryParameters(action, routeValues.Keys).ToList();
        var bodyParameter = BodyParameter(action);
        Dictionary<string, string> Query()
        {
            _window++;
            return queries.ToDictionary(parameter => parameter.Name, parameter => PlausibleQueryValue(parameter.ParameterType, parameter.Name), StringComparer.Ordinal);
        }

        JsonNode? NewBody()
        {
            _window++;
            return bodyParameter is null ? null : Plausible(bodyParameter.ParameterType, bodyParameter.Name, 0);
        }

        HttpContent? Body() => NewBody() is { } body ? Json(body) : null;

        // The plausible request first: whatever it answers, it is no fault and shows nothing of the server.
        yield return new(method, Path(endpoint, routeValues), Query(), Body(), PlausibleValues);

        foreach (var part in endpoint.RoutePattern.Parameters)
        {
            foreach (var (kind, value) in HostileRouteValues(part))
            {
                var values = new Dictionary<string, string>(routeValues, StringComparer.OrdinalIgnoreCase) { [part.Name] = value };
                yield return new(method, Path(endpoint, values), Query(), Body(), $"route {{{part.Name}}}: {kind}");
            }
        }

        foreach (var parameter in queries)
        {
            foreach (var (kind, value) in HostileQueryValues(parameter.ParameterType))
            {
                var values = Query();
                values[parameter.Name] = value;
                yield return new(method, Path(endpoint, routeValues), values, Body(), $"query {parameter.Name}: {kind}");
            }
        }

        if (bodyParameter is not null && NewBody() is { } sample)
        {
            foreach (var attempt in BodyAttempts(method, Path(endpoint, routeValues), Query(), bodyParameter, sample, NewBody))
            {
                yield return attempt;
            }
        }
        else if (routeValues.Count == 0 && queries.Count == 0)
        {
            // An endpoint that reads no input still meets input that it does not expect, and ignores it.
            yield return new(method, Path(endpoint, routeValues), new Dictionary<string, string> { ["q"] = HostileUrlText[0].Encoded }, Text("not json {"), IgnoredInput);
        }
    }

    private IEnumerable<Attempt> BodyAttempts(
        string method,
        string path,
        Dictionary<string, string> query,
        ParameterDescriptor bodyParameter,
        JsonNode sample,
        Func<JsonNode?> newBody)
    {
        var isOptional = bodyParameter.BindingInfo?.EmptyBodyBehavior == EmptyBodyBehavior.Allow
            || (bodyParameter is ControllerParameterDescriptor controllerParameter
                && (controllerParameter.ParameterInfo.HasDefaultValue
                    || new NullabilityInfoContext().Create(controllerParameter.ParameterInfo).WriteState == NullabilityState.Nullable));

        yield return new(method, path, query, Text("not json {"), "a body that is not JSON");
        yield return new(method, path, query, Text(sample is JsonArray ? "{}" : "[]"), "JSON of the wrong type");
        yield return new(method, path, query, Text("\"text\""), "a JSON string for the whole body");
        if (!isOptional)
        {
            yield return new(method, path, query, Text(string.Empty), "no body");
            yield return new(method, path, query, Text("null"), "a null body");
        }

        foreach (var (fieldPath, type, isNullable) in Fields(bodyParameter.ParameterType, [], 0))
        {
            var label = "$" + string.Concat(fieldPath.Select(segment => segment is int index ? $"[{index}]" : $".{segment}"));
            if (!isNullable)
            {
                yield return new(method, path, query, Json(With(newBody()!, fieldPath, null)), $"body {label}: null");
            }

            foreach (var (kind, value) in HostileJsonValues(type))
            {
                yield return new(method, path, query, Json(With(newBody()!, fieldPath, value)), $"body {label}: {kind}");
            }
        }
    }

    private static IEnumerable<ParameterDescriptor> QueryParameters(ControllerActionDescriptor? action, IEnumerable<string> routeNames)
    {
        if (action is null)
        {
            return [];
        }

        var route = new HashSet<string>(routeNames, StringComparer.OrdinalIgnoreCase);
        return action.Parameters.Where(parameter =>
            parameter.BindingInfo?.BindingSource == BindingSource.Query
            || (parameter.BindingInfo?.BindingSource is null or { IsFromRequest: true } && parameter.BindingInfo?.BindingSource != BindingSource.Body
                && parameter.BindingInfo?.BindingSource != BindingSource.Path && parameter.BindingInfo?.BindingSource != BindingSource.Header
                && parameter.BindingInfo?.BindingSource != BindingSource.Services && parameter.BindingInfo?.BindingSource != BindingSource.Special
                && !route.Contains(parameter.Name) && IsSimple(parameter.ParameterType)));
    }

    // The fields of a body: each value of the plausible JSON, with its CLR type and whether null is allowed.
    private IEnumerable<(object[] Path, Type Type, bool IsNullable)> Fields(Type type, object[] path, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (IsSimple(underlying) || depth > MaxDepth)
        {
            yield break;
        }

        var info = _json.GetTypeInfo(underlying);
        if (info.Kind == JsonTypeInfoKind.Enumerable && info.ElementType is not null)
        {
            object[] item = [.. path, 0];
            yield return (item, info.ElementType, IsNullableType(info.ElementType));
            foreach (var field in Fields(info.ElementType, item, depth + 1))
            {
                yield return field;
            }

            yield break;
        }

        if (info.Kind != JsonTypeInfoKind.Object)
        {
            yield break;
        }

        foreach (var property in info.Properties)
        {
            object[] child = [.. path, property.Name];
            yield return (child, property.PropertyType, property.IsSetNullable || IsNullableType(property.PropertyType) && property.PropertyType.IsValueType);
            foreach (var field in Fields(property.PropertyType, child, depth + 1))
            {
                yield return field;
            }
        }
    }

    #endregion

    #region Plausible values

    private JsonNode? Plausible(Type type, string name, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (IsSimple(underlying))
        {
            return PlausibleSimple(underlying, name);
        }

        if (depth > MaxDepth)
        {
            return null;
        }

        var info = _json.GetTypeInfo(underlying);
        switch (info.Kind)
        {
            case JsonTypeInfoKind.Enumerable when info.ElementType is not null:
                return new JsonArray(Plausible(info.ElementType, name, depth + 1));
            case JsonTypeInfoKind.Dictionary:
                return new JsonObject();
            case JsonTypeInfoKind.Object:
                var body = new JsonObject();
                foreach (var property in info.Properties)
                {
                    // An optional value is left out, as a client that does not use it would.
                    body[property.Name] = Nullable.GetUnderlyingType(property.PropertyType) is null
                        ? Plausible(property.PropertyType, property.Name, depth + 1)
                        : null;
                }

                return body;
            default:
                return null;
        }
    }

    private JsonNode? PlausibleSimple(Type type, string name) => type switch
    {
        _ when type == typeof(string) => JsonValue.Create(PlausibleText(name)),
        _ when type == typeof(bool) => JsonValue.Create(true),
        _ when type == typeof(Guid) => JsonValue.Create(Guid.NewGuid()),
        _ when type == typeof(DateOnly) => JsonValue.Create(PlausibleDate(name)),
        _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => JsonValue.Create(PlausibleDate(name) + "T12:00:00Z"),
        _ when type == typeof(TimeOnly) => JsonValue.Create("12:00:00"),
        _ when type == typeof(TimeSpan) => JsonValue.Create("00:30:00"),
        _ when type.IsEnum => JsonValue.Create(Convert.ToInt64(Enum.GetValues(type).GetValue(0), CultureInfo.InvariantCulture)),
        _ when IsNumber(type) => JsonValue.Create(PlausibleNumber(name)),
        _ => null,
    };

    // A number of a known kind gets a value that passes the rules of its use case; any other number is 1, which is
    // the key of the seeded room, guest and booking.
    private static int PlausibleNumber(string name) => name switch
    {
        _ when Has(name, "schema") => 2,
        _ when Has(name, "high") => 80,
        _ when Has(name, "low") => 40,
        _ => 1,
    };

    // A field of a known kind gets a value that passes the rules of its use case; any other text is new on each
    // request, so that a value that must be unique (a room number, a user name) is.
    private string PlausibleText(string name)
    {
        var unique = $"test{Interlocked.Increment(ref _unique)}";
        return name switch
        {
            _ when Has(name, "email") => $"{unique}@example.com",
            _ when Has(name, "currentPassword") => KnownPassword,
            _ when Has(name, "password") => NewPassword,
            _ when Has(name, "phone") => "+4512345678",
            _ when Has(name, "country") => "Denmark",
            _ when Has(name, "size") => nameof(RoomSize.Double),
            _ when Has(name, "purpose") => AiDefault.Website,
            _ when Has(name, "hash") || Has(name, "sha256") => new string('a', 64),
            _ when Has(name, "analysis") && Has(name, "type") => "hotel_cancellation",
            _ => unique,
        };
    }

    // The day of the window of this request; the end of a period lies two days after its start.
    private string PlausibleDate(string name)
    {
        var start = FirstDay.AddDays(3 * _window);
        var isEnd = Has(name, "end") || Has(name, "out") || Has(name, "last") || Has(name, "finish") || name.Equals("to", StringComparison.OrdinalIgnoreCase);
        return (isEnd ? start.AddDays(2) : start).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static bool Has(string name, string part) => name.Contains(part, StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Hostile values

    // The hostile values of one field, by the kind of value it holds; the wrong JSON type comes with each.
    private static IEnumerable<(string Kind, JsonNode? Value)> HostileJsonValues(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string))
        {
            foreach (var (kind, value) in HostileText)
            {
                yield return (kind, JsonValue.Create(value));
            }

            yield return ("a number in place of text", JsonValue.Create(42));
        }
        else if (IsNumber(underlying) || underlying.IsEnum)
        {
            yield return ($"a number above {(underlying.IsEnum ? typeof(int) : underlying).Name}.MaxValue", JsonNode.Parse(AboveMaxValue(underlying)));
            yield return ("a number below zero", JsonValue.Create(-1));
            yield return ("text in place of a number", JsonValue.Create("text"));
        }
        else if (underlying == typeof(DateOnly) || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
        {
            var time = underlying == typeof(DateOnly) ? string.Empty : "T00:00:00Z";
            yield return ("the first date of the calendar", JsonValue.Create("0001-01-01" + time));
            yield return ("the last date of the calendar", JsonValue.Create("9999-12-31" + time));
            yield return ("a date that does not exist", JsonValue.Create("2030-02-30" + time));
        }
        else if (underlying == typeof(bool) || underlying == typeof(Guid) || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan))
        {
            yield return ("text of the wrong form", JsonValue.Create("text"));
        }
        else
        {
            yield return ("text in place of an object or a list", JsonValue.Create("text"));
        }
    }

    // A number one above the largest value of its type, so that it cannot be read: for an int (and an enum, which
    // is an int) that is a number above Int32.MaxValue. A value that the type can hold, however large, is a matter
    // for the rules of the use case.
    private static string AboveMaxValue(Type type) => type switch
    {
        _ when type == typeof(long) => "9223372036854775808",
        _ when type == typeof(ulong) => "18446744073709551616",
        _ when type == typeof(uint) => "4294967296",
        _ when type == typeof(short) => "32768",
        _ when type == typeof(ushort) => "65536",
        _ when type == typeof(byte) => "256",
        _ when type == typeof(sbyte) => "128",
        _ when type == typeof(decimal) => "79228162514264337593543950336",
        _ when type == typeof(double) => "1e309",
        _ when type == typeof(float) => "1e39",
        _ => "2147483648",
    };

    private static IEnumerable<(string Kind, string Value)> HostileRouteValues(RoutePatternParameterPart part)
    {
        var constraints = part.ParameterPolicies.Select(policy => policy.Content).ToList();
        if (constraints.Contains("int", StringComparer.Ordinal))
        {
            yield return ("a number above Int32.MaxValue", AboveMaxValue(typeof(int)));
            yield return ("a number below zero", "-1");
            yield return ("text in place of a number", "text");
        }
        else if (constraints.Contains("guid", StringComparer.Ordinal))
        {
            yield return ("text in place of a GUID", "text");
        }
        else
        {
            // A NUL in the path never reaches the API: Kestrel refuses such a request target with 400 before routing,
            // and the client of the test server refuses to send it. The NUL goes in the query and the body instead.
            foreach (var value in HostileUrlText.Where(value => value.Kind != NulCharacter))
            {
                yield return value;
            }
        }
    }

    private static IEnumerable<(string Kind, string Value)> HostileQueryValues(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string))
        {
            return HostileUrlText;
        }

        if (IsNumber(underlying) || underlying.IsEnum)
        {
            return [($"a number above {(underlying.IsEnum ? typeof(int) : underlying).Name}.MaxValue", AboveMaxValue(underlying)), ("a number below zero", "-1"), ("text in place of a number", "text")];
        }

        if (underlying == typeof(DateOnly) || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
        {
            return [("the first date of the calendar", "0001-01-01"), ("the last date of the calendar", "9999-12-31"), ("a date that does not exist", "2030-02-30")];
        }

        return [("text of the wrong form", "text")];
    }

    #endregion

    #region Building and sending requests

    private string PlausibleRouteValue(RoutePatternParameterPart part)
    {
        var constraints = part.ParameterPolicies.Select(policy => policy.Content).ToList();
        return constraints.Contains("int", StringComparer.Ordinal) ? "1"
            : constraints.Contains("guid", StringComparer.Ordinal) ? PlausibleGuid
            : PlausibleText(part.Name);
    }

    private string PlausibleQueryValue(Type type, string name)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying == typeof(string) ? PlausibleText(name)
            : underlying == typeof(bool) ? "true"
            : underlying == typeof(DateOnly) || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) ? PlausibleDate(name)
            : underlying == typeof(Guid) ? PlausibleGuid
            : "1";
    }

    private static JsonNode With(JsonNode plausible, object[] path, JsonNode? value)
    {
        var body = plausible.DeepClone();
        var parent = body;
        foreach (var segment in path[..^1])
        {
            parent = segment is int index ? parent[index]! : parent[(string)segment]!;
        }

        switch (path[^1])
        {
            case int index:
                parent[index] = value;
                break;
            case string name:
                parent[name] = value;
                break;
        }

        return body;
    }

    private static string Path(RouteEndpoint endpoint, IReadOnlyDictionary<string, string> values)
    {
        var builder = new StringBuilder();
        foreach (var segment in endpoint.RoutePattern.PathSegments)
        {
            builder.Append('/');
            foreach (var part in segment.Parts)
            {
                builder.Append(part switch
                {
                    RoutePatternLiteralPart literal => literal.Content,
                    RoutePatternParameterPart parameter => values[parameter.Name],
                    RoutePatternSeparatorPart separator => separator.Content,
                    _ => string.Empty,
                });
            }
        }

        return builder.ToString();
    }

    private static StringContent Json(JsonNode body) =>
        Text(body.ToJsonString().Replace($"\"{LoneSurrogateMarker}\"", "\"\\ud800\"", StringComparison.Ordinal));

    private static StringContent Text(string body) => new(body, Encoding.UTF8, "application/json");

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) || underlying == typeof(decimal)
            || underlying == typeof(Guid) || underlying == typeof(DateOnly) || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset) || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan);
    }

    private static bool IsNumber(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(uint)
        || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) || type == typeof(decimal)
        || type == typeof(double) || type == typeof(float);

    private static bool IsNullableType(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private async Task<(int Status, string Body)> SendAsync(Attempt attempt)
    {
        using var client = _factory.CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, NewManagerToken());
        var query = attempt.Query.Count == 0
            ? string.Empty
            : "?" + string.Join('&', attempt.Query.Select(pair => $"{pair.Key}={pair.Value}"));
        using var request = new HttpRequestMessage(new HttpMethod(attempt.Method), attempt.Path + query) { Content = attempt.Content };
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return ((int)response.StatusCode, body);
    }

    // A new Manager for each request, whose password is known, so that the change of the own password can name it.
    private string NewManagerToken()
    {
        var member = _factory.AddStaff($"hostile-{Interlocked.Increment(ref _unique)}", KnownPassword, StaffRole.Manager);
        return _factory.Services.GetRequiredService<IStaffTokenIssuer>().Issue(member).Token;
    }

    private sealed record Attempt(string Method, string Path, IReadOnlyDictionary<string, string> Query, HttpContent? Content, string Description);

    #endregion
}
