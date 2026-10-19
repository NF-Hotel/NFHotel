# MIL-026 Market analysis report in the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-026 |
| CrossReference | [BC-001], [US-001], [MIL-019], [MIL-020] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Deprecated | Jens Tirsvad Nielsen | AI at the request of S01, not independent | Initial version; accepted after RC-114 Go | pending |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | The service does not push a result, so the API fetches and stores it on the first read of a completed run (task 3); the chart is the existing `GroupedBarChart` (task 5); tasks 1 to 7 done after RC-115 and RC-116 | pending |

---

## Purpose

Let a Manager start a market analysis and read its report in the back office. The web application calls the Python service of the repository `notebook-data-analysis` through the website API (API to API), the same way the benchmark works in [MIL-019] and [MIL-020]: the Python service runs the `market_analysis` notebook, delivers the result to the website API, and a Blazor page shows the report with charts and the AI analysis. This milestone is the website side; the Python side is a matching milestone in `notebook-data-analysis`. It delivers [US-001] story US-001.01 (View the market analysis) with the service of that repository.

## Deliverable

- A client of the `notebook-data-analysis` API (a port in the Application layer and an adapter) that starts the `market_analysis` run and reads its state, with the base address and the service token in the git-ignored env file.
- Website API endpoints: start a run (Manager only), read the state of a run, and read the latest report. The data analysis service does not push a result, so the API reads it when a completed run is first asked for, and stores it in the website database with its `schema_version`.
- A back office page `/admin/market-analysis` for a Manager: start a run, follow the state (`queued`, `running`, `completed`, `failed`), and read the report: charts of the market data and the AI analysis next to them, or a clear message when there is no result yet.
- A fake of the Python service for the tests, so that no test needs Python, Ollama or the VM.
- Deploy files: the `notebook-data-analysis` container on the internal networks `nfhotel-api` and `nfhotel-llm` (the website API calls it; the proxy has no route to it), token in an env file with a tracked example, no published host port.
- Unchanged: the benchmark pages, the staff and guest sign-in, TLS.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager starts a run on the page and the fake service receives `POST /api/v1/notebooks/market_analysis/runs` with the service token | Yes | No |
| 2 | A Receptionist and a Housekeeping user get 403 on every market analysis endpoint and cannot open the page | Yes | No |
| 3 | When the service is down or answers 401, 409 or 429, the page shows a clear message and starts nothing | Yes | No |
| 4 | The state on the page follows the state of the fake from `queued` to `completed`, and `failed` shows a sanitised error | Yes | No |
| 5 | A completed result shows charts and the AI analysis side by side; with no result the page says so instead of showing an empty page (US-001.01 acceptance criteria) | Yes | No |
| 6 | A result with an unknown `schema_version` is refused and not stored; a completed run is read from the service and stored once, however often the page asks | Yes | No |
| 7 | No token is in git; the compose file publishes no host port; `git check-ignore` is true for the env file | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp` and `qc-yaml` reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-019 | The result storage pattern and API conventions this milestone repeats |
| MIL-020 | The service client pattern, Manager policy, tokens and deploy files |
| notebook-data-analysis (API, `market_analysis` notebook, `result.json` schema) | The service this client calls; its own milestone must deliver the run API first |
| ADR-0003 | The VM and the network rules |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 6 of BC-001 (market analysis service) | All tasks |
| US-001.01 View the market analysis | Tasks 4 and 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the container and the ports |

## Target Date

2026-11-13 — about three weeks from the plan, after the `notebook-data-analysis` run API exists; for S01 to confirm.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Agree the result contract with notebook-data-analysis | Fix the `market_analysis` `result.json` fields (market figures per country, chart series, AI narrative, `schema_version`) with the Python repository and record them as a DTO and a sample file, so both sides test against the same example. | No | Done on the website side: `MarketAnalysisResultDto` and `docs/test-data/market-analysis-result.json`, which the client test reads. The Python repository has no API yet, so the fields follow its README and are to be confirmed there (action item of RC-115) |
| 2 | Client of the data analysis service | Port and adapter to start a `market_analysis` run and read its state with a bearer token; fake of the service for the tests; base address and token in the env file with a tracked example. | No | Done: `IMarketAnalysisServiceClient`, `MarketAnalysisServiceClient`, `MarketAnalysisServiceOptions`; tests against a fake HTTP service |
| 3 | Market analysis API and storage | `Permissions.MarketAnalysisView` and `MarketAnalysisStart` (Manager only); endpoints to start a run, read the latest run and receive the result; table for runs and results; reject an unknown `schema_version`. | Yes | US-001.01. Done: `MarketAnalysisController` (`POST api/market-analysis/runs`, `GET runs/{id}`, `GET latest`), `MarketAnalysisService`, table `market_analysis_run` (migration `AddMarketAnalysisRun`), `ApiMarketAnalysisService`; the result is fetched and stored on the first read of a completed run, not pushed |
| 4 | Market analysis report page | `/admin/market-analysis`: start, follow the state, show charts and the AI analysis next to them, and a clear message when there is no result; view model and page tests. | Yes | US-001.01. Done: `MarketAnalysisReport.razor`, `MarketAnalysisViewModel`, menu entry for the Manager; not looked at in a browser (action item of RC-115) |
| 5 | Charts for the market data | SVG charts of the stored series in the style of the benchmark `GroupedBarChart`; no script library. | No | Done by reusing `GroupedBarChart`: one group per point, one bar series per chart |
| 6 | Deploy files and token | Compose entry for the `notebook-data-analysis` container on `nfhotel-proxy`, env example, wiki section; no published host port. | No | Done: `deploy/data-analysis/` on `nfhotel-api` and `nfhotel-llm` (the API calls it, not the proxy), `nfhotel-api.env.example`, wiki section. Not in `scripts/deploy-services.sh` and not built: `notebook-data-analysis` has no Dockerfile yet |
| 7 | Check and review | Build, format, test; reviews against `qc-programming-csharp` and `qc-yaml`; record the RC. | No | Done: build 0 warnings, format clean, 1236 tests passed (3 Proxmox tests skipped); RC-115, RC-116 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-019]: ./mil-019-benchmark-results-api.md
[MIL-020]: ./mil-020-start-benchmark-run.md
