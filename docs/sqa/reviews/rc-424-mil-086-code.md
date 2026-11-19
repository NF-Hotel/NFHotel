# RC-424 Code review of MIL-086: one set of rules for the booking input

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-424 |
| CrossReference | [MIL-086], [ADR-0050], [RC-420], [RC-421], [RC-423], [RC-411], [QC-CS-001], [QC-SH-001], [QC-YML-001], [ADR-0001], [ADR-0049], [MIL-077], [MIL-087], [RC-418] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: code review of tasks 3 to 6 of MIL-086 and of the fixes of its findings; Go-with-conditions: every required check of the pull request green; mode autonomous, not independent | [7de27c7] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-086] tasks 3 to 6 on the branch `mil-086-validation-rules`, against `main` at
  bac28ba: the tests written first on a skeleton (bd062cc, with the failing run in
  `docs/sqa/evidence/mil-086/before-tests.txt`), the rules in the domain and the API (ce0842a), the rules on the pages
  (a4079c1), the size and time limits in nginx (6f4ef76), and the changes of this review (F1 to F4 and F8 to F13).
  The merge of `main` at 7e34eca brought only the edge-comparison scripts of MIL-084 under `ops/`, which are not part
  of this review. In all, 28 files are new and 71 changed under `src/`, `tests/` and `ops/`; none is removed.
  - Domain: the attributes of the guest details in `Domain/Attributes` (`PersonNameAttribute`, `EmailFormatAttribute`,
    `PhoneNumberAttribute`, `CountryNameAttribute`, `PassportNumberAttribute`), each with the static `Accepts` that the
    pages call; the value objects `TextNormalForm`, `Countries` and `IsoCountry`; `Guest`, which brings its details to
    normal form C and carries the attributes; `Booking` with the limits of a stay (`LatestDeparture`,
    `LatestPublicDeparture`, `CheckPeriod`, `CheckPublicNights`, `CheckPublicParty`); the codes of `BookingErrors` and
    `GuestErrors`.
  - Application: the request types of the flow with their lengths, a room size that is required, and no `Range` of
    their own; the codes of `ErrorCodes`; `BookingService` with the five years when a booking is made, moved or
    searched for, the 30 nights and the party on the public site, and the search text in normal form;
    `GuestBookingService` with the 30 nights of a move by a guest; `GuestAccessService`, which compares the email in
    normal form; `GuestService`, which splits the search in normal form.
  - API: `InputRules` with the 1,000 characters of a text without a rule of its own; `InputGuardFilter`, which passes
    the parameter to it; `RequestBodyLimit` (64 KB, the benchmark intake 30,000,000 bytes) and the answer 413 with
    `api.request_too_large`; `SecurityEvents` and `InputRefusalEventFilter`, which write `input_refused` without the
    value; the registration in `Program`.
  - Infrastructure: the three messages of `DependencyInjection` and `UnitOfWork` in English.
  - Web: `StayDates` and `DateInput`; the texts of `ErrorMessages`; `BookingFlowViewModel`, `MyBookingViewModel` and
    `BookingFormViewModel` with the earliest and latest days and the checks before a request; the pages `Book`,
    `GuestSignIn`, `MyBooking`, `BookingList` and `GuestList` with `min`, `max` and `maxlength` and the country as a
    list.
  - Ops: `snippets/proxy-headers.conf` (new), `snippets/proxy.conf` with 60 seconds, `snippets/blazor.conf` (new) with
    an hour for `/_blazor`, the two templates, `optional/api.conf` (a comment), `ops/tests/test-nginx.sh` and the
    header of `ops/scripts/check-guest-login.sh`.
  - Tests: `HostileStrings` and `ValidationRulesTable` in the shared test support, and the tests of the domain, the
    application, the API, the pages, the database and the browser named under the criteria below.
- Checklists used: [QC-CS-001] for the C# and Razor under `src/` and `tests/`, [QC-SH-001] for the two scripts.
  [QC-YML-001] does not apply: no YAML file changes; the files of nginx are held by `test-nginx.sh`. [ADR-0050] is
  reviewed in [RC-420] and [RC-421], the documents of task 7 in [RC-423].
- Scope: full review of the new and changed files.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An AI
  wrote the code and this record. A second AI, which had not written the code, read the diff of bac28ba to 7e34eca for
  bugs of correctness and security without the conclusions of this record. Its five points are F8 to F12, each
  checked against the code and given a test that failed before its fix, and its two stale comments are F13. It found
  sound: the body limit and the timeouts of nginx with the hour of `/_blazor` in both servers; the rules of the dates
  on the page and at the API, on the same clock, with the routes of staff behind a permission; the normal form before
  the rules, with no stored guest rewritten; the 249 countries, unique and in normal form C; the 1,000 characters,
  which break no endpoint; and the refusal of an unknown field with the room size required.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -c Release -warnaserror`, after the last fix | 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes --no-restore` (whitespace, style and analyzers), and its whitespace part, which CI runs | exit 0 for both, after F1 and F2 |
| `dotnet test`, NFHotel.Domain.Tests, Application.Tests, Api.Tests, ApiClient.Tests and Web.Tests, after the last fix | 717, 782, 613, 122 and 881 passed, none failed (`after-review.txt`) |
| `dotnet test`, NFHotel.Infrastructure.Tests against PostgreSQL in a container of Testcontainers on Docker Desktop | 243 passed, none failed |
| The tests of F8 to F12, written before their fixes | 20 failed, each on its point: Domain 1 of 717, Application 1 of 782, Api 8 of 613, Web 5 of 881, Infrastructure 1 of 243; the two tests on Kestrel (F11) passed at once (`before-review-fixes.txt`) |
| The tests of the body limit with the limit broken, one break at a time, each undone (F11) | Without the `RequestSizeLimit` of the intake both tests of the intake fail with 413. The 64 KB of the server are held twice, by the limit of Kestrel for every request and by the limit that the middleware sets per endpoint: with either left the chunked body is refused, and with both removed it is read and the test fails. The three files are identical to their copies afterwards (`mutation-body-limit.txt`) |
| `bash ops/tests/test-nginx.sh` in WSL on Docker Desktop | 51 passed, 0 failed, the seven checks of MIL-086 among them |
| `bash ops/tests/lint.sh`: actionlint, shellcheck v0.11.0, shfmt v3.14.1 with `-i 2 -ci` and yamllint 1.35.1 in their pinned containers | clean |
| A search of the added lines for `var`, `#pragma`, `SuppressMessage`, `async void`, `.Result`, `.Wait()`, `catch`, `throw ex` and `DateTime` | 247 lines with `var` before F1 and none after it. Of the rest only three `catch` blocks: two `catch (DomainException)`, where `GuestBookingService.PublicNightsError` leaves a period that cannot be built to the move, which answers its own code, and the test of the stored hostile strings records a string that a rule refuses; and the `catch (ArgumentException)` of `TextNormalForm` (F9), which leaves a text that the runtime cannot normalise to the rule of its field |
| `src/**/packages.lock.json` | unchanged; a build on this machine rewrites the lock file of NFHotel.Web, which was restored after each build |
| Not run here | the browser tests (`GuestBookingTests`, with the country chosen from the list), the baseline scan of the pull request, CodeQL and the coverage floor: NF Hotel CI runs them on the pull request |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `PersonNameAttribute`, `TextNormalForm`, `Countries`, `RequestBodyLimit`, `SecurityEvents`, `InputRefusalEventFilter`, `StayDates`; the constants `MaxPublicNights`, `DefaultMaxBytes`, `MaxLocalPartLength`; the static read-only fields `Entries` and `Names`; the private field `_logger`. After F4 the two static fields of `BookingInputApiTests` are `_daysUsed` and `_address`; `s_nextAddress` of MIL-085 is an older line |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | `RefuseAsync`, `PostOnSharedClientAsync`; `TaskReturningMethods_InEveryLayer_EndInAsync` passes; one type per file |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | After F1: no `var` in an added line, the format check clean, the Release build with warnings as errors clean, and no suppression added. The older lines of a changed file keep theirs, as rule 1 of the coding conventions says |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | Nullable is on for every project. No `!` in the added lines under `src/`; in the tests each `null!` and `default!` has its reason in the comment of its step, after F4, and the `!` of `JsonNode` reads the answer of the API that the step before checked |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | The new code under `src/` holds no disposable object; the clients, answers and factories of the tests are `using` declarations, and a client that a test goes on using is no longer disposed by the helper (F5) |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None; the event filter reads the result of the context with a property pattern, which `AsyncRulesTests` accepts (F5) |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | The two `catch (DomainException)` and the `catch (ArgumentException)` above have a body and a reason; `ArgumentNullException.ThrowIfNull` in the public members that take an object; no exception is thrown for a refused value, which is an error code of the result |
| 8 | Public types and members have XML documentation comments | Pass | Every project generates its documentation file and builds with warnings as errors; every public type and member of the new files has its summary, and the remarks give the reasons (ADR-0050, RFC 5321 for the lengths of an email, ISO 3166-1 for the countries, ITU-T E.164 for the digits of a phone number) |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | `IsoCountry` is a record, `Countries.All` a read-only list over a frozen set of names, the limits are constants, and the request types are records |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | The days of a stay are `DateOnly` read from the port `IClock`, today on the clock of the hotel; no point in time or money is added |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | Pass | `RefuseAsync` writes an answer of a few bytes in the pipeline of the request and uses its `RequestAborted`; every other new async member takes a token and passes it on |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | Task 3 wrote the tests first and saw 252 of them fail on the skeleton (`before-tests.txt`); the three tests of the normal form failed before their code (`before-tests-normal-form.txt`). The rules of the stay run on a fixed clock; the API tests share a host and take days and caller addresses of their own from counters, so no two tests meet; the one page test that used fixed days takes them from the clock of the host (F5). The tests of the points of the second AI were written first too, and 20 of them failed before the fixes (F8 to F12) |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | After F3 the class diagram draws the stay members of `Booking`, `TextNormalForm`, `Countries` with `IsoCountry`, and the five attributes of the guest details; the classes of the other layers implement no DCD, as in [RC-418] |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge | Pass | The decision of [ADR-0050]: each rule once in the domain (the attributes, `TextNormalForm`, `Countries`, the stay members of `Booking`), called by the API through the services and by the pages through `StayDates` and the view models; the texts of the pages once in `ErrorMessages`; the form of a date in `DateInput`; the limit of a body in `RequestBodyLimit`; the event of a refusal of the body limit and of the handler of exceptions in one method of `SecurityEvents` (F10) |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | The domain uses the base library only (`System.ComponentModel.DataAnnotations`, `System.Net.Mail`, `System.Text`, `System.Collections.Frozen`); the structure tests of [ADR-0001] pass, after they refused the first place of the rules ([RC-421]) |
| 16 | SOLID holds | Pass | One attribute per kind of detail with one static check; the event of a refusal is a filter of its own beside the guard of MIL-077; the limit of a body is a middleware with its own answer; the pages hold no rule of their own |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | `Domain/Attributes` and `Domain/ValueObjects`, `Api/Security`, `Web/Common`, as [ADR-0050] places them |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `PascalCase.cs` and `.razor`, `kebab-case.conf` |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer or event in the change |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | After F2: the twelve new types under `src/` with members group them in the standard order and names, each closed by a named `#endregion`; `IsoCountry` is a positional record without a body. The types that were only changed keep the layout of their file, as [RC-391] reads the criterion |

## Checklist Results: QC-SH-001 (`test-nginx.sh`, and the header of `check-guest-login.sh`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | Unchanged: `test-nginx.sh` keeps `set -uo pipefail`, the convention of `ops/tests`, with each check counted |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | The new function `body` quotes its two arguments, and every check reads its value with `$(...)`; shellcheck clean |
| 3 | Names follow the conventions | Pass | The function `body` and the variable `loaded` in snake_case |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck v0.11.0 clean; no `disable` added |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | Unchanged; a failed check is counted and the test ends with exit 1 |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | The new checks write no file: the body comes from `/dev/zero` through a pipe, and the loaded configuration is read into a variable |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | The checks use the test certificate and the documentation address 203.0.113.7 of the test |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | N-A | The test changes nothing outside its own containers |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | The header names the checks of MIL-086; `check-guest-login.sh` says in its line of needs that it runs on Linux or in WSL ([RC-422] F3) |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | [MIL-086] decisions 4 and 7 in the header and the section title |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | The body at the limit and one byte over it, the tailnet server that keeps 1 megabyte, the web socket on both servers, and the timeouts of the loaded configuration (F5) |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 with `-i 2 -ci`: no difference |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | Unchanged: the test removes its containers and network |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | `head -c`, `awk`, `paste -sd` and here-strings, all in bash 4 and the tools that the header names; no GNU-only option |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | The POST of a body of a given size is the function `body`, and the loaded configuration is read once into `loaded` |

## The criteria of MIL-086 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | [ADR-0050] is Accepted after [RC-420] and, in its second version, [RC-421], both Go |
| 2 | Pass | `Table_EveryFieldOfTheFlow_HasARow`, `Table_EveryRow_NamesAFieldOfTheFlow`, `Table_TypeOfEachRow_IsTheTypeOfItsField`, `Table_EveryRuleInTheCode_Exists` and `FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists` pass, and `BookingAddressRulesTableTests` holds the rows of `/book` |
| 3 | Pass | The domain tests of each attribute at its limits and one value of each refused kind (`GuestFieldFormatTests`, `GuestValidationTests`, `CountriesTests`), `PublicBooking_GuestField_IsAcceptedOrRefusedWithItsCode` and `GuestForm_GuestField_IsAcceptedOrRefusedWithItsCode` without the page, `PublicBooking_UnknownField_IsRefused`, `PublicBooking_BodyAtAndOverTheLimit_IsReadOrRefusedWith413`, `PublicBooking_ChunkedBodyOverTheLimit_IsRefusedWith413ByTheServer` on Kestrel and `PublicBooking_Party_IsOneToFour` pass, and `GuestFieldErrorsTests` shows each code at its field on the page (F8) |
| 4 | Pass | `StayLimitTests` on a fixed clock (five years and a day later, across 29 February, an arrival yesterday, 30 and 31 nights), `BookingInputRulesTests` for a guest and for staff when made and when moved, and `Nights_PublicThirtyAndStaffThirtyOne_AreBookedAndPublicThirtyOneIsRefused` and `Departure_FiveYearsAheadAndADayLater_IsBookedOrRefused` at the API pass |
| 5 | Pending | With the noncharacter U+FFFE in the shared list (F9): `GuestForm_HostileString_IsRefusedByTheFieldOrStoredUnchanged`, `Searches_HostileStrings_AreTextAndNoFault`, `Guest_HostileStringsThatMeetTheRules_AreStoredAndReadUnchanged` on PostgreSQL, the four `Render_StoredHostileValues_AreShownAsText` of the public, guest and staff pages, and the two searches that find `%` and `_` as themselves pass here; the browser test runs in CI |
| 6 | Pending | The hostile-input tests, the test of the public endpoints, and the events of the refusals before the action, of the handler of exceptions (F10) and of a chunked body on Kestrel (F11) pass here; the baseline scan and CodeQL run on the pull request |
| 7 | Pending | The documents in [RC-423], Go; this review, Go-with-conditions; the coverage floor and the required checks of the pull request |
| 8 | Pending | Task 8, after the merge and the CD run that S01 approves |

## Findings

- **F1, corrected while reviewing: `var` in 247 added lines** (QC-CS-001 3). The fix of the analyzers (IDE0008) wrote
  the explicit type of each local whose type is neither built in nor apparent, and only its changes on lines that
  the branch added were kept. For the rest, the compiler named each type: the declarations were given a type that
  nothing converts to, and the build answered with the real type of each; the types that may hold no value got their
  `?` from the nullable analysis, an object creation became `new(...)`, and an empty list `[]`, as the project prefers.
  The three country lists of the pages take `IsoCountry`.
- **F2, corrected while reviewing: no regions in the new types** (QC-CS-001 21). The twelve new types under `src/` with
  members group them in the standard order and names. No member moved: each file was already in that order.
- **F3, corrected while reviewing: the class diagram did not draw the new types of the domain** (QC-CS-001 13). See
  [RC-423].
- **F4, corrected while reviewing: a null-forgiving default without its reason, and two field names** (QC-CS-001 4 and
  1). The new test of the search text of the overview says why its set-up passes `default!`; the two static fields of
  `BookingInputApiTests` are `_daysUsed` and `_address`.
- **F5, note: the tests that changed while the code was built**, each a stand-in decision:
  - Two older tests held the behaviour before MIL-086: a party of 0 got 400 from an attribute of the request and now
    gets 422 with `booking.guests_out_of_range` from the rule of the domain (`HostileInputRegressionTests`); a move by
    a guest with a `roomId` was read without it and is now refused with 400 as an unknown field
    (`Reschedule_GuestRoute_KeepsTheRoomOfTheBooking` holds both answers and the room that is kept). [ADR-0050]
    decisions 2 and 4 make the new answers.
  - The test of the move on the page of the own booking used days of April 2026, which the check on the page refused
    once the real clock of the test host had passed them; it takes its days from that clock.
  - Two infrastructure tests made a guest of the country "Testland", which the list refuses; they use "Denmark".
  - `AsyncRulesTests` refused the first form of the event filter, `context.Result is ...`, as a blocking call; the
    filter reads the result with a property pattern, and the rule of the test was not weakened.
  - Assertions on a tuple that held an array compared the array by reference; the tests assert the status and the
    codes apart. A helper disposed a client that its test went on using; `PostOnSharedClientAsync` leaves the client
    to the test.
  - The timeouts of nginx are checked in the configuration that nginx loaded (`nginx -T`), not by waiting 60 seconds
    or an hour.
  - The test that the benchmark intake still takes a run over 64 KB
    (`PostResult_LongOutputInABodyOverSixtyFourKilobytes_Returns201AndStoresItWhole`) was written after the code,
    when the exception of [ADR-0050] was read against the tests; it passed at once.
- **F6, note: two decisions of the pages.** The country lists of the back office keep `@bind:event="oninput"`, as the
  other fields of the two forms, so a change is checked at once; a stored country that is not on the list is shown as
  the option "(not in the list)", so that the edit of an older guest asks for a country from the list ([ADR-0050]
  decision 8).

- **F7, note: the Danish texts outside the flow.** The messages of the architecture tests (`StructureTests`, "Typer
  uden for skillets mapper") and other Danish texts outside the booking flow stay as they are: [RC-420] F8 left the
  choice to S01, and MIL-086 builds nothing for it. The three Danish words in the parameters of `GuestFields`, which
  is a type of the flow, became English with F13.
- **F8, corrected while reviewing (the second AI's point 1): the codes of the new rules did not stand at their
  fields.** `GuestFieldErrors` placed the five codes of the rules that MIL-086 added (`guest.first_name_invalid`,
  `guest.last_name_invalid`, `guest.phone_number_invalid`, `guest.country_invalid`,
  `guest.passport_number_invalid`) among the errors of no field, so a guest of `/book` whose phone number was refused
  saw the text above the form and no mark at the field (BR-60, BR-96). `GuestFieldErrorsTests` reads every code of a
  guest field from `ErrorCodes.Guest` and checks that it stands at its field, so a code added later is tested too; it
  failed for the five codes before the fix.
- **F9, corrected while reviewing (point 2): a text with the noncharacter U+FFFE made the normal form throw.**
  `TextNormalForm` kept a text with a lone surrogate away from the runtime, which cannot normalise it, but the runtime
  refuses U+FFFE too, with `ArgumentException`. A first name "Ann" with U+FFFE after it, sent to the public booking
  without the page, was answered 400 `api.invalid_request` by the handler of exceptions instead of 422
  `guest.first_name_invalid`, with an error and its stack trace in the log and no event; the sign-in email and the
  two staff searches did the same. The normal form now catches the exception that `string.Normalize` documents for
  invalid Unicode and leaves the text to the rule of its field; the same catch covers a lone surrogate, so the guard
  for it is gone. The value joined the shared hostile strings, so the domain, the API, the database and the pages
  carry it through every field and both searches; nine tests failed before the fix.
- **F10, corrected while reviewing (point 3): the 400 of the handler of exceptions wrote no event.** [ADR-0050]
  decision 6 has the handler of exceptions write the event for its answers, but only its 413 did. A staff move with
  the key 0, which the service refuses as an error of the caller, was answered 400 with `api.invalid_request` and no
  event, so the alerts that [MIL-087] builds on the event would not count it. The handler now writes `input_refused`
  for its 400 too, through one method of `SecurityEvents` that the body limit uses as well (QC-CS-001 14);
  `Refusal_ByTheHandlerOfExceptions_WritesTheSecurityEvent` failed before the fix. The rule ledger promised
  `booking.not_found` for that key; its row now says `api.invalid_request` for a key that is missing or below one, as
  the rows of the other keys do ([RC-423] F6).
- **F11, corrected while reviewing (point 4): the tests of a body over 64 KB could not catch a broken limit.** The test
  host of ASP.NET Core has no limit of the server, and both tests of the benchmark intake sent their body without a
  declared length, so they passed with the `RequestSizeLimit` of the intake removed; no test sent a body in chunks
  through the server that refuses it, which is how the web application sends its requests. The test of the intake in
  `BenchmarkEndpointTests` now declares the length of its body, so the API reads the limit of the endpoint before the
  body, and two tests run the API on Kestrel, the server of production: a chunked body over 64 KB to the public
  booking is refused with 413, `api.request_too_large` and the event, and a chunked run with an output of 100,000
  characters is stored by the intake. Both passed at once, because the code was right. The mutation run shows that the
  tests catch the break: without the `RequestSizeLimit` of the intake both tests of the intake fail with 413; and the
  64 KB of the server are held twice, by the limit of Kestrel for every request and by the limit that the middleware
  sets per endpoint, so the chunked booking is refused while either is left and read when both are removed
  (`mutation-body-limit.txt`).
- **F12, corrected while reviewing (point 5): the day was read twice while a booking was made.** `BookingService`
  checked the stay against the clock, saved the new guest, and read the clock again for `Booking.Create`, so a booking
  for today sent just before midnight could pass the check, store its guest and then be refused with
  `booking.start_date_in_past`, which left the guest stored without a booking. The day is now read once.
  `CreateAsync_DayTurnsAfterTheCheck_BooksAgainstTheCheckedDay`, with a fake clock that turns the day after its first
  read, failed before the fix.
- **F13, corrected while reviewing: two comments that MIL-086 made stale.** The parameters of `GuestFields` said that
  a phone number has no format and a passport number no rule beyond its length, and three of them were Danish words;
  each now names its rule. The parameter of the move by a guest said that a room in the body is not read; it is now
  refused as an unknown field.

## Overall Verdict

Go-with-conditions, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of
[QC-CS-001] and [QC-SH-001] passes after F1 to F4 and F8 to F13; criteria 5 to 7 of [MIL-086] wait for the checks of the pull
request, and criterion 8 for the deploy. The condition: every required check of the pull request is green before the
merge. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| The commits, the commit links resolved, and the pull request that closes the issues of tasks 1 to 7 | The AI | 2026-11-19 |
| Every required check of the pull request green before the merge, with the browser test, the baseline scan and CodeQL | The AI | Before the merge |
| The check on the live site after the deploy and the gate review | The AI | Task 8 of [MIL-086] |

---

[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[RC-420]: ./rc-420-adr-0050.md
[RC-421]: ./rc-421-adr-0050-where-the-rules-live.md
[RC-423]: ./rc-423-mil-086-documents.md
[RC-411]: ./rc-411-mil-086.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[ADR-0001]: ../../adr/adr-0001-clean-architecture-structure.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[MIL-077]: ../../milestones/mil-077-hostile-input-against-the-api.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[RC-418]: ./rc-418-mil-085-code.md
[RC-391]: ./rc-391-mil-018-code.md
[RC-422]: ./rc-422-mil-085-gate.md
[7de27c7]: https://github.com/NF-Hotel/NFHotel/commit/7de27c769b753231a5cf2dc8e786c8197b4bda81
