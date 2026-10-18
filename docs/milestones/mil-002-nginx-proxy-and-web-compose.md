# MIL-002 Nginx proxy and web service in Docker Compose

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-002 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S04 | Initial version | [b690b57] |
| 2026-10-16 | Accepted | Claude (AI) | S04 (confirmed language/domain); AI as stand-in for S04 | Accepted after RC-006 Go | [b690b57] |

---

## Purpose

Decide whether NFHotel can be run as containers: one Docker Compose file per
service, each with its own `<service>.env` file, starting with an nginx reverse
proxy and then the NFHotel web application behind that proxy.

## Deliverable

- `deploy/nginx/docker-compose.yml`, `deploy/nginx/nginx.env.example` and the nginx configuration template.
- `deploy/nfhotel-web/docker-compose.yml` and `deploy/nfhotel-web/nfhotel-web.env.example`.
- The existing root `Dockerfile` for `NFHotel.Web` is reused by the web compose file (no second Dockerfile).
- A smoke-test script and a recorded run showing the application answering through the proxy.
- A README deployment section.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Each service has its own compose file and its own `<service>.env`; `docker compose config` accepts both | Yes | No |
| 2 | `*.env` files are git-ignored and only `*.env.example` files are tracked; no secret is in git | Yes | No |
| 3 | The smoke test gets HTTP 200 for `/` through the proxy, with the web container not published on the host | Yes | No |
| 4 | The Blazor SignalR path `/_blazor/negotiate` answers through the proxy (WebSocket upgrade configured) | Yes | No |
| 5 | Stopping the web service does not stop the proxy, and the reverse | Yes | No |
| 6 | The compose YAML and shell files have a `Go` review against `qc-yaml` and `qc-programming-shell` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-001 | Reuses the `*.env` and `*.env.example` convention and the Docker-only host assumption |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 5 in an amended BC-001: run NFHotel as containers, one compose file and one env file per service | All tasks |
| Open questions Q-31 and Q-42 in `docs/refactor/OpenQuestions-Tracks.md` | Tasks 3 and 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S04 |

## Target Date

2026-10-18 — two days after the start of MIL-001; the Business Case is amended to allow it.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 5 (containers, one compose and one env file per service), the in-scope items and the PP schedule row for MIL-002; review again. The current BC-001 scope covers only the Proxmox tests. | No | |
| 2 | Reuse the web application Dockerfile | The repository root `Dockerfile` already builds `NFHotel.Web` (SDK 10.0.400 to ASP.NET runtime, port 8080, non-root user) and a root `.dockerignore` excludes `*.env`. The web compose file points at it instead of adding a second Dockerfile (one definition of the build). | No | |
| 3 | Add nginx proxy service | `deploy/nginx/docker-compose.yml` with `nginx.env` (host name, ports, upstream) and a config template that forwards to the web service and upgrades WebSocket connections for Blazor Server (`/_blazor`). Publishes the proxy port only. | No | |
| 4 | Add NFHotel web service | `deploy/nfhotel-web/docker-compose.yml` with `nfhotel-web.env` (connection string, passport encryption key, time zone, holiday API URL and key, forwarded-headers flag). Joins the shared proxy network and publishes no host port. PostgreSQL is not part of this service. | No | |
| 5 | Add shared network and README section | Define the external Docker network the two compose files share, the start order (proxy first or either), and document the commands and the env files in the README. | No | |
| 6 | Add smoke-test script | A bash script that starts both services, checks `/` and `/_blazor/negotiate` through the proxy, checks that the web port is not published, and tears everything down. Runs unattended. | No | |
| 7 | Review code and record RC | Review the compose YAML and the shell script against `qc-yaml` and `qc-programming-shell`. | No | |

---

[BC-001]: ../business-case.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
