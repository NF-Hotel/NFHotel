# SD-001 Book a Room and Verify It by Email

## Metadata
| Key | Value |
| --- | --- |
| ID | SD-001 |
| CrossReference | [OC-002], [SSD-002], [UC-011], [DICT-001], [SA-001], [ADR-0047], [ADR-0049], [ADR-0051], [ADR-0052], [ADR-0053] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version, the first Sequence Diagram of the project; task 3 of MIL-088, written by the AI from the code of the branch: verifyBooking through the claim of the request, the booking under the lock `public_booking`, the guest session and the mail with the reference (ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | [739a25b] |

---

## Purpose

How the objects of the website and the API carry out verifyBooking, contract CO4 of [OC-002]: the step that turns a
booking request into a booking. It answers the concern CN-006 of S05, no double booking, because two locks of the
database and its exclusion constraint decide every race for a room and for a link; and CN-007 of S06, only the own
booking without an account and personal data kept private, because the request is taken once and is gone after it,
and the guest is signed in only after the verification ([SA-001]). It serves the developers, and S04 (CN-005), who
can trace each message to the code and to its decision in [ADR-0052] and [ADR-0053].

The other contracts of [OC-002] have no sequence diagram yet. bookAtOnce (CO6) books through the same frame as the
second diagram below, without a request and without a mail.

The lifelines are grouped by the layers of the design class diagram (`docs/dcd.md`): Delivery, Application, Domain
and Infrastructure. A lifeline of an outer layer that implements a port of an inner layer is named after both, the
port first (`IMailSender` and `:SmtpMailSender`), so a call across a layer boundary goes through the port. The
website is drawn as one lifeline: its page `BookingVerify` and its view model `BookingVerifyViewModel` call the API
through the port `IBookingRequestService`, which the API client `ApiBookingRequestService` implements with an HTTP
request. PostgreSQL and the mail relay are outside the application.

## Sequence: verifyBooking

**Realizes:** `verifyBooking` in [OC-002] (CO4), message 4 of [SSD-002]

### Diagram

From the button to the receipt:

```plantuml
@startuml
hide footbox
skinparam sequenceMessageAlign left
actor "Guest" as Guest
box "Delivery" #FADADD
  participant ":BookingVerify\n(web page)" as Page
  participant ":BookingsController" as Ctl
  participant "IBookingRequestEvents\n:ApiBookingRequestEvents" as Events
end box
box "Application" #FFF3B0
  participant "IBookingRequestService\n:BookingRequestService" as Svc
  participant "IPublicBookingService\n:BookingService" as Book
end box
box "Domain" #CDEFD3
  participant "claimed\n:BookingRequest" as Req
end box
box "Infrastructure" #D4E4F7
  participant "IBookingRequestRepository\n:BookingRequestRepository" as ReqRepo
  participant "IGuestTokenIssuer\n:JwtGuestTokenIssuer" as Tokens
  database "PostgreSQL" as DB
end box

Guest -> Page : press "Book the room"\n(POST with the token and the antiforgery token in the body)
activate Page
Page -> Ctl : POST api/bookings/public/requests/verify\n{ token } in the body
activate Ctl
Ctl -> Svc : VerifyAsync(VerifyBookingRequest)
activate Svc
Svc -> Svc : tokenHash = VerificationTokens.HashOf(token)
note right of Svc : null for a text that is not a token:\nit claims nothing and reaches no database
Svc -> ReqRepo : ClaimOpenAsync(tokenHash, now)
activate ReqRepo
ReqRepo -> DB : DELETE FROM booking_request\nWHERE token_hash = @hash AND expires_at > @now\nRETURNING *, xmin
activate DB
DB --> ReqRepo : the deleted row, or no row
deactivate DB
opt a row came back
  create Req
  ReqRepo -> Req : <<create>>
end
ReqRepo --> Svc : claimed, or null
deactivate ReqRepo

alt claimed is null: the link is unknown, used or lapsed, or a second press found no row
  Svc -> Events : VerificationFailed()
  Svc --> Ctl : booking.verification_link_invalid
  Ctl --> Page : 422
  Page --> Guest : the link does not work any more; ask again
else claimed
  Svc -> Svc : request = BookingOf(claimed)
  Svc -> Book : BookVerifiedAsync(request)
  activate Book
  ref over Book, DB : Book under the lock public_booking (second diagram)
  Book --> Svc : the booking details, or the refusal
  deactivate Book
  alt refused: the room went, the limit per mailbox, or a rule of the stay
    opt booking.too_many_requests
      Svc -> Events : LimitRefused("email", claimed.Email)
    end
    Svc --> Ctl : the codes of the refusal
    Ctl --> Page : 409, 429 or 422
    Page --> Guest : the text of the refusal
  else booked
    Svc -> Tokens : Issue(bookingId)
    activate Tokens
    Tokens --> Svc : GuestSessionDto (expires two hours from now)
    deactivate Tokens
    Svc -> Events : PublicBookingMade(claimed.Email)
    Svc -> Svc : receipt = BookingReceiptDto.From(details)
    ref over Svc, DB : Send the mail with the reference (third diagram)
    note right of Svc : whatever fails in the mail, its record included,\nends in a catch as MailNotSent(BookingReceipt, "other"):\nthe booking stands, and the answer is the receipt
    Svc --> Ctl : PublicBookingDto(receipt, session)
    Ctl --> Page : 200
    Page -> Page : GuestSignInRegistration.SignInAsync(session)\n(the guest cookie)
    Page --> Guest : the own booking with its reference\n(/my-booking?done=booked)
  end
end
deactivate Svc
deactivate Ctl
deactivate Page
@enduml
```

Book under the lock `public_booking`, `BookingService.BookVerifiedAsync`, the same frame as a booking at once:

```plantuml
@startuml
hide footbox
skinparam sequenceMessageAlign left
box "Application" #FFF3B0
  participant "IBookingRequestService\n:BookingRequestService" as Svc
  participant "IPublicBookingService\n:BookingService" as Book
end box
box "Domain" #CDEFD3
  participant "g\n:Guest" as G
  participant "b\n:Booking" as B
end box
box "Infrastructure" #D4E4F7
  participant "IUnitOfWork\n:UnitOfWork" as UoW
  participant "IBookingRepository, IBookingQueries\n:BookingRepository" as BRepo
  database "PostgreSQL" as DB
end box

Svc -> Book : BookVerifiedAsync(request)
activate Book
Book -> UoW : RunUnderLockAsync("public_booking", work)
activate UoW
UoW -> DB : BEGIN
UoW -> DB : SELECT pg_advisory_xact_lock(key)
note right of DB : key: the first 64 bits of the SHA-256 of "public_booking".\nEvery public booking waits here, so the public bookings\ncount and choose their rooms one after the other.
UoW -> Book : work(token)
activate Book
Book -> Book : PlanPublicAsync(request)\nthe party, the nights, the stay on today, the free rooms
activate Book
Book -> BRepo : GetOccupancyAsync(stay)
activate BRepo
BRepo -> DB : SELECT the bookings that block a room in the stay
BRepo --> Book : occupancies
deactivate BRepo
Book -> Book : rooms = RoomsInOrder(free rooms, size, guests)
Book -> BRepo : CountComingPublicBookingsAsync(email, today)
activate BRepo
BRepo -> DB : SELECT the emails of the coming public bookings\nof the domain of the email
BRepo --> Book : the number with the same mailbox
deactivate BRepo
deactivate Book
alt no room of the size sleeps the party, 3 coming bookings for the mailbox, or a rule is broken
  Book --> UoW : booking.room_type_not_available, booking.too_many_requests or the code of the rule
else planned
  loop each room of the order, at most MaxRoomTries = 5, until one is saved
    create G
    Book -> G : <<create>> GuestService.NewGuest(fields)
    create B
    Book -> B : <<create>> Booking.CreateForNewGuest(stay, room, g, today)
    note right of B : pending, with a new BookingReference;\nthe guest is stored with the booking
    Book -> B : MarkMadeOnPublicSite()
    Book -> BRepo : Add(b)
    Book -> UoW : SaveChangesAsync()
    activate UoW
    UoW -> DB : INSERT guest, INSERT booking: one save
    DB --> UoW : the keys of g and b, or an exclusion violation
    UoW --> Book : saved, or BookingOverlapConflictException
    deactivate UoW
    alt the exclusion constraint ex_booking_room_period refused the room:\na booking of staff took it since the plan
      Book -> BRepo : Discard(b)
      destroy B
      destroy G
    else saved
      Book -> Book : DescribeAsync(b), StorePricePerNightAsync(b)
      Book --> UoW : the booking details
    end
  end
  opt every room tried was taken
    Book --> UoW : booking.room_type_not_available
  end
end
deactivate Book
UoW -> DB : COMMIT, which releases the lock
UoW --> Book : the result of the work
deactivate UoW
Book --> Svc : the booking details, or the refusal
deactivate Book
@enduml
```

Send the mail with the reference, `BookingRequestService.SendReceiptAsync`:

```plantuml
@startuml
hide footbox
skinparam sequenceMessageAlign left
box "Delivery" #FADADD
  participant "IBookingRequestEvents\n:ApiBookingRequestEvents" as Events
end box
box "Application" #FFF3B0
  participant "IBookingRequestService\n:BookingRequestService" as Svc
end box
box "Domain" #CDEFD3
  participant "record\n:SentMail" as Rec
end box
box "Infrastructure" #D4E4F7
  participant "IUnitOfWork\n:UnitOfWork" as UoW
  participant "ISentMailRepository\n:SentMailRepository" as Mails
  participant "IMailSender\n:SmtpMailSender" as Smtp
  database "PostgreSQL" as DB
end box
participant "Mail relay" as Relay

Svc -> Svc : SendReceiptAsync(email, receipt)\nwith no cancellation: a guest who leaves the page does not stop it
activate Svc
Svc -> Svc : emailKey = VerificationTokens.KeyOfEmail(email)
Svc -> UoW : RunUnderLockAsync("booking_request", work)
activate UoW
UoW -> DB : BEGIN
UoW -> DB : SELECT pg_advisory_xact_lock(key)
note right of DB : the lock that every booking request takes\nbefore it counts and writes the mails
UoW -> Svc : work(token)
activate Svc
Svc -> Mails : CountSinceAsync(now - 24 hours)
activate Mails
Mails -> DB : SELECT count(*) FROM sent_mail\nWHERE sent_at > @since
Mails --> Svc : the mails of the last 24 hours
deactivate Mails
alt 400 or more (the fuse)
  Svc --> UoW : null
else
  create Rec
  Svc -> Rec : <<create>> SentMail.Record(emailKey, BookingReceipt, now)
  Svc -> Mails : Add(record)
  Svc -> UoW : SaveChangesAsync()
  UoW -> DB : INSERT sent_mail
  Svc --> UoW : record
end
deactivate Svc
UoW -> DB : COMMIT, which releases the lock
UoW --> Svc : record, or null
deactivate UoW

alt null: the fuse is blown
  Svc -> Events : MailNotSent(BookingReceipt, "fuse")
  Svc -> Events : MailFuseBlown()
else record
  Svc -> Svc : mail = BookingMails.Receipt(email, receipt, siteAddress)
  Svc -> Smtp : SendAsync(mail)
  activate Smtp
  Smtp -> Relay : SMTP over TLS, through the forwarder mail-relay where it is set,\nwithin one bound of 15 seconds, the quit included
  Relay --> Smtp : taken, or refused
  Smtp --> Svc : MailSendResult(sent, failureReason)
  deactivate Smtp
  alt sent
    Svc -> Events : MailSent(BookingReceipt)
  else not sent
    Svc -> Mails : Remove(record)
    Svc -> UoW : SaveChangesAsync()
    UoW -> DB : DELETE the record
    destroy Rec
    Svc -> Events : MailNotSent(BookingReceipt, reason)
  end
end
deactivate Svc
@enduml
```

Not drawn: with the verification by email switched off, `VerifyAsync` answers `booking.not_found` before it claims
anything, and the page shows the text of an invalid link; a token that is missing or longer than 64 characters is
refused with `api.invalid_request` before the action runs. Both change nothing (the first two exceptions of CO4).

### Pattern Annotations

| Pattern (GRASP / GoF) | Applied to | Rationale |
| --- | --- | --- |
| Controller (GRASP) | `BookingsController` | Receives the system operation verifyBooking and hands it to the use case; it holds no rule |
| Pure Fabrication (GRASP), use case controller | `BookingRequestService` | Coordinates the request's way to a booking: the claim, the booking, the session, the events and the mail. It is no concept of the Domain Model |
| Information Expert (GRASP) | `BookingService` (`PlanPublicAsync`, `RoomsInOrder`), `Booking.CreateForNewGuest`, `VerificationTokens`, `Mailbox` | Each knows its rules: the rules and the room order of a public booking, the invariants of a new booking, the form of a token, the normal form of a mailbox |
| Creator (GRASP), Factory Method (GoF) | `Booking.CreateForNewGuest`, `GuestService.NewGuest`, `SentMail.Record`; `BookingRequestRepository` for the claimed request | The static creation methods make valid objects only; the repository makes the request from the row that it deleted |
| Repository and Unit of Work (Fowler) | `IBookingRequestRepository`, `IBookingRepository`, `ISentMailRepository`, `IUnitOfWork` | The repositories mark and read; only the unit of work saves, and `RunUnderLockAsync` gives a piece of work one transaction under a lock of the database |
| Command (GoF) | the work passed to `RunUnderLockAsync` | The unit of work runs the work inside its transaction and runs it again when the execution strategy of the database retries |
| Adapter (GoF), Protected Variations (GRASP) | `SmtpMailSender` for `IMailSender`, `JwtGuestTokenIssuer` for `IGuestTokenIssuer`, `ApiBookingRequestEvents` for `IBookingRequestEvents`, the repositories and `UnitOfWork` for their ports | The use case does not change when the relay, the token format, the log or the database changes; the tests replace each port |

### Postcondition Coverage

| Postcondition (from contract) | Satisfied by message |
| --- | --- |
| r was deleted | `ClaimOpenAsync` with `DELETE ... RETURNING` (first diagram); the claim commits on its own, so r stays deleted whatever follows |
| A `Guest` g was created with the fields of r | `BookingOf(claimed)` (first diagram) and `<<create>> GuestService.NewGuest(fields)` (second diagram) |
| A `Booking` b was created: a new `BookingReference`, the dates of r, pending, `MadeOnPublicSite` set | `<<create>> Booking.CreateForNewGuest(stay, room, g, today)` and `MarkMadeOnPublicSite()` (second diagram) |
| b was associated with g and with a free `Room` of r.RoomSize that sleeps r.Guests; g and b were stored together | `RoomsInOrder` and the loop over the rooms; `SaveChangesAsync()` with one INSERT of the guest and the booking, under the lock `public_booking`, retried on the next room when the exclusion constraint refuses (second diagram) |
| A `GuestSession` gs was created and associated with b, ending two hours from now | `Issue(bookingId)` (first diagram); the page keeps it in the guest cookie (`SignInAsync`) |
| A `SentMail` s was created, unless the fuse is blown, the relay did not take the mail, or the sending failed in another way before s was stored | `SentMail.Record`, `Add` and `SaveChangesAsync` under the lock `booking_request`; `Remove` when the relay refuses (third diagram); any other failure ends in the catch of `VerifyAsync` with `MailNotSent(BookingReceipt, "other")` (first diagram) |

### Responsibility Check

No lifeline receives all the messages. `BookingsController` turns the HTTP request into one call and the result into a
status. `BookingRequestService` coordinates and decides nothing that another object knows: it does not choose the
room, check the rules of the stay or write SQL. `BookingService` keeps the checks and the save of every public booking,
so the booking at once and the verified booking cannot drift apart. The repositories hold the queries, `UnitOfWork`
the transaction and the lock, `SmtpMailSender` the mail protocol, `JwtGuestTokenIssuer` the token, and the domain
objects their own invariants. The two locks have one job each: `booking_request` orders the counts and writes of the
mails, and `public_booking` orders the counts and the choice of the room of the public bookings.

---

[OC-002]: ./oc.md
[SSD-002]: ./ssd.md
[UC-011]: ./uc.md
[DICT-001]: ../../dictionary.md
[SA-001]: ../../stakeholder-analysis.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[739a25b]: https://github.com/NF-Hotel/NFHotel/commit/739a25b00890c77fe75ba0bb02fcd64a00548089
