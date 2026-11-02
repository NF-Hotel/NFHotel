# ADR-0036 Start NF Hotel from Visual Studio with a setup script and user secrets

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0036 |
| CrossReference | [MIL-057], [ADR-0024], [ADR-0035] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; written under MIL-057, which S01 accepted in chat on 2026-11-02 | [7459205] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

Visual Studio 2026 builds `NFHotel.sln` and every test passes, but F5 fails at once. `NFHotel.Web` stops with `'Api:BaseUrl' must be an absolute http or https address of the API`. `NFHotel.Api` also needs settings that nothing provides on a developer's machine:

- a PostgreSQL database;
- `Api:Token`, `Staff:Token:SigningKey` and `Encryption:PassportKey`;
- the first Manager.

The solution has no profile that starts the two projects together.

The two local modes of `scripts/dev-up.sh` (the Proxmox mode of ADR-0003 and the light mode of ADR-0035) run the application as containers, so a breakpoint in Visual Studio is never hit. The guild developers (S02) need to debug a change before it is deployed.

Secrets are never in the repository or in an environment a container exposes (ADR-0024).

**Options considered:**

1. **A setup script, .NET user secrets and a solution launch profile.**
   - A script starts PostgreSQL in Docker and writes the settings once as user secrets, outside the repository.
   - `NFHotel.slnLaunch` starts the API and the web application together.
   - The projects read the same settings names as in production.
2. **A .NET Aspire AppHost project.** It wires PostgreSQL, the API and the web application in C#. It adds a project, the Aspire packages and their locked restore to the solution. It changes how the projects get their settings during development (service discovery and injected connection strings), and the Aspire workload must be kept current on every machine.
3. **Hand-written `appsettings.Development.json` files.** They need no script, but each developer writes the secrets into a file in the working tree, where they are one `git add` from the repository.

## Decision

We follow option 1.

1. **`scripts/dev-f5.sh`** (from PowerShell: `scripts/dev-f5.ps1` or `scripts/dev-up.ps1 --f5`) prepares a machine once.
   - It starts a PostgreSQL 16 container, from the image pinned in `ops/deploy/postgres`, on `127.0.0.1:55432` only, with a named volume.
   - The database password is a file (`POSTGRES_PASSWORD_FILE`), not in the environment of the container.
2. **The settings are .NET user secrets** of `NFHotel.Api` (`UserSecretsId` in its project file) and `NFHotel.Web`.
   - The script passes them to `dotnet user-secrets set` as JSON on standard input, never on a command line.
   - It writes only the keys a project lacks, so a second run changes nothing.
   - It records the keys it wrote in `ops/.dev-f5/` (git-ignored, readable by the owner only), so `--destroy` removes those keys and no others.
3. **One value per shared secret.** The API token and the passport key are the same in both projects, and an existing value in either project is reused.
4. **Fixed local ports.**
   - The launch profile `NFHotel.Api` listens on `https://localhost:59102` and `http://localhost:59103`.
   - The web application's profile `https` listens on `https://localhost:7060`.
   - `Api:BaseUrl` of the web application is `http://localhost:59103`.
   - `NFHotel.slnLaunch` has the profile "API and web" that starts both.
5. **The mobile app** can reach the API directly, with `--dart-define=API_BASE_URL=http://10.0.2.2:59103` from the Android emulator, or `http://localhost:59103` from the iOS simulator. The API serves the routes the app calls (`/api/auth/login`, `/api/rooms`) without the `/mobile-api` prefix that nginx takes off. Two things differ from the light mode: nginx's filter of the two routes (ADR-0015) is not in front of the API, and the address is plain http. This was not tested with the mobile app under MIL-057. The light mode remains the reference address for the app.
6. **A CI job** runs the script, starts both projects with `dotnet run` and their launch profiles, and signs in through the API and through the web form.

## Consequences

**Positive:**

- F5 works after one command, with the debugger on both projects and no Docker image of the application (maintainability, testability).
- No package or project is added to the solution, and the release build and the Docker images are unchanged.
- The settings names are those of production, so a setting that works under F5 works when deployed.
- Secrets stay outside the repository, and the database port is on the loopback only (security).

**Negative:**

- F5 is less like production than the light mode: no nginx, no TLS at a proxy, no secrets as files for the application. The light mode and CI cover those.
- The setup script needs Docker, bash (Git Bash on Windows) and the .NET SDK of `global.json`.
- The ports 59102, 59103, 7060 and 55432 must be free on the developer's machine.

## Affected Artifacts

- [MIL-057]: tasks 1 to 6.
- [ADR-0024]: unchanged; user secrets are the development form of its rule.
- [ADR-0035]: unchanged; the light mode remains the way to run the deployed shape.

---

[MIL-057]: ../milestones/mil-057-start-from-visual-studio.md
[ADR-0024]: adr-0024-secrets-as-files.md
[ADR-0035]: adr-0035-light-local-development-stack.md
[7459205]: https://github.com/NF-Hotel/NFHotel/commit/7459205acf77cd47e170d53da34fb735bcedf291
