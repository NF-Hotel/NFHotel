using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Rules;

/// <summary>
/// Statsløs validering af et rums stamdata (BR-97 til BR-100, BR-113).
/// </summary>
/// <remarks>
/// <see cref="Room.Create"/> fejler hurtigt på første brud; <see cref="Validate"/> samler
/// ALLE fejl, så en formular kan vise dem på én gang. Begge veje bruger de samme
/// prædikater — reglen findes ét sted.
/// <para>
/// Der returneres <b>fejlkoder</b>, ikke brugervendte tekster (A-09). Application mapper
/// kode til tekst; UI kan oversætte.
/// </para>
/// </remarks>
public static class RoomRules
{
    /// <summary>Fejlkode: værelsesnummer mangler (BR-97).</summary>
    public const string RoomNumberRequired = "room.room_number_required";

    /// <summary>Fejlkode: værelsesnummeret er længere end <see cref="Room.MaxRoomNumberLength"/> (A-10).</summary>
    public const string RoomNumberTooLong = "room.room_number_too_long";

    /// <summary>Fejlkode: etagen er ikke større end 0 (BR-98).</summary>
    public const string FloorInvalid = "room.floor_invalid";

    /// <summary>Fejlkode: værelsestypen er ikke en defineret <see cref="RoomSize"/> (BR-99).</summary>
    public const string SizeInvalid = "room.size_invalid";

    /// <summary>Fejlkode: kapaciteten er ikke større end 0 (BR-100).</summary>
    public const string CapacityInvalid = "room.capacity_invalid";

    /// <summary>
    /// Afgør om værelsesnummeret er brugbart (BR-97, A-10).
    /// </summary>
    /// <param name="roomNumber">Værelsesnummeret, fx "101" eller "12B".</param>
    /// <returns>Sand hvis nummeret hverken er tomt, kun whitespace eller for langt.</returns>
    public static bool IsValidRoomNumber(string? roomNumber) =>
        !string.IsNullOrWhiteSpace(roomNumber) && roomNumber.Length <= Room.MaxRoomNumberLength;

    /// <summary>
    /// Afgør om etagen er gyldig (BR-98).
    /// </summary>
    /// <param name="floor">Etagen.</param>
    /// <returns>Sand hvis etagen er større end 0.</returns>
    public static bool IsValidFloor(int floor) => floor > 0;

    /// <summary>
    /// Afgør om kapaciteten er gyldig (BR-100).
    /// </summary>
    /// <param name="capacity">Antal personer rummet kan rumme.</param>
    /// <returns>Sand hvis kapaciteten er større end 0.</returns>
    public static bool IsValidCapacity(int capacity) => capacity > 0;

    /// <summary>
    /// Afgør om værelsestypen er en defineret enum-værdi (BR-99).
    /// </summary>
    /// <param name="size">Værelsestypen.</param>
    /// <returns>Sand hvis værdien er defineret i <see cref="RoomSize"/>.</returns>
    public static bool IsValidSize(RoomSize size) => Enum.IsDefined(size);

    /// <summary>
    /// Samler alle valideringsfejl for et rum, så en formular kan vise dem på én gang.
    /// </summary>
    /// <param name="roomNumber">Værelsesnummeret.</param>
    /// <param name="floor">Etagen.</param>
    /// <param name="size">Værelsestypen.</param>
    /// <param name="capacity">Antal personer rummet kan rumme.</param>
    /// <returns>Fejlkoder i feltrækkefølge: nummer, etage, type, kapacitet. Tom liste betyder gyldig.</returns>
    public static IReadOnlyList<string> Validate(string? roomNumber, int floor, RoomSize size, int capacity)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            errors.Add(RoomNumberRequired);
        }
        else if (roomNumber.Length > Room.MaxRoomNumberLength)
        {
            errors.Add(RoomNumberTooLong);
        }

        if (!IsValidFloor(floor))
        {
            errors.Add(FloorInvalid);
        }

        if (!IsValidSize(size))
        {
            errors.Add(SizeInvalid);
        }

        if (!IsValidCapacity(capacity))
        {
            errors.Add(CapacityInvalid);
        }

        return errors;
    }
}