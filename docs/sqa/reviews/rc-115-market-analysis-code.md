# RC-115 Review of the market analysis report code (MIL-026)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-115 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-026] tasks 1 to 5: `MarketAnalysisRun`, `MarketAnalysisErrors`, `IMarketAnalysisRunRepository` (Domain); the DTOs of `Dto/MarketAnalysis`, `IMarketAnalysisServiceClient`, `IMarketAnalysisService`, `MarketAnalysisService`, the codes `ErrorCodes.MarketAnalysis` and `ErrorCodes.MarketAnalysisRunner`, the permissions `MarketAnalysisView` and `MarketAnalysisStart` (Application); `MarketAnalysisRunConfiguration`, `MarketAnalysisRunRepository`, the migration `AddMarketAnalysisRun` (Infrastructure); `MarketAnalysisServiceClient`, `MarketAnalysisServiceOptions`, `AddMarketAnalysisService` (External); `MarketAnalysisController` and the status mapping of `ResultResponses` (Api); `ApiMarketAnalysisService` (ApiClient); `MarketAnalysisViewModel`, `MarketAnalysisReport.razor`, the menu entry and the texts of `ErrorMessages` (Web)
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | Names follow the benchmark equivalents (`BenchmarkServiceClient`, `BenchmarkRunListViewModel`) |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The architecture tests pass; the first DTO file held eight types and was split, found by `Dto_EverySourceFile_DeclaresOneTopLevelType` |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format --verify-no-changes` clean; the generated migration was formatted like the earlier ones |
| 4 | Nullable reference types enabled; `!` justified | Pass | One `null!` in `MarketAnalysisServiceOptions.TryGetBaseUri`, with the same comment as the benchmark options |
| 5 | `IDisposable` objects are disposed | Pass | `using` for the request and the response in the client |
| 6 | Async code uses `await` throughout | Pass | No `.Result`, `.Wait()` or `async void`; the architecture test caught a wire property named `Result` and it was renamed `Content` with `[JsonPropertyName("result")]`; the page follows a run with `_ = PollAsync()`, the same deliberate fire-and-forget as the benchmark Start page, which ends with the component token |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The client turns every failure into a code; the two empty-looking catches of the page carry a comment; `ReadContent` returns `null` for unreadable stored JSON and the service answers `market_analysis.body_invalid` |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | Records for the DTOs and the wire types; private setters on the entity and the view model |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | No money; every time is a `DateTimeOffset`, stored in UTC; chart values are `double` because they are counts and rates, not money |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Every async method does |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | New tests: entity, service with a fake of the data analysis service, client against a fake HTTP service that reads the sample of the contract, endpoints through HTTP with the three roles, ApiClient paths, view model, and the repository against PostgreSQL; whole solution 1236 tests, 0 failed |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark or market analysis classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The contract is one sample (`docs/test-data/market-analysis-result.json`) used by the client test; the codes and their texts are one table each; the schema version is `MarketAnalysisService.SupportedSchemaVersion`; the chart is the existing `GroupedBarChart`, not a second one |
| 15 | Namespace and project references point inward | Pass | The web application holds no token of the data analysis service: it calls the API (`ApiMarketAnalysisService`), and only the External project knows HTTP to the service |
| 16 | SOLID holds | Pass | One narrow port for the service (start, state, result) and one use-case interface for the back office; the controller holds no rule |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | No new project; DTOs in `Dto/MarketAnalysis`, registered in the structure test |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. Permissions are checked twice: the API refuses a Receptionist and a Housekeeping user with 403 before the use case runs (test `AllEndpoints_OtherRoles_Return403`), and the page is inside the `MarketAnalysisView` policy with the start button inside `AuthorizeView` for `MarketAnalysisStart`. Nothing was tried against a real `notebook-data-analysis`: its API does not exist in this checkout, so the client is proved against a fake that follows the README of that repository. See the action items.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Check the `result.json` fields (`summary`, `figures`, `series`) and the `metadata` names against the real API of `notebook-data-analysis` when it has one, and change the sample and the client together if they differ | S01 | Before the first deployment |
| Look at `/admin/market-analysis` in a browser with a real result | S01 | Before the first deployment |
| Decide whether a failed run should be listed on the page, not only the one that was started in this session | S01 | When it is needed |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
