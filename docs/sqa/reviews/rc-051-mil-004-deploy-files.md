# RC-051 Review of the MIL-004 deploy files, as completed

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-051 |
| CrossReference | [MIL-004], [RC-016], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [9bbc0a3] |

---

## Artifact Under Review

- Instance reviewed: everything under `deploy/` (compose files for nginx, NFHotel web, PostgreSQL, Ollama, market analysis and benchmark, and the nginx templates) and `scripts/deploy.sh`, `scripts/deploy-services.sh`, `scripts/create-docker-vm.sh` and `scripts/test-compose-proxy.sh`, after the work that finishes [MIL-004]: one-script deploy of the services VM, internal PostgreSQL, Ollama, the benchmark service and the fix of the analysis notebook path. It supersedes [RC-016], which covered the first version.
- Checklist used: [QC-YML-001] for the compose files and [QC-SH-001] for the scripts. The Dockerfiles and nginx templates have no checklist and are covered by the smoke test and the deploy run.
- Scope: full review
- Language and domain: n/a (configuration and source code, technical types)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `yamllint` 1.38.0 with `.yamllint`, `docker compose config` for all six services, `shellcheck` 0.11.0, `shfmt` with `-i 2 -ci`, `bash -n`

## Go / No-Go criteria of MIL-004

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the Proxmox host and VM versus LXC, and is reviewed | Pass | [ADR-0003], RC-047 Go. The host is a Proxmox VE node reached over ssh; no real node address is in the repository |
| 2 | The analysis service starts on the VM with `docker compose up` using its own compose file and `market-analysis.env` | Pass | Started by `deploy.sh` in VM 100 on a Proxmox VE container: `market-analysis-api` and `market-analysis-notebook` healthy |
| 3 | HTTP 200 for the analysis through nginx, and no host port | Pass | In the VM, the notebook and `/health` answer 200 on the nginx loopback listener; only nginx has published ports; the smoke test passes |
| 4 | The web application and the analysis service are both reachable through the same nginx, each by its own route | Pass | The web application on the public port, the analysis by host name on the internal listener; the smoke test checks both |
| 5 | The gaps against US-001.01 are listed with an owner | Pass | The table in `deploy/market-analysis/README.md` (charts, AI analysis, input), owner S02 |
| 6 | The Dockerfile, compose, YAML and shell files have a `Go` review where a checklist exists | Pass | This record, below |

## Checklist Results

### YAML ([QC-YML-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader, no duplicate keys | Pass | yamllint 1.38.0 and `docker compose config` accept all compose files; no duplicate key |
| 2 | 2-space indentation, LF, final newline, no trailing spaces | Pass | yamllint clean |
| 3 | Passes `yamllint` with committed configuration | Pass | `.yamllint`, yamllint 1.38.0, `yamllint -c .yamllint deploy` prints nothing; no `disable` comment |
| 4 | Coercible scalars are quoted | Pass | Ports, `--port` arguments and the connection string are quoted |
| 5 | Only safe, plain types | Pass | No tags |
| 6 | No secret in the file | Pass | The database password is referenced as `${POSTGRES_PASSWORD}` and comes from the git-ignored `postgres.env`; the example files hold the placeholder `change-me` |
| 7 | Embedded program text at most five lines | Pass | The benchmark start command is a short shell command; the health checks are one line |
| 8 | Repeated blocks defined once | Pass | One image anchor in the analysis file; the network blocks repeat across compose projects by design (one file per service) |
| 9 | Anchors and aliases only where supported | Pass | The single anchor is expanded by Docker Compose |
| 10 | Validates against the tool's schema | Pass | `docker compose config -q` for postgres, ollama, benchmark, market-analysis, nfhotel-web and nginx |
| 11 | Header comment states purpose, tool and decision | Pass | Every file; the new ones cite the networks and ADR-0003 |
| 12 | One key-case style | Pass | Compose keys |
| 13 | Block scalars state chomping | Pass | The benchmark command uses `>-` |
| 14 | Lowercase kebab-case file name | Pass | `docker-compose.yml` |
| 15 | Exercised by a check that runs without the real target | Pass | `scripts/test-compose-proxy.sh` (9 of 9 checks) and a full deploy to a Proxmox VE container (below) |

### Shell ([QC-SH-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | All four scripts |
| 2 | Every expansion quoted; `[[ ]]` and `$(...)` | Pass | shellcheck clean |
| 3 | Naming conventions | Pass | `kebab-case.sh`, `snake_case` variables |
| 4 | Passes `shellcheck` and `bash -n` | Pass | shellcheck 0.11.0. Two `disable=SC2029` comments in `deploy.sh`, each with the reason: the remote command is meant to expand locally |
| 5 | Errors to stderr with `error:`, non-zero exit, usage line | Pass | `die` and `usage` in each script |
| 6 | Temporary files use `mktemp` and `trap` | Pass | `deploy.sh` and `deploy-services.sh` create none; the smoke test keeps its `mktemp -d` and `trap` |
| 7 | No secret in script, echo or command line | Pass | `deploy-services.sh` reads `POSTGRES_*` from `postgres.env` into the environment and never prints them; settings files travel over ssh by `tar` on stdin, mode 600 on the VM; the development ssh key has no passphrase and is used only for the throwaway container |
| 8 | Dry run or explicit flag for state change | Pass | `deploy.sh --dry-run` prints the Proxmox commands and stops; the headers say that a run changes the Proxmox node and the VM |
| 9 | Header comment complete | Pass | Purpose, usage, options, exit codes and tools in each |
| 10 | Implements a cited task | Pass | MIL-004 tasks 3 and 7 and ADR-0003 |
| 11 | Behaviour tested for success, failure and bypass | Pass | Success: a full run of `deploy.sh --local` against the Proxmox VE container `proxmox-node1` created VM 100 (12288 MiB) and brought six containers to healthy. Failure: the first run stopped with `error: the VM reported no address` and exit 1, and an earlier smoke test run stopped with `error: the market analysis service did not become healthy` and five failed checks; both were fixed. This records the failure path that RC-016 action item 1 asked for. Bypass: `--skip-vm`, `--dry-run` and `--down` were exercised by dry run and `bash -n` only |
| 12 | Formatted with `shfmt` | Pass for three, Fail (optional) for one | `shfmt -i 2 -ci` clean for `deploy.sh`, `deploy-services.sh` and `test-compose-proxy.sh`. `create-docker-vm.sh` keeps its one-line `case` arms from ADR-0003 and is not reformatted here, to keep that pull request's file unchanged |
| 13 | Safe to re-run | Pass | `deploy.sh` finds the VM by name and skips creation; the second run in this review reused VM 100; compose `up -d` is idempotent |
| 14 | Bash version and tools stated | Pass | Headers |
| 15 | Repeated knowledge defined once | Pass | The service order and directories are in one function each in `deploy-services.sh`; `deploy.sh` calls it in the VM instead of repeating it |

## Overall Verdict

Go — every Mandatory criterion of both checklists passes, and all six Go criteria of MIL-004 are met. What was not tested: a real Proxmox node over ssh (the run used a Proxmox VE container on this machine, as [MIL-003] intended), and GPU passthrough for the benchmark (the Ollama service runs on the CPU).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close action items 1 and 2 of [RC-016]: the failure path is recorded in criterion 11 above, and tasks 2 and 3 of [MIL-004] are done | S02 | Closed 2026-10-16 |
| Run `deploy.sh` once against the real Proxmox node and keep its address in the team's own notes, not in the repository | S02 | Open |

---

[MIL-003]: ../../milestones/mil-003-throwaway-proxmox.md
[MIL-004]: ../../milestones/mil-004-market-analysis-service.md
[RC-016]: ./rc-016-market-analysis-service.md
[ADR-0003]: ../../adr/adr-0003-proxmox-vm-for-services.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[9bbc0a3]: https://github.com/NF-Hotel/NFHotel/commit/9bbc0a31ca2065a8e6532d2e0ee61e4c51954ebf
