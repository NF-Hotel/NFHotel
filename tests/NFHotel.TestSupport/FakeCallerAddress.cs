using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>The address of the caller that a test sets (ADR-0049 decision 6); a documentation address by default.</summary>
public sealed class FakeCallerAddress : ICallerAddress
{
    #region Fields

    /// <summary>The address a test calls from unless it sets another one (RFC 5737, a range for documentation).</summary>
    public const string DefaultAddress = "203.0.113.10";

    #endregion Fields

    #region Properties

    /// <inheritdoc />
    public string? Address { get; set; } = DefaultAddress;

    #endregion Properties
}
