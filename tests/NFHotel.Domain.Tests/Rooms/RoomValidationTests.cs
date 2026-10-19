using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>
/// Validation of the master data and bookability of a room.
/// Covers BR-81, BR-84, BR-97 to BR-100, BR-110 and BR-113 as well as A-10 and premise B-04.
/// </summary>
public class RoomValidationTests
{
    #region Room number (BR-97, A-10)

    /// <summary>BR-97: the room number must be neither null, empty nor only whitespace.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_RoomNumberIsMissing_ReturnsRoomNumberRequired(string? roomNumber)
    {
        // Arrange & Act
        var errors = ErrorsOf(roomNumber, RoomFactory.Floor, RoomSize.Double, RoomFactory.Capacity);

        // Assert
        Assert.Equal(RoomErrors.RoomNumberRequired, Assert.Single(errors));
    }

    /// <summary>BR-97: the number is text, not a number — "12B" is a valid room number.</summary>
    [Theory]
    [InlineData("101")]
    [InlineData("12B")]
    [InlineData("Suite Nord")]
    public void Validate_RoomNumberIsFreeText_ReturnsNoErrors(string roomNumber)
    {
        // Arrange & Act
        var errors = ErrorsOf(roomNumber, RoomFactory.Floor, RoomSize.Double, RoomFactory.Capacity);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>A-10: the limit itself is allowed, one more character is not.</summary>
    [Fact]
    public void Validate_RoomNumberIsExactlyAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var roomNumber = new string('1', Room.MaxRoomNumberLength);

        // Act
        var errors = ErrorsOf(roomNumber, RoomFactory.Floor, RoomSize.Double, RoomFactory.Capacity);

        // Assert
        Assert.Empty(errors);
    }

    /// <summary>A-10: a too long room number is rejected in Domain, not first by the database.</summary>
    [Fact]
    public void Validate_RoomNumberExceedsMaxLength_ReturnsRoomNumberTooLong()
    {
        // Arrange
        var roomNumber = new string('1', Room.MaxRoomNumberLength + 1);

        // Act
        var errors = ErrorsOf(roomNumber, RoomFactory.Floor, RoomSize.Double, RoomFactory.Capacity);

        // Assert
        Assert.Equal(RoomErrors.RoomNumberTooLong, Assert.Single(errors));
    }

    #endregion

    #region Floor and capacity (BR-98, BR-100)

    /// <summary>
    /// BR-98: the floor must be > 0. Ground floor (0) and basement (negative) are thus not
    /// allowed — the rule is ported unchanged, even though it is restrictive.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    public void Floor_AnyValue_RequiresGreaterThanZero(int floor, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, floor, RoomSize.Double, RoomFactory.Capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-98: an invalid floor gives <c>room.floor_invalid</c>.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_FloorIsNotGreaterThanZero_ReturnsFloorInvalid(int floor)
    {
        // Arrange & Act
        var errors = ErrorsOf(RoomFactory.RoomNumber, floor, RoomSize.Double, RoomFactory.Capacity);

        // Assert
        Assert.Equal(RoomErrors.FloorInvalid, Assert.Single(errors));
    }

    /// <summary>BR-100: the capacity must be > 0 — a room without places cannot be booked.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    public void Capacity_AnyValue_RequiresGreaterThanZero(int capacity, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, RoomSize.Double, capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-100: an invalid capacity gives <c>room.capacity_invalid</c>.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_CapacityIsNotGreaterThanZero_ReturnsCapacityInvalid(int capacity)
    {
        // Arrange & Act
        var errors = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, RoomSize.Double, capacity);

        // Assert
        Assert.Equal(RoomErrors.CapacityInvalid, Assert.Single(errors));
    }

    #endregion

    #region Room type (BR-99)

    /// <summary>
    /// BR-99: the type was free text in the old system and was never checked against a set of
    /// allowed values. Now it is an enum, and an undefined ordinal value is rejected —
    /// C# enums otherwise gladly accept any int.
    /// </summary>
    [Theory]
    [InlineData(RoomSize.Single, true)]
    [InlineData(RoomSize.Double, true)]
    [InlineData(RoomSize.Suite, true)]
    [InlineData((RoomSize)3, false)]
    [InlineData((RoomSize)(-1), false)]
    [InlineData((RoomSize)99, false)]
    public void Size_AnyValue_AcceptsOnlyDefinedEnumValues(RoomSize size, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, size, RoomFactory.Capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-99: an undefined room type gives <c>room.size_invalid</c>.</summary>
    [Fact]
    public void Validate_SizeIsNotDefined_ReturnsSizeInvalid()
    {
        // Arrange & Act
        var errors = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, (RoomSize)42, RoomFactory.Capacity);

        // Assert
        Assert.Equal(RoomErrors.SizeInvalid, Assert.Single(errors));
    }

    #endregion

    #region All errors at once and fail-fast (BR-84, BR-113)

    /// <summary>
    /// <c>ErrorsOf</c> collects all errors in field order, so the room form can show them
    /// at once.
    /// </summary>
    [Fact]
    public void Validate_EveryFieldIsInvalid_ReturnsAllErrorsInFieldOrder()
    {
        // Arrange & Act
        var errors = ErrorsOf("  ", 0, (RoomSize)42, 0);

        // Assert
        Assert.Collection(
            errors,
            error => Assert.Equal(RoomErrors.RoomNumberRequired, error),
            error => Assert.Equal(RoomErrors.FloorInvalid, error),
            error => Assert.Equal(RoomErrors.SizeInvalid, error),
            error => Assert.Equal(RoomErrors.CapacityInvalid, error));
    }

    /// <summary>
    /// BR-84, BR-113: <c>Create</c> fails with the first error as its code, so an invalid room can never
    /// arise as an object. In the old system the form called <c>Validate()</c> and
    /// ignored the result; the error was caught only in the repository layer.
    /// </summary>
    [Fact]
    public void Create_MultipleFieldsAreInvalid_ThrowsWithTheFirstErrorCode()
    {
        // Arrange & Act
        var exception = Assert.Throws<DomainException>(() => Room.Create("  ", 0, (RoomSize)42, 0));

        // Assert
        Assert.Equal(RoomErrors.RoomNumberRequired, exception.Code);
    }

    #endregion

    #region Creation and update

    /// <summary>
    /// BR-81: a newly created room is bookable and clean. <c>Clean</c> is also
    /// <c>default(HousekeepingStatus)</c>, so the database <c>DEFAULT 0</c> says the same.
    /// </summary>
    [Fact]
    public void Create_ValidInput_IsAvailableAndClean()
    {
        // Arrange & Act
        var room = Room.Create("12B", 3, RoomSize.Suite, 4);

        // Assert
        Assert.Equal(RoomStatus.Available, room.Status);
        Assert.Equal(HousekeepingStatus.Clean, room.HousekeepingStatus);
        Assert.True(room.IsBookable);
        Assert.Equal("12B", room.RoomNumber);
        Assert.Equal(3, room.Floor);
        Assert.Equal(RoomSize.Suite, room.Size);
        Assert.Equal(4, room.Capacity);
    }

    /// <summary>
    /// A-06, A-08: master data and status are separate. <c>UpdateDetails</c> must not reset
    /// either bookability or cleaning state — status changes have their own methods.
    /// </summary>
    [Fact]
    public void UpdateDetails_ValidInput_LeavesStatusAndHousekeepingStatusUntouched()
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(HousekeepingStatus.CleaningInProgress);
        room.SendToMaintenance();

        // Act
        room.UpdateDetails("204", 2, RoomSize.Single, 1);

        // Assert
        Assert.Equal("204", room.RoomNumber);
        Assert.Equal(2, room.Floor);
        Assert.Equal(RoomStatus.Maintenance, room.Status);
        Assert.Equal(HousekeepingStatus.CleaningInProgress, room.HousekeepingStatus);
    }

    /// <summary>BR-84: a rejected update must not leave the room half overwritten.</summary>
    [Fact]
    public void UpdateDetails_InvalidInput_ThrowsAndLeavesRoomUnchanged()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        var exception = Assert.Throws<DomainException>(() => room.UpdateDetails("204", 0, RoomSize.Single, 1));

        // Assert
        Assert.Equal(RoomErrors.FloorInvalid, exception.Code);
        Assert.Equal(RoomFactory.RoomNumber, room.RoomNumber);
        Assert.Equal(RoomFactory.Floor, room.Floor);
        Assert.Equal(RoomFactory.Capacity, room.Capacity);
    }

    #endregion

    #region Bookability (BR-110, B-04)

    /// <summary>
    /// BR-110: only <c>Available</c> makes the room bookable. The cleaning state is deliberately
    /// NOT included (B-04) — a room can be bookable and at the same time wait for cleaning.
    /// </summary>
    [Theory]
    [InlineData(HousekeepingStatus.Clean)]
    [InlineData(HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingStatus.ServiceInProgress)]
    public void IsBookable_AnyHousekeepingStatus_IsUnaffected(HousekeepingStatus housekeepingStatus)
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(housekeepingStatus);

        // Act
        var isBookable = room.IsBookable;

        // Assert
        Assert.True(isBookable);
        Assert.Equal(housekeepingStatus, room.HousekeepingStatus);
    }

    /// <summary>
    /// BR-110, BR-126: the three bookability statuses. The old XAML dropdown had four
    /// values that did not match the enum; here there is one list.
    /// </summary>
    [Fact]
    public void RoomStatusTransitions_OutOfAndBackToService_ChangeBookabilityAndAreReversible()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act & Assert
        room.TakeOutOfService();
        Assert.Equal(RoomStatus.OutOfService, room.Status);
        Assert.False(room.IsBookable);

        room.SendToMaintenance();
        Assert.Equal(RoomStatus.Maintenance, room.Status);
        Assert.False(room.IsBookable);

        room.ReturnToService();
        Assert.Equal(RoomStatus.Available, room.Status);
        Assert.True(room.IsBookable);
    }

    /// <summary>
    /// A-08: the bookability methods are idempotent. The mobile app retries over an unstable
    /// network, and a retry must not give an error.
    /// </summary>
    [Fact]
    public void RoomStatusTransitions_RepeatedCall_IsNoOp()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        room.ReturnToService();
        room.TakeOutOfService();
        room.TakeOutOfService();

        // Assert
        Assert.Equal(RoomStatus.OutOfService, room.Status);
    }

    /// <summary>
    /// B-04: <c>ReturnToService</c> makes the room bookable again without touching the cleaning state.
    /// </summary>
    [Fact]
    public void ReturnToService_AnyHousekeepingStatus_LeavesHousekeepingStatusUntouched()
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(HousekeepingStatus.DepartureCleaningDue);
        room.SendToMaintenance();

        // Act
        room.ReturnToService();

        // Assert
        Assert.Equal(RoomStatus.Available, room.Status);
        Assert.Equal(HousekeepingStatus.DepartureCleaningDue, room.HousekeepingStatus);
    }

    /// <summary>
    /// Builds a room and returns every error code, or an empty list when the room is valid.
    /// The rules are attributes on <see cref="Room"/> (ADR-0002).
    /// </summary>
    private static IReadOnlyList<string> ErrorsOf(string? roomNumber, int floor, RoomSize size, int capacity)
    {
        try
        {
            _ = Room.Create(roomNumber!, floor, size, capacity);

            return [];
        }
        catch (DomainException exception)
        {
            return exception.Codes;
        }
    }

    #endregion
}
