namespace NFHotel.Web.Common;

/// <summary>Gives the runner the address of the browser whose page makes the call (MIL-087, ADR-0051 decision 4).</summary>
public interface IClientAddressSource
{
    #region Public methods

    /// <summary>Reads the address of the current browser.</summary>
    /// <returns>The address, or <c>null</c> when the host gives none.</returns>
    ValueTask<string?> GetAddressAsync();

    #endregion Public methods
}
