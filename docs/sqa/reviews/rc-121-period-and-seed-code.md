# RC-121 Review of the period of the analysis and the fake bookings to 2026 (MIL-026)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-121 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version; a delta review of tasks 11 and 12 on top of RC-118 | [364b34c] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026] tasks 11 and 12: `StartMarketAnalysisRequest`, `MarketAnalysisService.StartRunAsync` (period check), `MarketAnalysisRunTracker.PeriodOf`, `MarketAnalysisRunner`, `IMarketAnalysisBookingQueries` and `MarketAnalysisBookingQueries` (period filter), `MarketAnalysisController`, `ApiMarketAnalysisService`, `MarketAnalysisViewModel`, `MarketAnalysisReport.razor`, the texts of `ErrorMessages`; `MarketSeedExpander`, `MarketDataSeeder` (status from today's date), the `--through` option of `ApiStartup`
- Checklist used: [QC-CS-001]
- Scope: delta review of the changed code, and the criteria that the change could affect
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | `StartMarketAnalysisRequest` and `MarketSeedExpander` are one type each; the architecture tests pass |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format` clean. The query parameters are named `firstArrival` and `lastArrival` because `from` is a LINQ keyword |
| 4 | Nullable reference types enabled; `!` justified | Pass | No new `!` |
| 5 | `IDisposable` objects are disposed | Pass | Unchanged |
| 6 | Async code uses `await` throughout | Pass | No blocking call |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The service refuses a first date after the last with `market_analysis.period_invalid`; the `--through` option reports a missing or unreadable date as a usage error (exit 2) |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build |
| 9 | State through properties; immutability where it fits | Pass | The period is a record; the tracker changes it only under its lock |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | Dates of arrival are `DateOnly`, as the booking has them |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | Unchanged |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | New tests: the period in the service, the runner, the endpoint (including 422), the ApiClient body, the view model, the query against PostgreSQL with both ends included and open ends; the expander (copies, cut, unique numbers, plan without overlap); the seeder statuses around today and the real file through 2026-12-31 (loaded with the overlap constraint on). Whole solution 1307 tests, 0 failed, 3 Proxmox tests skipped. The seeder takes today as a parameter, so no test reads the clock |
| 13 | Classes and members trace to the Design Class Diagram | N-A | As in RC-118 |
| 14 | Each piece of knowledge is defined in one place | Pass | The period check is in the service only; the filter is in the query only; the status rule is in the seeder only |
| 15 | Namespace and project references point inward | Pass | Unchanged |
| 16 | SOLID holds | Pass | The expander is a pure function apart from the planner and the seeder |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | `MarketSeedExpander` is in `Infrastructure/Seeding` |
| 18 | File and folder names are English with no space or odd character | Pass | |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. The change was deployed to the development VM, where the seed with `--replace --through 2026-12-31` loaded 156,686 bookings in 294 rooms and the NFHotel API reached the markets API on the shared network. An analysis of all bookings, started from the page, ended with the timeout of 780 s, because six AI answers of up to 240 s each can take 24 minutes on a CPU; the call timeout is now 1500 s. An analysis for the last 3 years (2023-10-20 to 2026-10-20) was then started through the API and completed in 10 minutes 25 seconds: 55,484 bookings, all six analyses with their charts (2 to 7 each, none for the holidays), and the report was stored and read back. The model was `phi4-mini`: one executive summary was written (room value), four insights were rejected by the guardrails of the markets API (`BAD_STRUCTURE`) and one timed out, which its README says small models often do. The quick choice of the last 3 years on the page was added after that run and tested in the view model only.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Look at the finished report in a browser; the browser tool of this session could not open the development site | S01 | Before the first deployment |
| Try a larger model for the insights: `phi4-mini` gave one summary of six, the others were rejected by the guardrails or timed out | S01 | When the AI summaries matter |
| Decide whether the repeated bookings, which arrive up to 2026-12-31 and are confirmed, may stay in the development database | S01 | Before the first deployment |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[364b34c]: https://github.com/NF-Hotel/NFHotel/commit/364b34c0d17a512d3bea06d9e01ad839b2a86e77
