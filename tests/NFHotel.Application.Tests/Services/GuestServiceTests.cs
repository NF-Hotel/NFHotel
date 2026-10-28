using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.TestSupport;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// Unit tests of <c>GuestService</c> with in-memory repositories and a fake unit of work: no database,
/// no sleeping, no network.
/// </summary>
public sealed class GuestServiceTests
{
    private static IStaffAuthorizationService CreateAuthorization(
    params string[] permissions)
    {
        var authorization = Substitute.For<IStaffAuthorizationService>();

        foreach (var permission in permissions)
        {
            authorization.HasPermissionAsync(
                permission,
                Arg.Any<CancellationToken>())
                .Returns(true);
        }

        return authorization;
    }

    #region Reading

    /// <summary>Positive: an existing guest is returned as the query port describes it.</summary>
    [Fact]
    public async Task GetByIdAsync_ExistingGuest_ReturnsTheDetails()
    {
        // Arrange
        var world = new ServiceWorld();
        var details = new GuestDetailsDto(1, "Anna", "Jensen", "Anna Jensen", "anna@example.com", "+4512345678", "Denmark", null);
        world.GuestQueries.GetDetailsAsync(1, TestContext.Current.CancellationToken).Returns(details);

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.GetByIdAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(details, result.Value);
    }

    /// <summary>Negative: an unknown guest is reported with the not-found code.</summary>
    [Fact]
    public async Task GetByIdAsync_UnknownGuest_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.GetByIdAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.NotFound, result.FirstError?.Code);
    }

    /// <summary>Boundary: a key below one is a programming error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetByIdAsync_KeyBelowOne_ThrowsArgumentOutOfRange(int guestId)
    {
        // Arrange
        var service = new ServiceWorld().GuestService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetByIdAsync(guestId, TestContext.Current.CancellationToken));
    }

    /// <summary>Security: accessing guest details without permissions is forbidden.</summary>
    [Fact]
    public async Task GetByIdAsync_WithoutPermissions_ReturnsForbidden()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.GuestService();

        // Act
        var result = await service.GetByIdAsync(
            1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    /// <summary>Security: guest details require both permissions.</summary>
    [Theory]
    [InlineData(Permissions.ViewOtherGuestData)]
    [InlineData(Permissions.ViewGuestPassportData)]
    public async Task GetByIdAsync_MissingOnePermission_ReturnsForbidden(
        string permission)
    {
        // Arrange
        var world = new ServiceWorld();

        var authorization = CreateAuthorization(permission);
        var service = world.GuestService(authorization);

        // Act
        var result = await service.GetByIdAsync(
            1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    #endregion

    #region Search

    /// <summary>Positive: the result is sorted on the lower-case full name, so "anna" comes before "Zed" (BR-69).</summary>
    [Fact]
    public async Task SearchAsync_MixedCaseNames_SortsOnTheLowerCaseFullName()
    {
        // Arrange
        var world = new ServiceWorld();
        world.GuestQueries.SearchAsync(Arg.Any<IReadOnlyCollection<string>>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<GuestListItemDto>>(
            [
                ListItem(2, "Zed", "Olsen"),
                ListItem(1, "anna", "Jensen"),
            ]));

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.SearchAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([1, 2], result.Value.Select(guest => guest.GuestId).ToArray());
    }

    /// <summary>Equivalence: spaces, tabs and line breaks all separate the search terms and empty terms vanish.</summary>
    [Fact]
    public async Task SearchAsync_TextWithMixedWhitespace_SplitsIntoTerms()
    {
        // Arrange
        var world = new ServiceWorld();
        world.GuestQueries.SearchAsync(Arg.Any<IReadOnlyCollection<string>>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<GuestListItemDto>>([]));

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData);

        var service = world.GuestService(authorization);

        // Act
        await service.SearchAsync("  anna \t jensen\n", TestContext.Current.CancellationToken);

        // Assert
        await world.GuestQueries.Received(1).SearchAsync(
            Arg.Is<IReadOnlyCollection<string>>(terms => terms.SequenceEqual(new[] { "anna", "jensen" })),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Edge: no search text means no terms, not an error.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_NoSearchText_AsksWithoutTerms(string? searchText)
    {
        // Arrange
        var world = new ServiceWorld();
        world.GuestQueries.SearchAsync(Arg.Any<IReadOnlyCollection<string>>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<GuestListItemDto>>([]));

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.SearchAsync(searchText, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await world.GuestQueries.Received(1).SearchAsync(
            Arg.Is<IReadOnlyCollection<string>>(terms => terms.Count == 0),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Error handling: a failing query is not swallowed.</summary>
    [Fact]
    public async Task SearchAsync_QueryFails_PropagatesTheException()
    {
        // Arrange
        var world = new ServiceWorld();
        world.GuestQueries.SearchAsync(Arg.Any<IReadOnlyCollection<string>>(), TestContext.Current.CancellationToken)
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var authorization = CreateAuthorization(
            Permissions.ViewOtherGuestData);

        var service = world.GuestService(authorization);

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(
         () => service.SearchAsync(
             "anna", TestContext.Current.CancellationToken));
    }

    /// <summary>Security: searching guests without permission is forbidden.</summary>
    [Fact]
    public async Task SearchAsync_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.GuestService();

        // Act
        var result = await service.SearchAsync(
            "anna", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    #endregion

    #region Creating

    /// <summary>Positive: valid fields give a saved guest with a key.</summary>
    [Fact]
    public async Task CreateAsync_ValidFields_SavesTheGuestAndReturnsItsDetails()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new CreateGuestRequest(ServiceWorld.ValidGuestFields());

        var authorization = CreateAuthorization(
              Permissions.GuestsManage,
              Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Anna Jensen", result.Value.FullName);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: every field error is reported at once and nothing is saved (BR-60, BR-96).</summary>
    [Fact]
    public async Task CreateAsync_SeveralInvalidFields_ReturnsAllErrorsAndSavesNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        var fields = ServiceWorld.ValidGuestFields() with { FirstName = " ", Email = "not-an-email", Country = " " };

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.CreateAsync(new CreateGuestRequest(fields), TestContext.Current.CancellationToken);

        // Assert
        var codes = result.Errors.Select(error => error.Code).ToArray();
        Assert.Contains(ErrorCodes.Guest.FirstNameRequired, codes);
        Assert.Contains(ErrorCodes.Guest.EmailInvalid, codes);
        Assert.Contains(ErrorCodes.Guest.CountryRequired, codes);
        Assert.Empty(world.Guests.Items);
    }

    /// <summary>Boundary: a name of exactly the maximum length is accepted, one more is not.</summary>
    [Fact]
    public async Task CreateAsync_FirstNameAtAndAboveTheMaximum_AcceptsTheMaximumOnly()
    {
        // Arrange
        var world = new ServiceWorld();
        var atLimit = ServiceWorld.ValidGuestFields() with { FirstName = new string('a', Guest.MaxNameLength) };
        var aboveLimit = ServiceWorld.ValidGuestFields() with { FirstName = new string('a', Guest.MaxNameLength + 1) };

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var accepted = await service.CreateAsync(
            new CreateGuestRequest(atLimit),
            TestContext.Current.CancellationToken);

        var rejected = await service.CreateAsync(
            new CreateGuestRequest(aboveLimit),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(accepted.IsSuccess);
        Assert.Equal(ErrorCodes.Guest.FirstNameTooLong, rejected.FirstError?.Code);
    }

    /// <summary>Edge: Unicode names and a missing passport number are valid.</summary>
    [Fact]
    public async Task CreateAsync_UnicodeNameWithoutPassportNumber_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var fields = ServiceWorld.ValidGuestFields() with { FirstName = "Søren", LastName = "Åkesson-Ørsted", PassportNumber = null };

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.CreateAsync(
            new CreateGuestRequest(fields),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Søren Åkesson-Ørsted", result.Value.FullName);
    }

    /// <summary>Error handling: a concurrency conflict while saving is reported, not thrown.</summary>
    [Fact]
    public async Task CreateAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.CreateAsync(
            new CreateGuestRequest(ServiceWorld.ValidGuestFields()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.ConcurrencyConflict, result.FirstError?.Code);
    }

    /// <summary>Negative: a missing request is a programming error.</summary>
    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNull()
    {
        // Arrange
        var service = new ServiceWorld().GuestService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>Security: creating a guest without permissions is forbidden.</summary>
    [Fact]
    public async Task CreateAsync_WithoutPermissions_ReturnsForbidden()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new CreateGuestRequest(ServiceWorld.ValidGuestFields());
        var service = world.GuestService();

        // Act
        var result = await service.CreateAsync(
            request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Empty(world.Guests.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Security: creating a guest requires both permissions.</summary>
    [Theory]
    [InlineData(Permissions.GuestsManage)]
    [InlineData(Permissions.ViewGuestPassportData)]
    public async Task CreateAsync_MissingOnePermission_ReturnsForbidden(
        string permission)
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new CreateGuestRequest(ServiceWorld.ValidGuestFields());

        var authorization = CreateAuthorization(permission);
        var service = world.GuestService(authorization);

        // Act
        var result = await service.CreateAsync(
            request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Empty(world.Guests.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Updating

    /// <summary>Positive: the guest changes and the change is saved once.</summary>
    [Fact]
    public async Task UpdateAsync_ValidFields_ChangesTheGuestAndSaves()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        var request = new UpdateGuestRequest(guest.Id, ServiceWorld.ValidGuestFields() with { LastName = "Nielsen" });

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.UpdateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Nielsen", guest.LastName);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an unknown guest cannot be updated.</summary>
    [Fact]
    public async Task UpdateAsync_UnknownGuest_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new UpdateGuestRequest(99, ServiceWorld.ValidGuestFields());

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.UpdateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.NotFound, result.FirstError?.Code);
    }

    /// <summary>Negative: invalid fields leave the guest unchanged and save nothing.</summary>
    [Fact]
    public async Task UpdateAsync_InvalidEmail_ReturnsEmailInvalidAndKeepsTheGuest()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        var request = new UpdateGuestRequest(guest.Id, ServiceWorld.ValidGuestFields() with { Email = "broken" });

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.UpdateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.EmailInvalid, result.FirstError?.Code);
        Assert.Equal("anna@example.com", guest.Email);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Error handling: a concurrency conflict while saving is reported.</summary>
    [Fact]
    public async Task UpdateAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        var authorization = CreateAuthorization(
            Permissions.GuestsManage,
            Permissions.ViewGuestPassportData);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.UpdateAsync(new UpdateGuestRequest(guest.Id, ServiceWorld.ValidGuestFields()),
        TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.ConcurrencyConflict, result.FirstError?.Code);
    }

    /// <summary>Security: updating a guest without permissions is forbidden.</summary>
    [Fact]
    public async Task UpdateAsync_WithoutPermissions_ReturnsForbidden()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();

        var request = new UpdateGuestRequest(
            guest.Id,
            ServiceWorld.ValidGuestFields() with { LastName = "Nielsen" });

        var service = world.GuestService();

        // Act
        var result = await service.UpdateAsync(
            request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Equal("Jensen", guest.LastName);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Security: updating a guest requires both permissions.</summary>
    [Theory]
    [InlineData(Permissions.GuestsManage)]
    [InlineData(Permissions.ViewGuestPassportData)]
    public async Task UpdateAsync_MissingOnePermission_ReturnsForbidden(
        string permission)
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();

        var request = new UpdateGuestRequest(
            guest.Id,
            ServiceWorld.ValidGuestFields() with { LastName = "Nielsen" });

        var authorization = CreateAuthorization(permission);
        var service = world.GuestService(authorization);

        // Act
        var result = await service.UpdateAsync(
            request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Equal("Jensen", guest.LastName);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Deleting

    /// <summary>Positive: a guest without bookings is removed and the removal is saved.</summary>
    [Fact]
    public async Task DeleteAsync_GuestWithoutBookings_RemovesTheGuest()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();

        var authorization = CreateAuthorization(
            Permissions.GuestsManage);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.DeleteAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(world.Guests.Items);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a guest with bookings is kept, bookings are history (D-05).</summary>
    [Fact]
    public async Task DeleteAsync_GuestWithBookings_ReturnsHasBookingsAndKeepsTheGuest()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        world.Guests.GuestIdsWithBookings.Add(guest.Id);

        var authorization = CreateAuthorization(
            Permissions.GuestsManage);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.DeleteAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.HasBookings, result.FirstError?.Code);
        Assert.Single(world.Guests.Items);
    }

    /// <summary>Negative: an unknown guest cannot be deleted.</summary>
    [Fact]
    public async Task DeleteAsync_UnknownGuest_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        var authorization = CreateAuthorization(
            Permissions.GuestsManage);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.DeleteAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.NotFound, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency conflict while deleting is reported.</summary>
    [Fact]
    public async Task DeleteAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        var authorization = CreateAuthorization(
            Permissions.GuestsManage);

        var service = world.GuestService(authorization);

        // Act
        var result = await service.DeleteAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.ConcurrencyConflict, result.FirstError?.Code);
    }

    /// <summary>Security: deleting a guest without permission is forbidden.</summary>
    [Fact]
    public async Task DeleteAsync_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();

        var service = world.GuestService();

        // Act
        var result = await service.DeleteAsync(
            guest.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Single(world.Guests.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Validating fields

    /// <summary>Positive: valid fields give a success and change nothing.</summary>
    [Fact]
    public void ValidateFields_ValidFields_ReturnsSuccess()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = world.GuestService().ValidateFields(ServiceWorld.ValidGuestFields());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: invalid fields give every error code.</summary>
    [Fact]
    public void ValidateFields_EmptyNames_ReturnsBothRequiredCodes()
    {
        // Arrange
        var fields = ServiceWorld.ValidGuestFields() with { FirstName = "", LastName = "" };

        // Act
        var result = new ServiceWorld().GuestService().ValidateFields(fields);

        // Assert
        var codes = result.Errors.Select(error => error.Code).ToArray();
        Assert.Contains(ErrorCodes.Guest.FirstNameRequired, codes);
        Assert.Contains(ErrorCodes.Guest.LastNameRequired, codes);
    }

    /// <summary>Negative: missing fields are a programming error.</summary>
    [Fact]
    public void ValidateFields_NullFields_ThrowsArgumentNull()
    {
        // Arrange
        var service = new ServiceWorld().GuestService();

        // Act and assert
        Assert.Throws<ArgumentNullException>(() => service.ValidateFields(null!));
    }

    #endregion

    #region Helpers

    private static GuestListItemDto ListItem(int id, string firstName, string lastName) =>
        new(id, firstName, lastName, $"{firstName} {lastName}", "g@example.com", "+4511111111", "Denmark", false);

    #endregion
}
