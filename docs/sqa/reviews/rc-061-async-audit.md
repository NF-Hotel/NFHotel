# RC-061 Review of the async audit (MIL-014)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-061 |
| CrossReference | [MIL-014], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [ceab0e4] |

---

## Artifact Under Review

- Instance reviewed: the asynchronous code of all four layers in the order Domain, Application, Infrastructure, Web; `CancellableComponentBase`, the six pages that inherit it, the two renamed helpers, and `AsyncRulesTests`
- Checklist used: [QC-CS-001], criteria 2, 5, 6 and 11 in particular
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`, the source scans of `AsyncRulesTests`

## Audit, layer by layer

| Layer | Search and result | Change |
| --- | --- | --- |
| Domain | Entities, value objects and errors contain no `async`, `Task` or `ValueTask`; only the repository interfaces use `Task`, as they describe I/O. They are pure computation, so synchronous is right | None, kept by `DomainTypes_ThatAreNotRepositories_DoNotUseTasks` |
| Application | No `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `Task.Run` or `async void`; every public asynchronous method takes a `CancellationToken` and ends in `Async` (the rule scans every method). `GuestService.ValidateFields` is synchronous and pure, correctly | None |
| Infrastructure | No blocking call on a database or HTTP path. The `.ToArray()` calls are on in-memory collections (the status lists), and the one `Any(...)` in `RoomRepository` is inside an EF expression that is translated to SQL. Repositories, queries, the unit of work and the holiday client are asynchronous end to end | None |
| Web | The view models are asynchronous and take a token, but **no page passed one**: 30 calls in six pages used the default token, so a closed page or a lost circuit left the calls running. Two private helpers returned `Task` without the `Async` suffix | `CancellableComponentBase` and `@inherits` in the six pages; every view-model call passes `ComponentToken`; `Invoke` renamed `InvokeAsync` in two view models |

## Go / No-Go criteria of MIL-014

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The four searches find nothing in `src/` | Pass | `AsyncRulesTests`: blocking calls, suffix, domain and token rules all pass; the suffix rule failed before the Web change on `BookingListViewModel.Invoke` and `RoomListViewModel.Invoke` |
| 2 | `AsyncRulesTests` fails on a deliberate `.Result`, a `Task` without the suffix and an async method in an entity | Pass | Shown with temporary files in `src/`: the blocking-call rule and the domain rule failed with the file named, and a file with a public asynchronous method without a token failed the token rule. The suffix rule failed on the two real helpers before the Web change. The temporary files were deleted |
| 3 | Every I/O call of a Web view model receives the component token | Pass | `ViewModelCalls_InEveryComponent_PassTheComponentToken`; shown to fail on the old `GuestList.razor`, which was then restored |
| 4 | One commit per layer in the order Domain, Application, Infrastructure, Web; each builds and passes | Pass with a note | Only the Web layer changed, so there is one code commit. The tests are in the same commit because a rule test cannot pass before its fix. The three layers with no change have no commit and are recorded in the audit table |
| 5 | The result of the audit, including "no change needed", is in this review | Pass | The audit table below |
| 6 | 0 build warnings, format clean, all tests pass, this review `Go` | Pass | 514 tests, 0 failed, 3 skipped |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `CancellableComponentBase`, `ComponentToken`, `InvokeAsync`; test names have three parts |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The two `Invoke` helpers became `InvokeAsync`; `AsyncRulesTests` fails on a task-returning method without the suffix |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; 0 warnings |
| 4 | Nullable enabled; `!` justified | Pass | No `!` added |
| 5 | `IDisposable` disposed | Pass | `CancellableComponentBase` implements `IDisposable` and cancels and disposes its token source; Blazor calls it when the component goes away |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | `AsyncRulesTests` fails on all of them; the source holds none |
| 7 | Specific exceptions, no empty catch | N-A | Not touched |
| 8 | Public types have XML documentation | Pass | The new base class and its members are documented |
| 9 | Properties, not public fields; immutability | Pass | `ComponentToken` is a get-only property |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | Every public asynchronous method of Application and Infrastructure takes one (rule enforced); the six pages now pass `ComponentToken` to the 30 view-model calls |
| 12 | Tests cover new behaviour, three-part names, no order, time or network | Pass | 5 architecture tests added (509 to 514); each shown to fail on a deliberate violation |
| 13 | Classes trace to the Design Class Diagram | N-A | One web helper class; no domain class |
| 14 | Each piece of knowledge is defined once | Pass | The token source of a page lives once in the base class |
| 15 | References point inward, no cycles | Pass | The base class is in the web project and references only ASP.NET Core components |
| 16 | SOLID | Pass | Pages extend the base class instead of each managing a token source |
| 17 | Layer folders | Pass | `Common/` of the web project |
| 18 | File and folder names | Pass | English, no space |

## Overall Verdict

Go — every applicable criterion of QC-CS-001 and all six Go criteria of MIL-014 pass. The one real finding was in the Web layer: no page cancelled its calls. Behaviour on screen is unchanged, except that a call stops when its page goes away. The scan rules are source scans, so a method whose signature is split by an unusual layout could escape the token rule; the numbers in the vacuity checks (over 30 public asynchronous methods, over 50 task-returning methods, over 20 view-model calls) show that they do see the code.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-014]: ../../milestones/mil-014-async-where-it-matters.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[ceab0e4]: https://github.com/NF-Hotel/NFHotel/commit/ceab0e428d78c0673a2a40a975aedf8ed57c7aba
