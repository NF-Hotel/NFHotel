namespace NFHotel.TestSupport;

/// <summary>
/// The hostile strings that the tests carry through every text field of the booking flow (MIL-086 decision 5,
/// ADR-0050): markup, a <c>javascript:</c> address, fragments of SQL, format strings, template expressions, a line
/// break with a mail header, the two wildcards of a SQL pattern, and a noncharacter that the runtime cannot bring to
/// normal form. Each one is either refused by the rule of its field or stored unchanged and shown as text; no rule
/// strips or rewrites it.
/// </summary>
/// <remarks>
/// The list is shared, so the domain, the use cases, the API, the database and the pages are tested with the same
/// values. A value that every rule refuses still belongs here: it shows that the refusal, not a rewrite, protects the
/// field.
/// </remarks>
public static class HostileStrings
{
    /// <summary>The hostile strings with the kind of attack that each one stands for.</summary>
    public static IReadOnlyList<(string Kind, string Value)> All { get; } =
    [
        ("a script element", "<script>alert(1)</script>"),
        ("an image element with an event attribute", "<img src=x onerror=alert(1)>"),
        ("an attribute that leaves its quotes", "\" onmouseover=\"alert(1)"),
        ("a javascript: address", "javascript:alert(1)"),
        ("a SQL fragment of letters, spaces and apostrophes", "x' OR 'x"),
        ("a SQL fragment with a comparison", "' OR '1'='1"),
        ("a SQL statement", "Robert'); DROP TABLE guests;--"),
        ("a C format string", "%s%s%n"),
        ("a composite format item", "{0}"),
        ("a template expression", "${7*7}"),
        ("a template expression of a page", "{{7*7}}"),
        ("a line break with a mail header", "a@example.com\r\nBcc: b@example.com"),
        ("the wildcard of any text", "%"),
        ("the wildcard of one character", "_"),
        ("a noncharacter that the runtime cannot bring to normal form", "Ann\uFFFE"),
    ];
}
