# MIL-014 Async where it gives meaning, layer by layer

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-014 |
| CrossReference | [BC-001], [MIL-013] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-060 Go | [417a575] |

---

## Purpose

Decide, layer by layer and in the order of the dependencies (Domain, then
Application, then Infrastructure, then Web), where asynchronous code gives
meaning, make the code follow that, and keep it that way with architecture
tests. Async gives meaning where the code waits for something outside the
process: the database, an HTTP service, a file. It does not where the work is
pure computation in memory.

## Audit so far (2026-10-16)

| Layer | Finding | Action |
| --- | --- | --- |
| Domain | Pure rules in memory: no I/O, so synchronous is right. The repository interfaces are already `Task`-based | None; an architecture test keeps `async` and `Task` out of entities and value objects |
| Application | Every use case is `async`, takes a `CancellationToken` and ends in `Async`. No `.Result`, `.Wait()` or `GetAwaiter().GetResult()` found. `GuestService.ValidateFields` is synchronous and pure, correctly | Check every public method with a source search, not by eye; fix what the search finds |
| Infrastructure | Repositories, queries, unit of work and the holiday client are `async` end to end. No blocking call on a database or HTTP path found | Same search; check that `AsNoTracking` queries stream into lists with `ToListAsync`, not `ToList` |
| Web | View models call the services through the async runner. Two private helpers return `Task` without the `Async` suffix (`Invoke` in `BookingListViewModel` and `RoomListViewModel`); `BeginRescheduleAsync` and `ExecuteAsync` take no token | Rename the helpers; pass the component's token to the calls; look for `IAsyncDisposable` where a component holds an async resource |

## Deliverable

- The audit repeated by a source search per layer, with the result in the review record.
- The fixes the search finds, in the order Domain, Application, Infrastructure, Web, one commit per layer.
- `AsyncRulesTests`: no `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `Task.Run` or `async void` in `src/`; every method that returns `Task` ends in `Async`; entities and value objects contain no `async`; every public asynchronous method of Application and Infrastructure takes a `CancellationToken`.
- Unchanged: behaviour, the database and the screens.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The four searches (blocking calls, `Task` without the suffix, public async methods without a token, `async` in the Domain) find nothing in `src/` | Yes | No |
| 2 | `AsyncRulesTests` exists and fails on a deliberate `.Result`, a deliberate `Task` without the suffix and a deliberate async method in an entity (shown once) | Yes | No |
| 3 | Every I/O call of a Web view model receives the cancellation token of the component | Yes | No |
| 4 | `git diff` shows one commit per layer in the order Domain, Application, Infrastructure, Web; each builds and passes the tests on its own | Yes | No |
| 5 | The result of the audit, including "no change needed" for a layer, is written in the review record | Yes | No |
| 6 | `dotnet build` has 0 warnings, `dotnet format --verify-no-changes` is clean, all tests pass, and a review against `qc-programming-csharp` (criteria 2, 6 and 11) has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-013 | It changes `BookingService` and the booking view models; the async pass comes after to avoid conflicts |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 17 of an amended BC-001: asynchronous code where it gives meaning, enforced by architecture tests | All tasks |
| Success criterion 20 | Criteria 1 to 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-30 — a week after MIL-013 (2026-12-23); the audit found little to change.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 17, the scope item, success criterion 20 and the MIL-014 schedule row; review again. | No | |
| 2 | Write `AsyncRulesTests` | The architecture tests of the deliverable; they fail on what the audit found. | No | |
| 3 | Domain | Search and fix; keep the entities free of `async`. | No | |
| 4 | Application | Search and fix: tokens, suffixes, blocking calls. | No | |
| 5 | Infrastructure | Search and fix: queries that materialise with `ToList` on a database path. | No | |
| 6 | Web | Rename the two helpers, pass the component token, check `IAsyncDisposable`. | No | |
| 7 | Check and review | Show each rule failing once; build, format and test; record an RC against `qc-programming-csharp`. | No | |

---

[BC-001]: ../business-case.md
[MIL-013]: ./mil-013-booking-guards.md
[417a575]: https://github.com/NF-Hotel/NFHotel/commit/417a575b978d56c59da23eb6cff5967a7b9bf6bf
