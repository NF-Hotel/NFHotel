# Business Case

## Metadata
| Key | Value |
| --- | --- |
| ID | BC-001 |
| CrossReference | [SA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | S04 (confirmed language/domain); AI as stand-in for S04 | Amended (accepted after RC-005 Go): objective 5 and scope for running NFHotel as containers (nginx proxy and web service, one compose file and one env file per service); target date for MIL-002 | [b690b57] |
| 2026-10-16 | Proposed | Claude (AI) | AI as stand-in for S04 | Amended: Proxmox VE is a throwaway Docker container; pve.env holds only the root password; the API token is created per run and kept in environment variables | pending |

---

## Executive Summary

NFHotel 2.0 is a Blazor Server back office on Clean Architecture with
PostgreSQL. The DevOps track still has no decided place to run integration
tests or to host the application (the open questions Q-30, Q-31 and Q-42 in
`docs/refactor/OpenQuestions-Tracks.md`). This project gives the software
engineer a repeatable way to prove, from a disposable Docker container and
without any user interaction, that the team's Proxmox VE server is reachable and
healthy before anything is deployed or tested against it. The Proxmox VE under test is itself a throwaway Docker container. The git-ignored `pve.env` holds only its root password; an API token is created for every run and lives only in environment variables. A committed `pve.env.example` documents the format. A second step runs NFHotel itself as containers: one Docker Compose file and one `<service>.env` file per service, starting with an nginx reverse proxy and then the web application behind it.

## Methodological and Standards Foundation

The artifacts follow the SQA and QC framework mounted at `framework/` (Larman,
*Applying UML and Patterns*, for analysis and design artifacts) and the quality
characteristics of ISO/IEC 25010:2023. Code is reviewed against the `qc-programming-*`
checklists. Existing project analysis lives in `docs/refactor/` (in Danish,
informal notes, not framework artifacts) and is the source for context here.

## Problem Statement

- No automated check exists that the Proxmox VE server the team intends to use
  is reachable and correctly credentialed; a failure is found by hand, late.
- A test that needs a person to type a password or accept a prompt cannot run in
  CI or overnight.
- A test run on the developer's workstation depends on what is installed there
  and leaves state behind.
- Decision D-06 and the note on D-07 (`docs/refactor/Decisions.md`) record that
  credentials must never sit in the repository.

## Business Opportunity

A single command runs a throwaway container, checks the Proxmox VE API with a
read-only token, prints a pass or fail, and removes the container. The same
command can later run in the CI pipeline (Q-42) and gives DevOps the factual
basis for the hosting decision (Q-31).

## Objectives

1. Provide `pve.env.example` (committed) and `pve.env` (git-ignored) holding only
   the Proxmox root password; the API token is created for each run and is kept
   in environment variables, never in a file.
2. Provide a runner that starts a Proxmox VE container, runs the Proxmox tests in a
   second container, and removes both with their volumes afterwards, with no
   interactive input.
3. Provide read-only integration tests that verify API reachability,
   authentication, version and node status.
4. Guarantee that the test client cannot change anything on the Proxmox server; only the token script writes, and only inside the throwaway container.
5. Run NFHotel as containers: one Docker Compose file and one `<service>.env` file per service, with an nginx reverse proxy in front of the web application.

## Scope

### In Scope

- Credential files `pve.env` and `pve.env.example`.
- A container image definition and a runner script.
- A .NET test project `NFHotel.Proxmox.Tests` with read-only Proxmox API checks.
- A Docker Compose file for the throwaway Proxmox VE container and a script that creates the per-run API token.
- Reuse of the existing root `Dockerfile` for the web application and one Docker Compose file per service (nginx proxy, NFHotel web), each with its own `<service>.env` and a tracked `<service>.env.example`.
- A smoke test that proves the web application answers through the proxy.

### Out of Scope

- Tests against a real, permanent Proxmox host (decided by the DevOps track).
- Creating, starting, changing or deleting virtual machines or containers on
  Proxmox.
- Deploying NFHotel itself to Proxmox (decision Q-31 stays with DevOps).
- A CI pipeline definition (decision Q-42).
- Secret storage for production (decision D-07).
- Tests that need a graphical interface.
- PostgreSQL hosting: the web service only receives a connection string.
- TLS certificates: the proxy serves plain HTTP for now.

## Expected Benefits

### Tangible Benefits

- A Proxmox connectivity problem is found in under one minute by an unattended
  run instead of by hand.
- No state is left on the workstation after a run: the container is removed.

### Intangible Benefits

- Confidence in the target environment before the DevOps track commits to it.
- A pattern for further infrastructure tests that never touch the host desktop.

## Strategic Alignment

Supports the DevOps track of NFHotel 2.0 (open questions Q-30, Q-31, Q-42) and
the rule that GUI and end-to-end tests never run on the host desktop
(`framework/process/gui-testing.md`).

## Success Criteria

| # | Criterion | Target | Measure |
| --- | --- | --- | --- |
| 1 | Unattended run | 0 prompts or manual inputs during a run | The runner finishes with exit code 0 or 1 without stdin |
| 2 | Throwaway container | 0 containers left after a run | `docker ps -a` shows none from the runner |
| 3 | Read-only | 0 requests with a method other than GET | A test fails if the client sends any other method |
| 4 | Secrets safe | 0 secrets in git | `pve.env` is ignored; `git ls-files` lists no `pve.env` |
| 5 | Speed | Run completes in 120 seconds or less once the image is built | Wall-clock time of the runner |
| 6 | Web through proxy | HTTP 200 for `/` and an answer from `/_blazor/negotiate` through the proxy, with no host port published by the web service | Smoke-test script exit code 0 |
| 7 | One env file per service | 2 compose files, 2 tracked `.env.example` files, 0 tracked `.env` files | `git ls-files` |
| 8 | No token on disk | 0 files containing the API token after a run | A search of the repository and the temporary directory finds no token |

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| The token is a root token without privilege separation | A test bug could change the container | The container is throwaway; the HTTP client only exposes GET and a test asserts it; the token is never written to a file |
| KVM is not available to Docker | The Proxmox container cannot start | The runner checks `/dev/kvm` first and stops with a message naming the cause |
| Self-signed certificate on Proxmox | Connection refused | `PVE_VERIFY_TLS=false` is an explicit, documented setting |
| Server unreachable from the container | Tests fail | Short timeout (`PVE_TIMEOUT_SECONDS`) and a clear message naming the host |
| Secrets committed by mistake | Credential leak | `*.env` is git-ignored; only the example file is tracked |
| Docker not installed or not running | Runner cannot start | Runner checks first and exits with a clear message |
| Secrets placed in a compose file or image | Credential leak | Settings only in the git-ignored `<service>.env`; the compose files reference it with `env_file` |
| Proxy does not upgrade WebSocket connections | Blazor Server pages load but are not interactive | The nginx template sets the upgrade headers for `/_blazor`; the smoke test checks the path |

## Assumptions

- The Proxmox VE server is reachable by network from the developer's machine
  and from the container.
- Docker can run privileged containers with `/dev/kvm` (on WSL2 a distribution with the KVM modules must have been started).
- `pve.env` holds a throwaway root password for the test container only.
- S01 (Team 2, the owner of NF Hotel, all developers) is the Proxmox administrator for now; S04 (Valdamar) stands in for the Product Owner and reviews.

## Constraints

- Docker is the only required tool on the host.
- The test client is read-only; it runs against a throwaway Proxmox VE container.
- Target date 2026-10-17 for MIL-001 and 2026-10-18 for MIL-002; one working session each.
- Existing solution conventions apply (.NET 9 SDK per `global.json`).

## Cost–Benefit Assessment

Qualitative, because no money changes hands and the effort is one working session.

| Costs | Benefits |
| --- | --- |
| About one working day of engineering | Early, repeatable detection of Proxmox problems |
| Small container image to maintain | Decision input for Q-31 and Q-42 |
| One more test project in the solution | Reusable unattended infrastructure-test pattern |
| About half a day for the compose files, image and smoke test | A reproducible way to run NFHotel behind a proxy |

## Stakeholders

| Stakeholder ID (SA) | Interest in this project |
| --- | --- |
| S01 | Needs unattended, repeatable Proxmox tests |
| S02 | Needs the Proxmox server proven before relying on it |
| S03 | Needs assurance that tests never change the server |
| S04 | Needs a plan and reviews he can accept or reject |

## Recommendation

Proceed — the cost is one working session and it removes a manual, error-prone check.

---

[SA-001]: ./stakeholder-analysis.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
