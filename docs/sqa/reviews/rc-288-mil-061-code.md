# RC-288 Code review of MIL-061: the network nfhotel-python, nginx without the internal listener, the tests and ADR-0039

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-288 |
| CrossReference | [MIL-061], [ADR-0039], [ADR-0004], [ADR-0006], [QC-SH-001], [QC-YML-001], [QC-WF-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 6 of MIL-061 before its pull request | [2622186] |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-061-python-services-network`, 31 files, 363 insertions and 226 deletions against `main` at bf854cf):
  - [ADR-0039], new, and the amendments of [ADR-0004] (the network bullet) and [ADR-0006] (the internal-listener bullet), each with a Version History row (task 1);
  - `ops/scripts/deploy-services.sh` (the network `nfhotel-python`, created `--internal`; nginx no longer waits for the analysis API; the header) and `ops/scripts/lib/stack.sh` (`STACK_PYTHON_NET`, created, passed to compose and removed; the `ANALYSIS_*` lines of `nginx.env` gone) (tasks 2 and 4);
  - the compose files and settings examples of `ops/deploy/nfhotel-web`, `ops/deploy/nfhotel-api`, `ops/deploy/market-analysis` and `ops/deploy/benchmark` (task 3);
  - `ops/deploy/nginx/docker-compose.yml` and `nginx.env.example` without the listener on 8081, `ops/deploy/nginx/templates/market-analysis.conf.template` deleted, `ops/deploy/dev-lite/nginx-port.yml`, `ops/deploy/market-analysis/README.md`, `ops/scripts/deploy.sh` and `ops/scripts/test-compose-proxy.sh` (task 4);
  - `ops/tests/test-service-networks.sh`, new, with the job `Ops service networks (MIL-061)` of `.github/workflows/ops-ci.yaml`, and the changes of `ops/tests/test-nginx.sh`, `ops/tests/test-optional-services.sh`, `ops/tests/test-benchmark-volume.sh` and `ops/tests/check-e2e-stack.sh` (task 5);
  - the wiki pages Network Isolation, Containers and Images, Environment Files, Proxmox Deployment and Workflows (task 6).
- **Checklists used:** [QC-SH-001] for the scripts and tests, [QC-YML-001] for the compose files, the settings examples and the workflow, [QC-WF-001] for the new job, [QC-ADR-001] for ADR-0039 and the two amendments.
- **Scope:** full review of the new files; delta review of the changed ones against `main`.
- **Mode:** autonomous, not independent. The AI wrote the code and this record; S01 accepted the plan of MIL-061 in chat on 2026-11-05 ("Accepter planen"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-05, on the PC of S01 (Windows 11, Git Bash, Docker Desktop with Docker 29.7.2 and Compose 5.5.0, jq 1.8.2):** `ops/tests/lint.sh` in containers (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1); `ops/tests/test-service-networks.sh` (49 checks); `ops/tests/test-wiki-coverage.sh` (91); `ops/tests/test-workflow-policy.sh` (the 14 workflows and actions, ADR-0037); `ops/tests/check-layout.sh`; `ops/tests/test-deploy-services-env.sh` (16, with a fake `docker`); the hardened nginx of `ops/deploy/nginx/docker-compose.yml` started by hand with the changed file (healthy, `docker port` shows 80 and 443 only, no `listen 8081` in the rendered configuration, port 80 answers 301 and 443 answers 502 without a web application); the compose steps of `ops/tests/test-benchmark-volume.sh` (the service turns healthy on the python network, the one-shot service keeps its capabilities and has no network). `test-nginx.sh` and `test-benchmark-volume.sh` cannot run whole on Windows (the path conversion of Git Bash breaks their bind mounts and `stat` calls, before and after this change), so the Ops CI run of the pull request is their proof, and the merge waits for it.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1, 2, 3 | Header line, `set` options, quoting, names | Pass | `test-service-networks.sh` starts with `#!/usr/bin/env bash` and `set -uo pipefail`; no `-e`, as every test of `ops/tests/` does, so every check runs and is counted (the header says so). Every expansion is quoted; the two unquoted ones (`env ${SETTINGS[$1]}`, `$(networks_of …)`) split a list on purpose and carry a `shellcheck disable` with the reason. `kebab-case.sh`, `snake_case` functions, `UPPER_SNAKE` tables |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean on the final files. Three findings of the first runs were fixed before this record: a flag the status lines of `deploy-services.sh` still read (SC2154; the flag is kept), hyphenated keys of the associative arrays, which shfmt reads as arithmetic (now quoted), and `$` inside single quotes in two `grep` patterns (now `grep -F` with double quotes) |
| SH 5, 9, 14 | Errors, header, needs | Pass | The test prints `FAIL` lines with the value got and expected and exits 1; its header states purpose, usage, the containers and networks it makes, exit codes and needs (bash 4+, docker with the compose plugin, jq) |
| SH 6, 13 | Temporary objects, re-run | Pass | The test makes no file; its containers and networks are named `t-net-*`, removed by a `trap … EXIT` and by a cleanup before the run, so a second run starts clean |
| SH 7 | No secret | Pass | `API_TOKEN=test-token` and the other values the test passes to `docker compose config` are placeholders for required variables, never real; the settings examples it reads hold `<placeholders>`; nothing is echoed |
| SH 8 | State outside the script | Pass | The test changes only Docker objects of its own name; `deploy-services.sh` keeps its behaviour, with one network more created on the host it deploys to |
| SH 10 | Implements the task it cites | Pass | MIL-061 tasks 2, 4 and 5 and ADR-0039: the network keys of the compose files are compared with the table of the wiki page, the listener is gone, the isolation is proven with stand-ins. One deviation from the plan: nginx no longer waits for the analysis API in `deploy-services.sh`, because the wait existed for the route that is removed; recorded in the Findings |
| SH 11 | Tested for success, failure and the bypass path | Pass | The networks test passed 49 of 49 on the final files, and failed as it should on the way: 10 checks before the field separator was fixed, 1 before the empty row of `results-owner` was read as a row, and the live part gave `000` from the proxy network and `200` from the python network in every run. `test-deploy-services-env.sh` passes with the new network. The hardened nginx and the benchmark compose file were started by hand, as the run list above says |
| SH 15 | Knowledge once | Pass | The network table is in one place of the test (and on the wiki page, which the test keeps true); `config`, `networks_of`, `standin` and `reach` are the four helpers; the network name has one default per compose file and one variable in `deploy-services.sh` and `stack.sh`, as the four existing networks have |
| YML 1, 2, 3, 4, 5 | Parse, layout, yamllint, coercion, safe types | Pass | `docker compose config` (Compose 5.5.0) reads all ten compose files with their example settings; the network keys are plain strings; 2-space indentation, LF, one final newline; yamllint 1.35.1 clean (a step name over 100 characters was shortened) |
| YML 6 | No secret | Pass | The compose files keep their secrets as files (ADR-0024); the settings examples hold placeholders; `nginx.env.example` loses six variables and gains none |
| YML 8, 9 | Repetition, anchors | Pass | Each compose file declares the external network it uses with `name: ${NFHOTEL_PYTHON_NETWORK:-nfhotel-python}` and `external: true`, the form of the four existing networks; the knowledge "which networks a service is on" is in its own compose file and nowhere else, which is what the test reads. No anchors added |
| YML 11 | Header says what and why | Pass | The header of every changed compose file and settings example names the networks after the move and ADR-0039; the stale sentence of the API's compose file ("shared with the web service only") is gone |
| YML 15 | Exercised without the real target | Pass | The networks test is exactly that: `docker compose config` of every file, then stand-ins |
| WF 1, 3, 5, 14 | YAML checklist, keys, names, runner and timeout | Pass | The job `service-networks` has a descriptive `name`, `runs-on: ubuntu-24.04`, `timeout-minutes: 10`, two named steps |
| WF 4 | Header | Pass | The header of `ops-ci.yaml` names the networks of the services (ADR-0039) among what it checks |
| WF 6, 24 | actionlint, the static test | Pass | actionlint 1.7.7 clean; `test-workflow-policy.sh` passes (the seven rules of ADR-0037) |
| WF 7, 8, 9, 13 | Permissions, pins, secrets, token | Pass | `permissions: contents: read` at the workflow level, nothing raised; `actions/checkout` pinned to the same commit SHA as the other jobs, `persist-credentials: false`; no secret, no token |
| WF 20 | Inline script | Pass | One command: `bash ops/tests/test-service-networks.sh` |
| WF 22 | Verified on the host | Not yet | The Ops CI run of the pull request on GitHub Actions is the proof; the ruleset lets the pull request merge only when every required check is green |
| ADR 1, 11 | ID, statuses | Pass | `ADR-0039` matches `adr-0039-python-services-network.md`, the next number of the registry; the rows of ADR-0039, ADR-0004 and ADR-0006 use `Proposed`, `Accepted` and `Deprecated` only |
| ADR 2, 3, 4 | Context, options, decision | Pass | The context states today's layout, the forces and four options, the fourth being the developer's first draft and why it was rejected; the decision is one sentence and is option 3 |
| ADR 5, 6 | Consequences, quality attributes | Pass | Five positive and five negative consequences; security, maintainability, operability and performance named |
| ADR 7 | Affected artifacts linked and in CrossReference | Pass | MIL-061, ADR-0004, ADR-0006, ADR-0003, ADR-0015 and ADR-0028 are links and in the CrossReference; ADR-0034 is cited in the text as the example of tailnet access, not as an affected artifact |
| ADR 8 | Version History | Pass | ADR-0004 and ADR-0006 each get a new row that names ADR-0039 and MIL-061; their retained rows keep their text and move to `Deprecated`; the oldest rows and the unused commit link of ADR-0006 are dropped by the two-row rule. ADR-0004 had Windows line endings in the working tree; the file is written with LF, as `.gitattributes` says, and `git diff` shows the ten changed lines only |
| MIL-061 1 | The networks of every service equal the table | Pass (locally) | 49 of 49 in `test-service-networks.sh`, among them the network keys of all twelve services of the ten compose files, the ports of nginx (80, 443) and the relay (5432), and `--internal` for the python network in both scripts |
| MIL-061 2 | nginx publishes 80 and 443 only; no 8081 left | Pass (locally) | The three `grep` checks of the test find no `listen 8081`, `NGINX_INTERNAL_PORT` or `ANALYSIS_*` under `ops/`; the hardened nginx started by hand publishes 80 and 443 only. `test-nginx.sh` runs in Ops CI (the two mobile routes and the public listener 8082 are among its 24 checks) |
| MIL-061 3 | The isolation, live | Pass (locally) | The ten live checks: the proxy network reaches none of the three stand-ins, the api network reaches the API only, the python network reaches all three, the llm network does not reach the API |
| MIL-061 4 | The existing stacks | Pending | `check-e2e-stack.sh` (with the two new checks on the python network), `test-dev-lite.sh`, `test-benchmark-volume.sh` and `test-nginx.sh` run in Ops CI and E2E; locally, the harness and the compose steps that could run passed |
| MIL-061 7, 8 | Documentation and this review | Pass | The five wiki pages describe the running design and `test-wiki-coverage.sh` passes 91 of 91; this record is the review of criterion 8, Go, and ADR-0039 with the two amendments is Accepted with it |

## Findings

- **nginx no longer waits for the analysis API.** `deploy-services.sh` made nginx wait until `market-analysis-market-analysis-api-1` was healthy, because nginx proxied to it on 8081. The route is gone, so the wait is gone; nginx waits for the web application only, and the `die` message of `require` names the order without `analysis`. The flag `analysis_skipped` stays: the status lines at the end still say when the analysis was left out of a build.
- **The smoke test `test-compose-proxy.sh` no longer starts the market analysis.** It started it to check the route through the proxy; without the route the build of the analysis image (minutes, and the internet) bought nothing. The test now creates the python network for the web application and the API, and checks that nginx is not on it while both are, and that nginx publishes 80 and 443 only. It is a manual test, not in CI; it was read, `bash -n` and the linters passed, and it was not run here.
- **`test-optional-services.sh` was not run.** It needs the two Python images from the registry and runs on a developer machine by design. Its change is the network rename and two negative checks from a proxy-like network; the live part of `test-service-networks.sh` proves the same isolation with stand-ins in Ops CI.
- **The new check is not yet required by the ruleset.** The ruleset "Approve merges" names its 41 required checks; `Ops service networks (MIL-061)` runs on every pull request but blocks a merge only once S01 adds it (SAR-001, SEC-03: every check that runs on a pull request is required). The Workflows wiki page lists the job and keeps the count of the ruleset as it is until then.
- **One Windows lesson in the test.** `jq` on Windows prints CRLF and `read` collapses empty tab-separated fields, so the test joins its fields with `|` and strips `\r`; it runs the same on Linux. Recorded here so that the next test does not find it again.

## Overall Verdict

Go. Every Mandatory criterion of the four checklists passes on the files as they are; the two criteria that need the host (WF 22, MIL-061 criterion 4) close with the green Ops CI and E2E runs of the pull request, which the ruleset requires before the merge. ADR-0039 and the amendments of ADR-0004 and ADR-0006 are Accepted with this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green, and read the job `Ops service networks (MIL-061)` on it (WF 22, criteria 1 to 4) | S01 (AI as stand-in) | At the merge |
| Add `Ops service networks (MIL-061)` to the required checks of the ruleset "Approve merges", and change the count on the Workflows wiki page | S01 | After the merge |
| Under "Review deployments", approve the CD run of the merge of MIL-061 (ADR-0038) | S01 | At the merge |
| Give the approval in chat for the check-mode read of `nf-hotel` after the deploy (MIL-061 task 8) | S01 | After the deploy |

---

[MIL-061]: ../../milestones/mil-061-python-services-network.md
[ADR-0039]: ../../adr/adr-0039-python-services-network.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[ADR-0006]: ../../adr/adr-0006-tls.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[2622186]: https://github.com/NF-Hotel/NFHotel/commit/2622186882fa901355733d4a584126df95dfa6f1
