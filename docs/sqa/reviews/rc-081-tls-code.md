# RC-081 Review of the HTTPS requirement of the web application (MIL-017)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-081 |
| CrossReference | [MIL-017], [ADR-0006], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [065c6e8] |

---

## Artifact Under Review

- Instance reviewed: `HttpsRequirement` and the `Secure` cookie policy in `StaffSecurityRegistration`, the wiring in `Program.cs` of `NFHotel.Web`, and the tests (`HttpsRequirementTests`, the extension point of `WebFactory`)
- Checklist used: [QC-CS-001]
- Scope: the changes of this milestone
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`, the smoke test, a deploy to the Proxmox VE container

## Go / No-Go criteria of MIL-017

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | `http://<vm>/anything` answers 301 to the same path on HTTPS and no page is served on port 80 | Pass | In the VM: `301 -> https://127.0.0.1/anything?x=1`; the smoke test checks `/` and `/admin` on port 80 (both 301) |
| 2 | HTTPS answers 200 with the development certificate; TLS 1.3 and 1.2 work, 1.1 and 1.0 are refused | Pass | In the VM: TLS 1.2 200, TLS 1.3 200, TLS 1.1 and 1.0 `000` (refused); the smoke test checks 1.2 and 1.1 |
| 3 | The staff cookie has `Secure` and `HttpOnly`, and a sign-in works over HTTPS end to end | Pass | In the VM the sign-in over HTTPS answered 302 and set `nfhotel.staff=<value>; path=/; secure; samesite=lax; httponly`, then `/admin/rooms` answered 200; `HttpsRequirementTests` checks the attributes behind a forwarded HTTPS request |
| 4 | No private key or certificate is in git; the folder is mounted read-only | Pass | `git check-ignore` is true for both files; the compose file mounts `./tls` with `:ro`; the script writes the files in the ignored folder only |
| 5 | HSTS is absent with the self-signed certificate and present with `NGINX_HSTS` set | Pass | Absent in the VM and in the smoke test; the template adds the header with the value of `NGINX_HSTS`, and nginx omits it when the value is empty (shown by the two checks above). The presence with a value was not run: no real certificate exists |
| 6 | From outside the VM only ports 80 and 443 of nginx answer; the API, PostgreSQL and the web container publish nothing | Pass | `docker ps` in the VM: `0.0.0.0:80`, `0.0.0.0:443` and `127.0.0.1:8081` on nginx, only unpublished container ports elsewhere; the smoke test checks the API, web and analysis publish none |
| 7 | The internal listener on 8081 is unchanged and bound to the loopback | Pass | `127.0.0.1:8081->8081`; the analysis and benchmark checks of the smoke test pass |
| 8 | 0 build warnings, format clean, all tests pass; reviews `Go` | Pass | 863 tests, 0 failed, 3 skipped (the live Proxmox tests), up from 852; ADR-0006 RC-080, this record and RC-082 |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `HttpsRequirement`, `UseHttpsRequirement`; three-part test names |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file | Pass | The middleware is an inline delegate; `AsyncRulesTests` passes |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; no suppression added |
| 4 | Nullable enabled; `!` justified | Pass | No `!` added |
| 5 | `IDisposable` disposed | Pass | Test factories and clients are disposed |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | `AsyncRulesTests` |
| 7 | Specific exceptions, arguments validated at public boundaries, no empty catch | Pass | Both public methods check their arguments; no catch |
| 8 | Public types have XML documentation | Pass | Enforced by the build |
| 9 | Properties not public fields; immutability | Pass | Static class with constants |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | N-A | No public async method added |
| 12 | Tests cover new behaviour, three-part names, no order, time or network | Pass | 11 tests added: the setting, the redirect with its query, the forwarded request, the `Secure` cookie, the sign-in arriving as HTTP, and local development; no sleep, no network |
| 13 | Classes trace to the Design Class Diagram | N-A | No domain class changed |
| 14 | Each piece of knowledge is defined once | Pass | The setting name is one constant used by the reader and the tests |
| 15 | References point inward, no cycles | Pass | `DatabaseIsolationTests` passes |
| 16 | SOLID | Pass | One small extension with one job; the cookie policy is chosen where the cookie is configured |
| 17 | Layer folders | Pass | `Security/` of the web project |
| 18 | File and folder names | Pass | English, no space |

## Deviations from the accepted milestone

| Milestone text | What was done | Why |
| --- | --- | --- |
| The cookie is always `Secure` | Always `Secure` when `Tls__Require=true` (the deployment); `Secure` when the request is HTTPS otherwise | Local development runs on plain HTTP without a certificate; the deployment env file sets the switch, and the smoke test and the VM run with it |
| The web application trusts the forwarded headers of the proxy network only | It trusts them as `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` does, and the container is reachable only from the proxy network | Unchanged setting of MIL-002; the network is the restriction. Recorded as an accepted risk |
| `HSTS` presence checked with a real certificate | Absence checked; presence not run | No real certificate exists in the project yet |

## Overall Verdict

Go — all eight criteria of MIL-017 pass (criterion 5 for the absent case, as written in the deviations) and the applicable criteria of QC-CS-001 pass. The review is by the stand-in, not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Get a public host name and a real certificate, then decide issuing and renewal in a new ADR; set `NGINX_HSTS` | S01, S03 | before production |
| Update the open issue of UC-004 (the password is no longer sent in clear text) at its next change | S01 | next change of UC-004 |

---

[MIL-017]: ../../milestones/mil-017-tls.md
[ADR-0006]: ../../adr/adr-0006-tls.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[065c6e8]: https://github.com/NF-Hotel/NFHotel/commit/065c6e85054e7b24dc5848e241038ede6d747f49
