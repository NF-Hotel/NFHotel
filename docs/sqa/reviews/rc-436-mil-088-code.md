# RC-436 Code review of MIL-088: a booking verified by email

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-436 |
| CrossReference | [MIL-088], [ADR-0052], [ADR-0053], [RC-435], [RC-431], [RC-432], [RC-433], [RC-434], [QC-CS-001], [QC-SH-001], [QC-YML-001], [RC-428], [ADR-0024], [ADR-0037], [ADR-0047], [MIL-089], [ADR-0051] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Deprecated | mikemike | AI as stand-in for S01 | F17 to F19: the comments of CodeQL, GitHub's code quality and SonarCloud on the pull request; F18 corrected (0ad9c7e), F17 and F19 recorded; the verdict stays Go-with-conditions<br>Accepted by the AI as stand-in for S01 in autonomous mode; mode autonomous, not independent | [5861a42] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | F17 names what each alert of CodeQL holds: 681, as 676 on `main`, is the name of the limit `email`, a constant, and no email<br>Accepted by the AI as stand-in for S01 in autonomous mode; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-088] tasks 4 to 7 and the parts of task 8 that are code, on the branch
  `mil-088-booking-verified-by-email`, against `main` at 3b35a3d, which the branch merged in ac53f09: the tests written
  first on a skeleton (their failing run is `docs/sqa/evidence/mil-088/before-tests.txt`), the code of tasks 5 to 7 with
  the lock `public_booking` that the test of criterion 4 called for (7f115a0), and the changes of this review (0ce2e0c,
  the comments of F14 in 4239529, the remarks of F16 in 733a5b2, and the fixes of F18 in 0ad9c7e). In all, 66 files are new and 73 changed under
  `src/`, `tests/`, `ops/`, `scripts/` and `.github/`; none is removed.
  - Domain: `BookingRequest` and `SentMail` with their repositories, the value objects `Mailbox` (the normal form of
    an email for the limits) and `MailKind`, and `IBookingRepository.Discard`.
  - Application: `BookingRequestService` (ask, verify and the mode), `BookingRequestCleanup`, `VerificationTokens`
    (the token, its hash and the link), `BookingMails` (the two mails as plain text), the ports `IMailSender`,
    `OutgoingMail`, `MailSendResult`, `MailFailureReasons`, `IBookingRequestEvents`, `IBookingVerificationSettings`
    and `IUnitOfWork.RunUnderLockAsync`; in `BookingService` the public booking of both paths under the lock
    `public_booking` with the next room after a lost race (`BookPublicAsync`), the check without a save
    (`CheckPublicAsync`) and the booking of a verified request (`BookVerifiedAsync`); the five error codes; the DTOs
    of the request, the verification and the mode.
  - API: the three routes of `BookingsController` (`POST api/bookings/public/requests`, `POST .../requests/verify`
    and `GET api/bookings/public/mode`) and the refusal of the old route with the verification on; the answers of
    `ResultResponses`; `ApiBookingRequestEvents` with the events `booking_request_made`, `verification_failed`,
    `mail_not_sent` and `mail_fuse_blown`; the counter `nfhotel.mails` of `SecurityMetrics`;
    `BookingRequestCleanupWorker`; `Program`.
  - ApiClient: `ApiBookingRequestService` and the routes of `ApiGateway`.
  - Infrastructure: `SmtpMailSender` (MailKit 5.0.0) with `SmtpOptions`, `SmtpSecurity`, `MailSettings` and
    `BookingVerificationOptions`, read and checked at the start in `DependencyInjection`;
    `ConfiguredBookingVerificationSettings`; `BookingRequestRepository` with the claim by `DELETE ... RETURNING`,
    `SentMailRepository`, their configurations, the migration `AddBookingRequests` and the model snapshot;
    `UnitOfWork.RunUnderLockAsync` with a transaction lock of PostgreSQL inside the retrying execution strategy.
  - External: `TelemetryRegistration`, which sends the counter of the mails.
  - Web: the step "check your inbox" of `Book.razor` and `BookingFlowViewModel` (ask, send again, the mode),
    `BookingVerify.razor` with its view model, its script `wwwroot/js/booking-verify.js` and `VerifyPageHeaders` (no
    referrer, no store), the texts of `ErrorMessages`, `MyBookingViewModel` and `GuestSignInRegistration`.
  - Ops: the compose folders `mail-relay` (HAProxy, the forwarder) and `mailpit` (the mail catcher, pinned), the
    networks `nfhotel-mail` and `nfhotel-mail-out`, the settings and the secret file of the API
    (`nfhotel-api.env.example`, `write-secret-files.sh`, `deploy-services.sh`), the profiles of the two containers,
    the throwaway stack (`e2e-stack.sh`, `lib/stack.sh`, `run-e2e-tests.sh` with its two modes, `check-e2e-stack.sh`)
    and the light development mode (`scripts/dev-lite.sh`, `mailpit-port.yml`), `check-log-privacy.sh` with a file of
    values and patterns, and the alerts `MailNotSent`, `MailFuseBlown` and `MailVolumeHigh` with their rule tests.
  - CI: the end-to-end action runs the browser tests twice, with the verification off and on, checks the logs after
    each run and keeps the tokens of the tests out of its artifact; Dependabot watches the two new compose folders.
  - Tests: the tests named under the criteria below, the browser tests `BookingVerificationTests` with `MailCatcher`,
    and `BookingRequestTestDoubles` in the shared support.
- Checklists used: [QC-CS-001] for the C# under `src/` and `tests/`; [QC-SH-001] for the 16 changed scripts;
  [QC-YML-001] for the 8 changed YAML files: the three compose files and the override of the light mode, the two
  files of the alerts, the end-to-end action and the configuration of Dependabot. The action and the configuration of
  Dependabot are no workflows, so the workflow checklist does not apply to them; the check of the workflow policy
  ([ADR-0037]) passes. The script of the page of the link (50 lines of JavaScript) has no checklist of its own in the
  framework; it is held by `Script_OfThePage_IsServedAndTakesTheTokenOutOfTheAddress` and the browser tests, and was
  read for the points of security below. The documents of task 3 and the second versions of [ADR-0052] and
  [ADR-0053] are reviewed in [RC-435]; their first versions in [RC-431] to [RC-434].
- Scope: full review of the new and changed files.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal Opus
  5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An AI wrote
  the code and this record. A second AI, which had not written the code, read the worktree of the branch as it stood
  early on 2026-11-20, before the first commit of the code, for bugs of correctness and security and against the three
  checklists, without the conclusions of this record. It ran `bash -n`, shellcheck v0.11.0, shfmt v3.14.1, yamllint
  1.37.1, PyYAML, promtool v3.16.0 and `docker compose config` on what changed, all clean. Its points are F1 to F10 and
  the notes in F15 below, each checked against the code; each one that changed behaviour got a test that failed without
  its fix (`before-review-fixes.txt`). The AI's own runs found F11 to F13 while the second AI read; their fixes are in
  7f115a0. The AI found F14 while it wrote this record. The second AI found sound: the token (32 bytes of
  `RandomNumberGenerator`, base64url, only its SHA-256 stored under a unique index), the claim by `DELETE ... RETURNING`
  and the one answer 422 for an unknown, used or lapsed link; the link built from the configured https address of the
  site; the page that moves the token from the fragment into the body of a post with antiforgery, then out of the
  address, and sends no referrer and no store; the guards against a header in a mail (the check of the email, one line
  for the address and the subject, `MailboxAddress.Parse`, fixed subjects and no typed text); TLS from the first byte or
  STARTTLS only, the chain and the host name checked, the pin only when it is set, and the name of the relay for TLS
  through the forwarder; a cancellation by the caller as `OperationCanceledException` in both modes of TLS; the password
  only in a secret file and in no message; the masked events and the closed set of reasons of the mails; the limits and
  the fuse under `booking_request`, with no lock taken inside another; and the checks at the start, the hardening of the
  compose files, the pinned digests, the expressions of the alerts and their rule tests.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -c Release -warnaserror`, after the last fix | 0 Warning(s), 0 Error(s) |
| `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore`, which CI runs | exit 0 |
| `dotnet test`, the six projects, after the last fix | Domain 746, Application 831, Api 729, ApiClient 144, Infrastructure 360 (against PostgreSQL 16 in a container of Testcontainers on Docker Desktop) and Web 980 passed, none failed: 3,790 tests (`after-tests.txt`) |
| The tests of F1 and F3, run without their fixes | F1: the three tests failed with the catch taken out (3 of 41 of the class); F3: the test of a relay that never answers failed on the TLS handshake (`tls`, not `timeout`) before the fix of the mapping, and the test of the quit ran into its limit of 20 seconds with the old quit of 30 seconds (`before-review-fixes.txt`) |
| `bash ops/tests/check-e2e-stack.sh` against the throwaway stack of `ops/scripts/e2e-stack.sh`, from WSL on Docker Desktop, then the browser tests in the container of Playwright with the verification off and on, and the checks of the logs after each run | On 0ce2e0c, before the comments of F14: the 54 checks of the stack passed; the browser tests passed, 16 with the verification off and 3 with it on; after each run no log of the 6 containers held the test guest's personal data, and after the run with the verification on none held the start of a link, one of the 5 tokens that the tests wrote down, or an email that the tests made for themselves (`stack-check.txt`) |
| The 27 tests of the job "Ops script tests" of NF Hotel Ops CI, each in Debian with `ops/` read-only, and the check of the workflow policy with its self-test | all passed, among them `test-write-secret-files.sh` 22, `test-deploy-services-env.sh` 33 and `test-cd-steps.sh` 97 after F9 (`after-tests.txt`) |
| `bash ops/tests/test-dev-lite-portable.sh` | 28 passed, 0 failed, after F8 |
| `bash ops/tests/lint.sh`: actionlint, shellcheck, shfmt and yamllint in their pinned containers | `==> all linters are clean` |
| `bash ops/tests/check-monitoring.sh` in WSL | 7 passed, among them `promtool test rules` with the tests of the three new alerts |
| `bash ops/tests/test-wiki-coverage.sh` | 133 passed, 0 failed |
| `bash scripts/check-comment-language.sh` on every new and changed file, which NF Hotel CI runs | exit 0 |
| `**/packages.lock.json` | the four lock files of the projects that take MailKit add MailKit and MimeKit.Core 5.0.0 and nothing else; the others are unchanged |
| The required checks of the pull request on 7dbc737, then the comments on it | 60 passed and 2 skipped (the full scan of DAST and OWASP ZAP, which run after a deploy); CodeQL, GitHub's code quality and SonarCloud commented on the pull request: F17 to F19 |
| After F18 (0ad9c7e): the build of the solution with warnings as errors, the tests of the clean-up worker and of `SmtpMailSender`, the whitespace format check and the check of the comment language on the three changed files | 0 warnings; 5 and 17 passed; the two new tests of the worker passed on the worker as it was, and each failed when its half of the filter was taken out; exit 0 and exit 0 (`after-pr-comments.txt`) |
| Not run here | `check-container-runtime.sh` against the containers of the stack (it needs root on Linux), `test-dev-lite.sh` and `test-secret-rotation.sh` (they start the light mode and rotate its secrets), the baseline scan, CodeQL and the coverage floor: NF Hotel CI and NF Hotel Ops CI run them on the pull request |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `BookingRequestService`, `SmtpMailSender`, `IMailSender`, `MailSettings`, `VerificationTokens`; the constants `MailsPerHour`, `LockName` and `PublicBookingLock`; the private fields `_mailSender` and `_sendTimeout` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | `AskAsync`, `VerifyAsync`, `ClaimOpenAsync`, `RunUnderLockAsync`; one type per file under `src/`; `BookingRequestTestDoubles.cs` groups the doubles of the tests as the other `*TestDoubles.cs` do |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | After F6 no `var` in an added line; the format check and the Release build with warnings as errors are clean; the suppressions are CA1031 with its reason (the clean-up worker and, after F1, the second mail) and the `612, 618` that EF Core writes into the designer file of the migration and into the model snapshot |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | After F7 and F14 each null-forgiving operator that the branch adds has its reason in a comment: the two in `BookingRequestService.AskAsync`, the navigation of the guest in the query of `BookingRepository`, the dates in `BookingFlowViewModel.RequestFor`, and those of the tests, in the comment of their step or on the line before |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | `SmtpMailSender`: the client and both sources of cancellation are `using` declarations, and the socket to the forwarder is disposed when the connection fails; the scope of each run of the worker is `await using` |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None in the new code |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | Guards on every public member; `throw;` in the worker; the catches are filtered to the exceptions they expect, and the two that hold only a comment (the quit and the clean-up) say why (N1). The catch of F1 takes every exception but a cancellation, writes its event and has its reason |
| 8 | Public types and members have XML documentation comments | Pass | Every project builds its documentation file with warnings as errors; the remarks give the decisions ([ADR-0052], [ADR-0053]) |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | `SmtpOptions` and `BookingVerificationOptions` have `init` properties, the DTOs are records, and the fields of the services are `readonly` |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | Pass | `ExpiresAt`, `CreatedAt` and `SentAt` are `DateTimeOffset` from `IClock`; no money is added |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | Pass | Every new port and use case takes a token and passes it on; the second mail runs with `CancellationToken.None` by design ([ADR-0052] decision 5), so a guest who leaves the page does not stop it |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | Task 4 wrote the tests first and saw them fail on the skeleton (`before-tests.txt`); the clock is fixed in every test of the limits, the lapse and the clean-up; the relay of the tests and the race of 20 run on the loopback and in PostgreSQL. The bound of a send is waited for in three tests of 3 seconds each, never with a sleep |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | After F2 the class diagram draws `BookingRequest`, `SentMail`, `Mailbox`, `MailKind`, `BookingRequestService`, `IMailSender` and `SmtpMailSender` ([RC-435]); the sequence diagram SD-001 follows `VerifyAsync` |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge | Pass | One check of a public booking for both paths (`CheckPublicAsync`), one normal form of a mailbox (`Mailbox`), one closed set of reasons (`MailFailureReasons`, held by `MailEventValuesTests`), one table of the limits (`LimitsTableTests`); the path of the page is written twice and pinned on each side by a test (N2) |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | MailKit only in the infrastructure; the use cases know `IMailSender`, `IUnitOfWork.RunUnderLockAsync` and `IBookingRequestEvents`; the structure tests pass |
| 16 | SOLID holds | Pass | `BookingRequestService` is apart from `BookingService` and reaches it through `IPublicBookingService`; each port is narrow and injected |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | `Domain/Entities`, `Application/Ports`, `Application/UseCases/Bookings`, `Infrastructure/Mail`, `Infrastructure/Config`, `Api/Startup`, `Web/Features/Booking` and the matching test projects |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `PascalCase.cs`, `booking-verify.js`, `mailpit-port.yml`, the migration `20261120012052_AddBookingRequests` |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer; `IBookingRequestEvents` is a port |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code; `SearchValues` in `BookingRequest` is a library call |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | After F5 every new type under `src/` with members groups them; the types that were only changed keep the layout of their file, as [RC-428] reads the criterion |

## Checklist Results: QC-SH-001 (the 16 changed scripts)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | The lines are kept in all 16; the tests keep `set -uo pipefail`, the convention of `ops/tests`, with each check counted |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | After F8 the file of the mail catcher is an array; the split in `test-service-networks.sh` carries its SC2046 reason; shellcheck clean |
| 3 | Names follow the conventions | Pass | `mailrelay`, `stack_write_mail_settings`, `stack_set_settings`, `MAIL_RELAY_HOST`, `DEV_MAIL_PORT` |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck and `bash -n` clean on the 16 scripts and on the `run` steps of the action |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `die "error: ..."` and the usage lines of `deploy-services.sh`, `e2e-stack.sh`, `run-e2e-tests.sh` and `check-log-privacy.sh` |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | `mktemp -d` with a trap in `run-e2e-tests.sh` and the tests; `/tmp/mailpit-tls` in `test-container-runtime-live.sh` is a value for `docker compose config`, not a file |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | `Smtp__Password` reaches its secret file by `printf` ([ADR-0024]), and the forwarder gets only the host and the port of the relay |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | `mailrelay` starts only when `Smtp__Forwarder` is set; `e2e-stack.sh verification` acts on the folder of its own `--out` |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | The headers name `--mode`, `--values-file`, `--pattern`, `verification on/off`, `mailrelay` and `DEV_MAIL_PORT` |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | Every change cites [MIL-088] and [ADR-0052] or [ADR-0053] |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | `test-deploy-services-env.sh`, `test-check-log-privacy.sh`, `test-service-networks.sh`, the two tests of the light mode and `check-e2e-stack.sh`; N4 |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt with `-i 2 -ci`: no difference |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | `stack_set_settings` replaces the lines of a key; `test-dev-lite-portable.sh` checks a second run |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | The `Needs:` lines (`lib/stack.sh` adds openssl); `test-dev-lite-portable.sh` finds no GNU-only option |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | After F9 `write-secret-files.sh` reads the secrets of the API from its one list; the three readers of a setting stay apart for the reason in F9, recorded as a deviation |

## Checklist Results: QC-YML-001 (the 8 changed YAML files)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys in any mapping | Pass | PyYAML `safe_load_all` reads all 8; yamllint finds no duplicate key |
| 2 | Indentation is 2 spaces everywhere, never tabs; lines end with LF; the file ends with one newline and has no trailing spaces | Pass | yamllint clean; no CR |
| 3 | Passes `yamllint` (or the project's YAML linter) with its configuration committed next to the code and no unexplained `disable` comments | Pass | yamllint with `ops/.yamllint` in `lint.sh`; no `disable` |
| 4 | A scalar that YAML would coerce to another type is quoted | Pass | `"true"`, `"65534:65534"` and the mapping of the port are quoted |
| 5 | Only safe, plain types are used | Pass | No tag and no include |
| 6 | No secret is written in the file | Pass | `Smtp__Password` is a secret file of compose; the example of the settings has placeholders (F10) |
| 7 | Embedded program text in a value is at most five lines | Pass | The longest is the check of the logs in the action, five lines |
| 8 | A block of keys or values repeated in the file or across files is defined once | Pass | The hardening of each service as in every compose file of `ops/deploy`; each alert once, with its tests in `rules-test.yml` |
| 9 | Anchors, aliases and merge keys are used only where the consuming tool is known to support them | N-A | None |
| 10 | The file validates against the schema of the consuming tool where one exists, and the schema or tool version is stated | Pass | `docker compose config -q` for the compose files and `promtool check config` of the pinned Prometheus image |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision or design is recorded | Pass | The new compose files, `mailpit-port.yml`, `security.yml` and the action cite [MIL-088] and [ADR-0053] |
| 12 | Key names follow the convention of the consuming tool; one case style is used within the file | Pass | The keys of each tool as it names them |
| 13 | Multi-line strings use a block scalar and state the chomping indicator where the trailing newline matters | Pass | `run: >-` and `run: |`, and the block scalars of the alerts |
| 14 | The file name is lowercase kebab-case with one extension style used throughout the repository | Pass | `.yml`, as in `ops/deploy` |
| 15 | A change is exercised by a check that runs without the real target | Pass | `promtool test rules` holds `MailNotSent`, `MailFuseBlown` and `MailVolumeHigh`; the throwaway stack runs the compose files |

## The criteria of MIL-088 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | [ADR-0052] and [ADR-0053] Accepted after [RC-431] to [RC-434]; their second versions after [RC-435] |
| 2 | Pass | UC-011 and US-001.14 with the artifacts they affect and the two terms of the dictionary, Go in [RC-435] |
| 3 | Pass | `AskAsync_ValidRequest_StoresARequestAndSendsOneMail`, `AskAsync_LastRoomAskedFor_IsStillOfferedToTheNextGuest`, `AskAsync_Mail_HoldsTheStayAndTheLinkAndNoTypedText`, `AskAsync_Token_IsStoredOnlyAsItsHash`, `VerifyAsync_UsedWrongMalformedOrLapsedLink_GetsTheSameAnswer`, `Verify_SameTokenTwice_SecondGetsTheAnswerOfALinkThatDoesNotWork`, `ClaimOpenAsync_TenPressesAtOnce_ClaimTheRequestOnce`, `Get_Opened_ShowsTheDisabledFormAndCallsNothing` (a read of the page changes nothing) and the mail checks of the throwaway stack (`stack-check.txt`) |
| 4 | Pass | `VerifyAsync_OpenLink_BooksSignsInAndMailsTheReference` (the guest is signed in as [ADR-0047] signs a guest in), `VerifyAsync_TokenOfTheMail_StoresTheBookingAndItsGuestAndLeavesNoRequest` and, in PostgreSQL, `VerifyAsync_TwentyAtOnceForTheLastRoom_OneBooksAndNineteenAreToldTheRoomIsGone`; after F1 also when the second mail fails |
| 5 | Pass | With the fixed clock: `AskAsync_SecondRequestOfTheMailbox_MakesTheFirstLinkUseless`, `AskAsync_FourthMailInAnHour_IsRefusedAndKeepsTheThirdRequest`, `AskAsync_SixthMailInADay_IsRefused`, `IsOpenAt_ThirtyMinutes_IsTheEnd`, `RunAsync_LapsedRequestsAndOldRecords_AreDeleted` and `DeleteLapsedAsync_LapsedAndOpenRequests_DeletesTheLapsedOnes`; the clean-up at the start and every 10 minutes in `BookingRequestCleanupWorkerTests` |
| 6 | Pass | `AskAsync_RelayDown_TellsTheGuestAndLeavesNothing`, `Ask_RelayDown_Answers503LeavesNoRequestAndWritesTheEvent`, `Start_RelayWithoutEncryption_IsRefusedWithTheNameOfTheSetting`, `AddInfrastructure_ValueNotAllowed_StopsTheStartAndNamesTheSetting`, `SendAsync_RelayWithoutStartTls_IsAFailureOfTlsBeforeTheSignIn`, `AskAsync_LineBreakInTheEmail_IsRefusedBeforeAnyMail`, `SendAsync_LineBreakInTheAddressOrTheSubject_IsRefusedBeforeAnyConnection`, `Events_OfTheRequestAndItsMails_AreCountedOnceWithTheirKind`, and the checks of the logs of the throwaway stack after both runs of the browser tests (`stack-check.txt`) |
| 7 | Pending | `promtool test rules` holds the alerts of failed mails and of 200 mails in a day. The browser tests passed in both modes on the throwaway stack here (`stack-check.txt`); NF Hotel CI runs them on the pull request. A lapsed link is not waited for in a browser: its answer is the one of a used or unknown link (`Verify_UsedOrUnknownLink_GetsTheOneAnswer`), and the lapse is held with the fixed clock and at the claim in PostgreSQL (`ClaimOpenAsync_AtTheMomentTheLinkLapses_ClaimsNothing`), a deviation the AI accepted as stand-in for S01 |
| 8 | Pass | `RequestAndVerify_VerificationOff_AreNotFoundAndThePublicBookingBooks`, `CreatePublicAsync_TwentyAtOnceForTheLastRoomWithTheVerificationOff_OneBooksAndNineteenAreToldTheRoomIsGone`, the browser tests with the verification off, `Start_VerificationOnWithoutARelay_IsRefusedWithTheNameOfTheSetting` and `AddInfrastructure_VerificationOnWithASettingMissing_StopsTheStartAndNamesIt` |
| 9 | Pending | The documents in [RC-435], Go; this review, Go-with-conditions; MailKit 5.0.0 and MimeKit.Core pinned in the lock files; the audit of the packages, the coverage floor and every required check run on the pull request |
| 10 | Pending | The live check waits for the relay of task 9, which is S01's |

## Findings

- **F1, corrected while reviewing (the second AI's point F1, Medium): the second mail could turn a booking into a
  500.** After the verification had committed the booking, an exception in the step of the second mail escaped
  `VerifyAsync`: a transaction of its record that the database still refused after the retries, the save that
  removes the record of a refused mail, or a failure of the send that is no answer of the relay (MailKit's
  `SaslException`, which no reason named). The API then answered 500, and the guest had a booking but no receipt, no
  reference and no session, against [ADR-0052] decision 5. `VerifyAsync` now catches every exception of that step
  but a cancellation (CA1031, with its reason), writes the event of a mail not sent with the reason `other` and
  answers the receipt; `SaslException` reads as a failed sign-in. The text of the exception is not logged, because
  the reasons are a closed set of words ([ADR-0053] decision 5) and the text of a relay can hold an address. The
  three tests (the send throws, the record cannot be written, the record of a refused mail cannot be removed) failed
  with the catch taken out again (`before-review-fixes.txt`), and [ADR-0052] decision 5 says it.
- **F2, closed by task 3 (point F2, Medium): no class diagram and no use case for the new classes.** The reviewer
  read the worktree while task 3 was still open, with UC-011 an empty template. UC-011 with SSD-002, OC-002 and
  SD-001, the booking request and the record of a mail in the domain model and the class diagram, and the terms of
  the dictionary are written and reviewed in [RC-435]; criterion 13 passes on them.
- **F3, corrected while reviewing (point F3, Low): a slow relay could outlast the web application.** A send had 30
  seconds, and its quit 30 more, while the web application gives up on the API after 30 seconds: a slow relay
  showed an error page after the booking was made, as in F1, and a request after its mail had gone. A send now has
  one bound of 15 seconds, from the connection to the end of the session, the quit included; the time limit of
  MailKit for one step is a backstop at twice the bound; and once the bound has run out a failure reads as
  `timeout` at every step. The new test of a relay that never answers found that a bound that ran out in the TLS
  handshake read as `tls`, which the mapping now corrects, and the test of a relay that never answers the quit ran
  into its own limit of 20 seconds with the old quit (`before-review-fixes.txt`). The second constructor of
  `SmtpMailSender`, which takes another bound, is for the tests; the composition makes the adapter with the bound of
  production. [ADR-0053] decision 1 says the bound in a second version, reviewed in [RC-435].
- **F4, accepted and recorded (point F4, Low): a commit whose answer is lost.** When the connection breaks after
  PostgreSQL committed and before the API heard it, the retrying execution strategy runs the work again: a limit
  then counts what the first attempt wrote and gets stricter, but a booking may be made once more, in the next free
  room, or answered with no free room although it stands. The second AI offered a check of the commit
  (`verifySucceeded`) or the risk recorded. The AI decided as stand-in for S01 to record it: every save with the
  retries of EF Core in the application has the same risk, it needs the connection to break at the moment of the
  commit, staff see a second booking in the list, and a check would need a key of the request in the booking. The
  remarks of `RunUnderLockAsync` and the consequences of [ADR-0052] say it.
- **F5, corrected while reviewing (point F5, Low): five new types had no regions** (QC-CS-001 21): `BookingRequest`,
  `SentMail`, `IBookingRequestRepository`, `ISentMailRepository` and `VerifyBookingRequest`. `GuardHash`, which
  `SentMail` shares, is in a region `Internal methods`.
- **F6, corrected while reviewing (point F6, Low): four new `var`** (QC-CS-001 3), in `ApiGateway`, `UnitOfWork`,
  `BookingFlowViewModel` and `BrowserFixture`. The `var` of the older method beside it in `ApiGateway` is outside the
  change and stays.
- **F7, corrected while reviewing (point F7, Low): a null-forgiving operator without its reason** (QC-CS-001 4) in
  `MailSettings.Text`, now a pattern.
- **F8, corrected while reviewing (point F8, Low): an unquoted expansion used as a list** (QC-SH-001 2) in
  `stack_start` of `lib/stack.sh`; the file of the mail catcher is now an array.
- **F9, corrected in part and recorded (point F9, Low): three readers of a setting** (QC-SH-001 15).
  `write-secret-files.sh` named the secrets of the API twice, and this change had added `Smtp__Password` to both
  lists; the loop now reads them from `SECRETS`, its one list. The three readers stay apart, as stand-in for S01:
  `deploy-services.sh` and `lib/stack.sh` take the last line of a key, as compose does with the settings that the API
  reads, so the forwarder and the API agree; `write-secret-files.sh` takes the first line of a secret since MIL-035,
  and which line of a duplicated secret wins on the VM is a change to the deploy of production outside this
  milestone. One reader in `lib/common.sh` stays a candidate for a later change.
- **F10, recorded (point F10, Low): the example file names the relay of Gmail.** `nfhotel-api.env.example` names the
  host of that relay in a line that is commented out, beside placeholders. Kept as intended: the brief of the build
  gives the example file as the one file of the code and its configuration that names Gmail, the line is commented out,
  and the wiki page Mail-Relay holds the settings that S01 enters.
- **F11, corrected while testing (found by the AI's own runs): the browser fixture waited for the wrong thing.**
  `check-e2e-stack.sh` makes a Suite of its own before the browser tests, so the search of the fixture offered other
  room types, and all 19 browser tests failed in the fixture on the first run. The fixture now waits until the step
  of the room types is ready and then reads whether the room of the tests is offered.
- **F12, corrected while testing (found by the AI's own runs): the tokens in the artifact.** The end-to-end action
  uploaded `verification-tokens.txt` of the tests with the traces. The tokens belong to a stack that is removed, but
  the artifact keeps them for 14 days, so a step now removes the file after the check of the logs.
- **F13, corrected while testing (found by the AI's own runs): the gates of NF Hotel CI.** Five comments used a word
  that the check of the comment language reads as Danish, and the format check found one initializer of a test; both
  are corrected, and both checks pass on every new and changed file.
- **F14, corrected while writing this record (found by the AI): null-forgiving operators without their reason**
  (QC-CS-001 4). Four that the branch adds had no reason in a comment: the dates in `RequestFor` of
  `BookingFlowViewModel`, moved there from the older `SubmitAsync`, where they had none either, and three in the
  tests (`BookingRequestTests`, `BookingRequestPersistenceTests` and `ServiceWorld`). The comment of a fifth, in
  `BookingRequestService.AskAsync`, gave the reason without naming the operator. Each has its reason now
  (4239529); comments only, so the build, the format check and every suite ran again on that commit.
- **F15, notes of the second AI that needed no change.**
  - N1: two catches hold only a comment (the quit and the clean-up worker); both are filtered to expected
    exceptions.
  - N2: the path of the page of the link is written twice, in `BookingMails` and in `GuestSignInRegistration`; a unit
    test on each side pins it to `/booking/verify` (the link in the mail, and the headers of the page), so a drift
    fails a test without the stack.
  - N3: the next room after a lost race inside the transaction of `public_booking` is tested with fakes only; only a
    booking by staff can still win that race, and the exclusion constraint decides it.
  - N4: `deploy-services.sh --down mailrelay` passes over a missing settings file without a word, which is the
    behaviour of the other services.
  - N5: the route of the verification has no limit per sender; a token has 256 bits, so a guess costs a line of the
    log and one delete that finds nothing. [MIL-089] reads it against ASVS.
  - N6: the lock file of the browser tests that a local publish rewrites is kept out of the commits.
- **F16, corrected while the documents were reviewed (reported by the writer of the documents of task 3): the
  remarks of the port named one lock.** The summary of `IUnitOfWork.RunUnderLockAsync` named only the lock
  `booking_request` of [ADR-0052] decision 6, while the public booking takes the lock `public_booking` of decision 5
  through the same method. It names both now (733a5b2); a comment only, so the build ran again with no warning, and
  the check of the comment language passes ([RC-435]).
- **F17, recorded (CodeQL on the pull request, Medium): the masked email in the log.** The query
  `cs/exposure-of-sensitive-information` raised five alerts on `BookingFlowEvents`, 677 to 681. Four are on the events
  `public_booking_made`, `guest_lookup_failed`, `limit_refused` and `booking_request_made`, which write the email
  masked to its first character and its domain, as [ADR-0051] decision 6 says. The fifth, 681, is a second alert on
  the line of `limit_refused`: the name of the limit `email` (`LimitNames.Email`), a constant that the query reads as
  private data by its name, and no email. Four of the five are the alerts 673 to 676 of MIL-087 on `main` at new
  lines: the guard `IsEnabled` of this change, which builds the masked email only when the log writes the event, moved
  their lines, so GitHub counts them as new and is likely to mark 673 to 676 fixed when the branch reaches `main`,
  although what is logged has not changed. The one that is new, 680, is the same pattern in the new event
  `booking_request_made`. The AI decided as stand-in for S01 not to change what the events log in this pull request: a
  change, a keyed hash in place of the masked email for one, changes decision 6 of [ADR-0051] and wants its own
  review, and task 3 of [MIL-089] owns these alerts. No alert is dismissed or closed by hand, by S01's rule of
  2026-11-19. The review threads of the five alerts on the pull request say this and are resolved; the alerts stay
  open.
- **F18, corrected after the review (GitHub's code quality on the pull request, Low): two points.** The clean-up
  worker caught every exception after a separate catch that let the stop of the API through (a generic catch
  clause); one catch with a filter now does both. Two new tests hold the behaviour: a cancellation because the API
  stops is thrown, and a cancellation while the API runs, as a timeout of the database, is logged. They passed on the
  worker as it was, and each failed when its half of the filter was taken out (`after-pr-comments.txt`). A test of
  `SmtpMailSender` started a `TcpListener` to find a closed port and did not dispose it; it does now (0ad9c7e).
- **F19, recorded (SonarCloud on the pull request, Info): four issues kept.** IDE0290, a primary constructor, on
  `BookingRequestRepository` and `SentMailRepository`: every repository of the project has a classic constructor
  that calls its base class, and the two follow them. IDE0161, a file-scoped namespace, and CA1861, a constant array
  as an argument, in the migration `AddBookingRequests`: the code that EF Core generated, as in the earlier
  migrations, and a migration runs once. The quality gate of SonarCloud passed.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-CS-001], [QC-SH-001] and [QC-YML-001] passes after the fixes,
and the criteria of [MIL-088] that a test on this machine can hold pass. The conditions: every required check of the
pull request is green (the browser tests in both modes, the check of the throwaway stack, the coverage floor, the audit
of the packages, CodeQL and the baseline scan among them). Criterion 10 waits for the relay of task 9, which only S01
can open, and the gate (task 10) stays open until then. The comments of CodeQL, GitHub's code quality and SonarCloud on
the pull request are F17 to F19: F18 is corrected, and F17 and F19 leave the verdict as it is. The AI accepted this record as stand-in for S01 in autonomous
mode, at S01's word in chat on 2026-11-19. The reviewer is not independent: an AI wrote the code and this record, and
a second AI read the code. S01 may challenge any decision of it, F4, F9, F17 and the lapsed link of criterion 7 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, or return it | S01; done on 2026-11-20 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-088-booking-verified-by-email` |
| Every required check of the pull request green | mikemike | Before the merge |
| The relay of task 9: the app password, the settings of the wiki page Mail-Relay, and the release with them on the VM | S01 | Before the gate |
| The deploy check and the gate (task 10) | mikemike; the CD run is approved by S01 | After the merge and the relay |
| Read the route of the verification, which has no limit per sender, against ASVS (N5) | mikemike | With the ASVS review of [MIL-089] |
| The alerts of CodeQL on the masked email (F17): a change of what the events log, with its own review of decision 6 of [ADR-0051], or the reason each alert stays; a dismissal only at S01's word | mikemike | Task 3 of [MIL-089] |

---

[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[RC-435]: ./rc-435-mil-088-documents.md
[RC-431]: ./rc-431-adr-0052.md
[RC-432]: ./rc-432-adr-0053.md
[RC-433]: ./rc-433-adr-0052-second-round.md
[RC-434]: ./rc-434-adr-0053-second-round.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[RC-428]: ./rc-428-mil-087-code.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[5861a42]: https://github.com/NF-Hotel/NFHotel/commit/5861a42cfcb5908874412a28c1cb63198692926e
