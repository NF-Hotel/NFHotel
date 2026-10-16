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
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Amended: objective 12, validation attributes on the entities (MIL-009) | [4cab5fc] |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Amended: objective 13, the comments in the source code are in English (MIL-010) | [e67e815] |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Amended: objective 14, the tests run on xUnit v3 (MIL-011) | [ef2bd4b] |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Amended: objective 15, the tests cover the kinds of behaviour of the coding conventions (MIL-012) | [8200afa] |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Amended: objective 16, the booking service refuses an inverted overview period and a check-in to a room that cannot be let (MIL-013) | [5036584] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Amended: objective 17, asynchronous code where it gives meaning, enforced by architecture tests (MIL-014) | [417a575] |

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
checklists. Existing project analysis lives in `docs/refactor/` (converted to English by MIL-006,
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
6. Run the market analysis service (`NF-Hotel/Notebook_Market_Analysis`) on a Proxmox VM through Docker Compose behind the nginx proxy, so the owner can see the market analysis with diagrams and an AI analysis (US-001.01, US-001.03, US-001.05).
7. Run the benchmark service (`NF-Hotel/notebook-benchmark`) on a Proxmox VM as a Docker container behind the proxy, so the owner can choose the local AI model by response time against answer quality (US-001.02, US-001.04).
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

## Scope

### In Scope

- Credential files `pve.env` and `pve.env.example`.
- A container image definition and a runner script.
- A .NET test project `NFHotel.Proxmox.Tests` with read-only Proxmox API checks.
- A Docker Compose file for the throwaway Proxmox VE container and a script that creates the per-run API token.
- Reuse of the existing root `Dockerfile` for the web application and one Docker Compose file per service (nginx proxy, NFHotel web), each with its own `<service>.env` and a tracked `<service>.env.example`.
- A smoke test that proves the web application answers through the proxy.
- The market analysis service and the benchmark service on Proxmox VMs behind the proxy: compose and env files, an nginx route per service and a smoke test (planned in MIL-004 and a later milestone).
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

### Out of Scope

- Tests against a real, permanent Proxmox host (decided by the DevOps track).
- Creating, starting, changing or deleting virtual machines or containers on
  Proxmox.
- Deploying the NFHotel web application itself to Proxmox (decision Q-31 stays with DevOps).
- Changing the code of the two source repositories, except through their own issues and pull requests.
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
| 9 | Analysis through the proxy | HTTP 200 for the market analysis through nginx with no host port published by the service | Smoke-test script exit code 0 |
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

## Assumptions

- The Proxmox VE server is reachable by network from the developer's machine
  and from the container.
- Docker can run privileged containers with `/dev/kvm` (on WSL2 a distribution with the KVM modules must have been started).
- A Proxmox VE host exists for the two services; it is not the throwaway test container.
- `pve.env` holds a throwaway root password for the test container only.
- S01 (Team 2, the owner of NF Hotel, all developers) is the Proxmox administrator for now; S04 (Valdamar) stands in for the Product Owner and reviews.

## Constraints

- Docker is the only required tool on the host.
- The test client is read-only; it runs against a throwaway Proxmox VE container.
- Target dates 2026-10-17 (MIL-001), 2026-10-18 (MIL-002), 2026-10-19 (MIL-003), 2026-10-23 (MIL-004) and 2026-10-30 (MIL-005) and 2026-10-26 (MIL-006); one working session each.
- Existing solution conventions apply (.NET 9 SDK per `global.json`).

## Cost–Benefit Assessment

Qualitative, because no money changes hands and the effort is one working session.

| Costs | Benefits |
| --- | --- |
| About one working day of engineering | Early, repeatable detection of Proxmox problems |
| Small container image to maintain | Decision input for Q-31 and Q-42 |
| One more test project in the solution | Reusable unattended infrastructure-test pattern |
| About half a day for the compose files, image and smoke test | A reproducible way to run NFHotel behind a proxy |
| About one week for the analysis service on a VM behind the proxy | The owner can see the market analysis |
| About two weeks for the restructuring and its tests | A source that follows one structure, easier to extend and to review |
| About one week for the translation | One language across all project documents |

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
[88a4946]: https://github.com/NF-Hotel/NFHotel/commit/88a49468aa21b75a0a5f9c8fd5a214164e1676ca
[7c6d8f8]: https://github.com/NF-Hotel/NFHotel/commit/7c6d8f85d2177b9fe9f2d11d175df7f0b8f1b5d5
[UCD-001]: ./use-case-diagram.md
[US-001]: ./user-stories.md
[469ff7a]: https://github.com/NF-Hotel/NFHotel/commit/469ff7a7871bb900155dcaf0639b5db1371681aa
[469c395]: https://github.com/NF-Hotel/NFHotel/commit/469c3959b999487820f8ffa1c24aa4866e61cfe7
[78eeb18]: https://github.com/NF-Hotel/NFHotel/commit/78eeb18cac8d761ebf5267fca5d6b418b04b98c2
[180a268]: https://github.com/NF-Hotel/NFHotel/commit/180a2683d3138fe0fce82cbbe34d817511e0097a
[e19063e]: https://github.com/NF-Hotel/NFHotel/commit/e19063e96ba5d221fffa80ad028432134ecdb111
[48701b5]: https://github.com/NF-Hotel/NFHotel/commit/48701b550cecaf94616bbae64d74a5c872f72d80
[4cab5fc]: https://github.com/NF-Hotel/NFHotel/commit/4cab5fc64b8358e18f9fec630d27d8bc267e6787
[e67e815]: https://github.com/NF-Hotel/NFHotel/commit/e67e8155fd63d307e6783924f956fa49f915edbd
[ef2bd4b]: https://github.com/NF-Hotel/NFHotel/commit/ef2bd4bda1edbd598228cb0fc043a4f2fc996974
[8200afa]: https://github.com/NF-Hotel/NFHotel/commit/8200afa457e37ca87a68ab7e34126db1dd85e3d1
[5036584]: https://github.com/NF-Hotel/NFHotel/commit/5036584e9809a90d8459f07554733917c7129a0e
[417a575]: https://github.com/NF-Hotel/NFHotel/commit/417a575b978d56c59da23eb6cff5967a7b9bf6bf
