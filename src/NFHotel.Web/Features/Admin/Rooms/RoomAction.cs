namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// De overgange rumoversigten kan udføre på ét rum.
/// </summary>
/// <remarks>
/// Én værdi pr. metode på <c>IRoomService</c> (A-07). Enum og ikke en streng, så en knap
/// der peger på en overgang der ikke findes, bliver en compilerfejl.
/// <para>
/// De to sidste værdier er den samme use case — <c>CompleteServiceAsync</c> — med hvert
/// sit svar på om rummet skal rengøres bagefter. De er delt op her, fordi det er to
/// forskellige knapper for brugeren, ikke to forskellige regler.
/// </para>
/// </remarks>
public enum RoomAction
{
    /// <summary>Tag rummet ud af drift.</summary>
    TakeOutOfService = 0,

    /// <summary>Send rummet til vedligehold.</summary>
    SendToMaintenance = 1,

    /// <summary>Sæt rummet i drift igen.</summary>
    ReturnToService = 2,

    /// <summary>Markér at daglig rengøring mangler.</summary>
    MarkDailyCleaningDue = 3,

    /// <summary>Markér at slutrengøring mangler.</summary>
    MarkDepartureCleaningDue = 4,

    /// <summary>Start rengøringen.</summary>
    StartCleaning = 5,

    /// <summary>Afslut rengøringen.</summary>
    CompleteCleaning = 6,

    /// <summary>Meld servicebehov.</summary>
    ReportServiceNeeded = 7,

    /// <summary>Start servicen.</summary>
    StartService = 8,

    /// <summary>Afslut servicen — rummet skal rengøres bagefter.</summary>
    CompleteServiceStillDirty = 9,

    /// <summary>Afslut servicen — rummet er rent.</summary>
    CompleteServiceClean = 10
}