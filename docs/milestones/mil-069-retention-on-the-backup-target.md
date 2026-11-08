# MIL-069 Retention on the backup target

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-069 |
| CrossReference | [BC-001], [DSA-001], [ADR-0008], [MIL-021], [MIL-066], [RC-313] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version: nothing deletes on the backup target, found at the gate of MIL-066 (RC-313 finding 8); planned at S01's request in chat ("ja til alle", to "Skal jeg lave en plan for oprydning på backup-målet?") | [7ce3ef7] |

---

## Purpose

Decide how long the backup host keeps the copies of the backups of the production virtual machine (VM), and let the backup host delete older copies itself, while the key of the VM still cannot delete or replace anything there.

The copy off the VM (decision D7 of [DSA-001], [ADR-0008]) lands on a write-only target on the backup host: since task 1 of [MIL-066] the key of the VM may only add files (`rrsync -wo -no-del -no-overwrite`). The VM keeps its own 14 daily and 8 weekly files and deletes older ones, but nothing deletes on the target ([RC-313], finding 8). The folder of the target, read on 2026-11-08, gave these findings:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | 425 files, 841 MB, from 2026-10-21 to 2026-11-08, and nothing deletes any of them: 201 daily dumps (651 MB), 56 weekly dumps (182 MB), and 112 daily and 56 weekly settings archives (8 MB) | The folder of the backup target on the backup host | About 47 MB a day without end, until the disk of the backup host is full and the copy fails |
| 2 | Most files are backups taken before a deploy: 201 daily dumps in 18 days, because every continuous deployment (CD) run takes one before its migrations, and on a Sunday every backup is a weekly one (56 weekly dumps on three Sundays) | `ops/scripts/backup-database.sh` on the VM | A rule that keeps a number of files, as on the VM, keeps only a day or two on a day of many deploys; a rule by age keeps a known time |
| 3 | The copy sends every backup that the target lacks (`rsync --ignore-existing` over the folder of the VM) | `backup-database.sh`, step 3 | A file that the target deletes while the VM still keeps it comes back with the next copy, so the target must keep at least what the VM keeps |

## Deliverable

- **The retention, for S01 to accept with this plan:** on the target, daily files (dumps and settings archives) are kept for 30 days and weekly files for 12 weeks, counted from the time in the file name; and always at least the newest 14 daily and 8 weekly files of each kind, the numbers the VM keeps, so that finding 3 cannot happen.
- **A script `ops/deploy/backup-target/prune-backups.sh`**, a dry run by default: it reads the four kinds of file name (`nfhotel-daily-*.dump.age`, `nfhotel-weekly-*.dump.age`, `nfhotel-settings-daily-*.tar.age` and `nfhotel-settings-weekly-*.tar.age`), takes the time from the name, and with `--apply` deletes what the retention allows and names each file it deletes. It never touches another name, such as the temporary files of `rsync`.
- **A second service, `prune`, in the compose file of the backup target**, from the same image: it runs the script with `--apply` at start and then once a day, without a network, as `nfhotel-backup`, with a read-only root file system and `no-new-privileges`. The ssh service and the key of the VM do not change: the VM still cannot delete.
- **The wiki page Disaster-Recovery** and the comments of the compose file say how long the target keeps what, and how to change it.
- **The proof:** the tests, the code review, the redeploy on the backup host, and the log of the first prune.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch, task 4 with its pull request, task 5 on the backup host, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `test-backup-target.sh` in `debian:13-slim` proves the retention: a daily file 30 days old is kept and one a second older is deleted, and the same at 12 weeks for a weekly file; the newest 14 daily and 8 weekly files of each kind are kept whatever their age; the time comes from the name, not from the file; another name is not touched; the dry run deletes nothing; and each of these cases fails against a script without the rule it tests | Yes | No |
| 2 | The tests of the write-only key of task 1 of [MIL-066] pass unchanged: the key of the VM still cannot replace, delete or read back a file | Yes | No |
| 3 | On the backup host, `docker inspect` shows the `prune` service without a network, as `nfhotel-backup`, with a read-only root file system and `no-new-privileges` | Yes | No |
| 4 | The first prune with `--apply` on the backup host leaves no daily file older than 30 days and no weekly file older than 12 weeks beyond the newest 14 and 8 of each kind, deletes no other name, and names in its log each file it deleted; the next copy of the VM sends no file that the prune deleted | Yes | No |
| 5 | The code review against `QC-SH-001` and `QC-YML-001` is Go, `lint.sh` is clean, `test-wiki-coverage.sh` passes, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-066 | The target on Debian 13 with the write-only key (task 1), whose tests must keep passing, and the copy without `--partial` (task 2) |
| MIL-021 | The off-VM backup and its target (decision D7, task 7) |
| ADR-0008 | The backups are encrypted to a key that the VM does not hold, and a copy is kept off the VM |
| DSA-001 | Decision D7: a copy of the backups off the VM |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23 and success criterion 26: the database has a backup whose restore is tested, and a restore of the daily backup is recorded | Criteria 1 and 4: the target keeps 30 days of daily and 12 weeks of weekly backups, so a restore has a known history to choose from, and the copy keeps working because the disk does not fill |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-31, the date of the action item of RC-313; at about 47 MB a day the disk of the backup host is in no danger before then, and no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The prune script and its tests | `ops/deploy/backup-target/prune-backups.sh` as in the deliverable, and its cases in `ops/tests/test-backup-target.sh` (Debian 13), each first shown to fail against a script without the rule it tests. | No | criteria 1, 2 |
| 2 | The prune service | The service `prune` in `ops/deploy/backup-target/docker-compose.yml`, from the same image with the script copied in by the Dockerfile: no network, the user `nfhotel-backup`, a read-only root file system and `no-new-privileges`; the retention as settings in `backup-target.env.example`, with the defaults of the deliverable. | No | criteria 3, 4 |
| 3 | The wiki | The page Disaster-Recovery: how long the target keeps what, why the key of the VM cannot delete there, how the prune runs, and how to read its log. | No | criterion 5 |
| 4 | Code review and pull request | The review record against `QC-SH-001` and `QC-YML-001` before the pull request, which closes one issue per task it completes. | No | criterion 5 |
| 5 | The redeploy on the backup host | At S01's approval in chat, the artificial intelligence (AI) assistant rebuilds the target on the backup host as on 2026-11-08 (the volume of host keys kept, the old image under a date tag), reads the log of the first prune, and checks the next copy of the VM. | No | criteria 3, 4 |
| 6 | Gate and review record | The gate review record against criteria 1 to 5 with the test output, `docker inspect`, the log of the prune and the log of the copy, the row of MIL-069 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-066]: ./mil-066-findings-of-7-november.md
[RC-313]: ../sqa/reviews/rc-313-mil-066-gate.md
[7ce3ef7]: https://github.com/NF-Hotel/NFHotel/commit/7ce3ef7252f8780770d87bd2fa5c19c3f5ba9b99
