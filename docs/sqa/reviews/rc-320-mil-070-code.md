# RC-320 Code review of MIL-070: the connection string over standard input

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-320 |
| CrossReference | [MIL-070], [RC-318], [ADR-0041], [ADR-0011], [ADR-0024], [MIL-023], [CIS-001], [QC-ADR-001], [QC-SH-001], [QC-YML-001], [QC-WF-001], [RC-321] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 3 of MIL-070 before their pull request: ADR-0041, `as_app` of `deploy-release.sh`, `test-sudo-log.sh`, Ops CI and three wiki pages; Go | [6411a2a] |
| 2026-11-09 | Accepted | mikemike | AI as stand-in for S01 | The criteria that waited for tasks 4 and 5 and the pull request are met: every required check of #620 passed on 6c9b5dc, the deploy of the fix wrote no new entry, and the old password is refused (RC-321); the verdict stays Go | pending |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-070-fix` against `mil-068-gate` at 922ac27, the head of #619, which stacks on the plan of MIL-070 in #618):
  - the new [ADR-0041] (task 1), a full review;
  - shell: `APP_SHELL`, `as_app` and the new `connection_string_line` in `ops/scripts/deploy-release.sh` (task 1), and the new `ops/tests/test-sudo-log.sh` (task 2);
  - workflow: `.github/workflows/ops-ci.yaml`, the test in the matrix of the script tests, in `debian:13-slim` (task 2);
  - wiki: the new sections "No secret in the log of sudo" of Host-Hardening and "A new password for `nfhotel_app`" of Database-Preparation (task 3), and the script tests and the ruleset row of Workflows (finding 6).
- **Not in this record:** criteria 3 and 4 of [MIL-070] need the deploy of this change (task 4) and the new password (task 5). Criterion 6 needs this record and `lint.sh`, which pass below, and every required check of the pull request, which Ops CI runs once the pull request targets `main`. The gate record of task 6 closes them.
- **Checklists used:** [QC-ADR-001] for the ADR; [QC-SH-001], [QC-YML-001] and [QC-WF-001] for the code.
- **Scope:** full review of ADR-0041; delta review of the changed files against `mil-068-gate`.
- **Language and domain:** n/a (technical types).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the ADR, the code and this record. S01 asked for the last milestones in autonomous mode in chat ("Fortsæt så med at lave de sidste milestones i automode on") and accepted the plan of MIL-070 in chat ("1 ja", [RC-318]), which names the decision of ADR-0041 and the ways it rejects. The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-08, on the PC of S01 (WSL 2, Docker 29.7.2):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean. On the way it found SC2016 on `APP_SHELL` (finding 4) and the spacing of `((! in_sudo))` in the test (shfmt); both are fixed.
- `test-sudo-log.sh` in `debian:13-slim`, as Ops CI runs it: 19 passed, 0 failed. Written first and committed alone (03f1ca4): against the `as_app` of 4d67484, 17 passed and 2 failed, the connection string in `/var/log/sudo.log` and the `--preserve-env` of `deploy-release.sh` line 134 (finding 1).
- A mutation run: two copies of the fixed `deploy-release.sh`, each against the test in `debian:13-slim`:
  - the connection string passed to `sudo` as an argument of the command instead of on standard input: 2 checks fail, the token in a file under `/var/log` and in the arguments of a process;
  - a shell that reads the line and does not export it: 1 check fails, the connection string that the command gets.
- The tests of the scripts around `as_app`, in `debian:12-slim` as Ops CI runs them: `test-deploy-release.sh` 95 passed, `test-switch-vm-checkout.sh` 14 passed and `test-cd-steps.sh` 97 passed, none failed.
- `test-wiki-coverage.sh`: 112 passed. `check-languages.sh`: exit 0.

**The review of every `sudo` call outside `ops/tests`** (criterion 2), from a search of `ops/` and `.github/`, call by call:

- `deploy-release.sh`, `as_app`: the one call that kept a secret. It now keeps only `NFHOTEL_API_MIGRATE_ON_START`. Its commands are `git` with commits and paths, and `env` with the image references and `NFHOTEL_ENVIRONMENT=Production` before `deploy-services.sh`: names and digests, which the log has always held.
- `cd-steps.sh`, four calls of `sudo -n /usr/local/sbin/nfhotel-deploy` over Tailscale SSH: the commits and the hash of the migration script, each checked against its pattern first, the release references and `--drill`; the migration script goes on standard input. None keeps a variable, and the rule of `install-deploy-user.sh` sets no `Defaults` for the deploy user, so `sudo` resets its environment.
- `deploy.sh`, the first install of the VM: `sudo cloud-init status`, `sudo mkdir` and `sudo chown` of a folder, and `sudo apt-get install git`, with paths, a user name and a reference.
- `ops-ci.yaml`: `sudo bash` of two tests and `sudo apt-get install age` on the runner; none keeps a variable.
- The other matches are text, not calls: the hints `sudo cat <file>` of `manage-team-db-users.sh` and `sudo $0 --confirm` of `apply-firewall.sh`, the `docker logs` hints of the alert rules, `sudo` in the list of packages to keep of `remove-desktop.sh`, and the settings files under `ops/deploy/host`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| ADR 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | ADR-0041, `adr-0041-connection-string-over-standard-input.md`, the registry's next number (now 0042); no other `adr-0041` |
| ADR 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | Who decides; the problem with the log line, its three destinations and the 186 entries of the journal since 2026-10-21; four forces: the trail of the log of `sudo`, the commands of older releases that read the variable, no secret in an argument or another file, and `use_pty` without an input log; four options |
| ADR 3 | At least two options were considered, or the absence of alternatives is justified | Pass | `runuser` or `setpriv`, a temporary file, no log of `sudo` for root, and standard input: the ways that the plan of MIL-070 names |
| ADR 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | "We follow option 4: …", one sentence, then three numbered points: `as_app`, the rule for every `sudo` call in `ops/`, and the exposed password |
| ADR 5 | Consequences list both Positive and Negative outcomes | Pass | Three positive and four negative: the standard input of the command, the longer log line, the limit of the word check and the password that stays exposed until task 5 |
| ADR 6 | Quality-attribute impacts are named where the decision affects them | Pass | Security (confidentiality of the database login, accountability through the log of `sudo`) and maintainability |
| ADR 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | MIL-070, MIL-023 and ADR-0024, each in CrossReference and defined at the bottom |
| ADR 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | One row, `Accepted` after this record and before its first commit, as ADR-0040 did; `pending` until the commit |
| ADR 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Nothing is superseded. ADR-0011 keeps the roles and ADR-0024 the secret file; this record changes only how the connection string reaches the command |
| ADR 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | VM, AI and CI are spelled out on first use; the one bare "CIS" in the prose became the link [CIS-001] in this review; `sudo`, `runuser`, `setpriv` and `printf` are names of programs |
| ADR 11 | Status is one of the allowed values, and every Version History row uses only these | Pass | `Accepted` |
| SH 1 | Header line and `set` options | Pass | `deploy-release.sh` keeps `#!/usr/bin/env bash` and `set -euo pipefail`; the test keeps `set -uo pipefail`, as the other tests of `ops/tests`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | The shell reads the value with `IFS= read -r` and exports it quoted, so a value with spaces, `;`, `=`, an apostrophe, double quotes, `$HOME` and a backslash reaches the command byte for byte (tested with all of them); the command passes as `"$@"`; the test reads the files with `find -print0` and `read -d ''`, and `NOT_SECRET` is an array |
| SH 3 | Names | Pass | `APP_SHELL`, a constant; `as_app` and `connection_string_line`, functions; the test is `test-sudo-log.sh`, with `sudo_kept`, `sudo_call`, `not_secret`, `NOT_SECRET`, `TRAP_PASSWORD`, `FAKE_PASSWORD` and `CONNECTION` |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean. `APP_SHELL` has one `disable`, SC2016, with its reason on the line, as `psql_as_migrator` has (finding 4); the test has SC2015, as every test of `ops/tests`, and SC2034 for `app_dir`, which `as_app` reads after the `eval` |
| SH 5 | Errors | Pass | `as_app` ends with the exit status of the command, which `exec` hands up through `sudo` (tested with 3); a failing `sudo` ends the pipeline with its own status under `pipefail`. The test ends at once with a failure when `sudo` cannot be installed |
| SH 6 | Temporary files | Pass | The fix writes no file: the value goes through a pipe. The test works in one `mktemp -d` folder, removed by `trap … EXIT` since this review (finding 5) |
| SH 7 | Secrets | Pass | The aim of the change. The connection string crosses `sudo` in no argument and in no kept variable, and `printf` is a builtin of bash, so it is in the arguments of no process. Tested: the token is in no file under `/var/log` and in no `/proc/*/cmdline` while the command runs, and the snapshot of the arguments was taken. The test holds only fake passwords that it writes itself |
| SH 8 | State outside its folder | Pass | Unchanged: `deploy-release.sh` changes the VM only when the CD runs it after S01's approval; the test changes only its own container |
| SH 9 | Header | Pass | The header of `deploy-release.sh` names no variable that crosses `sudo`, so it needs no change; the comment over `APP_SHELL` says why the value crosses on standard input and why the text stays on one line. The header of the test names its checks, how to run it, its exit codes and its needs |
| SH 10 | Implements the task it cites | Pass | Tasks 1 and 2 of MIL-070, and the wiki of task 3; the commits carry `Task:` trailers. Deviation: the second check scans every shell file of `ops/` outside `ops/tests`, wider than the `ops/scripts` of task 2 (finding 3) |
| SH 11 | Tested for success, failure and the bypass path | Pass | 19 checks in `debian:13-slim`: the trap reproduced first; the connection string byte for byte, the flag, the folder and the empty standard input of the command; the call in the log, the token in no log file and no argument; no settings; exit status 3; the path without `sudo` when the user already is the application user, which adds no log line; seven calls that the second check must refuse, each on its line, and six lines that it must pass; the scan of `ops/`. Both mutants fail |
| SH 13 | Safe to re-run | Pass | `as_app` keeps no state and reads the variable at each call; the test runs in a fresh container |
| SH 14 | Needs stated | Pass | `deploy-release.sh` already names `sudo`; the fix adds only builtins of bash (`printf`, `read`, `export`, `exec`). The test names Debian 13 and root |
| SH 15 | Knowledge once | Pass | The text of the shell is one constant, `APP_SHELL`; the test reads it, `as_app` and `connection_string_line` from the script with `sed` and `eval` instead of a copy. The names that may cross `sudo` are one array, `NOT_SECRET` |
| YML 1 | Parses, no duplicate key | Pass | actionlint 1.7.7 and yamllint 1.35.1 read the file; no duplicate key |
| YML 2 | Indentation, LF, end of file | Pass | 2 spaces, LF, one newline at the end, no trailing space (yamllint) |
| YML 3 | yamllint | Pass | yamllint 1.35.1 with `ops/.yamllint` clean; no `disable` comment |
| YML 4 | Coerced scalars | Pass | The new values are a file name and an image name, which YAML reads as strings |
| YML 5 | Safe types | Pass | No tag |
| YML 6 | Secrets | Pass | None |
| YML 7 | Embedded program text | Pass | No new program text; the run step is unchanged |
| YML 8 | Repeated blocks | Pass | One run step for every test; the image comes from the matrix, with the default `debian:12-slim` in one place |
| YML 9 | Anchors | N-A | None |
| YML 10 | Schema | Pass | actionlint 1.7.7 checks the workflow against the schema of GitHub Actions |
| YML 11 | Header comment | Pass | The comment over `include` now says why each of the three entries runs Debian 13 |
| YML 12 | Key names | Pass | The keys of GitHub Actions, unchanged |
| YML 13 | Block scalars | N-A | No multi-line string added |
| YML 14 | File name | Pass | Unchanged |
| YML 15 | A check without the real target | Pass | actionlint on the file, and the job runs on the pull request |
| WF 1 | Meets QC-YML-001 | Pass | The YML rows above |
| WF 2, 3, 4 | Place, keys, header | Pass | Unchanged: `.github/workflows/`, `name`, `on` and `jobs`, and a header that names the script tests |
| WF 5 | Unique names | Pass | `Ops script tests (${{ matrix.test }})` gives the new entry its own name |
| WF 6 | actionlint | Pass | actionlint 1.7.7 clean |
| WF 7 to 13 | Permissions, actions, secrets, untrusted text, triggers, state, token | Pass | Unchanged; the new entry runs the same step in a container with the `ops` folder read-only, and the test needs no secret |
| WF 14 | Runner and time limit | Pass | `runs-on: ubuntu-24.04` and `timeout-minutes: 15` of the job hold for the new entry |
| WF 15, 16 | Concurrency, filters | Pass | Unchanged |
| WF 17 | Pinned tools | Pass | The workflow installs no tool. `debian:13-slim` is a tag on purpose, as for the other two entries of Debian 13: the test has to see the `sudo` of Debian 13 that the VM gets |
| WF 22 | Verified on the host | Pass | The matrix entry with `image` is the form that two entries already run on GitHub-hosted runners; the checks of the pull request show the new one |
| MIL-070 1 | The regression test fails with the `as_app` of 4d67484 and passes with the fix, and Ops CI runs it | Pass | 17 passed and 2 failed against 4d67484 (03f1ca4); 19 passed, 0 failed with the fix; the entry in the matrix of Ops CI |
| MIL-070 2 | No `sudo` call in `ops/scripts` passes a secret in its environment or its arguments | Pass | The second check passes on every shell file of `ops/` outside its tests, and the review of every call above finds no secret |
| MIL-070 5 | The wiki pages Database-Preparation and Host-Hardening say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | Host-Hardening, "No secret in the log of sudo": the field `ENV=`, the deploys from 2026-10-21 to 2026-11-08, the rule, the test and the retention of the old entries; Database-Preparation, "A new password for `nfhotel_app`": the three steps of task 5; 112 passed |

## Findings

1. **The trap is real, and the test shows it first.** Against the `as_app` of 4d67484 the fake connection string was in `/var/log/sudo.log`, in the `ENV=` field of the line of the call, and the second check named `deploy-release.sh` line 134. The test also shows the other half with the settings of the VM: a value on standard input is in no log file, because Debian's `use_pty` without an input log records nothing of it.
2. **The branch with `sudo` had no test.** `test-deploy-release.sh` runs as root with root as the application user, so `as_app` took the branch without `sudo` and the trap stayed hidden. `test-sudo-log.sh` runs the branch with `sudo` as root for `nfhotel`, and the branch without it as the application user.
3. **The second check is wider than task 2 and narrower than criterion 2.** It scans every shell file of `ops/` outside `ops/tests`, not only `ops/scripts`. It reads the words of each call, not their meaning: a secret as an argument, such as `env NAME=value` as the command, and the `sudo` calls of the workflows are outside it. The review of every call above covers them, and ADR-0041 records the limit as a negative consequence.
4. **The single quotes of `APP_SHELL` are on purpose.** shellcheck SC2016 flagged `$value`, `$1` and `$@` inside single quotes; they must stay unexpanded until the shell that `sudo` starts. The `disable` has its reason on the line.
5. **The cleanup of the test.** The first version removed its folder only at its end, so a run that stopped early left it; SH 6 asks for `trap … EXIT`, which this review added. The run after it: 19 passed, 0 failed, and `lint.sh` clean.
6. **The wiki page Workflows had drifted.** It listed neither `test-remove-desktop.sh` nor the new test, and it said that the ruleset requires every check that runs on a pull request. The ruleset requires 41 checks, and the script tests of the migration script, of the removal of the desktop and of the log of `sudo` are not among them. The page now says so; whether the ruleset should require them is S01's decision.
7. **The command has nothing on its standard input.** `APP_SHELL` runs the command with standard input from `/dev/null`, so the line of the connection string cannot reach it (tested). No call of `as_app` gives its command input today; a later command that needs input must get it another way (ADR-0041).
8. **The log line is longer.** Each call of `as_app` now logs the 119 characters of `APP_SHELL` in place of the 24 of the old shell, and `sudo.log` wraps the line over more lines. No alert rule and no setting of Alloy reads these lines.

## Overall Verdict

Go. Every Mandatory criterion of QC-ADR-001, QC-SH-001, QC-YML-001 and QC-WF-001 that the change touches passes, and so do criteria 1, 2 and 5 of MIL-070. Criteria 3 and 4 need the deploy and the new password (tasks 4 and 5), and criterion 6 the required checks of the pull request; the gate record of task 6 closes them, and [RC-321] closed them on 2026-11-09. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request after #618 and #619, once the CD run of 4d67484 has deployed and every required check is green | AI as stand-in for S01 | At the merge |
| Approve the CD run of the merge (ADR-0038) | S01 | After the merge |
| Count the entries with the connection string in the journal and in `/var/log/sudo.log*` right before and after that deploy, without printing a line, and check the smoke test and the API (task 4) | AI as stand-in for S01 | At the deploy |
| Name a time for the new password of `nfhotel_app`, and approve the CD run of the same commit right after it (task 5) | S01 | 2026-11-15 |
| Decide whether the ruleset requires the script tests of the migration script, of the removal of the desktop and of the log of `sudo` (finding 6) | S01 | 2026-11-15 |

---

[MIL-070]: ../../milestones/mil-070-the-database-password-out-of-the-logs.md
[RC-318]: ./rc-318-mil-070.md
[ADR-0041]: ../../adr/adr-0041-connection-string-over-standard-input.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[RC-321]: ./rc-321-mil-070-gate.md
[6411a2a]: https://github.com/NF-Hotel/NFHotel/commit/6411a2a500971d387634758b345a7fda9d127722
