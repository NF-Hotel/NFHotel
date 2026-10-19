namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// Change a guest (BR-62, BR-66, BR-72, BR-91 to BR-96).
/// </summary>
/// <remarks>
/// <see cref="GuestId"/> is required and sits outside <see cref="Fields"/>, because the id
/// is not editable (BR-72). That also closes the fault where an edited guest lost
/// its id on the way and was saved on id 0. BR-66's "work on a copy" becomes superfluous:
/// the request <i>is</i> the copy.
/// </remarks>
/// <param name="GuestId">The id of the guest. Must be greater than 0.</param>
/// <param name="Fields">The new field values.</param>
public sealed record UpdateGuestRequest(int GuestId, GuestFields Fields);
