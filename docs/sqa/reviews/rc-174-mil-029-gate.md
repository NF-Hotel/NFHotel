# RC-174 Gate check of MIL-029 (the mobile app signs in and works with rooms through the API)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-174 |
| CrossReference | [MIL-029], [ADR-0015], [RC-135], [RC-136], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [6d810e2] |

---

## Artifact Under Review

- **Instance reviewed:** the nine Go/No-Go criteria of [MIL-029].
  - The API side is in this repository (NFHotel#196, RC-135).
  - The app is in NF-Hotel/MobileApp#1 (RC-136), with the commit `63337fc` that closes RC-136's conditions.
- **Checklist used:** [QC-MIL-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones on 2026-10-23.
- **Environment of the app checks:** the container `ghcr.io/cirruslabs/flutter:3.44.0`, pinned by digest, on the legion laptop. The end-to-end check ran against the local development server of `scripts/dev-up.sh`. Nothing ran on the host desktop (`framework/process/gui-testing.md`).

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | Login with no service token: 200 with a staff token; wrong credentials 401; the sixth failure 429 | Go | API tests of NFHotel#196 (RC-135). The E2E test `SignIn_SixthFailureInARow_ShowsTooManyAttempts` runs in CI |
| 2 | `GET /api/rooms` with a staff token alone is 200; with neither token 401 | Go | API tests of NFHotel#196 (RC-135) |
| 3 | A staff token alone is refused with 401 on bookings, guests, benchmark, market analysis and AI defaults | Go | One API test per route group (RC-135) |
| 4 | Housekeeping can clean and service a room, and gets 403 on create, update and delete | Go | API tests (RC-135). The app's end-to-end check: "housekeeping runs a cleaning round and the room ends clean" |
| 5 | No token or key in the app repository, its build output or its plain preferences | Go | `test/no_secret_in_the_app_test.dart`: no bearer token or `Authorization` header and no shared preferences. The token is kept in `flutter_secure_storage` |
| 6 | The app signs in as each development account and lists the seeded rooms as the back office shows them | Go | `test/e2e/dev_server_test.dart`, 6 of 6 against `https://host.docker.internal:8443/mobile-api`. Covered: "each development account signs in with its own role", and "the seeded rooms are listed with numbers, floors and actions". It reads the same API as the web back office |
| 7 | A cleaning action changes the state the back office shows; a tampered token returns to the login | Go | Same run: "housekeeping runs a cleaning round and the room ends clean", "a tampered token ends the session" |
| 8 | The end-to-end check runs in Docker and leaves the host desktop alone | Go | It ran in the Flutter container with `--env-file` for the accounts, which are never printed |
| 9 | Tests pass, `flutter analyze` and the C# analysers are clean, and the C# and Flutter reviews are `Go` | Go | `flutter analyze`: no issues. `flutter test`: 64 passed (6 end-to-end skipped without the accounts). The app's new CI workflow runs both on every PR. The C# side passes the 19 required checks of NFHotel. RC-135 is Go. RC-136 was Go-with-conditions: Flutter is now pinned at 3.44.0 (Dart ^3.12.0, the stable release), the lock file is regenerated, and the project is analyzer-clean. Its conditions are met |

## Findings

- **The SDK constraint was unsatisfiable.** `sdk: ^3.13.2` matched no stable Flutter. 3.44.0 ships Dart 3.12, so the constraint is now `^3.12.0`.
- **The release build and the public address of the API are still undecided** (ADR-0015). The app is used against the development server only.

## Overall Verdict

Go. MIL-029 is complete.

## Action Items

None.

---

[MIL-029]: ../../milestones/mil-029-mobile-app-staff-api.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[RC-135]: ./rc-135-mil-029-api-code.md
[RC-136]: ./rc-136-mil-029-flutter-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[6d810e2]: https://github.com/NF-Hotel/NFHotel/commit/6d810e246e686f0cd529ba51581af9d88273a649
