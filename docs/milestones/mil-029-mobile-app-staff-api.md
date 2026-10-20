# MIL-029 Mobile app signs in and works with rooms through the API

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-029 |
| CrossReference | [BC-001], [US-001], [MIL-016], [ADR-0005], [ADR-0015] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | Claude (AI) | S01 | Initial version, from the decisions of S01 in chat: login and rooms, staff token only, tested against the Docker development server | pending |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Accepted after RC-134 Go (objective 26 of BC-001 added, RC-133) | pending |

---

## Purpose

Let the Flutter mobile app (`NF-Hotel/MobileApp`) sign in a member of staff and show and update rooms through the NF Hotel API, instead of its local sample data. The app is a client of the same API as the web application, so it must not weaken [ADR-0004]: no shared secret goes into an app that anyone can unpack. [ADR-0015] decides that the staff token alone opens the login and the rooms routes. The gate decides whether the app may be called a working client for housekeeping and reception, on the development server.

## Deliverable

- **API (this repository).** The service-token middleware accepts a valid `X-Staff-Token` without the service token on `POST /api/auth/login` and on the rooms routes only ([ADR-0015]); every other route is unchanged. The login throttle also counts per client address. The route list is one constant, with a test that fails when another route accepts the staff token alone.
- **Development server.** A development-only nginx route `/mobile-api/` to the API in the Docker development server of `scripts/dev-up.sh`, absent in a release deployment.
- **Mobile app (`MobileApp` repository).** A small API client (login, list rooms, room by key, the housekeeping actions) with the base URL from `--dart-define=API_BASE_URL`; the staff token in secure storage; a login screen that works; the room overview and room view read from the API; the cleaning and service buttons call the matching action; a 401 signs the member out; errors show a message, not a stack trace. The layers follow the Flutter rules of `coding-conventions` (presentation, domain, data).
- **Tests.** API tests for the new rule; Flutter unit tests of the client with a fake HTTP layer and widget tests of the login and the room overview; one end-to-end check of the app against the development server in a Docker container or an emulator in Docker, never on the host desktop.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `POST /api/auth/login` with right credentials and no service token returns 200 with a staff token; wrong credentials give 401; the sixth failure in 15 minutes gives 429 | Yes | No |
| 2 | `GET /api/rooms` with a valid staff token and no service token returns 200; without both it returns 401 | Yes | No |
| 3 | A test per other route group (bookings, guests, benchmark, market analysis, AI defaults) shows that a staff token alone is refused with 401 | Yes | No |
| 4 | A Housekeeping token can start and complete cleaning and service of a room and gets 403 on create, update and delete of a room | Yes | No |
| 5 | No token or key is in the `MobileApp` repository, in its build output or in the plain preferences of the app (checked by a search and a test) | Yes | No |
| 6 | The app signs in as each development account of `dev-up.sh` against the development server, lists the rooms of the seed data, and shows the same room states as the web back office | Yes | No |
| 7 | A cleaning action in the app changes the state that the web back office shows; an expired or tampered token returns the app to the login screen | Yes | No |
| 8 | The end-to-end check runs in Docker (or an emulator in Docker) and leaves the host desktop alone | Yes | No |
| 9 | Tests pass, `flutter analyze` and the C# analysers are clean, and the `qc-programming-csharp` and Flutter reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The login, the roles and the permissions the app reuses |
| MIL-015 | The API and the service token |
| MIL-027 | The development server and the end-to-end test setup |
| ADR-0015 | The decision to accept the staff token alone on a few routes (must be `Accepted` first) |
| `NF-Hotel/MobileApp` | The repository the Flutter code goes in; it has no Business Case or milestone of its own, so this milestone is the plan for its pull requests |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 26 of BC-001 (mobile app through the API, staff token alone); success criterion 29 | All tasks |
| US-001.09 Sign in to the back office | Task 4 |
| US-001.07 Manage rooms | Task 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S02 for the mobile track |

## Target Date

2026-11-06 — two weeks from the plan, on the development server only; for S01 to confirm. A release build and the internet address are a later milestone.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Accept ADR-0015 | Review ADR-0015 against [ADR-0004] and [ADR-0005] and record the RC. Nothing under `src/` changes before it is `Accepted`. | No | |
| 2 | Staff token alone on login and rooms | Change `ApiTokenMiddleware` so that the routes listed in one constant accept a valid staff token without the service token; add the per-address login throttle; add API tests for criteria 1 to 4. | No | ADR-0015 |
| 3 | Development route for the app | Add the development-only `/mobile-api/` nginx route to the development server of `scripts/dev-up.sh` and document the base URL for the emulator and for a device on the same network. | No | ADR-0015 |
| 4 | Flutter API client and login | In `MobileApp`: add an HTTP client with `API_BASE_URL`, secure storage of the staff token, the login screen and the sign-out on 401, with unit and widget tests. | Yes | US-001.09 |
| 5 | Rooms from the API | In `MobileApp`: the room overview and room view read from the API, and the cleaning and service actions call it; replace the local sample data. | Yes | US-001.07 |
| 6 | End-to-end check in Docker | Run the app (emulator or test driver) in a Docker container against the development server with the seeded rooms and the three development accounts, per `framework/process/gui-testing.md`; record the result for criteria 6 to 8. | No | |
| 7 | Check and review | Tests, linters and the reviews against the C# and Flutter checklists; record the RC. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[MIL-016]: ./mil-016-backoffice-login.md
