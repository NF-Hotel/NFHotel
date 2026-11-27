# RC-451 Code review of MIL-091: the backup target answers on its port

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-451 |
| CrossReference | [MIL-091], [RC-450], [RC-361], [MIL-074], [MIL-069], [ADR-0008], [ADR-0037], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Deprecated | mikemike | AI as stand-in for S01 | F8: the four findings of SonarQube Cloud on the pull request, fixed in the code; the test renamed `test-start-target.js`, with one case more; the verdict stays Go-with-conditions<br>Accepted by the AI as stand-in for S01 in autonomous mode; mode autonomous, not independent | [61cd97b] |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | F9: the second analysis of SonarQube Cloud took the renamed test for production code and failed the quality gate (security rating D); the test is `start-target.test.js` again and its checks run as tests of `node:test`; the verdict stays Go-with-conditions<br>Accepted by the AI as stand-in for S01 in autonomous mode; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-091] tasks 1 and 2 in the worktree of the branch `mil-091-port-check`, from
  `main` at 1fde1f2d, before any commit, and the fixes of the findings of SonarQube Cloud on its pull request (F8 and
  F9):
  - task 1: `ops/deploy/backup-target/start-target.js` (the check of the port and the one restart),
    `ops/deploy/backup-target/register-start-task.ps1` (the delay at sign-in), the new test
    `ops/tests/start-target.test.js` (named `test-start-target.js` in the commit of F8, F9) with its wrapper
    `ops/tests/test-start-target.sh`, and its entry among the script tests of `.github/workflows/ops-ci.yaml`;
  - task 2: the description of `BackupTargetDown` in `ops/deploy/monitoring/prometheus/rules/nfhotel.yml` and its
    expected text in `rules-test.yml`, and the wiki pages Disaster-Recovery, Alerts, Containers-and-Images and
    Monitoring (F2).
- Checklist used: [QC-SH-001] (`qc-programming-shell`) for the wrapper and, as criterion 5 of [MIL-091] asks, for the
  two Windows scripts and the Node.js test, each criterion read with the equivalent of the language where it names
  bash (F1); [QC-YML-001] (`qc-yaml`) for the two rule files and `ops-ci.yaml`; and [QC-WF-001] (`qc-workflow`) for
  the change of `ops-ci.yaml`. The wiki pages have no checklist of their own; they are read against the deliverable
  of [MIL-091].
- Scope: full review of the new and changed files. Of `ops-ci.yaml` only the new entry of the script tests and its
  comment changed; the rest of the workflow is unchanged.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who switched the work on the accepted
  plan of [MIL-091] to autonomous mode in chat on 2026-11-27 at 11:36 UTC, "Godkend pull requested osv i auto mode",
  that is, approve the pull request and what follows in autonomous mode).

## Checklist Results

### QC-SH-001: `test-start-target.sh`, and `start-target.js`, `register-start-task.ps1` and `start-target.test.js`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The wrapper has both and ends with `exec node`, so its exit code is the one of the test. The equivalents: `register-start-task.ps1` keeps `$ErrorActionPreference = 'Stop'`; the test runs in `"use strict"`; JScript 5.8 has no strict mode, so every call that can throw is in a `try` that ends the run with a result of its own (row 5) |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The wrapper quotes `"$(dirname "${BASH_SOURCE[0]}")"`; shellcheck clean. In the script, the paths of `docker.exe`, the folder and the settings file are quoted in the command line of `docker compose`, and the port goes into the query of WMI only after it is checked to be 1 to 5 digits and at most 65535 (`isPort`), so the query takes no other text |
| 3 | Naming | Pass | `test-start-target.sh` in kebab-case, with the prefix `test-` of the other tests of `ops/tests`; the Node.js test `start-target.test.js` in kebab-case with the suffix `.test.js`, by which SonarQube Cloud reads it as a test file (F9). In the JScript, constants in UPPER_SNAKE (`PORT_ANSWERS`, `NO_ADDRESS`, `TCP_STATE_LISTEN`, `PORT_CHECKS`) and functions in the camelCase of JavaScript, as verbs or questions (`readTarget`, `mendPort`, `hostHasAddress`, `hasListenerWithin`, `composeSucceeds`); `$signInDelay` in the camelCase of the PowerShell file |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 through `lint.sh`: clean, no `disable`; `bash -n` passes. `node --check` passes for the test. The PowerShell parser gives no error for `register-start-task.ps1`, and Windows Script Host loads the new `start-target.js` (the probe in row 11) |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | The wrapper takes no argument. The script has no console, so each failure ends the run with a result of its own, which the task shows as its last result, and none is swallowed: `docker compose` that fails or `docker.exe` that cannot start gives 1, a host without the address 2, a port that still does not answer after the restart 3, and a settings file that cannot be read or gives no valid address or port, or a WMI that does not answer, 4. Each check of the test is a test of `node:test`, which reports a failed check with the result and the steps of its case, and the run ends with 1 (F9) |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | N-A | No file is written: the test works in memory, and the script only reads its settings file |
| 7 | No secret in the script, its output or a command line | Pass | `backup-target.env` holds no secret (the public key of the VM, the address, the port and the limits of the retention), and the script reads only the address and the port. The test uses the placeholder address of `backup-target.env.example` and an address of a home network; no real address is in the change |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The only change the script makes is the start of a stopped service, as before, and one restart of the service `backup-target` of its own folder, only while the host has the address and nothing listens on the port; a run while the port answers changes nothing. The task exists to do this ([RC-361], F1), and S01 registers it by hand; `register-start-task.ps1` replaces the task only when it is run, as its header says |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | `start-target.js`: what it does and why, how the task runs it, the results 0 to 4, where the addresses and the listeners come from, the dialect and the tests. The wrapper and the test: purpose, how to run them, how to compare with older copies, exit codes and what they need. `register-start-task.ps1`: the delay and why |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-091] tasks 1 and 2; F2 records the page Monitoring, which the deliverable does not name; F3 the reading of a quoted value, changed in this review; F8 and F9 the findings of SonarQube Cloud, fixed after it |
| 11 | Success, failure and bypass paths tested | Pass | `test-start-target.sh` in `node:26-trixie-slim` (Node.js v26.13.1): 28 of 28, again after F9. The five cases of criterion 1 of [MIL-091]: a port that answers gives 0 with no restart; no listener while the host has the address gives one restart, then 0; no listener after the restart either gives 3 and no second restart, while a listener on another address and a connection that is not a listener do not count; a host without the address gives 2 with no restart; `docker compose up -d` that fails gives 1 with nothing checked. Then: `docker.exe` that cannot start and a restart that fails give 1; the port of `BACKUP_TARGET_PORT` (2223), and 2222 when it is missing or empty; a comment and quotes read as docker compose reads them (F3), and white space after a value and a long run of it before a comment left out (F8); an address that is not one, no address, port 65536, port 0, an empty file and no file give 4 with nothing checked; WMI that does not answer gives 4; every command runs hidden and waited; a port that answers at once costs no wait; no run waits more than 60 seconds in all; only what JScript 5.8 has; and the delay of 5 minutes at sign-in with the hourly trigger kept in `register-start-task.ps1`. Against the two files of `main` (unchanged since 1d99f32f) the same test gives 4 of 28, and each case of criterion 1 fails; the 4 that pass check what [MIL-074] already did (hidden and waited, `docker.exe` that cannot start gives 1, JScript 5.8 only, and no wait in a script that waits for nothing). A read-only probe in Windows Script Host on the backup host, last run 2026-11-27 13:53 UTC after F8, loaded the functions of the new script without its last line, so no `docker compose` ran: the live settings file is read (port 2222), the host has the address of `BACKUP_TARGET_BIND`, something listens on that address and port, nothing listens on port 1, a quoted value with a comment after it reads as the text inside the quotes, and `trim` takes 6 ms for a line of 40003 characters. The two triggers, built in memory with Windows PowerShell 5.1 and not registered: `MSFT_TaskLogonTrigger` takes the delay `PT5M`, and the hourly trigger repeats every `PT1H` with no end |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 through `lint.sh`: no difference. The project has no formatter for JavaScript; both files keep the two-space layout of the file of [MIL-074] |
| 13 | Safe to re-run | Pass | A run while the port answers changes nothing; a run restarts at most once, and the next run checks again; `register-start-task.ps1` with `-Force` replaces the task |
| 14 | Bash version and tools stated; GNU-only options named | Pass | The wrapper and the test need bash and Node.js 20 or later (`node:test`, stable since Node.js 20, and `Object.hasOwn`), as their headers say; the script needs Windows Script Host, `docker.exe` of Docker Desktop and WMI, as its header says |
| 15 | Each sequence, constant, path or message defined once | Pass | The command line of `docker compose` is built once (`compose`) and run through one function (`composeSucceeds`); the wait for the port is one function (`hasListenerWithin`), used after the start and after the restart; each result and limit is a named constant. The test builds every case from one host (`host`) and from `backup-target.env.example` (`settingsWith`), so the example and the test cannot drift |

### QC-YML-001: `rules/nfhotel.yml`, `rules-test.yml` and `.github/workflows/ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool reads both rule files; actionlint reads the workflow |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; `git ls-files --eol` gives `i/lf w/lf` for every changed file |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`, as `lint.sh` runs it: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The new values are a file name, an image (`node:26-trixie-slim`, a string) and a folded description |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | No new program text; the expression of the rule is unchanged |
| 8 | A repeated block defined once | Pass | The new entry uses the `include` of the matrix, as the six entries with Debian 13 do. The description of the rule and its expected text in `rules-test.yml` are the same words, because promtool compares them |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | `check-monitoring.sh`: `promtool check config` and `promtool test rules rules-test.yml` with `prom/prometheus:v3.16.0`, the pin of the monitoring, both SUCCESS, and its other checks pass (the check of the dashboards run with relative paths on the Windows host); actionlint 1.7.7 reads the workflow |
| 11 | A header comment states purpose, tool and decision | Pass | The comment above `BackupTargetDown` now names a target that runs without its port and the task that restarts it ([MIL-091]); the comment above the entries with their own image says why the new one runs Node.js 26 on Debian 13 |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The description is a folded block scalar (`>-`) in both rule files |
| 14 | Lowercase kebab-case file names | Pass | Unchanged names |
| 15 | A check runs without the real target | Pass | The rule tests and the script test run in CI without the monitoring and without Windows |

### QC-WF-001: the new entry of the script tests of `ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Pass | Above |
| 2 | In `.github/workflows/`, `.yml` or `.yaml`, in one directory only | Pass | Unchanged |
| 3 | `name`, `on` and `jobs`; explicit events | Pass | Unchanged: pull requests to `main`, pushes to `main`, and by hand |
| 4 | Header: purpose, triggers, inputs, secrets, changes outside the repository, decision | Pass | Unchanged; it names the script tests and says "Secrets: none; nothing is pushed, and nothing outside the runner changes" |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | The name of the job, `Ops script tests (${{ matrix.test }})`, gives the new check the name `Ops script tests (test-start-target.sh)` |
| 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7 through `lint.sh`, named in the header; `test-workflow-policy.sh`: "the 15 workflows and actions ... follow the policy (ADR-0037)" |
| 7 | Explicit `permissions`, least access; write only on the job that needs it | Pass | Unchanged: `contents: read` for the workflow, which the job needs and no more |
| 8 | Actions pinned to a full commit SHA | Pass | No new action; `actions/checkout` keeps its pin |
| 9 | No secret in the file; through `env:` to the step that needs it, never on a command line | Pass | None is used |
| 10 | No untrusted text interpolated into `run:` | Pass | The test and the image reach the step through `env:` (`TEST`, `IMAGE`), from the matrix of the file, as before |
| 11 | No `pull_request_target` or `workflow_run` on untrusted code | N-A | Neither trigger |
| 12 | A workflow that changes state outside the repository is started by hand or by a protected branch | N-A | Nothing outside the runner changes |
| 13 | The token has only the scope the job needs; the built-in token where it is enough | Pass | The built-in token, read only |
| 14 | `runs-on` and `timeout-minutes` on every job | Pass | `ubuntu-24.04` and 15 minutes for the job of every script test; the new test takes seconds |
| 15 | `concurrency` where two runs would conflict | Pass | Unchanged: one group per workflow and ref, `cancel-in-progress: true` |
| 16 | Triggers filtered to the changes they concern | Pass | Unchanged: it runs on every pull request because its jobs are required checks, as the header says |
| 17 | Tools at pinned versions, each version in one place | Pass | `node:26-trixie-slim`, a tag of a long-term support line of Node.js on Debian 13, in one place, the matrix, as the `debian:12-slim` and `debian:13-slim` of the other entries; nothing is installed in it |
| 18 | A dependency cache where the install is slow | N-A | Nothing is installed |
| 19 | A failing step names the tool, the file and the reason | Pass | The wrapper runs the test with the spec reporter of `node:test`, which prints each failed check with its name, its result and its steps, and ends with the exit code of the test (F9) |
| 20 | `run:` within the limit; a longer script is a file called with one command | Pass | Unchanged: one `docker run` line |
| 21 | Shared logic is a reusable workflow or a composite action | Pass | The entry reuses the one step of the matrix |
| 22 | Every feature verified on the instance that runs it; host and version recorded | Pass | GitHub.com, as the header says. An entry of `include` with an image of its own is what six entries already use; the image `node:26-trixie-slim` runs on GitHub first in the checks of this pull request, which the condition of the verdict covers |
| 23 | A reusable workflow states how a caller pins it | N-A | Not reusable |
| 24 | A static test of the workflows runs on every pull request | Pass | `test-workflow-policy.sh` and actionlint, in Ops CI |

### The documents against the deliverable of [MIL-091]

- **The rule.** The description of `BackupTargetDown` names a target that runs without its published port, as after a
  start before the host had its tailnet address, says that the scheduled task restarts it within the hour, and
  otherwise what to do; the comment above the rule says the same. `rules-test.yml` expects the new text, and promtool
  passes.
- **Disaster-Recovery** says what the task does now (5 minutes after sign-in and every hour: `up -d`, then the check
  of the address and the port through WMI, and one restart), what each result means and what to do (0 to 4, and
  267011 for a task that has not run yet), how to see the port with `docker compose ps backup-target`
  (`<tailnet address>:2222->22/tcp`, or only `22/tcp`) and with `Get-NetTCPConnection`, how to restart the service by
  hand, and the case of 2026-11-24 to 2026-11-27. The signal names a target without its port too.
- **Alerts** says to read `LastTaskResult`, what to do for 1 and 2, that the task restarts a target without its port,
  and how to mend it at once.
- **Containers-and-Images** names `node:26-trixie-slim` and what Ops CI tests in it, in the row of Docker Hub of the
  list of images.
- **Monitoring** (F2): its runbook row of `BackupTargetDown` is changed to match Alerts.
- No tailnet address or name is in the new text; the pages write `<tailnet address>`.
- `test-wiki-coverage.sh` passes 133 of 133.

## The criteria of MIL-091 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | The five cases in `test-start-target.sh` above, each failing against the script of `main` at 1d99f32f; Ops CI runs the test in its new entry |
| 2 | Pending | Task 3, on S01's yes: the installed task still has the trigger at sign-in of [MIL-074], with no delay (read 2026-11-27, last run 12:00 UTC with the result 0); the triggers built in memory in row 11 of QC-SH-001 show what the new registration gives |
| 3 | Pending | Task 3: the drill, with S01 at the keyboard |
| 4 | Pending | Task 4: the age of the newest copy and the two alerts in Prometheus at the gate |
| 5 | Pending | The description, the wiki pages, the rule tests, `test-wiki-coverage.sh` and `lint.sh` pass, and this review is Go-with-conditions; the required checks of the pull request are still to come |

## Findings of the review

- **F1, note: the checklist of the Windows scripts.** Criterion 5 of [MIL-091] asks for a review against
  [QC-SH-001] "for the two Windows scripts, as RC-361 did". [RC-361] read them against the shared rules of
  `coding-conventions` (its F2), because the framework has no checklist for JScript or PowerShell. This review applies
  each criterion of [QC-SH-001] with the equivalent of the language where it names bash, and reads the shared rules
  (names, comments that say why, no magic number, no swallowed error, each piece of knowledge once) in the same rows.
  A checklist for these languages stays a change of the framework, to propose upstream (AGENTS.md rule 5).
- **F2, addition: the page Monitoring.** The deliverable names the pages Disaster-Recovery, Alerts and
  Containers-and-Images. The runbook table of the page Monitoring gave the same advice as Alerts for
  `BackupTargetDown`, which finding 4 of [MIL-091] calls wrong for a target without its port, so it changes with
  them.
- **F3, fixed in this review: a quoted value with a comment after it.** The first version of `plainValue` read
  `BACKUP_TARGET_BIND='<address>' # note` as the text with its quotes, so a run ended with 4 and checked nothing,
  while docker compose reads the text inside the quotes and skips the comment. A new case of the test failed against
  that version (result 4) and passes with the change: the value is the text up to the closing quote. Windows Script
  Host reads it the same way; a quote that is not closed stays invalid and gives 4. The live settings file reads the
  same with both versions.
- **F4, design: the wait for the port.** After `up -d`, and again after the restart, the script checks the port up to
  10 times, 2 seconds apart, because a service that has just started needs a few seconds to publish it. A run waits at
  most 36 seconds and the time of `docker compose`, well within the 10 minutes of the task; a port that answers at the
  first check costs no wait.
- **F5, design: a host without the address changes nothing.** While the host lacks the address of
  `BACKUP_TARGET_BIND` (Tailscale not up or not signed in), a restart could not publish the port either, so the script
  ends with 2 and the next hourly run checks again. A `BACKUP_TARGET_BIND` of `0.0.0.0` or `::` would always give 2;
  the wiki page CI-CD sets the tailnet address, and the target is reached only from the tailnet.
- **F6, note: the page Technology-Choices does not name Node.js.** The page has an entry for each product of the
  tests (k6 and Mailpit among them), and Node.js now runs one test. An entry needs the research of that page
  (practitioners' threads and the alternatives), and [MIL-091] does not name the page, so it is left out of this
  milestone, for S01 to decide.
- **F7, note: the new check is not required yet.** As task 1 says, `Ops script tests (test-start-target.sh)` becomes
  a required check only when S01 adds it to the rules of the branch. Until then a red run of it would not block the
  merge, so the condition of the verdict names it.
- **F8, fixed after the review: the four findings of SonarQube Cloud on the pull request.** Its quality gate passed
  with four new code smells, and all four are fixed in the code; none is dismissed. `javascript:S8786` (major): the
  expression `/^\s+|\s+$/` of `trim` backtracks over each run of white space inside a line, so its time grows with
  the square of the length of the line; `trim` is now a loop over the two ends. `javascript:S7773` (minor, twice):
  `Number.parseInt` is not in JScript 5.8, so `isAddress` and `isPort` read the digits with `Number()` after their
  expressions have checked them, which reads them as `parseInt` did. `javascript:S2187`: the analysis takes a file
  named `*.test.js` for a test file and found no test case of a test framework in it; the first fix, the name
  `test-start-target.js`, made the analysis read the test as production code, and F9 replaces it. A new case of the
  test, white space after a value and a long run of it before a comment, covers the new `trim`, and the probe ran it in
  Windows Script Host. The checks of the first commits were all green before this fix.
- **F9, fixed after F8: the second analysis of SonarQube Cloud.** With the name `test-start-target.js` the analysis
  took the test for production code and failed the quality gate on the security rating of new code, D where A is
  required, with five new issues in the test: `javascript:S1523` (vulnerability, critical) for `vm.runInContext`,
  which runs the script under test; `jssecurity:S8707` (vulnerability, major, twice) for the paths of the two files
  under test, which the command line may give; and `javascript:S1313` (minor, twice) for the placeholder address of
  `backup-target.env.example` and the address of a home network in the stand-in host. Each is what the test exists to
  do, and none was raised while the file was named `start-target.test.js`, which the analysis reads as a test file.
  The test has that name again, and each check is now a test of the test runner of Node.js (`node:test`, with `test`
  and `assert.ok`), so `javascript:S2187` finds the test cases it missed in F8; nothing is dismissed and no setting of
  the analysis changes. The 28 checks are the same: 28 of 28 with Node.js v24.18.0 and in `node:26-trixie-slim`, and
  4 of 28 against the two files of `main`; `node:test` reports each failed check with the result and the steps of its
  case, and the run ends with 1.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-YML-001] and [QC-WF-001] passes, and criterion 1
of [MIL-091] passes. The condition: every required check of the pull request is green before the merge, and so is
the new check `Ops script tests (test-start-target.sh)`; it holds again for the commits of F8 and F9. Criteria 2 to 4
wait for task 3 and the gate. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every required check of the pull request green, and the new check of the script test | S01 (the AI as stand-in) | At the merge |
| Approve or cancel the CD run of the merge, if one waits; the change deploys nothing to the VM | S01 | 2026-12-04 |
| Task 3 on the backup host: the new `start-target.js`, `register-start-task.ps1` run again, the reading of the triggers and of one run, and the drill | S01 (the AI on S01's yes) | 2026-12-04 |
| Bring the new description of `BackupTargetDown` to the monitoring host, if S01 wants it before the next deploy of the monitoring | S01 | 2026-12-04 |
| Decide whether the page Technology-Choices gets an entry for Node.js (F6) | S01 | 2026-12-04 |

---

[MIL-091]: ../../milestones/mil-091-the-backup-target-answers-on-its-port.md
[RC-450]: ./rc-450-mil-091.md
[RC-361]: ./rc-361-mil-074-code.md
[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[MIL-069]: ../../milestones/mil-069-retention-on-the-backup-target.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[61cd97b]: https://github.com/NF-Hotel/NFHotel/commit/61cd97b84b8bcbfc214dc3f32cdbf28876b6af6d
