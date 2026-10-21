# RC-135 Review of the API code of MIL-029 (tasks 2 and 3)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-135 |
| CrossReference | [MIL-029], [ADR-0015], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | [d4854a9] |

---

## Artifact Under Review

- Instance reviewed: the C# of MIL-029 tasks 2 and 3 on the branch `mil-029-mobile-app-staff-token`: `StaffTokenRoutes`, `ApiTokenMiddleware`, `AddressLoginThrottle`, `LoginAddressLimitFilter`, `AuthController` and `Program.cs` in `src/NFHotel.Api`, and the tests `MobileAccessTests`, `EndpointProtectionTests`, `StaffLoginTests`, `StaffAuthorizationTests` and `AsyncRulesTests`. The nginx and script files are not C# and are covered by the `qc-yaml` and shell checks of the milestone review
- Checklist used: [QC-CS-001]
- Scope: full review of the changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent. The AI wrote the code it reviewed

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming | Pass | PascalCase types and constants, `_camelCase` fields |
| 2 | Async suffix, one type per file | Pass | One type per file, named after it |
| 3 | Formatting and analyzers, no unexplained suppressions | Pass | `dotnet build NFHotel.sln`: 0 warnings, 0 errors (the build treats analyzer findings as errors; it caught one unused `using`, removed); no suppressions added |
| 4 | Nullable types, no unexplained `!` | Pass | No `!` in the new source; `IPAddress?` is handled |
| 5 | Disposal | Pass | Two test requests were not disposed; fixed to `using var` |
| 6 | No blocking calls | Pass after fix | The source scan of `AsyncRulesTests` read `executed.Result` in the filter as a blocking call (it is the action result, not `Task.Result`); rewritten as a property pattern |
| 7 | Exceptions and argument checks | Pass | `ArgumentNullException.ThrowIfNull` at every public boundary; no `catch` |
| 8 | XML documentation | Pass | On every public type and member |
| 9 | Immutability | Pass | `readonly` fields, `const` values |
| 10 | Money and time | N-A | None involved; the throttle uses the injected `IClock` |
| 11 | Cancellation | Pass after fix | The architecture rule wants a `CancellationToken` on a public async method; the filter receives `ActionExecutingContext`, whose `HttpContext.RequestAborted` is the token of the call, as for a middleware. The rule's exemption now names `ActionExecutingContext` beside `HttpContext`, with a comment |
| 12 | Tests | Pass | The new cases in `MobileAccessTests` (one per Go/No-Go criterion 1 to 4 and the forwarded address) and one in `EndpointProtectionTests`, named `Method_Condition_Expected`, covering criteria 1 to 4 of MIL-029 and the forwarded address. Two old tests that asserted the old rule were changed to the new one. Full solution: 1,343 tests, 0 failed. Tests that fail 20 times use their own factory so the shared address count cannot make them depend on order |
| 13 | Trace to the design | Pass | Each type cites ADR-0015 in its comment; the API has no Design Class Diagram of its own, and ADR-0015 records the deviation from ADR-0004 (the service token is not required on two route groups) |
| 14 | One place for each piece of knowledge | Pass after fix | "The call sent no bearer token" was written in the middleware and again in the controller; now `StaffTokenRoutes.HasNoServiceToken` is the one place, used by both. The list of mobile routes is one class |
| 15 | Dependencies point inward | Pass | `Api` already referenced `Infrastructure` for the staff token issuer; `AddressLoginThrottle` reuses `InMemoryLoginThrottle` from there. No reference from Domain or Application to Api |
| 16 | SOLID | Pass after fix | The first version put the address limit in `AuthController`, which broke ADR-0004 (a controller makes one call to a use case). The limit is now `LoginAddressLimitFilter`, applied with `[ServiceFilter]`; the controller is back to one call |
| 17 | Layers and folders | Pass | Security types in `NFHotel.Api/Security`, controller in `Controllers`, tests in `NFHotel.Api.Tests` |
| 18 | File and folder names | Pass | English, no spaces |

## Overall Verdict

Go — all criteria pass; four defects were found by this review and fixed before the verdict (the duplicated rule, the logic in the controller, the undisposed requests and the false hit of the blocking-call scan), and the architecture rule was widened by one named parameter type.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| The middleware checks only that `X-Staff-Token` is present on a rooms route; the permission attribute validates it. This is safe while every rooms action has the attribute, which `RoomsController_EveryAction_RequiresAPermission` now enforces. If S01 wants a second check, validate the token in the middleware as well | S01 | Optional |
| `UseForwardedHeaders` trusts every sender of `X-Forwarded-For` (limit 1: the last entry, which nginx appends). That is safe only while the API publishes no port; repeat the check in the ADR for a release build | S01 | Before any release build |
| Run the route on the real development server (`scripts/dev-up.sh`) and record the result for criteria 1 and 2 of MIL-029 | S01 | Task 6 |

---

[MIL-029]: ../../milestones/mil-029-mobile-app-staff-api.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[d4854a9]: https://github.com/NF-Hotel/NFHotel/commit/d4854a98afb67dfadb7294ecb0ddd1394a593dc1
