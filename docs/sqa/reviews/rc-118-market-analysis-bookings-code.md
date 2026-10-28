# RC-118 Review of the market analysis of the hotel bookings (MIL-026 amendment)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-118 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version; it replaces RC-115 for the code that the amendment changed | [1fd620f] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026] tasks 1 to 5 and 8 to 10 after the amendment of 2026-10-20: `BookingMarketDetail`, `BookingMarketValues`, `BookingMarketDetailErrors`, `MarketAnalysisRun` (Domain); the DTOs of `Dto/MarketAnalysis`, `IMarketAnalysisServiceClient`, `IMarketAnalysisBookingQueries`, `IMarketAnalysisRunLauncher`, `MarketAnalysisService`, `MarketAnalysisRunner`, `MarketAnalysisRunTracker`, `StayNights` (Application); `BookingMarketDetailConfiguration`, `MarketAnalysisBookingQueries`, the migrations `AddMarketAnalysisRun` and `AddBookingMarketDetail`, `MarketSeedCsv`, `MarketSeedPlanner`, `MarketSeedPricing`, `MarketDataSeeder` (Infrastructure); `MarketAnalysisServiceClient`, `MarketAnalysisResultMapper`, `FindingsSeries`, `MarketAnalysisServiceOptions` (External); `MarketAnalysisController`, `ChannelMarketAnalysisRunLauncher`, `MarketAnalysisRunWorker`, the `seed-market-data` command of `ApiStartup` (Api); `ApiMarketAnalysisService` (ApiClient); `MarketAnalysisViewModel`, `MarketAnalysisReport.razor` (Web)
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | Names follow the benchmark equivalents |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | The architecture tests pass; they found a file with eight DTOs and a wire property named `Result`, both fixed |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format --verify-no-changes` clean. One `#pragma warning disable CA1031` in `MarketAnalysisRunWorker`, with the reason: a background worker has no caller to throw to |
| 4 | Nullable reference types enabled; `!` justified | Pass | One `null!` in the options, as in the benchmark options; one `!` after `ExecuteScalarAsync` of a `SELECT EXISTS`, which cannot be null |
| 5 | `IDisposable` objects are disposed | Pass | `using` for the request, the response, the JSON document and the COPY writers; the transaction is `await using` |
| 6 | Async code uses `await` throughout | Pass | No `.Result`, `.Wait()`, `async void` or `Task.Run`; the architecture test refused a first version of the launcher that used `Task.Run`, so the run is a `BackgroundService` that reads a `Channel` |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The client turns every failure into a code; the worker catches the cancellation and any other exception, writes it to the run and logs it; the seeder refuses with an `InvalidOperationException` that the command turns into exit code 1 |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | Records for the DTOs and wire types; private setters on the entities; the tracker changes its state only under a lock |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | `PricePerNight` is `decimal` (`numeric(12,2)`); times are `DateTimeOffset`; the date of a booking is `DateOnly`; chart values are `double` because they are shares and counts |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Every async method does; the worker passes the stopping token |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | New tests for the entity, the planner, the CSV reader, the price rule, the seeder and the feed against PostgreSQL (the real 40,060-row file included), the tracker, the runner, the service, the client against a fake HTTP service and the real findings shape, the endpoints with the three roles, the view model; whole solution 1287 tests, 0 failed, 3 Proxmox tests skipped |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark or market analysis classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The contract sample is one file used by the client test; the codes and their texts are one table each; the overlap rule is not copied: the planner follows the half-open period and the database constraint is the proof; the chart is the existing `GroupedBarChart` |
| 15 | Namespace and project references point inward | Pass | The web application holds no address of the markets API: it calls the NFHotel API; only External knows HTTP to the markets API, and only Infrastructure knows Npgsql and `COPY` |
| 16 | SOLID holds | Pass | One narrow port each for the markets API, the booking read and the launcher; the runner does the work and the service only starts and reads |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | `Seeding` is a new folder of Infrastructure; the DTO folder is registered in the structure test |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. Permissions are checked twice: the API gives 403 to a Receptionist and a Housekeeping user before the use case runs, and the page is inside the `MarketAnalysisView` policy. The tests prove the bookings read are the rows of the database and that the real file loads with the overlap constraint on. The real markets API was started locally and accepted the 40,060 bookings that the code produced, with no invalid value counted; its answer (schema 1.0, no insights) was read by the mapper into all six analyses with their charts. What was not seen: a run with `insights=true` (no AI model was running, so the 1.1 insight parts of the sample were written by hand from the JSON Schema), and the page in a browser.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `/analyze?insights=true` with a model on the VM, and check that the real insight text and its reasons are read as in the sample | S01 | Before the first deployment |
| Look at `/admin/market-analysis` in a browser with the real result, and decide whether the charts of the 26 or 30 months should be shown as a line | S01 | Before the first deployment |
| Replace the fake bookings with the real ones, and the generated prices with the real ones, when the hotel has them | S01 | When real data exists |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[1fd620f]: https://github.com/NF-Hotel/NFHotel/commit/1fd620f1fbbf51a8296d577e2e7654bb4415ff01
