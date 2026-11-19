# RC-428 Code review of MIL-087: the limits on what one sender can ask

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-428 |
| CrossReference | [MIL-087], [ADR-0051], [RC-426], [RC-427], [RC-429], [RC-412], [QC-CS-001], [QC-SH-001], [QC-YML-001], [ADR-0049], [ADR-0015], [RC-418], [RC-422], [RC-424], [RC-425], [RC-081], [MIL-089] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: code review of tasks 3 to 6 of MIL-087 and of the fixes of its findings; Go-with-conditions: every required check of the pull request green and the load test passed on the branch; mode autonomous, not independent | [a574bf6] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-087] tasks 3 to 6 on the branch `mil-087-sender-limits`, against `main` at
  84ab63f, which the branch merged in bb075b4: the tests written first on a skeleton (their failing run is
  `docs/sqa/evidence/mil-087/before-tests.txt`), the code of tasks 4 to 6 (b626fdb), and the changes of this review
  (F1 to F3, F5 to F9, F11 and F12). In all, 42 files are new and 48 changed under `src/`, `tests/` and `ops/`; none
  is removed.
  - Domain: `Booking.MadeOnPublicSite` and `MarkMadeOnPublicSite`.
  - Application: the ports `ISenderLimits`, `IPublicBookingEmailLimit` (F6), `LimitNames`, `SenderLimitDecision` and
    `IBookingQueries.CountComingPublicBookingsAsync`; `CallerAddressKey`, the key of [ADR-0049] decision 6, moved out
    of `GuestAccessService`; `BookingService`, which marks a public booking and checks the limit per email in
    `CreatePublicAsync`; the code `booking.too_many_requests`.
  - API: `ServiceCall` and `ApiTokenMiddleware`, which mark a call that showed the service token; `HttpCallerAddress`,
    which takes the address header only on such a call, and the forwarded headers of the networks of the stack with
    one entry in `Program`; `SenderLimitAttribute`, `SenderLimitKind` and `SenderLimitFilter`;
    `BookingFlowEventAttribute`, `BookingFlowStep`, `BookingFlowEventFilter` and `BookingFlowEvents`; the events 8602
    to 8604 and the masking of `SecurityEvents`; the counter of `SecurityMetrics`; the 429 with `Retry-After` of
    `ResultResponses`; the attributes on the controllers.
  - Api.Contracts: `ClientAddresses`, the networks of the stack and the one form of an address, and
    `ApiAuthentication.ClientAddressHeader`.
  - Infrastructure: `BoundedWindowStore`, now shared; `InMemorySenderLimits`; `InMemoryGuestSignInLimits` over the
    shared store; `ConfiguredPublicBookingEmailLimit` (F6); `SenderLimitOptions` and the settings with their bounds in
    `DependencyInjection`; the count of `BookingRepository` with `SqlPattern`; the column in `BookingConfiguration`,
    the migration `AddBookingMadeOnPublicSite` and the model snapshot.
  - External: `TelemetryRegistration` sends the meter `NFHotel.Security`.
  - Web: `ClientAddressMiddleware`, `ClientAddressClaims`, `IClientAddressSource` and
    `AuthenticationStateClientAddressSource`; `UseCaseRunner`, which takes the address from that source; `Program`;
    the text of `ErrorMessages`.
  - Ops: `snippets/proxy-headers.conf` and `optional/api.conf` (the header of the address), `rules/security.yml` and
    `rules-test.yml` (the two alerts), `ops/tests/check-e2e-stack.sh` (the limits and the events on the throwaway
    stack) and `ops/tests/test-nginx.sh` (the header on every proxied location).
  - Tests: the tests named under the criteria below, and `LimitsTable`, `RuleLedger` and `MigrationDatabase` in the
    shared support.
- Checklists used: [QC-CS-001] for the C# under `src/` and `tests/`, [QC-SH-001] for the two scripts, [QC-YML-001]
  for the two YAML files of the alerts. The files of nginx are held by `test-nginx.sh`. [ADR-0051] is reviewed in
  [RC-426] and, in its second version, [RC-429]; the documents of task 7 in [RC-427].
- Scope: full review of the new and changed files.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An AI
  wrote the code and this record. A second AI, which had not written the code, read the diff of 5312839 to the first
  commit of the code for bugs of correctness and security without the conclusions of this record. Its twelve points
  are F1 to F12 below, each checked against the code; each one that needed a change in behaviour got a test that
  failed before it. It found sound: nginx replaces the address header and appends `X-Forwarded-For` on every proxied
  location; the API uses the header only after `ApiTokenMiddleware` marked a matching service token, under a key that
  no caller can reach, and nginx clears `Authorization` on the mobile routes; the API trusts `X-Forwarded-For` only
  from the stack and takes its last entry, and nothing reads `X-Real-IP`; one header value, a whitelist of characters,
  IPv4 that round-trips, mapped addresses as IPv4 and IPv6 by its first 64 bits; the address of a circuit, which the
  browser test proves; the check and the count under one lock; the count given back only for 400 and 422 without an
  exception, never for a booking that was made; the arithmetic of the windows and of `Retry-After`; the logs, which
  hold only the masked email, the address in its one form and the names of the limits; the query of the count per
  email; the migration; and the checklists of C#, shell and YAML.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -c Release -warnaserror`, after the last fix | 0 Warning(s), 0 Error(s) |
| `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore`, which CI runs | exit 0 |
| `dotnet test`, the six projects, after the last fix | Domain 719, Application 790, Api 699, ApiClient 122, Infrastructure 291 (against PostgreSQL in a container of Testcontainers on Docker Desktop) and Web 917 passed, none failed: 3,538 tests (`after-tests.txt`) |
| The tests of the points of the second AI, written before their fixes | 13 failed, each on its point: Api 2 of 698, Infrastructure 5 of 279, and 1 and 5 in two runs of tests written after the first; the tests of point 12 passed at once, because the code was right (`before-review-fixes.txt`) |
| `bash ops/tests/check-e2e-stack.sh` against the throwaway stack of `ops/scripts/e2e-stack.sh`, from WSL on Docker Desktop, with the settings at their defaults | 44 passed, 0 failed (`stack-check.txt`), on the code before the fixes of this review; NF Hotel CI runs it again on the pull request |
| `bash ops/tests/test-nginx.sh` in WSL on Docker Desktop, with `REPO_HOST` the path of `ops/` | 58 passed, 0 failed, the checks of the address header among them; after it only the header comment of the script changed (F1) |
| `bash ops/tests/lint.sh`: actionlint, shellcheck, shfmt and yamllint in their pinned containers | clean |
| `bash ops/tests/check-monitoring.sh` in WSL | 7 passed, among them `promtool test rules` with the tests of both new alerts |
| `bash ops/tests/test-wiki-coverage.sh` | 126 passed, 0 failed |
| A search of the added lines for `var`, `#pragma`, `SuppressMessage`, `async void`, `.Result`, `.Wait()`, `catch`, `throw ex`, `DateTime` and `null!` | No `var`; the two `#pragma` lines are in the designer file that EF Core generates for the migration; one `catch (InvalidOperationException)` with a body and its reason in `AuthenticationStateClientAddressSource`, as the staff token source has; every `null!` in a test that passes the null its guard must refuse, with that reason in the comment of its step (F9) |
| `src/**/packages.lock.json` | unchanged |
| Not run here | the browser tests (`CallerAddressTests` and the booking tests), the baseline scan, CodeQL, the coverage floor and the weekly load test with the numbers of production: NF Hotel CI runs them on the pull request, and the load test is started by hand on the branch (criterion 7) |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `BoundedWindowStore`, `InMemorySenderLimits`, `SenderLimitFilter`, `BookingFlowEvents`, `ClientAddresses`, `CallerAddressKey`, `ConfiguredPublicBookingEmailLimit`; the interfaces `ISenderLimits`, `IPublicBookingEmailLimit`, `IClientAddressSource`; the constants `MaxLimit` and `LimitRefusedKind`; the private fields `_dropsUsedUpWindowsWhenFull` and `_emailLimit` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | `CountComingPublicBookingsAsync`, `GetAddressAsync`; `TaskReturningMethods_InEveryLayer_EndInAsync` passes; one type per file |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | The format check and the Release build with warnings as errors are clean; no `var` and no suppression in an added line; the `#pragma` of the generated designer file is the one EF Core writes |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | Nullable is on for every project. After F9 each `null!` of the tests has its reason in the comment of its step; the one `!` under `src/` has its comment |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | The stores hold no disposable object (`System.Threading.Lock` is not one); the factories, clients and answers of the tests are `using` and `await using` declarations |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None; the two filters read the result of the action with a property pattern, which `AsyncRulesTests` accepts, as in [RC-424] F5 |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | After F7 the two filters refuse a missing dependency as `BookingFlowEvents` does; the stores and the limits throw `ArgumentNullException` and `ArgumentOutOfRangeException`, and the settings `InvalidOperationException` with the name of the setting; the one `catch` above has a body and a reason |
| 8 | Public types and members have XML documentation comments | Pass | Every project generates its documentation file and builds with warnings as errors; every public type and member of the new files has its summary, and the remarks give the reasons ([ADR-0051], [ADR-0049], [RC-418] and this review) |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | `SenderLimitDecision` is a `readonly record struct`, `SenderLimitOptions` has `init` properties, the limit per email and the windows are read-only properties, and the fields of the stores are `readonly` |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | The windows run on `IClock.UtcNow`, a `DateTimeOffset`; the count per email takes today as a `DateOnly` on the clock of the hotel; no money is added |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | Pass | `CountComingPublicBookingsAsync` takes the token of the use case and passes it to the query; the filters run inside the pipeline of the request |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | Task 3 wrote the tests first and saw them fail on the skeleton (`before-tests.txt`); the limits run on the fixed clock of the world, each test of the limits at the API has a host of its own, and the stores are tested at their bounds with 100,000 addresses and 1,000 threads. The points of the second AI got 13 failing tests before their fixes (`before-review-fixes.txt`) |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | The class diagram draws the mark of a public booking on `Booking` ([RC-427]); the classes of the other layers implement no DCD, as in [RC-418] and [RC-424] |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge | Pass | One store for the guest sign-in and the booking flow (`BoundedWindowStore`), one key of an address (`CallerAddressKey`), one list of the networks of the stack and one form of an address (`ClientAddresses`) for the web application and the API, one table of the limits held by `LimitsTableTests`, one reader of a limit and one of a limit of a window (`ReadLimit`, `ReadWindowLimit`), one escape of a pattern (`SqlPattern`) and one database of the migration tests (`MigrationDatabase`) |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | The use case knows the ports in `Application/Ports`, which the infrastructure implements; the domain adds a field and a method; the structure tests pass |
| 16 | SOLID holds | Pass | After F6 the use case depends on the one number it reads (`IPublicBookingEmailLimit`), and `ISenderLimits` keeps the counts per address; the limits and the events are filters beside the actions; the rule of a full store is a parameter of the one store, set by each owner |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | `Application/Ports`, `Application/UseCases`, `Infrastructure/Security`, `Infrastructure/Config`, `Api/Security`, `Web/Security`, `Api.Contracts` |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `PascalCase.cs`, `kebab-case.conf`, `security.yml`, `rules-test.yml` |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer or event in the change |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | After F8 every new type under `src/` with members groups them in the standard order and names; the types that were only changed keep the layout of their file, as [RC-424] reads the criterion |

## Checklist Results: QC-SH-001 (`check-e2e-stack.sh` and `test-nginx.sh`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | Both keep `set -uo pipefail`, the convention of `ops/tests`, with each check counted; the scripts that run inside the containers keep their `set -u` |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | The emails of the check are an array, `within` tests with `[[ =~ ]]`, and every value is read with `$(...)`; shellcheck clean |
| 3 | Names follow the conventions | Pass | The functions `within` and `limits_value` and the variables `limits` and `emails` in snake_case |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck clean in `lint.sh`; no `disable` added |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | Unchanged: a failed check is counted and the script ends with exit 1 |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | The new checks write no file; the tokens of the stack go through its files under `umask 077`, as before. The work folder of `test-nginx.sh` is removed by its trap and is now ignored by git (F1) |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | The service token reaches the container of the check through `--env-file`, never an argument; the emails are of example.com and the addresses of the documentation ranges |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | N-A | Both change nothing outside their own containers and the throwaway stack |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | Both headers name the checks of MIL-087; after F1 the header of `test-nginx.sh` says that `REPO_HOST` is the path of `ops/` and names its work folder |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | [MIL-087] criteria 4, 6 and 8 and [ADR-0051] decision 4 in the headers |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | The 60th search answered and the 61st refused, the 5th booking request made and the 6th refused, a search of another address answered, forged address headers that do not count, and a header on every proxied location |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt with `-i 2 -ci` in `lint.sh`: no difference |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | The stack is thrown away after its run, and the emails of the check carry the room of the run; `test-nginx.sh` removes its containers and networks |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | Bash 4 (arrays, brace ranges, `[[ =~ ]]`), docker and the pinned image of curl, as the headers say |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | The requests of the limits run in one script inside one container, and `limits_value` reads each result from its output |

## Checklist Results: QC-YML-001 (`rules/security.yml` and `rules-test.yml`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys in any mapping | Pass | promtool loads both (`check config` and `test rules`), and yamllint checks for duplicate keys |
| 2 | Indentation is 2 spaces everywhere, never tabs; lines end with LF; the file ends with one newline and has no trailing spaces | Pass | yamllint clean |
| 3 | Passes `yamllint` (or the project's YAML linter) with its configuration committed next to the code and no unexplained `disable` comments | Pass | yamllint 1.35.1 with `ops/.yamllint` in `lint.sh`; no `disable` |
| 4 | A scalar that YAML would coerce to another type is quoted | Pass | The new lines hold no bare `on`, `yes`, `no` or version-like scalar; the summary that starts with a template is quoted |
| 5 | Only safe, plain types are used | Pass | No tag |
| 6 | No secret is written in the file | Pass | None |
| 7 | Embedded program text in a value is at most five lines | Pass | Each expression of PromQL is two lines |
| 8 | A block of keys or values repeated in the file or across files is defined once | Pass | Each alert once; its tests once in `rules-test.yml`, as the other alerts have theirs |
| 9 | Anchors, aliases and merge keys are used only where the consuming tool is known to support them | N-A | None |
| 10 | The file validates against the schema of the consuming tool where one exists, and the schema or tool version is stated | Pass | `promtool check config` of the pinned Prometheus image loads the rule file through `prometheus.yml` |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision or design is recorded | Pass | The header of `security.yml` says that it counts the security events of the booking flow since MIL-087, and the group of the two alerts cites [ADR-0051] decision 6 |
| 12 | Key names follow the convention of the consuming tool; one case style is used within the file | Pass | The keys of Prometheus and promtool in snake_case |
| 13 | Multi-line strings use a block scalar and state the chomping indicator where the trailing newline matters | Pass | `>-` for the expressions and the texts |
| 14 | The file name is lowercase kebab-case with one extension style used throughout the repository | Pass | `.yml`, as the other rule files |
| 15 | A change is exercised by a check that runs without the real target | Pass | `promtool test rules`: each alert fires one step over its number and stays quiet at it |

## The criteria of MIL-087 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | [ADR-0051] is Accepted after [RC-426] and, in its second version, [RC-429], both Go |
| 2 | Pass | `Table_Numbers_AreTheDefaultsOfTheirSettings`, `Table_Windows_AreTheWindowsOfTheLimits`, `Table_Names_AreTheLimitsOfTheSecurityEvents`, `Table_Refusals_Are429WithACodeOfTheApi` and `Settings_MaxTrackedKeys_BoundEachStoreAtTenThousand` pass |
| 3 | Pass | At the API on the fixed clock of the world: `Search_SixtyFirstInAMinute_IsRefusedWithTheCodeAndTheTimeToWait`, `Search_AfterTheMinute_IsAnsweredAgain`, `Search_RefusedWithPartOfASecondLeft_NamesTheWholeSecondsRoundedUp`, `Booking_SixthInAnHour_IsRefusedWithTheCodeAndTheTimeToWait`, `Booking_AfterTheHour_IsMadeAgain`, `Booking_TwentyFirstInADay_IsRefusedByTheLimitOfTheDay`, `Booking_RefusedByARuleOfTheUseCase_GetsItsCountBack`, `Booking_RefusedByTheLimitPerEmail_KeepsItsCount`, `Booking_FourthComingBookingOfOneEmail_IsRefusedWithoutTheTimeToWait` and `Booking_ByStaffForAnEmailAtItsLimit_IsMade`; the use case in `PublicBookingLimitTests`; the one text on the pages in `FindRoomsAsync_RefusedByALimit_ShowsTheTextAndCanSearchAgain` and `SubmitAsync_RefusedByALimit_KeepsTheFormAndShowsTheText` |
| 4 | Pending | At the API: `StaffSignIn_WithTheServiceToken_IsAuditedUnderTheNamedAddress`, `StaffSignIn_WithoutTheServiceToken_CannotNameTheAddress`, `StaffSignIn_ForwardedFromOutsideTheStack_IsAuditedUnderThePeer`, `StaffSignIn_ForwardedChainFromTheStack_IsAuditedUnderTheLastEntry` and `HttpCallerAddressTests`; in the web application `ClientAddressMiddlewareTests`; through nginx, the header on every location (`test-nginx.sh`) and the 21 failed guest sign-ins with forged headers counted and audited under the address that nginx saw (`stack-check.txt`). The browser test `CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw` runs in CI |
| 5 | Pass | `TryCount_HundredThousandAddresses_StaysWithinTheBound`, `TryCount_OtherAddress_IsCountedApart`, `TryCount_ManyAtTheSameTime_CountsExactlyTheLimit`, `Search_HundredAtTheSameTime_AnswersSixty` and `Search_OfAnotherAddress_IsNotStopped`; after F2 a full store of addresses at their limits still counts a new address |
| 6 | Pass | `Booking_Made_WritesOneEventWithTheAddressAndTheMaskedEmail`, `GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheReference`, `Refusals_OfEveryLimit_WriteOneEventEachWithTheName`, `Events_OfTheBookingFlow_AreCountedOnceWithTheirKindAndLimit`, `Telemetry_Meters_IncludeTheMeterOfTheSecurityEvents` and `SecurityEventValuesTests`; the check of the logs of the throwaway stack (`stack-check.txt`); `promtool test rules`; the wiki page Alerts names both alerts ([RC-427], `test-wiki-coverage.sh`) |
| 7 | Pending | The documents in [RC-427], Go; this review, Go-with-conditions; the browser tests, the coverage floor and the required checks of the pull request, and the load test on the branch |
| 8 | Pending | The limit on booking requests with the numbers of production on the throwaway stack is in `stack-check.txt`; the search on the live site and the counter come with task 8, after the merge and the CD run that S01 approves |

## Findings

- **F1, corrected while reviewing (the second AI's point 1): a private key in a commit.** The first commit of the code
  (d2d24e6, not pushed) added `tests/output/nginx/tls/privkey.pem` and `fullchain.pem`: the self-signed key and
  certificate that `test-nginx.sh` makes for its run (CN `t`, valid one day). They landed under the root of the
  repository because `REPO_HOST` was given as the root, while the script takes the path of `ops/`, so its trap removed
  the work folder that it knew and not the one that the container wrote. The second AI named the removal from the
  commit a rewrite of history, which the stop rules of `AGENTS.md` leave to S01. The AI decided it as stand-in for
  S01: nothing had left the machine, and a commit that removes the files would have pushed the key into the history
  of the repository. The AI rebuilt the two unpushed commits without the two files (b626fdb for d2d24e6, and bb075b4
  for the merge of `main`); a comparison of each pair shows those two files as the only difference. The old commits
  stay in the local reflog until it expires. The key signs nothing of NF Hotel, so it was never a secret of
  production. `.gitignore` now ignores `ops/tests/output/` and `tests/output/`, and the header of `test-nginx.sh`
  says that `REPO_HOST` is the path of `ops/`.
- **F2, corrected while reviewing (point 2): the limits could be made to refuse everyone.** A full store whose windows
  were all at their limit refused every new key. 10,000 addresses with 5 booking requests each that are not given
  back (a 409 for a room type that is not free is the cheapest) fill the store of the hour, about 14 requests a
  second for an hour, and then every new guest got 429; the store of the day stays closed up to about 20 hours more;
  one IPv6 network of 48 bits gives 65,536 keys. A store of caller addresses (the three of the booking flow and the
  one of the guest sign-in) now drops the window that runs out first; the store of references keeps the closed rule
  of [RC-418] ([RC-429] F2). Three tests failed before the change. Not done, as stand-in for S01: a coarser key of 48
  bits and a limit name of its own for a full store. With the open stores of addresses a full store refuses nobody;
  the closed store of references, whose refusals read `guest_sign_in`, goes to [MIL-089] with the action below.
- **F3, corrected in the text (point 3): the web application takes forwarded headers.** With
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` the check of the peer in `ClientAddressMiddleware` sees the last entry
  of `X-Forwarded-For`, the address that nginx saw, not nginx. The result is the same, because nginx writes both
  headers and is the only peer; the second AI offered the options of the API in the web application or the text. The
  AI chose the text as stand-in for S01 ([RC-429] F3; [RC-081] accepted the setting): the second version of
  [ADR-0051] decision 4 and the remarks of the middleware say it.
- **F4, note (point 4): a service call without a valid address falls back to the connection.** `HttpCallerAddress`
  then takes the address of the web container, so a regression of the web application would put every guest under
  one key, and a hotel-wide 429 would come without a warning. Not changed: the browser test
  `CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw` and the audit check of the throwaway stack fail on
  such a regression before a merge. An event for that case goes to [MIL-089] with the action below.
- **F5, corrected while reviewing (point 5): one sentence said more than the code.** The rule ledger said that a
  refused request counts in no window, while a request that the limit per email refuses keeps its count, which the
  second AI judged the safer choice, because it limits what a stranger can learn of an email. The ledger and
  [ADR-0051] decision 2 now say it, and `Booking_RefusedByTheLimitPerEmail_KeepsItsCount` holds it; it passed at once.
- **F6, corrected while reviewing (point 6): the use case took a port for one number** (QC-CS-001 16). `BookingService`
  took `ISenderLimits` only to read the number per email. The number now has its own port, `IPublicBookingEmailLimit`,
  which `ConfiguredPublicBookingEmailLimit` reads from the settings; `ISenderLimits` keeps the counts per address, and
  `InMemorySenderLimits` no longer holds the number. Until the port existed the tests that name it did not compile;
  the tests of the use case, the API and the composition pass after it.
- **F7, corrected while reviewing (point 7): two filters did not check their dependencies** (QC-CS-001 7).
  `SenderLimitFilter` and `BookingFlowEventFilter` now refuse a missing one; `BookingFlowFilterArgumentsTests` failed
  before.
- **F8, corrected while reviewing (point 8): `IClientAddressSource` had no region** (QC-CS-001 21).
- **F9, corrected while reviewing (point 9): two `null!` without their reason** (QC-CS-001 4), in
  `AuthenticationStateClientAddressSourceTests` and `ClientAddressMiddlewareTests`; they carry the wording of the
  other tests.
- **F10, note (point 10): the sign-in limit of the mobile app reads the connection.** `LoginAddressLimitFilter` reads
  `Connection.RemoteIpAddress`, the same value for a call of the mobile app, and counts it whole, an IPv6 address
  too. It is outside the booking flow and unchanged; the second version of [ADR-0051] records it, and its limit per
  user name still holds.
- **F11, corrected while reviewing (point 11): no bound on a limit.** The store keeps a list for every count up to its
  limit, and nothing bounded the settings: `int.MaxValue` would have overflowed at the start-up, and the 100,000 of
  the tests of the API build about 300,000 lists per host. A limit above 100,000 (`BoundedWindowStore.MaxLimit`) is
  now refused by the store, and by the start-up for the five limits of windows with the name of the setting
  (`ReadWindowLimit`); the eight tests of both failed before, and `AddInfrastructure_WindowLimitAtTheCap_Starts` holds
  the 100,000 of the tests of the API.
- **F12, corrected while reviewing (point 12): three gaps in the tests.** Every case of `Retry-After` landed on whole
  seconds, so a truncation would have passed: a theory now refuses with 59.5, 58.2 and 58.7 seconds left and expects
  1, 2 and 2. The count per email had no test of its escaped pattern or of the normal form: `a_b@example.com` now
  counts only itself among `axb` and `abcd`, `a%d` counts nothing, and an email with spaces and capitals counts as
  itself. Both passed at once. The third gap is a note: the check of the stack that sends 61 searches would also pass
  if every caller had one key, and the audit check of the same script pins the address.
- **F13, note: what else the code holds.**
  - The model snapshot caught up with the length of the passport number that MIL-086 set (50 to 20); the column is
    `text`, so the migration has no statement for it. The migration `AddBookingMadeOnPublicSite` adds one column
    (boolean, not null, default false), and its `Down` drops it (`BookingPublicSiteMigrationTests`).
  - The notes of SonarCloud on the pull request of MIL-085 (#837, [RC-422] F5) and of the code quality tools on the
    pull request of MIL-086 (#841, [RC-425] F4) are fixed here: `System.Threading.Lock` in the two stores, a primary
    constructor for `RequiresGuestAttribute`, `ObjectResult` as the type of `GuestBookingController.Answer`, one
    lookup in `ValidationRulesTableTests`, `Where` in `GuestFieldErrorsTests` and a static array in
    `GuestServiceTests`.
  - `PublicBookingLimitTests` books as staff through a service with the permission to create bookings
    (`StaffService`), so its tests show that a booking by staff is neither counted nor limited.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-CS-001], [QC-SH-001] and [QC-YML-001] passes after the fixes,
and the criteria of [MIL-087] that a test on this machine can hold pass. The conditions: every required check of the
pull request is green (the browser tests, the check of the throwaway stack, the coverage floor, CodeQL and the
baseline scan among them), and the load test passes on the branch with the numbers of production (criterion 7). The
AI accepted this record as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19. The reviewer is
not independent: an AI wrote the code and this record, and a second AI read the code. S01 may challenge any decision
of it, F1, F2 and F4 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, or return it | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-087-sender-limits` |
| Every required check of the pull request green, and the load test passed on the branch | mikemike | Before the merge |
| The deploy check and the gate (task 8) | mikemike; the CD run is approved by S01 | After the merge |
| Ask of every limit what a flood of addresses does to it; an alert for failed guest sign-ins, a name of its own for a full store of references, and an event for a service call without a valid address (F2, F4) | mikemike | With the ASVS review and the abuse tests of [MIL-089] |

---

[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[RC-426]: ./rc-426-adr-0051.md
[RC-427]: ./rc-427-mil-087-documents.md
[RC-429]: ./rc-429-adr-0051-second-version.md
[RC-412]: ./rc-412-mil-087.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[RC-418]: ./rc-418-mil-085-code.md
[RC-422]: ./rc-422-mil-085-gate.md
[RC-424]: ./rc-424-mil-086-code.md
[RC-425]: ./rc-425-mil-086-gate.md
[RC-081]: ./rc-081-tls-code.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
