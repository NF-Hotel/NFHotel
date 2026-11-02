# MIL-057 Start NF Hotel from Visual Studio with F5

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-057 |
| CrossReference | [BC-001], [ADR-0024], [ADR-0035], [MIL-056] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | mikemike | S01 | Initial version; Visual Studio 2026 builds NFHotel and every test passes, but F5 on `NFHotel.Web` stops with `'Api:BaseUrl' must be an absolute http or https address of the API`; S01 asked in chat on 2026-11-02 to plan an F5 setup ("Yes, plan F5"); S01 accepted the plan in chat ("Yes, accept and build") after RC-274 Go | pending |

---

## Purpose

Decide whether a developer can open `NFHotel.sln` in Visual Studio (or Rider, or `dotnet run`), press F5 and get the back office and the API running with the debugger attached, without Docker images of the application and without writing settings by hand.

Today the only ways to run NF Hotel locally are the two Docker modes of `scripts/dev-up.sh` (Proxmox, and the light mode of MIL-056). Both run the application as containers, so breakpoints in Visual Studio are never hit. F5 fails at once:

- `NFHotel.Web` needs `Api:BaseUrl` and `Api:Token` and finds neither.
- `NFHotel.Api` needs a PostgreSQL database, `Api:Token`, `Staff:Token:SigningKey`, `Encryption:PassportKey` and the first Manager, and nothing provides them.
- The solution has no launch profile that starts the API and the web application together.

The plan: one setup script makes a local PostgreSQL container and the development secrets once, as .NET user secrets (outside the repository, ADR-0024), and a solution launch profile starts the API and the web application together. Visual Studio then runs the code with the debugger, against the same settings names as production.

## Deliverable

- **An ADR** that chooses how F5 gets its dependencies: (a) a setup script, user secrets and a solution launch profile (`NFHotel.slnLaunch`), recommended because it adds no runtime packages and keeps the settings names of production; (b) a .NET Aspire AppHost project, which wires the same things in code but adds a project and packages to the solution and to the locked restore. The ADR records the choice and why.
- **`scripts/dev-f5.sh` and `scripts/dev-f5.ps1`**: start a PostgreSQL 16 container for F5 with its port on `127.0.0.1` only, with a volume that survives a restart; and write, once, the user secrets of the API (the connection string, the API token, the signing key, the passport key, the first Manager, migrations at start) and of the web application (`Api:BaseUrl`, the same API token, the passport key). Existing secrets are kept. `--down` stops the database, `--destroy` removes it and the secrets, `--check` says what is missing. It prints the Manager account at the end, as the other development modes do.
- **`NFHotel.slnLaunch`** with a profile "API and web" that starts `NFHotel.Api` and `NFHotel.Web` together, and the launch profiles of the two projects on fixed local ports, so the web application finds the API.
- **The mobile app against F5**: the API's own address on the machine works for the mobile app's debug build through `--dart-define=API_BASE_URL`; the docs say which.
- **Docs**: a section "Run from Visual Studio" on the wiki page Local Development Deploy, a line in `README.md`, and the text for fellow students in the wiki: what F5 needs (Docker for PostgreSQL only) and how to fix a Visual Studio that cannot resolve the .NET SDK.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | After `scripts/dev-f5.ps1` (Windows) or `scripts/dev-f5.sh` (Linux, macOS), F5 on the profile "API and web" in Visual Studio 2026 starts both projects, and `/admin/login` signs in as the printed Manager | Yes | No |
| 2 | A CI job runs the setup script, starts the API and the web application with `dotnet run` and the launch profiles, and checks the sign-in page, the API's health endpoint and a sign-in through the API | Yes | No |
| 3 | No secret is written in a file of the repository or in a launch profile; secrets are user secrets on the developer's machine, and the database port listens on `127.0.0.1` only | Yes | No |
| 4 | Running the setup script twice changes nothing the first run made; `--destroy` removes the container, the volume and the user secrets it wrote, and nothing else | Yes | No |
| 5 | The release build, the Docker images and the two `dev-up.sh` modes work as before: their CI jobs pass unchanged | Yes | No |
| 6 | The code review against `QC-SH-001`, `QC-CS-001` (if C# changes) and `QC-WF-001` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-056 | The light mode is the Docker way to run the whole stack; F5 is the way to debug the code, and the docs place the two side by side |
| ADR-0024 | Secrets never in the repository or in an environment a container exposes; on a developer machine they are user secrets |
| ADR-0035 | The light mode, which the ADR of this milestone compares with |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001: every developer of the guilds (S02) can build, run and debug a change before it is deployed | Tasks 1 to 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-20. It changes only developer tooling and documentation, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for running from Visual Studio | Choose between a setup script with user secrets and a solution launch profile, and a .NET Aspire AppHost; record why. | No | criteria 3, 5 |
| 2 | Setup scripts for F5 | `scripts/dev-f5.sh` and `scripts/dev-f5.ps1`: the PostgreSQL container on `127.0.0.1`, the user secrets of the API and the web application made once, `--check`, `--down`, `--destroy`. | No | criteria 1, 3, 4 |
| 3 | Launch profiles | `NFHotel.slnLaunch` with "API and web", and fixed local ports in the launch profiles of `NFHotel.Api` and `NFHotel.Web`, so the web application finds the API. | No | criterion 1 |
| 4 | Test in CI | A job that runs the setup script, starts both projects with `dotnet run` and their launch profiles, and checks the sign-in page, the API's health and a sign-in through the API. | No | criteria 2, 5 |
| 5 | Docs | "Run from Visual Studio" on the wiki page Local Development Deploy, a line in `README.md`, the address for the mobile app, and the fix for a Visual Studio that cannot resolve the .NET SDK. | No | criterion 1 |
| 6 | F5 on Windows and code review | Press F5 in Visual Studio 2026 on S01's machine and sign in; the review record against `QC-SH-001`, `QC-CS-001` and `QC-WF-001` holds the result. | No | criteria 1, 6 |

---

[BC-001]: ../business-case.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[ADR-0035]: ../adr/adr-0035-light-local-development-stack.md
[MIL-056]: mil-056-dev-stack-without-kvm.md
