namespace NFHotel.ApiClient;

/// <summary>
/// Carries the guest token of one use-case call from the web application to the gateway (MIL-018, ADR-0047). It is
/// scoped like <see cref="StaffCallContext"/>: the runner of the web application puts the token of the signed-in guest
/// in the scope of the call, and the gateway of that scope sends it in the guest header, unless the call carries a staff
/// token, which always goes alone.
/// </summary>
public sealed class GuestCallContext
{
    #region Properties

    /// <summary>The signed token of the guest, or <c>null</c> when no guest is signed in.</summary>
    public string? Token { get; set; }

    #endregion Properties
}
