# UC-001 Manage Bookings

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-001 |
| CrossReference | [UCD-001], [US-001], [SA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Initial version, derived from the rule ledger | [a29c54a] |

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
3. The system stores the booking as pending and gives it a booking number.
4. Hotel staff confirm the booking.
5. When the guest arrives, hotel staff check the guest in.
6. When the guest leaves, hotel staff check the guest out.

### Extensions (Alternative / Exception Flows)

- 2a. The dates are invalid or the room is booked for any night of the period:
  1. The system reports every problem at once and stores nothing.
- 4a. The guest has not paid in advance:
  1. Hotel staff skip the confirmation and check the guest in directly from pending.
- 5a. The booking is pending or confirmed and the guest does not come:
  1. Hotel staff cancel the booking; it stays in the list with the status cancelled.
- 5b. The guest wants other nights or another room while the booking is pending or confirmed:
  1. Hotel staff move the booking; the same checks as in step 2 apply, and the booking does not block itself.
- 6a. The guest leaves before the departure date:
  1. The system records the actual check-out date, and the room is free again from that date.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 1 | A new booking is always pending (BR-01); arrival and departure date follow BR-41 to BR-48 of the ledger |
| 2 | The period is half-open: a guest leaving on a day and a guest arriving on that day do not share the room (BR-19, BR-39, BR-N-07) |
| 4 | Only a pending booking can be confirmed (BR-02, BR-03, BR-05) |
| 5 | Check-in is allowed for a pending or confirmed booking from the arrival date, once (BR-06, BR-07) |
| 6 | Check-out is allowed only for a checked-in booking, once (BR-09, BR-10) |
| 5a | Only a pending or confirmed booking can be cancelled; cancelling never deletes (BR-11, BR-13) |
| 5b | Only a pending or confirmed booking can be moved (BR-14, BR-17 to BR-20) |

### Open Issues

- The ledger contradicts itself on whether a checked-out booking blocks the room (BR-19 against BR-39); BR-N-07 resolves it by ending the occupancy at the actual check-out date. S01 and S04 are to confirm.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[a29c54a]: https://github.com/NF-Hotel/NFHotel/commit/a29c54a0e6cc95b2b627748aa841ee24a399c15d
