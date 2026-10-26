# MIL-046 A finished benchmark run is never recorded as interrupted

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-046 |
| CrossReference | [BC-001], [ADR-0031], [MIL-043], [MIL-045], [US-001.04] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | S01 | Initial version; the fault found in the gate review of MIL-045 (RC-235), which S01 asked in chat on 2026-10-26 to have fixed ("ja sæt igang"); S01 accepted it in chat ("Jeg accepterer planen for MIL-046"); accepted after RC-238 Go | pending |

---

## Purpose

Decide whether NF Hotel may claim that the state of a recorded benchmark run is true. On 2026-10-26 run `a1878e80` finished at 07:20 and its result was stored, but no page asked for its state before a deploy restarted the benchmark service. Afterwards the service no longer knew the run, and the tracker of ADR-0031 recorded it as `interrupted`. The result is safe; the record is wrong, and the back office offers "Start again" for a run that is done.

## Deliverable

- The tracker asks the API whether a result with the run id is stored before it records `interrupted`; when one is, it records `done`.
- The record of run `a1878e80` in production is corrected to `done`, through the same rule, not by hand: a record that is `interrupted` may become `done` when the stored result proves it (an amendment of ADR-0031 decision 2).
- Tests of the tracker, the entity and the service.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0031 records the amendment: `interrupted` may become `done` when the result is stored, and nothing else changes a final state | Yes | No |
| 2 | A run that the service forgot and whose result is stored is recorded `done` (unit test of the tracker) | Yes | No |
| 3 | A run that the service forgot and whose result is not stored is still recorded `interrupted` (unit test) | Yes | No |
| 4 | After the deploy, run `a1878e80` is `done` in production and no longer listed under the started runs | Yes | No |
| 5 | CI is green and the review against `qc-programming-csharp` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-043 | The record of started runs and the tracker |
| MIL-045 | The first stored results, which showed the fault |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Criterion 25 (benchmark results) and US-001.04: a run is never shown wrongly | Tasks 1 to 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-02.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend ADR-0031 | Decision 2: `interrupted` may become `done` when a stored result proves the run finished; a new Version History row. | No | criterion 1 |
| 2 | Check for a stored result | `BenchmarkRunTracker` asks the API for the stored run before it records `interrupted`, and records `done` when it is there; a recent record that is already `interrupted` is checked the same way when a page lists the runs; the entity allows `interrupted` to `done`; tests. | No | criteria 2, 3 |
| 3 | Release and check | Merge, deploy, open a benchmark page so the record of `a1878e80` is brought up to date, and prove criterion 4; reviews. | No | criteria 4, 5 |

---

[BC-001]: ../business-case.md
[ADR-0031]: ../adr/adr-0031-state-that-survives-a-deploy.md
[MIL-043]: ./mil-043-state-across-deploys.md
[MIL-045]: ./mil-045-benchmark-results-volume.md
[US-001.04]: ../user-stories.md
