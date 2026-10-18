namespace NFHotel.Domain.ValueObjects;

/// <summary>Reads a role from the text of a claim. Only the exact name of a role counts, never a number.</summary>
public static class StaffRoleParser
{
    /// <summary>Parses a role name.</summary>
    /// <param name="text">The text of the claim.</param>
    /// <param name="role">The role, when the text is the exact name of one.</param>
    /// <returns>True when the text names a role. <c>"2"</c>, <c>"manager"</c> and <c>"Manager,Housekeeping"</c> are refused.</returns>
    public static bool TryParse(string? text, out StaffRole role)
    {
        if (text is not null && Array.IndexOf(Enum.GetNames<StaffRole>(), text) >= 0)
        {
            role = Enum.Parse<StaffRole>(text);
            return true;
        }

        role = default;
        return false;
    }
}