# MIL-012 Tests follow the test rules of the coding conventions

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-012 |
| CrossReference | [BC-001], [MIL-011] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-053 Go | pending |

---

## Purpose

Decide whether the tests of NFHotel meet the test rules that framework v0.13.2
added to the `coding-conventions` skill: rule 17 (cover the kinds of behaviour
that apply, and say which kinds do not apply and why), the naming
`Method_Condition_Expected`, deterministic unit tests with fake ports and a
fake clock, and integration tests that are separate from unit tests. An audit
of the 145 tests on 2026-10-16 found four gaps, which this milestone closes
without changing the code under test.

| Gap found | Evidence |
| --- | --- |
| The use-case layer has no unit tests | `NFHotel.Application.Tests` holds only architecture tests; `BookingService`, `GuestService` and `RoomService` are exercised by no test of their own |
| 36 of 145 test names are not `Method_Condition_Expected` | For example `Conflicts_ReflectsStatus` and `Overlaps_IsSymmetric` |
| No test of the conflict paths | `ConcurrencyConflictException`, `BookingOverlapConflictException` and `UniqueConstraintViolationException` have no concurrency, race or integration test |
| Unit and integration tests are mixed, and no test plan says which kinds apply | `NFHotel.Infrastructure.Tests` mixes in-memory tests with PostgreSQL container tests |

## Deliverable

- A test plan in this document (section "Test plan"): for each unit of behaviour, the kinds of rule 17 that apply and the kinds that do not apply, each with a reason.
- Unit tests for `BookingService`, `GuestService` and `RoomService` in `NFHotel.Application.Tests`, with in-memory fakes of the ports and a fake clock (positive, negative, boundary, edge, state transition and error-handling kinds).
- Integration tests in `NFHotel.Infrastructure.Tests` for the repositories and the unit of work against PostgreSQL, including the overlap, unique-key and concurrency-token conflicts, made deterministic with a latch, not a sleep.
- The 36 test methods renamed to `Method_Condition_Expected`; test bodies unchanged.
- The PostgreSQL tests in an `Integration` folder of their project, apart from the unit tests.
- Every test class of more than 150 lines, and every new test class, split into named `#region` sectors by the kind of test (for example `#region Positive`, `#region Negative`, `#region Boundary`), as the C# rule of framework v0.13.2 asks for long classes. Test code is only moved, not changed.
- Unchanged: everything under `src/`, and the behaviour the existing tests prove.

## Test plan

| Unit of behaviour | Kinds that apply | Kinds that do not apply, and why |
| --- | --- | --- |
| `BookingService` (create, reschedule, confirm, check in and out, cancel) | Positive, negative, boundary (same-day and one-night stays), edge, state transition, error handling, equivalence partitioning | Timeout and retry: the service has no retry. Concurrency: covered at the repository, where the conflict arises |
| `GuestService` and `RoomService` | Positive, negative, boundary (field lengths), edge (Unicode names, empty optional fields), error handling | State transition for guests: a guest has no status. Timeout: none |
| Repositories and unit of work (PostgreSQL) | Integration, concurrency, race condition, error handling, regression | Boundary: the domain tests cover the field limits |
| `HolidayApiClient` | Already covered: positive, negative, timeout, cancellation, error handling | Concurrency: the client keeps no shared state |
| Domain entities and value objects | Already covered by 285 tests; this milestone adds none | A gap found during the audit of task 8 is added, not assumed |
| Exploratory | One time-boxed session of the booking flow, with a charter and a session note in the review record | Automated: by definition |

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The test plan states, for each unit of behaviour, the kinds that apply and those that do not, with a reason | Yes | No |
| 2 | Every public method of `BookingService`, `GuestService` and `RoomService` has at least one positive and one negative test | Yes | No |
| 3 | Every test method name has three parts, `Method_Condition_Expected`, checked by an architecture test | Yes | No |
| 4 | No test calls `Thread.Sleep` or `Task.Delay`, reads the system clock or calls the network; a search of `tests/` finds none | Yes | No |
| 5 | The overlap, unique-key and concurrency-token conflicts each have an integration test that fails without the guarding code, and none of them waits on a timer | Yes | No |
| 6 | Unit tests and PostgreSQL tests are in separate folders; the unit tests run without Docker | Yes | No |
| 7 | `git diff` of `src/` is empty; `dotnet build` has 0 warnings; `dotnet format --verify-no-changes` is clean; all tests pass | Yes | No |
| 8 | Every test class of more than 150 lines has at least two named `#region` sectors, and no `#region` is empty or unnamed; a source search shows it | Yes | No |
| 9 | A review record against `qc-programming-csharp` has the verdict `Go`, with the exploratory session note | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-011 | The tests run on xUnit v3, and the framework version that adds the test rules requires it |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 15 of an amended BC-001: the tests cover the kinds of behaviour of the coding conventions | All tasks |
| Success criterion 18 | Criteria 2 to 6 and 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-18 — a week after MIL-011 (2026-12-11), about 60 new tests, 36 renames and the regions of 11 classes.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 15, the scope item, success criterion 18 and the MIL-012 schedule row; review again. | No | |
| 2 | Rename the test methods | Rename the 36 methods to `Method_Condition_Expected` and add an architecture test that fails on a two-part name. | No | |
| 3 | Separate the integration tests | Move the PostgreSQL tests and their fixture to an `Integration` folder; the unit tests run without Docker. | No | |
| 4 | Unit-test `BookingService` | In-memory fakes of the ports and a fake clock; positive, negative, boundary, edge, state transition and error-handling tests. | No | |
| 5 | Unit-test `GuestService` and `RoomService` | Same method; boundary and edge cases of the field rules. | No | |
| 6 | Integration-test the conflict paths | Repositories and unit of work against PostgreSQL: overlap, unique key, concurrency token; two contexts and a latch force the race. | No | |
| 7 | Group the long test classes in regions | Wrap the groups of tests of every class over 150 lines (11 classes) in named `#region` sectors by kind; no test body changes. | No | |
| 8 | Audit the Domain tests and run the exploratory session | Check boundary and edge cases of the entities against the plan; one time-boxed session of the booking flow with a charter and a note. | No | |
| 9 | Check and review | Build, format and test the solution; run the sleep, clock, network and region searches; record an RC against `qc-programming-csharp`. | No | |

---

[BC-001]: ../business-case.md
[MIL-011]: ./mil-011-xunit-v3.md
