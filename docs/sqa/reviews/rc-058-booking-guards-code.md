# RC-058 Review of the booking guards (MIL-013)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-058 |
| CrossReference | [MIL-013], [QC-CS-001], [RC-054] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: `BookingService` (the overview guard, the check-in precondition), `BookingMapping` (the check-in flag follows the room), `ErrorCodes` and `ErrorMessages` (the new code and its text), and the new tests in `BookingServiceTests`, `BookingMappingTests` and `ErrorCodeTextTests`
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`

## Go / No-Go criteria of MIL-013

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The overview with the end one day before the start returns `booking.overview_period_invalid` and makes no query | Pass | `GetOverviewAsync_EndOneDayBeforeStart_ReturnsOverviewPeriodInvalidWithoutQuerying` |
| 2 | The overview with the end equal to the start succeeds | Pass | `GetOverviewAsync_EndEqualsStart_ReturnsTheOneDayView` |
| 3 | Check-in to a room out of service, and to one in maintenance, returns `booking.room_not_bookable`; the booking stays confirmed and nothing is saved | Pass | `CheckInAsync_RoomNotBookable_ReturnsRoomNotBookableAndChangesNothing` for both statuses |
| 4 | Check-in succeeds when the room is bookable and needs cleaning | Pass | `CheckInAsync_RoomNeedsCleaning_ChecksIn` (B-04) |
| 5 | `CanCheckIn` is false in the list and the details when the room is not bookable, true otherwise | Pass | Four tests in `BookingMappingTests` |
| 6 | Each new test fails when its guard is removed | Pass | The tests were written first and run before the guards existed: 5 failed (`GetOverviewAsync_EndOneDayBeforeStart_...`, both cases of `CheckInAsync_RoomNotBookable_...`, `ToListItem_RoomOutOfService_CannotCheckIn`, `ToDetails_RoomInMaintenance_CannotCheckIn`), and the 5 that describe unchanged behaviour passed; all pass after |
| 7 | `ErrorMessages` has a text for the new code, and a test that every code has a text passes | Pass | The statement of the milestone that such a test existed was wrong; none did. `ErrorCodeTextTests` was added, scans `ErrorMessages.cs` for every constant of `ErrorCodes`, and passes |
| 8 | UC-001 states both rules and has a `Go` review | Pass | [RC-057] |
| 9 | 0 build warnings, format clean, all tests pass, this review `Go` | Pass | 509 tests, 0 failed, 3 skipped (the live Proxmox tests) |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `OverviewPeriodInvalid`, `RoomMustBeBookableForCheckInAsync`, `roomIsBookable`; test names have three parts (the architecture test passes) |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The new helper ends in `Async`; no file gained a type |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; 0 warnings |
| 4 | Nullable enabled; `!` justified | Pass | No `!` added to `src/` |
| 5 | `IDisposable` disposed | N-A | No disposable added |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | None |
| 7 | Specific exceptions, no empty catch | Pass | The guards return error codes as the other rule errors do; no catch added |
| 8 | Public types have XML documentation | Pass | The new constant, the new parameter and the private helper have XML documentation; the build treats a missing tag as an error |
| 9 | Properties, not public fields; immutability | N-A | Not touched |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | The precondition delegate receives the token of the call |
| 12 | Tests cover new behaviour, named `Method_Condition_Expected`, no dependence on order, time or network | Pass | 10 tests added (499 to 509): see the Go criteria |
| 13 | Classes trace to the Design Class Diagram | Pass | No class added and no signature of the domain changed |
| 14 | Each piece of knowledge is defined once | Pass | The room check reuses `CheckRoomIsBookableAsync`, which creating and rescheduling use; the bookable rule is the domain's `Room.IsBookable`, and the read path uses `RoomStatus.Available` in one private function |
| 15 | References point inward, no cycles | Pass | No reference added |
| 16 | SOLID | Pass | The precondition is passed to the transition as a delegate, so the transition code is extended, not edited for each case |
| 17 | Layer folders | Pass | No folder added |
| 18 | File and folder names | Pass | English, no space |

## Overall Verdict

Go — every applicable criterion of QC-CS-001 and all nine Go criteria of MIL-013 pass. The domain is unchanged by this milestone: no commit of it touches `src/NFHotel.Domain`. The owner's uncommitted edit of `Guest.cs` is not part of it. One behaviour changes for users: a check-in to a room that cannot be let is now refused, and the check-in button is no longer shown for it.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-013]: ../../milestones/mil-013-booking-guards.md
[RC-054]: ./rc-054-tests-test-rules.md
[RC-057]: ./rc-057-use-case-001-booking-guards.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
