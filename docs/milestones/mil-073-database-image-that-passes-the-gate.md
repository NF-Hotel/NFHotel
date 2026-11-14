# MIL-073 The database image that passes the gate

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-073 |
| CrossReference | [BC-001], [DSA-001], [ADR-0031], [ADR-0040], [MIL-024], [MIL-064], [MIL-065] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: the daily scan (issue #642) fails the database image `postgres:16-alpine` on seven high findings in `krb5-libs`; planned at S01's request in chat on 2026-11-11 ("Opfylder de det nu? Ellers fix det så de opfylder !") as a patch layer from the release, and changed before its acceptance to the upstream build of 2026-11-12, which has the fixed package (rule 1 of decision 5 of ADR-0040); accepted after RC-333 Go, at S01's word in chat ("Du godkender for mig") | [62301c4] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Gate: the five criteria pass after the deploy of CD run 43590287146, the row counts inside the VM and the daily scan run by hand (RC-362, Go); tasks 1 to 4 done; accepted at S01's word in chat ("Du godkender for mig") | [cee9c20] |

---

## Purpose

Make the database image that runs in production pass the gate of [DSA-001] section 8, as success criterion 27 of [BC-001] asks, by the first remedy that decision 5 of [ADR-0040] gives: the upstream build that passes the gate.

Read on 2026-11-14:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The daily scan of the release in production (issue #642, report of 2026-11-13) fails `postgres:16-alpine@sha256:aa6cb33c…`, PostgreSQL 16.15: seven high findings in `krb5-libs` 1.22.2-r1, each fixed in 1.22.3-r0 | The pin in `ops/deploy/postgres/docker-compose.yml` | Success criterion 27 is not met while the image runs |
| 2 | Upstream built the tag again on 2026-11-12 at 20:59 UTC: `postgres:16-alpine@sha256:f377d590…`, PostgreSQL 16.16, whose SBOM attestation lists `krb5-libs` 1.22.3-r0 | Docker Hub | Rule 1 of decision 5 applies, so the patch layer of rule 2 that the first version of this milestone planned is not needed. A scan with Trivy on the PC failed twice on the download of its vulnerability database, so the scan of the pull request decides |
| 3 | The findings in `gosu`, a Go binary of the image, are accepted by 26 ignore entries until 2026-12-08 ([MIL-065], rule 3) | `ops/security/trivyignore.yaml` | The image stays a third-party image, so the entries gate the daily scan and not the release; the first scan of the new build shows whether they still match |
| 4 | A new digest makes CD recreate the container of the database once, at the deploy that brings it ([ADR-0031]); 16.15 to 16.16 is a minor release, which keeps the data files | `ops/scripts/deploy-services.sh` | The database stops and starts once, after the backup that the deploy makes first; the data stays in its volume |

## Deliverable

- **The pin.** `ops/deploy/postgres/docker-compose.yml` pins `postgres:16-alpine@sha256:f377d590e5474681d4d7838635031a2b50647923b7abfa803ce9b5ff35d5c663`, or a newer build of the tag that passes the gate on the day of the change.
- **The documents.** The wiki page Containers-and-Images (the version and the rows of `gosu`), and the statements of the ignore file where they name something that changed.
- **The proof:** the scan of the new digest, the deploy with a check of the data inside the virtual machine (VM), and the daily scan without the findings.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 and 2 on one branch with their pull request, task 3 after the CD run of the merge is approved, and task 4 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The compose file pins the new build, and its scan with `ops/scripts/scan-image.sh` passes the gate of DSA-001 section 8 with `krb5-libs` 1.22.3-r0 or later | Yes | No |
| 2 | Every required check of the pull request is green, and the code review against `QC-YML-001` is Go | Yes | No |
| 3 | After the deploy, inside the VM, the database runs the new digest, reports PostgreSQL 16.16 or a later 16 release, and has the same row count in every table as before the deploy; the backup before the deploy succeeded | Yes | No |
| 4 | The next daily scan passes every image, release and third-party, and its issue closes | Yes | No |
| 5 | The wiki page Containers-and-Images says what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-024 | PostgreSQL 16 on the VM, its compose file and its volume |
| MIL-064 | The daily scan of the third-party images that opened issue #642 |
| MIL-065 | The remedies of the third-party images; the ignore entries of `gosu` |
| ADR-0040 | Decision 5, rule 1 |
| ADR-0031 | What a deploy keeps when it recreates a container |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 24 and success criterion 27: only scanned and attested images, 0 fixable high findings in the images that run | Criteria 1, 3 and 4 |
| BC-001 objective 40 and success criterion 43: the daily scan opens an issue and closes it when fixed | Criterion 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-18. It moves one pin and restarts the database once, so it fits within the week; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The pin and the documents | Scan the new build with `scan-image.sh`, then move the pin of the compose file to it; the wiki page Containers-and-Images and the statements of the ignore file | No | criteria 1 and 5 |
| 2 | Code review and pull request | The review record against `QC-YML-001`; the pull request, which closes the issues of tasks 1 and 2 | No | criterion 2 |
| 3 | The deploy and the checks | The CD run of the merge is approved. Before it, the AI assistant counts the rows of every table inside the VM over Tailscale Secure Shell (SSH), without printing a secret; after it, the digest, the version of PostgreSQL, the rows again and the log of the backup | No | criterion 3 |
| 4 | Gate and review record | The gate review record against criteria 1 to 5 with the next daily scan, the row of MIL-073 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0031]: ../adr/adr-0031-state-that-survives-a-deploy.md
[ADR-0040]: ../adr/adr-0040-patched-nginx-image-from-the-release.md
[MIL-024]: ./mil-024-postgresql-api-readiness.md
[MIL-064]: ./mil-064-patched-nginx-and-third-party-scan.md
[MIL-065]: ./mil-065-third-party-image-remedies.md
[62301c4]: https://github.com/NF-Hotel/NFHotel/commit/62301c485e2ba5305b590865c00d02ad24988e59
[cee9c20]: https://github.com/NF-Hotel/NFHotel/commit/cee9c202c9a01660c76140226b8db4cf67f1c61d
