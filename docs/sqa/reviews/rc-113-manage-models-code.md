# RC-113 Review of adding and deleting models in the back office (MIL-020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-113 |
| CrossReference | [MIL-020], [ADR-0014], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [075beac] |

---

## Artifact Under Review

- Instance reviewed: [MIL-020] task 10: `Permissions.BenchmarkModelsManage`, `IBenchmarkServiceClient` with `StartModelPullAsync`, `GetModelPullAsync` and `DeleteModelAsync`, `ModelPullDto`, `ModelPullStates`, `ModelNames` and the new codes of `ErrorCodes.BenchmarkRunner` (Application); `BenchmarkServiceClient` (External); `BenchmarkModelsViewModel`, `ModelsPanel.razor`, the panel on `BenchmarkStart.razor` and the texts in `ErrorMessages` (Web); the commit pin of `deploy/benchmark`
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `StartModelPullAsync`, `ModelPullDto`, `BenchmarkModelsViewModel` follow the names of the run client and its view model |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The architecture tests pass; a first version of `ModelNames` used a generated regex that the one-type-per-file test did not read as one type, so it is a plain static class with a timed `Regex` |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format whitespace` clean |
| 4 | Nullable reference types enabled; `!` justified | Pass | One `trimmed!` after `ModelNames.IsValid` has proved the text is not null; the page code has none |
| 5 | `IDisposable` objects are disposed | Pass | `using` for the request and the response in the client, as before |
| 6 | Async code uses `await` throughout | Pass | No `.Result`, `.Wait()` or `async void`; the panel follows a download with `_ = FollowAsync()`, the same deliberate fire-and-forget as the Start page, which ends with the component token |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The client turns every failure into a code; the name is checked before a call; the two empty-looking catches in the panel carry a comment that says why |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | Records for the DTOs and the wire types; private setters in the view model |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | N-A | No money or time |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Every new async method does |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | 58 new tests: the client against a fake that follows the contract, the name rule, the view model and the panel rendered to HTML; whole solution 1160 tests, 0 failed |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The name rule is `ModelNames` for the client and the view model (and the same pattern as the service, which has its own test against the OpenAPI file); the codes and their texts are one table each |
| 15 | Namespace and project references point inward | Pass | The panel uses the view model, the view model the port; the client is the only place that knows HTTP |
| 16 | SOLID holds | Pass | Three narrow methods on the existing port; the panel and the Start page do not know each other except through one callback |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | No new project; the files are in the existing folders |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. The permission is checked twice: the panel is inside an `AuthorizeView` for `Benchmark.ModelsManage`, and the benchmark service checks its token and its own rules (name shape, one pull at a time, a model in use). A delete does not clear a default AI model; the panel warns before it. The panel was rendered to HTML in tests and the change was deployed to the development VM; the add and delete were tried against the benchmark service there, see the action items for what was not looked at in a browser.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Look at the Installed models panel on `/admin/benchmark/start` in a browser, add phi4-mini and delete a model | S01 | Before the next benchmark run |
| Clear or change a default AI model automatically when its model is deleted, if the warning is not enough | S01 | When it happens in practice |

---

[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[ADR-0014]: ../../adr/adr-0014-default-ai-model.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[075beac]: https://github.com/NF-Hotel/NFHotel/commit/075beac550e309d474b11836bd60fbbbaa8cced5
