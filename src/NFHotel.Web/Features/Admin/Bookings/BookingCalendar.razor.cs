using Microsoft.AspNetCore.Components;

using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// The code behind the availability calendar: forwarding to <see cref="BookingCalendarViewModel"/> with the token of
/// the component, and the night the user picks back to the page.
/// </summary>
public partial class BookingCalendar : CancellableComponentBase
{
    /// <summary>The room sizes of the filter, in the order of the enum.</summary>
    private static readonly RoomSize[] Sizes = Enum.GetValues<RoomSize>();

    /// <summary>The headings of the columns, Monday first.</summary>
    private static readonly string[] Weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    /// <summary>The night the booking panel is open for, marked in the grid. <c>null</c> when none is.</summary>
    [Parameter]
    public DateOnly? Selected { get; set; }

    /// <summary>Called with the night the user clicks, when a booking can start on it.</summary>
    [Parameter]
    public EventCallback<DateOnly> OnPickNight { get; set; }

    /// <summary>aria-busy as text; a bool would become an empty attribute.</summary>
    private string BusyText => ViewModel.IsBusy ? "true" : "false";

    /// <summary>Fetches the month when the calendar opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    /// <summary>The weekend columns are shaded like their days.</summary>
    private static string WeekdayClass(string weekday) =>
        weekday is "Sat" or "Sun" ? "cal-dow weekend" : "cal-dow";

    /// <summary>The classes of a day, with the night the panel is open for marked.</summary>
    private string DayClass(CalendarDay day) =>
        Selected == day.Date ? $"{day.CssClass} is-selected" : day.CssClass;

    /// <summary>A past day stays in the tab order with its tooltip, but says it cannot be picked.</summary>
    private static string? DisabledText(CalendarDay day) =>
        BookingCalendarViewModel.CanPick(day) ? null : "true";

    /// <summary>Writes aria-pressed as text; a bool would become an empty attribute.</summary>
    private string SizePressed(RoomSize? size) => ViewModel.SizeFilter == size ? "true" : "false";

    private void FilterBySize(RoomSize? size) => ViewModel.FilterBySize(size);

    private Task PreviousMonthAsync() => ViewModel.PreviousMonthAsync(ComponentToken);

    private Task NextMonthAsync() => ViewModel.NextMonthAsync(ComponentToken);

    private Task ThisMonthAsync() => ViewModel.ThisMonthAsync(ComponentToken);

    private Task PickAsync(CalendarDay day) =>
        BookingCalendarViewModel.CanPick(day) ? OnPickNight.InvokeAsync(day.Date) : Task.CompletedTask;
}
