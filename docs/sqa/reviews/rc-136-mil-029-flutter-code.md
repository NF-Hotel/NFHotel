# RC-136 Review of the Flutter code of MIL-029 (tasks 4 and 5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-136 |
| CrossReference | [MIL-029], [ADR-0015], [QC-FL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: the new Dart code in `NF-Hotel/MobileApp`, branch `mil-029-api-client-and-rooms`: `lib/domain`, `lib/infrastructure`, `lib/presentation/features`, `lib/presentation/session`, `lib/presentation/shared`, `lib/app.dart`, `lib/main.dart`, and all of `test/`. The older sign-up code (`components/`, `Model/`, `presentation/NewUserScreen.dart`) is not part of the work and is not reviewed
- Checklist used: [QC-FL-001]
- Scope: full review of the new code
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent. The AI wrote the code it reviewed
- Evidence: `dart format` check, `flutter analyze` and `flutter test` in the container `ghcr.io/cirruslabs/flutter:stable` (Dart 3.12, SDK constraint lowered to `^3.12.0` in a copy, because no image has Dart 3.13); an end-to-end run of the API layer against the development server (below)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming and casing | Pass | New files are `snake_case`, types `PascalCase`, members `lowerCamelCase`, private members `_`; the older `Model/UserRole.dart` style is outside the scope |
| 2 | Names state purpose; one public widget per file; booleans as questions | Pass | One widget per file; `isBusy`, `isRestoring`, `canStartCleaning` |
| 3 | `flutter analyze` clean; Flutter version pinned | Fail | The new code has 0 findings (after `prefer_initializing_formals` was fixed with private named parameters). The project as a whole has 12: 2 errors in `components/signup_form.dart` and 10 infos in the older code. The Flutter version is not pinned (no `.fvmrc`); `pubspec.yaml` asks for Dart `^3.13.2` and no published image has it |
| 4 | `dart format`; imports ordered; no hand-edited generated files | Pass | `dart format --set-exit-if-changed` passes on the new code |
| 5 | Null safety, no `dynamic`, immutable entities | Pass | No `!` in the new code; one `late final` (`_login` in `app.dart`) with the invariant in a comment; entities have `final` fields |
| 6 | `build` is pure | Pass | Side effects are in `initState`, event handlers and view models; the `ListenableBuilder` builders only read state |
| 7 | No helper methods that return widgets; `const` used | Pass after fix | `RoomOverviewScreen` had a `_body` method that returned widgets; it is now the private widget `_RoomOverviewBody`. `prefer_const_constructors` is clean |
| 8 | Dispose; `mounted` after `await` | Pass | The text controllers are disposed; `_open` checks `mounted` after the pushed route returns; `_perform` uses no `BuildContext` after its `await` |
| 9 | Errors by type, mapped to UI state | Pass | `Result` carries a `FailureKind`; the screens render loading, data and error. The HTTP client catches `TimeoutException`, `ClientException` and, last, `Exception` (a socket or TLS error that the client does not wrap), each mapped to a network failure. `FlutterError.onError` keeps its default; no crash reporting is added |
| 10 | One state-management approach; no business rules in widgets | Pass | `ChangeNotifier` with `ListenableBuilder` throughout; what an action is allowed comes from the API (`RoomActions`) |
| 11 | Localisation, `Theme`, `Semantics` | Not met (Optional) | The texts are in one file (`strings.dart`), not ARB; colours come from `Theme`; icon buttons have tooltips; no extra `Semantics` labels |
| 12 | Tests; no fixed delay; end to end in a container | Pass | 64 tests pass, with fakes and no `Future.delayed`; the one real timeout test uses 20 ms. The end-to-end test `test/e2e/dev_server_test.dart` ran in a container against the development server (below) |
| 13 | Trace to the Design Class Diagram | N-A | The app has no Design Class Diagram; ADR-0015 and MIL-029 are the design record |
| 14 | One place for each piece of knowledge | Pass | The route of each action is in one `switch`; the texts in one file; the API shapes in `ApiJson`; whether an action is allowed comes from the API and is not repeated in the app |
| 15 | Dependencies point inward | Pass | `lib/domain` imports no `package:flutter` and no network or storage package; the secure storage and the HTTP client are in `lib/infrastructure` |
| 16 | SOLID | Pass | Narrow ports (`AuthRepository`, `RoomRepository`, `SessionStore`) with the implementations given from `main.dart` |
| 17 | Layers and folders | Pass with a deviation | `domain/`, `infrastructure/`, `presentation/` by feature; there is no `application/` layer, because the app has no business rule of its own and the view models call the ports. The old `Model/`, `components/`, `utillity/` and `Assets/` folders remain |
| 18 | File and folder names | Pass for the new code | The old `NewUserScreen.dart` and `UserRole.dart` have PascalCase names |

## End-to-end result against the development server

Run in the container with the receptionist and housekeeping accounts of `dev-staff.env` (the manager account is not in that file and was not used), through `https://host.docker.internal:8443/mobile-api` after `scripts/dev-up.sh api nginx`:

- both accounts sign in with the right role; a wrong password gives the credentials failure and does not end a session;
- the seeded rooms are listed; housekeeping marks a clean room for daily cleaning, starts and completes cleaning, and the room reads clean again afterwards (it leaves the data as it found it);
- an illegal action (starting a service on a clean room) is rejected, not a network failure;
- a tampered token gives the unauthorized failure and calls the sign-out hook once.

By `curl` against the same server: the route answers only the sign-in and the rooms (bookings and guests give 404 at the proxy); a staff token alone reads rooms; no token, or a tampered token, gives 401; Housekeeping gets 403 on deleting a room; a made-up `Authorization` header is dropped by the proxy.

Not done: the screens were not driven in an emulator, and the rooms were not compared with the web back office, so criteria 6 and 7 of MIL-029 are only met for the API layer.

## Overall Verdict

Go-with-conditions — every Mandatory criterion passes for the new code except 3 (the Flutter version is not pinned, and the project as a whole is not analyzer-clean because of the older sign-up code). The conditions are in the action items.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Pin the Flutter version (`.fvmrc` or the CI image) that matches `^3.13.2`, run `flutter pub get` with it and commit the new `pubspec.lock` | S01 | Before the MobileApp pull request |
| Fix or remove `components/signup_form.dart` and the other older sign-up code so that `flutter analyze` is clean for the whole project | S01 | Before the MobileApp pull request |
| Drive the screens in an emulator in Docker and compare the rooms with the web back office (criteria 6 and 7 of MIL-029); or record that S01 accepts the API-layer test instead | S01 | Task 6 |
| Move the texts to ARB files with `flutter gen-l10n` | S01 | Later, optional |

---

[MIL-029]: ../../milestones/mil-029-mobile-app-staff-api.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[QC-FL-001]: ../../../framework/qc/code/qc-programming-flutter.md
