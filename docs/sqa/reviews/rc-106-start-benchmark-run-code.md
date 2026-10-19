# RC-106 Review of the start of a benchmark run (MIL-020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-106 |
| CrossReference | [MIL-020], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [847b639] |

---

## Artifact Under Review

- Instance reviewed: [MIL-020]: `IBenchmarkServiceClient`, the DTOs `StartBenchmarkRunRequest` and `BenchmarkRunStateDto` with the state constants, and `ErrorCodes.BenchmarkRunner` (Application); `BenchmarkServiceClient`, `BenchmarkServiceOptions` and `AddBenchmarkService` (External); `BenchmarkStartViewModel`, `BenchmarkStart.razor`, the link on the list page and the screen texts (Web); and their tests
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | PascalCase types and members, `_camelCase` fields (a test field `_models_started` was renamed during the work), `I` interfaces |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The architecture tests pass; the wire records and the fake service are nested private types |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format --verify-no-changes` flags none of the files of this change, only the 14 older files already named in RC-105 |
| 4 | Nullable reference types enabled; `!` justified | Pass | One `null!` in `BenchmarkServiceOptions.TryGetBaseUri` with the same justification comment as `HolidayApiOptions` |
| 5 | `IDisposable` objects are disposed | Pass | `using` for the request and the response in the client |
| 6 | Async code uses `await` throughout | Pass | No `.Result`, `.Wait()` or `async void`. The page starts its follow loop with `_ = FollowAsync()`, a deliberate fire-and-forget that catches the end of the page (cancellation and a disposed circuit) and is stopped by the component token |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | `ArgumentNullException.ThrowIfNull`; the client turns `HttpRequestException`, a timeout and `JsonException` into codes and logs them; the two empty-looking catches in the page carry a comment that says why |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | Records for the DTOs and the wire types; the view model exposes read-only lists |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | No money; the times are `DateTimeOffset` |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Every async method does |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | A fake of the benchmark service that follows the contract (token check, validation, queued, running, done, delivered) drives 19 client tests; 18 view model tests; 4 more page access cases. Whole solution: 1011 tests, 1008 passed, 3 skipped (live Proxmox), 0 failed. No test sleeps: the follow loop of the page is not unit-tested, only the view model that it calls |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The limits (4 models, 5 runs) are constants of the view model and the client check mirrors them, which the service checks again; the run states are constants in `BenchmarkRunStates`; the error codes and their texts are one table |
| 15 | Namespace and project references point inward | Pass | The port is in Application; the HTTP client is in External; the page uses the view model and the port, never the client class |
| 16 | SOLID holds | Pass | A narrow port with three methods; the view model depends on the runner and the port |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | Same projects and folders as the rest of the solution |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. The check that only a Manager starts a run is the policy `Benchmark.Run` on the page (ADR-0001 of the benchmark service); the client itself does not look at the role, so any other component that injects the port would bypass it.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run the start page and the follow loop against a real benchmark service on the shared network | S01 | notebook-benchmark MIL-004 |
| Do not inject `IBenchmarkServiceClient` outside the Start page; if a second place needs it, put the permission check in a use case that wraps the port | S01 | When a second caller appears |

---

[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[847b639]: https://github.com/NF-Hotel/NFHotel/commit/847b639fa32cd4699fbfb154011ea93b76fdc545
