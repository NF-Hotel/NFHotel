namespace NFHotel.Web.Common;

/// <summary>Gives the runner the staff token of the user whose circuit makes the call (ADR-0005).</summary>
public interface IStaffTokenSource
{
    /// <summary>Reads the staff token of the current user.</summary>
    /// <returns>The token, or <c>null</c> when nobody is signed in.</returns>
    ValueTask<string?> GetTokenAsync();
}