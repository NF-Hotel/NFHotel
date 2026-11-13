# Code review of MIL-080: the runtime check of the containers, its deploy gate, timer and alerts

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-346 |
| CrossReference | [MIL-080], [CIS-001], [RC-343], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions | [6be61ac] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-080] tasks 1 to 6 on the branch `mil-080-container-runtime-check`:
  - task 1: `ops/security/container-profiles.tsv`;
  - task 2: `ops/scripts/check-container-runtime.sh` and `ops/tests/test-check-container-runtime.sh`;
  - task 3: `ops/tests/test-container-runtime-live.sh`, its step in `.github/workflows/ops-ci.yaml`, the step of
    `.github/actions/e2e-tests/action.yml` that runs `ops/tests/check-e2e-stack.sh` and the check, and the limits of
    `results-owner` in `ops/deploy/benchmark/docker-compose.yml`;
  - task 4: `ops/scripts/deploy-release.sh`, `ops/deploy/systemd/nfhotel-container-check.service` and
    `nfhotel-container-check.timer`, and `ops/tests/test-deploy-release.sh`;
  - task 5: `ops/deploy/monitoring/prometheus/rules/security.yml`, `rules/nfhotel.yml` and `rules-test.yml`,
    `ops/scripts/deploy-monitoring.sh` and `ops/tests/test-monitoring-stack.sh`;
  - task 6: [CIS-001] and the wiki pages Host-Hardening, Alerts, Monitoring and Workflows.
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`), [QC-WF-001] (`qc-workflow`)
- Scope: full review of the new and changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who asked in chat on 2026-11-12 for this
  work in autonomous mode, with the merge and the approval of its CD run)

## Checklist Results

### QC-SH-001: the three new scripts and the four changed ones

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | Every new script starts with `#!/usr/bin/env bash`. The check uses `set -uo pipefail`, with the reason above it: every container is checked and counted, and the exit code says whether one broke its profile. The two new tests keep the convention of `ops/tests` (45 of its 47 tests), where each assertion is counted |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | shellcheck clean; the rules, the profile fields and the containers are arrays and associative arrays |
| 3 | Naming | Pass | kebab-case files, snake_case functions and variables (`readonly_rootfs`, not a variable named after the builtin), UPPER_SNAKE for `SENSITIVE` and the profile tables, which are read once |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 over the 99 shell files of the list of `ops/tests/lint.sh`: clean. One new `disable=SC2016` in `test-deploy-release.sh`, with its reason: the fake `systemctl` reads its own `$1` |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `die` prints `error: ...` on standard error and exits 1; a bad option prints the usage and exits 2; a profile row that is not valid names its line and exits 2 |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The report is written to a `mktemp` file beside it, removed by a trap, and moved into place; the tests work in `mktemp -d` folders removed on exit, and the live test removes its labelled containers on exit |
| 7 | No secret in the script, its output or a command line | Pass | The check prints no environment of a container; the live test uses stand-in values (`BACKUP_PUBLIC_KEY=test`) |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The check only reads, and writes a file only with `--metrics`; `deploy-release.sh` changes the VM only through its explicit commands, as before |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | Each option, `NFHOTEL_PROC` and `NFHOTEL_DOCKER_SOCKET`, the exit codes and the needs |
| 10 | Implements a cited task; deviations recorded | Pass | Each script names MIL-080 and its task; the one deviation is F6 |
| 11 | Success, failure and bypass paths tested | Pass | The unit test passes 81 of 81: a compliant and a breaking case for each rule, `--deploy` (a container outside the release is a warning), `--filter`, `--metrics`, a container removed during the check (added in this review), the lock before the read and a deploy that takes it during the read. Each of the 17 rules, and the lock test after the read, removed from a copy of the check, fails the test (18 mutants, each with 1 to 5 failed assertions). `test-deploy-release.sh` passes 105 of 105: the gate rolls back, a release without the check deploys, the service runs once after the release, and its failure is a warning. The live test passes 39 of 39 |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci`: no difference |
| 13 | Safe to re-run | Pass | The check only reads and replaces its report in one move; the update installs the units and enables the timer the same way each time |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs:` lines in each header; the check names GNU `stat -c` (added in this review) and reads `/proc` of Linux |
| 15 | Each sequence, constant, path or message defined once | Pass | The sensitive paths, `within` and `is_sensitive` are in the check only; the live test reads them from the check instead of copying them. The profile key is built in one place |

### QC-YML-001: the alert rules and their tests, the workflow, the action and the benchmark compose

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`) and promtool parse them |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; the files are written with LF |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` over the 44 YAML files of the list of `lint.sh`: clean, after the long descriptions were rewrapped |
| 4 | Scalars YAML would coerce are quoted | Pass | `cpus: 0.25` is a number on purpose and `mem_limit: 64m` a string; the label values of the series are quoted |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The new `run:` blocks have one and two lines; the expressions of the alerts, three |
| 8 | A repeated block defined once | Pass | The limits of `results-owner` follow the style of each service in the file; the two alerts share no block |
| 9 | Anchors only where supported | N-A | None added |
| 10 | Validates against the schema of its tool | Pass | `promtool test rules` and `check rules` of Prometheus v3.15.1 (the pinned image); actionlint 1.7.7 for the workflow and the action; the live test reads every compose file through `docker compose config` |
| 11 | A header comment states purpose, tool and decision | Pass | The headers of the workflow and the action name the runtime check |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The new descriptions use `>-` |
| 14 | Lowercase kebab-case file names | Pass | No file renamed |
| 15 | A check runs without the real target | Pass | `promtool test rules` with series for each new alert, firing and quiet |

### QC-WF-001: `.github/workflows/ops-ci.yaml` and `.github/actions/e2e-tests/action.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | Above |
| 2 | Location and extension | Pass | The workflow in `.github/workflows/` only, the action in `.github/actions/` |
| 3 | `name`, `on`, `jobs` | Pass | Unchanged |
| 4 | Header comment | Pass | Both headers name the runtime check and MIL-080 |
| 5 | Descriptive, unique names | Pass | "Ops script tests (test-check-container-runtime.sh)", "Check real containers and every compose file against the profiles (MIL-080)" and "Check the stack and the rights of its containers" are unique in their jobs |
| 6 | actionlint | Pass | actionlint 1.7.7: clean |
| 7 | Explicit least permissions | Pass | Unchanged: `contents: read` |
| 8 | Actions pinned | N-A | No new action |
| 9 | No secret | Pass | |
| 10 | No untrusted text in `run:` | Pass | `runner.temp` reaches the script through `env:`; no event field is used |
| 11 | No fork code under a privileged trigger | N-A | |
| 12 | State changes outside the repository | N-A | CI only; nothing outside the runner changes |
| 13 | Token scope | N-A | Unchanged |
| 14 | `runs-on` and `timeout-minutes` | Pass | Unchanged on both jobs |
| 15 | `concurrency` | Pass | Unchanged |
| 16 | Filtered triggers | Pass | Unchanged |
| 17 | Pinned tools | Pass | The live test's default image is `curlimages/curl:8.10.1`; the unit test runs in `debian:13-slim`, like the other Debian 13 tests of the matrix |
| 18 | Dependency cache | N-A | |
| 19 | A failing step names the tool, the file and the reason | Pass | Each `FAIL` line names the container, its profile key and the rules it breaks; the tests name the assertion that failed |
| 20 | `run:` within the limit | Pass | |
| 21 | Logic used in several places is shared | Pass | One script serves the deploy, the timer, the monitoring, the live test and the action |
| 22 | Features verified on the instance that runs them | Pending | `sudo` in the composite action and the root steps run on GitHub's `ubuntu-24.04` runner only in the CI of the pull request; here they ran in WSL with docker-ce 29.9.3 |
| 23 | Reusable workflow header | N-A | No reusable workflow changed |
| 24 | A static test of the workflows on every pull request | Pass | `test-workflow-policy.sh`: the 14 workflows and actions follow the policy of ADR-0037 |

## The criteria of MIL-080 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | The unit test 81 of 81, and the 18 mutants above |
| 2 | Pending | 39 of 39 as root in WSL (Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2), with the default image; AppArmor is off there, so its line runs on the CI runner only. The run on the runner is in the pull request |
| 3 | Pending | `e2e-stack.sh up` built the throwaway stack from this branch in WSL; `check-e2e-stack.sh` passed its 16 checks and the runtime check passed its 4 containers. The CI job End-to-end tests runs both in the pull request |
| 4 | Pending | The deploy (task 7) |
| 5 | Pending | `promtool test rules` passes; the deploy of the monitoring is task 7 |
| 6 | Pass | `test-wiki-coverage.sh` 114 of 114; [CIS-001] rows 4.1, 5.5 and 5.31 and the evidence `compose`; the wiki pages Host-Hardening, Alerts, Monitoring and Workflows |
| 7 | Pending | This review; the linters of `lint.sh` at its versions are clean (F7); the checks of the pull request |

## Findings of the review

- **F1, fixed: every container read as removed.** The live test found it: Docker CLI 29 runs the template of
  `docker inspect` over the raw JSON, where `len` of an empty `DeviceCgroupRules` (null) fails and a missing label
  prints `<no value>`, so each inspect failed and each container was reported as removed during the check. The
  template guards each list and label with `with`. The unit test feeds prepared output and cannot see this; the live
  test is its regression test.
- **F2, fixed: `results-owner` of the benchmark had no limits.** The comparison of every row with its compose file
  found it; it gets 64 MB, a quarter CPU and 32 processes.
- **F3, fixed: row 5.5 of [CIS-001] said Applied.** Alloy mounts `/`, `/proc`, `/sys`, `/var/log/journal` and
  `/etc/machine-id` read-only for the metrics and the journal of the host (ADR-0020). The row is now Applied with one
  exception, and its profile names those paths.
- **F4, fixed: `check-e2e-stack.sh` had no caller since MIL-031.** The end-to-end action runs it again before the
  tests; it had not drifted and passed against a stack built from this branch.
- **F5, fixed: the check held the lock of the deploy.** The first version took the lock for the seconds it read the
  containers, so a CD deploy that began then would stop with "another deploy is running". The check now takes the lock
  and gives it back at once, before and after the read, and drops a run during which a deploy began; two unit tests
  cover it, and a mutant without the second test fails.
- **F6, deviation from task 4: the deploy runs the service, not the update.** The update holds the lock and comes
  before the new release, so a run there would be skipped or would read the release before. After the release is
  recorded, the deploy gives its lock back and starts the service once; criterion 4 reads that run in the CD log.
- **F7, note: the linters ran in WSL.** Docker Desktop is off on S01's PC and is started only on S01's word, so
  `lint.sh` could not reach Docker; the same images at the same versions (actionlint 1.7.7, shellcheck v0.11.0, shfmt
  v3.14.1, yamllint 1.35.1) ran over the same file lists in WSL. The job Ops linters runs `lint.sh` itself.
- **F8, note: a new check that is not required.** "Ops script tests (test-check-container-runtime.sh)" is not in the
  ruleset, which changes only on S01's word. The live test and the end-to-end step run inside the jobs Ops monitoring
  configuration (MIL-023) and End-to-end tests, which are required.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001] and [QC-YML-001] passes, and every one of [QC-WF-001]
but 22, which stays Pending until the checks of the pull request run on GitHub's runner. The condition: every check of
the pull request is green before the merge; the gate review records it with the CD log and the answers of Prometheus
(criteria 2 to 5 and 7 of [MIL-080]).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge, read the check, the recorded release and the run of the service in its log, deploy the monitoring and query Prometheus (task 7) | S01 (the AI as stand-in) | 2026-11-13 |
| Decide whether "Ops script tests (test-check-container-runtime.sh)" becomes a required check | S01 | Open |

---

[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[CIS-001]: ../cis-checklist.md
[RC-343]: ./rc-343-mil-080.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[6be61ac]: https://github.com/NF-Hotel/NFHotel/commit/6be61acbb1abe0539e309cf0745a4c50a0955d98
