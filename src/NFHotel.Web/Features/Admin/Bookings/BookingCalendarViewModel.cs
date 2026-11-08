using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// Presentation logic for the availability calendar of the booking overview: the free rooms of each size, night by
/// night, a month at a time.
/// </summary>
/// <remarks>
/// The service counts (<see cref="IBookingService.GetAvailabilityAsync"/>) and <see cref="CalendarGrid"/> lays the
/// month out. The class holds the state of the screen only: the month, the size filter and the figures last fetched.
/// The size filter narrows what has already been fetched, so it needs no call.
/// <para>
/// "Today" comes from <see cref="IClock"/> and never from <c>DateTime.Now</c> (A-05).
/// </para>
/// </remarks>
public sealed class BookingCalendarViewModel
{
    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IClock _clock;

    private IReadOnlyList<NightAvailabilityDto> _nights = Array.Empty<NightAvailabilityDto>();

    /// <summary>
    /// Creates the viewmodel on the month of today.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    /// <param name="clock">The clock of the hotel (A-05).</param>
    public BookingCalendarViewModel(IUseCaseRunner<IBookingService> bookings, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _clock = clock;
        Month = CalendarGrid.MonthOf(clock.Today);
    }

    /// <summary>The 1st of the month shown.</summary>
    public DateOnly Month { get; private set; }

    /// <summary>The month in words, for example "November 2026".</summary>
    public string MonthText => Month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>True when the month shown is the month of today, so the button that goes back to it has nothing to do.</summary>
    public bool IsThisMonth => Month == CalendarGrid.MonthOf(_clock.Today);

    /// <summary>The one size shown, or <c>null</c> for all of them.</summary>
    public RoomSize? SizeFilter { get; private set; }

    /// <summary>The days of the grid, Monday first, a multiple of seven.</summary>
    public IReadOnlyList<CalendarDay> Days { get; private set; } = Array.Empty<CalendarDay>();

    /// <summary>The nights ahead in the month on which a size shown is sold out.</summary>
    public int SoldOutNightsAhead => CalendarGrid.SoldOutNightsAhead(Days);

    /// <summary>The sentence under the grid about <see cref="SoldOutNightsAhead"/>.</summary>
    public string SoldOutText
    {
        get
        {
            int nights = SoldOutNightsAhead;
            string what = SizeFilter switch
            {
                RoomSize.Single => "single rooms",
                RoomSize.Double => "double rooms",
                RoomSize.Suite => "suites",
                _ => "a size",
            };

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{nights} {(nights == 1 ? "night" : "nights")} ahead this month with {what} sold out");
        }
    }

    /// <summary>Errors to show to the user.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the calendar has fetched figures at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Fetches the month shown. The month is kept when the user comes back to the calendar, so a look at the list
    /// does not lose the place.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) => LoadAsync(cancellationToken);

    /// <summary>
    /// Fetches the figures of every day in the grid of the month shown, the weeks around the month included.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        DateOnly month = Month;
        DateOnly from = CalendarGrid.FirstShown(month);
        DateOnly to = CalendarGrid.EndShown(month);

        IsBusy = true;

        try
        {
            Result<IReadOnlyList<NightAvailabilityDto>> result = await _bookings
                .RunAsync((service, token) => service.GetAvailabilityAsync(from, to, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                _nights = Array.Empty<NightAvailabilityDto>();
            }
            else
            {
                Errors = Array.Empty<string>();
                _nights = result.Value;
            }

            Rebuild();
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Shows the month before.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task PreviousMonthAsync(CancellationToken cancellationToken = default) =>
        ShowMonthAsync(Month.AddMonths(-1), cancellationToken);

    /// <summary>Shows the month after.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task NextMonthAsync(CancellationToken cancellationToken = default) =>
        ShowMonthAsync(Month.AddMonths(1), cancellationToken);

    /// <summary>Goes back to the month of today.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ThisMonthAsync(CancellationToken cancellationToken = default) =>
        ShowMonthAsync(CalendarGrid.MonthOf(_clock.Today), cancellationToken);

    /// <summary>
    /// Shows one size, or all of them, from the figures already fetched.
    /// </summary>
    /// <param name="size">The size, or <c>null</c> for all.</param>
    public void FilterBySize(RoomSize? size)
    {
        SizeFilter = size;

        Rebuild();
    }

    /// <summary>Tells whether a booking can start on a day: not before today (BR-44).</summary>
    /// <param name="day">The day.</param>
    /// <returns>True when the day can be picked.</returns>
    public static bool CanPick(CalendarDay day)
    {
        ArgumentNullException.ThrowIfNull(day);

        return !day.IsPast;
    }

    private Task ShowMonthAsync(DateOnly month, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        Month = month;

        return LoadAsync(cancellationToken);
    }

    private void Rebuild() =>
        Days = CalendarGrid.Build(Month, _clock.Today, SizeFilter, _nights);
}
