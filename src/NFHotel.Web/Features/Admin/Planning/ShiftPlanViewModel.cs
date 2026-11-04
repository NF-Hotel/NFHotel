using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Shifts;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Planning;

/// <summary>
/// State of the shift plan (MIL-060): one week of shifts in the time of the hotel, the form to plan or change a shift,
/// and the trades that wait for a Manager. The page holds only markup and binding; the API checks every rule again.
/// </summary>
public sealed class ShiftPlanViewModel
{
    private readonly IUseCaseRunner<IShiftService> _shifts;
    private readonly IUseCaseRunner<IStaffAdminService> _staff;
    private readonly HotelTimeZone _zone;

    /// <summary>Creates the view model.</summary>
    /// <param name="shifts">Runs the use cases of the shifts.</param>
    /// <param name="staff">Runs the list of the accounts, to choose who works a shift.</param>
    /// <param name="clock">Gives the week to start with.</param>
    /// <param name="zone">The time of the hotel.</param>
    public ShiftPlanViewModel(
        IUseCaseRunner<IShiftService> shifts,
        IUseCaseRunner<IStaffAdminService> staff,
        IClock clock,
        HotelTimeZone zone)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(zone);

        _shifts = shifts;
        _staff = staff;
        _zone = zone;
        WeekStart = MondayOf(clock.Today);
    }

    /// <summary>The roles a shift can need.</summary>
    public static IReadOnlyList<StaffRole> Roles { get; } = Enum.GetValues<StaffRole>();

    /// <summary>The Monday of the week shown.</summary>
    public DateOnly WeekStart { get; private set; }

    /// <summary>The shifts of the week, by start.</summary>
    public IReadOnlyList<ShiftDto> Shifts { get; private set; } = [];

    /// <summary>The trades that a colleague agreed to and that wait for a Manager.</summary>
    public IReadOnlyList<ShiftTradeDto> AwaitingApproval { get; private set; } = [];

    /// <summary>The active accounts, to choose who works a shift.</summary>
    public IReadOnlyList<StaffAccountDto> Staff { get; private set; } = [];

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the week has been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True while the form of a shift is open.</summary>
    public bool IsEditing { get; private set; }

    /// <summary>The shift being changed, or <c>null</c> for a new one.</summary>
    public ShiftDto? EditTarget { get; private set; }

    /// <summary>The shift that waits for the confirmation of its deletion, or <c>null</c>.</summary>
    public ShiftDto? PendingDelete { get; private set; }

    /// <summary>The date of the shift in the form.</summary>
    public DateOnly FormDate { get; set; }

    /// <summary>The start in the form, on the clock of the hotel.</summary>
    public TimeOnly FormStart { get; set; } = new(7, 0);

    /// <summary>The end in the form; a time before the start is on the next day.</summary>
    public TimeOnly FormEnd { get; set; } = new(15, 0);

    /// <summary>The role in the form.</summary>
    public StaffRole FormRole { get; set; } = StaffRole.Housekeeping;

    /// <summary>The member of staff in the form, or <c>null</c> for nobody yet.</summary>
    public int? FormAssigneeId { get; set; }

    /// <summary>The days of the week shown.</summary>
    public IEnumerable<DateOnly> Days => Enumerable.Range(0, 7).Select(WeekStart.AddDays);

    /// <summary>The shifts that start on a day, on the clock of the hotel.</summary>
    /// <param name="day">The day.</param>
    /// <returns>The shifts, by start.</returns>
    public IEnumerable<ShiftDto> ShiftsOn(DateOnly day) =>
        Shifts.Where(shift => DateOnly.FromDateTime(_zone.ToHotel(shift.StartsAt).DateTime) == day);

    /// <summary>The start and the end of a shift on the clock of the hotel, for example <c>07:00–15:00</c>.</summary>
    /// <param name="shift">The shift.</param>
    /// <returns>The times.</returns>
    public string TimesOf(ShiftDto shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        return $"{_zone.ToHotel(shift.StartsAt):HH:mm}–{_zone.ToHotel(shift.EndsAt):HH:mm}";
    }

    /// <summary>The day and the times of a shift, for example <c>Mon 3 Nov 07:00–15:00</c>.</summary>
    /// <param name="shift">The shift.</param>
    /// <returns>The text.</returns>
    public string WhenOf(ShiftDto shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        return $"{_zone.ToHotel(shift.StartsAt):ddd d MMM} {TimesOf(shift)}";
    }

    /// <summary>The members of staff with the role of the form, for the choice of who works the shift.</summary>
    public IEnumerable<StaffAccountDto> StaffForRole => Staff.Where(account => account.Role == FormRole);

    /// <summary>Loads the week, the trades that wait, and the accounts.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when everything is loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var from = _zone.AtHotel(WeekStart, TimeOnly.MinValue);
            var to = _zone.AtHotel(WeekStart.AddDays(7), TimeOnly.MinValue);
            var shifts = await _shifts.RunAsync((service, token) => service.ListAsync(from, to, token), cancellationToken).ConfigureAwait(false);
            var trades = await _shifts.RunAsync((service, token) => service.ListTradesAsync(token), cancellationToken).ConfigureAwait(false);
            var staff = await _staff.RunAsync((service, token) => service.ListAsync(token), cancellationToken).ConfigureAwait(false);

            Errors = [.. Failures(shifts, trades, staff)];
            Shifts = shifts.IsSuccess ? shifts.Value : [];
            AwaitingApproval = trades.IsSuccess ? [.. trades.Value.Where(trade => trade.State == TradeState.AwaitingApproval)] : [];
            Staff = staff.IsSuccess ? [.. staff.Value.Where(account => account.IsActive)] : [];
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Shows the week before.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded.</returns>
    public Task PreviousWeekAsync(CancellationToken cancellationToken = default)
    {
        WeekStart = WeekStart.AddDays(-7);
        return LoadAsync(cancellationToken);
    }

    /// <summary>Shows the week after.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded.</returns>
    public Task NextWeekAsync(CancellationToken cancellationToken = default)
    {
        WeekStart = WeekStart.AddDays(7);
        return LoadAsync(cancellationToken);
    }

    /// <summary>Opens the form of a new shift on a day.</summary>
    /// <param name="day">The day.</param>
    public void BeginCreate(DateOnly day)
    {
        Cancel();
        IsEditing = true;
        FormDate = day;
    }

    /// <summary>Opens the form of a shift with its values.</summary>
    /// <param name="shift">The shift.</param>
    public void BeginEdit(ShiftDto shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        Cancel();
        IsEditing = true;
        EditTarget = shift;
        var start = _zone.ToHotel(shift.StartsAt);
        FormDate = DateOnly.FromDateTime(start.DateTime);
        FormStart = TimeOnly.FromDateTime(start.DateTime);
        FormEnd = TimeOnly.FromDateTime(_zone.ToHotel(shift.EndsAt).DateTime);
        FormRole = shift.Role;
        FormAssigneeId = shift.AssigneeId;
    }

    /// <summary>Asks for the confirmation of deleting a shift.</summary>
    /// <param name="shift">The shift.</param>
    public void BeginDelete(ShiftDto shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        Cancel();
        PendingDelete = shift;
    }

    /// <summary>Closes the forms.</summary>
    public void Cancel()
    {
        IsEditing = false;
        EditTarget = null;
        PendingDelete = null;
        FormAssigneeId = null;
        SuccessMessage = null;
        Errors = [];
    }

    /// <summary>Saves the shift of the form: a new one, or the changes of the one being edited.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded again.</returns>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var startsAt = _zone.AtHotel(FormDate, FormStart);
        var endsAt = _zone.AtHotel(FormEnd > FormStart ? FormDate : FormDate.AddDays(1), FormEnd);
        var assignee = StaffForRole.Any(account => account.Id == FormAssigneeId) ? FormAssigneeId : null;
        var request = new SaveShiftRequest(startsAt, endsAt, FormRole, assignee);
        var target = EditTarget;

        await RunAsync(
            async (service, token) => target is null
                ? await service.CreateAsync(request, token).ConfigureAwait(false)
                : await service.UpdateAsync(target.Id, request, token).ConfigureAwait(false),
            target is null ? "Planned the shift." : "Saved the shift.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes the shift that waits for the confirmation.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded again.</returns>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        if (PendingDelete is not { } shift)
        {
            return;
        }

        await RunAsync((service, token) => service.DeleteAsync(shift.Id, token), "Deleted the shift.", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Approves a trade; the shifts change hands.</summary>
    /// <param name="trade">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded again.</returns>
    public Task ApproveAsync(ShiftTradeDto trade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trade);

        return RunAsync(
            async (service, token) => await service.ApproveTradeAsync(trade.Id, token).ConfigureAwait(false),
            "Approved the trade. The shifts have changed hands.",
            cancellationToken);
    }

    /// <summary>Rejects a trade; the shifts stay.</summary>
    /// <param name="trade">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the week is loaded again.</returns>
    public Task RejectAsync(ShiftTradeDto trade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trade);

        return RunAsync(
            async (service, token) => await service.RejectTradeAsync(trade.Id, token).ConfigureAwait(false),
            "Rejected the trade.",
            cancellationToken);
    }

    /// <summary>What a trade is, for example <c>Give away: Mon 3 Nov 07:00–15:00 (Hilda Berg → Otto Lund)</c>.</summary>
    /// <param name="trade">The trade.</param>
    /// <returns>The text.</returns>
    public string Describe(ShiftTradeDto trade)
    {
        ArgumentNullException.ThrowIfNull(trade);

        return trade.SwapShift is { } swap
            ? $"Swap: {trade.OfferedByName} gives {WhenOf(trade.Shift)} for {trade.TakenByName}'s {WhenOf(swap)}"
            : $"Give away: {WhenOf(trade.Shift)} from {trade.OfferedByName} to {trade.TakenByName}";
    }

    private static DateOnly MondayOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    private static IEnumerable<string> Failures(params Result[] results) =>
        results.Where(result => result.IsFailure).SelectMany(ErrorMessages.Describe).Distinct();

    private async Task RunAsync(Func<IShiftService, CancellationToken, Task<Result>> action, string success, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Result result;
        try
        {
            result = await _shifts.RunAsync(action, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            SuccessMessage = null;
            return;
        }

        Cancel();
        SuccessMessage = success;
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }
}
