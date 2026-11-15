# RC-391 Review of the code of MIL-018 against QC-CS-001 and QC-SH-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-391 |
| CrossReference | [MIL-018], [MIL-016], [ADR-0047], [ADR-0005], [MIL-079], [UC-010], [OC-001], [QC-CS-001], [QC-SH-001], [QC-YML-001], [RC-378], [RC-388], [RC-389], [RC-390] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: review of the code of MIL-018 (tasks 3 to 6) against QC-CS-001 and QC-SH-001; F1 to F3, F6, F7, F11, F12, F14 and F15 fixed; Go; mode autonomous, not independent | [9e72b0e] |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | F1: `TryCountAttempt` written as a compare-and-swap loop after SonarCloud S2583 (a false positive on the flag set in the delegates); behaviour and tests unchanged<br>CA1829 and CA1859 of `AccessControlMatrixTests` fixed; Go unchanged | [0532e78] |

---

## Artifact Under Review

- Instance reviewed: the code of tasks 3 to 6 of [MIL-018] on the branch `mil-018-guest-login` (on `main` at
  6dd1817), before its pull request: 42 new and 41 changed C# and Razor files under `src/` and `tests/`, three removed
  (the receipt page of [MIL-079], which the guest sign-in replaces), the new `ops/scripts/check-guest-login.sh` and the
  guest part of `ops/tests/check-e2e-stack.sh`. By task:
  - task 3, the sign-in and the ownership rule: `GuestAccessService`, `GuestOwnership`, the ports `IGuestTokenIssuer`
    and `IGuestCallContext`, `JwtGuestTokenIssuer`, and the throttle per booking number that the sign-in shares with
    the receipt lookup of [MIL-079] (`ILoginThrottle`, `InMemoryLoginThrottle`);
  - task 4, the API: `GuestAuthController` (`POST api/auth/guest-login`), the guest scheme `GuestAuthentication` beside
    the staff scheme, `HttpGuestCallContext`, the guest permission of four actions (`RequiresPermissionAttribute`), and
    the session in the answer of the public booking;
  - task 5, the pages: `/booking` (`GuestSignIn`) and `/my-booking` (`MyBooking`) with their view models, the guest
    cookie (`GuestSignInRegistration`, `GuestClaims`), the one-time ticket from the booking flow
    (`GuestSessionTickets`), the token holder and the runner that sends the guest token (`GuestTokenHolder`,
    `UseCaseRunner`), and the client of the API (`ApiGuestAccessService`, `GuestCallContext`);
  - task 6, the smoke tests: `check-guest-login.sh` for the deployed site, and the guest part of `check-e2e-stack.sh`,
    which the end-to-end job of NF Hotel CI runs.
- Checklists used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`), all 21 criteria; [QC-SH-001]
  (`framework/qc/code/qc-programming-shell.md`), all 15, for the new script and the changed part of the other.
  [QC-YML-001], which criterion 8 of [MIL-018] names: no YAML file changed, so it has nothing to review.
- Scope: full review of the code of the milestone. The shared types it changed (`BookingService`, `GuestService`,
  `BookingsController`, `GuestsController`, `RequiresPermissionAttribute`, `UseCaseRunner`, `StaffAuthentication` and
  others) are reviewed for the lines it changed; the rest of each stands as its own records left it.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. A separate AI agent, which had not written the code, read the whole change first
  and reported F1 to F13. The AI that wrote the code made the fixes this record names, ran the checks below and wrote
  this record. S01 asked in chat on 2026-11-15 that MIL-018 be finished in autonomous mode, without a decision of
  theirs.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln --configuration Release -warnaserror`, after the last fix | Build succeeded, 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | exit 0: nothing to change in the whole solution |
| `dotnet test`, NFHotel.Application.Tests | 730 passed, 0 failed. The architecture tests in it refused a first version of the fix of F7, a public method that returned a `Task` without the suffix `Async` and without a `CancellationToken`; the method is now synchronous |
| `dotnet test`, NFHotel.Api.Tests | 401 passed, 0 failed |
| `dotnet test`, NFHotel.Web.Tests | 834 passed, 0 failed |
| `dotnet test`, NFHotel.ApiClient.Tests | 118 passed, 0 failed |
| `dotnet test`, NFHotel.Infrastructure.Tests, the classes `DependencyInjectionTests`, `InMemoryLoginThrottleTests` and `JwtGuestTokenIssuerTests` | 43 passed, 0 failed |
| The race test of F1 against the code before the fix | `LookUpReceiptAsync_ManyAtTheSameTime_ReadsOnlyAsManyAsTheThrottleAllows` failed (5 reads expected, 10 made); with the fix it passes |
| `bash ops/tests/lint.sh`: the layout check, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1 and yamllint, in containers | all linters clean, exit 0 |
| `bash -n` of both scripts | no syntax error |
| The failure paths of `check-guest-login.sh`, run on 2026-11-15 | no arguments: the usage, exit 2; a site that is not an address: an `error:` line, exit 1; no email and no terminal: an `error:` line, exit 1; a site that does not answer: 11 checks FAIL, `error: 11 check(s) failed`, exit 1 |
| A search of the added lines for `var`, `#pragma`, `SuppressMessage`, `async void`, `.Result`, `.Wait()`, `catch`, `throw ex` and `DateTime` | none in the code of the milestone after F14. The one `.Result` is the property of the context of an MVC filter, and the one `.Wait()` is the latch of a thread test |
| `src/**/packages.lock.json` | unchanged |
| Not run here | the integration tests against PostgreSQL (the setup of `BookingPriceTests` gained the guest context), the end-to-end tests (`GuestBookingTests`) and the guest part of `check-e2e-stack.sh`: NF Hotel CI runs them on the pull request. The success path of `check-guest-login.sh` is task 7: it runs against the deployed VM after the merge, and the gate record of MIL-018 holds it |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | For example `GuestAccessService`, `SignInAsync`, `MyBookingPath`, `_receiptThrottle`; the interfaces `IGuestAccessService`, `IGuestTokenIssuer`, `IGuestCallContext` and `IGuestTokenSource` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | `TaskReturningMethods_InEveryLayer_EndInAsync` passes; no exception type added; one top-level type per file, named after it |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | Format clean and no suppression added; no `var` in the added lines after F14. `StaffAuthentication` keeps the `var` of its own lines (rule 1 of the coding conventions); the branch changes only its block of validation parameters |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | Nullable is on for every project. The four `!` of the two Razor forms are justified by the Razor comment above each form (the model is set before the first render); the `null!` and `default!` of the tests have their reason in the comment of the step since F15 |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | The new code under `src/` holds no disposable object (the ticket takes its bytes from the static `RandomNumberGenerator.GetBytes`); the tests dispose `HttpClient`, `HttpResponseMessage`, `HttpRequestMessage` and the latches with `using` |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None in the code; the middleware awaits with `ConfigureAwait(false)`, as the file around it does |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | The public members validate their arguments (`ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero` in `JwtGuestTokenIssuer.Issue`, `ArgumentException.ThrowIfNullOrWhiteSpace` in `ApiTokenValidation`), and the tests pass a null to each; no `catch` added |
| 8 | Public types and members have XML documentation comments | Pass | Every new public type and member has a summary; the remarks give the reasons (ADR-0047, BR-109) |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | The DTOs are positional records; the lifetimes `JwtGuestTokenIssuer.Lifetime` and `GuestSessionTickets.Lifetime` are `static readonly`, because a `TimeSpan` cannot be `const` |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | `GuestSessionDto.ExpiresAt` and every clock read are `DateTimeOffset`; `UtcDateTime` only where the token library takes a `DateTime`; no money in the change |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | Pass | The services, the client and the view models take one and pass it on. The two cookie helpers of `GuestSignInRegistration` wrap `HttpContext.SignInAsync` and `AuthenticateAsync`, which take none |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | 107 new tests, 100 of them in 16 new test files, all with three-part names. Positive, negative, boundary (the fifth and the sixth attempt, the ticket at 599 and 600 seconds), state transition (sign-in, change, cancel, sign-out), security (each kind of refusal, the four guest actions, every staff endpoint), concurrency and race (F1, F6) and regression (F1 to F3). The clocks are fakes; the races are released by a latch, not timed by a sleep. F6 names what is not tested |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | The milestone adds or changes no class of the Domain layer, the one layer the DCD draws ([RC-378], F11 of RC-266); the classes of the other layers implement no DCD |
| 14 | Each piece of knowledge is defined in one place; code is merged only where it expresses the same knowledge | Pass | After F7 the rules of a token of the API are in `ApiTokenValidation` for both schemes, and after F12 the addresses of the guest pages come from the constants that the cookie options use. The one refusal is `GuestAuthentication.Refusal`, and the throttle key of a booking is built once (`ReceiptThrottleKey`) for the sign-in and the receipt lookup |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | No project reference added. The ports are in the Application layer, their adapters in Infrastructure, the API and the client; the architecture tests pass in the Application run |
| 16 | SOLID holds | Pass | The guest sign-in is a service of its own (`GuestAccessService`) beside `BookingService`; ownership is one rule (`GuestOwnership`) that the services call; the ports are narrow (one or two members); the web application and the API depend on the interfaces and receive them. `ILoginThrottle` gained one member, which its one implementation and the test double implement |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | The new types are in the project and folder of their layer, as the repository lays them out (`UseCases/Guests`, `Ports` and `Dto` of the Application layer, `Security` of Infrastructure and of the API, `Features/Booking`, `Security` and `Common` of the web application); each test is in the test project of its layer |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `PascalCase.cs` and `.razor` files, `kebab-case.sh` scripts |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer or event in the change |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | The 22 new files under `src/` with members group them in the standard order and names, each closed by a named `#endregion`; the two positional records without a body have no member to group. The shared types it only changed keep the layout of their file, as G1 of [RC-378] reads the criterion (F13) |

## Checklist Results: QC-SH-001 (`check-guest-login.sh`, and the guest part of `check-e2e-stack.sh`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | The new script: both. The guest part of the other runs as `sh` in the curl image with `set -u` only, and the comment above it says why (every step runs, and one that fails leaves its value empty for the checks); the script itself keeps its `set -uo pipefail` |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | The options of curl, the form fields and the positional arguments are arrays; every expansion is quoted (shellcheck clean) |
| 3 | Names follow the conventions | Pass | `check-guest-login.sh`; `snake_case` functions and variables (`cookie_attributes`, `is_cancel`); `UPPER_SNAKE` constants and environment variables (`COOKIE`, `GUEST_EMAIL`) |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck v0.11.0 through `lint.sh`, and `bash -n`; no `disable`, one `source=` directive for the shared library |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `die` of `lib/common.sh` (`error:` on standard error, exit 1); bad arguments print the usage on standard error and exit 2 (run above) |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | `mktemp -d` with `trap 'rm -rf "$work"' EXIT`; the guest part uses `mktemp -d` inside a container that `--rm` removes |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | The email of the booking comes from `GUEST_EMAIL` or a prompt and reaches curl from a file (`--data-urlencode name@file`). In the guest part, the token and the account of the API scan come through `--env-file` and reach curl from files (`-H @file`, `--data @file`). The antiforgery token of a form is on the command line of curl; it is not a secret, and it is worth nothing without the cookie in the jar of the run |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | The cancellation needs `--cancel`, and the header says that it cannot be undone. Without it, a run changes only the count of failed attempts of the number (the wrong email), which the header states with the limit |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | Purpose, usage, `--cancel`, `--insecure`, `GUEST_EMAIL`, exit codes 0, 1 and 2, and the tools it needs |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | Task 7 and criterion 5 of [MIL-018], decision 6 of [ADR-0047] (the attributes of the guest cookie) |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | The failure paths ran here (the checks above). The success path is the recorded run of task 7 against the deployed VM, held by the gate record of MIL-018, and the gate is No-Go if it fails. The guest part of `check-e2e-stack.sh` runs in the end-to-end job of NF Hotel CI on the pull request |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 through `lint.sh` |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | A run without `--cancel` can be repeated; each adds one failed attempt, and the header says to wait between many runs. After `--cancel` the booking is cancelled, so a second run is refused by design |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | bash 4+, curl, grep, sed and mktemp; no GNU-only option |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | `check`, `fetch`, `post`, `hidden_value`, `input_name` and `cookie_attributes` hold the repeated steps; the cookie name, the refusal and the notice are constants; `die` and `usage` come from the shared library |

## Checklist Results: QC-YML-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 to all | Every criterion | N-A | No YAML file under `ops/` or `.github/` changed; the end-to-end job runs the changed `check-e2e-stack.sh` through the existing composite action, unchanged |

## Findings

F1 to F13 are the reading of the separate AI agent; F14 and F15 are from the checks of this record.

- **F1, major, fixed: attempts sent at the same time passed the throttle together.** The receipt lookup, which the
  guest sign-in uses, asked the throttle whether the number was blocked, read the booking, and counted a failure only
  after the read. Attempts sent at the same time all passed the check before any of them was counted, so more than
  five emails could be tried in one window. The path dates from [MIL-079]; the guest sign-in made it guard a session
  that can change a booking. Since the fix, `ILoginThrottle.TryCountAttempt` counts each attempt before the read, in
  one atomic step with the check (a compare-and-swap with `ConcurrentDictionary.TryUpdate`, which reads the window
  again when another attempt changed it), and only a success clears the count. The first form of the fix set a flag
  inside the delegates of `AddOrUpdate`; SonarCloud's rule S2583 read that flag as always false (a false positive that
  failed the quality gate of the pull request), so the method became the loop, with the same behaviour and the same
  tests. Tests: five
  in `InMemoryLoginThrottleTests` (the sixth attempt refused, a reset, a new window, 16 threads released at once in ten
  rounds, a null key) and `LookUpReceiptAsync_ManyAtTheSameTime_ReadsOnlyAsManyAsTheThrottleAllows`, which failed on
  the old code. [ADR-0047] decision 3 and [OC-001] say so now.
- **F2, minor, fixed: a cancelled booking with the right email cleared the count.** The count then told what the one
  refusal hides: whether an email was the email of a cancelled booking. Now only a booking that is not cancelled clears
  it. Tests: `LookUpReceiptAsync_RightEmailOfACancelledBooking_ReturnsTheReceiptAndKeepsTheCount` and
  `SignInAsync_RightEmailOfACancelledBooking_IsRefusedAndKeepsTheCount`; the exception of CO1 in [OC-001] says that
  the attempt stays counted.
- **F3, minor, fixed: the runner fell back to the staff token.** When a guest token was held but the host had no guest
  context to carry it, the call went with the staff token of the browser. No host is set up that way, but the fallback
  failed open. Now such a call carries no caller token: `RunAsync_HostWithoutTheGuestContext_GivesNoCallerToken`
  replaces the test that pinned the fallback.
- **F4, note: a stay that ended long ago still signs in.** Decision 7 refuses cancelled bookings only, so the number and
  the email of a booking checked out long ago open it for two hours, with the guest's own data. [ADR-0047] names it as
  a negative consequence now; whether to refuse a booking some time after its departure date is S01's decision
  (action item).
- **F5, note: the throttle counts per booking number, not per client.** That is decision 3 of [ADR-0047]. A request
  limit per client in front of the guest sign-in, for example at the edge, would also bound how many numbers one
  client can try. Left to S01 (action item).
- **F6, fixed in part: test gaps.** Added: the race of the throttle (F1); `Redeem_ManyThreadsAtOnce_GivesTheSessionOnce`
  (16 threads, one session); `GuestToken_SignedWithoutTheRoleGuestOrABooking_IsRefused`, which signs a token with the
  right key and then leaves out the role Guest or names booking 0, with a control token that opens the booking; and
  `StaffAndGuestToken_BothValid_TheStaffTokenDecides`. Not added: the expiry of a token at the API, plus and minus the
  clock skew, because the validation reads the clock of the host, not the fake clock of the tests; the lifetime is
  tested at the issuer, and an expired token is among the refusals of `Refusal_EveryKindOfFailedSignIn_...`.
- **F7, fixed (criterion 14): the rules of a token were written twice.** `GuestAuthentication` repeated the issuer, the
  key, the algorithm, the lifetime, the clock skew and the header reader of `StaffAuthentication`. Both now take them
  from `ApiTokenValidation`; the staff scheme adds its name claim. The staff tests of [MIL-016] pass unchanged.
- **F8, note: the guest read in `GuestService`.** The call of `GuestOwnership.Allows` with the booking of the token
  checks the role only; the ownership is the comparison of the guest that follows it. The guest read also decrypts the
  passport number and then clears it. Both are correct and tested; a read without the passport number would be simpler.
- **F9, note: client methods no longer called.** The web application no longer calls `LookUpReceiptAsync` and
  `CreatePublicAsync` of `ApiBookingService`; they go with the decision on the receipt endpoint, an action item of
  [RC-388].
- **F10, note: the ticket store.** A ticket that the guest leaves by starting over stays redeemable until its ten
  minutes end, and expired tickets are removed at the next issue. A ticket is 256 random bits, works once and is never
  in an address, so neither is a way in.
- **F11, fixed: the test of the matrix did not read its header row.** Two role names swapped in the header would have
  passed. `Matrix_HeaderRow_NamesTheRolesInTheOrderOfTheCells` reads it now.
- **F12, fixed (criterion 14): the addresses of the guest pages were written twice.** The pages declared `/booking` and
  `/my-booking` beside the constants of `GuestSignInRegistration`; they now take their routes from the constants. The
  link to `/booking` in the public layout is a line of [MIL-079] in a file this branch does not change.
- **F13, notes.** The shared types that the branch changed keep their layout without regions (G1 of [RC-378]).
  `EndpointProtectionTests` of [MIL-016] gained one entry, `GuestAuthController.SignInAsync`, in its list of the
  actions that need only the token of the web application, and its comment; its assertions are unchanged and pass, as
  criterion 7 of [MIL-018] asks. `HostileInputTests` gained the row of the new route. A guest who got a 403 would see
  the staff wording of `api.forbidden`; the pages never ask for another booking, so no guest meets it. The guest
  cookie lasts as long as its token, two hours, also across a restart of the browser, as decision 6 says; the page has
  a sign-out.
- **F14, fixed (criterion 3): one `var`.** The changed constructor test of `BookingFlowViewModelTests` declared its new
  local with `var`; its three locals are typed now.
- **F15, fixed (criterion 4): `null!` without a reason in the tests.** The reason is now in the comment of each step
  that passes a null to a guard or fills an argument that `DidNotReceiveWithAnyArgs` ignores, as
  `LoggerWarningLogTests` does.

## Overall Verdict

Go. Every Mandatory criterion of [QC-CS-001] and [QC-SH-001] passes after the fixes of F1 to F3, F7, F12, F14 and F15;
F6 and F11 add the tests that were missing; F8 to F10 and F13 are notes, and F4 and F5 are action items for S01.
[QC-YML-001] has nothing to review. S01 asked in chat on 2026-11-15 that MIL-018 be finished in autonomous mode, and
the AI accepted this record as stand-in for S01. Not verified here: the integration and end-to-end tests, which NF
Hotel CI runs on the pull request, and the success path of `check-guest-login.sh`, which task 7 runs against the
deployed VM. The reviewer is not independent: an AI wrote the code and this record, after the reading of a separate AI
agent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, or return it | S01; done on 2026-11-15 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-018-guest-login` |
| Read the integration and end-to-end tests in NF Hotel CI of the pull request | AI as stand-in for S01 | Before the merge |
| Decide whether a booking is refused for the guest sign-in some time after its departure date (F4) | S01 | 2026-12-31 |
| Decide whether the guest sign-in gets a request limit per client in front of it (F5) | S01 | 2026-12-31 |

---

[MIL-018]: ../../milestones/mil-018-guest-login.md
[MIL-016]: ../../milestones/mil-016-backoffice-login.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[UC-010]: ../../use-cases/uc-010/uc.md
[OC-001]: ../../use-cases/uc-010/oc.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[RC-378]: ./rc-378-mil-055-code-at-the-gate.md
[RC-388]: ./rc-388-adr-0047.md
[RC-389]: ./rc-389-uc-010.md
[RC-390]: ./rc-390-mil-018-amendments.md
[9e72b0e]: https://github.com/NF-Hotel/NFHotel/commit/9e72b0e99e8765b608373fe29391d33a2a8225ee
[0532e78]: https://github.com/NF-Hotel/NFHotel/commit/0532e7808c80425204915f0759279b548bece1e3
