# MIL-003 Throwaway Proxmox container for tests

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-003 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S04 | Initial version | [88a4946] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (instructed by the user) | Accepted after RC-010 Go | pending |

---

## Purpose

Decide whether the Proxmox tests can run against a Proxmox VE that is itself a throwaway Docker container: started, given a per-run API token made from the root password in `pve.env`, tested, and removed, with no token ever written to a file.

## Deliverable

- `deploy/proxmox/docker-compose.yml` for the Proxmox VE container (image `dockurr/proxmox`, as already used in the sibling project `iac-node-forge-py`).
- `pve.env` and `pve.env.example` holding only the root password (`PVE_PASSWORD`).
- A script that creates an API token in the running container and sets it as environment variables for the current run.
- `scripts/run-proxmox-tests.sh` reworked to start the container, create the token, run the tests and remove everything.
- A recorded unattended run.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `pve.env` and `pve.env.example` contain only `PVE_PASSWORD`; `grep` finds no token in any file of the repository or the temporary directory after a run | Yes | No |
| 2 | One command starts the container, creates the token, runs the tests and removes the container and its volumes with no input | Yes | No |
| 3 | The three live tests pass against the container | Yes | No |
| 4 | `docker ps -a` and `docker volume ls` list nothing from the run afterwards | Yes | No |
| 5 | The test client still sends only GET requests; only the setup script creates the token | Yes | No |
| 6 | The shell and YAML files have a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-001 | Reuses the Proxmox test project and the unattended runner; this milestone replaces MIL-001 task 4 (the run against a real server, issue #30) |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Amended objectives 1, 2 and 4 of BC-001: throwaway Proxmox container, root password only | All tasks |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S04 |

## Target Date

2026-10-19 — after MIL-002; the Business Case is amended to allow it.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Change the Proxmox target from a real server to a throwaway container, root password only in `pve.env`, token created per run; review again. | No | |
| 2 | Add Proxmox container service | `deploy/proxmox/docker-compose.yml` using `dockurr/proxmox` (privileged, `/dev/kvm`, password from `PVE_PASSWORD`, API published on 127.0.0.1:18006 so it does not clash with other Proxmox containers on 8006). Feasibility was checked on 2026-10-16: it starts once a WSL distribution with the KVM modules has been started. | No | |
| 3 | Add token script | A bash script that runs `pveum user token add` in the container and prints `export` lines for `PVE_API_TOKEN_ID` and `PVE_API_TOKEN_SECRET`, to be evaluated by the caller, so the token only lives in the environment of the run. | No | |
| 4 | Rework the runner | `scripts/run-proxmox-tests.sh` starts the container, waits for the API, sets the root password from `pve.env` through standard input (`chpasswd`), then gets the token, runs the tests container on the Proxmox network with the token passed by name, and removes containers and volumes on exit. | No | |
| 5 | Slim pve.env files | `pve.env` and `pve.env.example` hold only `PVE_PASSWORD`; host, node and TLS settings get defaults in the runner. Remove the token variables from the example. | No | |
| 6 | Run, review and document | Run the full unattended cycle, check that no token is on disk and nothing is left behind, review the code, and update the README. | No | |

---

[BC-001]: ../business-case.md
[88a4946]: https://github.com/NF-Hotel/NFHotel/commit/88a49468aa21b75a0a5f9c8fd5a214164e1676ca
