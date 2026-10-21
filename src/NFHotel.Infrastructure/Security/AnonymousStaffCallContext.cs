using NFHotel.Application.Ports;

namespace NFHotel.Infrastructure.Security;

/// <summary>The call context of a caller that is not an HTTP request, for example a command on the server (ADR-0017). It knows nobody.</summary>
public sealed class AnonymousStaffCallContext : IStaffCallContext
{
    /// <inheritdoc />
    public int? StaffId => null;

    /// <inheritdoc />
    public string? StaffName => null;

    /// <inheritdoc />
    public string? RemoteAddress => null;
}
