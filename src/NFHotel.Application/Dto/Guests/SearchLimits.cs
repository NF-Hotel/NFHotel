using NFHotel.Domain.Entities;

namespace NFHotel.Application.Dto.Guests;

/// <summary>The limits of the free text of a search for a guest: the guest list and the booking overview (MIL-077).</summary>
public static class SearchLimits
{
    /// <summary>
    /// The longest search text: twice the longest field that a search matches (a name, a country or an email), so
    /// that a first and a last name fit.
    /// </summary>
    public const int MaxTextLength = 2 * Guest.MaxNameLength;
}
