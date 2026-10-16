# RC-011 Review of throwaway Proxmox code

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-011 |
| CrossReference | [MIL-003], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S04 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: `deploy/proxmox/docker-compose.yml`, `scripts/pve-token.sh`, `scripts/run-proxmox-tests.sh`, implementing tasks 2 to 5 of [MIL-003]
- Checklist used: [QC-YML-001] for the compose file and [QC-SH-001] for the two scripts
- Scope: full review
- Language and domain: n/a (configuration and source code, technical types)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S04`)
- Tools: `yamllint` 1.32.0 with `.yamllint`, `docker compose config` (Compose v5.6.0), `shellcheck` 0.11.0, `shfmt` 3.14.1, `bash -n`

## Checklist Results

### YAML ([QC-YML-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader, no duplicate keys | Pass | yamllint and `docker compose config` accept the file |
| 2 | 2-space indentation, LF, final newline, no trailing spaces | Pass | yamllint clean |
| 3 | Passes `yamllint` with committed configuration | Pass | `.yamllint`, yamllint 1.32.0, no `disable` comments |
| 4 | Coercible scalars are quoted | Pass | The port mapping is a quoted string |
| 5 | Only safe, plain types | Pass | No tags |
| 6 | No secret in the file | Pass | The password is `${PVE_PASSWORD:?...}` read from the git-ignored `pve.env`; no default |
| 7 | Embedded program text at most five lines | Pass | None |
| 8 | Repeated blocks defined once | Pass | No repetition |
| 9 | Anchors and aliases only where supported | N-A | None used |
| 10 | Validates against the tool's schema | Pass | `docker compose config` renders the file |
| 11 | Header comment states purpose, tool and decision | Pass | Names the tool, the env file, the task and the warning about the third-party privileged image |
| 12 | One key-case style | Pass | Compose keys |
| 13 | Block scalars state chomping | N-A | No multi-line strings |
| 14 | Lowercase kebab-case file name | Pass | `docker-compose.yml` is the tool's own name |
| 15 | Exercised by a check that runs without the real target | Pass | `docker compose config`, and the full run below |

### Shell ([QC-SH-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | Both scripts |
| 2 | Every expansion quoted; `[[ ]]` and `$(...)` | Pass | `shellcheck` clean; the one `eval` runs only the output of `pve-token.sh`, which prints three `export` lines of values Proxmox returned |
| 3 | Naming conventions | Pass | `kebab-case.sh`, `snake_case` functions, `UPPER_SNAKE` constants |
| 4 | Passes `shellcheck` and `bash -n` | Pass | shellcheck 0.11.0, no disables |
| 5 | Errors to stderr with `error:`, non-zero exit, usage line | Pass | `die` helper; usage exits 2 |
| 6 | Temporary files use `mktemp` and `trap` | N-A | No temporary files; `trap cleanup EXIT` removes containers and volumes |
| 7 | No secret in script, echo or command line | Pass | The token is printed only to the command substitution of the runner, held in memory, and passed to `docker run` by name (`--env NAME`); a search of the repository found no token |
| 8 | Dry run or explicit flag for state change outside own directory | Pass | The scripts only start and remove their own `nfhotel-pve` containers and the test container; `pve-token.sh` writes only inside the named throwaway container, and its header says so |
| 9 | Header comment complete | Pass | Purpose, usage, exit codes, needs, task |
| 10 | Implements a cited task | Pass | Headers cite MIL-003 tasks 3 and 4 |
| 11 | Behaviour tested for success, failure and bypass | Pass | Recorded runs: success (12 tests passed, exit 0, no container, volume or network left); failure when `/dev/kvm` is missing (clear message, exit 1); failure when the token step could not read the node (the run exposed that a failing `pve-token.sh` was ignored by `eval`; fixed so the runner dies) |
| 12 | Formatted with `shfmt` | Pass | `shfmt -i 2 -ci` clean |
| 13 | Safe to re-run | Pass | Fixed project name; `down --volumes` on exit; each run makes a new token name |
| 14 | Bash version and tools stated | Pass | Header: bash 4+, docker compose, curl, `/dev/kvm` |
| 15 | Repeated knowledge defined once | Pass | `compose_proxmox`, constants for names and the port |

## Overall Verdict

Go — all Mandatory criteria pass or are not applicable with a stated reason. Full cycle on 2026-10-16: Proxmox VE container started, API answered, per-run token created, 12 of 12 tests passed (the 3 live ones included), everything removed.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Note in the README that the KVM check needs a running WSL distribution with the KVM modules (done in the README section) | S01 | Closed 2026-10-16 |

---

[MIL-003]: ../../milestones/mil-003-throwaway-proxmox.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
