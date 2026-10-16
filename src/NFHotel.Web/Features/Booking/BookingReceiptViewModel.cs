using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Slår én booking op på dens bookingnummer til den offentlige kvitteringsside
/// <c>/booking/{bookingNumber}</c> (BR-C-06).
/// </summary>
/// <remarks>
/// <b>Hvorfor siden viser så lidt.</b> Der er ingen adgangskontrol i Fase 1 (Fase1-Scope.md),
/// og bookingnummeret er det eneste der skal til for at åbne siden. Formatet er
/// <c>FLZ-</c> plus bookingens id (BR-109), så <c>FLZ-000123</c> ligger ét ciffer fra
/// <c>FLZ-000124</c>: den der gætter, rammer en anden gæsts booking i første forsøg.
/// <para>
/// Derfor bærer siden kun det, en gætter ikke kan bruge til noget — bookingnummer,
/// datoer, antal nætter, rum og status (BR-C-05). Navn, e-mail, telefon og pasnummer
/// vises <b>ikke</b>, selv om <see cref="BookingDetailsDto.Guest"/> har dem: kunden har
/// lige selv indtastet dem og har dem i forvejen, mens en fremmed ellers ville kunne høste
/// persondata ved at tælle opad. Det er en bevidst begrænsning indtil Identity kommer, og
/// den skal først løftes samtidig med adgangskontrollen — ikke før.
/// </para>
/// <para>
/// Begrænsningen håndhæves af <c>BookingReceiptCard.razor</c>, som kun har markup til de
/// tilladte felter. Viewmodellen bærer hele DTO'en, fordi den kommer sådan fra servicen.
/// </para>
/// </remarks>
public sealed class BookingReceiptViewModel
{
    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IBookingService> _bookings;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="bookings">Kører use cases på <see cref="IBookingService"/>.</param>
    public BookingReceiptViewModel(IUseCaseRunner<IBookingService> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        _bookings = bookings;
    }

    /// <summary>Bookingen, eller <c>null</c> hvis den ikke blev fundet.</summary>
    public BookingDetailsDto? Details { get; private set; }

    /// <summary>Sand mens opslaget er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand når opslaget er forsøgt mindst én gang.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Den danske forklaring på hvorfor der ingen booking er.</summary>
    /// <remarks>
    /// Samme tekst uanset om nummeret var noget vrøvl eller pegede på en booking der ikke
    /// findes. Forskellen ville kun oplyse den der prøver sig frem.
    /// </remarks>
    public string NotFoundText { get; private set; } = string.Empty;

    /// <summary>
    /// Slår bookingen op.
    /// </summary>
    /// <param name="bookingNumber">Bookingnummeret fra URL'en, fx <c>FLZ-000123</c>.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task LoadAsync(string? bookingNumber, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (!BookingNumberText.TryParseId(bookingNumber, out var bookingId))
        {
            // Et ulæseligt nummer koster ikke et databaseopslag.
            Details = null;
            NotFoundText = Describe(ErrorCodes.Booking.NotFound);
            IsLoaded = true;

            return;
        }

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync((service, token) => service.GetByIdAsync(bookingId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Details = null;
                NotFoundText = ErrorMessages.Describe(result) is [var first, ..]
                    ? first
                    : ErrorMessages.Unexpected;

                return;
            }

            Details = result.Value;
            NotFoundText = string.Empty;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>De fejl der skal vises. Tom når bookingen blev fundet.</summary>
    public IReadOnlyList<string> Errors =>
        IsLoaded && Details is null && NotFoundText.Length > 0 ? [NotFoundText] : NoMessages;

    /// <summary>Oversætter én fejlkode til dansk.</summary>
    private static string Describe(string code) => ErrorMessages.Describe(new Error(code));
}