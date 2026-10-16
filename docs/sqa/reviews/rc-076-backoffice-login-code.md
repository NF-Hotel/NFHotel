# RC-076 Review of the back office login (MIL-016)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-076 |
| CrossReference | [MIL-016], [ADR-0005], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: the staff account and role (`StaffMember`, `StaffRole`, `StaffRoleParser`, `IStaffRepository`), the sign-in and account use cases and the permission table (`UseCases/Staff`), the three ports, the persistence (`StaffConfiguration`, `StaffRepository`, migration `AddStaffMember`), the password hasher, token issuer and throttle in `NFHotel.Infrastructure`, the staff authentication, `RequiresPermissionAttribute`, `AuthController` and `ApiStartup` in `NFHotel.Api`, `StaffCallContext` and `ApiStaffAuthService` in `NFHotel.ApiClient`, the sign-in cookie, policies, login page, access denied page, menu and runner change in `NFHotel.Web`, and the new tests (`NFHotel.Web.Tests` and the staff tests of the other projects)
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`, the smoke test, a deploy to the Proxmox VE container

## Go / No-Go criteria of MIL-016

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A request to any `/admin/**` page without a session is redirected to the login page, and a staff endpoint of the API without a staff token gets 401, in the deployed VM | Pass | In the VM through nginx: `GET /admin` answered `302 -> /admin/login?returnUrl=%2Fadmin`; the API answered 401 to `GET /api/rooms` with the web token alone. `AdminAccessTests` covers the five pages; `StaffAuthorizationTests` covers the API; the smoke test checks both (19 of 19 checks pass) |
| 2 | For every role and operation a test states allowed or refused, and the table equals the Access Control Matrix | Pass | `RolePermissionsTests` writes the matrix out per role and compares all 24 role and permission pairs; `EndpointProtectionTests` fails when a controller action neither requires a permission nor is on the four-entry public list, and when that list names an action that does not exist |
| 3 | Housekeeping gets 403 on bookings, guests and sales and cannot see a passport number; a Receptionist gets 403 on room management and sales | Pass | In the VM a Housekeeping token got 403 on `GET /api/guests` and `POST /api/bookings/overview` and 200 on `GET /api/rooms`. `StaffAuthorizationTests` (guest answer holds no guest data; receptionist refused on create, delete, take out of service, cleaning); `AdminAccessTests` (pages for Housekeeping and the revenue page for a Receptionist) |
| 4 | A tampered, expired or wrongly signed token is refused; the signing key and every password hash are absent from git, logs and API responses | Pass | `StaffAuthorizationTests`: tampered signature, expired, another key, algorithm none, another audience, the staff token in the wrong header, an unknown role and a numeric role; in the VM a tampered token got 401. The signing key and the first password are only in the git-ignored `nfhotel-api.env`; the examples hold placeholders; the smoke test shows the web container holds neither. The session DTO has token, expiry, name and role only; the hash is never in a DTO, and the first-Manager error names the code, not the password (`ApiStartupTests`) |
| 5 | A wrong password and an unknown user give the same answer; the sixth failed attempt in the window gets 429 | Pass | `StaffLoginTests` (same body for unknown, wrong and inactive; five 401, then 429 even with the right password; four failures then success; other names unaffected); in the VM the answers were byte-identical and the sixth attempt got 429, which also locked the Manager out until the API restarted, as ADR-0005 says it would |
| 6 | The public booking flow works unchanged without signing in | Pass | In the VM with the service token only: free rooms 200, a booking with a new guest 200, the booking read 200, the confirmation refused with 401 and accepted with the Manager token; `/book` answered 200 through nginx |
| 7 | The deploy applies the migration and creates the first Manager from the environment file; a second run changes nothing | Pass | The first deploy created the table and the Manager (`staff_member` had one row, hash prefix `pbkdf2-sha256$600000$`); after `docker restart` of the API the Manager signed in and no second row was made; `ApiStartupTests` and `StaffAccountServiceTests` cover creation, idempotence, the race and a short password |
| 8 | 0 build warnings, format clean, all tests pass; reviews `Go` | Pass | 852 tests, 0 failed, 3 skipped (the live Proxmox tests), up from 606; ADR-0005 RC-069, this record and RC-077 |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `StaffMember`, `StaffAuthService`, `Pbkdf2PasswordHasher`; three-part test names (the architecture test passes) |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file | Pass | `AsyncRulesTests` covers the new code; `ILoginThrottle` and the other ports are synchronous on purpose (in-memory, no I/O) |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; 0 warnings; no suppression added |
| 4 | Nullable enabled; `!` justified | Pass | `!` only in the login page where the form model is created in `OnInitialized`, and in tests that pass `null!` on purpose |
| 5 | `IDisposable` disposed | Pass | Scopes are `await using`; test hosts and clients are disposed |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | `AsyncRulesTests`; its pattern was narrowed so that assigning the `Result` property of an authorization filter is not taken for reading a task |
| 7 | Specific exceptions, arguments validated at public boundaries, no empty catch | Pass | The hasher catches only `FormatException` of a malformed stored hash and answers false; the token source catches only `InvalidOperationException` when no authentication state exists; every public method checks its arguments |
| 8 | Public types have XML documentation | Pass | Enforced by the build |
| 9 | Properties not public fields; immutability | Pass | Records for the DTOs; `StaffMember` has private setters and a factory |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched; the token expiry is a `DateTimeOffset` |
| 11 | Public async APIs take a `CancellationToken` | Pass | Every new public async method; the start-up methods receive `ApplicationStopping` |
| 12 | Tests cover new behaviour, three-part names, no order, time or network | Pass | 246 tests added (606 to 852); the throttle and the token expiry use `FakeClock`; no sleep and no system clock; the web and API tests use in-process hosts |
| 13 | Classes trace to the Design Class Diagram | N-A | The project DCD is not part of MIL-016; the domain model DM-004 and DM-005 are updated |
| 14 | Each piece of knowledge is defined once | Pass | The permission table is one class used by the API attribute and the web policies; the role is parsed by one parser; the routes and the header name are in `NFHotel.Api.Contracts` |
| 15 | References point inward, no cycles | Pass | `DatabaseIsolationTests` still passes: the web side has no reference to Infrastructure, EF Core or Npgsql; the JWT code is in Infrastructure and the API only |
| 16 | SOLID | Pass | Ports in Application, adapters in Infrastructure; the controller maps one call to one use case; the attribute checks one permission |
| 17 | Layer folders | Pass | `UseCases/Staff`, `Dto/Staff`, `Ports`, `Security`, `Startup`; the structure tests were extended for `Dto/Staff` |
| 18 | File and folder names | Pass | English, no space |

## Defect found by the tests, and fixed

`Enum.TryParse` accepts a number, so a token whose role claim was `"2"` would have been read as Manager. A test (`Get_TokenWithTheRoleAsANumber_ReturnsForbidden`) caught it before the first run of the API; `StaffRoleParser` accepts only the exact name of a role and is used by the API, the web claims and the command line (which first matches the name without regard to case).

## Deviations from the accepted milestone

| Milestone text | What was done | Why |
| --- | --- | --- |
| The Application layer holds a permission check in each back office use case | The permission table is in Application (`UseCases/Staff/RolePermissions`); the check runs in the API before the use case is called, and in the web policies | The use cases take no caller (the interfaces are shared with the client and with the public flow), and ADR-0005 records the API as the authority; a staff call that reaches a use case has passed the check |
| Cookie is `HttpOnly`, `Secure`, `SameSite` | `Secure` only when the request is HTTPS | The public port serves plain HTTP; TLS is a separate decision, recorded in ADR-0005 and UC-004 |
| Manager also manages staff accounts | No endpoint or page; the Manager adds accounts with the `create-staff` command | Keeps the account management small; recorded in UC-004 open issues |
| The deploy applies the migration | The API applies it at start when `Database__MigrateOnStart=true` | The deploy has no step that knows the schema; the setting is in the env example |

## Overall Verdict

Go — all eight criteria of MIL-016 pass and the applicable criteria of QC-CS-001 pass. The review is by the stand-in, not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Put TLS in front of the public port before the sign-in is used over an untrusted network | S03 | before production |
| Persist the data protection keys of the web application, so a restart of the container does not sign everyone out | S01 | optional |
| Plan the Guest role and an own-resource check for the receipt page | S01 | later milestone |

---

[MIL-016]: ../../milestones/mil-016-backoffice-login.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
