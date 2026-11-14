# Business Case

## Metadata
| Key | Value |
| --- | --- |
| ID | BC-001 |
| CrossReference | [SA-001], [UCD-001], [US-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | Claude (AI) | S01 | Amended: objective 29 says that the market analysis service makes the price suggestions from holidays and the occupancy history of the next two months, and the back office presents them (MIL-071); the website no longer works them out; accepted by S01 in chat on 2026-11-11 | [f674e8a] |
| 2026-11-12 | Accepted | mikemike | AI as stand-in for S01 | Brought in line with the products as they run on 2026-11-11, at S01's request in chat ("Ret det så der er overensstemmelse med det aktuelle", "Indkluder dem alle"): the summary, problem, opportunity, scope, benefits, risks, assumptions, constraints (.NET 10), stakeholders and recommendation<br>Objectives 6, 7, 18 and 28 and success criteria 7, 9, 21, 27, 31 and 32 as they are now<br>Objectives 30 to 40 and success criteria 33 to 43 for the eleven products that had none<br>Objectives 18, 31, 32 and 34 and success criteria 34, 35 and 37 extended, objective 41 and success criterion 44 added (configuration as code), and two lines out of scope (Kubernetes in production, the Proxmox machines as declarative code), for MIL-075 to MIL-078, planned at S01's request in chat on 2026-11-12 ("Skriv 1–4") | pending |

---

## Executive Summary

NF Hotel is a hotel system on Clean Architecture with PostgreSQL: a Blazor Server
back office, public guest pages, an internal database API and a mobile app for the
staff. It runs in containers on the Proxmox virtual machine (VM) `nf-hotel` (ADR-0003, ADR-0010) and is
public at `nfhotel.devmatiker.com` through Cloudflare Tunnel, with no port open in the
home network and the back office behind Cloudflare Access (ADR-0028, ADR-0029).

The project began as an unattended check, from throwaway Docker containers, that the
team's Proxmox VE server is reachable and healthy (objectives 1 to 4). It grew into the
delivery and operation of the hotel system. A change reaches production only as images
built once in continuous integration (CI), scanned and attested, and deployed by digest
by continuous deployment (CD) after green checks and S01's approval (ADR-0021, ADR-0038). Secrets are files, never in git or in an environment
(ADR-0024). The monitoring watches the site, the server, the backups and the scans and
alerts the team in Discord (ADR-0020). The database is backed up encrypted every day and
copied off the VM (ADR-0008, ADR-0025).

This revision brings the objectives and success criteria in line with the products as
they run on 2026-11-11, and adds criteria for the eleven products that had none
(objectives 30 to 40, success criteria 33 to 43).

## Methodological and Standards Foundation

The artifacts follow the SQA and QC framework mounted at `framework/` (Larman,
*Applying UML and Patterns*, for analysis and design artifacts) and the quality
characteristics of ISO/IEC 25010:2023. Code is reviewed against the `qc-programming-*`
checklists. Existing project analysis lives in `docs/refactor/` (converted to English by MIL-006,
informal notes, not framework artifacts) and is the source for context here.

## Problem Statement

- Before the project, no automated check proved that the team's Proxmox VE server was
  reachable and correctly credentialed; a failure was found by hand, late.
- A test that needs a person to type a password or accept a prompt cannot run in CI or
  overnight, and a test on a workstation depends on what is installed there and leaves
  state behind.
- The hotel system had no decided place to run, no repeatable way from a change to
  production, and no way to be reached from the internet without opening the home network.
- A system in production that is not watched, not backed up off its server and not
  scanned for vulnerabilities fails, loses data or is attacked without anyone knowing in time.
- Decision D-06 and the note on D-07 (`docs/refactor/Decisions.md`) record that
  credentials must never sit in the repository.

## Business Opportunity

A single command runs a throwaway container, checks the Proxmox VE API with a read-only
token, prints a pass or fail, and removes the container; it runs in Ops CI, where the live
tests that need a real Proxmox host skip. The same approach of throwaway containers and
unattended checks carries the whole delivery: the open questions Q-30, Q-31 and Q-42 are
answered by Testcontainers in CI, the Proxmox VM `nf-hotel` (ADR-0003, ADR-0010) and
GitHub Actions (ADR-0007). With a pipeline, a monitoring stack and a backup off the VM,
the team runs the hotel system in production on its own hardware.

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
6. Run the market analysis service (`NF-Hotel/Notebook_Market_Analysis`) as a container on the VM `nf-hotel`, reached only by the web application and the API over the internal network `nfhotel-python` and never through nginx (ADR-0039), so the owner sees its analysis in the back office: the holidays and the occupancy behind each price suggestion, and the AI analysis marked as written by an AI (US-001.01, US-001.03, US-001.05, MIL-071).
7. Run the benchmark service (`NF-Hotel/notebook-benchmark`) as a container on the VM `nf-hotel`, reached only over `nfhotel-python` (ADR-0039), so the owner can choose the local AI model by response time against answer quality (US-001.02, US-001.04).
8. Bring the NFHotel source (`src/`) to the clean architecture structure of the `clean-architecture` and `coding-conventions` skills: a common key base type for every entity, the folders of each layer, repository interfaces in the domain, key columns named `<table>_id`, and DTOs in one folder per entity, with no change of behaviour.
9. Convert the legacy project documents in `docs/refactor/` from Danish to English, the Product Owner language, with English file and folder names, without changing their meaning, identifiers or links.
10. Capture the business concepts of the hotel back office (bookings, rooms, guests) in a Domain Model that follows the framework rules (`QC-DM-001`), so that design and code can be checked against it.
11. Dissolve the folder `NFHotel.Domain/Rules` so that the domain has only the folders of the `clean-architecture` skill, the rules living with the entities, value objects and errors they guard, with no change of behaviour.
12. Express the field rules of the entities as attributes on their properties (ADR-0002), so that the entities hold no validation code, with no change of behaviour.
13. Convert the comments in the source code and the tests from Danish to English, the Product Owner language, without changing any code; the text of the application screens stays as it is.
14. Move the four test projects from xUnit v2 to xUnit v3, the supported line, without losing or changing a test and without changing the code under test.
15. Make the tests follow the test rules of the coding conventions: the use-case layer has unit tests, the conflict paths have integration tests, every test is named `Method_Condition_Expected`, and unit and integration tests are apart; the code in `src/` does not change.
16. Make the booking service refuse an overview period whose end is before its start and a check-in to a room that cannot be let; the code of the domain does not change.
17. Use asynchronous code where it gives meaning, layer by layer from the domain to the web, and keep it that way with architecture tests; behaviour does not change.
18. Make the database reachable for the applications only through an API that is not exposed to the internet, and for the team only over the tailnet with a personal role (objective 39, ADR-0034): the web application holds a token for the API and neither the database login nor the passport key (MIL-075).
19. Make the back office available only to signed-in staff, with a role per staff member that limits what each may see and do.
20. Serve the one service that the internet reaches only over TLS: HTTP redirects to HTTPS and the staff cookie is never sent in clear text.
21. Let a guest sign in with the booking number and the email of the booking, so that a guest reaches only their own booking and guest details and nobody else's, with no account and no password to keep.
22. Let an administrator start a benchmark run of local AI models from the back office and read the result of every run later, the result being sent by the benchmark service to the web API and stored in the web database.
23. Release only through an approved pipeline: a change reaches the server only after the required CI checks pass, as an image built once in CI and deployed by digest after an approval, over a private connection; the database has a backup whose restore is tested (MIL-021).
24. Deploy only scanned and attested images: every released image has a vulnerability scan, an SBOM and build provenance that the deploy verifies, and every dependency is pinned (MIL-022).
25. Harden and observe the server: selected CIS items for Docker and Debian are applied with evidence, the firewall and patching are managed as code, and the posture and delivery performance are measured (MIL-023).
26. Let hotel staff sign in and work with the rooms from the mobile app (Flutter) through the API, with the staff token alone and no shared secret in the app, so that a role sees and does in the app no more than in the back office (MIL-029, ADR-0015).
27. Let the owner and the Managers manage staff accounts and passwords in the back office: a staff member changes their own password with the current one, a Manager creates, disables, enables and resets accounts, a change or a disable ends the old sessions at once, and account events are kept in an audit trail (MIL-030).
28. Let guests book and the mobile app work from the public internet at `nfhotel.devmatiker.com`, without a port opened in the home network, and let staff reach the back office there only through Cloudflare Access (objective 34), so that the hotel is reachable by anyone while its home stays closed (MIL-040, ADR-0028, ADR-0029).
29. Let a Manager approve room price changes with one click: the market analysis service works out the suggestions with a fixed rule over its history, a surcharge where earlier years show an occupancy of 80 percent or more on the holidays and weeks of the next two months, and a discount where they show a low occupancy, and the back office presents them; no price changes, and no AI text sets a price, until a Manager approves; every approval, undo and change of a setting is kept in the audit trail (MIL-055, MIL-071).
30. Watch production and tell the team in time: metrics and logs of the VM and the applications in a monitoring stack on its own host, the Linux container (LXC) `nf-monitoring`, alerts in Discord, and service level objectives for availability, speed and errors (MIL-023, MIL-034, MIL-053, ADR-0020).
31. Test the running application as an attacker would: browser tests and an OWASP ZAP scan, signed out and signed in, on every pull request against a throwaway stack, and a full active scan every week, with a scan of the API from its OpenAPI document; and every endpoint of the API refuses hostile input and never answers it with a fault (MIL-027, MIL-044, MIL-067, MIL-077).
32. Keep the data safe off the VM: an encrypted backup of the database and of the settings every day and before every deploy, copied within its time to a write-only target on the backup host, kept there for a set time, the dump of the database read back every night while it is made, and restored in a drill (ADR-0008, ADR-0025, MIL-021, MIL-037, MIL-066, MIL-069, MIL-074, MIL-076).
33. Release without a staging environment, by decision: every release is tested on a throwaway stack in CI and, after S01's approval, checked on the VM by health checks, a secret check and a smoke test, and rolled back by itself when a check fails (ADR-0026, ADR-0038, MIL-039).
34. Show the back office on the public address only to listed staff: Cloudflare Access with a one-time code to the listed addresses in front of every spelling of `/admin`, the Access token checked at the tunnel and its header at nginx, and the hotel's own sign-in behind both; the routes of the tunnel and the Access application kept as code and compared with the live setting every week (ADR-0029, MIL-041, MIL-072, MIL-078).
35. Keep the secrets of production out of git, images, environments and logs, and rotate them in a drill: each secret a file that only its container reads, checked after every deploy, and never on a command line (ADR-0024, ADR-0041, MIL-035, MIL-070).
36. Measure the quality and security of the source on every pull request: CodeQL and SonarQube Cloud as required checks, with a coverage floor (MIL-047, MIL-054, MIL-058, MIL-059).
37. Keep the organisation on GitHub and its workflows from becoming a way into production: only allowed actions, pinned to a full commit hash (SHA); workflows that keep the rules of ADR-0037; a ruleset with the required checks, a review of every pull request and no bypass; and every deploy approved by S01 (ADR-0037, ADR-0038, MIL-059, the security audit SAR-001).
38. Keep dependencies current: Dependabot proposes the updates of NuGet packages, Docker images, compose pins and actions every week, and each update passes the same checks as any change (MIL-059, MIL-060, MIL-064).
39. Let the team work directly in the database: a personal role each, in a group that reads and writes the tables but cannot change the schema, over the tailnet only, and every change logged with the role's name (ADR-0034, MIL-048).
40. Handle incidents and know the capacity before guests notice: a daily scan of the images that run, alerts on failed sign-ins, a written incident process with severities and the 72-hour rule of the General Data Protection Regulation (GDPR), and a weekly load test against a throwaway stack (MIL-036, MIL-064).
41. Keep the configuration of the platform in git and reviewed: the compose files, the units and the scripts that set up the VM and the LXC in git; the routes of the tunnel and the Access application as code that a weekly check compares with the live setting; and the tailnet policy and the Proxmox definitions brought into git when a trigger of ADR-0027 comes (ADR-0027, MIL-038, MIL-078).

## Scope

### In Scope

- Credential files `pve.env` and `pve.env.example`, under `ops/` since MIL-031 (ADR-0016).
- A container image definition and a runner script.
- A .NET test project `NFHotel.Proxmox.Tests` with read-only Proxmox API checks.
- A Docker Compose file for the throwaway Proxmox VE container and a script that creates the per-run API token.
- The root `Dockerfile` of the web application and `Dockerfile.api` of the API (MIL-015), and one Docker Compose file per stack under `ops/deploy/`, each with its own `<service>.env` and a tracked `<service>.env.example`; nginx is a patched image that the release builds (ADR-0040, MIL-064).
- A smoke test that proves the web application answers through the proxy.
- The market analysis service and the benchmark service as containers on the VM `nf-hotel`: compose and env files, and the internal network `nfhotel-python`, which they share only with the web application and the API, with no route in nginx (MIL-004, MIL-061, ADR-0039).
- The restructuring of `NFHotel.Domain`, `NFHotel.Application` and `NFHotel.Infrastructure` and their tests (MIL-005); the Web project only changes where it must to compile.
- The translation of the notes, the presentation and the diagrams in `docs/refactor/` (MIL-006); source-code comments and the text of the application screens stay as they are.
- A Domain Dictionary, three brief use cases and the project Domain Model of the back office, compared with the code (MIL-007); the code is not changed by that milestone.
- The move of the rules of `NFHotel.Domain/Rules` into the entities, value objects and errors, with the tests and callers updated (MIL-008).
- Attributes on the properties of `Room` and `Guest` in place of their validation methods, with tests that fix codes, rules and order (MIL-009).
- The conversion of all comments in `src/` and `tests/` to English, with a check that only comments changed (MIL-010); the text of the application screens, string literals and test data stay as they are.
- The move of the four test projects from xUnit v2 to xUnit v3, with the same tests and the same test counts (MIL-011); the code in `src/` is not changed.
- Unit tests for the three services, integration tests for the conflict paths, the renaming of the test methods and the split of unit and integration tests, with a test plan (MIL-012); the code in `src/` is not changed.
- Two guards in the booking service, the matching error text and the check-in flag of the booking screens, with the use case updated (MIL-013)
- An audit of asynchronous code per layer in the order domain, application, infrastructure and web, the fixes it finds, and architecture tests for the rules (MIL-014)
- An internal API in front of PostgreSQL, a client that implements the use-case interfaces over it, the web application switched to the client, the deploy files, and ADR-0004 (MIL-015)
- Staff accounts, sign-in with a signed token, a permission table per role enforced in the Application layer and the API, the login and logout pages, authorization of the back office pages, the deploy files, and ADR-0005 (MIL-016)
- TLS termination in nginx, a development certificate script, HTTP to HTTPS redirect, a Secure staff cookie, the deploy files, and ADR-0006 (MIL-017)
- A guest sign-in by booking number and email, a short-lived guest token, an own-resource check in the API and the guest pages, the guest permissions of the permission table, the deploy files, and ADR-0007 (MIL-018)
- A results API with a table and an administrator page for benchmark runs, and a client and a start page for the benchmark service on the VM (MIL-019, MIL-020); the benchmark service itself is in the repository `notebook-benchmark`.
- The CI/CD and DevSecOps assessment DSA-001, ADR-0007 and ADR-0008; required checks and repository security settings; a release workflow to GHCR; an approved CD workflow over Tailscale with health checks and rollback; PostgreSQL backups and restore drills (MIL-021)
- Vulnerability scanning, SBOMs, build provenance and its verification, pinned actions and images, CodeQL, and migrations applied by the deploy (MIL-022)
- CIS-aligned hardening of Docker and Debian, nftables, unattended security updates, log rotation and an uptime check, OpenSSF Scorecard, an OWASP SAMM baseline and DORA metrics (MIL-023)
- Access for the mobile app: the staff token alone on the sign-in and the rooms routes of the API, a per-address limit on sign-in, a development-only route on the development server, and in the `MobileApp` repository an API client, a working login and the rooms overview and room view with the cleaning and service actions, tested against the development server in Docker (MIL-029, ADR-0015)
- A change-password function for every staff role, account management and an audit page for the Manager, a security stamp that ends old sessions, a password policy, the `reset-staff-password` recovery command, the deploy files and the replacement of the production Manager password, and ADR-0017 (MIL-030)
- A nightly rate per room size, price suggestions that the market analysis service works out from the holidays and the occupancy history of the next two months (MIL-071), a one-click approval and undo for a Manager, audit events and the page for them (MIL-055); a price on a booking that is already made is not changed.
- Public access through Cloudflare Tunnel, with a public listener of nginx and the two mobile routes (MIL-040, ADR-0028), and the back office behind Cloudflare Access (MIL-041, MIL-072, ADR-0029)
- Configuration as code: the decision on the tailnet policy and the Proxmox definitions, both deferred until a trigger (MIL-038, ADR-0027), and the routes of the tunnel and the Access application in OpenTofu, compared with the live setting every week (MIL-078)
- The monitoring stack on the LXC `nf-monitoring` (Prometheus, Alertmanager, Loki, Grafana, a blackbox exporter and a scan exporter) with Alloy on the VM, alerts in Discord, service level objectives and the alarm drill (MIL-023, MIL-034, MIL-053, ADR-0020)
- Browser tests and OWASP ZAP scans on every pull request, a weekly full scan with a scan of the API, and a test of every endpoint of the API with hostile input (MIL-027, MIL-044, MIL-067, MIL-077)
- The encrypted backup of the database and the settings, its copy to a write-only target on the backup host, the retention there, the nightly read-back of the dump, the restore drill and the recovery objectives (MIL-021, MIL-037, MIL-066, MIL-069, MIL-074, MIL-076, ADR-0008, ADR-0025)
- Secrets as files with a check after every deploy, the passport key in the API only, a rotation drill, and no secret on a command line or in the log of `sudo` (MIL-035, MIL-070, MIL-075, ADR-0024, ADR-0041)
- CodeQL and SonarQube Cloud as required checks, with a coverage floor (MIL-047, MIL-054, MIL-058, MIL-059)
- The hardening of the GitHub organisation and its workflows, and the security audit with its findings (MIL-059, ADR-0037, ADR-0038, SAR-001)
- Dependabot for NuGet packages, Docker images, compose pins and actions (MIL-060)
- Direct access to the database for the team, over the tailnet with personal roles (MIL-048, ADR-0034)
- The daily scan of the images that run with the remedies for a third-party image, alerts on failed sign-ins, the incident process and a weekly load test (MIL-036, MIL-064, MIL-065, ADR-0040)

### Out of Scope

- Tests against a real, permanent Proxmox host (decided by the DevOps track).
- Updating and administering the Proxmox VE host itself, which S03 does; the project creates
  and configures only the VM `nf-hotel` and the LXC `nf-monitoring` on it (ADR-0003, MIL-023).
- Changing the code of the two source repositories, except through their own issues and pull requests.
- Certification against ISO/IEC 27001, or any claim of compliance with a standard that is not backed by evidence (DSA-001).
- A staging environment: a release is tested on a throwaway stack in CI and on the VM after the deploy (ADR-0026, MIL-039).
- Tests that need a graphical interface on the host desktop; browser tests run in Docker only (MIL-027, `framework/process/gui-testing.md`).
- The mobile app on the internet: a release build and its store publication need their own decision; the public address and TLS of the API for it are decided in ADR-0028 (MIL-040); the mobile bookings and guests screens are not part of MIL-029.
- Kubernetes or another orchestrator in production: one hotel runs on one VM with Docker Compose, with health checks, restart policies and limits on processor and memory per service, and an orchestrator would add a control plane to run, patch and secure.
- The Proxmox VM and LXC, and the tailnet policy, as declarative code: scripts with cloud-init make the machines, and S01 deferred the recorded definitions and the policy in git until a trigger (ADR-0027, MIL-038); only the public edge is planned as code (MIL-078).

## Expected Benefits

### Tangible Benefits

- A Proxmox connectivity problem is found in under one minute by an unattended
  run instead of by hand.
- No state is left on the workstation after a run: the container is removed.
- A change reaches production only as scanned and attested images, after green checks
  and an approval, and a failed deploy rolls back by itself.
- A site that is down, an old backup, a slow or failing application and a vulnerable
  image reach the team in Discord within minutes to a day, not when a guest complains.
- Guests book from the internet with no port open in the home network, and strangers
  never see the staff sign-in.
- The data survives the loss of the VM: an encrypted copy lies off the VM, and a restore
  has been proven.

### Intangible Benefits

- Confidence in production: every claim about it has evidence in a review record.
- A pattern for further infrastructure tests that never touch the host desktop.

## Strategic Alignment

Supports the DevOps and cybersecurity tracks of NFHotel 2.0, whose open questions Q-30,
Q-31 and Q-42 are answered (see Business Opportunity), and follows the CI/CD and
DevSecOps assessment DSA-001 and the rule that GUI and end-to-end tests never run on
the host desktop (`framework/process/gui-testing.md`).

## Success Criteria

| # | Criterion | Target | Measure |
| --- | --- | --- | --- |
| 1 | Unattended run | 0 prompts or manual inputs during a run | The runner finishes with exit code 0 or 1 without stdin |
| 2 | Throwaway container | 0 containers left after a run | `docker ps -a` shows none from the runner |
| 3 | Read-only | 0 requests with a method other than GET | A test fails if the client sends any other method |
| 4 | Secrets safe | 0 secrets in git | `pve.env` is ignored; `git ls-files` lists no `pve.env` |
| 5 | Speed | Run completes in 120 seconds or less once the image is built | Wall-clock time of the runner |
| 6 | Web through proxy | HTTP 200 for `/` and an answer from `/_blazor/negotiate` through the proxy, with no host port published by the web service | Smoke-test script exit code 0 |
| 7 | One env file per service | 0 tracked `.env` files; a tracked `.env.example` for each settings file that a stack reads (14 compose files and 14 `.env.example` files under `ops/` on 2026-11-11); the 4 tracked `.env.dev` files hold development values only | `git ls-files` and a review of the `.env.dev` files |
| 8 | No token on disk | 0 files containing the API token after a run | A search of the repository and the temporary directory finds no token |
| 9 | Python services off the proxy | The market analysis and the benchmark service answer the web application over `nfhotel-python`, and nothing on the network of nginx reaches them; only nginx (80, 443) and the relay of the team (5432) publish a host port | `test-service-networks.sh` in Ops CI |
| 10 | Benchmark results | At least 2 local models measured for response time and answer quality | Result file of one benchmark run |
| 11 | Clean architecture | 0 architecture-test failures; every entity derives from `Entity<TKey>`; every key column is `<table>_id`; 0 build warnings; the test suite passes as before | `dotnet build` and `dotnet test` |
| 12 | Legacy documents in English | 0 Danish letters or words in `docs/refactor/`; every identifier unchanged | Language scan and identifier count in the review record |
| 13 | Domain Model | Every Mandatory criterion of `QC-DM-001` passes; 0 concepts without a source use case or dictionary term | Review record of the Domain Model |
| 14 | Domain folders | 0 folders in `NFHotel.Domain` other than `Entities`, `ValueObjects`, `Repositories` and `Errors`; error code strings unchanged | `dotnet test` and a folder listing |
| 15 | Attribute validation | 0 methods named `Validate` or `IsValid…` in `Room` and `Guest`; error codes, rules and their order unchanged | `dotnet test` and a source search |
| 16 | Source comments in English | 0 Danish letters or words in the comments of `src/` and `tests/`; 0 changed lines that are not comments | Language scan and comment-only diff check in the review record |
| 17 | Tests on xUnit v3 | 0 references to `xunit` 2.x in the project files; the same test count per project as before; 0 changed lines in `src/` | `dotnet test`, a search of the project files and `git diff` of `src/` |
| 18 | Tests follow the test rules | 0 test names with fewer than three parts; at least one positive and one negative test for each public method of the three services; 0 sleeps, system-clock reads or network calls in `tests/`; 0 changed lines in `src/` | An architecture test, a source search and `git diff` of `src/` |
| 19 | Booking guards | The overview refuses an end before the start and allows one day; check-in is refused for a room out of service or in maintenance and allowed for a room that needs cleaning; 0 changed lines in `NFHotel.Domain` | `BookingServiceTests`, a mapping test and `git diff` of the domain |
| 20 | Asynchronous code | 0 blocking calls (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `Task.Run`, `async void`) in `src/`; every method that returns `Task` ends in `Async`; 0 `async` in entities and value objects; every public asynchronous method of the application and infrastructure takes a `CancellationToken` | `AsyncRulesTests` and a source search per layer in the review record |
| 21 | Database API | 0 references from the web application to Infrastructure, EF Core or Npgsql; 0 database settings and no passport key in the web container; the API and PostgreSQL publish no host port, and the relay of ADR-0034 publishes 5432 on the tailnet address only (success criterion 42); a call without the token gets 401; every method of the three service interfaces gives the same result through the API as directly | An architecture test, `docker inspect` in the VM, the contract test and the deploy check |
| 22 | Back office login | 0 back office pages or staff endpoints answer without a valid sign-in (redirect to the login, or 401); for every role and operation a test states allowed or refused and equals the Access Control Matrix; Housekeeping gets 403 on bookings, guests and sales; a tampered or expired token is refused; the sixth failed sign-in in the window gets 429 | Permission table tests, API and page tests, and the deploy check in the VM |
| 23 | TLS at the public port | Port 80 answers only 301 to HTTPS; HTTPS answers with TLS 1.2 or 1.3 and refuses 1.1 and 1.0; the staff cookie has Secure and HttpOnly; 0 private keys in git; only ports 80 and 443 of nginx answer from outside | The smoke test, openssl s_client and the deploy check in the VM |
| 24 | Guest sign-in | A call for a booking without a valid staff or guest token gets 401; a guest token for booking A gets 403 for booking B and for every other guest; a wrong email, an unknown number and an expired token give the same refusal; the sixth failed attempt for a number in the window gets 429; a guest never receives a passport number | Permission and own-resource tests, API and page tests, and the deploy check in the VM |
| 25 | Benchmark results | A run result posted twice with the same run id is stored once; a call to the results API without the service token gets 401; a Manager lists the runs and reads one with every model and its measurements; a Receptionist and a Housekeeping user get 403; a start request reaches the benchmark service only from a Manager | API, permission and page tests, and the deploy check in the VM |
| 26 | Approved pipeline | A pull request with a failing required check cannot be merged; every deploy runs the image digests of a green release after an approval in `production`; 0 images built on the VM; a failed smoke test rolls back to the previous digests; 0 ssh keys or Tailscale secrets stored in GitHub; a restore of the daily backup is recorded | The ruleset API, the CD run record, `docker inspect` in the VM and the restore drill RC |
| 27 | Verified supply chain | Every image of a release has an SBOM and a provenance attestation that `gh attestation verify` accepts before CD deploys it; 0 critical and 0 fixable high findings in the images that run, release and third-party images alike, except findings accepted under ADR-0040 with a statement and an expiry at most 30 days ahead; 0 unpinned actions or image tags | The CD log, the daily scan of the running release (its issue closed) and the Scorecard Pinned-Dependencies check |
| 28 | Hardened server | Every selected CIS item has a status and evidence; only ports 80 and 443 answer from outside the tailnet; security updates install unattended; a Scorecard result, a SAMM baseline and a monthly DORA report exist | The CIS checklist, an nmap scan, the update log and the reports |
| 29 | Mobile app access | The sign-in and `GET /api/rooms` answer to a valid staff token with no service token and give 401 without either; every other route group gives 401 to a staff token alone; Housekeeping gets 403 on room create, update and delete; the sixth failed sign-in in the window gives 429; 0 tokens or keys in the `MobileApp` repository; the app signs in as each development account and lists the seeded rooms on the development server | API and permission tests, a source search, and the end-to-end check in Docker |
| 30 | Staff accounts and passwords | A password change with a wrong current password, a short, common or reused new password is refused, and the sixth wrong current password in the window gets 429; a token issued before a change, reset or disable gets 401; a Receptionist and a Housekeeping user get 403 on account management; the last active Manager cannot be disabled; every account event writes one audit row with no password, hash or token in it; in the VM the initial Manager password is refused after the owner changed it | Unit, API and page tests, the audit page and the deploy check in the VM |
| 31 | Public access | From outside the tailnet, the site and `/book` answer 200 with a valid certificate; the back office answers as success criterion 37 says; every API route but the two mobile routes answers 404; 0 ports opened in the home router; the tunnel token in no environment | A check from outside the tailnet, `check-secret-env.sh` and the router's port list |
| 32 | Price suggestions | The suggestions come from the market analysis service, and no price on the page comes from an AI text; a suggestion of 20 percent on a base rate of 100 shows 120, a percentage above the cap is cut to the cap and a price below the minimum price is raised to it; each suggestion shows the holiday, the reason and the occupancy of each earlier year; one approval creates one price change with the dates shown and a second approval of it creates none; an undo ends it the same day; a Receptionist and a Housekeeping user get 403 on the approval; with the service stopped the page offers no approval; every approval, undo, rate and setting change writes one audit row | Unit, API, architecture and page tests and the audit page |
| 33 | Monitoring and service levels | The rules and their tests pass in Ops CI (`promtool test rules`); over 30 days the site is available 99.5 percent of the time, 95 percent of requests take under 1 second and under 5 percent answer 5xx; a stopped container alerts in Discord within 3 minutes; the alarm drill reaches Discord and Healthchecks.io | Ops CI, the service level panels of Grafana and the review record of the alarm drill |
| 34 | Dynamic application security testing | 0 High alerts that are not accepted; every ignored or warned ZAP rule has a reason and an expiry at most 30 days ahead; the baseline scan runs on every pull request and the full scan every week, with a scan of the API from its OpenAPI document, and a failed full scan opens an issue; every endpoint of the API answers hostile input in the 400 range, never 500, and the document is not served in Production | The runs of the end-to-end and DAST workflow, `dast-scan.sh --check-rules`, the issue list and the test of hostile input of the API |
| 35 | Backups off the VM within their time | A backup at least every 26 hours (`BackupStale` silent); the newest copy off the VM less than 72 hours old whenever the backup host has been on in that time (`OffVmCopyStale` silent), and a warning within 3 hours when the host is on but its target does not take the copy; the key of the VM cannot delete or replace a file on the target; daily files kept there 30 days and weekly files 12 weeks; every nightly dump read back to its end with its tables counted (`BackupUnreadable` and `BackupShrunk` silent); a restore with matching row counts recorded | The alerts in Prometheus, `test-backup-target.sh` in Ops CI, the log of the prune and the review record of the restore drill |
| 36 | Release safety without staging | Every deploy runs the health checks, the secret check and the smoke test, and a failed check rolls back to the previous digests by itself; the rollback drill of CD passes; an application error, slowness or exceptions after a deploy alert within 10 minutes | The CD run records, a run of the rollback drill and the rule tests |
| 37 | Back office behind Access | Without an Access session, `/admin` in any spelling on the public name gives the Access sign-in or 404, never the hotel's sign-in; the route of the tunnel that validates the token is first in the list, in Cloudflare and in its code (success criterion 44); an address that is not listed gets no code; a session lasts at most 1 week | A check from outside the tailnet, `test-nginx.sh` in Ops CI, the log of `cloudflared` and the weekly comparison of the edge with its code |
| 38 | Secrets in production | 0 secrets in the environment of a container after every deploy, and a finding rolls the deploy back; the rotation drill changes the database password, the API token, the signing key and the first Manager's password, and the old values are refused; 0 secret values in the log of `sudo` after a deploy | The CD run records, `test-secret-rotation.sh` and `test-sudo-log.sh` in the required checks |
| 39 | Code quality gates | A pull request with a failing CodeQL or SonarQube Cloud check cannot be merged; the quality gate asks for 80 percent coverage on new code; the coverage is 95 percent or more in total and 90 percent or more per project; every CodeQL alert is fixed or dismissed with a reason | The ruleset API, `check-coverage-floor.sh` in CI and the code scanning alerts |
| 40 | Organisation and workflow security | `test-workflow-policy.sh` passes in the required checks; 0 actions that are not allowed or not pinned by SHA; the ruleset requires every check of CI and an approval by a member other than the author, with 0 bypass actors; every CD run waits for S01's approval; every finding of the security audit is fixed, or accepted by S01 with a reason | The ruleset and organisation settings in the API, Ops CI and the security audit |
| 41 | Dependency updates | Every folder with a project file, a Dockerfile, a compose file or a workflow is in `.github/dependabot.yml`; a Dependabot pull request merges only through the required checks | `.github/dependabot.yml` against `git ls-files`, and the ruleset API |
| 42 | Team access to the database | Port 5432 answers only on the tailnet address of the VM, to the team's devices; each member has a personal role in a group that reads and writes the tables and cannot create or truncate; 1 administrator role; every change is logged with the role's name and kept 14 days | `test-team-db-users.sh` in the required checks, a scan from the local network, a query of the roles in the VM and a change found in Loki |
| 43 | Incident response and capacity | The daily scan of the images that run opens an issue on a finding that blocks a release and closes it when fixed; more than 20 failed staff sign-ins or more than 10 failed Secure Shell (SSH) logins in 10 minutes alert; the wiki page Incidents has the severities SEV1 to SEV3, the 72-hour rule of the GDPR and a postmortem template; the weekly load test with 20 virtual users for 2 minutes keeps the 95th percentile under 1,000 ms and the failed requests under 5 percent | The issue list, the rule tests, the wiki page Incidents and the runs of the load test |
| 44 | Configuration as code | The routes of the tunnel and the Access application with its policy are in git; the weekly plan against Cloudflare shows 0 changes, or an open issue names the difference and is closed when it is fixed; 0 Cloudflare tokens that can write in GitHub; a change of the edge reaches Cloudflare only from a reviewed pull request | The runs of the comparison workflow, the issue list and the secrets of the repository |

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
| GPU is not available inside the Proxmox VM | The benchmark times are not those of the real hardware, or the models cannot run | The result names the machine; GPU passthrough is decided in an ADR before the benchmark service is planned |
| The analysis application has no charts and no container image yet | The owner sees tables instead of diagrams | A gap task in MIL-004; the image is built from the repository at a pinned ref |
| A restructuring of many files breaks behaviour | A regression in bookings, guests or rooms | One layer at a time, build and tests after each step, no behaviour change, the PostgreSQL tests run in a container |
| A translation changes a rule or an identifier | A wrong business rule is read later | Identifiers are counted before and after; code blocks and tables are not edited; one reviewer record against the language checklist |
| The staff token is stolen from a phone | Someone reads or changes rooms until the token expires | The token lives in the platform secure storage, expires after 8 hours and carries only the role of the member; it opens the rooms routes and nothing else, and the sign-in is limited per user name and per address |
| The backup host is off, or its target has stopped, for days | The copy off the VM grows old, and a loss of the VM loses more data | An hourly copy, `BackupTargetDown` within hours and `OffVmCopyStale` at 72 hours (MIL-074); the backups wait on the VM |
| A third-party image has a fixable finding, and upstream has no fixed build | A known vulnerability runs in production | The daily scan opens an issue, and ADR-0040 gives the remedy: the upstream fix, a patch layer from the release, or an ignore entry of at most 30 days with a statement |
| A defect reaches production, because there is no staging | Guests or staff meet an error | Tests on a throwaway stack in CI, a smoke test and an automatic rollback after the deploy, and alerts within 10 minutes (ADR-0026) |
| An account of the GitHub organisation, where every member is an administrator, is taken over | A change, a workflow or a secret reaches production without the team | Required checks and a required review with no bypass, pinned actions, and S01's approval of every deploy (ADR-0037, ADR-0038); two-factor authentication for every member, still open as SEC-01 of the security audit |
| A secret leaks into a log, an image or a chat | Someone reaches production or the data | Secrets as files, checked after every deploy, the rotation drill, and no secret on a command line (ADR-0024, ADR-0041) |
| The public site is attacked | The site is slow or down, or someone tries the staff sign-in | Cloudflare in front, the back office behind Access, rate limits at Cloudflare and in the application, ZAP scans, and alerts on failed sign-ins |

## Assumptions

- The Proxmox VE server is reachable by network from the developer's machine
  and from the container.
- Docker can run privileged containers with `/dev/kvm` (on WSL2 a distribution with the KVM modules must have been started).
- A Proxmox VE host runs the VM `nf-hotel` and the LXC `nf-monitoring`; it is not the throwaway test container.
- The backup host is on at least once in 72 hours; while it is off longer, the copy off the VM waits (objective 32).
- Cloudflare Tunnel and Cloudflare Access stay free for a team of up to 50 users (ADR-0029).
- `pve.env` holds a throwaway root password for the test container only.
- S01 (Team 2, the owner of NF Hotel, all developers) is the Proxmox administrator for now; S04 (Valdamar) stands in for the Product Owner and reviews.

## Constraints

- Docker is the only required tool on the host.
- The test client is read-only; it runs against a throwaway Proxmox VE container.
- Target dates per milestone, in the Project Plan and in each milestone document.
- Existing solution conventions apply (.NET 10 SDK per `global.json`).
- No port may be opened in the home network (ADR-0028).
- Every deploy waits for S01's approval (ADR-0038).

## Cost–Benefit Assessment

Qualitative, because the project has no budget of its own: the costs are the team's time and the upkeep of what runs.

| Costs | Benefits |
| --- | --- |
| About one working day of engineering | Early, repeatable detection of Proxmox problems |
| Small container image to maintain | Decision input for Q-31 and Q-42 |
| One more test project in the solution | Reusable unattended infrastructure-test pattern |
| About half a day for the compose files, image and smoke test | A reproducible way to run NFHotel behind a proxy |
| About one week for the analysis service on a VM behind the proxy | The owner can see the market analysis |
| About two weeks for the restructuring and its tests | A source that follows one structure, easier to extend and to review |
| About one week for the translation | One language across all project documents |
| The pipeline, the scans and the monitoring to keep up: Dependabot pull requests, ignore entries to renew, alerts to answer | A change reaches production only green and approved, and a failure or a finding is seen within minutes to a day |
| The backup host must be on, with its target running | The data survives the loss of the VM |
| A daily scan, a weekly ZAP scan and a weekly load test use runner time | Vulnerabilities and capacity limits are found before guests meet them |

## Stakeholders

| Stakeholder ID (SA) | Interest in this project |
| --- | --- |
| S01 | Owns NF Hotel and develops it: needs unattended, repeatable checks, and a pipeline, monitoring and backups that run without manual steps |
| S02 | Needs the Proxmox server proven before relying on it |
| S03 | Needs assurance that tests never change the server, that the VM and the LXC change only through reviewed code and approved deploys, and that only their key opens the backups |
| S04 | Needs a plan and reviews he can accept or reject |
| S05 | Needs the back office and the mobile app to work from anywhere, with their own account, and the guests' data kept safe |

## Recommendation

Proceed. The platform runs and meets most of its criteria. Continue with what is open on 2026-11-11: the findings in the deployed images (success criterion 27: the release of the .NET 10.0.15 and nginx images, and MIL-073 for the database image), the copy off the VM within its time (success criterion 35, MIL-074), the deploy and gate of MIL-072 (success criterion 37), two-factor authentication for the organisation and the required review (success criterion 40), and the passport key out of the web container (success criterion 21, MIL-075). Then build what S01's check of 2026-11-12 found missing: every nightly backup read back (success criterion 35, MIL-076), hostile input against the API (success criterion 34, MIL-077), and the routes of the tunnel and Access as code (success criteria 37 and 44, MIL-078). Kubernetes in production and the Proxmox machines as declarative code stay out of scope.

---

[SA-001]: ./stakeholder-analysis.md
[UCD-001]: ./use-case-diagram.md
[US-001]: ./user-stories.md
[f674e8a]: https://github.com/NF-Hotel/NFHotel/commit/f674e8a3fe47dfe3c36bbc59f4eeae6180985b0d
