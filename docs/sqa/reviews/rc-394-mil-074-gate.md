# RC-394 Gate review of MIL-074 The copy off the VM within its time

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-394 |
| CrossReference | [MIL-074], [RC-334], [RC-361], [ADR-0008], [ADR-0020], [BC-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-16 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-074 after the restart of the backup host, the hourly copies inside the VM and the read of Prometheus; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-074] at its gate. It covers the six Go/No-Go criteria, with this evidence:
  - pull request #747, merged as 6d45a2b on 2026-11-14 at 11:48 UTC, reviewed in [RC-361] (Go-with-conditions);
  - the CD run 43592788461 of that commit (2026-11-14, 12:01 to 12:05 UTC), which installed and started the timer;
  - the timer and the journal of the hourly copy inside the virtual machine (VM) on 2026-11-16;
  - two reads of Prometheus on the monitoring host on 2026-11-16;
  - the restart of the backup host on 2026-11-16 and the scheduled task there.

  Before the gate: [RC-334] (the plan, Go) and [RC-361] (the code, Go-with-conditions).
- Checklist used: the Go/No-Go criteria of [MIL-074]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (tasks 4 to 6). Criteria 1, 2 and 6 cite [RC-361] and the checks of #747. Criteria 3, 4 and 5 rest
  on the evidence of 2026-11-16 below.
- Language and domain: en / it (the Metadata rows of MIL-074). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14 to the AI in chat ("Du
  godkender for mig"), and on 2026-11-16 asked it to finish the open milestones in autonomous mode and to close each
  when it is done ("du skal i automode lave alle disse milestones", "færdiggør dem alle og luk dem når de er 100%
  done"). The restart of the backup host and the three decisions below are S01's own, in chat. The AI wrote the plan,
  the change and this record.

**Evidence of 2026-11-16:**

- **The hourly copy inside the VM.** Read over Tailscale Secure Shell (SSH) at 06:37 UTC, without printing a secret:
  `nfhotel-offvm-copy.timer` was active, its last run at 06:34:10 UTC and its next at 07:30:03 UTC. The journal of
  `nfhotel-offvm-copy.service` showed "copied off the VM" at 22:33:35 UTC on 2026-11-15 and at 06:34:15 UTC. Each run
  from 23:31 to 05:32 UTC failed with the reason in the journal: "ssh: connect to host … port 2222: Connection timed
  out", then "warning: the last copy off the VM was at 2026-11-15T22:33:35Z". The backup host was off or asleep in
  those hours (F2). The daily backup of 02:00 UTC read its dump back (15 tables) and its own copy failed in the same
  way. The first hourly copy after the host woke sent it at 06:34:15 UTC, four and a half hours later, not the next
  night.
- **Prometheus, first read (07:33:59 UTC).** Read over SSH on the monitoring host:
  - `offvm_copy` was at 06:34:15 UTC (1.00 hour old), and `backup_host_seen` at the same time;
  - the rules `BackupTargetDown` and `OffVmCopyStale` of `nfhotel.yml` were loaded, inactive, with health `ok`.
- **The restart of the backup host.** S01 restarted the backup host and said so in chat ("Ja, genstartet"). It was a
  restart of Windows: the engine of Docker Desktop ended at 07:28:11 UTC, and Windows started at 07:28:41 UTC.
  - Docker Desktop did not start at sign-in, because its start at sign-in was off: its setting `AutoStart` was false,
    and its Windows startup entry was disabled (F1).
  - The scheduled task `\NFHotel\Backup target up` ran at sign-in, at 07:29:01 UTC, and ended with 1, because the
    engine was not running. That is the result it is built to give ([RC-361] F1).
  - Docker Desktop started again at 07:35:14 UTC; the AI did not start it. Both services of the backup target,
    `backup-target` and `prune`, ran again at 07:35:22 UTC by their restart policy `unless-stopped`. No one started
    them.
  - The task's hourly run at 08:00:01 UTC ended with 0. Read at 08:41 UTC: both services still ran, started at
    07:35:22 UTC with no restart since, `AutoStart` was true and the startup entry of Docker Desktop enabled.
- **S01's decisions in chat (2026-11-16).** Three answers in one dialog:
  - Docker Desktop starts at sign-in: yes. The AI enabled the startup entry of Docker Desktop under S01's user at
    07:37:51 UTC, and Docker Desktop's own watcher set `AutoStart` to true at the same second.
  - A second target that is always on: "Nej, ikke nu" (F3).
  - The AI may delete the three `.stale` files of Docker Desktop and start it. Neither was needed (F4).
- **The copy after the restart.** The hourly copy that started at 07:30:10 UTC, while the backup host was starting
  again, failed after 20 seconds: "ssh: connect to host … port 2222: Connection timed out", then "warning: the last
  copy off the VM was at 2026-11-16T06:34:15Z". The next run of the timer started at 08:34:10 UTC and wrote "copied
  off the VM" one second later. No one started it (F6).
- **Prometheus, second read (08:41:05 UTC).** Read over SSH on the monitoring host:
  - `offvm_copy` and `backup_host_seen` were at 08:34:11 UTC (0.12 hours old);
  - the daily backup and its check of the read-back were at 02:00:42 UTC, and `nfhotel_backup_tables` was 15;
  - the rules `OffVmCopyStale`, `BackupTargetDown`, `BackupUnreadable` and `BackupShrunk` of `nfhotel.yml` were
    loaded, inactive, with health `ok`.

## Checklist Results

| # | Criterion (MIL-074) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The tests show that `--copy-only` copies what the target lacks and writes `offvm_copy`, makes no dump or archive and deletes no file; that it skips its turn while the lock is taken; and that the update of CD installs the two units and starts the timer. Each test fails without the change, and Ops CI runs them | Pass | [RC-361] criterion 1: `test-backup-target.sh` 44 of 44, 38 of 44 without the change; `test-deploy-release.sh` 106 of 106, 104 of 106 without it. Both jobs of Ops script tests passed on #747 |
| 2 | The rule tests show that `BackupTargetDown` fires when the backup host answered more than 2 hours after the last copy, and stays silent when the host has not answered since the last copy; Ops CI checks the rules with promtool | Pass | [RC-361] criterion 2: the two new groups of `rules-test.yml`, silent at 2 hours, firing at 3, silent when the host has not answered; the check Ops monitoring configuration (MIL-023) passed on #747 |
| 3 | After the deploy, inside the VM, `nfhotel-offvm-copy.timer` is active with its next run within the hour, and the journal of a run shows the copy, or the reason why it failed | Pass | CD run 43592788461 installed and started the timer on 2026-11-14. On 2026-11-16 the timer was active with its next run within the hour, and the journal showed the copies and, for each failed run, the reason (above) |
| 4 | In Prometheus, at the gate, the newest copy is less than 2 hours old and the rule `BackupTargetDown` is loaded | Pass | Read at 08:41:05 UTC: the newest copy, `offvm_copy`, was 0.12 hours old, and `BackupTargetDown` was loaded, inactive, with health `ok`. The first read, at 07:33:59 UTC, gave 1.00 hour |
| 5 | On the backup host, after one restart of Docker Desktop on S01's yes, both services of the backup target run again within an hour without anyone starting them, and the last result of the scheduled task is 0 | Pass | S01 restarted the backup host on 2026-11-16, a restart of Windows at 07:28 UTC. Docker Desktop was up again at 07:35:14 UTC, and both services ran at 07:35:22 UTC by their restart policy, without anyone starting them, well within the hour. The last result of the scheduled task, its run at 08:00:01 UTC, was 0. Its run at sign-in gave 1, because Docker Desktop did not start at sign-in then; it does now (F1) |
| 6 | The wiki pages and ADR-0008 say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-SH-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Pass | [RC-361] read Disaster-Recovery, Alerts, Monitoring, CI-CD and the row of 2026-11-14 of [ADR-0008] against the deliverable; `test-wiki-coverage.sh` passed 117 of 117. On #747 the checks Ops wiki coverage and Ops linters (`lint.sh`) passed, with 52 more; DAST full scan was skipped and OWASP ZAP neutral, by design. [RC-361] is Go-with-conditions; both conditions are met (F5) |

## Findings

- **F1, note: the start at sign-in is set, and not yet seen.** The restart of 2026-11-16 was a restart of Windows,
  and Docker Desktop stayed closed after the sign-in, because its start at sign-in was off. The backup target was
  down from 07:28:11 to 07:35:22 UTC. The task's run at sign-in reported it with the result 1. Since S01's yes,
  Docker Desktop starts at sign-in, but no sign-in has shown it yet; the next restart of the backup host does. If it
  does not start, `BackupTargetDown` warns about 2 hours after the last copy while the host answers, and
  `OffVmCopyStale` at 72 hours.
- **F2, note: the backup host sleeps at night.** The hourly copies from 23:31 to 05:32 UTC timed out because the
  backup host did not answer, and no alert fired. A host that is off raises nothing new, as the deliverable says. The
  copy of the night reached the host at 06:34 UTC, with the first copy after the host was back.
- **F3, decision: no second target now.** S01 decided in chat on 2026-11-16 that no second target that is always on
  is added now. Decision D7 of [ADR-0008] and the costs of [BC-001] stay as they are.
- **F4, note: the `.stale` files stayed.** S01 allowed the AI to delete the three `.stale` files before a start. When
  the AI looked, at 07:36 UTC, Docker Desktop had already started, and that start had gone through with the files in
  place: it moved the sockets of 2026-11-14 over them. So nothing was deleted.
- **F5, note: the conditions of the code review are met.** [RC-361] gave Go-with-conditions: every check of the pull
  request green before the merge, and the restart of criterion 5 by S01. #747 merged with every check green, and S01
  restarted the backup host on 2026-11-16. The review counts as Go for criterion 6.
- **F6, note: one hourly copy fell in the restart.** The copy of 07:30 UTC timed out while the backup host was
  starting again, and the next run of the timer, at 08:34 UTC, sent it. A copy that fails in such a window is
  taken by the next hourly run, without anyone acting, as the deliverable says.

## Overall Verdict

Go. Criteria 1 to 6 of [MIL-074] pass. While the backup host is on, the hourly copy keeps the newest copy off the
VM less than two hours old, and `BackupTargetDown` warns when the host answers but no copy comes. After the restart
of the backup host, the backup target ran again by itself. S01 decided that no second target is added now (F3).
The row of 2026-11-16 of [MIL-074] is Accepted and its row of 2026-11-14 Deprecated. The reviewer is not
independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-074, or return them | S01; done on 2026-11-16 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read NF Hotel CI of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| At the next restart of the backup host, see that Docker Desktop starts at sign-in (F1) | S01 | At the next restart |

---

[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[RC-334]: ./rc-334-mil-074.md
[RC-361]: ./rc-361-mil-074-code.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[BC-001]: ../../business-case.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
