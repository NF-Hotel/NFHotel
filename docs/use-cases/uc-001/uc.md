# UC-001 Manage Bookings

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-001 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-011] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | A search for free rooms is limited per caller address, and hotel staff may see its refusal; the limits of the public site on booking requests per caller address and on coming bookings per email do not hold for hotel staff (MIL-087, ADR-0051)<br>Go in RC-427; accepted by the AI as stand-in for S01 in autonomous mode | [a574bf6] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Rule 1: the coming bookings of the public site are counted per mailbox; hotel staff book without a booking request and without a mail, and a booking request of the website is not a booking and holds no room (MIL-088, UC-011, ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Hotel staff
- **Stakeholders and Interests:**
  - S05 — Hotel staff: every guest has a room for the nights booked, and only allowed actions are offered.
  - S01 — Team 2: the rules are written down once and can be checked.
  - S04 — Valdamar: the rules can be traced to the rule ledger.
- **Preconditions:** At least one room and one guest exist.
- **Postconditions (success guarantee):** The booking is stored with its new status and period; no room is booked twice for the same night; a cancelled booking is kept.

### Main Success Scenario

1. Hotel staff ask for a booking for a guest, a room and an arrival and departure date.
2. The system checks the dates and that the room is free for every night of the period.
3. The system stores the booking as pending and gives it a booking reference.
4. Hotel staff confirm the booking.
5. When the guest arrives, hotel staff check the guest in.
6. When the guest leaves, hotel staff check the guest out.

### Extensions (Alternative / Exception Flows)

- 1a. Hotel staff look for the free rooms of a period more than 60 times in a minute from one address:
  1. The system refuses the search with the text of a refusal by a limit, shows no rooms and stores nothing. Hotel staff search again when the minute has passed.
- 2a. The dates are invalid or the room is booked for any night of the period:
  1. The system reports every problem at once and stores nothing.
- 4a. The guest has not paid in advance:
  1. Hotel staff skip the confirmation and check the guest in directly from pending.
- 5a. The booking is pending or confirmed and the guest does not come:
  1. Hotel staff cancel the booking; it stays in the list with the status cancelled.
- 5b. The guest wants other nights or another room while the booking is pending or confirmed:
  1. Hotel staff move the booking; the same checks as in step 2 apply, and the booking does not block itself.
- 5c. The room of the booking is out of service or in maintenance when the guest arrives:
  1. The system refuses the check-in and tells hotel staff that the room cannot be let.
  2. Hotel staff move the booking to another room as in 5b and check the guest in there. A room that only needs cleaning can be checked in to.
- 6a. The guest leaves before the departure date:
  1. The system records the actual check-out date, and the room is free again from that date.
- *a. At any time, hotel staff look at the bookings of a period whose end is before its start:
  1. The system refuses the period and shows nothing. A period of one day, with the same start and end, is allowed.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 1 | A new booking is always pending (BR-01); arrival and departure date follow BR-41 to BR-48 of the ledger, and the stay ends at most five years after today (BR-N-08). Hotel staff may book more than 30 nights: that limit holds only on the public site (BR-C-07). The details of a new guest follow the table "Validation rules of the booking flow" of the ledger (MIL-086, ADR-0050). The limits of the public site on booking requests per caller address and on coming bookings per mailbox neither count nor limit a booking by hotel staff (the table "Limits of the booking flow" of the ledger; ADR-0051, decisions 2 and 3; ADR-0052, decision 6). Hotel staff book at once, without a booking request and without a mail. A booking request of the public site ([UC-011]) is not a booking: the back office does not show it, it holds no room, and a booking by hotel staff may take the last free room of the size that a request asked for (ADR-0052, decisions 1 and 9) |
| 1a | A search for free rooms, on the public site or in the back office, is limited to 60 in a minute per address of the caller: the address from which the search reached the site, which the caller cannot set. A refused search shows the one text of a refusal by a limit of the booking flow (the table "Limits of the booking flow" of the ledger; ADR-0051, decisions 2 and 4) |
| 2 | The period is half-open: a guest leaving on a day and a guest arriving on that day do not share the room (BR-19, BR-39, BR-N-07) |
| 4 | Only a pending booking can be confirmed (BR-02, BR-03, BR-05) |
| 5 | Check-in is allowed for a pending or confirmed booking from the arrival date, once (BR-06, BR-07) |
| 6 | Check-out is allowed only for a checked-in booking, once (BR-09, BR-10) |
| 5a | Only a pending or confirmed booking can be cancelled; cancelling never deletes (BR-11, BR-13) |
| 5b | Only a pending or confirmed booking can be moved (BR-14, BR-17 to BR-20); the new stay ends at most five years after today (BR-N-08) |
| 5c | A guest is checked in only to a room that can be let; whether the room is clean does not matter (BR-110, B-04) |
| *a | The end of an overview period is not before its start; a period of one day is allowed |

### Open Issues

- The ledger contradicts itself on whether a checked-out booking blocks the room (BR-19 against BR-39); BR-N-07 resolves it by ending the occupancy at the actual check-out date. S01 and S04 are to confirm.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-011]: ../uc-011/uc.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
