namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// Opret en gæst (BR-52 til BR-57, BR-60, BR-91 til BR-96).
/// </summary>
/// <param name="Fields">Gæstens felter.</param>
public sealed record CreateGuestRequest(GuestFields Fields);