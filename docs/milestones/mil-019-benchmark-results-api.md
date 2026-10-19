# MIL-019 Benchmark results API and pages

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-019 |
| CrossReference | [BC-001], [MIL-018], [ADR-0004], [ADR-0005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Proposed | Claude (AI) | S01 | Initial version | [7fbeba1] |
| 2026-10-18 | Accepted | Claude (AI) | AI at the request of S01 | Accepted after RC-086 Go | [7fbeba1] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Tasks 1 to 7 marked done after RC-088 and RC-089 | pending |

---

## Purpose

Decide whether the web application may be the one place that stores and shows the results of the benchmark service. The benchmark service (repository `notebook-benchmark`) runs local AI models on the VM and, when a run is ready, sends the result to the web API. This milestone builds the receiving side: an API that stores a run once, a table, and pages in which a Manager reads the runs later. It builds against the contract of `notebook-benchmark` MIL-001 (ADR-0001 and its OpenAPI file); the milestone cannot start before that contract has a `Go` review.

## Decisions to record in ADR-0008

| # | Decision | Why |
| --- | --- | --- |
| 1 | `POST /api/benchmark/results` receives a run with its per-model records; it needs the service token of ADR-0004 only (no staff token) and answers 201 for a new run and 200 for a repeat of a run id | The benchmark service has no user; a repeat after a failed delivery must be harmless |
| 2 | Two tables, `benchmark_run` (run id, start and end time, dataset name and hash, prompt version, machine, backend version) and `benchmark_result` (one row per request: model, cold flag, time to first token, total time, tokens per second, memory, validity, response) with the key columns named `<table>_id`; the run id of the contract is a unique column | Results stay queryable; the idempotency rests on a unique constraint |
| 3 | The result body is validated against the contract; an unknown `schema_version` gets 400 and is not stored | The numbers in the pages are only as good as the rows |
| 4 | New permissions `Benchmark.View` and `Benchmark.Run` in the one permission table, held by the Manager only | Least privilege; the matrix stays the one place that says who may do what |
| 5 | `GET /api/benchmark/runs` and `GET /api/benchmark/runs/{id}` need the staff token and `Benchmark.View` | The API is the authority, the pages are a convenience |
| 6 | The raw model response is stored but not shown in the list; the detail page shows it behind a click | Responses can be long and may contain the dataset's statistics |

## Deliverable

- ADR-0008 recording the decisions above, reviewed.
- Domain and application: the run and result types, the repository port, the use cases to store, list and read runs, tests with fakes.
- Infrastructure: the tables, the migration and the repository; integration tests for the unique run id.
- API: the three endpoints and the permission checks.
- Web: a list page and a detail page in the back office for a Manager, and the menu entry.
- Unchanged: the staff sign-in, the guest sign-in and TLS.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Posting the same run twice gives 201 then 200 and one `benchmark_run` row with its result rows once | Yes | No |
| 2 | A post without the service token gets 401; a post with an unknown `schema_version` gets 400 and stores nothing | Yes | No |
| 3 | A Manager lists the runs and reads one with every model and its measurements; the values equal the posted values | Yes | No |
| 4 | A Receptionist and a Housekeeping user get 403 on the list and the detail endpoints and cannot open the pages | Yes | No |
| 5 | The permission table test states allowed or refused for every role and permission, including the two new ones | Yes | No |
| 6 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp` reviews have the verdict `Go`; ADR-0008 has a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-018 | The permission table and the staff sign-in that are extended |
| notebook-benchmark MIL-001 | The contract (ADR-0001 and the OpenAPI file) this milestone implements |
| ADR-0004, ADR-0005 | The service token and the staff token |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 22 of an amended BC-001 | All tasks |
| Success criterion 25 | Criteria 1 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the database change |

## Target Date

2027-04-02 — two weeks after MIL-018 (2027-03-19): two tables, three endpoints, two pages.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 22, the scope item, success criterion 25 and the schedule rows of MIL-019 and MIL-020; review again. | No | Done: RC-085 |
| 2 | Record the decisions in ADR-0008 | The six decisions above. | No | Done: ADR-0008, RC-088 |
| 3 | Domain and application | Run and result types, port, use cases to store, list and read; unit tests. | Yes | UC: administrator reads a stored benchmark run. Done: `BenchmarkRun`, `BenchmarkService`; the use case document is open (RC-089) |
| 4 | Tables and repository | Migration, repository, integration test of the unique run id. | No | Done: migration `AddBenchmarkRun`, `BenchmarkRunRepository`; 8 tests against PostgreSQL |
| 5 | API endpoints and permissions | The three endpoints, the two permissions, the tests. | No | Done: `BenchmarkController`, `Benchmark.View`, `Benchmark.Run` |
| 6 | Back office pages | List and detail page, menu entry for the Manager; page tests. | No | Done: `/admin/benchmark`, menu item for the Manager |
| 7 | Check and review | Build, format, test; reviews against `qc-programming-csharp` and `qc-adr`. | No | Done: RC-088, RC-089 |

---

[BC-001]: ../business-case.md
[MIL-018]: ./mil-018-guest-login.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[7fbeba1]: https://github.com/NF-Hotel/NFHotel/commit/7fbeba1c24eb3fb4b21f8fde5b83e76c26fdf2d0
