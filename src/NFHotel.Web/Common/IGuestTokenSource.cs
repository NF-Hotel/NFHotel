namespace NFHotel.Web.Common;

/// <summary>
/// Gives the runner the guest token of the signed-in guest of the request (MIL-018, ADR-0047). Only a guest page fills
/// it, from the guest cookie; every other page, and every interactive circuit, has none.
/// </summary>
public interface IGuestTokenSource
{
    #region Properties

    /// <summary>The guest token of the request, or <c>null</c> when no guest is signed in.</summary>
    string? Token { get; }

    #endregion Properties
}
