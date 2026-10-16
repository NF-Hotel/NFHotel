# MIL-001 Proxmox integration test environment

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-001 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S04 | Initial version | pending |
| 2026-10-16 | Accepted | Claude (AI) | S04 (confirmed language/domain); AI as stand-in for S04 | Accepted after RC-003 Go | pending |

---

## Purpose

Decide whether the software engineer can verify, unattended and from a
throwaway Docker container, that the Proxmox VE server is reachable,
authenticated and healthy, using a read-only API token.

## Deliverable

- `pve.env.example` (tracked) and `pve.env` (ignored) with all connection settings.
- A .NET test project `tests/NFHotel.Proxmox.Tests` with read-only Proxmox API tests.
- A container image definition and a runner script that executes the tests with `docker run --rm`.
- A recorded run against the real Proxmox server.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The runner finishes without stdin and exits 0 when all checks pass, non-zero otherwise | Yes | No |
| 2 | `docker ps -a` lists no container from the runner after a run | Yes | No |
| 3 | A test proves the client sends only GET requests | Passes | Missing or failing |
| 4 | `git ls-files` contains `pve.env.example` and not `pve.env` | Yes | No |
| 5 | The run against the real server reports version and node status, or fails with a message naming the failing setting | Yes | No |
| 6 | The C# and shell code have a `Go` review against `qc-programming-csharp` and `qc-programming-shell` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| None | First milestone; Docker is the only tool required on the host |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 1: credential files | Task 1 |
| Objective 2: unattended throwaway runner | Task 3 |
| Objective 3: read-only integration tests | Task 2 |
| Objective 4: tests cannot change the server | Tasks 2 and 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S04 |

## Target Date

2026-10-17 — matches the Business Case constraint of one working session.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Add Proxmox credential files | Create `pve.env.example` (tracked) and `pve.env` (ignored by `*.env`) with host, port, node, API token id and secret, TLS verification and timeout. Document that the token needs role PVEAuditor so the tests are read-only. Supports Business Case objective 1 and decision D-06. | No | |
| 2 | Create Proxmox test project | Add `tests/NFHotel.Proxmox.Tests` (xUnit, .NET 9) with a settings loader reading `PVE_*` variables, a GET-only Proxmox client, and tests for API version, node status and the GET-only guarantee. Tests fail fast with a message naming the missing setting. | No | |
| 3 | Add container image and runner | Add a Dockerfile and a bash runner script that checks Docker is available, then runs the tests with `docker run --rm --env-file pve.env` and no TTY or stdin, so it works unattended and leaves no container behind. | No | |
| 4 | Run tests against real Proxmox | Run the runner against the real Proxmox server with the read-only token, confirm no container remains, and record the result. | No | |
| 5 | Review code and document usage | Review the C# and shell code against `qc-programming-csharp` and `qc-programming-shell`, record the RC, and add a short usage section (token creation, running the tests) to the README. | No | |

---

[BC-001]: ../business-case.md
