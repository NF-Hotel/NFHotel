# RC-249 Review of the live view of runs and downloads (MIL-020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-249 |
| CrossReference | [MIL-020], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [81760a7] |

---

## Artifact Under Review

- Instance reviewed: [MIL-020] task 11: `IBenchmarkServiceClient.ListRunsAsync` and `ListModelPullsAsync` (Application); `BenchmarkServiceClient` (External); `BenchmarkStartViewModel.KnownRuns`, `IsLive` and `RefreshLiveAsync`, `BenchmarkModelsViewModel.RefreshLiveAsync`, `BenchmarkStart.razor` and `ModelsPanel.razor` with their `PollAsync` (Web); the commit pin of `deploy/benchmark`
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `KnownRuns` is named so that it does not clash with `Runs`, the number of repetitions; `RefreshLiveAsync` on both view models |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The architecture tests pass |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format whitespace` clean |
| 4 | Nullable reference types enabled; `!` justified | Pass | No new `!` |
| 5 | `IDisposable` objects are disposed | Pass | No new disposable |
| 6 | Async code uses `await` throughout | Pass | No `.Result`, `.Wait()` or `async void`; the page and the panel start their loops with `_ = PollAsync()`, the same deliberate fire-and-forget as before, stopped by the component token |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The two empty-looking catches in the loops carry a comment that says why; a failing read is a code, never an exception |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | Private setters; the lists are replaced as a whole |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | Times are `DateTimeOffset`, shown in UTC |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Every new async method does |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | 14 new tests: the client lists against a fake that follows the contract (4), the Start view model (5), the models view model (4), and a panel rendered to HTML with an active download (1); the loops themselves are not tested, only what a read does; whole solution 1174 tests, 0 failed |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The interval is one constant per component; a run and a download are the same DTOs as the single reads |
| 15 | Namespace and project references point inward | Pass | The pages use the view models, the view models the port |
| 16 | SOLID holds | Pass | Two narrow reads on the existing port |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | No new project or folder |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. Two decisions are open to challenge: the pages read the service every 3 seconds instead of the service pushing (ADR-0006 of notebook-benchmark), and the Start page no longer has a separate panel for the run that it started, because the Runs table shows every run. The loops were not run in a browser; the check in the development VM is in the action items.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Look at the Runs table in a browser: refresh while a run is queued, and start a run from another tab | S01 | Before the next benchmark run |
| Refresh the stored runs on `/admin/benchmark` live too, when a result is delivered | S01 | If it is wanted |

---

[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[81760a7]: https://github.com/NF-Hotel/NFHotel/commit/81760a7483bd15219e274ffdc199a319a670c2e8
