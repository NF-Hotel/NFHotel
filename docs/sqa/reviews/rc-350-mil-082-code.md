# RC-350 Code review of MIL-082: Ollama as a user that is not root, the masked paths, the runtime versions and the page Container-Escape

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-350 |
| CrossReference | [MIL-082], [RC-345], [RC-346], [RC-347], [RC-349], [CIS-001], [MIL-045], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | [f7dbdae] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-082] tasks 1 to 5 on the branch `mil-082-container-escape`:
  - task 1: the lines of the runtime check that `ops/tests/test-monitoring-stack.sh` now prints, and the host-access
    map on the wiki page Container-Escape;
  - task 2: the wiki page Container-Escape, its entry in `_Sidebar`, its links from Containers-and-Images and
    Host-Hardening, and the rule `system_paths` of `ops/scripts/check-container-runtime.sh` with its unit test
    `ops/tests/test-check-container-runtime.sh` and its live test `ops/tests/test-container-runtime-live.sh`;
  - task 3: `ops/tests/test-ollama-user.sh`, the service `models-owner` and the user of Ollama in
    `ops/deploy/ollama/docker-compose.yml`, the two rows of `ollama` in `ops/security/container-profiles.tsv`, the
    row of `models-owner` in the table of `ops/tests/test-service-networks.sh` and on the wiki page
    Network-Isolation, and the job "Ops Ollama as a non-root user (MIL-082)" of `.github/workflows/ops-ci.yaml`;
  - task 4: the metric `nfhotel_container_runtime_info` of the check with its unit tests, and the section "When a fix
    for an escape is published" of the page;
  - task 5: [CIS-001] rows 1.2.2, 2.1 and 4.1 and the evidence `compose`, and the wiki pages Containers-and-Images,
    Host-Hardening and Workflows.
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`), [QC-WF-001] (`qc-workflow`). The wiki
  pages have no checklist of their own; they are read against the deliverable of [MIL-082] below.
- Scope: full review of the new and changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who asked in chat on 2026-11-12 for this
  work in autonomous mode, with the merge and the approval of its CD run, and on 2026-11-13 to finish the milestones
  and the gaps found)

## Checklist Results

### QC-SH-001: `test-ollama-user.sh` (new), `check-container-runtime.sh`, `test-check-container-runtime.sh`, `test-container-runtime-live.sh` and `test-monitoring-stack.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The new test keeps the convention of `ops/tests`, `set -uo pipefail` with each assertion counted; it stops at once where the later checks would mean nothing (no network, `compose create` failed, the image as root did not answer) |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | shellcheck clean; the compose call and the start of the image as before MIL-082 are functions with quoted arguments |
| 3 | Naming | Pass | snake_case functions (`as_before`, `in_volume`, `health_of`, `answers`, `listed`, `label_value`) and variables; UPPER_SNAKE for `COMPOSE`, `PROJECT`, `NETWORK`, `OLLAMA`, `OWNER`, `VOLUME` and `IMAGE`, set once and read-only |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 over the shell files of the list of `ops/tests/lint.sh`: clean, after two `A && B \|\| C` (SC2015) were written as `if`; no new `disable` |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `error: run it as root`, `error: no image of ollama in …`, `error: compose create failed: …` and `error: the image as root did not answer` go to standard error; the test takes no argument |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The test works in a `mktemp -d` folder; its trap removes the compose project, its volume, the network `t-ollama-llm` and the containers `t-ollama-root` and `t-ollama-old`. `test-monitoring-stack.sh` reads `/tmp/monitoring-deploy.out`, which it wrote before this change |
| 7 | No secret in the script, its output or a command line | Pass | The test reads `ollama.env.example`; the metric holds only versions |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The check still only reads. The test changes only what it creates and removes it |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header of the test names what it checks, its usage, what it creates, its exit codes and what it needs; the header of the check names the rule `system_paths` and the versions in `--metrics` |
| 10 | Implements a cited task; deviations recorded | Pass | Each change names MIL-082; F1 and F2 below record what differs from the tasks |
| 11 | Success, failure and bypass paths tested | Pass | The unit test passes 94 of 94: three containers break `system_paths` (both lists empty, no masked path, no read-only path), and a mutant without the rule fails those three; two tests read the line of the versions in the report, one with a quote and a backslash in a version. The live test passes 41 of 41 in WSL, a container started with `systempaths=unconfined` among them. `test-ollama-user.sh` failed 8 of its 20 checks against 7df3d47, the commit that added it before the compose file changed (Ollama configured and running as root, the volume and its key owned by root, no one-shot service, and a write that must fail succeeded), and passed 20 of 20 at the head of the branch; both runs as root in WSL with Docker Engine 29.9.3 |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci`: no difference |
| 13 | Safe to re-run | Pass | The test removes what it starts at its beginning and at exit |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs:` names bash 4, root and docker with the compose plugin |
| 15 | Each sequence, constant, path or message defined once | Pass | The test reads the image from the compose file; the user 10001 is the anchor `x-ollama-user` of the compose file, used by both services; the image is written in both (F3) |

### QC-YML-001: `ops/deploy/ollama/docker-compose.yml` and `.github/workflows/ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); `docker compose` reads the compose file in the test |
| 2 | Two spaces, LF, one final newline | Pass | yamllint |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` over the YAML files of the list of `lint.sh`: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | `"10001:10001"`, `user: "0"` and `restart: "no"` |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The entrypoint of `models-owner` is one command |
| 8 | A repeated block defined once | Pass | The user is the anchor `x-ollama-user`; the image is written twice on purpose (F3) |
| 9 | Anchors only where supported | Pass | Docker Compose reads anchors and `x-` fields |
| 10 | Validates against the schema of its tool | Pass | `docker compose create` and `up --wait` in the test |
| 11 | A header comment states purpose, tool and decision | Pass | The header says that Ollama runs as 10001 and that `models-owner` gives it the volume; the comment of the service says why it exists |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | N-A | None |
| 14 | Lowercase kebab-case file names | Pass | `test-ollama-user.sh` |
| 15 | A check runs without the real target | Pass | The test starts the project on the runner with a model of a few bytes; nothing is downloaded but the image |

### QC-WF-001: `.github/workflows/ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | Above |
| 2 | Location and extension | Pass | |
| 3 | `name`, `on`, `jobs` | Pass | Unchanged |
| 4 | Header comment | Pass | The header names Ollama as a user that is not root (MIL-082) |
| 5 | Descriptive, unique names | Pass | "Ops Ollama as a non-root user (MIL-082)" and its step are unique |
| 6 | actionlint | Pass | actionlint 1.7.7: clean |
| 7 | Explicit least permissions | Pass | The workflow keeps `contents: read`; the job adds nothing |
| 8 | Actions pinned | Pass | `actions/checkout` at the full commit of the other jobs, with `persist-credentials: false` |
| 9 | No secret | Pass | |
| 10 | No untrusted text in `run:` | Pass | One script, no input |
| 11 | No fork code under a privileged trigger | N-A | |
| 12 | State changes outside the repository | N-A | CI only |
| 13 | Token scope | N-A | Unchanged |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04`, 20 minutes |
| 15 | `concurrency` | Pass | Unchanged |
| 16 | Filtered triggers | Pass | Unchanged |
| 17 | Pinned tools | Pass | The image of Ollama is pinned by digest in the compose file that the test reads |
| 18 | Dependency cache | N-A | |
| 19 | A failing step names the tool, the file and the reason | Pass | Each `FAIL` line names the check, what it got and what it expected |
| 20 | `run:` within the limit | Pass | One line |
| 21 | Logic used in several places is shared | Pass | The test calls the runtime check of MIL-080 |
| 22 | Features verified on the instance that runs them | Pending | The job runs on GitHub's runner in the pull request; here the test ran in WSL with docker-ce 29.9.3 |
| 23 | Reusable workflow header | N-A | |
| 24 | A static test of the workflows on every pull request | Pass | `test-workflow-policy.sh`: the 14 workflows and actions follow the policy of ADR-0037 |

### The wiki pages, against the deliverable of [MIL-082]

- **Container-Escape** has one row for each of the twelve paths of the deliverable, with its measure, evidence, what is
  left and the decision with its owner; the host-access map of the 11 containers of `nf-hotel`, from the check in CD
  run 43506763115; user namespaces and rootless Docker evaluated again, with what would change the decision; and the
  steps for a fix of an escape, with runc 1.5.3 as the first case.
- **Containers-and-Images** names Ollama as 10001, the service `models-owner`, and the masked paths; **Host-Hardening**
  names the rule `system_paths` and the metric of the versions; **Workflows** names the new job; the sidebar links the
  page. `test-wiki-coverage.sh` passes 115 of 115.

## The criteria of MIL-082 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pending | The page has the twelve rows and the map of the VM; the rows of `nf-monitoring` come from the deploy of the monitoring or, until it runs, from the lines that the job Ops monitoring configuration (MIL-023) prints for the same compose files (F6) |
| 2 | Pending | 20 of 20 in WSL at the head of the branch: the user 10001 in the configuration and in the kernel's view, healthy, the model that root wrote listed, and healthy again after a restart and a second start. The job Ops Ollama as a non-root user runs it in the pull request |
| 3 | Pending | The deploy (task 6) |
| 4 | Pass | The line of the versions and the metric `nfhotel_container_runtime_info` (unit tests), and the section of the page on installing a fix |
| 5 | Pass | [CIS-001] rows 1.2.2, 2.1 and 4.1 and the wiki pages above; `test-wiki-coverage.sh` 115 of 115 |
| 6 | Pending | This review; the linters of `lint.sh` at its versions are clean (F7); the checks of the pull request |

## Findings of the review

- **F1, gap closed: nothing checked `/proc` and `/sys`.** The deliverable names them as a path out, and the check of
  [MIL-080] read neither `MaskedPaths` nor `ReadonlyPaths`. A container started with `--security-opt
  systempaths=unconfined` empties both lists without `privileged`, passed every rule, and root in it can write
  `/proc/sysrq-trigger` of the host. The rule `system_paths` fails a container with an empty list; the unit test has
  three breaking cases, the live test starts the real one, and the deploy of this branch reads the rule for every
  container of the VM.
- **F2, deviation from task 3: the profile names 10001, not `nonroot`.** The rule `user` takes a user ID as well, so
  the check fails Ollama as root and as any other user. It is stricter than the plan.
- **F3, note: the image of Ollama is written twice.** `models-owner` runs the image of Ollama, so no other image is
  pulled. Dependabot reads the `image` lines, so each service names the image, and the first check of the test fails
  when the two differ.
- **F4, note: a rollback to the release before.** `deploy-release.sh` deploys the release before when a deploy fails,
  and that release runs Ollama as root without capabilities on a volume that 10001 now owns. Root without
  `DAC_OVERRIDE` reads what every user may read: the test shows it starts and lists the models. A pull of a model would
  fail until the next deploy gives the volume to 10001 again. The deploy pulls only the models that
  `OLLAMA_PULL_MODELS` names, and the CD logs of 2026-11-13 have no such pull.
- **F5, note: runc 1.5.3.** Published on 2026-11-12, with a fix for CVE-2026-119101, which needs a checkpoint and
  restore that NF Hotel does not use; the VM ran 1.5.2 on 2026-11-13. [CIS-001] row 1.2.2 is Applied with that
  exception, and the page Container-Escape records the case. Installing it on the VM is a change of the host for S01,
  with S03 for the reboot, in the monthly window.
- **F6, note: the rows of `nf-monitoring`.** They come from the check that `deploy-monitoring.sh --apply` runs on
  `nf-monitoring`, and that deploy waits for S01 to approve its Tailscale SSH check ([RC-347] finding 1). This branch
  makes the job Ops monitoring configuration print the lines of the check over the same compose files on the runner,
  so the gate can use them, named as CI, until the deploy runs.
- **F7, note: the linters ran in WSL.** As F9 of RC-348: Docker Desktop is off on S01's PC, so actionlint 1.7.7,
  shellcheck v0.11.0, shfmt v3.14.1 and yamllint 1.35.1 ran over the file lists of `lint.sh` in WSL. The job Ops
  linters runs `lint.sh` itself.
- **F8, note: a new check that is not required.** "Ops Ollama as a non-root user (MIL-082)" is not in the ruleset,
  which changes only on S01's word.
- **F9, fixed: the table of the service networks.** The job Ops service networks (MIL-061) of the pull request failed
  on its first run: `models-owner` was not in the table of `ops/tests/test-service-networks.sh`, which wants a row for
  every service of the compose files. It is on no network, as `results-owner` of the benchmark is; the table has its
  row, the wiki page Network-Isolation names both one-shot services, and the test passed 51 of 51 in WSL.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001] and [QC-YML-001] passes, and every one of [QC-WF-001]
but 22, which stays Pending until the checks of the pull request run on GitHub's runner. The condition: every check of
the pull request is green before the merge, the job Ops Ollama as a non-root user among them; the gate record reads
the CD log for criterion 3. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge and read Ollama as 10001 with no violation, and the market analysis and the benchmark healthy (task 6) | S01 (the AI as stand-in) | 2026-11-14 |
| Install runc 1.5.3 on `nf-hotel` and `nf-monitoring` with the packages of Docker's repository, and read the line of the versions after it | S01, S03 for the reboot | The next monthly window |
| Decide whether "Ops Ollama as a non-root user (MIL-082)" becomes a required check | S01 | Open |

---

[MIL-082]: ../../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-345]: ./rc-345-mil-082.md
[RC-346]: ./rc-346-mil-080-code.md
[RC-347]: ./rc-347-mil-080-gate.md
[RC-349]: ./rc-349-mil-081-gate.md
[CIS-001]: ../cis-checklist.md
[MIL-045]: ../../milestones/mil-045-benchmark-results-volume.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[f7dbdae]: https://github.com/NF-Hotel/NFHotel/commit/f7dbdae7b158150f030dd523c8236250ed2689a7
