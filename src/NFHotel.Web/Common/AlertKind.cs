namespace NFHotel.Web.Common;

/// <summary>
/// Alvorlighedsgraden af en besked vist i <c>AlertBox</c>.
/// </summary>
/// <remarks>
/// Rent visningsbegreb og derfor Web-lagets ansvar. Application-laget returnerer
/// fejlkoder, ikke farver — oversættelsen sker her og i <see cref="ErrorMessages"/>.
/// </remarks>
public enum AlertKind
{
    /// <summary>Neutral oplysning. Kræver ingen handling.</summary>
    Info = 0,

    /// <summary>Handlingen lykkedes.</summary>
    Ok = 1,

    /// <summary>Handlingen mislykkedes, eller data er ugyldige.</summary>
    Error = 2
}