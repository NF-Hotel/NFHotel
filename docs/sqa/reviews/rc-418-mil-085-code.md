# RC-418 Code review of MIL-085: the booking reference in every layer

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-418 |
| CrossReference | [MIL-085], [ADR-0049], [RC-416], [RC-417], [RC-419], [QC-CS-001], [QC-SH-001], [ADR-0047], [ADR-0005], [ADR-0015], [RC-391], [MIL-077], [MIL-087], [MIL-018] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: code review of tasks 2 to 4 of MIL-085 and of the fixes of its findings; Go-with-conditions: every required check of the pull request green; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-085] tasks 2 to 4 on the branch `mil-085-booking-reference`, against `main` at
  04c0a87: the tests written first (1a34d55, with the failing build in `docs/sqa/evidence/mil-085/before-tests.txt`),
  the code (87bb891), and the changes of this review (F1 to F11). The merge of `main` at 46b29fb brought nothing under
  `src/` or `tests/`. In all, 23 files are new, 95 changed and 3 removed under `src/`, `tests/` and `ops/`.
  - Domain: `BookingReference` (the form, the parser and the generator), `Booking.Reference` set by `Booking.Create`,
    and `BookingStatusExtensions.IsOpen`.
  - Application: the ports `IGuestSignInLimits` and `ICallerAddress` and the read by reference of `IBookingQueries`;
    `GuestSignInRequest`, `GuestRescheduleRequest`, `GuestSessionDto` (the token and its end), `PublicBookingDto` (the
    receipt and the session) and `BookingReceiptDto` (the reference, the dates, the nights, the room size and the
    status); `GuestAccessService`, the one lookup, with the key of a caller address; `GuestBookingService`, the own
    booking without a key; `IPublicBookingService`; and `BookingService` without the receipt lookup and the old number.
  - Infrastructure: the migration `20261119062812_AddBookingReference`, the column in `BookingConfiguration`, the read
    by reference and the search in the repository, `InMemoryGuestSignInLimits` with `GuestLookupOptions`,
    `UnknownCallerAddress` and the registration.
  - API: `GuestBookingController` with `RequiresGuestAttribute`, `HttpCallerAddress`, `RequiresPermissionAttribute`
    without the guest permissions, `GuestAuthController` with the reference, `BookingsController` without the receipt
    lookup, `GuestsController` for staff only, and `ApiRoutes`.
  - API client: `ApiGuestBookingService`, `ApiGuestAccessService`, and `ApiBookingService` without the lookup.
  - Web: the guest cookie with the token only (`GuestClaims`), the redirect of an old address
    (`GuestSignInRegistration`), the sign-in form, `/my-booking` with the receipt (`MyBookingViewModel`), the receipt
    card, the footer of the public pages, the booking page, the list, the form and the dashboard of the back office
    with the reference, and the texts of `ErrorMessages`; `BookingNumberText` removed.
  - Ops: `ops/scripts/check-guest-login.sh` and the guest smoke test of `ops/tests/check-e2e-stack.sh`.
- Checklists used: [QC-CS-001] for the C# under `src/` and `tests/`, [QC-SH-001] for the two scripts. [ADR-0049] is
  reviewed in [RC-416] and, after its amendment by this review, in [RC-419]; the documents of task 5 in [RC-417].
- Scope: full review of the new and changed files.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An AI
  wrote the code and this record. A second AI, which had not written the code, read the diff of 04c0a87 to 87bb891 for
  bugs of correctness and security without the conclusions of this record; F4 to F11 and F13 are its points, each
  checked against the code before it was fixed or accepted.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -c Release -warnaserror`, after the last fix | 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes --no-restore`: whitespace, style and analyzers; CI runs the whitespace part | exit 0, after F1 and F3 |
| `dotnet test`, NFHotel.Domain.Tests, Application.Tests, Api.Tests, ApiClient.Tests, Web.Tests and External.Tests | 516, 746, 426, 122, 838 and 217 passed; none failed |
| `dotnet test`, NFHotel.Infrastructure.Tests without PostgreSQL: the namespace `Security` and the class `DependencyInjectionTests` | 69 and 27 passed; none failed |
| The tests of this review against the code before its fixes, and after them | before: 9 failed, 4 of `InMemoryGuestSignInLimitsTests` (F4), 3 cases of the address key (F5), 1 of the search (F8) and 1 of the sign-in page (F9) (`docs/sqa/evidence/mil-085/review-tests-before.txt`); after: every one passes (`review-tests-after.txt`) |
| Every test with coverage, as the build job of NF Hotel CI runs them, in the image `mcr.microsoft.com/dotnet/sdk:10.0.400` on the Docker engine of WSL, at 87bb891 | 3,103 tests: 2,975 passed, 15 skipped, 113 failed. All 113 are in `NFHotel.Infrastructure.Tests.Integration.Persistence`, and each failed because its PostgreSQL container refused the connection on that engine; coverage 92.27 % in total and 57.21 % for NFHotel.Infrastructure without them (F12) |
| shellcheck v0.11.0 with `-x`, shfmt v3.14.1 with `-i 2 -ci -d` and `bash -n`, over both scripts, in containers | clean, after the last change of the check |
| The failure paths of `check-guest-login.sh`, run on 2026-11-19 | no arguments or an unknown option: the usage, exit 2; a site that is no address, an old number in place of a reference, or no email and no terminal: an `error:` line on standard error, exit 1; a site that does not answer (a closed port): 14 checks FAIL, `error: 14 check(s) failed`, exit 1 |
| `bash ops/tests/test-wiki-coverage.sh` | 124 passed, 0 failed |
| A search of the added lines for `var`, `#pragma`, `SuppressMessage`, `async void`, `.Result`, `.Wait()`, `catch`, `throw ex` and `DateTime` | none in the code after F1. The `.Result` lines set the result of the context of an MVC filter, the one `.Wait()` is the latch of a thread test, and the one `#pragma` is in the designer file of the migration, which Entity Framework generates with it, as every earlier designer |
| `src/**/packages.lock.json` | unchanged; a build on this machine rewrites the lock file of NFHotel.Web, which was restored after each build |
| Not run here | the tests on PostgreSQL (`BookingReferenceMigrationTests`, `BookingReadTests`, `BookingPriceTests` and the rest of their namespace), the end-to-end tests (`GuestBookingTests`) and the guest smoke test of `check-e2e-stack.sh`: NF Hotel CI runs them on the pull request (F12). The success path of `check-guest-login.sh` is task 6, against the live site after the deploy |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `BookingReference`, `GuestBookingController`, `RequiresGuestAttribute`, `HttpCallerAddress`, `InMemoryGuestSignInLimits`, `GuestLookupOptions`, `AddressKey`; private fields such as `_windows`, `_byFailures`, `_limits`, `_clock`; interfaces `IGuestSignInLimits`, `ICallerAddress`, `IPublicBookingService` and `IGuestBookingService` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | `SignInAsync`, `BookAsync`, `GetAsync`, `CancelAsync`, `RescheduleAsync`, `GetReadModelByReferenceAsync`; `TaskReturningMethods_InEveryLayer_EndInAsync` passes. One type per file; `BookingNumberText.cs`, `BookingReceiptRequest.cs` and `CountingLoginThrottle.cs` went with their types |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | After F1 and F3: `dotnet format --verify-no-changes` clean, the Release build with warnings as errors clean, no suppression added, and no `var` in an added line. The older lines of a changed file keep theirs, as rule 1 of the coding conventions says |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | Nullable is on for every project. No `!` in the added lines under `src/`; each `null!` and `default!` of the added tests has its reason in the comment of its step or block, after F1 |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | The new code under `src/` holds no disposable object; the latch of the thread test is a `using` declaration |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None in the code; see the search above |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | `ArgumentNullException.ThrowIfNull` in the constructors and public methods of the new types; a guest setting below one stops the start with an `InvalidOperationException` that names the key; no `catch` added |
| 8 | Public types and members have XML documentation comments | Pass | Every public type and member of the new files has its summary, and the remarks give the reasons (ADR-0049, RFC 4291 and RFC 8981 for the key of an IPv6 caller); the members of the two private nested types of `InMemoryGuestSignInLimits` are not public and are documented too |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | Records for the requests and answers, `init` on `GuestLookupOptions`, `readonly` fields; `Booking.Reference` has a private setter |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | The windows of the limits are `DateTimeOffset` read from the port `IClock`; no money is touched |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | Pass | Every new public async method takes one and passes it on |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | Task 2 wrote the tests first and saw them fail (36 compiler errors in `before-tests.txt`); this review added 17 test methods (F4 to F9 and F11) and saw the 9 cases of new behaviour fail before the fixes. Each has its three parts. The limits run on a `FakeClock`, the thread test on a latch, and the end-to-end tests only against the throwaway stack. The tests on PostgreSQL run in CI only (F12) |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | The DCD draws the Domain layer. After the correction recorded in [RC-417] F7 it draws `Booking` with its reference and the static class `BookingReference` with its length, generator, form and parser; the classes of the other layers implement no DCD, as in [RC-391] |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge | Pass | The form of the reference in `BookingReference` (parser and formatter); the limits in `GuestLookupOptions`; the key of a caller address in `GuestAccessService.AddressKey`; the one refusal in `GuestAuthentication.Refusal()`; the routes in `ApiRoutes`; the texts of the pages in `ErrorMessages` and the components |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | `BookingReference` uses the base library only; the ports are in the application; `HttpCallerAddress` (API) and `UnknownCallerAddress` (Infrastructure, the default outside a request) implement `ICallerAddress`; the key of an address uses `System.Net` of the base library |
| 16 | SOLID holds | Pass | The guest routes are a controller with a filter of their own, and the staff filter lost its branch for guests; `IGuestSignInLimits` has two methods; the store of one kind of key is a nested class used twice; the caller address is a port, so [MIL-087] replaces one implementation |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | The existing projects and folders |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `PascalCase.cs`, the generated name of the migration, and `kebab-case.sh` |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer or event in the change |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | After F2: every new file under `src/` with members groups them in the standard order and names, each closed by a named `#endregion`, and the nested types of `InMemoryGuestSignInLimits` are in its region `Nested types`; the two positional records without a body (`GuestSignInRequest`, `GuestRescheduleRequest`) have no member to group. The types that were only changed keep the layout of their file, as [RC-391] reads the criterion |

## Checklist Results: QC-SH-001 (`check-guest-login.sh`, and the guest smoke test of `check-e2e-stack.sh`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | `check-guest-login.sh` keeps both. The guest smoke test runs as `sh` in the curl image with `set -u` only, and the comment above it says why; `check-e2e-stack.sh` keeps its `set -uo pipefail`, the convention of `ops/tests`, with each check counted |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | The arguments and the options of curl are arrays, every expansion is quoted, and the form of a reference is tested with `[[ =~ ]]`; shellcheck clean |
| 3 | Names follow the conventions | Pass | `kebab-case.sh` files; snake_case functions (`check`, `fetch`, `post`, `hidden_value`, `input_name` and `cookie_attributes` in the check; `token_of`, `reference_of`, `book`, `sign_in`, `status` and `guest_value` in the smoke test) and variables; the constants `COOKIE`, `REFUSAL`, `CANCELLED`, `OLD_NUMBER` and `REFERENCE_FORM` read-only in UPPER_SNAKE |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck v0.11.0 with `-x` over both files and `bash -n`: clean; no `disable` added |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | An unknown option or a wrong number of arguments prints the usage on standard error and exits 2; a site that is no address, a reference that is not in the form of a reference, or no email ends with `error:` from `die` of `lib/common.sh`, exit 1; the header names all three cases since this review |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | The cookie jar and the emails are in a `mktemp -d` folder that a trap removes; the smoke test uses `mktemp -d` inside its container |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | The email of the booking comes from `GUEST_EMAIL` or a prompt and reaches curl from a file; the secrets of the smoke test reach its container with `--env-file`, and its curl reads them from files. Neither script prints an email, and both check that the own booking shows none |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | `--cancel` cancels the booking and says that it cannot be undone; without it the booking stays. The failed tries of each run count against the caller's address, which the header says |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | Purpose, usage with the booking reference, `--cancel`, `--insecure`, `GUEST_EMAIL`, the exit codes 0, 1 and 2 with every case of 1, and the tools |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | [MIL-085] task 6 and [ADR-0049] in the header of the check; the smoke test names [ADR-0049] and [MIL-018] |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pending | The failure paths ran here (the checks above). The smoke test runs on the throwaway stack in the end-to-end job of CI, and the success path of the check is task 6 on the live site after the deploy |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 with `-i 2 -ci`: no difference |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | The check books nothing. A run without `--cancel` can be repeated: each makes one failed try for the reference, which the right email then clears, and two for the address, so ten runs within 15 minutes use up the tries of the address; the header says to wait between many runs. The smoke test makes a Suite of its own with a random number each run |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | "bash 4+, curl, grep, sed, mktemp"; no GNU-only option |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | The old number, the form of a reference and the text of the refusal are constants of the check, and `check`, `fetch`, `post`, `hidden_value`, `input_name` and `cookie_attributes` hold its repeated steps. The smoke test checks that the answers of the API hold version 4 references in lower case, a stricter rule than the form that the check asks of its argument |

## The criteria of MIL-085 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | [ADR-0049] is Accepted after [RC-416], Go, and its amendment after this review after [RC-419], Go |
| 2 | Pending | `New_TenThousandInARow_AreAllDifferentVersion4UuidsWithTheRfcVariant` and `Create_NewBooking_HasAVersion4Reference` pass, and `BookingReference.New` names `Guid.NewGuid`. The tests on PostgreSQL in `BookingReferenceMigrationTests` (the column and its unique index; one column and one index only; fifty bookings, each with its own version 4 reference; an insert by the release before, which gets its reference from the database; a taken reference refused; the migration down and up again) run in CI (F12) |
| 3 | Pending | `GuestEndpoints_TheThreeGuestRoutes_AreTheOnlyOnesOpenToAGuestAndTakeNoKey`, `GuestAnswers_GuestWithAPassportNumber_HoldTheReceiptAndNothingMore`, the page tests of the receipt, the sign-in, the footer, `/my-booking` and the back office pass; the browser test runs in CI |
| 4 | Pass | `Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail` (status and body equal for an old number, an unknown and an unreadable reference, a wrong email and a cancelled booking), `SignIn_RightReferenceAndEmailInAnotherCase_ReturnsASessionOfTwoHours`, `SignIn_SixthTryForOneReference_ReturnsTooManyRequests`, `SignIn_TwentyFirstFailedTryFromOneAddress_ReturnsTooManyRequests` and `TryCount_HundredThousandKeys_KeepsEachStoreWithinItsBound` pass. A reference with a control or direction character, or one that is no text, gets the refusal of hostile input before the sign-in reads it (F6) |
| 5 | Pass | `GuestAnswers_GuestWithAPassportNumber_HoldTheReceiptAndNothingMore` and `PublicBooking_NoToken_ReturnsTheReceiptWithAReferenceAndASessionThatOpensIt` hold the fields of decision 7; `StaffBookingRoute_GuestToken_ReturnsForbidden` gives 403 for any booking on the routes of staff, and the guest routes take no key at all |
| 6 | Pending | `GuestBookingTests` (book, the reference in UUID form, find with reference and email, sign out, sign in, cancel; an old link and an old number refused) run in the end-to-end job of CI |
| 7 | Pending | The documents in [RC-417], Go; this review, Go-with-conditions; the coverage floor and the required checks of the pull request (F12) |
| 8 | Pending | Task 6, after the merge and the CD run that S01 approves |

## Findings

- **F1, corrected while reviewing: `var` in added lines, and three `null!` without their reason.** Four added lines
  of `src/NFHotel.Infrastructure/DependencyInjection.cs` (the reading of the guest settings) and 40 added or changed
  lines in nine test files (`BookingReadProtectionTests`, `ContractParityTests`, `BookingMappingTests`,
  `BookingStateMachineTests`, `BookingValidationTests`, `GuestBookingTests`, `DependencyInjectionTests`,
  `BookingReadTests` and `ServiceWorld`) used `var` (QC-CS-001 3); they now name their types. The older lines of those
  files keep theirs, as rule 1 of the coding conventions says. The `null!` of the new `HttpCallerAddressTests` and the
  two new `null!` of the scenario table of `ContractParityTests` now have their reason in a comment (QC-CS-001 4), as
  [RC-391] F15 asked of the tests of MIL-018.
- **F2, corrected while reviewing: seven new files without regions.** `BookingReference`, `GuestLookupOptions`,
  `UnknownCallerAddress`, `RequiresGuestAttribute`, `ICallerAddress`, `IGuestSignInLimits` and `IPublicBookingService`
  now group their members in the standard regions (QC-CS-001 21), as the interfaces of MIL-018 do.
- **F3, corrected while reviewing: five usings that the removals left without use.** Two in `BookingPriceTests` and
  one each in `GuestAccessServiceTests`, `BookingServiceTests` and `BookingReadProtectionTests` named namespaces whose
  types MIL-085 removed or moved. `dotnet format --verify-no-changes` with its analyzers (IDE0005) found them; CI runs
  the whitespace part only, so its build stayed green. They are removed.
- **F4, security, fixed: a flood could free a reference whose tries were used up.** When a store was full, it dropped
  the window that had started first, whatever its count. The scenario of the second reviewer: a caller who holds a
  reference tries five emails, so the reference is refused; then the caller sends about 10,000 tries, each with a new
  random reference and a new first entry of `X-Forwarded-For`, which a caller can write until [MIL-087] (F13). The
  window of the used-up reference is then the oldest that has not run out, it is dropped, and five more tries follow,
  again and again. A test of the store asserted that drop. The store now keeps its windows in the order of their start
  and in lists by their failed tries: it drops the windows that have run out first, then the first window of the
  lowest list below the limit, never a window at its limit, and it refuses the try of a new key when every window is at
  its limit, so the sign-in fails closed. `TryCount_FullStoreAndAFloodOfNewReferences_KeepsAReferenceAtItsLimitLocked`,
  `TryCount_FullStore_DropsTheWindowWithTheFewestFailuresFirst`,
  `TryCount_FullStoreOfReferencesAtTheirLimit_RefusesANewReferenceAndCountsNothing` and
  `TryCount_FullStoreAndAFloodFromNewAddresses_KeepsAnAddressAtItsLimitLocked` failed on the code before the fix and
  pass after it; the old test is replaced. Decision 6 and the consequences of [ADR-0049] say it since its amendment
  ([RC-419]), with the cost: a store full of windows at their limit refuses new keys for up to 15 minutes, after at
  least 50,000 failed tries.
- **F5, security, fixed: an IPv6 caller could change its key for each try.** The key of the limit per address was the
  whole address. The last 64 bits of an IPv6 address are its interface identifier (RFC 4291 section 2.5.1), which a
  host forms itself and may change at will (RFC 8981), and Cloudflare passes the IPv6 address of a client on. So the
  20 tries per address were 20 tries per address of a host that holds a /64, and the flood of F4 needed no written
  header. `GuestAccessService.AddressKey` now counts an IPv6 caller by its first 64 bits and an IPv4 address in IPv6
  form as the IPv4 address; the theory `SignInAsync_CallerAddressInAnyForm_IsCountedUnderTheKeyOfItsCaller` failed for
  its three IPv6 cases before the fix and passes for all six after it. [MIL-087] keeps the key for its own limits per
  address ([RC-419], action item).
- **F6, deviation, documented and tested: hostile input gets the refusal of hostile input.** A reference with a
  control or bidirectional character (U+0000, U+202E), or a JSON value that is no text, gets 400 with
  `api.invalid_request` from the input rules of every request ([MIL-077]) before the sign-in runs, not the 401 of
  [ADR-0049] decision 2, and no try is counted. The answer names no booking; it is the answer to such input on every
  route. The existing test sent the three characters `%00`, not U+0000.
  `SignIn_ReferenceWithAControlOrDirectionCharacter_GetsTheRefusalOfHostileInputAndCountsNothing` and
  `SignIn_ReferenceThatIsNoText_GetsTheRefusalOfHostileInputAndCountsNothing` now hold it, and decision 2 says it. The
  AI, as stand-in for S01, reads "an unreadable one" of criterion 4 as a reference that the sign-in reads; a request
  that the input rules refuse never reaches it. They passed on the code before: the behaviour was there; the tests keep
  it.
- **F7, test gap, closed: the address of the browser on the way to the API.** No test showed that the web application
  forwards the address of the browser on the guest sign-in. If that regressed, every guest would share the address of
  the web container, and 20 failed tries would lock every guest out. `SignInAsync_WithTheAddressOfTheBrowser_ForwardsIt`
  (the header on the anonymous post of `ApiGuestAccessService`), `RunAsync_RequestWithForwardedAddresses_GivesTheFirstToTheCall`
  and `RunAsync_NoForwardedAddressOrNoRequest_GivesTheConnectionOrNothing` (the address that `UseCaseRunner` gives the
  call) now hold it. They passed on the code before: the code was right; the tests keep it.
- **F8, bug (low), fixed: a reference with spaces around found nothing in the back office.** `BookingListViewModel`
  passed the search text as typed, so a reference pasted with a space or a line end matched no booking. It now passes
  the trimmed text; `SearchAsync_TextWithSpacesAround_SendsTheTrimmedText` failed before the fix. Note: the list
  searches within the period it shows, as it did before MIL-085, so a booking outside that period is found after the
  period is widened.
- **F9, bug (low), fixed: the footer of the public pages still said "with its number".** The footer, on every public
  page and on `/booking` too, now says "with its booking reference and your email".
  `SignInPage_Opened_AsksForTheReferenceAndTheEmail` also looks for "its number" now and failed before the fix, and
  `check-guest-login.sh` looks for it on the live sign-in page beside "booking number" and `FLZ-`.
- **F10, wording, fixed: the time of the answer.** Decision 2 said that the cost of the answer does not tell whether a
  reference exists. A reference that exists costs the read of one booking with its guest and room, and
  `CryptographicOperations.FixedTimeEquals` returns at once for two emails of different lengths. Both tell a caller
  only about a reference that the caller holds, within five tries in 15 minutes; with 122 random bits a reference is
  not found by trying. Decision 2 now says what the code does ([RC-419]); the code is unchanged.
- **F11, test gaps, closed: the caller address and the give-back.** `HttpCallerAddress` had no test of its own, the
  key of an address none of its forms, and the give-back none for a window of the address that runs out between the
  try and the success. `HttpCallerAddressTests` (five tests, nine cases: the first forwarded address, the address of
  the connection without one, no request or no address, the current request, no accessor), the theory of F5 and
  `Succeeded_AfterTheWindowOfTheAddressRanOut_GivesNothingBackAndTheNextTryStartsANewWindow` now cover them.
- **F12, note: the tests on PostgreSQL and in the browser run in CI only.** The 113 tests on PostgreSQL could not reach
  their container on the Docker engine of WSL (the connection was refused), as in earlier runs on this machine, and the
  end-to-end tests need the throwaway stack. NF Hotel CI runs both on the pull request, and the coverage floor is read
  there, because 57.21 % for NFHotel.Infrastructure here is the share without them. This is the condition of the
  verdict.
- **F13, note: the address that a caller can write, until MIL-087.** The limit per address counts the first entry of
  `X-Forwarded-For`, which nginx passes on with the entries that the caller wrote. A caller can dodge the limit per
  address, and can use up the 20 tries of another address, such as that of a guest or an office, for 15 minutes. The
  limit per reference holds regardless, since F4. [MIL-087] makes the address the one that nginx saw; [ADR-0049] names
  the risk among its negative consequences, and it is accepted until then.

The second reviewer also checked, and found no problem in: the parser of the reference (version, variant, form, any
letter case, spaces around); the count before the read, atomic under one lock, with the refused try counting nothing
and the boundary of the window; the one refusal, also for a cancelled booking; the matrix of 401 and 403 of the guest
and staff filters and the audiences of the tokens; the guest routes, which take no key and answer with the receipt
alone; the registration of the caller address; the migration (each row its own value, the unique index, nothing but
additions, `Down`); the guest cookie with the token only, `/my-booking` and the redirect of an old address; the removal
of the receipt lookup; no key and no `FLZ-` in a public or guest answer; and both scripts.

## Overall Verdict

Go-with-conditions, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of
[QC-CS-001] passes, and every one of [QC-SH-001] but criterion 11, which waits for the end-to-end job of CI and for
task 6. The two security findings (F4, F5) are fixed and tested, and [ADR-0049] says what the code does after its
amendment ([RC-419]). The condition: every required check of the pull request is green before the merge, which runs
the tests on PostgreSQL and in the browser and reads the coverage floor (F12). The reviewer is not independent; a
second AI read the code for bugs, and each stand-in decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Read the summary of the run and challenge any stand-in decision: the sign-in that fails closed under a large flood (F4, [RC-419] F2), the reading of criterion 4 in F6, this review | S01 | When S01 reads the summary |
| The commits, the commit links resolved, and the pull request that closes the issues of tasks 1 to 5, one per line | The AI | 2026-11-19 |
| Merge only with every required check of the pull request green (F12) | The AI, as stand-in for S01 | At the merge |
| Approve the CD run that the merge starts | S01 | After the merge |
| Count an IPv6 caller by its first 64 bits in every limit per address of [MIL-087], and make the address the one that nginx saw (F5, F13) | The AI | With [MIL-087] |
| Task 6 of [MIL-085]: the check on the live site and the gate | The AI | After the CD run |

---

[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[RC-416]: ./rc-416-adr-0049.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-419]: ./rc-419-adr-0049-after-the-code-review.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[RC-391]: ./rc-391-mil-018-code.md
[MIL-077]: ../../milestones/mil-077-hostile-input-against-the-api.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-018]: ../../milestones/mil-018-guest-login.md
