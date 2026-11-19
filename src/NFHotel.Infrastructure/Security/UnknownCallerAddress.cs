using NFHotel.Application.Ports;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The address of a caller that is not an HTTP request: unknown (ADR-0049 decision 6). The API replaces it with the
/// address of the request.
/// </summary>
public sealed class UnknownCallerAddress : ICallerAddress
{
    /// <inheritdoc />
    public string? Address => null;
}
