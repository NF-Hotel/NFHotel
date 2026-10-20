namespace NFHotel.Application.Ports;

/// <summary>
/// Who is calling the use case and from where (ADR-0017). The API fills it from the validated staff token and the
/// request; a command on the server has no caller. It is read for the audit trail and for rules about the caller's
/// own account.
/// </summary>
public interface IStaffCallContext
{
    /// <summary>The id of the signed-in member of staff, or <c>null</c> when nobody is signed in.</summary>
    int? StaffId { get; }

    /// <summary>The user name of the signed-in member of staff, or <c>null</c>.</summary>
    string? StaffName { get; }

    /// <summary>The address the request came from, or <c>null</c> when it is unknown.</summary>
    string? RemoteAddress { get; }
}
