# RC-007 Review of compose files and smoke test

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-007 |
| CrossReference | [MIL-002], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S04 | Initial version | [b690b57] |

---

## Artifact Under Review

- Instance reviewed: `deploy/nginx/docker-compose.yml`, `deploy/nfhotel-web/docker-compose.yml`, `deploy/nginx/templates/default.conf.template`, `scripts/test-compose-proxy.sh`, implementing tasks 3 to 6 of [MIL-002]
- Checklist used: [QC-YML-001] for the two compose files and [QC-SH-001] for the smoke test. The nginx template has no checklist; it is covered by the smoke test. The root `Dockerfile` already on main is reused and was not changed.
- Scope: full review
- Language and domain: n/a (configuration and source code, technical types)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S04`)
- Tools: `yamllint` 1.32.0 with `.yamllint`, `docker compose config` (Compose v5.6.0), `shellcheck` 0.11.0, `shfmt` 3.14.1, `bash -n`

## Checklist Results

### YAML ([QC-YML-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader, no duplicate keys | Pass | yamllint 1.32.0 (key-duplicates rule) and `docker compose config` accept both files |
| 2 | 2-space indentation, LF, final newline, no trailing spaces | Pass | yamllint clean; files are ASCII with LF |
| 3 | Passes `yamllint` with committed configuration | Pass | `.yamllint` at the repository root, no `disable` comments |
| 4 | Coercible scalars are quoted | Pass | Ports and port-like values are quoted strings |
| 5 | Only safe, plain types | Pass | No tags, anchors or includes |
| 6 | No secret in the file | Pass | Both files use `env_file`; secrets exist only in git-ignored `<service>.env` files, and the examples hold placeholders |
| 7 | Embedded program text at most five lines | Pass | One single-line `healthcheck` command |
| 8 | Repeated blocks defined once | Pass | The `proxy` network block appears in both files because each service must be a standalone compose project (requirement of MIL-002); it is one line of knowledge, the network name, taken from `NFHOTEL_PROXY_NETWORK` in both |
| 9 | Anchors and aliases only where supported | N-A | None used |
| 10 | Validates against the tool's schema | Pass | `docker compose config` (Compose v5.6.0) renders both files |
| 11 | Header comment states purpose, tool and decision | Pass | Both files name the tool, the env file and the MIL-002 task |
| 12 | One key-case style | Pass | Compose keys; env var names are upper snake |
| 13 | Block scalars state chomping | N-A | No multi-line strings |
| 14 | Lowercase kebab-case file name, one extension style | Pass | `docker-compose.yml` is the tool's own name |
| 15 | Exercised by a check that runs without the real target | Pass | `scripts/test-compose-proxy.sh` runs both files with throwaway settings |

### Shell ([QC-SH-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | Lines 1 and 17 |
| 2 | Every expansion quoted; `[[ ]]` and `$(...)` | Pass | `shellcheck` clean |
| 3 | Naming conventions | Pass | `kebab-case.sh`, `snake_case` functions, `UPPER_SNAKE` constants |
| 4 | Passes `shellcheck` and `bash -n` | Pass | shellcheck 0.11.0, no disables |
| 5 | Errors to stderr with `error:`, non-zero exit, usage line | Pass | `die` helper; usage exits 2 |
| 6 | Temporary files use `mktemp` and `trap` | Pass | `mktemp -d`, removed by `trap cleanup EXIT` |
| 7 | No secret in script, echo or command line | Pass | The passport key is random per run, written to a temporary env file, never echoed |
| 8 | Dry run or explicit flag for state change outside own directory | Pass | The script only touches `nfhotel-smoke-*` containers and one private network, and removes them; header says so |
| 9 | Header comment complete | Pass | Purpose, usage, options, exit codes, needs, task |
| 10 | Implements a cited task | Pass | Header cites MIL-002 task 6 |
| 11 | Behaviour tested for success, failure and bypass | Pass | Recorded run: 6 checks pass, exit 0, 0 containers and 0 networks left. The failure path (a check fails) is covered by `check` counting failures and exiting 1; it was not provoked in this run |
| 12 | Formatted with `shfmt` | Pass | `shfmt -i 2 -ci` applied |
| 13 | Safe to re-run | Pass | Fixed project names; `down` runs on exit |
| 14 | Bash version and tools stated | Pass | Header: bash 4+, docker compose, curl, openssl |
| 15 | Repeated knowledge defined once | Pass | `compose_nginx` and `compose_web` wrap the repeated compose call; constants for names |

## Overall Verdict

Go — all Mandatory criteria pass or are not applicable with a stated reason. The smoke test passed end to end on 2026-10-16: HTTP 200 for `/`, 200 for `POST /_blazor/negotiate`, no published web port, proxy up with the web service stopped (502), web up with the proxy stopped.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Provoke a failing check once to record the failure path (criterion 11) | S01 | 2026-10-31 |

---

[MIL-002]: ../../milestones/mil-002-nginx-proxy-and-web-compose.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
