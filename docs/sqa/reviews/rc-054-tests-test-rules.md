# RC-054 Review of the tests against the test rules

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-054 |
| CrossReference | [MIL-012], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [1d120c8] |

---

## Artifact Under Review

- Instance reviewed: the four test projects of `tests/` after tasks 2 to 8 of [MIL-012]: three-part test names, the `Integration` folder, unit tests for `BookingService`, `GuestService` and `RoomService`, integration tests of the conflict paths, named regions in long classes, and the architecture tests that keep these rules
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln` (499 total: Domain 285, Application 148, Infrastructure 54, Proxmox 12; 3 skipped), `dotnet test --filter-not-namespace "*Integration*"` for the project that mixes both kinds

## Go / No-Go criteria of MIL-012

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The test plan states, for each unit of behaviour, the kinds that apply and those that do not, with a reason | Pass | Section "Test plan" of MIL-012, covering the services, the repositories and unit of work, the holiday client, the domain and the exploratory session |
| 2 | Every public method of the three services has at least one positive and one negative test | Pass | 9 methods of `BookingService`, 6 of `GuestService`, 17 of `RoomService`. Where a method has no failing input (`GetAllAsync`, `GetFilterOptionsAsync`, `SearchAsync`), the negative test is a failing query or a null argument that must not be swallowed |
| 3 | Every test method name has three parts, checked by an architecture test | Pass | `TestRulesTests.TestMethods_InEveryTestProject_HaveThreePartNames`; shown to fail on a deliberate two-part name. 35 methods were renamed; the milestone text said 36, a count that included a helper |
| 4 | No test sleeps, reads the system clock or calls the network | Pass with a note | `TestRulesTests.Tests_InEveryTestProject_DoNotSleepOrReadTheSystemClock` fails on `Thread.Sleep`, `Task.Delay` and the system clock; shown to fail on a deliberate one. Network: the holiday client tests use a stub handler. The live tests of `NFHotel.Proxmox.Tests` are integration tests of a Proxmox server by design; they are skipped without `PVE_HOST` (3 skipped) |
| 5 | The overlap, unique-key and concurrency-token conflicts each have an integration test and none waits on a timer | Pass | `ConflictPathTests`: 7 tests against PostgreSQL 16. The guards are the exclusion constraint, the unique index and the `xmin` token, so each exception can only come from them. The parallel race uses a latch and was run 5 times with the same result |
| 6 | Unit and PostgreSQL tests are in separate folders; the unit tests run without Docker | Pass | `Integration/Persistence/` holds the 9 PostgreSQL tests; `--filter-not-namespace "*Integration*"` runs the other 45 in 1.7 s and starts no container |
| 7 | `src/` unchanged by this milestone; 0 build warnings; format clean; all tests pass | Pass | No commit of this milestone touches `src/`. One file of `src/` (`Guest.cs`) carries an uncommitted edit of the owner that is not part of this milestone. 499 tests, 0 failed, 3 skipped (the live Proxmox tests, as before) |
| 8 | Every test class over 150 lines has at least two named regions | Pass | `TestRulesTests.LongTestClasses_InEveryTestProject_HaveNamedRegions`; 11 classes were grouped and every new class has regions |
| 9 | This review has the verdict `Go`, with the exploratory session note | Pass | Below |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | Test classes `PascalCase`, private fields `_camelCase`; 35 test methods renamed to `Method_Condition_Expected`; an architecture test fails on a two-part name |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The in-memory fakes in `ServiceWorld.cs` are test support, grouped in one file by design; every test class is alone in its file |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; 0 build warnings |
| 4 | Nullable enabled; `!` justified | Pass | `!` appears only where a test passes `null!` on purpose to prove the guard, and on `GetProperty(...)!` for the key property that must exist |
| 5 | `IDisposable` disposed | Pass | Every context in the integration tests is in `await using` |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | None |
| 7 | Specific exceptions, no empty catch | Pass | The race helper catches only `BookingOverlapConflictException` |
| 8 | Public types have XML documentation | Pass | Every new test has a summary line that names its kind |
| 9 | Properties, not public fields; immutability | Pass | Fakes expose get-only collections |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | Every awaited call in a test passes `TestContext.Current.CancellationToken` (analyzer xUnit1051 is an error) |
| 12 | Tests cover new behaviour, named `Method_Condition_Expected`, no dependence on order, time or network | Pass | 140 tests added (359 to 499); no sleep, no system clock; the clock is a fake. See the Go criteria |
| 13 | Classes trace to the Design Class Diagram | N-A | No product class changed |
| 14 | Each piece of knowledge is defined once | Pass | The in-memory ports and the fake clock live once in `ServiceWorld.cs`; the repository root search lives once in `RepositoryRoot` |
| 15 | References point inward, no cycles | Pass | `NFHotel.Application.Tests` references only Application and what it brings; no project reference was added |
| 16 | SOLID | N-A | Test code |
| 17 | Layer folders | Pass | Service tests in `Services/`, architecture tests in `Architecture/`, PostgreSQL tests in `Integration/Persistence/` |
| 18 | File and folder names | Pass | English, no space |

## Domain audit (task 8)

Every public method of the entities and value objects of `NFHotel.Domain` is referenced by at least one Domain test (a search of every method name found none without a test). The 285 Domain tests already cover positive, negative, boundary, equivalence, state-transition and edge kinds. No Domain test was added; the plan said a gap found would be added, and none was found.

## Exploratory session note

- **Charter:** explore the booking flow through the services, with a fake clock, to find behaviour that surprises a reader of the business rules (rule 17, kind "Exploratory"). Time box: one session of about 30 minutes.
- **Tester and date:** Claude (AI), 2026-10-16.
- **Scenarios tried (7):** early check-out then letting the rest of the stay; check-out later on the day of arrival; rescheduling a checked-in booking; cancelling twice; checking in to a room that went out of service after booking; an overview with the end before the start; two back-to-back bookings and a third that spans both.
- **Behaved as the rules say (5):** the first, second, third, fourth and seventh scenarios. They are kept as scripted tests in `BookingServiceTests`, region "Scenarios from exploratory testing".
- **Findings that the owner must decide (2),** not changed here because this milestone leaves `src/` alone, and not pinned by a test, because a test would fix the questionable behaviour:
  1. `GetOverviewAsync` accepts a period whose end is before its start and returns an empty list, while `GetAvailableRoomsAsync` rejects the same input with `booking.end_not_after_start`. Decide whether the overview should reject it too.
  2. `CheckInAsync` succeeds for a confirmed booking whose room was taken out of service after the booking was made; only creating and rescheduling check that the room is bookable (BR-110). Decide whether check-in should refuse such a room.

## Overall Verdict

Go — every applicable criterion of QC-CS-001 and all nine Go criteria of MIL-012 pass. The 3 skipped tests are the live Proxmox tests, as before. Coverage on the command line is still not measured (see RC-050).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide finding 1 of the exploratory session (overview with an inverted period) and, if wanted, plan a change to `src/` | S01 | Open |
| Decide finding 2 of the exploratory session (check-in to a room out of service) and, if wanted, plan a change to `src/` | S01 | Open |

---

[MIL-012]: ../../milestones/mil-012-test-kinds.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[1d120c8]: https://github.com/NF-Hotel/NFHotel/commit/1d120c80c459c38400244bd295e6df35502ce65a
