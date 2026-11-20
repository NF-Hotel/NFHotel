# OC-001 View and Change Own Booking

## Metadata
| Key | Value |
| --- | --- |
| ID | OC-001 |
| CrossReference | [SSD-001], [DM-004], [DICT-001], [UC-010], [ADR-0047], [ADR-0049], [SD-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | CO1 takes the booking reference in place of the booking number and returns the receipt; failed attempts count per reference and per caller address; CO2 and CO3 name no booking, so their refusals of another booking or room are gone (MIL-085, ADR-0049)<br>Go in RC-417; accepted by the AI as stand-in for S01 in autonomous mode | [e551458] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | SD-001 in the CrossReference: the first Sequence Diagram of the project, which realizes verifyBooking of UC-011 (MIL-088); no contract changed<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | [739a25b] |

---

The contracts use the IT terms of [DICT-001]. A `GuestSession` (the PO term Guest sign-in) is the guest's sign-in of
[SSD-001]: the booking it is for and the moment it ends. It is not a concept of [DM-004] and is not stored: the API signs it as a token, and the
web application keeps it in the guest cookie ([ADR-0047], decisions 2 and 6). A `BookingReceipt` is what a guest is
shown of a `Booking`: its `BookingReference`, `StartDate`, `EndDate`, `NumberOfNights`, `RoomSize` and `BookingStatus`,
and nothing of its `Guest` ([ADR-0049], decision 7). changeDates and cancelBooking name no booking and no room: they
act on the `Booking` of the guest's `GuestSession` in its own `Room`, so no other booking or room can be asked for
([ADR-0049], decision 3).

## Contract CO1: signIn

- **Operation:** `signIn(bookingReference: BookingReference, email: Email): BookingReceipt`
- **Cross References:** [SSD-001] message 1 (use case steps 1 and 2); [DM-004] concepts Booking, Guest.
- **Preconditions:**
  - A `Booking` b with the `BookingReference` exists.
  - The `Guest` of b has the `Email`, compared without letter case and surrounding spaces.
  - The `BookingStatus` of b is not cancelled.
  - Fewer than five failed attempts for the `BookingReference`, and fewer than twenty from the caller's address, were
    counted in the last 15 minutes.
- **Postconditions:**
  - A `GuestSession` s was created.
  - s was associated with b.
  - s.ExpiresAt became the time of the sign-in plus two hours.
  - The failed attempts counted for the `BookingReference` were cleared, and the attempt counted for the caller's
    address was taken back.
- **Exceptions:**
  - No `Booking` has the `BookingReference`, or the `Email` is not the one of the `Guest` of b: the one refusal
    `api.guest_unauthorized`; no `GuestSession` is created, and the failed attempt is counted for the
    `BookingReference` and for the caller's address.
  - b is cancelled: the same refusal; no `GuestSession` is created, and the attempt stays counted for the
    `BookingReference` and for the caller's address, as for a wrong `Email`.
  - The `BookingReference` cannot be read, a booking number of the old kind included: the same refusal; no `Booking`
    is read, and the failed attempt is counted for the caller's address.
  - Five failed attempts for the `BookingReference`, or twenty from the caller's address, within 15 minutes:
    `booking.too_many_lookups`; nothing is created.

## Contract CO2: changeDates

- **Operation:** `changeDates(startDate: DateOnly, endDate: DateOnly): BookingReceipt`
- **Cross References:** [SSD-001] message 2 (use case steps 3 and 4); [DM-004] concepts Booking, Room.
- **Preconditions:**
  - A `GuestSession` s exists, has not ended, and is associated with the `Booking` b.
  - The `BookingStatus` of b is pending or confirmed.
  - `startDate` is not before today, and `endDate` is after `startDate`.
  - No other `Booking` of the `Room` of b claims a night from `startDate` to the night before `endDate`.
- **Postconditions:**
  - b.StartDate became `startDate`.
  - b.EndDate became `endDate`.
  - b stays associated with the same `Room`.
- **Exceptions:**
  - s has ended or is not valid: `api.guest_unauthorized`; nothing changes.
  - b is not pending or confirmed: `booking.reschedule.invalid_state`; nothing changes.
  - The dates are missing, `endDate` is not after `startDate`, or `startDate` is before today: the date errors of
    the booking; nothing changes.
  - The `Room` is claimed for a night of the new period: `booking.room_not_available`; nothing changes.

## Contract CO3: cancelBooking

- **Operation:** `cancelBooking(): BookingReceipt`
- **Cross References:** [SSD-001] message 3 (use case steps 5 and 6); [DM-004] concept Booking.
- **Preconditions:**
  - A `GuestSession` s exists, has not ended, and is associated with the `Booking` b.
  - The `BookingStatus` of b is pending or confirmed.
- **Postconditions:**
  - b.BookingStatus became cancelled; b is kept.
- **Exceptions:**
  - s has ended or is not valid: `api.guest_unauthorized`; nothing changes.
  - b is not pending or confirmed: `booking.cancel.invalid_state`; nothing changes.

## Contract CO4: signOut

- **Operation:** `signOut(): void`
- **Cross References:** [SSD-001] message 4 (use case steps 7 and 8); no concept of [DM-004].
- **Preconditions:**
  - None; a guest without a `GuestSession` may sign out as well.
- **Postconditions:**
  - The association between the guest's browser and the `GuestSession` s was removed.
  - A staff sign-in of the same browser is unchanged.
- **Exceptions:**
  - The request does not carry the antiforgery token of the website: refused; nothing changes.

The token of s is not revoked by signOut: a copy of it stays valid in the API until s.ExpiresAt ([ADR-0047],
Consequences).

---

[SSD-001]: ./ssd.md
[DM-004]: ../../domain-model.md
[DICT-001]: ../../dictionary.md
[UC-010]: ./uc.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[SD-001]: ../uc-011/sd.md
[e551458]: https://github.com/NF-Hotel/NFHotel/commit/e551458c167261cad75f9ffd2902fb6163a78b81
[739a25b]: https://github.com/NF-Hotel/NFHotel/commit/739a25b00890c77fe75ba0bb02fcd64a00548089
