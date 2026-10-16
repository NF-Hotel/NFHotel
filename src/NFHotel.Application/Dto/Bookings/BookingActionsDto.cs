namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Hvilke handlinger der er lovlige på en booking lige nu (BR-22).
/// </summary>
/// <remarks>
/// Den vigtigste enkeltdel i hele DTO-designet. Beregnes i Application ud fra domænets
/// tilstandsregler, så Web kan tegne knapper uden at kende en eneste statusovergang. Det er
/// sådan BR-02, BR-11, BR-14 og BR-22 (ViewModel) og BR-121, BR-122, BR-123 (XAML-triggere)
/// bliver til <b>én</b> implementering i stedet for seks.
/// <para>
/// Navnet er <c>CanReschedule</c> og ikke <c>CanEdit</c>, så DTO og domæne bruger samme ord
/// om samme regel (A-06).
/// </para>
/// </remarks>
/// <param name="CanConfirm">Om bookingen må bekræftes (BR-02, BR-121).</param>
/// <param name="CanCheckIn">Om gæsten må tjekkes ind i dag (BR-06). Walk-in er tilladt (B-01).</param>
/// <param name="CanCheckOut">Om gæsten må tjekkes ud (BR-09).</param>
/// <param name="CanCancel">Om bookingen må annulleres (BR-11, BR-122).</param>
/// <param name="CanReschedule">Om datoer og rum må ændres (BR-14, BR-123).</param>
public sealed record BookingActionsDto(
    bool CanConfirm,
    bool CanCheckIn,
    bool CanCheckOut,
    bool CanCancel,
    bool CanReschedule);