namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// Ret en gæst (BR-62, BR-66, BR-72, BR-91 til BR-96).
/// </summary>
/// <remarks>
/// <see cref="GuestId"/> er påkrævet og ligger uden for <see cref="Fields"/>, fordi id'et
/// ikke er redigérbart (BR-72). Det lukker samtidig fejlen hvor en redigeret gæst mistede
/// sit id undervejs og blev gemt på id 0. BR-66's "arbejd på en kopi" bliver overflødigt:
/// requesten <i>er</i> kopien.
/// </remarks>
/// <param name="GuestId">Gæstens id. Skal være større end 0.</param>
/// <param name="Fields">De nye feltværdier.</param>
public sealed record UpdateGuestRequest(int GuestId, GuestFields Fields);