namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The steps of the customer's booking flow on <c>/book</c>.
/// </summary>
/// <remarks>
/// The values are ascending and are compared with <c>&lt;</c> in the step indicator, so a step before
/// the current one is drawn as completed. The order is therefore part of the contract — not
/// just a list.
/// <para>
/// The step alone is not a permission: <see cref="BookingFlowViewModel"/> always checks
/// dates and room choice again before it moves on (BR-C-04). Otherwise a step that
/// was set by an earlier action could carry a user past a rule.
/// </para>
/// </remarks>
public enum BookingFlowStep
{
    /// <summary>Step 1: arrival and departure date.</summary>
    Dates = 0,

    /// <summary>Step 2: choice among the rooms that are free in the period (BR-C-02).</summary>
    Room = 1,

    /// <summary>Step 3: the guest's details, entered without an account (BR-C-01).</summary>
    Guest = 2,

    /// <summary>Trin 4: kvitteringen for den oprettede booking (BR-C-05).</summary>
    Receipt = 3
}
