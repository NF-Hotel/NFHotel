using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// The verification of a booking request (MIL-088, ADR-0052 decision 3): the token of the link, sent in the body by the
/// button of the page of the link, never in an address.
/// </summary>
/// <param name="Token">The token after the <c>#</c> of the link: 43 characters of base64url.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record VerifyBookingRequest(
    [property: JsonRequired][StringLength(VerifyBookingRequest.MaxTokenLength)] string Token)
{
    #region Fields

    /// <summary>
    /// The longest token that the API reads: a token has 43 characters, and anything longer is refused before a use case
    /// runs.
    /// </summary>
    public const int MaxTokenLength = 64;

    #endregion Fields
}
