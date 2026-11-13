# Code review of MIL-081: the allowlist of the socket proxy, its test matrix and the probe of the runtime check

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-348 |
| CrossReference | [MIL-081], [ADR-0042], [RC-344], [RC-346], [CIS-001], [QC-SH-001], [QC-YML-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-081] tasks 1 to 5 on the branch `mil-081-socket-proxy-allowlist`:
  - task 1: the measurement in `ops/tests/test-alloy-start.sh` (the proxy at the log level `info`, the restart of
    Alloy before the log is read, the list of Alloy's calls);
  - task 2: `ops/deploy/alloy/socket-proxy/haproxy.cfg`, its `Dockerfile`, `ops/deploy/alloy/docker-compose.yml` and
    [ADR-0042];
  - task 3: the matrix and the option `--upstream` of `ops/tests/test-alloy-start.sh`;
  - task 4: the rule `proxy_allowlist` of `ops/scripts/check-container-runtime.sh` and its unit test
    `ops/tests/test-check-container-runtime.sh`;
  - task 5: [CIS-001] row 5.31 and the wiki pages Containers-and-Images and Technology-Choices.
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`), [QC-ADR-001] (`qc-adr`). The HAProxy
  configuration and the Dockerfile have no checklist of their own; they are read against the decision of [ADR-0042]
  below.
- Scope: full review of the new and changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who asked in chat on 2026-11-12 for this
  work in autonomous mode, with the merge and the approval of its CD run)

## Checklist Results

### QC-SH-001: `test-alloy-start.sh`, `check-container-runtime.sh` and `test-check-container-runtime.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | Unchanged: the check keeps `set -uo pipefail` with its reason (every container is checked and counted); the two tests keep the convention of `ops/tests`, where each assertion is counted |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | shellcheck clean; the compose files, the allowed and the refused requests, their answers and the expected answers of the probe are arrays |
| 3 | Naming | Pass | snake_case functions (`probe_proxy`, `wrong_answers`, `ask_proxy`) and variables; UPPER_SNAKE for `PROXY_PROBE`, `PROXY_ANSWERS`, `LOKI` and `CURL`, which are set once |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 over the 99 shell files of the list of `ops/tests/lint.sh`: clean. No new `disable`; the `SC2015` of the test keeps its reason |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | An unknown option of the test prints its usage and exits 2; `error: run it as root` and `error: this host has no group systemd-journal` go to standard error |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The test works in a `mktemp -d` folder; its trap removes it with the compose project, Loki, the logger, their volume, the network `t-alloy-api` and the job report. The probe writes no file |
| 7 | No secret in the script, its output or a command line | Pass | The probe prints only a name and a status code per call; the test writes stand-in addresses into the environment file of Alloy |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The check still only reads: the probe sends `GET` requests from inside Alloy, and its three refused calls name a container of zeros that does not exist, so a proxy that let them through would get 404 from Docker and hand out no data (the run with `--upstream` shows it: archive, export and attach answered 404) |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header of the test names the measurement, the matrix, `--upstream` and its expected exit 1; the check's header names the rule `proxy_allowlist` |
| 10 | Implements a cited task; deviations recorded | Pass | Each script names MIL-081 and ADR-0042; no deviation from the tasks |
| 11 | Success, failure and bypass paths tested | Pass | The unit test passes 89 of 89: a proxy that answers as it should, one that lets archive through, one that refuses the list, a probe with no answer and a proxy with no Alloy beside it. Each of the 19 mutants of the check (the 18 of [RC-346] and one without `proxy_allowlist`) fails the test, with 1 to 5 failed assertions. `test-alloy-start.sh` passes 18 of 18 with the allowlist and fails 2 of 18 with `--upstream`, the matrix and the runtime check (criterion 3). The live test of MIL-080 passes 39 of 39 |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci`: no difference |
| 13 | Safe to re-run | Pass | The test removes everything it starts at exit. After a run that was killed before its trap (a restart of WSL), the next run printed that the network `t-alloy-api` existed, went on with it, passed and removed it |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs:` names bash 4, root, docker with the compose plugin (2.24 or later for the `!reset` tag of `--upstream`) and getent |
| 15 | Each sequence, constant, path or message defined once | Pass | The allowlist is defined once, in `haproxy.cfg`; the test and the probe send requests and compare answers, and neither copies the rule. The sample key of RFC 6455 is in the test and in the probe, each with its reason: a constant of the standard that two programs send, not a rule of this project |

### QC-YML-001: `ops/deploy/alloy/docker-compose.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); `docker compose` reads it in the test |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; written with LF |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` over the 44 YAML files of the list of `lint.sh`: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The removed switches were the quoted ones; `LOG_LEVEL` is a string from the environment with the default `warning` |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | Unchanged: the health checks |
| 8 | A repeated block defined once | Pass | Nothing repeated |
| 9 | Anchors only where supported | N-A | None |
| 10 | Validates against the schema of its tool | Pass | `docker compose up --build --wait` of the test, with and without the override of `--upstream` |
| 11 | A header comment states purpose, tool and decision | Pass | The header and the comment of the service name the allowlist and ADR-0042, and why the switches are gone |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | N-A | None added |
| 14 | Lowercase kebab-case file names | Pass | No file renamed |
| 15 | A check runs without the real target | Pass | `test-alloy-start.sh` starts the project with a real Loki and a logger on the runner |

### QC-ADR-001: [ADR-0042]

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID matches the filename number and is unique and sequential | Pass | ADR-0042 after ADR-0041; the registry's next ADR is 0043 |
| 2 | Context states the problem, the forces and the options evaluated | Pass | Who decides, the problem (the rule of `CONTAINERS` on the decoded path, without an end anchor), the measured calls, five forces and five options |
| 3 | At least two options were considered | Pass | Five |
| 4 | Decision in one or two unhedged sentences, matching an evaluated option | Pass | "We follow option 5: the socket proxy lets through only the calls of Alloy, and refuses every other request with 403." Six numbered points follow |
| 5 | Consequences list both Positive and Negative outcomes | Pass | Three positive and four negative, among them that the logs and the inspect of every container still pass and that no alert watches a refused call |
| 6 | Quality-attribute impacts named | Pass | Security, reliability and maintainability |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | MIL-081, ADR-0020, ADR-0040, MIL-080 and CIS-001, all in CrossReference |
| 8 | Status changes as new Version History rows | Pass | One row; it becomes Accepted with this review, before its commit is resolved |
| 9 | A superseded ADR names its successor | N-A | |
| 10 | No unexplained abbreviations | Pass | After this review (F7) |
| 11 | Status is one of the allowed values | Pass | |

### The HAProxy configuration and the Dockerfile, against [ADR-0042]

- **Decision 1, reads only:** `http-request deny unless METH_GET`; HAProxy's `METH_GET` matches `GET` and `HEAD`.
- **Decision 2, no upgrade:** two rules refuse a request with an `Upgrade` header or with `upgrade` in its `Connection`
  header (case-insensitive), before the path is read.
- **Decision 3, the exact paths:** the rule reads `path`, which HAProxy takes as it arrives, without decoding and without
  the query; the regular expression is anchored at both ends, and the container id must be 64 lowercase hexadecimal
  characters. After the one `allow`, a last `deny` refuses the rest.
- **Decision 4, the switches go:** the compose file sets only `LOG_LEVEL`. The configuration keeps the global and
  default sections of the upstream template, which read `LOG_LEVEL` and `SOCKET_PATH`.
- **The build:** the Dockerfile copies the file over the template and checks it with `haproxy -c`, with the bind
  address put in as the entrypoint does, so a configuration that HAProxy refuses never reaches an image. The release
  builds the image from `ops/deploy/alloy/socket-proxy`, where the file is.
- **The log:** `option logasap` logs a request when its answer starts, so a log stream of Alloy, which stays open while
  its container runs, shows at once.

## The criteria of MIL-081 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pending | The five calls of [ADR-0042] are the ones the test read from the log of the proxy in WSL, with the allowlist and with the configuration of the upstream image. The job Ops monitoring configuration (MIL-023), which is required, prints the list in the pull request |
| 2 | Pending | 18 of 18 as root in WSL (Docker Engine 29.9.3): Alloy's calls pass, the log lines of a container reach Loki through the proxy, the 12 calls of the list answer 200 and the other 69 answer 403. The same job runs it in the pull request |
| 3 | Pass | With `--upstream`, in WSL: 16 passed and 2 failed. The matrix let 23 of the 69 calls through: archive (`GET` and `HEAD`), export, attach over a WebSocket, top, stats and changes of a container of zeros (404 from Docker), events and version (200), each with and without an API version, and five forms of the path, among them the archive of a real container with an encoded slash (200). The runtime check failed the proxy: archive 404, export 404, attach 404 |
| 4 | Pending | The deploy (task 6) |
| 5 | Pass | [ADR-0042], [CIS-001] row 5.31 and the wiki pages Containers-and-Images and Technology-Choices say what the deliverable says; `test-wiki-coverage.sh` 114 of 114 |
| 6 | Pending | This review is Go; the linters of `lint.sh` at its versions are clean (F9); the scan gate of the release and the checks of the pull request follow |

## Findings of the review

- **F1, fixed: a key that was not valid hid the upgrade rule.** The first matrix and the first probe sent a
  `Sec-WebSocket-Key` that was not 16 bytes. HAProxy answers such a request 400 as a bad request before any rule, with
  both configurations, so the attach call looked refused with the configuration of the upstream image too. The test and
  the probe now send the sample key of RFC 6455, and the upstream run shows the attach call passing (404 from Docker).
- **F2, fixed: no call of Alloy was found in the log.** The proxy listens on IPv4 and IPv6, so HAProxy logs the address
  of Alloy as `::ffff:<IPv4 address>`; the test strips that prefix before it compares the address.
- **F3, fixed: the log streams showed only at their end.** HAProxy logs a request when it ends, and a log stream ends
  only when Alloy closes it. `option logasap` logs it when its answer starts, and the test restarts Alloy before it
  reads the log, so the streams show with the configuration of the upstream image too, which has no `logasap`.
- **F4, fixed: the probe could add its requests to the list.** The probe of the runtime check runs inside Alloy, so its
  requests come from Alloy's address; the test reads the list before the runtime check runs.
- **F5, fixed: an earlier image could stand in for the configuration under test.** `compose up --build` builds the
  proxy at every run, so an image `nfhotel-socket-proxy:local` of an earlier run on the same host is not used.
- **F6, fixed in this review: a failure named an upgrade as the call of the list.** The line of a refused call left out
  the headers, so the upgrade of `GET /containers/json` read like the allowed call. It now says "(an upgrade to a
  WebSocket)". The function was run alone with prepared answers: an allowed call with the right answer, the upgrade let
  through, a refused call let through and a missing answer gave the expected lines.
- **F7, fixed in this review: an abbreviation of [ADR-0042].** API is spelled out at its first use.
- **F8, note: the test needs a shared root mount.** Alloy mounts `/` of the host with `rslave`, which needs `/` to be a
  shared mount. systemd makes it so on the CI runner and the VM; in WSL it is not, and `mount --make-rshared /` ran
  before each run. Nothing changes in the repository.
- **F9, note: the linters ran in WSL.** As F7 of [RC-346]: Docker Desktop is off on S01's PC, so the same images at the
  same versions (actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1) ran over the file lists of
  `lint.sh` in WSL. The job Ops linters runs `lint.sh` itself.

## Overall Verdict

Go. Every applicable criterion of [QC-SH-001], [QC-YML-001] and [QC-ADR-001] passes, and so do criteria 3 and 5 of
[MIL-081]. Criteria 1 and 2 need the job Ops monitoring configuration (MIL-023) of the pull request, criterion 6 its
checks and the scan gate of the release, and criterion 4 the deploy; the gate record of task 7 closes them. The reviewer
is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green and the list of calls printed by the job Ops monitoring configuration (MIL-023) the same as in ADR-0042 | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge and read the runtime check in its log: the answers of the proxy, the socket rule and Alloy healthy (task 6) | S01 (the AI as stand-in) | 2026-11-14 |

---

[MIL-081]: ../../milestones/mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
[ADR-0042]: ../../adr/adr-0042-only-the-calls-of-alloy-through-the-socket-proxy.md
[RC-344]: ./rc-344-mil-081.md
[CIS-001]: ../cis-checklist.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[RC-346]: ./rc-346-mil-080-code.md
