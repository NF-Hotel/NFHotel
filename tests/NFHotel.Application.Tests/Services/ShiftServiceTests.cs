using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.UseCases.Shifts;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The shifts and their trades (MIL-060): a Manager plans, a member gives away or swaps, the colleague takes or accepts,
/// a Manager approves, and the cut-off, the roles and the overlaps hold throughout.
/// </summary>
public sealed class ShiftServiceTests
{
    /// <summary>The clock of the world is 2026-01-01 12:00 UTC; this is three days later at 07:00.</summary>
    private static readonly DateTimeOffset Monday = new(2026, 1, 5, 7, 0, 0, TimeSpan.Zero);

    #region Planning

    /// <summary>Positive: a Manager plans a shift for a member with its role, and the member's list shows it.</summary>
    [Fact]
    public async Task CreateAsync_ForAMemberWithTheRole_IsListedForTheRole()
    {
        // Arrange
        var plan = new Plan();

        // Act
        var created = await plan.Service(plan.Manager).CreateAsync(
            new SaveShiftRequest(Monday, Monday.AddHours(8), StaffRole.Housekeeping, plan.Hilda.Id),
            TestContext.Current.CancellationToken);
        var hilda = await plan.Service(plan.Hilda).ListAsync(Monday.AddDays(-1), Monday.AddDays(1), TestContext.Current.CancellationToken);
        var rita = await plan.Service(plan.Rita).ListAsync(Monday.AddDays(-1), Monday.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Hilda Berg", created.Value.AssigneeName);
        Assert.Single(hilda.Value);
        Assert.Empty(rita.Value);
    }

    /// <summary>Negative: a shift cannot go to a member of another role or to one who already works then.</summary>
    [Fact]
    public async Task CreateAsync_OtherRoleOrOverlap_IsRefused()
    {
        // Arrange
        var plan = new Plan();
        plan.Shift(plan.Hilda, Monday);
        var service = plan.Service(plan.Manager);

        // Act
        var otherRole = await service.CreateAsync(new SaveShiftRequest(Monday, Monday.AddHours(8), StaffRole.Receptionist, plan.Hilda.Id), TestContext.Current.CancellationToken);
        var overlap = await service.CreateAsync(new SaveShiftRequest(Monday.AddHours(4), Monday.AddHours(12), StaffRole.Housekeeping, plan.Hilda.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.AssigneeInvalid, otherRole.FirstError?.Code);
        Assert.Equal(ShiftErrors.Overlap, overlap.FirstError?.Code);
    }

    /// <summary>Boundary: a period of 62 days is listed, one longer or backwards is refused.</summary>
    [Fact]
    public async Task ListAsync_Period_IsAtMostSixtyTwoDays()
    {
        // Arrange
        var plan = new Plan();
        var service = plan.Service(plan.Manager);

        // Act
        var longest = await service.ListAsync(Monday, Monday.AddDays(62), TestContext.Current.CancellationToken);
        var tooLong = await service.ListAsync(Monday, Monday.AddDays(62).AddMinutes(1), TestContext.Current.CancellationToken);
        var backwards = await service.ListAsync(Monday, Monday, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(longest.IsSuccess);
        Assert.Equal(ShiftErrors.PeriodInvalid, tooLong.FirstError?.Code);
        Assert.Equal(ShiftErrors.PeriodInvalid, backwards.FirstError?.Code);
    }

    #endregion

    #region Give away

    /// <summary>Positive: a shift given away is taken by a colleague with its role, approved, and changes hands.</summary>
    [Fact]
    public async Task GiveAway_TakenAndApproved_ChangesHands()
    {
        // Arrange
        var plan = new Plan();
        var shift = plan.Shift(plan.Hilda, Monday);
        var offer = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);

        // Act
        var taken = await plan.Service(plan.Otto).TakeTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);
        var stillHers = shift.AssigneeId;
        var approved = await plan.Service(plan.Manager).ApproveTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TradeState.AwaitingApproval, taken.Value.State);
        Assert.Equal(plan.Hilda.Id, stillHers);
        Assert.Equal(TradeState.Done, approved.Value.State);
        Assert.Equal(plan.Otto.Id, shift.AssigneeId);
    }

    /// <summary>Negative: a colleague of another role, or one who works then, cannot take the shift.</summary>
    [Fact]
    public async Task TakeTradeAsync_OtherRoleOrOverlap_IsRefused()
    {
        // Arrange
        var plan = new Plan();
        var shift = plan.Shift(plan.Hilda, Monday);
        plan.Shift(plan.Otto, Monday.AddHours(2));
        var offer = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);

        // Act
        var rita = await plan.Service(plan.Rita).TakeTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);
        var otto = await plan.Service(plan.Otto).TakeTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.TradeWrongRole, rita.FirstError?.Code);
        Assert.Equal(ShiftErrors.Overlap, otto.FirstError?.Code);
    }

    /// <summary>Boundary: a shift that starts within 24 hours can no longer be offered; one just outside can.</summary>
    [Fact]
    public async Task OfferTradeAsync_AroundTheCutoff_ClosesAtTwentyFourHours()
    {
        // Arrange
        var plan = new Plan();
        var now = plan.Data.Clock.UtcNow;
        var inside = plan.Shift(plan.Hilda, now.AddHours(24));
        var outside = plan.Shift(plan.Hilda, now.AddHours(24).AddMinutes(1).AddDays(1));

        // Act
        var closed = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(inside.Id, null), TestContext.Current.CancellationToken);
        var open = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(outside.Id, null), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.TradeExpired, closed.FirstError?.Code);
        Assert.True(open.IsSuccess);
    }

    /// <summary>Negative: only the member who works a shift offers it, and only once at a time.</summary>
    [Fact]
    public async Task OfferTradeAsync_NotYoursOrTwice_IsRefused()
    {
        // Arrange
        var plan = new Plan();
        var shift = plan.Shift(plan.Hilda, Monday);

        // Act
        var otto = await plan.Service(plan.Otto).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);
        await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);
        var twice = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.TradeNotYours, otto.FirstError?.Code);
        Assert.Equal(ShiftErrors.TradeAlreadyOffered, twice.FirstError?.Code);
    }

    #endregion

    #region Swap

    /// <summary>Positive: a swap accepted by the colleague and approved exchanges the two shifts.</summary>
    [Fact]
    public async Task Swap_AcceptedAndApproved_ExchangesTheShifts()
    {
        // Arrange
        var plan = new Plan();
        var hers = plan.Shift(plan.Hilda, Monday);
        var his = plan.Shift(plan.Otto, Monday.AddDays(1));
        var offer = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(hers.Id, his.Id), TestContext.Current.CancellationToken);

        // Act
        var accepted = await plan.Service(plan.Otto).AcceptTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);
        await plan.Service(plan.Manager).ApproveTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TradeState.AwaitingApproval, accepted.Value.State);
        Assert.Equal((plan.Otto.Id, plan.Hilda.Id), (hers.AssigneeId, his.AssigneeId));
    }

    /// <summary>Negative: a swap with a shift of another role is refused, and only the colleague of the swap accepts it.</summary>
    [Fact]
    public async Task Swap_OtherRoleOrWrongColleague_IsRefused()
    {
        // Arrange
        var plan = new Plan();
        var hers = plan.Shift(plan.Hilda, Monday);
        var ritas = plan.Shift(plan.Rita, Monday.AddDays(1), StaffRole.Receptionist);
        var ottos = plan.Shift(plan.Otto, Monday.AddDays(1));

        // Act
        var otherRole = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(hers.Id, ritas.Id), TestContext.Current.CancellationToken);
        var offer = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(hers.Id, ottos.Id), TestContext.Current.CancellationToken);
        var byNora = await plan.Service(plan.Nora).AcceptTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.TradeSwapInvalid, otherRole.FirstError?.Code);
        Assert.Equal(ShiftErrors.TradeNotYours, byNora.FirstError?.Code);
    }

    /// <summary>State transition: when a Manager moved the shift meanwhile, approving calls the trade off.</summary>
    [Fact]
    public async Task ApproveTradeAsync_ShiftMovedMeanwhile_CancelsTheTrade()
    {
        // Arrange
        var plan = new Plan();
        var shift = plan.Shift(plan.Hilda, Monday);
        var offer = await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(shift.Id, null), TestContext.Current.CancellationToken);
        await plan.Service(plan.Otto).TakeTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);
        await plan.Service(plan.Manager).UpdateAsync(shift.Id, new SaveShiftRequest(Monday, Monday.AddHours(8), StaffRole.Housekeeping, plan.Nora.Id), TestContext.Current.CancellationToken);

        // Act
        var result = await plan.Service(plan.Manager).ApproveTradeAsync(offer.Value.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ShiftErrors.TradeShiftChanged, result.FirstError?.Code);
        Assert.Equal(TradeState.Cancelled, plan.Data.Shifts.Trades.Items.Single().State);
        Assert.Equal(plan.Nora.Id, shift.AssigneeId);
    }

    #endregion

    #region Lists

    /// <summary>
    /// Equivalence: a shift given away shows to the colleagues of its role, not to other roles; a swap shows only to its two
    /// members; a Manager sees all.
    /// </summary>
    [Fact]
    public async Task ListTradesAsync_ByRoleAndParty_ShowsWhatEachMayActOn()
    {
        // Arrange
        var plan = new Plan();
        var gift = plan.Shift(plan.Hilda, Monday);
        var hers = plan.Shift(plan.Hilda, Monday.AddDays(2));
        var his = plan.Shift(plan.Otto, Monday.AddDays(3));
        await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(gift.Id, null), TestContext.Current.CancellationToken);
        await plan.Service(plan.Hilda).OfferTradeAsync(new OfferShiftTradeRequest(hers.Id, his.Id), TestContext.Current.CancellationToken);

        // Act
        var nora = await plan.Service(plan.Nora).ListTradesAsync(TestContext.Current.CancellationToken);
        var otto = await plan.Service(plan.Otto).ListTradesAsync(TestContext.Current.CancellationToken);
        var rita = await plan.Service(plan.Rita).ListTradesAsync(TestContext.Current.CancellationToken);
        var manager = await plan.Service(plan.Manager).ListTradesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(nora.Value);
        Assert.Equal(2, otto.Value.Count);
        Assert.Empty(rita.Value);
        Assert.Equal(2, manager.Value.Count);
    }

    #endregion

    /// <summary>A Manager, three housekeepers and a receptionist.</summary>
    private sealed class Plan
    {
        public Plan()
        {
            Manager = AddStaff("mia", StaffRole.Manager, "Mia", "Holm");
            Hilda = AddStaff("hilda", StaffRole.Housekeeping, "Hilda", "Berg");
            Otto = AddStaff("otto", StaffRole.Housekeeping, "Otto", "Lund");
            Nora = AddStaff("nora", StaffRole.Housekeeping, null, null);
            Rita = AddStaff("rita", StaffRole.Receptionist, "Rita", "Hansen");
        }

        public ServiceWorld Data { get; } = new();

        public StaffMember Manager { get; }

        public StaffMember Hilda { get; }

        public StaffMember Otto { get; }

        public StaffMember Nora { get; }

        public StaffMember Rita { get; }

        public ShiftService Service(StaffMember caller) =>
            Data.ShiftService(context: new FakeStaffCallContext { StaffId = caller.Id, StaffName = caller.UserName });

        public Shift Shift(StaffMember member, DateTimeOffset start, StaffRole role = StaffRole.Housekeeping)
        {
            var shift = NFHotel.Domain.Entities.Shift.Plan(start, start.AddHours(8), role, member.Id);
            Data.Shifts.Add(shift);
            return shift;
        }

        private StaffMember AddStaff(string userName, StaffRole role, string? first, string? last)
        {
            var member = StaffMember.Create(userName, "hash", role, firstName: first, lastName: last);
            Data.Staff.Add(member);
            return member;
        }
    }
}
