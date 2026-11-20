using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

using BookingEntity = NFHotel.Domain.Entities.Booking;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// All state and all service calls of the customer's booking flow on <c>/book</c>:
/// dates, a free room type, guest details and receipt.
/// </summary>
/// <remarks>
/// <c>Book.razor</c> is markup. It calls the methods below and reads properties — it
/// knows neither <c>IBookingService</c>, <c>IGuestService</c> nor a single error code
/// (B-10, UI-Spec section 4).
/// <para>
/// The step is a <i>view</i>, not a permission. Every transition checks the dates
/// and the room choice again, so BR-C-04 holds even if an earlier action left
/// <see cref="CurrentStep"/> further ahead than the input justifies.
/// </para>
/// <para>
/// No method clears the user's input. Going back to step 1, correcting a date and
/// getting forward again should cost a click, not a name and an email. The only thing that can
/// disappear is a room type that is no longer free — and that is a rule (BR-C-02),
/// not a loss.
/// </para>
/// <para>
/// The guest chooses a room type, never a room: the API picks a free room of the type when the booking is sent, so
/// the room number stays reception's to give and to change.
/// </para>
/// <para>
/// "Today" comes from <see cref="IClock"/> and never from <c>DateTime.Now</c> (A-05), so
/// the client's date validation uses the same day as the service afterwards.
/// </para>
/// <para>
/// With the verification by email on (MIL-088, ADR-0052 decision 8), the last step asks for the booking instead of
/// making it: the API mails a link, the flow ends in <see cref="BookingFlowStep.Inbox"/>, and the room is booked only on
/// the page of the link. The flow asks the API which way holds once, when the page starts (<see cref="LoadModeAsync"/>).
/// </para>
/// </remarks>
public sealed class BookingFlowViewModel
{
    /// <summary>Default stay when the page is opened: one night, tomorrow.</summary>
    private const int DefaultNights = 1;

    /// <summary>
    /// The most guests the flow asks about: the party of the public site (BR-C-07, MIL-086). A bigger party books two
    /// rooms, like at the desk.
    /// </summary>
    public const int MaxGuests = BookingEntity.MaxPublicGuests;

    /// <summary>The format of a date in the address of /book, as the booking box on the front page writes it.</summary>
    private const string QueryDateFormat = "yyyy-MM-dd";

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();
    private static readonly IReadOnlyList<AvailableRoomDto> NoRooms = Array.Empty<AvailableRoomDto>();

    /// <summary>The notice after "Send again": a new request replaces the open one, and its link (ADR-0052 decision 6).</summary>
    private const string NewLinkNotice = "We've sent you a new link. The link in the earlier email no longer works.";

    private readonly IUseCaseRunner<IBookingService> _bookings;
    private readonly IUseCaseRunner<IGuestService> _guests;
    private readonly IUseCaseRunner<IGuestAccessService> _guestAccess;
    private readonly GuestSessionTickets _tickets;
    private readonly IClock _clock;
    private readonly IUseCaseRunner<IBookingRequestService>? _requests;

    private int _party = 1;
    private bool _isModeLoaded;

    /// <summary>
    /// Creates the viewmodel with an empty flow and a suggestion for dates.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    /// <param name="guests">Runs use cases on <see cref="IGuestService"/>.</param>
    /// <param name="guestAccess">Runs the public booking that answers with the guest's session (MIL-018).</param>
    /// <param name="tickets">Keeps that session under a one-time ticket until the guest asks for it (MIL-018).</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    /// <param name="requests">
    /// Runs the booking verified by email (MIL-088, ADR-0052). Without it the flow never asks the mode and books at once,
    /// as it did before MIL-088.
    /// </param>
    public BookingFlowViewModel(
        IUseCaseRunner<IBookingService> bookings,
        IUseCaseRunner<IGuestService> guests,
        IUseCaseRunner<IGuestAccessService> guestAccess,
        GuestSessionTickets tickets,
        IClock clock,
        IUseCaseRunner<IBookingRequestService>? requests = null)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(guests);
        ArgumentNullException.ThrowIfNull(guestAccess);
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _guests = guests;
        _guestAccess = guestAccess;
        _tickets = tickets;
        _clock = clock;
        _requests = requests;

        CheckInDate = clock.Today.AddDays(1);
        CheckOutDate = clock.Today.AddDays(1 + DefaultNights);
    }

    // ------------------------------------------------------------------
    // STATE
    // ------------------------------------------------------------------

    /// <summary>The step shown right now.</summary>
    public BookingFlowStep CurrentStep { get; private set; } = BookingFlowStep.Dates;

    /// <summary>True while a service call is in progress. Buttons are disabled, the page does not jump.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Errors that do not belong to a single field, as the texts of the screen.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    /// <summary>Arrival date, inclusive. <c>null</c> until the user has chosen.</summary>
    public DateOnly? CheckInDate { get; set; }

    /// <summary>Departure date, exclusive. <c>null</c> until the user has chosen.</summary>
    public DateOnly? CheckOutDate { get; set; }

    /// <summary>The rooms that are free in the period (BR-C-02). Empty until step 2 has been fetched.</summary>
    public IReadOnlyList<AvailableRoomDto> AvailableRooms { get; private set; } = NoRooms;

    /// <summary>
    /// How many people the room must sleep. 1 shows every free room, and is the default, so nothing is hidden
    /// from a guest who never touched the field. Kept between 1 and <see cref="MaxGuests"/>.
    /// </summary>
    /// <remarks>
    /// A filter on the fetched list, not a parameter of the service: the service answers which rooms are free
    /// (BR-C-02), and which of them are big enough is the guest's own question.
    /// </remarks>
    public int Guests
    {
        get => _party;
        set => _party = Math.Clamp(value, BookingEntity.MinPublicGuests, MaxGuests);
    }

    /// <summary>The choices of the guests field: 1 to <see cref="MaxGuests"/>.</summary>
    public static IReadOnlyList<int> GuestOptions { get; } = Enumerable.Range(1, MaxGuests).ToArray();

    /// <summary>
    /// The room types with a free room that sleeps <see cref="Guests"/>, smallest type first. Every type with a free
    /// room when the party is one.
    /// </summary>
    public IReadOnlyList<RoomTypeChoice> VisibleRoomTypes => AvailableRooms
        .Where(room => room.Capacity >= Guests)
        .GroupBy(room => room.Size)
        .OrderBy(group => group.Key)
        .Select(group => new RoomTypeChoice(group.Key, group.Count(), group.Max(room => room.Capacity)))
        .ToArray();

    /// <summary>True when availability has been fetched at least once for the current dates.</summary>
    public bool AreRoomsLoaded { get; private set; }

    /// <summary>The chosen room type, or <c>null</c>.</summary>
    public RoomSize? SelectedRoomType { get; private set; }

    /// <summary>The guest's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>The guest's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>The guest's email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The guest's phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>The guest's country.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>The guest's passport number. Optional (BR-57).</summary>
    public string PassportNumber { get; set; } = string.Empty;

    /// <summary>Validation errors distributed over the fields of the guest form.</summary>
    public GuestFieldErrors GuestErrors { get; private set; } = GuestFieldErrors.None;

    /// <summary>
    /// The receipt of the booking just created, or <c>null</c> until step 4. Only the receipt is kept, not the whole
    /// booking that the service answers with: the guest has just typed the rest, and the page shows no more (MIL-079).
    /// </summary>
    public BookingReceiptDto? Confirmation { get; private set; }

    /// <summary>
    /// The one-time ticket of the guest session of the booking just created, or <c>null</c> until step 4 (MIL-018). The
    /// button "Manage your booking" posts it, and the answer sets the guest cookie; the token itself stays on the server.
    /// </summary>
    public string? SessionTicket { get; private set; }

    /// <summary>
    /// True when a booking on the public site is asked for and verified by email (MIL-088, ADR-0052 decision 8). False
    /// until <see cref="LoadModeAsync"/> has heard otherwise from the API, and when the API could not say.
    /// </summary>
    /// <remarks>
    /// False when the API could not say, because the API holds the rule either way: with the verification on, it refuses a
    /// booking at once with <c>booking.verification_required</c>, and the page asks the guest to reload it.
    /// </remarks>
    public bool IsVerificationRequired { get; private set; }

    /// <summary>The email that the link was sent to, as the guest typed it, or <c>null</c> until the API took a request.</summary>
    public string? SentTo { get; private set; }

    /// <summary>When the link of the last mail lapses, or <c>null</c> until the API took a request.</summary>
    public DateTimeOffset? LinkExpiresAt { get; private set; }

    /// <summary>The notice of the inbox step after "Send again", or <c>null</c>.</summary>
    public string? Notice { get; private set; }

    /// <summary>True when the user has tried to move on from the date step at least once.</summary>
    /// <remarks>
    /// Controls only whether "must be filled in" may be shown. An empty form is not an error until
    /// the user has tried to use it.
    /// </remarks>
    private bool HasAttemptedDates { get; set; }

    // ------------------------------------------------------------------
    // DERIVED STATE
    // ------------------------------------------------------------------

    /// <summary>
    /// True when both dates are filled in, the arrival is not in the past, the departure is after the arrival, and the
    /// stay keeps the rules of the public site: a departure within five years and at most 30 nights (BR-C-07, BR-N-08,
    /// MIL-086). The gate to step 2 (BR-C-04).
    /// </summary>
    public bool HasValidDates =>
        CheckInDate is DateOnly checkIn
        && CheckOutDate is DateOnly checkOut
        && checkIn != default
        && checkIn >= _clock.Today
        && checkOut > checkIn
        && StayDates.PublicError(checkIn, checkOut, _clock.Today) is null;

    /// <summary>The first arrival that the page offers: today (MIL-086).</summary>
    public DateOnly EarliestCheckIn => _clock.Today;

    /// <summary>The last arrival that the page offers: the day before the last departure (MIL-086).</summary>
    public DateOnly LatestCheckIn => StayDates.LatestArrival(_clock.Today);

    /// <summary>The first departure that the page offers: the day after the chosen arrival, or tomorrow (MIL-086).</summary>
    public DateOnly EarliestCheckOut => (ChosenArrival ?? _clock.Today).AddDays(1);

    /// <summary>
    /// The last departure that the page offers: five years ahead, and no more than 30 nights after a chosen arrival
    /// (BR-C-07, BR-N-08, MIL-086).
    /// </summary>
    public DateOnly LatestCheckOut => ChosenArrival is DateOnly arrival
        ? BookingEntity.LatestPublicDeparture(arrival, _clock.Today)
        : BookingEntity.LatestDeparture(_clock.Today);

    /// <summary>The error text at the arrival field, or <c>null</c>.</summary>
    public string? CheckInError => Describe(CheckInErrorCode);

    /// <summary>The error text at the departure field, or <c>null</c>.</summary>
    public string? CheckOutError => Describe(CheckOutErrorCode);

    /// <summary>
    /// Number of nights in the chosen period, or 0 if the period is not complete yet.
    /// </summary>
    /// <remarks>
    /// Counted by the domain's <see cref="DateRange"/>, so "a night" has one definition in the whole
    /// system (BR-109, BR-117). Computed independently of <see cref="HasValidDates"/>, so the number
    /// is also updated while the user corrects a date that lies in the past.
    /// </remarks>
    public int Nights =>
        CheckInDate is DateOnly checkIn
        && CheckOutDate is DateOnly checkOut
        && checkIn != default
        && checkOut > checkIn
            ? new DateRange(checkIn, checkOut).Nights
            : 0;

    /// <summary>The number of nights as text, or a prompt if the period is not complete.</summary>
    public string NightsText => Nights > 0
        ? PublicDateText.Nights(Nights)
        : "Pick your check-in and check-out dates and we'll work out the nights.";

    /// <summary>The period as text, or empty if both dates are not chosen.</summary>
    public string PeriodText =>
        CheckInDate is DateOnly checkIn && CheckOutDate is DateOnly checkOut && checkOut > checkIn
            ? PublicDateText.Period(checkIn, checkOut)
            : string.Empty;

    /// <summary>
    /// Short summary of the choice for step 3 — without a price, like the rest of the flow
    /// (BR-C-05).
    /// </summary>
    public string SelectionSummary => SelectedRoomType is not RoomSize size
        ? string.Empty
        : string.Join(
            " · ",
            DisplayText.Describe(size),
            PeriodText,
            PublicDateText.Nights(Nights),
            GuestsText,
            PaymentText);

    /// <summary>True when "find free rooms" may be pressed.</summary>
    public bool CanFindRooms => !IsBusy && HasValidDates;

    /// <summary>True when the booking may be sent.</summary>
    public bool CanSubmit => !IsBusy && HasValidDates && SelectedRoomType is not null;

    /// <summary>True when "Send again" may be pressed: the inbox step shows, and no call is in progress.</summary>
    public bool CanSendAgain => !IsBusy && CurrentStep == BookingFlowStep.Inbox;

    /// <summary>
    /// The last step of the flow: the inbox with the verification on, otherwise the receipt (MIL-088, ADR-0052).
    /// </summary>
    public BookingFlowStep FinalStep => IsVerificationRequired ? BookingFlowStep.Inbox : BookingFlowStep.Receipt;

    /// <summary>The name of the last step in the step indicator.</summary>
    public string FinalStepText => IsVerificationRequired ? "Check your inbox" : "Confirmation";

    /// <summary>
    /// The hint at the email field, which says what the email is used for: the link that books the room with the
    /// verification on; with it off no mail is sent, and the email finds the booking again with its reference (UC-010).
    /// </summary>
    public string EmailHint => IsVerificationRequired
        ? "We email you a link here. Your room is booked only when you use it."
        : "With your booking reference, it finds your booking again.";

    /// <summary>The first line of the inbox step: where the mail went. Empty until the API took a request.</summary>
    public string InboxText => SentTo is null
        ? string.Empty
        : $"We've sent an email with a link to {SentTo}.";

    /// <summary>
    /// What the link does, and until when: it works once, and the room is booked only when the button on its page is
    /// pressed (ADR-0052 decisions 2 and 3). Empty until the API took a request.
    /// </summary>
    /// <remarks>The time is in UTC, as the mail writes it; see <see cref="PublicDateText.TimeUtc"/>.</remarks>
    public string LinkText => LinkExpiresAt is not DateTimeOffset expiresAt
        ? string.Empty
        : "Open the link and press the button on its page: your room is booked only then. "
            + $"The link works once, until {PublicDateText.TimeUtc(expiresAt)}.";

    /// <summary>
    /// The text that stands where a price would otherwise stand. The price model does not exist in Phase 1
    /// (BR-C-05, BR-31), and an empty field would look like a fault rather than a deliberate omission.
    /// </summary>
    public static string PaymentText => "Payment on arrival";

    /// <summary>The party as text, for example <c>2 guests</c>.</summary>
    public string GuestsText => DescribeGuests(Guests);

    /// <summary>The title of the empty state when rooms are free but none sleeps the party.</summary>
    public string NoRoomForGuestsText => $"No free room sleeps {Guests}";

    /// <summary>
    /// Describes a number of guests.
    /// </summary>
    /// <param name="guests">The number of guests.</param>
    /// <returns>For example <c>1 guest</c> or <c>3 guests</c>.</returns>
    public static string DescribeGuests(int guests) => guests == 1
        ? "1 guest"
        : $"{guests.ToString(CultureInfo.InvariantCulture)} guests";

    // ------------------------------------------------------------------
    // DISPLAY HELPERS
    // ------------------------------------------------------------------

    /// <summary>
    /// Gives the classes for one point in the step indicator.
    /// </summary>
    /// <param name="step">The step being drawn.</param>
    /// <returns>The class string, for example <c>"step step-active"</c>.</returns>
    public string StepClass(BookingFlowStep step) => step switch
    {
        _ when step < CurrentStep => "step step-done",
        _ when step == CurrentStep => "step step-active",
        _ => "step"
    };

    /// <summary>
    /// Gives <c>aria-current</c> for one point in the step indicator.
    /// </summary>
    /// <param name="step">The step being drawn.</param>
    /// <returns><c>"step"</c> on the current step; otherwise <c>null</c>, so the attribute is left out.</returns>
    public string? StepAria(BookingFlowStep step) => step == CurrentStep ? "step" : null;

    /// <summary>
    /// Describes how many people the rooms of a type sleep.
    /// </summary>
    /// <param name="sleeps">How many the biggest free room of the type sleeps.</param>
    /// <returns>For example <c>"sleeps 1"</c> or <c>"sleeps up to 3"</c>.</returns>
    public static string DescribeSleeps(int sleeps) => sleeps == 1
        ? "sleeps 1"
        : $"sleeps up to {sleeps.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Describes how many rooms of a type are free.
    /// </summary>
    /// <param name="free">The number of free rooms of the type.</param>
    /// <returns><c>"Last room"</c> for one, otherwise for example <c>"4 rooms free"</c>.</returns>
    public static string DescribeFree(int free) => free == 1
        ? "Last room"
        : $"{free.ToString(CultureInfo.InvariantCulture)} rooms free";

    // ------------------------------------------------------------------
    // OVERGANGE
    // ------------------------------------------------------------------

    /// <summary>
    /// Asks the API once which way a booking on the public site goes (MIL-088, ADR-0052 decision 8). A second call does
    /// nothing, so the flow keeps one mode from its start to its end.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the mode is known, or when the API could not say.</returns>
    /// <remarks>
    /// An answer that is no answer leaves the flow booking at once: the API refuses that with
    /// <c>booking.verification_required</c> when the verification is on, and the text of that code asks the guest to reload
    /// the page, which asks again. Without the runner of the requests the flow books at once, as before MIL-088.
    /// </remarks>
    public async Task LoadModeAsync(CancellationToken cancellationToken = default)
    {
        if (_requests is null || _isModeLoaded)
        {
            return;
        }

        _isModeLoaded = true;

        Result<PublicBookingModeDto> mode = await _requests
            .RunAsync((service, token) => service.GetModeAsync(token), cancellationToken)
            .ConfigureAwait(false);

        IsVerificationRequired = mode.IsSuccess && mode.Value.VerificationRequired;
    }

    /// <summary>
    /// Fetches the free rooms for the chosen dates and goes to step 2 (BR-C-02, BR-C-04).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Invalid dates keep the user on step 1 with a message at the field. The service call
    /// therefore never happens for a period we already know is illegal.
    /// </remarks>
    public async Task FindRoomsAsync(CancellationToken cancellationToken = default)
    {
        HasAttemptedDates = true;

        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        var checkIn = CheckInDate!.Value;
        var checkOut = CheckOutDate!.Value;

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync(
                    (service, token) => service.GetAvailableRoomsAsync(checkIn, checkOut, null, token),
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                AvailableRooms = NoRooms;
                AreRoomsLoaded = false;

                return;
            }

            Errors = NoMessages;
            AvailableRooms = result.Value;
            AreRoomsLoaded = true;

            // A room type chosen for an earlier period must not follow along into one where it is not free.
            if (SelectedRoomType is RoomSize chosen && !IsVisible(chosen))
            {
                SelectedRoomType = null;
            }

            CurrentStep = BookingFlowStep.Room;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Chooses a room type and goes to step 3. A type that has no free room for the party is ignored, and invalid
    /// dates send the guest back to step 1 (BR-C-04).
    /// </summary>
    /// <param name="size">The room type.</param>
    public void SelectRoomType(RoomSize size)
    {
        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        if (!IsVisible(size))
        {
            return;
        }

        SelectedRoomType = size;
        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Guest;
    }

    /// <summary>
    /// Goes back to step 1. The input is kept.
    /// </summary>
    public void BackToDates()
    {
        if (IsBusy)
        {
            return;
        }

        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Dates;
    }

    /// <summary>
    /// Goes back to step 2. The guest details are kept.
    /// </summary>
    /// <remarks>
    /// If the rooms have never been fetched — or the dates have since been changed to something invalid —
    /// the user ends up on step 1 instead of on an empty room list.
    /// </remarks>
    public void BackToRooms()
    {
        if (IsBusy)
        {
            return;
        }

        Errors = NoMessages;
        CurrentStep = HasValidDates && AreRoomsLoaded
            ? BookingFlowStep.Room
            : BookingFlowStep.Dates;
    }

    /// <summary>
    /// Drops the guests filter, so every free room is listed. The guest's way out when nothing sleeps the party.
    /// </summary>
    public void ShowAllRooms() => Guests = 1;

    /// <summary>
    /// Takes the dates and the party from the address of the page, as the booking box on the front page
    /// writes them: <c>?checkIn=2026-03-12&amp;checkOut=2026-03-15&amp;guests=2</c>.
    /// </summary>
    /// <param name="checkIn">The arrival date as <c>yyyy-MM-dd</c>, or <c>null</c>.</param>
    /// <param name="checkOut">The departure date as <c>yyyy-MM-dd</c>, or <c>null</c>.</param>
    /// <param name="guests">The number of guests as the address spells it, or <c>null</c>.</param>
    /// <returns>
    /// True when both dates were readable and make a valid stay, so the page can fetch the rooms at once.
    /// A date that is missing or unreadable leaves the suggested dates alone and returns false; dates that break a
    /// rule of the stay are taken and shown with the rule. A party that is not a whole number from 1 to
    /// <see cref="MaxGuests"/> is ignored (BR-C-07, MIL-086). The party is read either way.
    /// </returns>
    public bool Prefill(string? checkIn, string? checkOut, string? guests)
    {
        if (TryParseQueryGuests(guests, out int party) && BookingEntity.CheckPublicParty(party) is null)
        {
            Guests = party;
        }

        if (!TryParseQueryDate(checkIn, out var start) || !TryParseQueryDate(checkOut, out var end))
        {
            return false;
        }

        CheckInDate = start;
        CheckOutDate = end;

        return HasValidDates;
    }

    /// <summary>
    /// Validates the guest details and creates the booking (BR-C-01, BR-C-03).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <c>ValidateFields</c> is called <b>before</b> the creation. It is not a duplication of
    /// the rules — it is the same rule, called without IO — and it is the difference between seeing
    /// one's errors and seeing them after a round trip to the database.
    /// <para>
    /// The booking is created without <c>GuestId</c> and with <c>NewGuest</c> filled in (BR-47).
    /// The status is set by the domain to <c>Pending</c>; the confirmation is the staff's
    /// action (BR-C-03), and there is therefore no button for it here.
    /// </para>
    /// <para>
    /// If the room becomes occupied between the display and the click, the list is fetched again, and the customer
    /// is sent back to step 2 with the message kept. The alternative — an error on a
    /// form whose room no longer exists — is a dead end.
    /// </para>
    /// <para>
    /// With the verification on, the same answers are asked for instead (MIL-088, ADR-0052 decision 1), and the flow
    /// ends in the inbox step: nothing is booked until the button behind the link is pressed.
    /// </para>
    /// </remarks>
    public async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (!HasValidDates)
        {
            HasAttemptedDates = true;
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        if (SelectedRoomType is not RoomSize size)
        {
            CurrentStep = HasValidDates && AreRoomsLoaded ? BookingFlowStep.Room : BookingFlowStep.Dates;

            return;
        }

        var fields = BuildGuestFields();

        var validation = _guests.Run(service => service.ValidateFields(fields));

        if (validation.IsFailure)
        {
            GuestErrors = GuestFieldErrors.FromResult(validation);
            Errors = GuestErrors.Other;

            return;
        }

        GuestErrors = GuestFieldErrors.None;
        Errors = NoMessages;

        CreatePublicBookingRequest request = RequestFor(size, fields);

        if (IsVerificationRequired && _requests is { } requests)
        {
            await AskAsync(requests, request, isRepeat: false, cancellationToken).ConfigureAwait(false);

            return;
        }

        Result<PublicBookingDto> result;

        IsBusy = true;

        try
        {
            // The public booking answers with the receipt and the guest's session (MIL-018, ADR-0049 decision 3).
            result = await _guestAccess
                .RunAsync((service, token) => service.BookAsync(request, token), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        if (result.IsSuccess)
        {
            Confirmation = result.Value.Receipt;
            SessionTicket = _tickets.Issue(result.Value.Session);
            CurrentStep = BookingFlowStep.Receipt;

            return;
        }

        await ShowRefusalAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asks again with the same answers from the inbox step (MIL-088, ADR-0052 decision 6): a new mail with a new link,
    /// under the same limits, and the link of the earlier mail stops working.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// Dates that are no longer valid, because the day has moved on, send the guest back to step 1 (BR-C-04), like
    /// <see cref="SubmitAsync"/>. A refusal is shown on the inbox step, except one that names a field or the room, which
    /// takes the guest to the step that can fix it.
    /// </remarks>
    public async Task SendAgainAsync(CancellationToken cancellationToken = default)
    {
        if (!CanSendAgain || _requests is not { } requests)
        {
            return;
        }

        if (!HasValidDates || SelectedRoomType is not RoomSize size)
        {
            HasAttemptedDates = true;
            Notice = null;
            CurrentStep = BookingFlowStep.Dates;

            return;
        }

        Errors = NoMessages;

        await AskAsync(requests, RequestFor(size, BuildGuestFields()), isRepeat: true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Goes back from the inbox step to the guest details, with everything typed kept, so a mistyped email can be put
    /// right and asked for again.
    /// </summary>
    public void BackToDetails()
    {
        if (IsBusy)
        {
            return;
        }

        Errors = NoMessages;
        Notice = null;
        CurrentStep = HasValidDates && SelectedRoomType is not null
            ? BookingFlowStep.Guest
            : BookingFlowStep.Dates;
    }

    /// <summary>
    /// Starts a new flow from scratch. Used from the receipt and from the inbox step.
    /// </summary>
    /// <remarks>The mode of the flow is kept: it is the API's, not the guest's.</remarks>
    public void StartOver()
    {
        if (IsBusy)
        {
            return;
        }

        CheckInDate = _clock.Today.AddDays(1);
        CheckOutDate = _clock.Today.AddDays(1 + DefaultNights);
        Guests = 1;
        HasAttemptedDates = false;
        AvailableRooms = NoRooms;
        AreRoomsLoaded = false;
        SelectedRoomType = null;
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Country = string.Empty;
        PassportNumber = string.Empty;
        GuestErrors = GuestFieldErrors.None;
        Confirmation = null;
        SessionTicket = null;
        SentTo = null;
        LinkExpiresAt = null;
        Notice = null;
        Errors = NoMessages;
        CurrentStep = BookingFlowStep.Dates;
    }

    // ------------------------------------------------------------------
    // PRIVAT
    // ------------------------------------------------------------------

    /// <summary>Reads a date of the address. Only the exact <c>yyyy-MM-dd</c> of the booking box counts.</summary>
    private static bool TryParseQueryDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, QueryDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Reads the party of the address: a whole number that fits an <see cref="int"/>, in the invariant culture.</summary>
    private static bool TryParseQueryGuests(string? text, out int guests) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out guests);

    /// <summary>The error code at the arrival field, or <c>null</c>.</summary>
    private string? CheckInErrorCode
    {
        get
        {
            if (CheckInDate is not DateOnly checkIn || checkIn == default)
            {
                return HasAttemptedDates ? ErrorCodes.Booking.StartDateRequired : null;
            }

            return checkIn < _clock.Today ? ErrorCodes.Booking.StartDateInPast : null;
        }
    }

    /// <summary>
    /// The error code at the departure field, or <c>null</c>: a departure that is not after the arrival, beyond the five
    /// years, or more than 30 nights after it (BR-C-07, BR-N-08, MIL-086).
    /// </summary>
    private string? CheckOutErrorCode
    {
        get
        {
            if (CheckOutDate is not DateOnly checkOut || checkOut == default)
            {
                return HasAttemptedDates ? ErrorCodes.Booking.EndDateRequired : null;
            }

            if (CheckInDate is not DateOnly checkIn)
            {
                return null;
            }

            if (checkOut <= checkIn)
            {
                return ErrorCodes.Booking.EndNotAfterStart;
            }

            // An arrival in the past is shown at the arrival, so the departure shows only the rules that it breaks.
            string? stayError = StayDates.PublicError(checkIn, checkOut, _clock.Today);
            return stayError is ErrorCodes.Booking.StartDateInPast ? null : stayError;
        }
    }

    /// <summary>The chosen arrival when the page offers it, from today to <see cref="LatestCheckIn"/>; otherwise <c>null</c>.</summary>
    private DateOnly? ChosenArrival =>
        CheckInDate is DateOnly arrival && arrival >= _clock.Today && arrival <= LatestCheckIn ? arrival : null;

    /// <summary>
    /// Translates one error code to the text of the screen through <see cref="ErrorMessages"/>.
    /// </summary>
    /// <param name="code">The error code, or <c>null</c>.</param>
    /// <returns>The text, or <c>null</c>.</returns>
    /// <remarks>
    /// The date validation deliberately uses the codes of the Application layer rather than its own
    /// texts. Then the client validation says word for word the same as the service would say, and
    /// there is still only one place with the error texts (A-09).
    /// </remarks>
    private static string? Describe(string? code) =>
        code is null ? null : ErrorMessages.Describe(new Error(code));

    /// <summary>Builds the guest fields from the form, trimmed (BR-47).</summary>
    private GuestFields BuildGuestFields() => new(
        FirstName.Trim(),
        LastName.Trim(),
        Email.Trim(),
        PhoneNumber.Trim(),
        Country.Trim(),
        string.IsNullOrWhiteSpace(PassportNumber) ? null : PassportNumber.Trim());

    /// <summary>Builds the booking of the flow: the dates, the room type, the party and the new guest (BR-47).</summary>
    private CreatePublicBookingRequest RequestFor(RoomSize size, GuestFields fields) => new(
        CheckInDate!.Value,
        CheckOutDate!.Value,
        size,
        Guests,
        fields);

    /// <summary>
    /// Asks for the booking (MIL-088, ADR-0052 decision 1) and goes to the inbox step when the API took the request; a
    /// refusal is shown as a refusal of the booking at once is.
    /// </summary>
    /// <param name="requests">Runs the booking verified by email.</param>
    /// <param name="request">The same answers as a booking at once.</param>
    /// <param name="isRepeat">True for "Send again", whose notice says that the earlier link stopped working.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task AskAsync(
        IUseCaseRunner<IBookingRequestService> requests,
        CreatePublicBookingRequest request,
        bool isRepeat,
        CancellationToken cancellationToken)
    {
        Result<BookingRequestReceiptDto> result;

        IsBusy = true;

        try
        {
            result = await requests
                .RunAsync((service, token) => service.AskAsync(request, token), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        if (result.IsSuccess)
        {
            SentTo = request.NewGuest?.Email;
            LinkExpiresAt = result.Value.ExpiresAt;
            Notice = isRepeat ? NewLinkNotice : null;
            CurrentStep = BookingFlowStep.Inbox;

            return;
        }

        Notice = null;

        await ShowRefusalAsync(WithVerificationOff(result), cancellationToken).ConfigureAwait(false);

        // A field that the API refused is shown at its field, which the inbox step does not have.
        if (CurrentStep == BookingFlowStep.Inbox && HasFieldErrors)
        {
            CurrentStep = BookingFlowStep.Guest;
        }
    }

    /// <summary>
    /// Shows a refusal of the booking or of the request: the errors of the fields at the fields and the rest above the
    /// form. A refusal that names the room drops the choice, fetches the rooms again and keeps the reason.
    /// </summary>
    private async Task ShowRefusalAsync(Result result, CancellationToken cancellationToken)
    {
        GuestErrors = GuestFieldErrors.FromResult(result);
        Errors = GuestErrors.Other;

        if (!MentionsRoom(result))
        {
            return;
        }

        var messages = Errors;

        SelectedRoomType = null;

        await FindRoomsAsync(cancellationToken).ConfigureAwait(false);

        if (Errors.Count == 0)
        {
            Errors = messages;
        }
    }

    /// <summary>
    /// The API answers a request with <c>booking.not_found</c> when the verification was switched off after the page
    /// asked the mode (ADR-0052 decision 8). The way to book changed under the page, so the guest gets the text that asks
    /// for a reload rather than a booking that was not found.
    /// </summary>
    private static Result WithVerificationOff(Result result) =>
        result.Errors.Any(error => error.Code == ErrorCodes.Booking.NotFound)
            ? Result.Failure(ErrorCodes.Booking.VerificationRequired)
            : result;

    /// <summary>True when a refusal put an error on a field of the guest form.</summary>
    private bool HasFieldErrors =>
        GuestErrors.FirstName is not null
        || GuestErrors.LastName is not null
        || GuestErrors.Email is not null
        || GuestErrors.PhoneNumber is not null
        || GuestErrors.Country is not null
        || GuestErrors.PassportNumber is not null;

    /// <summary>True if the result is about the room rather than about the guest.</summary>
    private static bool MentionsRoom(Result result)
    {
        foreach (var error in result.Errors)
        {
            if (error.Code is ErrorCodes.Booking.RoomNotAvailable
                or ErrorCodes.Booking.RoomNotBookable
                or ErrorCodes.Booking.RoomTypeNotAvailable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the room type has a free room that sleeps the party.</summary>
    private bool IsVisible(RoomSize size) => VisibleRoomTypes.Any(choice => choice.Size == size);
}
