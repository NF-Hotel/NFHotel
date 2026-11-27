# RC-452 Gate review of MIL-091 The backup target answers on its port

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-452 |
| CrossReference | [MIL-091], [RC-450], [RC-451], [MIL-074], [MIL-069], [ADR-0044], [BC-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-091 after the installation (16:06 UTC) and the drill (19:09 to 20:10 UTC) on the backup host, with Prometheus read at the gate; the conditions of RC-451 met for pull request 891, and its F10 in the gate pull request; Go on criteria 1 to 5; F6 for S01: the quality gate of SonarQube Cloud on main is red on a test that MIL-091 does not touch, so CD deploys nothing until it is fixed; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-091] at its gate. The gate covers the five Go/No-Go criteria, with this evidence:
  - the code and the documents of tasks 1 and 2 on `main` since cb3d56f1 (pull request 891, merged on 2026-11-27 at
    15:22 UTC), reviewed in [RC-451];
  - the checks of pull request 891 on its last head (e0b83c14): 61 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 46013560988, NF Hotel E2E and DAST
    46013561121, NF Hotel Ops CI 46013560882, NF Hotel Release check 46013560863, CodeQL 46013557639 and code quality
    46013558056;
  - the runs of the merge commit cb3d56f1 on `main`: NF Hotel E2E and DAST 46024325811, NF Hotel Ops CI 46024325770,
    NF Hotel Wiki 46024325681, NF Hotel Scorecard 46024325720, CodeQL 46024325104 and code quality 46024325150 and
    46035166049 passed; NF Hotel CI 46024325711 failed on one test that is known to be flaky, and passed when its
    failed job ran again (F3);
  - the fix F10 of [RC-451] on the branch of this gate, `mil-091-gate` (189b7c82, with its commit links in c18e145b),
    run on the workstation: the test 29 of 29 with Node.js v24.18.0, `lint.sh` clean and `test-wiki-coverage.sh` 133
    of 133;
  - the installation of task 3 and the drill on the backup host (below);
  - the journal of `nfhotel-offvm-copy.service` on the VM, read over Tailscale SSH;
  - Prometheus, read through Grafana (below);
  - the runs of NF Hotel Release (46063115310) and NF Hotel CD (46063759377) of cb3d56f1 after the second attempt of
    NF Hotel CI (F3), and SonarQube Cloud through its public web API at about 20:33 UTC: the analyses of `main`, its
    quality gate and its open issues of new code (F6).

  Before the gate: [RC-450] (the plan, Go) and [RC-451] (the code, Go-with-conditions).
- Checklist used: the Go/No-Go criteria of [MIL-091]; [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 4), the conditions that [RC-451] left to it, and its F10, which reaches `main` with the gate
  pull request.
- Language and domain: en / it (the Metadata rows of MIL-091). The gate adds a row to the Version History of MIL-091.
  This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 switched the work on the accepted plan of MIL-091 to autonomous mode in chat
  on 2026-11-27 at 11:36 UTC ("Godkend pull requested osv i auto mode"). The AI that built the milestone made the
  checks of the gate and wrote this record; no other reviewer read it. Each step on the backup host had S01's yes in
  chat: "Ja, installer" for the installation; "Ja, nu", "Ja, afbrudt" and "Ja, forbundet" for the drill, in which S01
  took Tailscale down and up; and "Ja, start den nu" for one copy off the VM started by hand (F1).

**The installation (task 3).** On S01's "Ja, installer", on 2026-11-27:

- `start-target.js` and `register-start-task.ps1` of `main` at cb3d56f1 were copied into the folder of the backup
  target, and the old ones kept in its folder `previous-2026-11-27`;
- the task was registered again with `register-start-task.ps1 -Folder` and the folder, because the script, run as its
  header says, found no folder under Windows PowerShell 5.1 ([RC-451] F10);
- the triggers, read back: at sign-in with the delay `PT5M`, and every hour with the repetition `PT1H` and no end; the
  task runs with limited rights in the session of the user, with a limit of 10 minutes;
- a run started at 16:06 UTC ended with 0, and the service kept its start time, 09:34 UTC, and its published port. The
  hourly runs that followed left it alone too: at 19:09 UTC the service still ran since 09:34 UTC, and the run of
  19:00 UTC had ended with 0.

**The drill (task 3).** With S01 at the keyboard of the backup host. The AI read the state with a script that prints
no address: whether the host has the address of `BACKUP_TARGET_BIND`, whether something listens on it and on port
2222, the start time of the service, its ports in docker, and the last run of the task.

- 19:09 UTC, before: the host had its tailnet address, something listened on port 2222, the service had run since
  09:34 UTC, and the hourly run of 19:00 UTC had ended with 0. The last copy off the VM was at 18:32:11 UTC.
- At about 19:27 UTC S01 had taken Tailscale down ("Ja, afbrudt"): the host no longer had the address, while the old
  listener stayed until the restart.
- 19:27:41 UTC: `docker compose restart backup-target` in the folder of the target. The service ran from 19:27:42 UTC,
  `Up`, with only `22/tcp`: no published port and no listener, the state of 2026-11-24 to 2026-11-27.
- 19:28:09 UTC: one run of the task ended with 2 (the host does not have the address) and left the service alone.
- 19:31:09 UTC: the timed copy off the VM failed with `ssh: connect to host <address> port 2222: Connection timed
  out`, as expected while Tailscale was down, and the VM kept the backups ("the last copy off the VM was at
  2026-11-27T18:32:11Z").
- At about 19:52 UTC S01 had brought Tailscale up again ("Ja, forbundet"). At 19:52:32 UTC the host had the address,
  but nothing listened on port 2222 and the service still ran without its port: Docker does not publish it again by
  itself.
- 19:52:45 UTC: one run of the task restarted the service once (19:53:09 UTC). The port was published and answered on
  the tailnet address, and the run ended with 0.
- 20:10:09 UTC: on S01's "Ja, start den nu", the AI started `nfhotel-offvm-copy.service` on the VM, the service that
  the hourly timer starts. The journal: "copied off the VM" at 20:10:10 UTC.
- 20:31:34 UTC: the timed copy of the hourly timer, "copied off the VM" at 20:31:35 UTC.

**Prometheus at the gate.** Read through Grafana at 20:18:26 UTC:

- `time() - nfhotel_task_last_success_timestamp_seconds{host="nf-hotel", task="offvm_copy"}`: 497 seconds, the copy of
  20:10:10 UTC; `backup_host_seen` has the same time;
- `count_over_time(ALERTS{alertname=~"BackupTargetDown|OffVmCopyStale"}[6h])`: no series, so neither alert was pending
  or firing from 14:18 UTC on, through the whole drill;
- the rule `BackupTargetDown` in the running Prometheus: state normal, health ok, last evaluated at 20:19:27 UTC, with
  the expression and the new description of `main` (F2).
- read again at 20:34:04 UTC, after the timed copy: the newest copy 149 seconds old, the copy of 20:31:35 UTC, and
  still no series of `ALERTS` for either alert in the 6 hours before.

## Checklist Results

| # | Criterion (MIL-091) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The tests show that `start-target.js`: leaves a service whose port answers alone and ends with 0; restarts the service once when nothing listens while the host has the address, and ends with 0 when the port then answers; and ends with its own code, and no further restart, when the port still does not answer after the restart, when the host does not have the address, and when `docker compose up -d` fails. Each test fails against the script of `main` at 1d99f32f, and Ops CI runs them | Pass | [RC-451] row 11 and its criterion 1: the five cases in `start-target.test.js`, each failing against the script of `main` at 1d99f32f (4 of 29 checks pass there, none of them a case of criterion 1). Ops CI runs the test as `Ops script tests (test-start-target.sh)`, which passed on the last head of pull request 891 (46013560882) and on the merge (46024325770); with F10 on the gate branch, 29 of 29 |
| 2 | On the backup host, after task 3: the task has a trigger at sign-in with a delay of 5 minutes and an hourly trigger; and a run while the port answers leaves the service alone (its start time does not change) and ends with the last result 0 | Pass | The installation above: the trigger at sign-in with `PT5M` and the hourly trigger with `PT1H`; the run at 16:06 UTC ended with 0 and the service kept its start time of 09:34 UTC; the hourly runs after it left the service alone as well, its start time unchanged at 19:09 UTC, and the run of 19:00 UTC ended with 0 |
| 3 | A drill on the backup host, on S01's yes and with S01 at the keyboard: with Tailscale down, the service is restarted and its state is read; with Tailscale up again, one run of the task brings the port back, so that it answers on the tailnet address, and the next copy off the VM succeeds | Pass | The drill above: with Tailscale down, the restart left the service `Up` with only `22/tcp`, and a run of the task ended with 2 and changed nothing; with Tailscale up again, the port did not come back by itself, and one run at 19:52:45 UTC restarted the service once and brought the port back on the tailnet address, ending with 0. The next copy off the VM, at 20:10:09 UTC, succeeded; it was started by hand (F1). The timed copy that followed, at 20:31:34 UTC, succeeded too |
| 4 | In Prometheus, at the gate, the newest copy off the VM is less than 2 hours old, and `BackupTargetDown` and `OffVmCopyStale` are silent | Pass | Prometheus above: at 20:18:26 UTC the newest copy was 497 seconds old, and no series of `ALERTS` for either alert in the 6 hours before; the rule `BackupTargetDown` is normal. At 20:34:04 UTC, after the timed copy: 149 seconds, and still no series |
| 5 | The description of the rule and the wiki pages say what the deliverable says; `test-wiki-coverage.sh` and the rule tests pass; the code review against `QC-SH-001` (for the two Windows scripts, as [RC-361] did) and `QC-YML-001` is Go; `lint.sh` is clean; and every required check of the pull request is green | Pass | [RC-451] read the description and the wiki pages Disaster-Recovery, Alerts, Containers-and-Images and Monitoring against the deliverable, and the running Prometheus has the new description (F2). `test-wiki-coverage.sh` 133 of 133 and `lint.sh` clean on the gate branch; the rule tests (`promtool test rules`) passed in the job `Ops monitoring configuration` of Ops CI on the last head of pull request 891 and on the merge. [RC-451] is Go-with-conditions; its condition, every required check green and the new script test, held for pull request 891: 61 passed and 2 skipped by design on e0b83c14, and the quality gate of SonarQube Cloud passed with no new issue ([RC-451] F9). Its criteria 3 and 4 pass here, so it counts as Go. F10 reaches `main` with the gate pull request, whose checks are read before its merge (Action Items) |

## Findings

- **F1, deviation: the copy that ended the drill was started by hand, for a reason that was wrong.** Criterion 3
  ends with "the next copy off the VM succeeds". After the port came back at 19:53 UTC, the next timed copy was due
  between 20:30 and 20:35 UTC (`OnCalendar=*-*-* *:30:00 UTC` with `RandomizedDelaySec=5min`). The AI asked S01 to
  let it start one copy at once, and said that `BackupTargetDown` would otherwise fire at 20:32 UTC, two hours after
  the host was last seen; S01 said yes. That reason was wrong. The rule fires only when a run sees the backup host and
  still fails to copy more than 2 hours after the last copy
  (`backup_host_seen - ignoring(task) offvm_copy > 2 * 3600`); it does not fire on the time alone, and with the port
  back the timed copy had what it needed. The copy by hand ran the same service as the timer, which sends only what
  the target lacks, and changed nothing else. The timed copy at 20:31:34 UTC succeeded as well, so criterion 3 holds
  with either copy, and no alert fired.
- **F2, note: the new description already runs.** [RC-451] left an action for S01: bring the new description of
  `BackupTargetDown` to the host of the monitoring. The running Prometheus has it, with the expression of `main` (read
  through Grafana at 20:19 UTC), as the pull deploy of [ADR-0044] brings each change of the monitoring on `main`. The
  action is closed here.
- **F3, note: NF Hotel CI on the merge failed on a known flaky test.** In run 46024325711, at cb3d56f1, only
  `NFHotel.Api.Tests.BookingRequestApiTests.Start_SeriesOfTheMails_StandAtZero` failed (`Assert.Superset()`, with the
  series empty), in the job "Build, tests and security". The test races the start of the application; it failed in
  the same way on pull request 886 earlier the same day, and passed on the last head of pull request 891. The merge
  changes nothing under `src/` or `tests/`. Its failed job was started again at 20:17 UTC and passed in attempt 2 at
  20:21 UTC, and so did the job "Docker build" after it. While the first attempt stood, NF Hotel Release (46024700578)
  and NF Hotel CD (46024707828) of cb3d56f1 skipped; after the second, NF Hotel Release ran again (46063115310) and
  NF Hotel CD 46063759377 followed. S01 approved that run for `production` on GitHub from their own account at about
  20:29 UTC, and it deployed nothing: its step "Wait until every check is green" found the check SonarCloud Code
  Analysis of cb3d56f1 failed (F6), so the steps that join the tailnet and deploy on the VM were skipped. A fix of the
  flaky test is outside this milestone and needs a plan.
- **F4, note: an exited container beside the target.** On the backup host, `docker ps -a` lists `upbeat_bhabha`, a
  container of the image `nfhotel-backup-target:local` with a name that docker generated, created on 2026-11-27 at
  09:35:06 UTC; it ran for a quarter of a second and ended with 1. It holds no port and takes no copy, it is older
  than the plan of this milestone, and the AI leaves it; removing it is S01's call.
- **F5, note: the new check is still not required.** The rules of `main` require 44 checks, and
  `Ops script tests (test-start-target.sh)` is not among them, as [RC-451] F7 said it would be until S01 adds it. It
  ran and passed on pull request 891 and on the merge.
- **F6, note for S01: the quality gate of SonarQube Cloud on `main` is red since 20:19 UTC.** The analysis of `main`
  at cb3d56f1, at 20:19:48 UTC during the second attempt of NF Hotel CI, failed the quality gate on the reliability
  rating of new code, C where A is required, and turned the gate of `main` from green, at the analysis of 1fde1f2d at
  11:41 UTC, to red. The rating comes from nine bugs of the rule `python:S1244` (major: an equality check with a
  floating point value) on lines 89 to 169 of `ops/deploy/monitoring/scan-exporter/tests/unit/test_collector.py`, a
  test of the scan exporter that is unchanged since 2026-11-01 (MIL-053) and that this milestone does not touch.
  SonarQube Cloud gives the nine the date of their lines, 2026-11-01, and the time of this analysis as their last
  update. The new code of `main` is everything since the previous version, 2026-09-29, because no version is set, so
  they count as new code; the analysis of pull request 891 reported no new issue, because the new code of a pull
  request is its own changes. NF Hotel CD deploys a commit only when all its checks are green, so no commit of `main`
  deploys while the nine are open, the merge of this gate included, which deploys nothing new. Nothing is dismissed
  and no setting of the analysis changes; the fix of the test is outside this milestone, for S01 to plan.

## Overall Verdict

Go on criteria 1 to 5, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence
above: the tests of the five cases in Ops CI, the triggers and a run that leaves the service alone on the backup host,
the drill with S01 at the keyboard and the copies after it, Prometheus with the newest copy minutes old and both
alerts silent, and the documents, the reviews and the checks. F10 reaches `main` with the gate pull request, whose
required checks the AI reads before the merge. F6, the red quality gate of SonarQube Cloud on `main`, comes from a
test that this milestone does not touch; it holds back the next deploy, not this gate, and is for S01 to plan. The
reviewer is not independent, and each stand-in decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-091, or return them | S01; done on 2026-11-27 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Merge the gate pull request only with every required check green, and `Ops script tests (test-start-target.sh)`, which runs the check of F10 | AI as stand-in for S01 | Before the merge |
| Copy the `register-start-task.ps1` of F10 into the folder of the target once it is on `main`; the registered task needs no new registration | The AI (S01's yes to the installation) | After the merge |
| Plan the fix of the nine bugs of F6, so that CD deploys again; none is to be dismissed | S01 | Before the next deploy |
| Approve or cancel the CD run of the gate merge, if one waits; it deploys nothing new to the VM, and while F6 is open its check step refuses it | S01 | 2026-12-04 |
| Decide whether `Ops script tests (test-start-target.sh)` becomes a required check (F5) | S01 | When S01 decides |
| Decide whether the page Technology-Choices gets an entry for Node.js ([RC-451] F6) | S01 | When S01 decides |
| Decide on the exited container beside the target (F4) | S01 | When S01 decides |
| Plan a fix of the flaky test of F3, if S01 wants one | S01 | When S01 decides |

---

[MIL-091]: ../../milestones/mil-091-the-backup-target-answers-on-its-port.md
[RC-450]: ./rc-450-mil-091.md
[RC-451]: ./rc-451-mil-091-code.md
[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[MIL-069]: ../../milestones/mil-069-retention-on-the-backup-target.md
[ADR-0044]: ../../adr/adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[BC-001]: ../../business-case.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[RC-361]: ./rc-361-mil-074-code.md
