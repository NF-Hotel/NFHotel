# MIL-013 Booking guards: inverted overview period and check-in to a blocked room

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-013 |
| CrossReference | [BC-001], [MIL-012], [UC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-056 Go | pending |

---

## Purpose

Decide whether the two findings of the exploratory session in [RC-054] can be
closed with two small guards in `BookingService`, each with a user-visible error
and a test that fails without the guard. The findings are:

1. `GetOverviewAsync` accepts a period whose end is before its start and returns
   an empty list, while `GetAvailableRoomsAsync` rejects the same input.
2. `CheckInAsync` succeeds for a confirmed booking whose room was taken out of
   service or sent to maintenance after the booking was made; only creating and
   rescheduling check that the room is bookable (BR-110).

## Decisions

| # | Decision | Why |
| --- | --- | --- |
| 1 | An overview period whose end is **before** its start is rejected with the new code `booking.overview_period_invalid`. A period whose end **equals** its start is allowed: it is the view of one day | The booking period needs a stay of at least one night, an overview does not; reusing `end_not_after_start` would forbid the one-day view |
| 2 | Checking in requires that the room of the booking is bookable (status `Available`, BR-110). Otherwise `CheckInAsync` returns the existing code `booking.room_not_bookable` and changes nothing | The same code and text as creating and rescheduling say the same thing to the user. The cleaning state stays out of it, a room that needs cleaning may be checked in to (B-04) |
| 3 | The `CanCheckIn` flag of the booking list and details follows the room: it is false while the room is not bookable, so the screen does not offer an action that the service refuses | The read model already carries the room status |
| 4 | Check-out, cancel and confirm are not changed | The findings concern only the two actions above |

## Deliverable

- `GetOverviewAsync` and `CheckInAsync` with the two guards, and the error code `booking.overview_period_invalid` with its screen text in `ErrorMessages`.
- `CanCheckIn` of the booking list item and the booking details false for a room that is not bookable.
- Tests for each guard in `BookingServiceTests` (positive, negative and boundary: the one-day overview; a room under maintenance; a room that needs cleaning but is bookable) and a mapping test for `CanCheckIn`.
- UC-001 (Manage Bookings) with the two rules in its extensions and business rules table, reviewed again.
- Unchanged: creating, rescheduling, check-out, cancel, confirm, the domain entities and the database.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `GetOverviewAsync` with the end one day before the start returns `booking.overview_period_invalid`, and no query is made | Yes | No |
| 2 | `GetOverviewAsync` with the end equal to the start succeeds (boundary) | Yes | No |
| 3 | `CheckInAsync` for a confirmed booking whose room is out of service, and one whose room is in maintenance, returns `booking.room_not_bookable`; the booking stays confirmed and nothing is saved | Yes | No |
| 4 | `CheckInAsync` succeeds when the room is bookable and needs cleaning (B-04) | Yes | No |
| 5 | `CanCheckIn` is false in the list and the details when the room is not bookable, true otherwise; checked by a mapping test | Yes | No |
| 6 | Each new test fails when its guard is removed (shown once by commenting the guard out) | Yes | No |
| 7 | `ErrorMessages` has a text for the new code, and an existing test that every code has a text still passes | Yes | No |
| 8 | UC-001 states both rules and has a `Go` review against `qc-use-case` | Yes | No |
| 9 | `dotnet build` has 0 warnings, `dotnet format --verify-no-changes` is clean, all tests pass, and a review against `qc-programming-csharp` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-012 | The service tests and the in-memory ports that the new tests use |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 16 of an amended BC-001: the booking service refuses an inverted overview period and a check-in to a room that cannot be let | All tasks |
| Success criterion 19 | Criteria 1 to 6 |
| US-001.06 Manage bookings | Tasks 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-23 — five days after MIL-012 (2026-12-18); two guards, one mapping and one use case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 16, the scope item, success criterion 19 and the MIL-013 schedule row; review again. | No | |
| 2 | Write the tests first | In `BookingServiceTests`: the inverted and the one-day overview, the check-in to a room out of service, in maintenance and needing cleaning; a mapping test for `CanCheckIn`. They fail before the change. | No | |
| 3 | Guard the overview period | `GetOverviewAsync` rejects an end before the start with `booking.overview_period_invalid`; add the code and its screen text. | No | |
| 4 | Guard the check-in | `CheckInAsync` checks that the room is bookable (code `booking.room_not_bookable`); the transition and the room check share the helper that creating uses. | No | |
| 5 | Make the screen follow the room | `CanCheckIn` of the list item and the details is false for a room that is not bookable. | No | |
| 6 | Update the use case | UC-001: the two rules in the extensions and the business rules; review against `qc-use-case`. | Yes | UC-001 |
| 7 | Check and review | Show each test failing without its guard; build, format and test; record an RC against `qc-programming-csharp`. | No | |

---

[BC-001]: ../business-case.md
[MIL-012]: ./mil-012-test-kinds.md
[UC-001]: ../use-cases/uc-001/uc.md
[RC-054]: ../sqa/reviews/rc-054-tests-test-rules.md
