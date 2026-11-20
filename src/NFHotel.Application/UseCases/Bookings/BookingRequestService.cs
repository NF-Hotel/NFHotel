using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// The booking request of the public site and its verification by email (UC-011, MIL-088, ADR-0052 and ADR-0053).
/// </summary>
/// <remarks>
/// The checks of a public booking stay in <see cref="IPublicBookingService"/>: the request runs them before it stores
/// anything, and the verification books through them, so the two ways of the public booking cannot drift apart. The use
/// case writes the security events where it decides, because only it knows which limit refused and whether a mail went.
/// </remarks>
public sealed class BookingRequestService : IBookingRequestService
{
    #region Fields

    /// <summary>The most verification mails to one mailbox in an hour (ADR-0052 decision 6).</summary>
    public const int MailsPerHour = 3;

    /// <summary>The most verification mails to one mailbox in a day (ADR-0052 decision 6).</summary>
    public const int MailsPerDay = 5;

    /// <summary>
    /// The name of the one lock of the database that every request takes before it counts and writes, so two requests
    /// never count at the same time (ADR-0052 decision 6).
    /// </summary>
    public const string LockName = "booking_request";

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan Day = TimeSpan.FromDays(1);

    private readonly IPublicBookingService _bookings;
    private readonly IBookingRequestRepository _requests;
    private readonly ISentMailRepository _sentMails;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMailSender _mailSender;
    private readonly IBookingVerificationSettings _settings;
    private readonly IGuestTokenIssuer _tokens;
    private readonly IBookingRequestEvents _events;
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="bookings">The checks and the booking of the public site.</param>
    /// <param name="requests">The booking requests.</param>
    /// <param name="sentMails">The records of the mails sent.</param>
    /// <param name="unitOfWork">The transaction boundary, shared with <paramref name="bookings"/>.</param>
    /// <param name="mailSender">Sends the mails through the relay.</param>
    /// <param name="settings">The setting of the verification, the site address and the fuse.</param>
    /// <param name="tokens">Issues the guest session after a verification (MIL-018).</param>
    /// <param name="events">Writes the security events.</param>
    /// <param name="clock">The clock of the application (A-05).</param>
    public BookingRequestService(
        IPublicBookingService bookings,
        IBookingRequestRepository requests,
        ISentMailRepository sentMails,
        IUnitOfWork unitOfWork,
        IMailSender mailSender,
        IBookingVerificationSettings settings,
        IGuestTokenIssuer tokens,
        IBookingRequestEvents events,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(sentMails);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(mailSender);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(clock);

        _bookings = bookings;
        _requests = requests;
        _sentMails = sentMails;
        _unitOfWork = unitOfWork;
        _mailSender = mailSender;
        _settings = settings;
        _tokens = tokens;
        _events = events;
        _clock = clock;
    }

    #endregion Constructors

    #region Properties

    // The start of the API refuses to run with the verification on and no absolute https site address (decision 8).
    private Uri SiteAddress => _settings.SiteAddress
        ?? throw new InvalidOperationException("The setting BookingVerification:SiteAddress is missing.");

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public Task<Result<PublicBookingModeDto>> GetModeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<PublicBookingModeDto>.Success(new PublicBookingModeDto(_settings.Enabled)));

    /// <inheritdoc />
    public async Task<Result<BookingRequestReceiptDto>> AskAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.Enabled)
        {
            return Result<BookingRequestReceiptDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        Result check = await _bookings.CheckPublicAsync(request, cancellationToken).ConfigureAwait(false);

        if (check.IsFailure)
        {
            // ! is justified: the limit per mailbox counts only when the request holds a guest.
            WriteEmailLimitEvent(check, () => request.NewGuest!.Email);
            return Result<BookingRequestReceiptDto>.Failure(check.Errors);
        }

        // ! is justified: the check accepted the guest of the request, so the request holds one and its fields make a
        // guest; the request keeps their normal forms.
        Guest guest = GuestService.NewGuest(request.NewGuest!).Value;
        DateRange stay = new(request.CheckInDate, request.CheckOutDate);
        string emailKey = VerificationTokens.KeyOfEmail(guest.Email);
        (string token, string tokenHash) = VerificationTokens.NewToken();

        Result<StoredRequest> stored = await _unitOfWork.RunUnderLockAsync(
            LockName,
            work => StoreRequestAsync(guest, stay, request, emailKey, tokenHash, work),
            cancellationToken).ConfigureAwait(false);

        if (stored.IsFailure)
        {
            if (stored.FirstError?.Code == ErrorCodes.Booking.TooManyMails)
            {
                _events.LimitRefused(LimitNames.VerificationMail, guest.Email);
            }
            else
            {
                _events.MailFuseBlown();
            }

            return Result<BookingRequestReceiptDto>.Failure(stored.Errors);
        }

        (BookingRequest kept, SentMail record) = stored.Value;
        MailSendResult sent = await _mailSender
            .SendAsync(BookingMails.Request(kept, BookingMails.LinkOf(SiteAddress, token)), cancellationToken)
            .ConfigureAwait(false);

        if (!sent.Sent)
        {
            // The guest never got the link: the request goes, unless a newer request of the mailbox replaced it since,
            // and so does the record, so a mail that did not go counts against no limit (decision 6).
            await _requests.ClaimOpenAsync(tokenHash, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
            _sentMails.Remove(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _events.MailNotSent(MailKind.VerificationLink, sent.FailureReason ?? MailFailureReasons.Other);
            return Result<BookingRequestReceiptDto>.Failure(ErrorCodes.Booking.MailNotSent);
        }

        _events.MailSent(MailKind.VerificationLink);
        _events.RequestMade(guest.Email);
        return Result<BookingRequestReceiptDto>.Success(new BookingRequestReceiptDto(kept.ExpiresAt));
    }

    /// <inheritdoc />
    public async Task<Result<PublicBookingDto>> VerifyAsync(
        VerifyBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.Enabled)
        {
            return Result<PublicBookingDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        // A text of the wrong shape claims nothing and never reaches the database (decision 3).
        BookingRequest? claimed = VerificationTokens.HashOf(request.Token) is { } tokenHash
            ? await _requests.ClaimOpenAsync(tokenHash, _clock.UtcNow, cancellationToken).ConfigureAwait(false)
            : null;

        if (claimed is null)
        {
            _events.VerificationFailed();
            return Result<PublicBookingDto>.Failure(ErrorCodes.Booking.VerificationLinkInvalid);
        }

        Result<BookingDetailsDto> booked = await _bookings
            .BookVerifiedAsync(BookingOf(claimed), cancellationToken)
            .ConfigureAwait(false);

        if (booked.IsFailure)
        {
            WriteEmailLimitEvent(booked, () => claimed.Email);
            return Result<PublicBookingDto>.Failure(booked.Errors);
        }

        GuestSessionDto session = _tokens.Issue(booked.Value.BookingId);
        _events.PublicBookingMade(claimed.Email);
        BookingReceiptDto receipt = BookingReceiptDto.From(booked.Value);

        // The booking stands whatever the second mail does (decision 5), so a guest who leaves the page does not stop it,
        // and a failure of the mail or of its record after the retries ends as the event of a mail not sent: the guest
        // gets the receipt on the screen, never an error after the booking was made (RC-436 F1).
        try
        {
            await SendReceiptAsync(claimed.Email, receipt, CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // The booking is made: whatever fails in its second mail is counted, and the answer stays the receipt.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _events.MailNotSent(MailKind.BookingReceipt, MailFailureReasons.Other);
        }

        return Result<PublicBookingDto>.Success(new PublicBookingDto(receipt, session));
    }

    #endregion Public methods

    #region Private methods

    // The request as the public booking takes it: the stay, the size, the party and the fields of the guest.
    private static CreatePublicBookingRequest BookingOf(BookingRequest claimed) => new(
        claimed.StartDate,
        claimed.EndDate,
        claimed.RoomSize,
        claimed.Guests,
        new GuestFields(
            claimed.FirstName,
            claimed.LastName,
            claimed.Email,
            claimed.PhoneNumber,
            claimed.Country,
            claimed.PassportNumber));

    // Under the lock: the limits per mailbox and the fuse, then the request and the record of its mail in one save, the
    // record written before the mail is sent (decision 6).
    private async Task<Result<StoredRequest>> StoreRequestAsync(
        Guest guest,
        DateRange stay,
        CreatePublicBookingRequest request,
        string emailKey,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;

        if (await _sentMails.CountForEmailSinceAsync(emailKey, MailKind.VerificationLink, now - Hour, cancellationToken)
                .ConfigureAwait(false) >= MailsPerHour
            || await _sentMails.CountForEmailSinceAsync(emailKey, MailKind.VerificationLink, now - Day, cancellationToken)
                .ConfigureAwait(false) >= MailsPerDay)
        {
            return Result<StoredRequest>.Failure(ErrorCodes.Booking.TooManyMails);
        }

        if (await FuseIsBlownAsync(now, cancellationToken).ConfigureAwait(false))
        {
            return Result<StoredRequest>.Failure(ErrorCodes.Booking.MailPaused);
        }

        BookingRequest? kept = await _requests.FindByEmailKeyAsync(emailKey, cancellationToken).ConfigureAwait(false);

        if (kept is null)
        {
            kept = BookingRequest.Create(guest, stay, request.RoomSize, request.Guests, tokenHash, emailKey, now);
            _requests.Add(kept);
        }
        else
        {
            kept.Renew(guest, stay, request.RoomSize, request.Guests, tokenHash, now);
        }

        SentMail record = SentMail.Record(emailKey, MailKind.VerificationLink, now);
        _sentMails.Add(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<StoredRequest>.Success(new StoredRequest(kept, record));
    }

    // The mail with the reference (decision 5), under the fuse of ADR-0053 decision 6: the record is written under the
    // lock before the mail is sent and removed when the relay does not take it. Nothing here undoes the booking.
    private async Task SendReceiptAsync(string email, BookingReceiptDto receipt, CancellationToken cancellationToken)
    {
        string emailKey = VerificationTokens.KeyOfEmail(email);
        SentMail? record = await _unitOfWork.RunUnderLockAsync<SentMail?>(
            LockName,
            async work =>
            {
                DateTimeOffset now = _clock.UtcNow;

                if (await FuseIsBlownAsync(now, work).ConfigureAwait(false))
                {
                    return null;
                }

                SentMail kept = SentMail.Record(emailKey, MailKind.BookingReceipt, now);
                _sentMails.Add(kept);
                await _unitOfWork.SaveChangesAsync(work).ConfigureAwait(false);
                return kept;
            },
            cancellationToken).ConfigureAwait(false);

        if (record is null)
        {
            _events.MailNotSent(MailKind.BookingReceipt, MailFailureReasons.Fuse);
            _events.MailFuseBlown();
            return;
        }

        MailSendResult sent = await _mailSender
            .SendAsync(BookingMails.Receipt(email, receipt, SiteAddress), cancellationToken)
            .ConfigureAwait(false);

        if (sent.Sent)
        {
            _events.MailSent(MailKind.BookingReceipt);
            return;
        }

        _sentMails.Remove(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _events.MailNotSent(MailKind.BookingReceipt, sent.FailureReason ?? MailFailureReasons.Other);
    }

    // The fuse of ADR-0053 decision 6: the mails of every mailbox and kind in the last day.
    private async Task<bool> FuseIsBlownAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await _sentMails.CountSinceAsync(now - Day, cancellationToken).ConfigureAwait(false) >= _settings.MaxMailsPerDay;

    // The refusal by the limit of coming public bookings per mailbox is a security event (ADR-0051 decision 6).
    private void WriteEmailLimitEvent(Result result, Func<string> email)
    {
        if (result.Errors.Any(error => error.Code == ErrorCodes.Booking.TooManyRequests))
        {
            _events.LimitRefused(LimitNames.Email, email());
        }
    }

    #endregion Private methods

    #region Nested types

    // A request stored under the lock, and the record of its mail.
    private sealed record StoredRequest(BookingRequest Request, SentMail Record);

    #endregion Nested types
}
