namespace NFHotel.Application.Dto.Rooms;

/// <summary>An active housekeeper and how many rooms the housekeeper has (MIL-060).</summary>
/// <param name="Id">The account.</param>
/// <param name="Name">The first and the last name, or the user name while the account has none.</param>
/// <param name="RoomCount">How many rooms are assigned.</param>
public sealed record HousekeeperDto(int Id, string Name, int RoomCount);
