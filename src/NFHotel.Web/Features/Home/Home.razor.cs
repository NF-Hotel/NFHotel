using System.Globalization;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Ports;

namespace NFHotel.Web.Features.Home;

/// <summary>
/// The code behind the front page: the dates the booking box starts on. The page has no viewmodel,
/// because it fetches nothing — the only thing it needs is the clock.
/// </summary>
public partial class Home : ComponentBase
{
    /// <summary>The value format of a date input, whatever the culture of the server.</summary>
    private const string DateInputFormat = "yyyy-MM-dd";

    /// <summary>Today, from the clock of the application and never from the server (A-05).</summary>
    [Inject]
    private IClock Clock { get; set; } = null!;

    /// <summary>The booking box starts on the same dates as the flow on /book: tomorrow, for one night.</summary>
    private string DefaultCheckIn => Iso(Clock.Today.AddDays(1));

    private string DefaultCheckOut => Iso(Clock.Today.AddDays(2));

    private string Today => Iso(Clock.Today);

    private string Tomorrow => Iso(Clock.Today.AddDays(1));

    private static string Iso(DateOnly date) => date.ToString(DateInputFormat, CultureInfo.InvariantCulture);
}
