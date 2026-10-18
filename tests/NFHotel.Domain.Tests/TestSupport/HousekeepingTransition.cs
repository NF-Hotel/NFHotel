namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// The eight cleaning and service transitions on <c>Room</c>, as a value.
/// </summary>
/// <remarks>
/// The transitions must be able to stand in <c>[InlineData]</c>, so the whole transition table can be tested
/// as theories instead of as eight times three nearly identical tests.
/// <c>CompleteService</c> counts as two transitions, because its <c>requiresCleaning</c> flag
/// chooses two different target states.
/// <para>
/// Public, not internal: xUnit test methods are public, and a parameter type must not be
/// less accessible than the method.
/// </para>
/// </remarks>
public enum HousekeepingTransition
{
    /// <summary>The occupied room is waiting for daily cleaning.</summary>
    MarkDailyCleaningDue,

    /// <summary>The room is waiting for departure cleaning after departure.</summary>
    MarkDepartureCleaningDue,

    /// <summary>The cleaning staff acknowledge and start work.</summary>
    StartCleaning,

    /// <summary>The cleaning is finished.</summary>
    CompleteCleaning,

    /// <summary>A problem is reported to the service technician.</summary>
    ReportServiceNeeded,

    /// <summary>The service technician acknowledges and starts work.</summary>
    StartService,

    /// <summary>The service is finished without any need for cleaning afterwards.</summary>
    CompleteServiceWithoutCleaning,

    /// <summary>The service is finished and has left the room dirty.</summary>
    CompleteServiceRequiringCleaning
}