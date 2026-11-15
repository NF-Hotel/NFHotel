# OC-001 View and Change Own Booking

## Metadata
| Key | Value |
| --- | --- |
| ID | OC-001 |
| CrossReference | [SSD-001], [DM-004], [DICT-001], [UC-010], [ADR-0047] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 3 of MIL-018, written by the AI<br>Go in RC-389; accepted by the AI as stand-in for S01 | pending |

---

The contracts use the IT terms of [DICT-001]. A `GuestSession` (the PO term Guest sign-in) is the guest's sign-in of
[SSD-001]: the booking it is for and the moment it ends. It is not a concept of [DM-004] and is not stored: the API signs it as a token, and the
web application keeps it in the guest cookie ([ADR-0047], decisions 2 and 6).

## Contract CO1: signIn

- **Operation:** `signIn(bookingNumber: BookingNumber, email: Email): BookingDetails`
- **Cross References:** [SSD-001] message 1 (use case steps 1 and 2); [DM-004] concepts Booking, Guest.
- **Preconditions:**
  - A `Booking` b with the `BookingNumber` exists.
  - The `Guest` of b has the `Email`, compared without letter case and surrounding spaces.
  - The `BookingStatus` of b is not cancelled.
  - Fewer than five failed attempts for the `BookingNumber` were counted in the last 15 minutes.
- **Postconditions:**
  - A `GuestSession` s was created.
  - s was associated with b.
  - s.ExpiresAt became the time of the sign-in plus two hours.
  - The failed attempts counted for the `BookingNumber` were cleared.
- **Exceptions:**
  - No `Booking` has the `BookingNumber`, or the `Email` is not the one of the `Guest` of b: the one refusal
    `api.guest_unauthorized`; no `GuestSession` is created, and the failed attempt is counted for the
    `BookingNumber`.
  - b is cancelled: the same refusal; no `GuestSession` is created, and the attempt stays counted for the
    `BookingNumber`, as for a wrong `Email`.
  - The `BookingNumber` cannot be read: the same refusal, given by the website without a call to the API; nothing is
    counted.
  - Five failed attempts for the `BookingNumber` within 15 minutes: `booking.too_many_lookups`; nothing is created.

## Contract CO2: changeDates

- **Operation:** `changeDates(startDate: DateOnly, endDate: DateOnly): BookingDetails`
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
  - The booking asked for is not b, or another `Room` is asked for: `api.forbidden`; nothing changes.
  - b is not pending or confirmed: `booking.reschedule.invalid_state`; nothing changes.
  - The dates are missing, `endDate` is not after `startDate`, or `startDate` is before today: the date errors of
    the booking; nothing changes.
  - The `Room` is claimed for a night of the new period: `booking.room_not_available`; nothing changes.

## Contract CO3: cancelBooking

- **Operation:** `cancelBooking(): BookingDetails`
- **Cross References:** [SSD-001] message 3 (use case steps 5 and 6); [DM-004] concept Booking.
- **Preconditions:**
  - A `GuestSession` s exists, has not ended, and is associated with the `Booking` b.
  - The `BookingStatus` of b is pending or confirmed.
- **Postconditions:**
  - b.BookingStatus became cancelled; b is kept.
- **Exceptions:**
  - s has ended or is not valid: `api.guest_unauthorized`; nothing changes.
  - The booking asked for is not b: `api.forbidden`; nothing changes.
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
