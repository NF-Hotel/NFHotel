# ADR-0013 Storing and showing benchmark results

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0013 |
| CrossReference | [BC-001], [MIL-019], [ADR-0004], [ADR-0005] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Proposed | Claude (AI) | S01 | Initial version | [5e09a59] |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Accepted after RC-104 Go | [5e09a59] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The benchmark service (repository `notebook-benchmark`) runs local models and, when a run is ready, sends its result to this API (ADR-0001 of that repository, `openapi/benchmark-contract.yaml`). The web application must store each run once, however often it is delivered, and show it to a Manager later (objectives 4 and 5 of the benchmark Business Case, objective 22 of [BC-001]).

Forces: the benchmark service has no user, only the service token of [ADR-0004]; a delivery can be repeated after a failure or at the same moment from a retry; a result holds long model answers; the permission table of [ADR-0005] is the one place that says who may do what; the API is the authority and the pages are a convenience.

Options considered for the storage:

1. **One table with the result as a JSON column.** Simple, but the measurements cannot be queried or sorted, and the page reads JSON.
2. **Two tables**, `benchmark_run` for the header and `benchmark_result` for one row per request, with the key column named `<table>_id` as the other tables.
3. **Files on a volume** read by the web application. No database change, but a second place that holds data, outside the backup and the permission checks.

## Decision

We follow option 2. The API stores a run once on its run id and shows it to a Manager only.

- **Receiving.** `POST /api/benchmark/results` needs the service token only, like the other endpoints without a permission. It answers 201 for a new run id and 200 for a repeat of a run id, whatever the body. A body that does not match the contract, or has a `schema_version` other than 2, gets 400 with the error codes of the API envelope and stores nothing. The benchmark service reads the status code only.
- **Tables.** `benchmark_run` holds the run id (unique, `ux_benchmark_run_run_id`), start and end time, dataset name, hash and rows, prompt version, the machine (CPU, cores, memory, GPU or none, operating system) and the backend with its version. `benchmark_result` holds one row per request with the fields of the result schema version 2: model, backend, analysis type, run number, cold flag, times, tokens, memory and video memory, outcome and the answer text. The answer is kept but the list does not read it.
- **Idempotency.** The service looks the run id up first; the unique index is the safeguard when two deliveries arrive at the same moment, and the loser answers 200.
- **Permissions.** `Benchmark.View` reads runs and `Benchmark.Run` starts one (MIL-020). Both are held by the Manager only and are rows of the one permission table, with a test for every role.
- **Reading.** `GET /api/benchmark/runs` and `GET /api/benchmark/runs/{id}` need the staff token and `Benchmark.View`. The list shows run id, time, dataset, machine and the models with their count of requests; the detail shows every request with its measurements and, on its own call, the answer.
- **Validation.** The body is checked in the Application layer by the same rules the contract states (required fields, ranges, non-empty results); the database holds no rule that the code does not.

## Consequences

**Positive:**

- A repeated or simultaneous delivery cannot store a run twice.
- Measurements can be queried and sorted in SQL, and the table names follow the existing convention.
- Only a Manager reads results, and the check is in the API, so a changed page cannot bypass it.
- No new service and no new secret.

**Negative:**

- A run with many requests is many rows, and the answer text makes the table large; there is no cleanup yet.
- The 400 body is the envelope of this API, not the `Problem` object of the OpenAPI file; the benchmark service ignores the body, but the contract file should say so in a later version.
- A change of the result schema needs a new `schema_version`, a migration and a change of the validation.
- Whoever holds the service token can post any run; the token is the only check on the sender.

Quality attributes: reliability improves (idempotent receiving, no lost result); security is kept (least privilege, the check in the API); maintainability is neutral (one more aggregate that follows the existing pattern).

## Affected Artifacts

- [BC-001]
- [MIL-019]
- [ADR-0004]
- [ADR-0005]

---

[BC-001]: ../business-case.md
[MIL-019]: ../milestones/mil-019-benchmark-results-api.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[5e09a59]: https://github.com/NF-Hotel/NFHotel/commit/5e09a59285ead213d7e846b5367ee8daa6a00104
