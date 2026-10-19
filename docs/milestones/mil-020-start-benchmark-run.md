# MIL-020 Start a benchmark run

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-020 |
| CrossReference | [BC-001], [MIL-019], [ADR-0003] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Proposed | Claude (AI) | S01 | Initial version | [7fbeba1] |
| 2026-10-18 | Accepted | Claude (AI) | AI at the request of S01 | Accepted after RC-087 Go | [7fbeba1] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Tasks 1 to 4 marked done after RC-106 and RC-107 | [847b639] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Added tasks 5 and 6 (delete a stored run, readable results table) and acceptance criterion 7; done after RC-108 | [4f23a15] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Added task 7 (default AI model for the website and the market analysis) and acceptance criterion 8; done after RC-109 and RC-110 | [b1daae2] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Added task 8 (the analysis view of a run, like the notebook) and acceptance criterion 9; done after RC-111 | [09e3167] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Added task 9 (the default model reaches the market analysis under the exact installed name) and acceptance criterion 10; done after RC-112 | [b195b69] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Task 3 note: nginx sends a plain HTTP request to the TLS port on to HTTPS (status 497), checked by the proxy test | [9962898] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Added task 10 (a Manager adds and deletes Ollama models through the benchmark service, notebook-benchmark MIL-005) and acceptance criterion 11; done after RC-113 | pending |

---

## Purpose

Let a Manager start a benchmark run from the back office and see its state until the result is stored. The web application calls the benchmark service (repository `notebook-benchmark`) over the internal network of the VM with a service token; the service answers at once with a run id and delivers the result later to the API of MIL-019. This milestone is the web side of the start; the proof of the whole chain is `notebook-benchmark` MIL-004.

## Deliverable

- A client of the benchmark service (an adapter and a port in the Application layer) that starts a run for chosen models and reads its state, with the base address and the token in the git-ignored env file.
- A start page for a Manager: choose the installed models, start, and see the state (`queued`, `running`, `done`, `failed`) until the result is in the list of MIL-019.
- The deploy files: the benchmark and Ollama containers on the external network `nfhotel-proxy`, the two tokens in env files with tracked examples, no published host port.
- A fake of the benchmark service for the tests, so that no test needs Ollama or the VM.
- Unchanged: the results API, the staff and guest sign-in, TLS.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager starts a run on the page and the fake benchmark service receives the chosen models with the service token | Yes | No |
| 2 | A Receptionist and a Housekeeping user get 403 on the start endpoint and cannot open the start page | Yes | No |
| 3 | When the benchmark service is down or answers 401 or 429, the page shows a clear message and starts nothing | Yes | No |
| 4 | The state on the page follows the state of the fake from `queued` to `done` | Yes | No |
| 5 | No token is in git; the compose files publish no host port for the benchmark or Ollama; `git check-ignore` is true for the env files | Yes | No |
| 6 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp` and `qc-yaml` reviews have the verdict `Go` | Yes | No |
| 7 | A Manager deletes a stored run with a confirmation and it is gone with its requests; a Receptionist and a Housekeeping user get 403; an unknown run is 404 | Yes | No |
| 8 | A Manager sets the default AI model of the website and of the market analysis from the benchmark page; the service token reads it; a Receptionist and a Housekeeping user get 403 on the write; the deploy script writes the market analysis model into its settings | Yes | No |
| 9 | The open run shows the performance per model and phase with charts, the deterministic outcomes, the answers side by side, the memory use and a CSV and JSON download, built from the stored results | Yes | No |
| 10 | A default model written without its tag (phi3.5) reaches the market analysis settings as the exact name that Ollama lists (phi3.5:latest), a model that is not installed is not written, and the page says to use the exact name | Yes | No |
| 11 | A Manager adds a model by name and follows the download to done, then can choose it for a run; a Manager deletes an installed model after a confirmation, with a warning when it is a default AI model; the benchmark service refuses a delete for a model that a run uses; a Receptionist and a Housekeeping user do not see the panel | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-019 | The results API, the table and the pages the run ends in |
| notebook-benchmark MIL-002 and MIL-003 | The API and the containers this client calls |
| ADR-0003 | The VM and the network rules |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 22 of an amended BC-001 | All tasks |
| Success criterion 25 (a start request only from a Manager) | Criteria 1 and 2 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the containers and the ports |

## Target Date

2027-04-16 — two weeks after MIL-019 (2027-04-02): one client, one page and the deploy files.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Client of the benchmark service | Port and adapter to start a run and read its state; fake for the tests. | No | Done: `BenchmarkServiceClient`, `IBenchmarkServiceClient`; 19 tests against a fake of the service |
| 2 | Start page | Choose models, start, follow the state; Manager only; page tests. | Yes | UC: administrator starts a benchmark run. Done: `/admin/benchmark/start`, `BenchmarkStartViewModel`; Manager only |
| 3 | Deploy files and tokens | Compose entries and env examples for the benchmark and Ollama containers. | No | Done: `nfhotel-web.env.example`, `docker-compose.yml` comment, wiki section; nginx redirects a plain HTTP request on the TLS port to HTTPS (`error_page 497`), proxy test |
| 4 | Check and review | Build, format, test; reviews against `qc-programming-csharp` and `qc-yaml`. | No | Done: RC-106, RC-107 |
| 5 | Delete a stored run | `Permissions.BenchmarkDelete` (Manager only); `DELETE api/benchmark/runs/{runId}`; a Delete button with a confirmation on `/admin/benchmark`; the requests go with the run. | Yes | Done: `BenchmarkService.DeleteRunAsync`, `BenchmarkController.DeleteRunAsync`, `ApiBenchmarkService.DeleteRunAsync`, `BenchmarkRunListViewModel.ConfirmDeleteAsync`; 17 new tests |
| 6 | Readable results table and models | A Task column (hotel cancellation analysis or the task name) and a 1-based run number in the table of one run; `phi3.5` is pulled with `llama3.2:3b` by `scripts/dev-up.sh`. | No | Done: `BenchmarkRuns.razor`, `scripts/dev-up.sh`; review RC-108 |
| 7 | Default AI model | Table `ai_default` (one row per purpose), `AiDefaults.Manage` for the Manager, `GET`/`PUT api/ai-defaults`, a Default AI model card and "Use for" buttons on `/admin/benchmark`, and `apply_default_model` in `scripts/deploy-services.sh` (ADR-0014). | Yes | Done: `AiDefault`, `AiDefaultService`, `AiDefaultsController`, `ApiAiDefaultService`, migration `AddAiDefault`; 37 new tests; reviews RC-109 and RC-110 |
| 8 | Analysis view of a run | Workload filter, means per model and phase with SVG bar charts (first token, total time, tokens per second), task outcome grid, up to three answers side by side with the parsed parts, RAM and GPU memory charts, CSV and JSON export; `BenchmarkResultDto` carries the memory moments and the finish reason. The figures the benchmark service calculates from the dataset are not delivered, so the cards show n/a for them. | Yes | Done: `BenchmarkResultAnalysis`, `HotelAnswer`, `BenchmarkExport`, `BenchmarkAnalysisViewModel`, `GroupedBarChart`, `AnswerCard`, `BenchmarkRunAnalysis`; 35 new tests; review RC-111 |
| 9 | Exact model name for the market analysis | `scripts/resolve-model-name.sh` finds the listed name (tag included) and `deploy-services.sh` writes it; a model that is not installed is not written; a hint on the Default AI model card; `generation_timeout_seconds` 240 in the settings example. Found when the AI executive summary was missing: the default `phi3.5` selected no model because Ollama lists `phi3.5:latest`. | Yes | Done: `resolve-model-name.sh` with 14 shell checks, `IsUnknownModel`; review RC-112 |
| 10 | Add and delete models | `Permissions.BenchmarkModelsManage` (Manager only); `IBenchmarkServiceClient.StartModelPullAsync`, `GetModelPullAsync`, `DeleteModelAsync` and the codes of the service; an Installed models panel on `/admin/benchmark/start` with an Add field, progress, and a Delete button with the confirmation panel; `deploy/benchmark` is built from the commit of notebook-benchmark that has the endpoints (`409693d`). | Yes | Done: `BenchmarkModelsViewModel`, `ModelsPanel.razor`, `ModelNames`, client methods; 58 new tests; review RC-113 |

---

[BC-001]: ../business-case.md
[MIL-019]: ./mil-019-benchmark-results-api.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[7fbeba1]: https://github.com/NF-Hotel/NFHotel/commit/7fbeba1c24eb3fb4b21f8fde5b83e76c26fdf2d0
[847b639]: https://github.com/NF-Hotel/NFHotel/commit/847b639fa32cd4699fbfb154011ea93b76fdc545
[4f23a15]: https://github.com/NF-Hotel/NFHotel/commit/4f23a1592ed1f75d3f3a7c30d473671ffce49b64
[b1daae2]: https://github.com/NF-Hotel/NFHotel/commit/b1daae275e05b207cd5403a3ec47e46fc76f33d0
[09e3167]: https://github.com/NF-Hotel/NFHotel/commit/09e3167d660245247524dcd240d05b0bdf6d5419
[b195b69]: https://github.com/NF-Hotel/NFHotel/commit/b195b6954dc291bc1131c121b0bc403c73b2dcec
[9962898]: https://github.com/NF-Hotel/NFHotel/commit/99628981e27b95218fa5f75b79a1b5e1e940c519
