# RC-283 Gate review of MIL-046 A finished benchmark run is never recorded as interrupted

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-283 |
| CrossReference | [MIL-046], [ADR-0031], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; S01 accepted the plan to close the four open milestones in chat on 2026-11-05 ("Accepter planen") | [9a18561] |

---

## Artifact Under Review

- Instance reviewed: [MIL-046] at its gate: the five Go/No-Go criteria, with new evidence for criterion 4 read on `nf-hotel` on 2026-11-05. The earlier records are [RC-238] (the milestone against QC-MIL-001, Go) and [RC-239] (the code against QC-CS-001, Go).
- Checklist used: the Go/No-Go criteria of [MIL-046]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate (objectively checkable criteria).
- Scope: gate review of criteria 1 to 5. Criterion 4 is new evidence; criteria 1, 2, 3 and 5 cite RC-239 and the checks of pull request #378.
- Language and domain: en / it (the Metadata rows of MIL-046); this record is a technical type.
- Language reviewer: none
- Mode: autonomous, not independent. The AI wrote the milestone, the code review and this record; S01 asked in chat on 2026-11-05 to finish the open milestones and accepted the plan ("Accepter planen"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on `nf-hotel` on 2026-11-05 at 07:14 UTC** (Tailscale SSH in check mode, approved by S01 in the browser; read-only `psql` in the container `nfhotel-postgres-postgres-1`):

- `benchmark_request` of `run_id` `a1878e80-801f-4626-baaa-7bfe48e6067a` (`benchmark_request_id` 4): `state` `done`, `error` empty, `updated_at` 2026-10-27 07:36:34 UTC; requested on 2026-10-26 at 07:11:05 UTC by `manager`, models `llama3.1:latest` and `phi4-mini:latest`.
- `benchmark_run` of the same run: started 2026-10-26 07:11:05 UTC, finished 07:20:06 UTC, dataset `synthetic_hotel_bookings.csv` (3000 rows), backend ollama 0.40.6.
- Requests by state: `done` 3, `failed` 1, `interrupted` 2, `running` 1. None of the requests in `queued` or `running` is `a1878e80`.
- The release: pull request #378 merged on 2026-10-26 (`189f2fd`) with every check green, deployed by the CD of that day (automatic deploy, ADR-0021 as it then stood). The record changed to `done` on 2026-10-27 at 07:36 UTC, at the first page view that listed the runs after the deploy, as task 2 designed it.

## Checklist Results

| # | Criterion (MIL-046) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0031 records the amendment: `interrupted` may become `done` when the result is stored, and nothing else changes a final state | Pass | ADR-0031 decisions 2 and 3 with its Version History row of 2026-10-26, reviewed in RC-239 |
| 2 | A run that the service forgot and whose result is stored is recorded `done` (unit test of the tracker) | Pass | RC-239: the tests of `BenchmarkRunTracker`; the solution ran 1 567 tests with 0 failed |
| 3 | A run that the service forgot and whose result is not stored is still recorded `interrupted` (unit test) | Pass | RC-239: the same tests; a result that cannot be read counts as no proof |
| 4 | After the deploy, run `a1878e80` is `done` in production and no longer listed under the started runs | Pass | `state` `done` since 2026-10-27 07:36 UTC, read on 2026-11-05; the run is not among the requests in `queued` or `running`, which is what the back office lists as started |
| 5 | CI is green and the review against `qc-programming-csharp` is Go | Pass | Pull request #378 merged with every check green on 2026-10-26; RC-239 Go with no action item |

## Findings

- **The record changed a day after the deploy**, at the first page view that listed the runs, because the check runs when a page lists the runs and not on a timer. That is the design of task 2 and fits a back office that somebody opens daily; a run forgotten in the night is corrected the next morning.
- **Two other requests are `interrupted` and one is `running`.** They are not `a1878e80` and are outside this gate. The `running` one is either live or is checked by the tracker at the next page view, as criterion 2 and 3 describe.
- **The traceability matrix had no row for MIL-046, RC-238, RC-239 or ADR-0031** since 2026-10-26, nor for MIL-056, ADR-0035 and RC-271 to RC-273 since 2026-11-02. The rows are added with this record.

## Overall Verdict

Go. Every criterion passes, and the one that waited for production, criterion 4, is proven by the record itself. The milestone closes with the pull request of this record (task 3, issue #377). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close the GitHub milestone of MIL-046 once the pull request of this record is merged and #377 is closed | S01 (AI as stand-in) | At the merge |

---

[MIL-046]: ../../milestones/mil-046-finished-run-not-interrupted.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[RC-238]: ./rc-238-mil-046.md
[RC-239]: ./rc-239-mil-046-code.md
[9a18561]: https://github.com/NF-Hotel/NFHotel/commit/9a18561ca66b5c10d38d74ac1ffae259ca99e84a
