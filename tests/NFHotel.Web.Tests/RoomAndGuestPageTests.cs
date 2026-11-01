using Bunit;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Guests;
using NFHotel.Web.Features.Admin.Rooms;
using NFHotel.Web.Security;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The room page of the back office, rendered and clicked (MIL-054 task 4).</summary>
public sealed class RoomListPageTests : IAsyncDisposable
{
    #region Setup

    private readonly IRoomService _rooms = Substitute.For<IRoomService>();
    private readonly PageHost _host = new();

    public RoomListPageTests()
    {
        var runner = new PassThroughRunner<IRoomService>(_rooms);
        _host.With(new RoomListViewModel(runner)).With(new RoomFormViewModel(runner));
        _rooms.GetFilterOptionsAsync(Arg.Any<CancellationToken>()).Returns(Ok(new RoomFilterOptionsDto(
            [1, 2],
            Enum.GetValues<RoomSize>(),
            Enum.GetValues<RoomStatus>(),
            Enum.GetValues<HousekeepingStatus>())));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void SearchFinds(params RoomListItemDto[] rooms) =>
        _rooms.SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>()).Returns(List(rooms));

    #endregion

    #region Render 1

    /// <summary>Positive: a manager sees the rooms with edit, delete and every action of the selected room.</summary>
    [Fact]
    public void Render_Manager_SeesRoomsAndActions()
    {
        SearchFinds(Room(1), Room(2, RoomStatus.Maintenance, HousekeepingStatus.ServiceRequired));
        _rooms.StartServiceAsync(2, Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(2)));
        _host.SignedInWith(StaffPolicies.RoomsManage, StaffPolicies.HousekeepingUpdate);
        var page = _host.Render<RoomList>();

        Assert.Contains("Maintenance", page.Markup, StringComparison.Ordinal);
        Assert.Equal(2, page.FindAll("button").Count(button => button.TextContent.Trim() == "Edit"));
        page.FindAll("button").Last(button => button.TextContent.Trim() == "Actions").Click();
        Assert.Contains("Room 102", page.Markup, StringComparison.Ordinal);
        Assert.True(page.HasButton("Hide actions"));
        page.Button("Return to service");
        page.Button("Start service").Click();

        _rooms.Received(1).StartServiceAsync(2, Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a member without the manage policy sees no edit, delete or new room buttons.</summary>
    [Fact]
    public void Render_Housekeeper_SeesNoManageButtons()
    {
        SearchFinds(Room(1, housekeeping: HousekeepingStatus.DailyCleaningDue));
        _host.SignedInWith(StaffPolicies.HousekeepingUpdate);
        var page = _host.Render<RoomList>();

        Assert.False(page.HasButton("Edit"));
        Assert.False(page.HasButton("New room"));
        page.Button("Actions").Click();
        Assert.True(page.HasButton("Start cleaning"));
        Assert.False(page.HasButton("Take out of service"));
    }

    #endregion

    #region Actions

    /// <summary>Edge: a room with no allowed action says so.</summary>
    [Fact]
    public void Actions_NoneAllowed_SaysSo()
    {
        SearchFinds(Room(1, RoomStatus.Available, HousekeepingStatus.CleaningInProgress));
        _host.SignedInWith();
        var page = _host.Render<RoomList>();

        page.Button("Actions").Click();

        Assert.Contains("Room 101", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Form

    /// <summary>Positive: a room is created and edited through the form, and the list loads again.</summary>
    [Fact]
    public void Form_CreateThenEdit_Saves()
    {
        SearchFinds(Room(1));
        _rooms.CreateAsync(Arg.Any<CreateRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(5)));
        _rooms.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(1)));
        _rooms.UpdateAsync(Arg.Any<UpdateRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(1)));
        _host.SignedInWith(StaffPolicies.RoomsManage);
        var page = _host.Render<RoomList>();

        page.Button("New room").Click();
        page.Find("#room-number").Change("105");
        page.Find("#room-floor").Change("1");
        page.Find("#room-size").Change(nameof(RoomSize.Suite));
        page.Find("#room-capacity").Change("4");
        page.Button("Save room").Click();
        Assert.Contains("Room 105 was created.", page.Markup, StringComparison.Ordinal);
        _rooms.Received(1).CreateAsync(new CreateRoomRequest("105", 1, RoomSize.Suite, 4), Arg.Any<CancellationToken>());

        page.Button("Edit").Click();
        Assert.Contains("Edit room 101", page.Markup, StringComparison.Ordinal);
        page.Button("Save room").Click();
        Assert.Contains("Room 101 was updated.", page.Markup, StringComparison.Ordinal);

        page.Button("New room").Click();
        page.Button("Cancel").Click();
        Assert.Empty(page.FindAll("#room-form"));
    }

    #endregion

    #region Delete

    /// <summary>State transition: a deletion asks first, can be aborted, and deletes on confirm.</summary>
    [Fact]
    public void Delete_AskThenAbortThenConfirm_DeletesOnce()
    {
        SearchFinds(Room(1));
        _rooms.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _host.SignedInWith(StaffPolicies.RoomsManage);
        var page = _host.Render<RoomList>();

        page.Button("Delete").Click();
        Assert.Contains("Delete room 101?", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        page.Button("Delete").Click();
        page.Button("Delete room").Click();

        Assert.Contains("Room 101 was deleted.", page.Markup, StringComparison.Ordinal);
        _rooms.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Filter

    /// <summary>Equivalence: the four filters are sent; an empty filtered result and an empty hotel each have their state.</summary>
    [Fact]
    public void Filter_FieldsAndEmptyStates_Work()
    {
        SearchFinds();
        _host.SignedInWith(StaffPolicies.RoomsManage);
        var page = _host.Render<RoomList>();
        Assert.Contains("There are no rooms yet", page.Markup, StringComparison.Ordinal);

        page.Find("#filter-floor").Change("2");
        page.Find("#filter-size").Change(nameof(RoomSize.Double));
        page.Find("#filter-status").Change(nameof(RoomStatus.Maintenance));
        page.Find("#filter-housekeeping").Change(nameof(HousekeepingStatus.Clean));
        page.Button("Search").Click();
        Assert.Contains("No rooms match the filter", page.Markup, StringComparison.Ordinal);
        _rooms.Received(1).SearchAsync(
            new RoomFilter(2, RoomSize.Double, RoomStatus.Maintenance, HousekeepingStatus.Clean),
            Arg.Any<CancellationToken>());

        page.FindAll("button").Last(button => button.TextContent.Trim() == "Clear filter").Click();
        page.Button("Clear filter").Click();
        Assert.Contains("There are no rooms yet", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Render 2

    /// <summary>Error handling: a refused load shows the error text.</summary>
    [Fact]
    public void Render_OptionsRefused_ShowsTheError()
    {
        _rooms.GetFilterOptionsAsync(Arg.Any<CancellationToken>()).Returns(Fail<RoomFilterOptionsDto>(ErrorCodes.Room.NotFound));
        _host.SignedInWith();

        var page = _host.Render<RoomList>();

        Assert.Contains(Text(ErrorCodes.Room.NotFound), page.Markup, StringComparison.Ordinal);
    }

    #endregion
}

/// <summary>The guest page of the back office, rendered and clicked (MIL-054 task 5).</summary>
public sealed class GuestListPageTests : IAsyncDisposable
{
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly PageHost _host = new();

    public GuestListPageTests()
    {
        _host.With(new GuestListViewModel(new PassThroughRunner<IGuestService>(_guests)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void SearchFinds(params GuestListItemDto[] guests) =>
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(List(guests));

    /// <summary>Positive: the guests are listed, and the passport number is never shown, only whether it is on file.</summary>
    [Fact]
    public void Render_Guests_HidesThePassportNumber()
    {
        SearchFinds(Guest(1, hasPassport: true), Guest(2, "Ole"));

        var page = _host.Render<GuestList>();

        Assert.Contains("2 guests", page.Markup, StringComparison.Ordinal);
        Assert.Contains("On file", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Ole Hansen", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Positive: a new guest is typed in, validated as it is typed, and saved.</summary>
    [Fact]
    public void Form_NewGuest_ValidatesAndSaves()
    {
        SearchFinds();
        _guests.CreateAsync(Arg.Any<CreateGuestRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(1)));
        var page = _host.Render<GuestList>();

        page.Button("New guest").Click();
        foreach (var field in new[] { "#guest-first-name", "#guest-last-name", "#guest-email", "#guest-phone", "#guest-country", "#guest-passport" })
        {
            page.Find(field).Input("x");
        }

        page.Button("Save guest").Click();

        _guests.Received(6).ValidateFields(Arg.Any<GuestFields>());
        Assert.Contains("Rita Hansen was created.", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Positive: an existing guest is edited; cancelling closes the form.</summary>
    [Fact]
    public void Form_EditThenCancel_LoadsAndCloses()
    {
        SearchFinds(Guest(1));
        _guests.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(1)));
        var page = _host.Render<GuestList>();

        page.Button("Edit").Click();
        Assert.Contains("Edit guest", page.Markup, StringComparison.Ordinal);
        page.Button("Cancel").Click();

        Assert.Empty(page.FindAll("#guest-form"));
    }

    /// <summary>State transition: a deletion asks first, can be aborted, and deletes on confirm.</summary>
    [Fact]
    public void Delete_AskThenAbortThenConfirm_DeletesOnce()
    {
        SearchFinds(Guest(1));
        _guests.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var page = _host.Render<GuestList>();

        page.Button("Delete").Click();
        Assert.Contains("Delete Rita Hansen?", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        page.Button("Delete").Click();
        page.Button("Delete guest").Click();

        _guests.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    /// <summary>Edge: an empty search offers to clear it; an empty register offers to create the first guest.</summary>
    [Fact]
    public void Search_EmptyResults_ShowTheirStates()
    {
        SearchFinds();
        var page = _host.Render<GuestList>();
        Assert.Contains("The register is empty", page.Markup, StringComparison.Ordinal);

        page.Find("#guest-search").Change("zed");
        page.Button("Search").Click();
        Assert.Contains("No guests match that search", page.Markup, StringComparison.Ordinal);

        page.FindAll("button").Last(button => button.TextContent.Trim() == "Clear search").Click();
        page.Button("Clear search").Click();
        page.FindAll("button").Last(button => button.TextContent.Trim() == "New guest").Click();
        Assert.Contains("New guest", page.Find("#guest-form").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Error handling: a refused search shows the error text.</summary>
    [Fact]
    public void Render_Refused_ShowsTheError()
    {
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<GuestListItemDto>>(ErrorCodes.Guest.NotFound));

        var page = _host.Render<GuestList>();

        Assert.Contains(Text(ErrorCodes.Guest.NotFound), page.Markup, StringComparison.Ordinal);
    }
}
