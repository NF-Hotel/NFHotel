# RC-016 Review of the market analysis service files

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-016 |
| CrossReference | [MIL-004], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [469c395] |

---

## Artifact Under Review

- Instance reviewed: `deploy/market-analysis/docker-compose.yml`, `deploy/nginx/docker-compose.yml` and its templates (refactored to a shared `snippets/proxy.conf`), `scripts/test-compose-proxy.sh` (extended), implementing tasks 4, 5, 6 and 7 of [MIL-004]
- Checklist used: [QC-YML-001] for the two compose files and [QC-SH-001] for the smoke test. The Dockerfile and the nginx templates have no checklist; they are covered by the smoke test.
- Scope: full review
- Language and domain: n/a (configuration and source code, technical types)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `yamllint` 1.32.0 with `.yamllint`, `docker compose config` (Compose v5.6.0), `shellcheck` 0.11.0, `shfmt` 3.14.1, `bash -n`

## Checklist Results

### YAML ([QC-YML-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader, no duplicate keys | Pass | yamllint and `docker compose config` accept both files |
| 2 | 2-space indentation, LF, final newline, no trailing spaces | Pass | yamllint clean |
| 3 | Passes `yamllint` with committed configuration | Pass | `.yamllint`, yamllint 1.32.0, no `disable` comments |
| 4 | Coercible scalars are quoted | Pass | Ports and the `--port` argument are quoted strings |
| 5 | Only safe, plain types | Pass | No tags |
| 6 | No secret in the file | Pass | The service needs no secret; settings come from the git-ignored `market-analysis.env` |
| 7 | Embedded program text at most five lines | Pass | The two health checks are one-line Python statements |
| 8 | Repeated blocks defined once | Pass | The image tag is one anchor; the `proxy` network block repeats across compose projects by design (separate files per service) |
| 9 | Anchors and aliases only where supported | Pass | The single anchor `&analysis-image` is expanded by Docker Compose; `docker compose config` shows it resolved |
| 10 | Validates against the tool's schema | Pass | `docker compose config` renders the file |
| 11 | Header comment states purpose, tool and decision | Pass | Both files |
| 12 | One key-case style | Pass | Compose keys |
| 13 | Block scalars state chomping | N-A | No multi-line strings |
| 14 | Lowercase kebab-case file name | Pass | `docker-compose.yml` is the tool's own name |
| 15 | Exercised by a check that runs without the real target | Pass | `scripts/test-compose-proxy.sh` builds and starts the service with throwaway settings |

### Shell ([QC-SH-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | Unchanged |
| 2 | Every expansion quoted; `[[ ]]` and `$(...)` | Pass | `shellcheck` clean; the header array is expanded as `"${header[@]}"` |
| 3 | Naming conventions | Pass | |
| 4 | Passes `shellcheck` and `bash -n` | Pass | shellcheck 0.11.0 |
| 5 | Errors to stderr with `error:`, non-zero exit, usage line | Pass | `die` helper |
| 6 | Temporary files use `mktemp` and `trap` | Pass | `mktemp -d`, removed by `trap cleanup EXIT`; the analysis stack is removed too |
| 7 | No secret in script, echo or command line | Pass | No secret is introduced; the passport key stays random per run |
| 8 | Dry run or explicit flag for state change outside own directory | Pass | Touches only `nfhotel-smoke-*` projects and one private network; header says so |
| 9 | Header comment complete | Pass | Cites MIL-002 task 6 and MIL-004 task 7 |
| 10 | Implements a cited task | Pass | |
| 11 | Behaviour tested for success, failure and bypass | Pass | Recorded run on 2026-10-16: 9 checks pass, exit 0, 0 containers, networks and volumes left. A failing check was not provoked (see action item 1) |
| 12 | Formatted with `shfmt` | Pass | `shfmt -i 2 -ci` clean |
| 13 | Safe to re-run | Pass | Fixed project names; `down --volumes` on exit |
| 14 | Bash version and tools stated | Pass | Header |
| 15 | Repeated knowledge defined once | Pass | One `compose_*` wrapper per service; the shared proxy settings moved to `snippets/proxy.conf` and the WebSocket `map` to one template |

## Overall Verdict

Go — all Mandatory criteria pass or are not applicable with a stated reason. The smoke test passed on 2026-10-16: the analysis notebook answers 200 on `analysis.localhost` and the API answers 200 on `/health` on `analysis-api.localhost`, both through the proxy, with no host port published, next to the web application.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| 1. Provoke a failing check once to record the failure path (criterion 11; same open item as RC-007) | S02 | 2026-10-31 |
| 2. Tasks 2 and 3 of MIL-004 (host decision, VM) are open: the files were tested on the local Docker only | S02 | 2026-10-23 |

---

[MIL-004]: ../../milestones/mil-004-market-analysis-service.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[469c395]: https://github.com/NF-Hotel/NFHotel/commit/469c3959b999487820f8ffa1c24aa4866e61cfe7
