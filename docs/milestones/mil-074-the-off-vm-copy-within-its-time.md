# MIL-074 The copy off the VM within its time

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-074 |
| CrossReference | [BC-001], [DSA-001], [ADR-0008], [ADR-0020], [MIL-021], [MIL-023], [MIL-051], [MIL-069] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the newest copy off the VM was 54 hours old on 2026-11-11, because the copy is tried once a night and the backup target had stayed stopped for a day; planned at S01's request in chat ("Lav så backuppen er inden for tiden også fremadrettet. Du sørger for det."); the target moved from 2026-11-15 to 2026-11-18 on 2026-11-14, before its acceptance; accepted after RC-334 Go, at S01's word in chat ("Du godkender for mig") | [62301c4] |

---

## Purpose

Keep the newest copy of the backups of the production virtual machine (VM) on the backup host within the limit that the monitoring sets, 72 hours (alert `OffVmCopyStale`), whenever the backup host is on; and say within hours, not days, when the backup host is on but its target does not take the copy.

The daily backup ([ADR-0008], [MIL-021]) dumps the database, encrypts it, and copies every file that the backup host lacks to the write-only target there (decision D7 of [DSA-001]). Read on 2026-11-11:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The copy runs only inside the daily backup, at 02:00 UTC (at most 10 minutes later), and a copy that fails waits for the next night | `ops/scripts/backup-database.sh`, step 3; `nfhotel-backup.timer` | The age of the newest copy grows by whole days: a backup host that is off at 02:00 UTC on two nights brings the copy close to the limit, even when the host was on all day between them |
| 2 | Docker Desktop on the backup host was off in the night to 2026-11-10. It ran again on 2026-11-10 with the backup target, but it was restarted that day, and after the restart the two services of the backup target stayed stopped (restart policy `unless-stopped`) until they were started on S01's yes on 2026-11-11 at 08:27 UTC. The copies of the nights of 10 and 11 November failed: the newest copy was 54 hours old, and the copy of the night of 12 November lands close to 72 hours after the copy of 9 November | The backup target in Docker Desktop on the backup host | Why Docker did not start the two services again is not known; nothing on the backup host starts them when Docker does not |
| 3 | The only rule on the copy fires at 72 hours, and nothing tells a backup host that is off from a target that is down while its host is on | `ops/deploy/monitoring/prometheus/rules/nfhotel.yml` | A target that is down is seen only when the limit is reached. A weekend with the host off is normal (wiki page Alerts), so an earlier rule on the age alone would fire on most weekends |

## Deliverable

- **The copy every hour.** `backup-database.sh --copy-only` runs only the copy (step 3) and its report (step 5): it dumps nothing, archives nothing and deletes nothing. A service and a timer, `nfhotel-offvm-copy.service` and `nfhotel-offvm-copy.timer`, run it every hour (at most 5 minutes later, and at once after a boot that missed a run). One lock file keeps the two runs apart: the daily backup waits for the lock, and an hourly copy that finds the lock taken skips its turn, because the backup copies at its end. A target that comes back is caught up within the hour, not the next night.
- **The installation.** `install-backup.sh` installs and starts the new timer. The update command of continuous deployment (CD), `deploy-release.sh update`, installs a changed unit and starts the timer (`systemctl enable --now`), as it does for the health report, so the first CD run after the merge puts it in place.
- **The signal for a target that is down while its host is on.** When a copy fails, the script asks Tailscale whether the backup host answers (`tailscale ping`), and reports the time when it did as the task `backup_host_seen`, beside `offvm_copy` ([ADR-0020]). A new alert `BackupTargetDown`, severity warning, fires when the backup host has answered more than 2 hours after the last copy that succeeded: the host is on, but the hourly copies fail. A backup host that is off raises nothing new; `OffVmCopyStale` stays at 72 hours.
- **The backup target starts again by itself.** On the backup host: the reason why the two services did not start again on 2026-11-10, read in the log of Docker Desktop; a scheduled task of Windows, at sign-in and every hour, that runs `docker compose up -d` with the settings file in the folder of the backup target, which changes nothing when both services run and starts what has stopped; and the setting of Docker Desktop that starts it at sign-in, as S01 decides. The scheduled task is a standing change on S01's computer and is made only on S01's yes in chat.
- **The documents.** The wiki pages Disaster-Recovery (the hourly copy, the scheduled task and how to check both), Alerts and Monitoring (the new alert and what to do), and a Version History row of [ADR-0008] for decision D7: the copy is tried every hour.
- **Not part of this milestone:** a second target that is always on. With the hourly copy, the copy is within its time whenever the backup host is on at least once in 72 hours; a host that is off for longer still raises `OffVmCopyStale`, which is what that alert is for. A second target is a change of decision D7 and of the costs of [BC-001]; S01 decides on it at the gate.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch with their pull request, task 4 on the backup host on S01's yes, task 5 after S01 approves the CD run, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The tests show that `--copy-only` copies what the target lacks and writes `offvm_copy`, makes no dump or archive and deletes no file; that it skips its turn while the lock is taken; and that the update of CD installs the two units and starts the timer. Each test fails without the change, and Ops CI runs them | Yes | No |
| 2 | The rule tests show that `BackupTargetDown` fires when the backup host answered more than 2 hours after the last copy, and stays silent when the host has not answered since the last copy; Ops CI checks the rules with promtool | Yes | No |
| 3 | After the deploy, inside the VM, `nfhotel-offvm-copy.timer` is active with its next run within the hour, and the journal of a run shows the copy, or the reason why it failed | Yes | No |
| 4 | In Prometheus, at the gate, the newest copy is less than 2 hours old and the rule `BackupTargetDown` is loaded | Yes | No |
| 5 | On the backup host, after one restart of Docker Desktop on S01's yes, both services of the backup target run again within an hour without anyone starting them, and the last result of the scheduled task is 0 | Yes | No |
| 6 | The wiki pages and ADR-0008 say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-SH-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-021 | The daily backup and its copy off the VM |
| MIL-023 | The reports of the VM to the monitoring, and the rules on them |
| MIL-069 | The two services of the backup target, which the scheduled task starts |
| ADR-0008 | Decision D7, the copy off the VM, tried more often here |
| ADR-0020 | The reports as files that Alloy reads |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23 and success criterion 26: a backup whose restore is tested | Criteria 1, 3 and 4: the copy of every backup reaches the backup host within the hour while it is on |
| BC-001 objective 32 and success criterion 35: the backups off the VM within their time | Criteria 2 to 5: the copy within 72 hours whenever the backup host is on in that time, and a warning within hours when its target is down |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-18. A short window, because finding 2 can happen again at any restart of Docker Desktop: on 2026-11-14 Docker Desktop on the backup host had been closed since 2026-11-12, and the newest copy on the backup target was from 2026-11-12 at 16:51 UTC. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The copy every hour | Written first: tests in `ops/tests/test-backup-target.sh` that `backup-database.sh --copy-only` sends a new file to the stand-in target and writes `offvm_copy`, makes no dump or archive and deletes nothing, and skips its turn while the lock is taken; and a test in `ops/tests/test-deploy-release.sh` that `update` installs `nfhotel-offvm-copy.service` and `nfhotel-offvm-copy.timer` and starts the timer. Then `--copy-only` and the lock in `backup-database.sh`, the two units in `ops/deploy/systemd/`, and their installation in `install-backup.sh` and `deploy-release.sh update` | No | criterion 1 |
| 2 | The signal for a target that is down | Written first: rule tests in `ops/deploy/monitoring/prometheus/rules-test.yml` for `BackupTargetDown`, firing and silent. Then the report `backup_host_seen`: after a copy that succeeded, and after a failed copy when `tailscale ping -c 1` reaches the host of `BACKUP_TARGET`; and the rule in `rules/nfhotel.yml` | No | criterion 2 |
| 3 | The wiki, code review and pull request | Disaster-Recovery, Alerts and Monitoring; the Version History row of ADR-0008; the review record against `QC-SH-001` and `QC-YML-001`; the pull request, which closes the issues of tasks 1 to 3 | No | criteria 1, 2 and 6 |
| 4 | The backup target on the backup host | On S01's yes in chat: the AI assistant reads the log of Docker Desktop of 2026-11-10 and writes down why the two services did not start; creates the scheduled task (at sign-in and every hour, `docker compose up -d` with the settings file, as S01's user, with no password stored); reads the setting of Docker Desktop that starts it at sign-in and sets it as S01 decides; then, on S01's yes, restarts Docker Desktop once and checks that both services run again within the hour | No | criterion 5 |
| 5 | The deploy and the checks | S01 approves the CD run of the merge, which installs and starts the timer. The rules reach the monitoring host as the wiki page Monitoring says, by hand until [MIL-051] is built (S01, or the AI assistant on S01's words in chat). Then, over Tailscale Secure Shell (SSH) and without printing a secret, the AI assistant reads the timer and the journal of one run inside the VM, and the age of the newest copy and the rule in Prometheus | No | criteria 3 and 4 |
| 6 | Gate and review record | The gate review record against criteria 1 to 6, S01's decision on a second target, the row of MIL-074 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-051]: ./mil-051-monitoring-continuous-deployment.md
[MIL-069]: ./mil-069-retention-on-the-backup-target.md
[62301c4]: https://github.com/NF-Hotel/NFHotel/commit/62301c485e2ba5305b590865c00d02ad24988e59
