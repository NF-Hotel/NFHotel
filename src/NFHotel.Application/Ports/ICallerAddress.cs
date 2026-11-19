namespace NFHotel.Application.Ports;

/// <summary>
/// The address of the caller of the current request, as the API sees it (ADR-0049 decision 6). It stands behind one
/// port, so that MIL-087 changes one implementation when it makes the address the one the proxy saw.
/// </summary>
public interface ICallerAddress
{
    /// <summary>The address of the caller, or <see langword="null"/> when the API does not know it.</summary>
    string? Address { get; }
}
