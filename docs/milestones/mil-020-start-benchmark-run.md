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
| 3 | Deploy files and tokens | Compose entries and env examples for the benchmark and Ollama containers. | No | Done: `nfhotel-web.env.example`, `docker-compose.yml` comment, wiki section |
| 4 | Check and review | Build, format, test; reviews against `qc-programming-csharp` and `qc-yaml`. | No | Done: RC-106, RC-107 |
| 5 | Delete a stored run | `Permissions.BenchmarkDelete` (Manager only); `DELETE api/benchmark/runs/{runId}`; a Delete button with a confirmation on `/admin/benchmark`; the requests go with the run. | Yes | Done: `BenchmarkService.DeleteRunAsync`, `BenchmarkController.DeleteRunAsync`, `ApiBenchmarkService.DeleteRunAsync`, `BenchmarkRunListViewModel.ConfirmDeleteAsync`; 17 new tests |
| 6 | Readable results table and models | A Task column (hotel cancellation analysis or the task name) and a 1-based run number in the table of one run; `phi3.5` is pulled with `llama3.2:3b` by `scripts/dev-up.sh`. | No | Done: `BenchmarkRuns.razor`, `scripts/dev-up.sh`; review RC-108 |

---

[BC-001]: ../business-case.md
[MIL-019]: ./mil-019-benchmark-results-api.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[7fbeba1]: https://github.com/NF-Hotel/NFHotel/commit/7fbeba1c24eb3fb4b21f8fde5b83e76c26fdf2d0
[847b639]: https://github.com/NF-Hotel/NFHotel/commit/847b639fa32cd4699fbfb154011ea93b76fdc545
[4f23a15]: https://github.com/NF-Hotel/NFHotel/commit/4f23a1592ed1f75d3f3a7c30d473671ffce49b64
