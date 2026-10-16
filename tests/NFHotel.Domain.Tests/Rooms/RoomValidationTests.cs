using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>
/// Validering af et rums stamdata og bookbarhed.
/// Dækker BR-81, BR-84, BR-97 til BR-100, BR-110 og BR-113 samt A-10 og præmis B-04.
/// </summary>
public class RoomValidationTests
{
    // --------------------------------------------------------
    // VÆRELSESNUMMER (BR-97, A-10)
    // --------------------------------------------------------

    /// <summary>BR-97: værelsesnummeret må hverken være null, tomt eller kun whitespace.</summary>
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

    /// <summary>BR-97: nummeret er tekst, ikke et tal — "12B" er et gyldigt værelsesnummer.</summary>
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

    /// <summary>A-10: grænsen selv er tilladt, ét tegn mere er ikke.</summary>
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

    /// <summary>A-10: for langt værelsesnummer afvises i Domain, ikke først af databasen.</summary>
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

    // --------------------------------------------------------
    // ETAGE OG KAPACITET (BR-98, BR-100)
    // --------------------------------------------------------

    /// <summary>
    /// BR-98: etagen skal være > 0. Stueetage (0) og kælder (negativ) er dermed ikke
    /// tilladt — reglen er porteret uændret, også selv om den er restriktiv.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    public void Floor_RequiresGreaterThanZero(int floor, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, floor, RoomSize.Double, RoomFactory.Capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-98: en ugyldig etage giver <c>room.floor_invalid</c>.</summary>
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

    /// <summary>BR-100: kapaciteten skal være > 0 — et rum uden pladser kan ikke bookes.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    public void Capacity_RequiresGreaterThanZero(int capacity, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, RoomSize.Double, capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-100: en ugyldig kapacitet giver <c>room.capacity_invalid</c>.</summary>
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

    // --------------------------------------------------------
    // VÆRELSESTYPE (BR-99)
    // --------------------------------------------------------

    /// <summary>
    /// BR-99: typen var fri tekst i det gamle system og blev aldrig tjekket mod et sæt
    /// tilladte værdier. Nu er den en enum, og en udefineret ordinalværdi afvises —
    /// enums i C# accepterer ellers glad enhver int.
    /// </summary>
    [Theory]
    [InlineData(RoomSize.Single, true)]
    [InlineData(RoomSize.Double, true)]
    [InlineData(RoomSize.Suite, true)]
    [InlineData((RoomSize)3, false)]
    [InlineData((RoomSize)(-1), false)]
    [InlineData((RoomSize)99, false)]
    public void Size_AcceptsOnlyDefinedEnumValues(RoomSize size, bool expected)
    {
        // Arrange & Act
        var isValid = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, size, RoomFactory.Capacity).Count == 0;

        // Assert
        Assert.Equal(expected, isValid);
    }

    /// <summary>BR-99: en udefineret værelsestype giver <c>room.size_invalid</c>.</summary>
    [Fact]
    public void Validate_SizeIsNotDefined_ReturnsSizeInvalid()
    {
        // Arrange & Act
        var errors = ErrorsOf(RoomFactory.RoomNumber, RoomFactory.Floor, (RoomSize)42, RoomFactory.Capacity);

        // Assert
        Assert.Equal(RoomErrors.SizeInvalid, Assert.Single(errors));
    }

    // --------------------------------------------------------
    // ALLE FEJL PÅ ÉN GANG OG FAIL-FAST (BR-84, BR-113)
    // --------------------------------------------------------

    /// <summary>
    /// <c>Validate</c> samler alle fejl i feltrækkefølge, så rumformularen kan vise dem
    /// på én gang.
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
    /// BR-84, BR-113: <c>Create</c> fejler på den første fejl, så et ugyldigt rum aldrig
    /// kan opstå som objekt. I det gamle system kaldte formularen <c>Validate()</c> og
    /// ignorerede resultatet; fejlen blev først fanget i repo-laget.
    /// </summary>
    [Fact]
    public void Create_MultipleFieldsAreInvalid_ThrowsWithTheFirstErrorCode()
    {
        // Arrange & Act
        var exception = Assert.Throws<DomainException>(() => Room.Create("  ", 0, (RoomSize)42, 0));

        // Assert
        Assert.Equal(RoomErrors.RoomNumberRequired, exception.Code);
    }

    // --------------------------------------------------------
    // OPRETTELSE OG OPDATERING
    // --------------------------------------------------------

    /// <summary>
    /// BR-81: et nyoprettet rum er bookbart og rent. <c>Clean</c> er tilmed
    /// <c>default(HousekeepingStatus)</c>, så databasens <c>DEFAULT 0</c> siger det samme.
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
    /// A-06, A-08: stamdata og status er adskilt. <c>UpdateDetails</c> må ikke nulstille
    /// hverken bookbarhed eller rengøringsstand — statusskift har deres egne metoder.
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

    /// <summary>BR-84: en afvist opdatering må ikke efterlade rummet halvt overskrevet.</summary>
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

    // --------------------------------------------------------
    // BOOKBARHED (BR-110, B-04)
    // --------------------------------------------------------

    /// <summary>
    /// BR-110: kun <c>Available</c> gør rummet bookbart. Rengøringsstand indgår bevidst
    /// IKKE (B-04) — et rum kan være bookbart og samtidig afvente rengøring.
    /// </summary>
    [Theory]
    [InlineData(HousekeepingStatus.Clean)]
    [InlineData(HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingStatus.ServiceInProgress)]
    public void IsBookable_IsIndependentOfHousekeepingStatus(HousekeepingStatus housekeepingStatus)
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
    /// BR-110, BR-126: de tre bookbarhedsstatusser. Den gamle XAML-dropdown havde fire
    /// værdier der ikke matchede enum'en; her er der én liste.
    /// </summary>
    [Fact]
    public void RoomStatusTransitions_ChangeBookabilityAndAreReversible()
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
    /// A-08: bookbarhedsmetoderne er idempotente. Mobilappen retry'er over ustabilt
    /// netværk, og en retry må ikke give en fejl.
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
    /// B-04: <c>ReturnToService</c> gør rummet bookbart igen uden at røre rengøringsstanden.
    /// </summary>
    [Fact]
    public void ReturnToService_LeavesHousekeepingStatusUntouched()
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
}