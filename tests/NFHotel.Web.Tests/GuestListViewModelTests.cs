using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Features.Admin.Guests;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the guest page of the back office (MIL-054 task 5).</summary>
public sealed class GuestListViewModelTests
{
    #region Setup

    private readonly IGuestService _guests = Substitute.For<IGuestService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private GuestListViewModel Create() => new(new PassThroughRunner<IGuestService>(_guests));

    private void SearchFinds(params GuestListItemDto[] guests) =>
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(List(guests));

    #endregion

    #region Constructor

    /// <summary>Negative: the runner is required.</summary>
    [Fact]
    public void Constructor_MissingRunner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GuestListViewModel(null!));
    }

    #endregion

    #region InitializeAsync

    /// <summary>Positive: the first load lists every guest.</summary>
    [Fact]
    public async Task InitializeAsync_Guests_ListsThem()
    {
        SearchFinds(Guest(1), Guest(2, "Ole"));
        var viewModel = Create();

        await viewModel.InitializeAsync(Token);

        Assert.True(viewModel.IsLoaded);
        Assert.Equal(2, viewModel.Guests.Count);
        Assert.False(viewModel.IsEmpty);
        Assert.False(viewModel.IsBusy);
    }

    #endregion

    #region SearchAsync

    /// <summary>Equivalence: an empty result with search text is an empty search; without it, an empty register.</summary>
    [Fact]
    public async Task SearchAsync_NoGuests_TellsAnEmptySearchFromAnEmptyRegister()
    {
        SearchFinds();
        var viewModel = Create();
        viewModel.SearchText = "zed";

        await viewModel.SearchAsync(Token);
        Assert.True(viewModel.IsEmptySearch);

        await viewModel.ClearSearchAsync(Token);
        Assert.Null(viewModel.SearchText);
        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.IsEmptySearch);
    }

    /// <summary>Error handling: a refused search shows the text and empties the list.</summary>
    [Fact]
    public async Task SearchAsync_Refused_ShowsTheError()
    {
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<GuestListItemDto>>(ErrorCodes.Guest.NotFound));
        var viewModel = Create();

        await viewModel.SearchAsync(Token);

        Assert.Equal([Text(ErrorCodes.Guest.NotFound)], viewModel.Errors);
        Assert.Empty(viewModel.Guests);
        Assert.False(viewModel.IsEmpty);
    }

    #endregion

    #region BeginCreate

    /// <summary>State transition: a new form is empty and cannot be saved before it is valid.</summary>
    [Fact]
    public void BeginCreate_AfterEdit_OpensAnEmptyForm()
    {
        var viewModel = Create();
        viewModel.FirstName = "Old";
        viewModel.PassportNumber = "P1";

        viewModel.BeginCreate();

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal("New guest", viewModel.FormTitle);
        Assert.Equal(string.Empty, viewModel.FirstName);
        Assert.Null(viewModel.PassportNumber);
        Assert.False(viewModel.CanSave);
    }

    #endregion

    #region BeginEditAsync

    /// <summary>Positive: editing loads the guest into the form and validates it.</summary>
    [Fact]
    public async Task BeginEditAsync_Guest_FillsTheFormAndValidates()
    {
        _guests.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(3, "P123")));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        var viewModel = Create();

        await viewModel.BeginEditAsync(3, Token);

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal(3, viewModel.EditingGuestId);
        Assert.Equal("Edit guest", viewModel.FormTitle);
        Assert.Equal("Rita", viewModel.FirstName);
        Assert.Equal("Hansen", viewModel.LastName);
        Assert.Equal("rita@example.com", viewModel.Email);
        Assert.Equal("+4512345678", viewModel.PhoneNumber);
        Assert.Equal("Denmark", viewModel.Country);
        Assert.Equal("P123", viewModel.PassportNumber);
        Assert.True(viewModel.CanSave);
    }

    /// <summary>Error handling: a guest that cannot be read leaves the form closed with the text.</summary>
    [Fact]
    public async Task BeginEditAsync_Refused_KeepsTheFormClosed()
    {
        _guests.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Fail<GuestDetailsDto>(ErrorCodes.Guest.NotFound));
        var viewModel = Create();

        await viewModel.BeginEditAsync(3, Token);

        Assert.False(viewModel.IsFormOpen);
        Assert.Equal([Text(ErrorCodes.Guest.NotFound)], viewModel.Errors);
    }

    #endregion

    #region Validate

    /// <summary>Negative: invalid fields give the texts and block the save; an empty passport is sent as missing.</summary>
    [Fact]
    public void Validate_InvalidFields_BlocksTheSave()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Failure(ErrorCodes.Guest.NotFound));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.PassportNumber = " ";

        viewModel.Validate();

        Assert.False(viewModel.CanSave);
        Assert.Equal([Text(ErrorCodes.Guest.NotFound)], viewModel.FormErrors);
        _guests.Received(1).ValidateFields(Arg.Is<GuestFields>(fields => fields.PassportNumber == null));
    }

    #endregion

    #region SaveAsync

    /// <summary>Positive: a new guest is created, the form closes, the list is loaded again and the receipt stays.</summary>
    [Fact]
    public async Task SaveAsync_NewGuest_CreatesAndKeepsTheReceipt()
    {
        SearchFinds(Guest(1));
        _guests.CreateAsync(Arg.Any<CreateGuestRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(1)));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.FirstName = "Rita";

        await viewModel.SaveAsync(Token);

        Assert.False(viewModel.IsFormOpen);
        Assert.Equal("Rita Hansen was created.", viewModel.SuccessMessage);
        Assert.Single(viewModel.Guests);
        await _guests.Received(1).CreateAsync(
            Arg.Is<CreateGuestRequest>(request => request.Fields.FirstName == "Rita"),
            Arg.Any<CancellationToken>());

        viewModel.DismissSuccess();
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>Positive: an edited guest is updated under its own id.</summary>
    [Fact]
    public async Task SaveAsync_EditedGuest_Updates()
    {
        SearchFinds();
        _guests.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(3)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _guests.UpdateAsync(Arg.Any<UpdateGuestRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(3)));
        var viewModel = Create();
        await viewModel.BeginEditAsync(3, Token);

        await viewModel.SaveAsync(Token);

        Assert.Equal("Rita Hansen was updated.", viewModel.SuccessMessage);
        Assert.Null(viewModel.EditingGuestId);
        await _guests.Received(1).UpdateAsync(Arg.Is<UpdateGuestRequest>(request => request.GuestId == 3), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused save keeps the form open with the texts and does not reload.</summary>
    [Fact]
    public async Task SaveAsync_Refused_KeepsTheFormOpen()
    {
        _guests.CreateAsync(Arg.Any<CreateGuestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<GuestDetailsDto>(ErrorCodes.Guest.ConcurrencyConflict));
        var viewModel = Create();
        viewModel.BeginCreate();

        await viewModel.SaveAsync(Token);

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal([Text(ErrorCodes.Guest.ConcurrencyConflict)], viewModel.FormErrors);
        await _guests.DidNotReceiveWithAnyArgs().SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SaveAndDelete

    /// <summary>Concurrency: a save or a deletion while a load runs is ignored.</summary>
    [Fact]
    public async Task SaveAndDelete_WhileLoading_AreIgnored()
    {
        var pending = new TaskCompletionSource<Result<IReadOnlyList<GuestListItemDto>>>();
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = Create();
        viewModel.RequestDelete(Guest(1));

        var load = viewModel.SearchAsync(Token);
        await viewModel.SaveAsync(Token);
        viewModel.RequestDelete(Guest(1));
        await viewModel.ConfirmDeleteAsync(Token);
        pending.SetResult(Result<IReadOnlyList<GuestListItemDto>>.Success([]));
        await load;

        await _guests.DidNotReceiveWithAnyArgs().CreateAsync(Arg.Any<CreateGuestRequest>(), Arg.Any<CancellationToken>());
        await _guests.DidNotReceiveWithAnyArgs().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ConfirmDeleteAsync

    /// <summary>Positive: deleting the guest being edited closes the form, reloads and keeps the receipt.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_GuestBeingEdited_ClosesTheForm()
    {
        SearchFinds();
        _guests.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(1)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _guests.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var viewModel = Create();
        await viewModel.BeginEditAsync(1, Token);
        viewModel.RequestDelete(Guest(1));

        await viewModel.ConfirmDeleteAsync(Token);

        Assert.False(viewModel.IsFormOpen);
        Assert.Null(viewModel.GuestPendingDeletion);
        Assert.Equal("Rita Hansen was deleted.", viewModel.SuccessMessage);
    }

    /// <summary>Positive: deleting another guest keeps the open form.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_OtherGuest_KeepsTheForm()
    {
        SearchFinds();
        _guests.DeleteAsync(2, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.RequestDelete(Guest(2, "Ole"));

        await viewModel.ConfirmDeleteAsync(Token);

        Assert.True(viewModel.IsFormOpen);
        Assert.Equal("Ole Hansen was deleted.", viewModel.SuccessMessage);
    }

    /// <summary>Error handling: a refused deletion shows the text and clears the question.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_Refused_ShowsTheError()
    {
        _guests.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure(ErrorCodes.Guest.HasBookings)));
        var viewModel = Create();
        viewModel.RequestDelete(Guest(1));

        await viewModel.ConfirmDeleteAsync(Token);

        Assert.Equal([Text(ErrorCodes.Guest.HasBookings)], viewModel.Errors);
        Assert.Null(viewModel.GuestPendingDeletion);
        Assert.Null(viewModel.SuccessMessage);
    }

    #endregion

    #region RequestDelete

    /// <summary>State transition: a deletion can be aborted, nothing is deleted without a question, and a missing guest is refused.</summary>
    [Fact]
    public async Task RequestDelete_ThenAbort_DeletesNothing()
    {
        var viewModel = Create();
        viewModel.RequestDelete(Guest(1));
        viewModel.AbortDelete();

        await viewModel.ConfirmDeleteAsync(Token);

        await _guests.DidNotReceiveWithAnyArgs().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        Assert.Throws<ArgumentNullException>(() => viewModel.RequestDelete(null!));
    }

    #endregion

    #region CancelEdit

    /// <summary>State transition: cancelling the edit closes and resets the form.</summary>
    [Fact]
    public async Task CancelEdit_OpenForm_Closes()
    {
        _guests.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(GuestDetails(1)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        var viewModel = Create();
        await viewModel.BeginEditAsync(1, Token);

        viewModel.CancelEdit();

        Assert.False(viewModel.IsFormOpen);
        Assert.Null(viewModel.EditingGuestId);
        Assert.False(viewModel.CanSave);
        Assert.Empty(viewModel.FormErrors);
    }

    #endregion
}
