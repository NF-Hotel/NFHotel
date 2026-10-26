# MIL-043 Sign-ins and benchmark runs that survive a deploy

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-043 |
| CrossReference | [BC-001], [ADR-0005], [ADR-0013], [MIL-020], [US-001.04] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-26 for Data Protection keys that survive a deploy and benchmark runs that do not disappear on a restart ("Ja, planlæg MIL-043"), after the deploy of MIL-042 broke a page and lost a queued run; S01 accepted it in chat ("Jeg accepterer planen for MIL-043"); accepted after RC-226 Go | [9ea7f6f] |

---

## Purpose

Decide whether NF Hotel may claim that a deploy does not break the work of a member of staff. On 2026-10-26 the deploy of MIL-042 recreated every container. Two things were lost:

- **The Data Protection keys of the web application.** They live only in memory (`EphemeralXmlRepository`). After the restart every staff cookie and every antiforgery token is unreadable: a page that was open fails when a form is sent ("The key … was not found in the key ring"), and staff are signed out.
- **A queued benchmark run.** The benchmark service keeps its runs in memory. After its restart the run is gone, and the back office says only that no run has been stored.

Every release gives all four images a new digest, so this happens at every deploy.

## Deliverable

- ADR-0031: where the keys are kept and how they are protected, and how the hotel keeps track of a benchmark run that the benchmark service forgets, compared with the alternatives.
- The web application keeps its Data Protection keys in a Docker volume, under a fixed application name, so cookies and antiforgery tokens stay valid across a restart.
- The API stores every benchmark run it starts, with its choice, who started it and when. A run that the benchmark service no longer knows becomes `interrupted`. The runs page shows the runs that are queued, running or interrupted; the start page picks up a run that is still going; an interrupted run can be started again with the same choice.
- Tests, the wiki pages that describe the back office and the deploy, and the reviews.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0031 records the decisions and the alternatives, with the reasons | Yes | No |
| 2 | On `nf-hotel`, after the web container is recreated, a signed-in Manager is still signed in and a form opened before the restart is sent without an error; the web log has 0 "was not found in the key ring" after the restart | Yes | No |
| 3 | The keys are in a named volume, readable only by the user of the web application; the web container keeps its hardening (read-only root, all capabilities dropped), checked by the hardening test | Yes | No |
| 4 | A run started from the back office is stored by the API at once; after the benchmark container is restarted, the runs page shows it as `interrupted`, with its models and start time, and "Start again" starts the same choice | Yes | No |
| 5 | Leaving the start page and opening it again shows the run that is still going, and keeps following it | Yes | No |
| 6 | Unit tests cover the states of a run (allowed and refused transitions); integration tests cover storing, listing, `interrupted` on an unknown run, the key folder shared by two instances, and the migration; CI is green and the review against `qc-programming-csharp` and `qc-programming-shell` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-020, ADR-0013 | The benchmark service, its client in the API and the stored results |
| ADR-0005 | The staff sign-in whose cookie the keys protect |
| The node | Criteria 2, 4 and 5 are checked on `nf-hotel` |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Criterion 22 (back office login): staff are not signed out by a deploy | Tasks 1, 2 |
| Criterion 25 (benchmark results) and US-001.04: a run is never lost without a trace | Tasks 1, 3, 4 |

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
| 1 | ADR-0031 | Keys in a volume (against the database, a certificate, or no change) and their protection; a run record in the API with the state `interrupted` (against waiting in the deploy, or a queue that the benchmark service keeps itself in its own repository). | No | criterion 1 |
| 2 | Data Protection keys in a volume | `PersistKeysToFileSystem` with a fixed application name in the web application, the folder from its settings; a named volume in `ops/deploy/nfhotel-web`, owned by the user `app`; an integration test with two instances sharing the folder; the hardening test. | No | criteria 2, 3, 6 |
| 3 | A stored record of each benchmark run | Domain entity with the states queued, running, done, failed and interrupted, and its transitions; a table and a migration; the API stores the run when it starts it, updates it when it asks for the state, and marks it `interrupted` on an unknown run; the list of runs includes the open and interrupted ones. Extends US-001.04 with one acceptance criterion. | No (extends US-001.04) | criteria 4, 6 |
| 4 | The back office follows stored runs | The runs page shows queued, running and interrupted runs; the start page picks up a run that is still going; "Start again" for an interrupted run. bUnit tests of the view models; no GUI test on the host (Docker container, as `framework/process/gui-testing.md` says). | No | criteria 4, 5, 6 |
| 5 | Documents, release and check | The wiki pages Proxmox-Deployment (the benchmark) and CI-CD (what a deploy keeps); merge, follow the release and the deploy, prove criteria 2 to 5 on `nf-hotel`; reviews. | No | criteria 2 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0013]: ../adr/adr-0013-benchmark-results.md
[MIL-020]: ./mil-020-start-benchmark-run.md
[US-001.04]: ../user-stories.md
[9ea7f6f]: https://github.com/NF-Hotel/NFHotel/commit/9ea7f6f2a0f2544371a36b9f081a67f5025eec65
