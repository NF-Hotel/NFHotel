# MIL-045 A benchmark results volume the service can always write

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-045 |
| CrossReference | [BC-001], [MIL-020], [MIL-025], [MIL-043], [US-001.04] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-26 to fix the owner of the volume `benchmark_results` on the server at once and to plan a lasting fix in the deployment ("ret ejeren … og plan en varig rettelse i deploy-scriptet") ; S01 accepted it in chat ("Jeg accepterer planerne for MIL-044 og MIL-045"; accepted after RC-231 Go) | [989973a] |

---

## Purpose

Decide whether NF Hotel may claim that a benchmark run can always store its result. On 2026-10-26 a run failed with `[Errno 13] Permission denied: '/data/results.jsonl'`. The volume `benchmark_results` was created on 2026-10-20 by an earlier image whose user had uid 1000; the image of MIL-025 runs as `app`, uid 10001. Docker sets the owner of a named volume only when it creates it, so no run has stored a result since, and nothing showed it: the container was healthy. The owner was corrected by hand on 2026-10-26 (`chown -R 10001:10001`); this milestone makes it hold for every future volume and every change of the image's user.

## Deliverable

- A one-shot service in `ops/deploy/benchmark/docker-compose.yml` that runs the benchmark image as root with only the capabilities to change owners, gives `/data` to the image's own user, and ends; the benchmark service starts only after it succeeded.
- A healthcheck of the benchmark service that also checks that `/data` is writable, so a volume it cannot write turns the container unhealthy and the deploy fails instead of every run.
- A test in the ops CI that starts the compose file with a volume owned by another user and shows that the service turns healthy and can write.
- The wiki page Incidents: the symptom, the cause and the fix.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | With a volume owned by uid 1000, `docker compose up --wait` of `ops/deploy/benchmark` turns the service healthy and `/data` is writable by its user (ops CI test) | Yes | No |
| 2 | With `/data` not writable, the service is unhealthy (ops CI test) | Yes | No |
| 3 | The one-shot service drops every capability except those that change owners, and the benchmark service keeps its hardening | Yes | No |
| 4 | After the deploy, a benchmark run from the back office ends `done` and its result is listed on `/admin/benchmark` | Yes | No |
| 5 | CI is green and the review against `qc-yaml` and `qc-programming-shell` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-020, MIL-025 | The benchmark service and its image |
| MIL-043 | A failed run is now shown with its reason, which revealed this fault |
| S01 | Starts the run of criterion 4 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Criterion 25 (benchmark results) and US-001.04: results are measured and kept | Tasks 1 to 4 |

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
| 1 | Volume owner set at every start | A one-shot `results-owner` service with the benchmark image, user 0, `cap_drop: ALL` and `cap_add: [CHOWN, FOWNER]`, that runs `chown -R` to the uid and gid of the image's user; the benchmark service `depends_on` it with `service_completed_successfully`. | No | criteria 1, 3 |
| 2 | Healthcheck that writes | The healthcheck of the benchmark service also creates and removes a file in `/data`. | No | criterion 2 |
| 3 | Ops CI test | `ops/tests/test-benchmark-volume.sh` with a stand-in image that runs as uid 10001: a volume owned by uid 1000 becomes writable and healthy; a read-only `/data` is unhealthy. Added to the ops CI matrix. | No | criteria 1, 2, 5 |
| 4 | Incident note, release and check | The wiki page Incidents; merge, deploy, a run that ends `done` and is stored; reviews. | No | criteria 4, 5 |

---

[BC-001]: ../business-case.md
[MIL-020]: ./mil-020-start-benchmark-run.md
[MIL-025]: ./mil-025-devops-repository-consolidation.md
[MIL-043]: ./mil-043-state-across-deploys.md
[US-001.04]: ../user-stories.md
[989973a]: https://github.com/NF-Hotel/NFHotel/commit/989973a4780193a81f30f3d06e76d25318142d36
