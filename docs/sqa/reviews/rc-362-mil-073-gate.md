# RC-362 Gate review of MIL-073 The database image that passes the gate

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-362 |
| CrossReference | [MIL-073], [RC-333], [RC-359], [DSA-001], [ADR-0031], [ADR-0040], [MIL-065], [MIL-074], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-073 after the CD run of the merge, the row counts inside the VM and the daily scan run by hand; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-073] at its gate. It covers the five Go/No-Go criteria, with this evidence:
  - pull request #693, merged as 606698e on 2026-11-14 at 11:17 UTC;
  - the release run 43589710098 of that commit;
  - the CD run 43590287146 of that commit;
  - the row counts inside the virtual machine (VM) before and after the deploy;
  - the daily scan 43591366298, run by hand after the deploy.

  Before the gate: [RC-333] (the plan) and [RC-359] (the code, Go-with-conditions).
- Checklist used: the Go/No-Go criteria of [MIL-073]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criteria 1, 2 and 5 cite RC-359 and the checks of #693. Criteria 3 and 4 rest on the deploy, the
  counts inside the VM and the scan.
- Language and domain: en / it (the Metadata rows of MIL-073). The language of the document was reviewed in RC-333, and
  the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14 to the AI in chat ("Du godkender
  for mig"), with the merge and the approval of the CD run. The AI wrote the plan, the change and this record.

**Evidence of 2026-11-14:**

- **The pull request.** #693 on its last commit e019bb2: 54 checks passed. `DAST full scan` and `OWASP ZAP` were
  skipped by design (they run only by hand and on schedule). All 44 required checks of `main` passed. The AI merged it
  at 11:17 UTC, as the plan allows.
- **The release.** Run 43589710098 of 606698e logged "PASS … no secret, no critical finding and no high finding with a
  fix" for each of the six release images between 11:24 and 11:26 UTC. The database image is a third-party image, so
  the release does not build or scan it; the daily scan does (criterion 4).
- **The count before the deploy.** At 11:31:57 UTC, before the approval, a read-only query over Tailscale Secure Shell
  (SSH) counted the rows of every table into a file on the VM: PostgreSQL 16.15 on
  `postgres:16-alpine@sha256:aa6cb33c…`, 15 tables, SHA-256 of the file `966a23d58731d703…`. The session ran with
  `default_transaction_read_only=on`, and no setting or secret was printed.
- **The deploy.** CD run 43590287146 was approved by the AI as stand-in for S01 after the count, and ended with success
  at 11:41:26 UTC.
  - The update step found the commands equal to 606698e.
  - The backup before the migrations dumped and archived at 11:33:33 UTC and logged "copied off the VM" at 11:39:24 UTC
    (F2); the backup before the deploy did the same from 11:39:25 to 11:39:51 UTC.
  - At 11:39:52 UTC CD pulled `postgres:16-alpine@sha256:f377d590…` and recreated the container of the database, which
    was healthy at 11:40:08 UTC.
  - The check of the containers found no secret in an environment and 11 of 11 containers within their profiles. The
    smoke test answered 301 on port 80 and 200 on port 443. The run logged "deployed 606698e…" at 11:41:20 UTC.
- **The count after the deploy.** At 11:42:10 UTC the same query on the running container:
  `postgres:16-alpine@sha256:f377d590e5474681d4d7838635031a2b50647923b7abfa803ce9b5ff35d5c663`, PostgreSQL 16.16, 15
  tables, SHA-256 `966a23d58731d703…`; `cmp` found the two files identical. The three files of the counts were then
  deleted from the VM.
- **The daily scan.** Run 43591366298 of NF Hotel running image scan, started by hand at 11:43 UTC without
  `test_image` (F1), found the release in production at 606698e and scanned 17 images from 11:43 to 11:48 UTC: the six
  release images and the eleven third-party images that the compose files pin at its operations commit, among them
  `postgres:16-alpine@sha256:f377d590…`. It found nothing that the gate refuses, said so in a comment on issue #642 and
  closed it at 11:48:21 UTC.

## Checklist Results

| # | Criterion (MIL-073) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The compose file pins the new build, and its scan with `ops/scripts/scan-image.sh` passes the gate of DSA-001 section 8 with `krb5-libs` 1.22.3-r0 or later | Pass | [RC-359] criterion 1: `scan-image.sh` passed `postgres:16-alpine@sha256:f377d590…` with `krb5-libs` 1.22.3-r0 on 2026-11-14; `ops/deploy/postgres/docker-compose.yml` on `main` pins that digest; the daily scan passed it again (criterion 4) |
| 2 | Every required check of the pull request is green, and the code review against `QC-YML-001` is Go | Pass | All 44 required checks of #693 passed; [RC-359] is Go-with-conditions, and its one condition, every check of the pull request green, is met |
| 3 | After the deploy, inside the VM, the database runs the new digest, reports PostgreSQL 16.16 or a later 16 release, and has the same row count in every table as before the deploy; the backup before the deploy succeeded | Pass | The counts above: the digest `f377d590…`, 16.16, 15 tables with identical counts; both backups of the CD run copied off the VM |
| 4 | The next daily scan passes every image, release and third-party, and its issue closes | Pass | Run 43591366298: the 17 images pass, the database image among them, and issue #642 closed at 11:48:21 UTC (F1) |
| 5 | The wiki page Containers-and-Images says what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | [RC-359]: the row of `postgres` names PostgreSQL 16.16, the build of the tag of 2026-11-12, and the row of the accepted `gosu` findings stays, as the binary is the same ([RC-359] F2); `test-wiki-coverage.sh` passed 116 of 116, and the check `Ops wiki coverage` of #693 passed |

## Findings

- **F1, note: the daily scan ran by hand.** Criterion 4 names the next daily scan. The AI ran the workflow by hand right
  after the deploy, with no `test_image`, so it ran the same steps on the release in production as the schedule does.
  The scheduled run of 2026-11-15 follows, and it opens the issue again if a finding comes back.
- **F2, note: the first copy of the deploy caught up.** The backup target had been down since 2026-11-12, while Docker
  Desktop was closed on its host; it ran again on 2026-11-14 from 11:05 UTC. The backup before the migrations therefore
  sent every backup the target lacked, which took about six minutes, and the next one 26 seconds. [MIL-074] makes the
  copy run every hour, so a target that comes back gets what it lacks within the hour.
- **F3, note: one restart and no rebuild.** The database was down from the recreate at 11:40:01 UTC to its health at
  11:40:08 UTC. 16.15 to 16.16 is a minor release, which keeps the data files, and no index needed a rebuild ([RC-359]
  F3); the counts are identical.
- **F4, note: the counts stay on the VM.** The evidence keeps the comparison and the hash, not the counts of the
  tables, which would say how many guests and bookings the hotel has. The files were deleted after the comparison.

## Overall Verdict

Go. The five criteria pass on the evidence of 2026-11-14. S01 gave the acceptance to the AI in chat on 2026-11-14
("Du godkender for mig"), and the AI accepted this record and the gate row of MIL-073 as stand-in for S01; the gate of
MIL-073 has passed. Its pull request closes issues #696 and #697, and with them the GitHub milestone #59. The reviewer
is not independent: the AI wrote the plan, the change and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-073, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Renew or remove the 26 ignore entries of `gosu` before they expire | [MIL-065], its own action | 2026-12-08 |

---

[MIL-073]: ../../milestones/mil-073-database-image-that-passes-the-gate.md
[RC-333]: ./rc-333-mil-073.md
[RC-359]: ./rc-359-mil-073-code.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
