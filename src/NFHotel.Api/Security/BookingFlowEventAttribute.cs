using Microsoft.AspNetCore.Mvc;

namespace NFHotel.Api.Security;

/// <summary>
/// Writes the security events of a step of the booking flow from the answer of its action (MIL-087, ADR-0051 decision 6).
/// It runs inside the limits of <see cref="SenderLimitAttribute"/>, so a request that they refuse writes their event only.
/// </summary>
public sealed class BookingFlowEventAttribute : TypeFilterAttribute
{
    #region Constructors

    /// <summary>Writes the events of <paramref name="step"/>.</summary>
    /// <param name="step">The step of the action.</param>
    public BookingFlowEventAttribute(BookingFlowStep step)
        : base(typeof(BookingFlowEventFilter))
    {
        Step = step;
        Arguments = [step];
        Order = 1;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The step of the action.</summary>
    public BookingFlowStep Step { get; }

    #endregion Properties
}
