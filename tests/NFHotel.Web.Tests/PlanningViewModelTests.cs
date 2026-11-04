using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Shifts;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Planning;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the shift plan and the room assignments of the back office (MIL-060).</summary>
public sealed class PlanningViewModelTests
{
    private static readonly HotelTimeZone Copenhagen = new(TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"));

    #region Hotel time

    /// <summary>Equivalence: a time on the clock of the hotel gets the offset of that date, winter and summer.</summary>
    [Fact]
    public void AtHotel_WinterAndSummer_UsesTheOffsetOfTheDate()
    {
        // Act
        var winter = Copenhagen.AtHotel(new DateOnly(2026, 1, 5), new TimeOnly(7, 0));
        var summer = Copenhagen.AtHotel(new DateOnly(2026, 7, 6), new TimeOnly(7, 0));

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), winter.Offset);
        Assert.Equal(TimeSpan.FromHours(2), summer.Offset);
        Assert.Equal(new TimeOnly(7, 0), TimeOnly.FromDateTime(Copenhagen.ToHotel(summer.ToUniversalTime()).DateTime));
    }

    #endregion

    #region Shift plan

    /// <summary>Positive: the week starts on Monday, and saving an end before the start ends the shift the next day.</summary>
    [Fact]
    public async Task SaveAsync_NightShift_EndsTheNextDayInHotelTime()
    {
        // Arrange
        var shifts = Substitute.For<IShiftService>();
        shifts.ListAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ShiftDto>>.Success([]));
        shifts.ListTradesAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ShiftTradeDto>>.Success([]));
        shifts.CreateAsync(Arg.Any<SaveShiftRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ShiftDto>.Success(new ShiftDto(1, call.Arg<SaveShiftRequest>().StartsAt, call.Arg<SaveShiftRequest>().EndsAt, StaffRole.Housekeeping, null, null)));
        var viewModel = ShiftPlan(shifts, today: new DateOnly(2026, 1, 8));
        viewModel.BeginCreate(new DateOnly(2026, 1, 6));
        viewModel.FormStart = new TimeOnly(22, 0);
        viewModel.FormEnd = new TimeOnly(6, 0);

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 5), viewModel.WeekStart);
        await shifts.Received(1).CreateAsync(
            Arg.Is<SaveShiftRequest>(request =>
                request.StartsAt == new DateTimeOffset(2026, 1, 6, 22, 0, 0, TimeSpan.FromHours(1))
                && request.EndsAt == new DateTimeOffset(2026, 1, 7, 6, 0, 0, TimeSpan.FromHours(1))),
            Arg.Any<CancellationToken>());
        Assert.Equal("Planned the shift.", viewModel.SuccessMessage);
    }

    /// <summary>Negative: a refusal keeps the form open and shows the translated text.</summary>
    [Fact]
    public async Task SaveAsync_Overlap_KeepsTheFormAndShowsTheText()
    {
        // Arrange
        var shifts = Substitute.For<IShiftService>();
        shifts.CreateAsync(Arg.Any<SaveShiftRequest>(), Arg.Any<CancellationToken>()).Returns(Result<ShiftDto>.Failure(ShiftErrors.Overlap));
        var viewModel = ShiftPlan(shifts, today: new DateOnly(2026, 1, 8));
        viewModel.BeginCreate(new DateOnly(2026, 1, 6));

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsEditing);
        Assert.Equal(["That member of staff already has a shift at that time."], viewModel.Errors);
    }

    /// <summary>Positive: loading keeps only the trades that wait for a Manager, and only active accounts.</summary>
    [Fact]
    public async Task LoadAsync_Trades_KeepsTheOnesAwaitingApproval()
    {
        // Arrange
        var shift = new ShiftDto(1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(8), StaffRole.Housekeeping, 2, "Hilda Berg");
        var shifts = Substitute.For<IShiftService>();
        shifts.ListAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ShiftDto>>.Success([shift]));
        shifts.ListTradesAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ShiftTradeDto>>.Success(
        [
            new ShiftTradeDto(1, shift, null, 2, "Hilda Berg", 3, "Otto Lund", TradeState.AwaitingApproval, false, DateTimeOffset.UnixEpoch),
            new ShiftTradeDto(2, shift, null, 2, "Hilda Berg", null, null, TradeState.Open, false, DateTimeOffset.UnixEpoch),
        ]));
        var viewModel = ShiftPlan(shifts, today: new DateOnly(2026, 1, 8));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        var waiting = Assert.Single(viewModel.AwaitingApproval);
        Assert.Contains("Give away", viewModel.Describe(waiting), StringComparison.Ordinal);
        Assert.Single(viewModel.Staff);
    }

    #endregion

    #region Room assignments

    /// <summary>Positive: an assignment calls its use case and loads again; a housekeeper below the minimum is told.</summary>
    [Fact]
    public async Task AssignAsync_Success_ReloadsAndTellsTheMinimum()
    {
        // Arrange
        var room = new RoomAssignmentDto(7, "207", 2, null, null);
        var service = Substitute.For<IRoomAssignmentService>();
        service.GetOverviewAsync(Arg.Any<CancellationToken>()).Returns(Result<RoomAssignmentOverviewDto>.Success(
            new RoomAssignmentOverviewDto([room], [new HousekeeperDto(3, "Otto Lund", 2), new HousekeeperDto(4, "Nora", 3)], 3)));
        service.ListTradesAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<RoomTradeDto>>.Success([]));
        service.AssignAsync(7, Arg.Any<AssignRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var viewModel = new RoomAssignmentsViewModel(Runner(service));

        // Act
        await viewModel.AssignAsync(room, 3, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).AssignAsync(7, new AssignRoomRequest(3), Arg.Any<CancellationToken>());
        Assert.Equal("Assigned room 207.", viewModel.SuccessMessage);
        Assert.True(viewModel.IsBelowMinimum(viewModel.Overview!.Housekeepers[0]));
        Assert.False(viewModel.IsBelowMinimum(viewModel.Overview.Housekeepers[1]));
    }

    /// <summary>Equivalence: a gift and a swap are described with their rooms.</summary>
    [Fact]
    public void Describe_GiftAndSwap_NamesTheRooms()
    {
        // Arrange
        TradedRoomDto[] give = [new(1, "101"), new(2, "102")];
        var gift = new RoomTradeDto(1, 2, "Hilda Berg", 3, "Otto Lund", give, [], TradeState.AwaitingApproval, DateTimeOffset.UnixEpoch);
        var swap = gift with { Take = [new TradedRoomDto(5, "201")] };

        // Act and assert
        Assert.Equal("Hilda Berg gives 101, 102 to Otto Lund", RoomAssignmentsViewModel.Describe(gift));
        Assert.Equal("Hilda Berg gives 101, 102 to Otto Lund for 201", RoomAssignmentsViewModel.Describe(swap));
    }

    #endregion

    #region Helpers

    private static ShiftPlanViewModel ShiftPlan(IShiftService shifts, DateOnly today)
    {
        var staff = Substitute.For<IStaffAdminService>();
        staff.ListAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<StaffAccountDto>>.Success(
        [
            new StaffAccountDto(2, "hilda", StaffRole.Housekeeping, true, false, "Hilda", "Berg"),
            new StaffAccountDto(5, "gone", StaffRole.Housekeeping, false, false),
        ]));
        var clock = Substitute.For<IClock>();
        clock.Today.Returns(today);
        return new ShiftPlanViewModel(Runner(shifts), Runner(staff), clock, Copenhagen);
    }

    private static IUseCaseRunner<TService> Runner<TService>(TService service)
        where TService : class
    {
        var runner = Substitute.For<IUseCaseRunner<TService>>();
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result>>>()(service, call.Arg<CancellationToken>()));
        Forward<TService, IReadOnlyList<ShiftDto>>(runner, service);
        Forward<TService, IReadOnlyList<ShiftTradeDto>>(runner, service);
        Forward<TService, IReadOnlyList<StaffAccountDto>>(runner, service);
        Forward<TService, RoomAssignmentOverviewDto>(runner, service);
        Forward<TService, IReadOnlyList<RoomTradeDto>>(runner, service);
        return runner;
    }

    private static void Forward<TService, TValue>(IUseCaseRunner<TService> runner, TService service)
        where TService : class =>
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result<TValue>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result<TValue>>>>()(service, call.Arg<CancellationToken>()));

    #endregion
}
