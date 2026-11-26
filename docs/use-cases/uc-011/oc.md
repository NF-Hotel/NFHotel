# OC-002 Book a Room and Verify It by Email

## Metadata
| Key | Value |
| --- | --- |
| ID | OC-002 |
| CrossReference | [SSD-002], [DM-004], [DICT-001], [SD-001], [UC-011], [OC-001], [UC-010], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0052], [ADR-0053] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; task 3 of MIL-088, written by the AI: one contract for each system operation of SSD-002, the booking request, its verification and its clean-up (ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | [739a25b] |
| 2026-11-21 | Accepted | mikemike | AI as stand-in for S01 | The limit of the coming public bookings per mailbox is named `mailbox`, as the events and the code name it since MIL-089 (decision 6 of ADR-0051, RC-439)<br>Go in the delta review of RC-439; accepted by the AI as stand-in for S01 in autonomous mode | [00d609b] |

---

The contracts use the IT terms of [DICT-001]. A `BookingRequest` (the PO term Booking request) is the concept of
[DM-004]. The other terms are not concepts of [DM-004], because they exist only because the software does:

- `GuestFields` are the details of a guest as the guest types them: `FirstName`, `LastName`, `Email`, `PhoneNumber`,
  `Country` and the optional `PassportNumber`.
- The `Mailbox` of an email is the mailbox it reaches: the email without letter case and without a `+` and what
  follows it before the `@`, and for `gmail.com` and `googlemail.com` also without the dots before the `@` and with
  the domain `gmail.com` ([ADR-0052], decision 6). The key of a `Mailbox` is the SHA-256 hash of that form; the system
  keeps the key, never the form.
- A token is the secret part of the link of a `BookingRequest`: 256 random bits, written as 43 characters after the
  `#` of the link. A `BookingRequest` keeps only the SHA-256 hash of its token (`TokenHash`), so the token of a request
  is known only to the mailbox it was sent to ([ADR-0052], decision 2).
- A `SentMail` is the record that the system sent a mail to a `Mailbox`: the key of the `Mailbox`, the kind of the
  mail (`VerificationLink` or `BookingReceipt`) and the time (`SentAt`), never the email. The limits of the mails per
  `Mailbox` and the fuse of 400 mails in 24 hours count `SentMail`s, which are kept for 24 hours ([ADR-0052],
  decision 6; [ADR-0053], decision 6).
- A `BookingRequestReceipt` is what the guest is shown of a `BookingRequest`: the time its link lapses (`ExpiresAt`).
  A `BookingReceipt` and a `GuestSession` are those of [OC-001].
- A booking is coming when it is not cancelled and its `EndDate` is today or later, and made on the public site when
  its `MadeOnPublicSite` is set.

The counts per caller address (searches, booking requests) are those of [ADR-0051]: the address from which the
request reached the site, kept in the memory of the API for the window of the limit. `now` is the time and `today` the
day on the clock of the hotel. The verification by email is switched on or off for the whole site ([ADR-0052],
decision 8).

## Contract CO1: findFreeRoomSizes

- **Operation:** `findFreeRoomSizes(startDate: DateOnly, endDate: DateOnly, guests: int): RoomSize[]`
- **Cross References:** [SSD-002] message 1 (use case steps 1 and 2); [DM-004] concepts Room, Booking.
- **Preconditions:**
  - `startDate` is not before today, `endDate` is after `startDate` and at most five years after today, and the stay
    has at most 30 nights; `guests` is from 1 to 4 (BR-C-07, BR-N-08).
  - Fewer than 60 searches came from the caller's address in the last minute.
- **Postconditions:**
  - No instance was created, deleted or changed. The `RoomSize`s returned are those of the `Room`s whose `Capacity`
    is at least `guests` and that no `Booking` claims for a night from `startDate` to the night before `endDate`.
  - The search was counted for the caller's address.
- **Exceptions:**
  - A date breaks a rule: `booking.start_date_in_past`, `date_range.end_not_after_start`,
    `booking.end_date_too_far_ahead` or `booking.stay_too_long`; no room size is returned.
  - 60 searches from the caller's address within a minute: `booking.too_many_requests` with the seconds to wait; no
    room size is returned, and the search is not counted.

## Contract CO2: askForBooking

- **Operation:** `askForBooking(startDate: DateOnly, endDate: DateOnly, roomSize: RoomSize, guests: int,
  guestDetails: GuestFields): BookingRequestReceipt`
- **Cross References:** [SSD-002] message 2 (use case steps 3 and 4, and extension 5a); [DM-004] concepts Booking
  request, Room, Booking, Guest.
- **Preconditions:**
  - The verification by email is switched on.
  - The dates, `roomSize`, `guests` and every field of `guestDetails` meet the table "Validation rules of the booking
    flow" of the rule ledger and the rules of CO1 ([ADR-0050]).
  - A `Room` of `roomSize` whose `Capacity` is at least `guests` is claimed by no `Booking` for a night from
    `startDate` to the night before `endDate`.
  - Fewer than 3 coming `Booking`s made on the public site have a `Guest` whose `Email` has the `Mailbox` mb of
    `guestDetails.Email`.
  - Fewer than 3 `SentMail`s of the kind `VerificationLink` for mb were made in the last hour, and fewer than 5 in the
    last day.
  - Fewer than 400 `SentMail`s were made in the last 24 hours, for every `Mailbox` and of both kinds.
  - Fewer than 5 booking requests came from the caller's address in the last hour, and fewer than 20 in the last day.
  - The mail relay takes the mail.
- **Postconditions:**
  - If no `BookingRequest` of mb existed, a `BookingRequest` r was created; otherwise the `BookingRequest` r of mb was
    renewed, and the token of its earlier link no longer matches it.
  - r.StartDate, r.EndDate, r.RoomSize and r.Guests became `startDate`, `endDate`, `roomSize` and `guests`.
  - r.FirstName, r.LastName, r.Email, r.PhoneNumber, r.Country and r.PassportNumber became the fields of
    `guestDetails` in their normal form; r.PassportNumber is kept encrypted, as the passport number of a `Guest` is.
  - r.EmailKey became the key of mb, and r.TokenHash the hash of a new token.
  - r.CreatedAt became `now`, and r.ExpiresAt became `now` plus 30 minutes.
  - A `SentMail` s was created: s.EmailKey became the key of mb, s.Kind became `VerificationLink`, and s.SentAt became
    `now`.
  - The booking request was counted for the caller's address.
  - No `Booking` and no `Guest` was created, and no `Room` is held for r.
- **Exceptions:**
  - The verification by email is switched off: `booking.not_found`; nothing is created or changed.
  - A field, a date or the party breaks a rule: the codes of the broken rules; nothing is created or changed, no mail
    is sent, and the count of the caller's address is given back.
  - No free `Room` of `roomSize` sleeps the party: `booking.room_type_not_available`; nothing is created or changed,
    and no mail is sent.
  - 3 coming `Booking`s made on the public site have the `Mailbox` mb: `booking.too_many_requests`, the limit
    `mailbox`; nothing is created or changed, and no mail is sent.
  - 3 `SentMail`s of the kind `VerificationLink` for mb in the last hour, or 5 in the last day:
    `booking.too_many_mails`, the limit `verification_mail`; nothing is created or changed, so an open
    `BookingRequest` of mb keeps its link.
  - 400 `SentMail`s in the last 24 hours: `booking.mail_paused`; nothing is created or changed.
  - 5 booking requests from the caller's address in the last hour, or 20 in the last day: `booking.too_many_requests`
    with the seconds to wait; nothing is created or changed, and the request is not counted.
  - The mail relay does not take the mail: `booking.mail_not_sent`; r and s were deleted, so mb has no open
    `BookingRequest`: one that r renewed is gone too.

Only a refusal by a rule of a field, a date or the party gives the count of the caller's address back. The other
refusals leave the booking request counted, except the refusal by the limit per address itself, which counts nothing
(the table "Limits of the booking flow" of the rule ledger).

The mail of the request goes to `guestDetails.Email` with the link of the new token, the stay, the room size, the
number of guests and the time the link lapses, and nothing else that the guest typed ([ADR-0052], decision 4). It is
an output of the operation, not a change of state.

## Contract CO3: openLink

- **Operation:** `openLink(): BookingVerify`, the page of the link, which asks the guest to verify the booking
- **Cross References:** [SSD-002] message 3 (use case steps 5 and 6); no concept of [DM-004].
- **Preconditions:**
  - None: every link, a lapsed, used or unknown one included, opens the same page.
- **Postconditions:**
  - No instance was created, deleted or changed. The token is not sent to the system: it stays after the `#` of the
    link in the guest's browser ([ADR-0052], decision 3).
- **Exceptions:**
  - None. A link that does not work is refused by verifyBooking (CO4); the page asks for script, without which the
    token cannot be sent.

## Contract CO4: verifyBooking

- **Operation:** `verifyBooking(token: string): BookingReceipt`
- **Cross References:** [SSD-002] message 4 (use case steps 7 to 9); [DM-004] concepts Booking request, Booking,
  Guest, Room; its design is [SD-001].
- **Preconditions:**
  - The verification by email is switched on.
  - A `BookingRequest` r exists whose `TokenHash` is the hash of `token`, and r.ExpiresAt is after `now`.
  - The stay of r still meets the rules of the public site on `today`.
  - A `Room` of r.RoomSize whose `Capacity` is at least r.Guests is claimed by no `Booking` for a night from
    r.StartDate to the night before r.EndDate.
  - Fewer than 3 coming `Booking`s made on the public site have a `Guest` whose `Email` has the `Mailbox` of r.Email.
- **Postconditions:**
  - r was deleted.
  - A `Guest` g was created; g.FirstName, g.LastName, g.Email, g.PhoneNumber, g.Country and g.PassportNumber became
    those of r.
  - A `Booking` b was created; b.BookingReference became a new random reference ([ADR-0049]), b.StartDate and
    b.EndDate became r.StartDate and r.EndDate, b.BookingStatus became pending, and b.MadeOnPublicSite was set.
  - b was associated with g, and with a `Room` of r.RoomSize whose `Capacity` is at least r.Guests and that no other
    `Booking` claims for a night of the stay. g and b were stored together.
  - A `GuestSession` gs was created and associated with b; gs.ExpiresAt became `now` plus two hours ([ADR-0047]).
  - A `SentMail` s was created for the `Mailbox` of r.Email, with s.Kind `BookingReceipt` and s.SentAt `now`, unless
    400 `SentMail`s were made in the last 24 hours, the mail relay did not take the mail, or the sending failed in
    another way before s was stored.
- **Exceptions:**
  - The verification by email is switched off: `booking.not_found`; nothing is created, changed or deleted.
  - `token` is missing or longer than 64 characters: `api.invalid_request`; nothing changes.
  - No `BookingRequest` has the hash of `token`, because the token is unknown, not a token at all, or was used, or r
    has lapsed: the one refusal `booking.verification_link_invalid`; nothing changes.
  - The stay of r breaks a rule of the public site on `today`, because its arrival has passed since the request: the
    code of the rule; r was deleted all the same, and no `Guest` and no `Booking` was created.
  - No free `Room` of r.RoomSize sleeps r.Guests, also when bookings by staff took the next free rooms, up to five,
    at the moment of the save: `booking.room_type_not_available`; r was deleted all the same, and no `Guest` and no
    `Booking` was created.
  - 3 coming `Booking`s made on the public site have the `Mailbox` of r.Email: `booking.too_many_requests`, the limit
    `mailbox`; r was deleted all the same, and no `Guest` and no `Booking` was created.
  - Two verifications with the same `token` at once: one gets the postconditions, the other
    `booking.verification_link_invalid`. Verifications of other requests for the last free room of a size at once:
    one gets the room, the others `booking.room_type_not_available`.
  - The mail relay does not take the mail with the reference, or 400 `SentMail`s were made in the last 24 hours: g,
    b and gs stand; no `SentMail` is kept, and no mail goes.
  - The sending of the mail with the reference fails in another way, its record in the database included: g, b and
    gs stand, and the answer is the receipt; a `SentMail` stored before the failure stays until it is 24 hours old
    ([ADR-0052], decision 5).

The mail with the reference goes to r.Email with b.BookingReference, the stay, the room size and the status
([ADR-0052], decision 5); it is an output of the operation. As every booking does, b keeps the price per night of its
room size on the arrival date for the market analysis (MIL-055), which is not a concept of [DM-004].

## Contract CO5: removeLapsedRequests

- **Operation:** `removeLapsedRequests(): (int requests, int mails)`, the numbers of `BookingRequest`s and
  `SentMail`s deleted
- **Cross References:** [SSD-002] message 5 (use case extension 5b); [DM-004] concept Booking request.
- **Preconditions:**
  - None. Time starts the operation: every ten minutes, and once when the API starts.
- **Postconditions:**
  - Every `BookingRequest` whose `ExpiresAt` is `now` or earlier was deleted, with its personal data.
  - Every `SentMail` whose `SentAt` is 24 hours or more before `now` was deleted.
- **Exceptions:**
  - The run fails, for example because the database cannot be reached: what the run had not deleted stays; the
    failure is written to the log of the API, and the next run deletes what has lapsed by then.

## Contract CO6: bookAtOnce

- **Operation:** `bookAtOnce(startDate: DateOnly, endDate: DateOnly, roomSize: RoomSize, guests: int,
  guestDetails: GuestFields): BookingReceipt`
- **Cross References:** [SSD-002] message 6 (use case extension *a); [DM-004] concepts Booking, Guest, Room.
- **Preconditions:**
  - The verification by email is switched off.
  - The preconditions of CO2 on the dates, `roomSize`, `guests`, `guestDetails`, a free `Room`, the coming `Booking`s
    of the `Mailbox` and the booking requests of the caller's address.
- **Postconditions:**
  - A `Guest` g was created with the fields of `guestDetails` in their normal form.
  - A `Booking` b was created as in CO4: a new `BookingReference`, the dates, pending, `MadeOnPublicSite` set; b was
    associated with g and with a free `Room` of `roomSize` whose `Capacity` is at least `guests`. g and b were stored
    together.
  - A `GuestSession` gs was created for b and is offered to the guest; it is associated with the guest's browser only
    when the guest takes it (extension 1a of [UC-010]).
  - The booking request was counted for the caller's address. No `BookingRequest` and no `SentMail` was created.
- **Exceptions:**
  - The verification by email is switched on: `booking.verification_required`; nothing is created.
  - The exceptions of CO2 on the rules, the free `Room`, the coming `Booking`s of the `Mailbox` and the caller's
    address, with the same codes; nothing is created. A `Room` taken by a booking of staff at the moment of the save
    makes b move on to the next free room of the size, up to five rooms, as in CO4.

---

[SSD-002]: ./ssd.md
[DM-004]: ../../domain-model.md
[DICT-001]: ../../dictionary.md
[SD-001]: ./sd.md
[UC-011]: ./uc.md
[OC-001]: ../uc-010/oc.md
[UC-010]: ../uc-010/uc.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[739a25b]: https://github.com/NF-Hotel/NFHotel/commit/739a25b00890c77fe75ba0bb02fcd64a00548089
[00d609b]: https://github.com/NF-Hotel/NFHotel/commit/00d609b1450afdb2d73df7a8d5dd9b1de2e1c8da
