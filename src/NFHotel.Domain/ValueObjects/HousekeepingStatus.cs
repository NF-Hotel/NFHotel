namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Rummets rengørings- og servicestand. Opdateres af rengøringspersonale og serviceteknikere.
/// </summary>
/// <remarks>
/// Påvirker IKKE bookbarhed — et rum kan være <see cref="RoomStatus.Available"/> og
/// samtidig afvente rengøring (B-04). <c>Clean = 0</c> er valgt, så
/// <c>default(HousekeepingStatus)</c> og databasens <c>DEFAULT 0</c> betyder
/// "intet i vejen", parallelt med <see cref="RoomStatus.Available"/>.
/// Nye trin (fx et inspektionstrin) tilføjes på enden — omnummerering er dyr.
/// </remarks>
public enum HousekeepingStatus
{
    /// <summary>Rent og klar til gæst. Startværdi for et nyoprettet rum.</summary>
    Clean = 0,

    /// <summary>Beboet rum der afventer daglig rengøring.</summary>
    DailyCleaningDue = 1,

    /// <summary>Gæsten er rejst; rummet afventer slutrengøring.</summary>
    DepartureCleaningDue = 2,

    /// <summary>Rengøringspersonalet har kvitteret og er i gang.</summary>
    CleaningInProgress = 3,

    /// <summary>Et problem er rapporteret; rummet afventer servicetekniker.</summary>
    ServiceRequired = 4,

    /// <summary>Serviceteknikeren har kvitteret og er i gang.</summary>
    ServiceInProgress = 5
}