using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// Create a guest (BR-52 to BR-57, BR-60, BR-91 to BR-96).
/// </summary>
/// <param name="Fields">The fields of the guest.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateGuestRequest(GuestFields Fields);
