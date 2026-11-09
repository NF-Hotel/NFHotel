# RC-322 Gate review of MIL-069 Retention on the backup target

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-322 |
| CrossReference | [MIL-069], [RC-315], [RC-316], [MIL-066], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-09 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-069 after the redeploy on the backup host (task 5), with the log of the first prune and the copies of the VM since; Go | [53e3a9e] |
| 2026-11-09 | Accepted | mikemike | AI as stand-in for S01 | Finding 2: the command for the log of the prune needs `--env-file backup-target.env`; the page Disaster-Recovery corrected in the same pull request; the verdict stays Go | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-069] at its gate: the five Go/No-Go criteria, with the evidence of pull request #602 (merged as 53e0341 on 2026-11-08 at 19:16 UTC), of the redeploy on the backup host (task 5, issue #615), of the checks of 2026-11-09, and of a read-only read of the backup host and the VM on 2026-11-09 at 08:38 UTC. Before the gate: [RC-315] (the plan and the retention, accepted by S01) and [RC-316] (the code of tasks 1 to 3, Go).
- Checklist used: the Go/No-Go criteria of [MIL-069]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 1, 2 and 5 cite [RC-316] and the checks of #602 and of 2026-11-09, and criteria 3 and 4 the reads below. The pull request of this record changes no code; it corrects the command for the log of the prune on the wiki page Disaster-Recovery (finding 2).
- Language and domain: en / it (the Metadata rows of MIL-069); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 accepted the plan and the retention in chat ([RC-315]), asked for the last milestones in autonomous mode, and approved the redeploy of task 5 in chat on 2026-11-08. On 2026-11-09 S01 asked in chat to finish MIL-070 and MIL-069 ("færdiggør disse"). Asked then whether the AI should schedule this gate for 2026-11-21, after the first prune that deletes a file, S01 answered no and asked to finish the milestones that exist now ("Nej vi skal ik ha flere milestones, du skal færdiggøre dem der er på nuværende tidspunkt"). So this record reads the first prune, as criterion 4 and task 5 say, and not a pass after 2026-11-20, as the last action item of [RC-316] planned (finding 1).

**Evidence of 2026-11-08 and 2026-11-09:**

- **How the hosts were read.** On the backup host: `docker inspect`, `docker top`, `docker logs` and `id` of the service `prune`, and a list of the names in its backup folder. On the VM, over Tailscale Secure Shell (SSH): the journal of `nfhotel-backup.service` and the names in `/var/backups/nfhotel`. Both reads were read-only and printed names, times, counts and states only. No backup was opened, and no line of `sudo` was printed.
- **The pull request.** #602, on its last commit 2b76d46: 52 checks, of which 50 passed and `OWASP ZAP` and `DAST full scan` were skipped by design. Every check that the ruleset required at the merge passed (41). Of the 44 it has required since 2026-11-08 at 23:26 UTC, the two that did not exist yet, `Ops script tests (test-remove-desktop.sh)` of MIL-068 and `Ops script tests (test-sudo-log.sh)` of MIL-070, did not run on it. The milestone and its issues were published after the merge, at 19:59 UTC, so #611 to #614, the issues of tasks 1 to 4, were closed by hand at 20:00 UTC, as the action item of [RC-316] allowed.
- **The tests on 2026-11-09.** `Ops script tests (test-backup-target.sh)` ran again on 4222dc4, the last commit of #627, at 08:08 UTC in `debian:13-slim`: 37 passed, 0 failed. These are the 24 checks of the target and the copy, those of the write-only key of task 1 of [MIL-066] among them, and the 13 checks of the prune. `Ops linters` and `Ops wiki coverage` passed on the same commit.
- **The redeploy (task 5).** At S01's approval in chat, on 2026-11-08 at 20:04 UTC: the old image was kept as `nfhotel-backup-target:2026-11-08`, the files of `main` at b33d654 were copied in, and the image was rebuilt (65e6d5cb51ee). The host keys were kept (the digest of the public keys was unchanged), and `docker compose --env-file backup-target.env up -d --wait` started both services. A dry run before the switch deleted nothing. The prune user created and deleted a check file in the backup folder, so it can delete there. Issue #615 holds the steps.
- **The service `prune` on 2026-11-09 at 08:38 UTC** (Docker Engine 29.7.2, Compose 5.5.0). It has run since 2026-11-08 at 20:04:47 UTC with no restart, from the image 65e6d5cb51ee, as `/usr/local/sbin/prune-backups --apply --every 86400`. `docker inspect` gives:
  - the user `nfhotel-backup`;
  - the network mode `none`, with no network;
  - a read-only root file system and `no-new-privileges:true`;
  - `cap_drop` `ALL`, no added capability, and not privileged;
  - 64 MiB, 0.25 CPU and 32 processes;
  - the restart policy `unless-stopped`;
  - one bind mount, on `/backups`;
  - `PRUNE_DAILY_DAYS=30` and `PRUNE_WEEKLY_WEEKS=12`.

  `docker top` shows two processes, the script and its `sleep 86400`, as user ID 1000, which `id` in the container gives as `nfhotel-backup`.
- **The first prune.** The log of the service holds one pass, at 2026-11-08 20:04:47 UTC. Its first line gives the rules: daily files 30 days, weekly files 12 weeks, never fewer than the newest 14 daily and 8 weekly of each kind. The four kinds follow: 201 daily dumps, 56 weekly dumps, 112 daily and 56 weekly settings archives, each with all kept and 0 deleted. No file was past its limit. The oldest names were a daily dump of 2026-10-21 05:35:17 UTC (18 days old), a daily settings archive of 2026-10-24 21:43:16 UTC, and a weekly dump and a weekly settings archive of 2026-10-25 00:21:06 UTC. The service runs a pass about every 24 hours, at about 20:05 UTC.
- **The folder on 2026-11-09 at 08:38 UTC.** It holds 445 files (916 MB), all of the four kinds, and no other name, such as a temporary file of `rsync`. All 425 names listed before the redeploy are still there. The 20 new names are the dump and the settings archive of 10 backups, each named after the first prune:
  - nine weekly backups of Sunday 2026-11-08: two from each of four deploys, and one from `nfhotel-backup.service` at 21:23:55 UTC;
  - the daily backup of 2026-11-09 at 02:03:40 UTC, from the timer.

  No daily file older than 30 days and no weekly file older than 12 weeks lies beyond the newest 14 and 8 of its kind, at the first prune or at this read.
- **The copies of the VM since the first prune.** The first was at 20:25:58 UTC, during the deploy of b33d654 (#615). The journal of the VM holds two runs of `nfhotel-backup.service` since then, at 2026-11-08 21:23:55 and 2026-11-09 02:03:40 UTC. Each logs `==> copying the backups to` the target, has no warning, and ends with `Deactivated successfully`, and `.last-copy` on the VM says 2026-11-09T02:03:43Z. A deploy takes its backups and copies inside the deploy, so those are in the logs of the CD runs and not in the journal; their files are on the target. The 44 backups that the VM keeps, 14 daily and 8 weekly of each kind, are all on the target.

## Checklist Results

| # | Criterion (MIL-069) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `test-backup-target.sh` in `debian:13-slim` proves the retention (the limits to the second, the floors, the time from the name, no other name, the dry run), and each case fails against a script without the rule it tests | Pass | The 13 checks of the prune passed on #602 and again on 2026-11-09 (37 passed, 0 failed). In the mutation run of [RC-316], each of 13 copies of the script without one rule fails at least one check |
| 2 | The tests of the write-only key of task 1 of [MIL-066] pass unchanged: the key of the VM still cannot replace, delete or read back a file | Pass | They passed on #602 and on 2026-11-09; the diff of their part of the test is its header only ([RC-316]) |
| 3 | On the backup host, `docker inspect` shows the `prune` service without a network, as `nfhotel-backup`, with a read-only root file system and `no-new-privileges` | Pass | The network mode `none` with no network, the user `nfhotel-backup`, a read-only root file system and `no-new-privileges:true`; also `cap_drop` `ALL`, 64 MiB, 0.25 CPU and 32 processes, as on 2026-11-08 (#615) |
| 4 | The first prune with `--apply` on the backup host leaves no daily file older than 30 days and no weekly file older than 12 weeks beyond the newest 14 and 8 of each kind, deletes no other name, and names in its log each file it deleted; the next copy of the VM sends no file that the prune deleted | Pass | No such file at the first pass or at the read of 2026-11-09. The pass deleted no file, so it deleted no other name, and its log gives 0 deleted for each kind; the 425 names from before it are all there, and the 10 backups copied since have new names. The pass had nothing to delete, so the first deletion on the backup host is still to come (finding 1) |
| 5 | The code review against `QC-SH-001` and `QC-YML-001` is Go, `lint.sh` is clean, `test-wiki-coverage.sh` passes, and every required check of the pull request is green | Pass | [RC-316] Go, with `lint.sh` clean and `test-wiki-coverage.sh` 112 passed; `Ops linters` and `Ops wiki coverage` passed on #602, and so did every check that the ruleset required at its merge |

## Findings

1. **No file has been deleted on the backup host yet.** The first pass had nothing to delete, because the oldest copy was 18 days old (finding 6 of [RC-316]). Criterion 4 asks for the first pass, so it holds, but it does not show a deletion on the backup host. The first deletion comes with the first pass after 2026-11-20 05:35:17 UTC, when the daily dump of 2026-10-21 is past 30 days: at about 20:05 UTC that day, unless the container restarts before (a restart runs a pass at once and moves the time of day). The first daily settings archive follows on 2026-11-24, and the first weekly files on 2027-01-17. Until then, a deletion rests on three pieces of evidence:
   - the choice of the files, on the 13 checks and the mutation run (criterion 1);
   - the same image and compose settings, on the compose check of [RC-316], where `up -d prune` deleted and named 13 of 56 test files and kept 43;
   - the right to delete in the folder of the backup host, on the check file of task 5.

   [RC-316] planned this gate with the log of a pass after 2026-11-20; S01 asked on 2026-11-09 to finish the milestone now, without a gate scheduled for that date. `docker compose --env-file backup-target.env logs prune` on the backup host shows the first deletion when it comes (finding 2). If it fails, the log names the file (`error: could not delete <name>`), but nothing alerts until the disk of the backup host is full and the copy of the VM fails (finding 5 of [RC-316]).

2. **The command for the log of the prune needs the env file.** The page Disaster-Recovery and the comment of the service `prune` in the compose file gave `docker compose logs prune`. Run on the backup host on 2026-11-09, it stops before it shows anything: Compose first fills in the settings of the whole file, and `BACKUP_PUBLIC_KEY` and `BACKUP_TARGET_BIND` are required and are set only in `backup-target.env`. With `--env-file backup-target.env` it shows the log, as the other commands of the page and of the compose file already pass it. The pull request of this record corrects the page. The comment of the compose file keeps the short form until that file changes next: it changes no behaviour, and this pull request stays documents only.

## Overall Verdict

Go: the five criteria pass on the evidence of 2026-11-08 and 2026-11-09. The gate of MIL-069 is passed: issue #616 closes through the pull request of this record, and the GitHub milestone #51 is closed after the merge. The first deletion on the backup host is still to come, on 2026-11-20 (finding 1). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close the GitHub milestone #51 after the merge of this record, and check that #616 is closed | AI as stand-in for S01 | At the merge |

---

[MIL-069]: ../../milestones/mil-069-retention-on-the-backup-target.md
[RC-315]: ./rc-315-mil-069.md
[RC-316]: ./rc-316-mil-069-code.md
[MIL-066]: ../../milestones/mil-066-findings-of-7-november.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[53e3a9e]: https://github.com/NF-Hotel/NFHotel/commit/53e3a9e61c8a078f9472805a6616ae0533e5c569
