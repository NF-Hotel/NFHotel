namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// De otte rengørings- og serviceovergange på <c>Room</c>, som værdi.
/// </summary>
/// <remarks>
/// Overgangene skal kunne stå i <c>[InlineData]</c>, så hele overgangstabellen kan testes
/// som theories i stedet for som otte gange tre næsten ens tests.
/// <c>CompleteService</c> tæller som to overgange, fordi dens <c>requiresCleaning</c>-flag
/// vælger to forskellige måltilstande.
/// <para>
/// Public, ikke internal: xUnit-testmetoder er public, og en parametertype må ikke være
/// mindre tilgængelig end metoden.
/// </para>
/// </remarks>
public enum HousekeepingTransition
{
    /// <summary>Beboet rum afventer daglig rengøring.</summary>
    MarkDailyCleaningDue,

    /// <summary>Rummet afventer slutrengøring efter afrejse.</summary>
    MarkDepartureCleaningDue,

    /// <summary>Rengøringspersonalet kvitterer og går i gang.</summary>
    StartCleaning,

    /// <summary>Rengøringen er færdig.</summary>
    CompleteCleaning,

    /// <summary>Et problem rapporteres til serviceteknikeren.</summary>
    ReportServiceNeeded,

    /// <summary>Serviceteknikeren kvitterer og går i gang.</summary>
    StartService,

    /// <summary>Servicen afsluttes uden efterfølgende rengøringsbehov.</summary>
    CompleteServiceWithoutCleaning,

    /// <summary>Servicen afsluttes og har efterladt rummet snavset.</summary>
    CompleteServiceRequiringCleaning
}