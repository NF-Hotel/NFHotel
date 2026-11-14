# MIL-076 Every nightly backup read back

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-076 |
| CrossReference | [BC-001], [ADR-0008], [ADR-0020], [ADR-0025], [MIL-021], [MIL-023], [MIL-074] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | S01 | Initial version: a dump is proven readable only by the monthly restore drill with the key of S03, and nothing reads the dump of a night while it is made; planned at S01's request in chat ("Skriv 1–4", to the plan of 2026-11-12) | [62301c4] |

---

## Purpose

Know every morning that the backup of the night can be read, not once a month: read each dump back to its end while it is made, count its tables, and warn when a dump cannot be read or is much smaller than the ones before it. The dump must never be written in clear text to the disk of the production virtual machine (VM), and the check itself must not stop a deploy until S01 has seen it run.

The daily backup ([ADR-0008], [MIL-021]) dumps the database inside the postgres container and encrypts the stream with age to the public key of S03, so the VM cannot read its own backups. The monthly restore drill ([ADR-0025]) proves the restore on another machine, with the private key. Read on 2026-11-12 at 1c2726b:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | `pg_dump --format=custom` runs inside the postgres container, and its stream goes straight into `age`; nothing reads the dump back | `ops/scripts/backup-database.sh`, steps 1 and 2 | A dump that ends early, or an archive that `pg_restore` cannot read, is found only at the next drill, up to a month later, or at a real restore |
| 2 | The restore is proven once a month: the drills of 2026-10-20 and 2026-11-05 are recorded with Go (RC-505, RC-284), and the next is due by 2026-12-05 | `ops/scripts/restore-drill.sh` | The drill proves the procedure and one backup a month, not the backups between |
| 3 | The VM cannot decrypt a backup, by the decision of ADR-0008 | `AGE_RECIPIENT` in `/etc/nfhotel/backup.env` | A check of the encrypted file is impossible on the VM; the check must read the stream before `age`, and keep it off the disk |
| 4 | Every deploy runs the backup first, and stops when it fails | `ops/scripts/deploy-release.sh deploy` | A check that fails by mistake would stop every deploy |

## Deliverable

- **The dump read back while it is made.** `backup-database.sh` passes the stream of `pg_dump` through `tee` to two readers in the same postgres container: `pg_restore --list`, which lists the archive and gives the number of tables, and `pg_restore --file=/dev/null`, which reads every block to the end and fails on a dump that is cut short or damaged. The clear stream exists only in the pipes, and `age` encrypts the same bytes as before.
- **The report.** The time of the last dump that was read back, as the task `backup_check` beside `backup` ([ADR-0020]); and the number of tables and the size of the encrypted file, as `nfhotel_backup_tables` and `nfhotel_backup_bytes`.
- **Two alerts, both warnings.** `BackupUnreadable`: a backup succeeded after the last read-back that succeeded. `BackupShrunk`: the newest dump has fewer tables than the most of the last 8 days, or is less than half the size of the largest of the last 8 days.
- **Report first, then a decision.** A failed read-back keeps the dump and lets the backup succeed, so a deploy goes on; it reports and alerts only. At the gate, after at least seven nights with no false alarm, S01 decides whether a failed read-back from then on fails the backup, and so stops a deploy.
- **The documents.** The wiki pages Disaster-Recovery (what the read-back proves, and what only the monthly drill proves), Alerts and Monitoring (the two alerts, and what to do), and a Version History row of [ADR-0008].
- **Not part of this milestone:** a restore of every night into a database. It would need a second PostgreSQL on the VM and the time of a whole restore every night; the read-back proves the archive, and the monthly drill keeps proving the restore and its row counts.

Order: after [MIL-074], which changes the same script. The review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch with their pull request, task 4 after S01 approves the run of continuous deployment (CD), and task 5 after seven nights.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The tests of `backup-database.sh` show, against the PostgreSQL 16 of `test-database.sh`, that a whole dump is read back and reports `backup_check`, the number of tables and the size; and, with a fake docker, that a dump cut short or unreadable fails the read-back and reports no `backup_check` while the backup itself succeeds, that `age` receives the same bytes with and without the read-back, and that no file holds the clear dump. Each test fails without the change, and Ops CI runs them | Yes | No |
| 2 | The rule tests show that `BackupUnreadable` and `BackupShrunk` fire in their cases and stay silent otherwise; Ops CI checks the rules | Yes | No |
| 3 | After the deploy, inside the VM, the journal of the nightly backup shows the read-back with the number of tables, and the backup took at most 5 minutes longer than in the night before the change | Yes | No |
| 4 | In Prometheus, `backup_check` is less than 26 hours old, `nfhotel_backup_tables` equals the number of tables of the live database, and both rules are loaded | Yes | No |
| 5 | Seven nights in a row with a read-back and no false alarm, and S01's decision on whether a failed read-back stops the backup, recorded in the gate review | Yes | No |
| 6 | The wiki pages and ADR-0008 say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-SH-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-021 | The daily backup and its encryption |
| MIL-023 | The reports of the VM to the monitoring, and the rules on them |
| MIL-074 | The same script gets `--copy-only` and the lock there; this milestone comes after it |
| ADR-0008 | The VM cannot read its own backups |
| ADR-0020 | The reports as files that Alloy reads |
| ADR-0025 | The recovery objectives and the monthly drill, which stays |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23 and success criterion 26: a backup whose restore is tested | Criteria 1, 3 and 4: every daily backup read back in the night it is made |
| BC-001 objective 32 and success criterion 35: the backups off the VM within their time, and every dump read back (in the Proposed revision of BC-001, RC-332) | Criteria 2, 4 and 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-28: after MIL-074 (2026-11-15), the change, and seven nights with the read-back. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The read-back in the backup | Written first: with a fake docker, as in `ops/tests/test-backup-settings.sh`, tests for a dump cut short, an unreadable archive, the same bytes to `age` and no clear file; and in `ops/tests/test-database.sh`, a whole dump of PostgreSQL 16 read back with its number of tables. Then the `tee` with the two `pg_restore` readers in `backup-database.sh`, and the reports `backup_check`, `nfhotel_backup_tables` and `nfhotel_backup_bytes` | No | criterion 1 |
| 2 | The two alerts | Written first: rule tests in `ops/deploy/monitoring/prometheus/rules-test.yml` for `BackupUnreadable` and `BackupShrunk`, firing and silent. Then the rules in `rules/nfhotel.yml` | No | criterion 2 |
| 3 | The documents, code review and pull request | Disaster-Recovery, Alerts and Monitoring; the Version History row of ADR-0008; the review record against `QC-SH-001` and `QC-YML-001`; the pull request, which closes the issues of tasks 1 to 3 | No | criteria 1, 2 and 6 |
| 4 | The deploy and the checks | S01 approves the CD run of the merge. The rules reach the monitoring host with `deploy-monitoring.sh --apply`, as the wiki page Monitoring says. Over Tailscale Secure Shell (SSH) and without printing a secret, the AI assistant reads the journal of the next nightly backup and its length, counts the tables of the live database, and reads `backup_check`, the two metrics and the rules in Prometheus | No | criteria 3 and 4 |
| 5 | Seven nights and the gate | After seven nights: the alerts of those nights; S01's decision on whether a failed read-back stops the backup (if yes, a small change with its test before the gate); the gate review record against criteria 1 to 6, the row of MIL-076 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0025]: ../adr/adr-0025-recovery-objectives.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-074]: ./mil-074-the-off-vm-copy-within-its-time.md
[62301c4]: https://github.com/NF-Hotel/NFHotel/commit/62301c485e2ba5305b590865c00d02ad24988e59
