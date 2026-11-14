# RC-366 Code review of MIL-051: the monitoring deployed by a pull on its host

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-366 |
| CrossReference | [MIL-051], [ADR-0044], [RC-365], [RC-356], [ADR-0020], [MIL-053], [QC-SH-001], [QC-PY-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green, among them the start of the stack in the job of the monitoring configuration | [2e57a94] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | F9 from the first checks of the pull request: the clear-text address of the health check marked; still Go-with-conditions | pending |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-051] tasks 2 to 7 on the branch `mil-051-monitoring-pull-deploy`:
  - task 2: the pull `ops/scripts/pull-monitoring.sh` and its units
    `ops/deploy/monitoring/systemd/nfhotel-monitoring-pull.service` and `.timer`; the fourth step of
    `ops/scripts/create-monitoring-lxc.sh`, which installs it on a new LXC;
  - task 3: the health check after the start in `ops/scripts/deploy-monitoring.sh`, and `promtool check config`
    over every rule file there and in `ops/tests/check-monitoring.sh`;
  - task 4: the rules of `ops/deploy/monitoring/prometheus/rules/monitoring.yml` with seven tests in
    `rules-test.yml`, and the report folder that the scan exporter serves: `scan_exporter/domain/reports.py`,
    `scan_exporter/infrastructure/report_folder.py`, the changed `main.py`, `metrics_endpoint.py` and
    `prometheus_text.py`, their tests, and the mount in `docker-compose.yml`;
  - task 5: the note in A3 of the wiki page Monitoring that the pull needs no grant of the tailnet;
  - task 6: `ops/tests/test-pull-monitoring.sh`, the new checks of `ops/tests/test-monitoring-stack.sh` and
    `ops/tests/test-create-monitoring-lxc.sh`, and the new test in the matrix of the job `script-tests` of
    `.github/workflows/ops-ci.yaml`;
  - task 7 (its documents): the wiki pages Monitoring, Alerts, CI-CD, Disaster-Recovery, Container-Escape and
    Workflows.
- Checklist used: [QC-SH-001] (`qc-programming-shell`) for the seven shell files, [QC-PY-001]
  (`qc-programming-python`) for the scan exporter, [QC-YML-001] (`qc-yaml`) for the compose file and the two rule
  files, and [QC-WF-001] (`qc-workflow`) for the changed job of `ops-ci.yaml`. The decision itself is reviewed in
  [RC-365]. The two systemd units have no checklist; they are reviewed against [ADR-0044] below.
- Scope: full review of the new files and of the changed parts of the others; partial review of `ops-ci.yaml` (the
  job `script-tests`)
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig"; the reviewer wrote the code).

## Checklist Results

### QC-SH-001: `pull-monitoring.sh`, `deploy-monitoring.sh`, `create-monitoring-lxc.sh` and four tests

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The three scripts keep both; the tests keep the convention of `ops/tests`, `set -uo pipefail`, so one failed check does not stop the others |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The monitoring paths and the units are the arrays `PATHS` and `UNITS`; every expansion is quoted; shellcheck is clean |
| 3 | Naming | Pass | `find_checkout`, `check_checkout`, `read_state`, `monitoring_changed`, `ops_ci`, `record_failed`, `write_report`, `install_from_checkout` in snake_case; `STATE`, `REPORTS`, `CI_RESULT` as read-only constants |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck through `lint.sh` over every script: clean after three SC2015 and one SC2034 were fixed before the first commit; the one `disable` of the new test (SC2015) says why |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `die` and every refusal print `error:` on standard error; `--help` prints the header, and an unknown option exits 2 with the usage (tested) |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The pull writes no temporary file in `/tmp`: each state file and the report are written beside their target as a dot file and renamed, under the lock; the tests use `mktemp -d` and a trap |
| 7 | No secret in the script, its output or a command line | Pass | The pull reads the public API without a token and no secret file; `deploy-monitoring.sh` prints no secret, and its new health check prints names of services and rules only |
| 8 | A change of state outside its folder needs an explicit flag | Pass | Without an argument the pull only fetches `main` and tells what `--apply` would do (tested: the report and the state stay as they were); `--apply` and `--install` refuse to run without root |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header of the pull has its seven steps, the monitoring paths, the three forms of use, the exit codes, the tools and the six variables; `deploy-monitoring.sh` names its new steps 4 and 6 and `MONITORING_REPORT_DIR` |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-051] tasks 2, 3, 4 and 6 in the headers and the commits; the findings below record what was added in the review |
| 11 | Success, failure and bypass paths tested | Pass | `test-pull-monitoring.sh` in `debian:13-slim`: 25 of 25 (the install, a change of the documents, the wait for Ops CI, a refused CI run, the dry run, a broken rule, a failed start and its rollback, a change made by hand, the self-install with `daemon-reload`, the ancestry check, a main that cannot be fetched, the lock, the usage). `test-create-monitoring-lxc.sh`: 15 of 15. The three new checks of `test-monitoring-stack.sh` need root and run in CI (F4) |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 through `lint.sh`: no difference, after `((! apply))` |
| 13 | Safe to re-run | Pass | The lock keeps two runs apart; a run that finds nothing to do changes only its report; `--install` replaces a file only when it differs and reloads systemd only when a unit changed |
| 14 | Bash version and tools stated; GNU-only options named | Pass | Needs: bash 4+, root, git, curl, python3, `flock` and systemd, all in the LXC (read on 2026-11-14); `flock --nonblock` and `timeout` are util-linux and coreutils |
| 15 | Each sequence, constant, path or message defined once | Pass | One `deploy_monitoring` runs the deploy without the lock, one `record_failed` records a refusal, one `write_report` writes the report, and the monitoring paths are listed once in `PATHS` |

### QC-PY-001: the report folder of the scan exporter

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PEP 8 casing | Pass | `reports.py`, `report_folder.py`, `ReportFolder`, `InvalidReportError`, `MAX_REPORT_BYTES`; ruff `N` rules clean |
| 2 | Names state purpose | Pass | `checked_report`, `served_reports`, `no_reports`, `METRIC_NAMES`, `taken` |
| 3 | Formatter and linter, no unexplained suppressions | Pass | ruff format and ruff check with the pinned tools of `test-scan-exporter.sh`: clean; no new suppression |
| 4 | Every signature annotated | Pass | `mypy --strict`: clean |
| 5 | No bare except, cause kept | Pass | `InvalidReportError` for a bad report; `OSError` and `UnicodeDecodeError` of one file are reported as a problem of that file and logged, not swallowed |
| 6 | No mutable defaults, no shadowed builtins | Pass | The default of `make_handler` is the function `no_reports` |
| 7 | Context managers | Pass | Files are read with `Path.read_text` |
| 8 | Docstrings | Pass | Every new module, class and public function |
| 9 | Logging, no secrets in logs | Pass | `logging` only: "report left out", with the file name and the reason, once each time the set of problems changes |
| 10 | Traces to the Design Class Diagram | N-A | A plain technical task of [MIL-051] with no DCD, as the exporter of [MIL-053] |
| 11 | Tests for new behaviour, no network | Pass | 64 tests, coverage 93.56 %: the text format and twelve kinds of refusal, the order and the limits of the folder, logging once, and the endpoint serving a valid report after its own metrics while it leaves out one that takes a name of the exporter |
| 12 | Strict type checking | Pass | No new `Any` |
| 13 | Dependencies declared and pinned | Pass | Standard library only |
| 14 | DRY | Pass | The names of the exporter come from its families once (`METRIC_NAMES`); one check serves every report |
| 15 | Imports point inward | Pass | `domain/reports.py` imports only the standard library; `report_folder.py` imports the domain; `main.py` wires them |
| 16 | SOLID | Pass | The endpoint gets the reports as a function and knows nothing of the folder |
| 17 | Clean architecture folders | Pass | `domain`, `infrastructure`, `presentation`; tests in `tests/unit` and `tests/integration` |
| 18 | File and folder names | Pass | |

### QC-YML-001: `docker-compose.yml`, `rules/monitoring.yml` and `rules-test.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool and docker compose read them |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` through `lint.sh`: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | Series values, summaries and the mount are quoted where needed |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The longest expression is two lines |
| 8 | A repeated block defined once | Pass | The seven tests follow the groups of the other rules |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | `promtool check config`, which now reads every rule file, and `promtool test rules` with `prom/prometheus:v3.15.1`: both SUCCESS after the merge of `main` |
| 11 | A header comment states purpose, tool and decision | Pass | `monitoring.yml` names [ADR-0044] and [MIL-051]; the compose comment names the report folder |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | Folded block scalars for the expressions and descriptions |
| 14 | Lowercase kebab-case file names | Pass | `monitoring.yml` |
| 15 | A check runs without the real target | Pass | The rule tests and the stack test run in CI without the LXC |

### QC-WF-001: the job `script-tests` of `ops-ci.yaml`

The change is one entry of the matrix, one entry of `include` and the comment above it.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | yamllint through `lint.sh`: clean |
| 2 | In `.github/workflows/`, `.yaml`, not in both directories | Pass | Unchanged |
| 3 | `name`, `on` and `jobs`; explicit events | Pass | Unchanged |
| 4 | Header comment: purpose, triggers, inputs, secrets, outside changes, decision | Pass | Unchanged; the comment above `include` says why the new test runs in Debian 13 |
| 5 | Every job and step named | Pass | The check is named `Ops script tests (test-pull-monitoring.sh)` |
| 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7 through `lint.sh`: clean |
| 7 | Explicit least `permissions` | Pass | `contents: read`, unchanged |
| 8 | Actions pinned to a full commit SHA | Pass | Unchanged |
| 9 | No secret in the file | Pass | |
| 10 | Untrusted text passed through `env:` | Pass | Unchanged |
| 11 | No checkout of untrusted code with secrets | N-A | |
| 12 | A change outside the repository needs a manual or protected start | N-A | The job changes nothing outside the runner; the test serves its own stand-in of the API on 127.0.0.1 |
| 13 | Token of least scope | Pass | The built-in token, read only |
| 14 | `runs-on` and `timeout-minutes` | Pass | Unchanged |
| 15 | `concurrency` | Pass | Unchanged |
| 16 | Triggers filtered | Pass | Unchanged, on purpose |
| 17 | Tools at pinned versions | Pass | `debian:13-slim`, as the other Debian 13 tests; it installs git, curl and python3 from that release |
| 18 | A dependency cache where it matters | N-A | |
| 19 | A failing step names the tool, file and reason | Pass | The test prints `FAIL` with the name of the check and the output of the run |
| 20 | `run:` within the limit of embedded text | Pass | Unchanged |
| 21 | Shared logic as a reusable workflow or composite action | N-A | |
| 22 | Features verified on the host that runs them | Pass | The same features as the other entries of the matrix |
| 23 | A reusable workflow states how a caller pins it | N-A | |
| 24 | A static test of the workflows on every pull request | Pass | `test-workflow-policy.sh`: the 14 workflows and actions follow the policy (ADR-0037) |

### The systemd units against [ADR-0044]

- `nfhotel-monitoring-pull.timer`: `OnBootSec=2min`, `OnUnitActiveSec=2min`, `AccuracySec=10s`, wanted by
  `timers.target`, as decision 1 says.
- `nfhotel-monitoring-pull.service`: `Type=oneshot`, after and requiring `docker.service`, with
  `TimeoutStartSec=20min` for the checks, a start of up to 6 minutes and a start of the commit before (F6). It runs
  as root with no sandbox of systemd (F5).

### The documents against the deliverable of [MIL-051]

- Monitoring: the overview and the quick reference name the timer; C5 installs it; the alarm drill and the update
  of Docker pause it; "Change the monitoring" says how a merged change reaches the LXC, how to pause the deploy and
  how to deploy by hand; the runbook and troubleshooting have the four alerts and the questions of the deploy; the
  reference lists the files, the report `monitoring_pull.prom` and the checks in CI.
- Alerts: a section for `rules/monitoring.yml` with the four alerts, and a known gap. The wiki coverage test passes:
  123 of 123, every alert rule in backticks on the page.
- CI-CD, Disaster-Recovery, Container-Escape and Workflows name the deploy where they meet it.

## The criteria of MIL-051 that this review can see

| # | Criterion | What this review sees |
| --- | --- | --- |
| 1 | The ADR Accepted with a Go review | Met: [RC-365] Go |
| 2 | A merged change of the monitoring runs on the LXC within 15 minutes | Not yet seen: after the merge and the install on the LXC |
| 3 | A change outside the monitoring paths starts no deploy | Seen in the test; on the LXC after the merge |
| 4 | A broken rule refused before the start, the running stack kept | The pull test (with a stand-in deploy); the stack test with the real tools in CI |
| 5 | A failed start returns to the last deployed commit, and the alert reaches the test receiver | The pull test; the stack test sees `MonitoringDeployFailed` in the Alertmanager of the throwaway stack, whose receiver is a stand-in, in CI |
| 6 | No secret in a workflow log, a commit or a test output | Met in the change: the tests use stand-in values and the pull reads no secret |
| 7 | The tailnet grants no more than the ADR records | Met: no grant is added (task 5) |
| 8 | The tests pass in Ops CI, the wiki describes the flow, the code review is Go | The wiki and this review; the tests in the checks of the pull request |

## Findings of the review

- **F1, design: a refused commit is not tried again.** A commit refused at any stage stays refused until a new
  commit of `main` or until `/var/lib/nfhotel-monitoring/failed` is removed, so a hard failure raises one alert and
  not a deploy every 2 minutes. The runbook says how to go on for each stage.
- **F2, fixed in the review: a change made by hand.** `git checkout` would carry a changed tracked file over to the
  head, so the LXC would run a configuration that no review saw. The pull now refuses it at the stage `checkout` and
  keeps the change (6db511c), and the alarm drill pauses the timer.
- **F3, fixed in the review: a rebuilt LXC.** `create-monitoring-lxc.sh` did not name the install of the pull among
  the steps left to S03, so a rebuilt LXC would fall back to manual deploys; its fourth step now does (65f5b8d), as
  does the rebuild of Disaster-Recovery.
- **F4, condition: the stack test runs in CI only.** It needs root to give the secret files to the users of their
  containers, and the reviewer did not raise its own rights on this computer. Its three new checks (the health line,
  a broken rule refused while the running rules stay loaded, `MonitoringDeployFailed` from a report in the folder)
  are seen first in the checks of the pull request. The two images that the health check asks from inside their
  containers, Alertmanager and the blackbox exporter, have the `wget` of BusyBox at their pinned digests (read on
  2026-11-14).
- **F5, note: no sandbox for the unit.** The pull must write `/opt/nfhotel-monitoring`, `/var/lib/nfhotel-monitoring`,
  `/usr/local/sbin` and `/etc/systemd/system` and run `docker compose` as root, as the manual deploy does; a sandbox
  of systemd would not narrow what it can do through Docker.
- **F6, note: a run that hangs.** `TimeoutStartSec=20min` ends a run that hangs; the lock goes with the process, the
  next tick tries the same commit again, and `MonitoringDeployBehind` fires after 30 minutes.
- **F7, note: who can write a report.** The report folder is 0755 and owned by root, so only root on the LXC writes a
  report, and a report cannot use a name of the exporter, so it cannot change the results of the scans.
- **F8, open for S01: the new required check.** "Ops script tests (test-pull-monitoring.sh)" is new, and only S01
  changes the ruleset.
- **F9, fixed: a clear-text address in the health check (SonarQube Cloud, rule S5332).** The first checks of the pull
  request failed the quality gate: `deploy-monitoring.sh` asks Prometheus for its rules at
  `http://$bind:9090/api/v1/rules`, which made the security rating of the new code B. `$bind` must be an address of
  the host itself (the script checks it before the start), so the plain HTTP of the health check never leaves the
  host; the line is marked `# NOSONAR`, with the reason in the comment above it, as in the other scripts of `ops/`.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-PY-001], [QC-YML-001] and [QC-WF-001] passes, and
criteria 1, 6 and 7 of [MIL-051] are met. The condition: every check of the pull request is green before the merge,
among them the job "Ops monitoring configuration (MIL-023)" with the new checks of the stack test (F4). The reviewer
is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| On the LXC over Tailscale SSH: deploy the merge by hand, install the pull, and read its journal and its report in Prometheus | S01 (the AI on S01's words) | 2026-11-16 |
| See a change of the monitoring reach the LXC within 15 minutes, and a change of the documents start no deploy (criteria 2 and 3) | S01 (the AI as stand-in) | 2026-11-16 |
| Add "Ops script tests (test-pull-monitoring.sh)" to the required checks (F8) | S01 | 2026-11-28 |

---

[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[ADR-0044]: ../../adr/adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[RC-365]: ./rc-365-adr-0044.md
[RC-356]: ./rc-356-mil-051-revision.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[MIL-053]: ../../milestones/mil-053-security-scan-metrics.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[2e57a94]: https://github.com/NF-Hotel/NFHotel/commit/2e57a943a3adf65d71c0ae7c0551599c795048f1
