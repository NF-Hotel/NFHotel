# RC-361 Code review of MIL-074: the copy off the VM within its time

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-361 |
| CrossReference | [MIL-074], [RC-334], [ADR-0008], [ADR-0018], [ADR-0020], [MIL-080], [QC-SH-001], [QC-YML-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green, the restart of criterion 5 by S01 | [d8275fa] |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-074] tasks 1 to 3, and the two files of task 4, on the branch
  `mil-074-off-vm-copy-every-hour`:
  - task 1: `ops/scripts/backup-database.sh` (`--copy-only` and the lock), the units
    `ops/deploy/systemd/nfhotel-offvm-copy.service` and `.timer`, `ops/scripts/install-backup.sh`, the update command
    of `ops/scripts/deploy-release.sh`, and the tests `ops/tests/test-backup-target.sh` and
    `ops/tests/test-deploy-release.sh`;
  - task 2: the report `backup_host_seen` in `backup-database.sh`, the rule `BackupTargetDown` in
    `ops/deploy/monitoring/prometheus/rules/nfhotel.yml` and its tests in `rules-test.yml`;
  - task 3: the wiki pages Disaster-Recovery, Alerts, Monitoring and CI-CD, and the row of 2026-11-14 of [ADR-0008];
  - task 4: `ops/deploy/backup-target/start-target.js` and `ops/deploy/backup-target/register-start-task.ps1`, the
    scheduled task of the backup host as code (F1).
- Checklist used: [QC-SH-001] (`qc-programming-shell`) for the shell files, [QC-YML-001] (`qc-yaml`) for the two rule
  files and, for the changed parts of [ADR-0008], [QC-ADR-001] (`qc-adr`). No workflow changed, so `qc-workflow` does
  not apply. The systemd units, the JScript file and the PowerShell file have no checklist of the framework; they are
  read against the shared rules of the skill `coding-conventions` and the deliverable of [MIL-074] below (F2).
- Scope: full review of the changed files; partial review of [ADR-0008] (decision 1, its current state and its row
  of 2026-11-14)
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig", with the merge and the approval of its CD run).

## Checklist Results

### QC-SH-001: `backup-database.sh`, `install-backup.sh`, `deploy-release.sh` and the two tests

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The three scripts keep both; the tests keep the convention of `ops/tests`, `set -uo pipefail`, with the comment on `ok` and `ko` |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The new code quotes every expansion; the units of `install-backup.sh` are a quoted list in one loop; shellcheck clean |
| 3 | Naming | Pass | `copy_off_vm`, `host_answers`, `copy_only`, `copy_units` in snake_case; `LOCK_WAIT_SECONDS` and `USAGE` as constants |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 `-x` over the five files: clean; `bash -n` passes; the one new `disable` (SC2016, the stand-in `tailscale` of the test writes its own `$*`) says why |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `die` writes `error:`; `--apply` with `--copy-only` gives the usage line and exit 2; a failed copy is a `warning:` on standard error, as before |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The tests work in their `mktemp -d` folder; the lock is `.lock` in the backup folder of the settings, not in `/tmp` |
| 7 | No secret in the script, its output or a command line | Pass | The copy logs the target and the times only; the output of `tailscale ping` is discarded; nothing new is read from the settings |
| 8 | A change of state outside its folder needs an explicit flag | Pass | `--copy-only` is explicit; without a flag the script only checks the settings, as before; `install-backup.sh` stays a dry run without `--apply` |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header names step 3 alone, the lock, the report `backup_host_seen`, the new option, the exit codes and the tools `flock` and `tailscale`; `install-backup.sh` and `deploy-release.sh` name the new timer |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-074] tasks 1 and 2; F1 records the hidden runner of task 4, F3 the stop of a copy that stalls |
| 11 | Success, failure and bypass paths tested | Pass | `test-backup-target.sh` in `debian:13-slim`: 44 of 44, with seven new checks: both modes together are refused; `--copy-only` sends 16 files the target lacks and makes, archives and deletes nothing on a VM that holds more daily files than it keeps; it reports `offvm_copy` and `backup_host_seen`, not `backup`; it skips its turn and sends nothing while the lock is taken; a backup waits while the lock is taken, then backs up and copies; a failed copy whose host answers reports `backup_host_seen` with the ping `-c 1 --until-direct=false --timeout 10s` to the host of `BACKUP_TARGET`, and one whose host does not answer reports nothing. Against the `backup-database.sh` before the change the same test gives 38 of 44: the six checks that need `--copy-only` or the lock fail, and the refusal of both modes passes there too, because that script refuses every unknown argument. `test-deploy-release.sh` in `debian:12-slim`: 106 of 106, with the new check that `update` installs both units and runs `enable --now nfhotel-offvm-copy.timer`, and 15 logged files instead of 13; against the `deploy-release.sh` before the change it gives 104 of 106, and those two are the checks that fail. `test-vm-setup.sh` in `debian:12-slim`: 19 of 19 |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci` over the five files: no difference |
| 13 | Safe to re-run | Pass | A copy sends only what the target lacks (`--ignore-existing`); a second `update` with equal files changes and logs nothing (the existing check); `install-backup.sh` installs the same units again |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `flock --nonblock` and `--wait` are options of util-linux, on the VM and in the test images; `tailscale` is optional and named |
| 15 | Each sequence, constant, path or message defined once | Pass | Step 3 is the one function `copy_off_vm`, which the backup and the hourly copy share; the time of the copy is set once, in the timer; the wait of the backup is the constant `LOCK_WAIT_SECONDS`, which the comment ties to `TimeoutStartSec` of the unit |

### QC-YML-001: `rules/nfhotel.yml` and `rules-test.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool reads both |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`, as `lint.sh` runs it: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The values of the series and the summary are quoted |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The expression of the rule is four lines |
| 8 | A repeated block defined once | Pass | The new tests follow the groups of `BackupStale` and `OffVmCopyStale` |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | `promtool test rules rules-test.yml` and `promtool check config` with `prom/prometheus:v3.15.1`, the pin of the monitoring: both SUCCESS |
| 11 | A header comment states purpose, tool and decision | Pass | The comment above the rule says what `backup_host_seen` is and why a host that is off raises nothing new |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The expression and the description are folded block scalars, as in the other rules |
| 14 | Lowercase kebab-case file names | Pass | Unchanged names |
| 15 | A check runs without the real target | Pass | The rule tests run in CI without the monitoring |

### QC-ADR-001: decision 1, the current state and the row of 2026-11-14 of [ADR-0008]

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces and the options evaluated | Pass | Unchanged; the new bullet makes decision 1 more frequent and does not choose another option |
| 3 | At least two options were considered | Pass | Unchanged; a second target that is always on stays out of scope, as [MIL-074] says |
| 4 | Decision stated in one or two unhedged sentences | Pass | "The copy is also tried every hour on its own" |
| 5 | Consequences list Positive and Negative outcomes | Pass | Unchanged; the current state names the alert that sees a target that is down within hours |
| 6 | Quality-attribute impacts named | Pass | Reliability (recoverability), unchanged |
| 7 | Affected Artifacts as links, each also in CrossReference | Pass | [MIL-074] in both, and [RC-361] in CrossReference |
| 8 | Status changes as new Version History rows; retained rows never rewritten | Pass | The row of 2026-11-14 is new; the row of 2026-10-22 stays as it was and becomes Deprecated; the oldest row and its link left the table |
| 9 | A superseded ADR names its successor | N-A | |
| 10 | Business or technical language, no unexplained abbreviations | Pass | |
| 11 | Status values from the allowed list | Pass | `Accepted` and `Deprecated` |

### The files without a checklist, against the shared rules and [MIL-074]

- **The units.** `nfhotel-offvm-copy.service` runs `nfhotel-backup --copy-only` as a oneshot with the hardening of
  `nfhotel-backup.service` (`Nice`, `IOSchedulingClass=idle`, `PrivateTmp`, `NoNewPrivileges`) and
  `TimeoutStartSec=30min` (F3); `nfhotel-offvm-copy.timer` runs it at half past each hour with
  `RandomizedDelaySec=5min` and `Persistent=true`: every hour, at most 5 minutes later, and at once after a boot that
  missed a run, as the deliverable says. `systemd-analyze verify` in `debian:13-slim` accepts both, and `systemd-analyze calendar` gives the next runs at 12:30, 13:30 and 14:30 UTC.
- **`start-target.js`.** Names say what (`COMMAND_NOT_STARTED`, `WINDOW_HIDDEN`, `WAIT_FOR_EXIT`), no magic number,
  the comment says why it exists (F1), and an error is not swallowed: a `docker.exe` that cannot start gives the task
  the result 1. Tried on the backup host on 2026-11-14: the task ran it with the result 0 while both services ran, and
  the same file in a folder without a compose file ended with 1.
- **`register-start-task.ps1`.** Named constants, `$ErrorActionPreference = 'Stop'`, it refuses a folder without
  `start-target.js` or `backup-target.env`, and `-Force` makes a second run replace the task. It stores no password:
  the logon type is `Interactive`, so the task runs only while its user is signed in. It registered the task on the
  backup host on 2026-11-14.

### The documents against the deliverable of [MIL-074]

- **Disaster-Recovery** has the section "The copy every hour and the backup target": the timer and how to check it on
  the VM, the signal, the scheduled task and how to set it up and check it, Docker Desktop and its `.stale` files, and
  why (the log of 2026-11-10).
- **Alerts** and **Monitoring** have `BackupTargetDown` with its threshold and what to do; Monitoring has the report
  `backup_host_seen.prom`, the hourly `offvm_copy.prom` and the journal of `nfhotel-offvm-copy.service` in Loki.
- **CI-CD** names the new timer and that the update step installs it.
- `test-wiki-coverage.sh` passes 117 of 117 (one more than before: the new alert on the page Alerts).

## The criteria of MIL-074 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | The new checks of `test-backup-target.sh` and `test-deploy-release.sh` above; Ops CI runs both |
| 2 | Pass | The two new groups of `rules-test.yml`: silent at 2 hours, firing at 3 hours, and silent when the host has not answered since the last copy; Ops CI runs promtool |
| 3 | Pending | Task 5, after the deploy: the timer inside the VM and the journal of one run |
| 4 | Pending | Task 5: the age of the newest copy and the rule in Prometheus, after the rules reach the monitoring host |
| 5 | Pending | Task 4: the task is created and its last result is 0 (2026-11-14 11:15 UTC); the one restart of Docker Desktop waits for S01 (F4) |
| 6 | Pending | The documents and the linters above pass, and this review is Go-with-conditions; the checks of the pull request |

## Findings of the review

- **F1, deviation: the task runs a hidden runner, not `docker compose` itself.** A console program that the task
  starts opens a window on the desktop of S01 every hour, and a console host started headless
  (`conhost.exe --headless`) always ends with 0: tried on 2026-11-14, `cmd /c exit 3` through it gave 0. Windows
  Script Host has no console, so `start-target.js` runs `docker compose up -d` hidden and ends with its exit code;
  `exit 3` through it gave 3. The script that registers the task is in the repository, so the task can be made again.
- **F2, note: three kinds of file have no checklist of the framework.** systemd units, JScript and PowerShell are read
  against the shared rules of `coding-conventions` and the deliverable. A checklist for them is a change of the
  framework, to propose upstream (AGENTS.md rule 5).
- **F3, design: a copy that stalls is stopped after 30 minutes.** The backup waits for the lock at most 45 minutes
  and then fails, so the unit stops a copy that hangs (`TimeoutStartSec=30min`) and gives the lock back first. A
  deploy that meets an hourly copy waits for it; a copy takes seconds when it sends only the new files.
- **F4, open: the restart of criterion 5 waits for S01.** On 2026-11-14 the AI's stop of Docker Desktop, to restart
  it once, was refused by the safety check of the session, so S01 restarts Docker Desktop once, or says so in chat.
- **F5, note: the cause on 2026-11-10 is not known.** The log of Docker Desktop of that day keeps only the user
  interface: the engine stopped at 11:25:59 UTC and ran again at 11:31 UTC. The logs of the engine had rotated away
  (the oldest from 2026-11-11 08:06 UTC), and the AI ran no Docker command in that window. The task starts the two
  services within the hour, whatever the cause.
- **F6, note: the `.stale` files of Docker Desktop.** A start of Docker Desktop 4.89 can stop on the 0-byte `.stale`
  files that an earlier start left; three such files were deleted before the start of 2026-11-14 11:05 UTC, and three
  new ones exist now. The wiki page says what to do; whether Docker Desktop starts at sign-in stays S01's decision.
- **F7, note: an older release and the update step.** The update command installs the units from the commit it gets,
  and dies when one is missing. A redeploy of a release older than this merge therefore stops at the update step, as
  one older than the container check of [MIL-080] already does; CD runs the update twice for a new deploy command, so
  the first CD run after the merge installs the units.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-YML-001] and [QC-ADR-001] passes, and criteria 1
and 2 of [MIL-074] pass. The row of 2026-11-14 of [ADR-0008] is Accepted and its row of 2026-10-22 Deprecated. The
conditions: every check of the pull request is green before the merge, and the restart of criterion 5 is done by S01.
The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green, after [MIL-075] (#746) | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge; read the timer and the journal of one run inside the VM over Tailscale SSH | S01 (the AI as stand-in) | 2026-11-18 |
| Bring the rules to the monitoring host and read the age of the newest copy and the rule in Prometheus | S01 (the AI on S01's words, by hand until MIL-051) | 2026-11-18 |
| Restart Docker Desktop once on the backup host, and check that both services run again within the hour | S01 | 2026-11-18 |
| Decide whether Docker Desktop starts at sign-in (F6) | S01 | 2026-11-18 |

---

[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[RC-334]: ./rc-334-mil-074.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[d8275fa]: https://github.com/NF-Hotel/NFHotel/commit/d8275fa8644f686d0bd751a5bc0ac2623fd0c916
