using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases;

/// <summary>
/// Én regelovertrædelse, identificeret ved sin fejlkode.
/// </summary>
/// <remarks>
/// Koden er kontrakten (A-09) — ikke en brugervendt tekst. UI vælger tekst ud fra koden og
/// kan dermed oversætte; et senere API kan mappe koden til en HTTP-status. Det er derfor
/// typen bevidst ikke bærer en engelsk <c>Message</c> som det gamle system gjorde.
/// </remarks>
/// <param name="Code">Fejlkoden, fx <c>"booking.room_not_available"</c>. Se <see cref="ErrorCodes"/>.</param>
public sealed record Error(string Code);