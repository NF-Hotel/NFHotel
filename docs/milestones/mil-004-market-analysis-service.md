# MIL-004 Market analysis service on a Proxmox VM behind nginx

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-004 |
| CrossReference | [BC-001], [US-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Proposed | Claude (AI) | AI as stand-in for S01 | Initial version: the Must stories US-001.01, US-001.03 and US-001.05 | pending |

---

## Purpose

Decide whether the market analysis service (`NF-Hotel/Notebook_Market_Analysis`)
can run on a Proxmox VM through Docker Compose behind the nginx proxy, so that
the owner can reach the market analysis, and whether nginx can route more than
one service. It delivers the Must stories of [US-001]; the Should stories (the
benchmark service and the model choice) follow in a later milestone.

## Deliverable

- A decision record for the Proxmox host and for a VM versus an LXC container.
- A Proxmox VM with Docker, running the market analysis service.
- `deploy/market-analysis/` with a Dockerfile, a compose file and `market-analysis.env.example`.
- An nginx configuration that routes more than one upstream service.
- A smoke test that proves the analysis answers through the proxy, and a list of the gaps against US-001.01 (charts).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the Proxmox host and VM versus LXC, and is reviewed | Yes | No |
| 2 | The analysis service starts on the VM with `docker compose up` using its own compose file and `market-analysis.env` | Yes | No |
| 3 | The smoke test gets HTTP 200 for the analysis through nginx, and the service publishes no host port | Yes | No |
| 4 | The web application and the analysis service are both reachable through the same nginx, each by its own route | Yes | No |
| 5 | The gaps against US-001.01 (charts, AI analysis) are listed with an owner | Yes | No |
| 6 | The Dockerfile, compose, YAML and shell files have a `Go` review where a checklist exists | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-002 | The nginx and web compose files that this milestone extends |
| MIL-003 | The throwaway Proxmox container for testing the scripts before they touch a real host |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 6 of the amended BC-001 | All tasks |
| US-001.01 View the market analysis | Tasks 6 and 7 |
| US-001.03 nginx in front of every web and API service | Task 5 |
| US-001.05 The market analysis service on a Proxmox VM | Tasks 3 and 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S02 |
| Approving reviewer | S01 |

## Target Date

2026-10-23 — one week after the start; stated in the amended Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objectives 6 and 7 (market analysis and benchmark services), the scope items, risks and the MIL-004 schedule row; review again. | No | |
| 2 | Decide the Proxmox host and VM versus LXC | Record in an ADR where the Proxmox host lives (the MIL-003 container is for tests only), whether the services run in a VM or an LXC container, and how a GPU would reach it for the later benchmark. | No | |
| 3 | Create the Proxmox VM with Docker | Create a VM on the chosen host with Docker and the compose plugin, by hand or with `iac-node-forge-py`, and record how. Test the scripts first against the MIL-003 container. | No | |
| 4 | Add the market analysis service files | `deploy/market-analysis/` with a Dockerfile that builds `Notebook_Market_Analysis` from a pinned git ref (it has none), a compose file using its HTTP service entry and its own `market-analysis.env` with a tracked example. No host port is published. | Yes | US-001.05 |
| 5 | Route more than one service through nginx | Extend the nginx template so each service has its own upstream and route (by host name or path) set from `nginx.env`, keeping the WebSocket upgrade for Blazor; the proxy keeps running when one upstream is down. | Yes | US-001.03 |
| 6 | Make the analysis visible to the owner | Expose the read-only marimo notebook of the analysis service behind the proxy and show its AI insights from a local model. List the gap to US-001.01: the notebook shows tables, not charts, so charts are a task in the source repository. | Yes | US-001.01 |
| 7 | Add the smoke test and README section | Extend the compose smoke test to start the analysis service and check HTTP 200 for it through nginx and that no host port is published; document the commands and env files in the README. | No | |
| 8 | Review code and record RC | Review the Dockerfile, compose YAML and shell script against `qc-yaml` and `qc-programming-shell`. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
