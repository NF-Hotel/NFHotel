# RC-065 Review of the database API (MIL-015)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-065 |
| CrossReference | [MIL-015], [ADR-0004], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [b8170b7] |

---

## Artifact Under Review

- Instance reviewed: `NFHotel.Api` (controllers, token middleware, composition root), `NFHotel.Api.Contracts`, `NFHotel.ApiClient`, `NFHotel.External`, the switch of `NFHotel.Web` to the client, the repaired booking read queries, and the new tests (`NFHotel.Api.Tests`, `NFHotel.ApiClient.Tests`, `NFHotel.External.Tests`, `NFHotel.TestSupport`, `DatabaseIsolationTests` and the integration tests of the read side)
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`, the deployed VM on a Proxmox VE container

## Go / No-Go criteria of MIL-015

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The web side has no reference to Infrastructure, EF Core or Npgsql, and no database code in its build | Pass | `DatabaseIsolationTests` reads the project files and the restored graph of `NFHotel.Web`, `NFHotel.ApiClient`, `NFHotel.External` and `NFHotel.Api.Contracts`; shown to fail when `NFHotel.Web` got a reference to Infrastructure for a moment, and shown to see the API's graph (Npgsql, Infrastructure) |
| 2 | In the deployed VM the web container has no database setting and is not on `nfhotel-db` | Pass | `docker inspect`: web on `nfhotel-api` and `nfhotel-proxy`, API on `nfhotel-api` and `nfhotel-db`, PostgreSQL on `nfhotel-db`; `printenv` of the web container shows 0 lines with `ConnectionStrings`, `PassportKey` or `POSTGRES` |
| 3 | A call to the API without the token, or with a wrong one, gets 401 and changes nothing | Pass | `ApiAuthenticationTests` (no token, four wrong tokens including a prefix, wrong scheme, a write without a token); in the VM a call without a token answered 401 |
| 4 | The API and PostgreSQL publish no host port; from outside the VM only port 80 of nginx answers | Pass | `docker ps` in the VM: ports only on nginx (`0.0.0.0:80`, and `127.0.0.1:8081` on the loopback); the smoke test checks that the API and web publish none |
| 5 | Every method of the three interfaces gives the same result through the API as directly | Pass | `ContractParityTests`: about a hundred scenarios compared in step, value, error codes and exceptions; a second test fails if an interface method has no scenario; shown to fail when one client path was broken |
| 6 | The view-model tests pass unchanged, and a full booking, check-in and check-out can be done in the deployed application | Pass | No view-model or page test exists in the repository, so there was nothing to change. In the VM, through the API with the token of the web container and a real PostgreSQL: room, guest, booking, confirm, check-in and check-out all answered 200, a second booking of the same period 409, and the pages `/`, `/admin`, `/admin/rooms`, `/admin/guests` and `/admin/bookings` through nginx rendered the stored rooms and guests with no error in the web log. The run found a real defect (see below), fixed in this milestone |
| 7 | The passport number is encrypted in the database and readable in the application | Pass | The column holds ciphertext (`AhV6mEWsoZKQ...`), the API returns `PASS12345`, and `GuestGetDetailsAsync_StoredGuest_ReturnsTheDetailsWithThePassportNumber` passes against PostgreSQL |
| 8 | 0 build warnings, format clean, all tests pass; reviews `Go` | Pass | 606 tests, 0 failed, 3 skipped (the live Proxmox tests); ADR-0004 RC-064, this record and RC-066 |

## Defect found by the proof, and fixed

A full booking flow through the API against PostgreSQL failed: creating a booking saved it and then returned 500 when it read the booking back, and the overview returned 500. `BookingRepository.GetReadModelAsync` and `GetOverviewAsync` filtered and ordered on members of the positional read model that the query had just constructed, which the current EF Core cannot translate to SQL. The filter and the order now sit on the entity before the projection. No test of the suite read bookings from a real database, so nothing had caught it. Fifteen integration tests now cover the read side of bookings, guests and rooms (`BookingReadTests`, `GuestAndRoomReadTests`). The defect was in the code before this milestone.

## Deviations from the accepted milestone

| Milestone text | What was done | Why |
| --- | --- | --- |
| Project `NFHotel.Holidays` | `NFHotel.External` | It also holds the clock, which the web application and the API both need; recorded in ADR-0004 |
| An endpoint per interface method | No endpoint for `IGuestService.ValidateFields` | It reads and writes nothing, so the client runs the same domain rule locally (`GuestService.Validate`); a blocking call to the API would be the sync-over-async that MIL-014 forbids |
| Criterion 6: view-model tests pass unchanged | There are no view-model tests | The criterion is met by the proof in the VM |
| Test projects | Four added: `NFHotel.Api.Tests`, `NFHotel.ApiClient.Tests`, `NFHotel.External.Tests`, `NFHotel.TestSupport` | The contract test needs the in-memory services of the test support |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | Projects, controllers, `ApiGateway`, `ApiToken`, `ServiceWorld`; three-part test names (the architecture test passes) |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file | Pass | `AsyncRulesTests` covers the new projects too. `ServiceWorld.cs` groups the in-memory fakes of one test-support project in one file, by design |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; 0 warnings; no suppression added |
| 4 | Nullable enabled; `!` justified | Pass | `!` only on `GetProperty(...)!` in the test support and in tests that pass `null!` on purpose |
| 5 | `IDisposable` disposed | Pass | `HttpClient` comes from the factory; test factories are disposed in `DisposeAsync`; `await using` for contexts |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | `AsyncRulesTests` |
| 7 | Specific exceptions, arguments validated at public boundaries, no empty catch | Pass | The gateway catches only `HttpRequestException`, `TaskCanceledException` (when the caller did not cancel), `JsonException` and `NotSupportedException` and maps each to one code; `ArgumentException` in a controller call is answered 400 |
| 8 | Public types have XML documentation | Pass | Every new public type and member, enforced by the build |
| 9 | Properties not public fields; immutability | Pass | Records for the envelope and the options |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | Every controller action and every client method; the middleware uses `HttpContext.RequestAborted` |
| 12 | Tests cover new behaviour, three-part names, no order, time or network | Pass | 92 tests added in this milestone (514 to 606), among them the contract test; no sleep, no system clock; the API tests use an in-process server, not the network |
| 13 | Classes trace to the Design Class Diagram | N-A | No domain class changed |
| 14 | Each piece of knowledge is defined once | Pass | Routes, envelope and JSON settings in `NFHotel.Api.Contracts`; the guest rule is `GuestService.Validate`, used by the service and the client; the in-memory ports in one project |
| 15 | References point inward, no cycles | Pass | `DatabaseIsolationTests`; the API references Application, Infrastructure and External; the client references Application and the contracts only |
| 16 | SOLID | Pass | Controllers only map one call to one use case; the client implements the same interfaces, so the screens depend on the abstraction |
| 17 | Layer folders | Pass | `Controllers/`, `Security/` in the API; the projects of ADR-0004 |
| 18 | File and folder names | Pass | English, no space |

## Overall Verdict

Go — every applicable criterion of QC-CS-001 and all eight Go criteria of MIL-015 pass. What was not done: a deploy to a real remote Proxmox node over ssh, and authentication of the people who use the back office. The admin pages `/admin/...` are served on the public port without a login; that was so before this milestone and is outside it (the old issue about role-based access control, D-03).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide on a login for the back office before the VM is reachable from the internet | S01 | Open |
| Apply the EF migrations in the deploy (the deploy applies none; the proof applied them by hand from `dotnet ef migrations script --idempotent`) | S01 | Open |

---

[MIL-015]: ../../milestones/mil-015-database-api.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[b8170b7]: https://github.com/NF-Hotel/NFHotel/commit/b8170b7a84409f6e19f209090c310a9f42c53bc9
