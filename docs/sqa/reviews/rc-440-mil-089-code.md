# RC-440 Code review of MIL-089: the booking flow attacked and measured

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-440 |
| CrossReference | [MIL-089], [ADR-0051], [ADR-0052], [SAR-002], [RC-438], [RC-439], [RC-436], [RC-428], [RC-424], [RC-204], [SD-001], [OC-002], [ADR-0047], [ADR-0037], [MIL-090], [QC-CS-001], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-21 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: review of the code of MIL-089 (tasks 2 to 6); No-Go; mode autonomous, not independent | pending |
| 2026-11-26 | Accepted | mikemike | AI as stand-in for S01 | Delta review after the fixes: No-Go (D1); round 3 after the fixes of D1 and D2: Go-with-conditions (F5, F6); mode autonomous, not independent<br>Accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-089] tasks 2 to 6 in the worktree of the branch `mil-089-booking-flow-attacked`,
  against `main` at 3cfe3ab, uncommitted, as it stood on 2026-11-21. 27 files changed outside `docs/`; none is new or
  removed.
  - API: `SecurityEvents`, `BookingFlowEvents`, `ApiBookingRequestEvents`, `BookingFlowEventFilter` and
    `SenderLimitFilter`. Every security event of the booking flow holds the address of the caller and no email, whole,
    masked or hashed, as decision 6 of [ADR-0051] says in its third version; `MaskEmail` and `EmailOf` are gone, and
    the filter reads no argument of the action.
  - Application: the port `IBookingRequestEvents` takes no email; `LimitNames.Email` (`email`) is
    `LimitNames.Mailbox` (`mailbox`); `BookingRequestService` writes the events without the email. These close the
    five alerts 677 to 681 of CodeQL (`cs/exposure-of-sensitive-information`) through the code ([SAR-002] section 5).
  - Web: `SecurityHeaders` sends `Referrer-Policy: same-origin` on every answer (ASVS 5.0 requirement 3.4.5) and its
    summary gives the reason for `base-uri 'self'` (requirement 3.4.3). `Program` registers `UseVerifyPageHeaders`
    before `UseSecurityHeaders`, so `no-referrer` still wins on `/booking/verify`.
  - Tests: the events without an email, the referrer policy, the lock `public_booking` of a public booking (abuse case
    6 of [SAR-002]) and a guest token after the cancellation of its booking ([ADR-0047] decision 7, finding 5 of
    [MIL-089]); the shared doubles and the table of the limits follow the rename.
  - Ops: the load test with the booking flow (`ops/load/nfhotel.js`, `ops/scripts/load-test.sh`,
    `.github/workflows/load-test.yaml`), the checks of the log of the API in `ops/tests/check-e2e-stack.sh`, the fixture
    of `ops/tests/test-check-log-privacy.sh`, the label `limit` in the rule tests of Prometheus, and the nine rules of
    the scan in `ops/security/zap-rules.tsv`, renewed to 2026-12-18.
  - Build: `Directory.Build.props` says 0.3.0 (decision 6).
  - Not in this pass: the fix of finding BF-02 of [SAR-002] (a limit of the open circuits of one caller address on
    `/_blazor`, with its files under `src/NFHotel.Web/Security/` and `tests/NFHotel.Web.Tests/`, a line in
    `Program.cs`, a setting in `appsettings.json`, the client timeouts of `ops/deploy/nginx` and their check in
    `ops/tests/test-nginx.sh`). It is added after this review and gets a delta review of its own.
- Checklists used: [QC-CS-001] for the 19 C# files (10 under `src/`, 9 under `tests/`); [QC-SH-001] for the three
  scripts; [QC-YML-001] for the two YAML files (the workflow and `rules-test.yml`); [QC-WF-001] for the workflow. The
  k6 script has no checklist in the framework; it was read against the rules for every language of the
  coding-conventions skill and for security, as [RC-436] read the script of the page of the link, and as [RC-204]
  reviewed its first version. `zap-rules.tsv` and `Directory.Build.props` are data, checked as the table "The data
  files" below says. The documents of MIL-089 are not in this record: [SAR-002] is reviewed in [RC-438]; the third
  version of [ADR-0051], the new row of [ADR-0052], the business rules and the wiki in [RC-439].
- Scope: full review of the changed files.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Reviewer: AI as stand-in for S01.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 for the run of MIL-085 to
  MIL-089 and handed the decisions to the AI, as the Version History of [MIL-089] records. The AI that wrote the code
  wrote this record; no second reader read the code. S01 may challenge any decision of it.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -c Release -warnaserror` (the author's run on the worktree) | 0 warnings, 0 errors |
| `dotnet format whitespace NFHotel.sln --verify-no-changes` (author) | exit 0 |
| `dotnet test` (author) | Domain 746, Application 834, Api 717 and Web 981 passed, none failed. Infrastructure needs Docker and runs in NF Hotel CI; ApiClient is not in the report, and nothing of it changed |
| The classes of the changed tests again, here: `dotnet test --project <project> -c Release --no-build --filter-class <class>`; the build output is newer than every changed file | Api 53 (`SecurityEventValuesTests`, `BookingRequestApiTests`, `SenderLimitApiTests`, `LimitsTableTests`, `BookingRequestCleanupWorkerTests`), Application 54 (`BookingRequestServiceTests`, `GuestBookingServiceTests`) and Web 20 (`SecurityHeadersTests`, `BookingVerifyPageTests`) passed, none failed |
| `bash ops/tests/lint.sh` (author): actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1 and yamllint 1.35.1 in their pinned containers | clean |
| Here: actionlint 1.7.12 (without shellcheck) on the workflow; yamllint 1.37.1 with `ops/.yamllint` on the two YAML files; PyYAML 6.0.3 with a loader that refuses duplicate keys; `bash -n` on the three scripts; `node --check` on a copy of `nfhotel.js` as a module | exit 0 each; one document per YAML file and no duplicate key |
| Line endings, trailing spaces and the final newline of the 27 changed files, here | LF only, no trailing space, one final newline each |
| `bash ops/tests/check-monitoring.sh` (author) | 7 passed, `promtool test rules` among them, with the series of the limit `mailbox` |
| `test-check-log-privacy.sh`, `test-workflow-policy.sh` and `test-dast-rules.sh` (author) | 10 passed; passed; 33 passed |
| `scripts/check-comment-language.sh` and the tests of the wiki (author) | clean; passed |
| `ops/scripts/dast-scan.sh --check-rules ops/security/zap-rules.tsv` here, with the date of the check set to today, to 2026-12-04 and to 2026-12-19 | exit 0, exit 0, exit 1: the nine rules hold until 2026-12-18 |
| `ops/scripts/load-test.sh` here with a fake `docker` and a `stack.env` of made-up values, in Git Bash, and in WSL for the file modes | A run that passes: exit 0, the summary written, the container and the secrets folder removed. k6 exits 99: exit 1. `E2E_API_NETWORK` missing: `error:` naming the key, exit 1, no call of docker. A token that is not hex: `error:` without the value, exit 1. No arguments: the usage line, exit 2. No secret value in any argument of docker; the secrets folder 700 and `load.json` 600 while k6 runs |
| The load test of task 4 (author, `docs/sqa/evidence/mil-089/load-verification-off.txt` and `-on.txt`), 2026-11-20 on `main` at 3cfe3ab with the load test of this task, before the C# changes of this branch | Both modes: 3,445 requests of the guests, 0 failed, 95 percent within 15.31 ms (off) and 19.9 ms (on); the burst got 65 answers and 23 refusals of the limits, no answer of the 500 kind and nothing unexpected |
| The tests of four abuse cases without their control (author, `without-their-control.txt`) | Cases 8, 7, 2 and 6 each failed with the control taken out and passed with it |
| `**/packages.lock.json` | no change against `main`; this review ran no restore and no build |
| Not run here | Anything that needs Docker: `check-e2e-stack.sh` with its new checks, the browser tests, `lint.sh`, the Infrastructure tests and the load test on the final code. CodeQL, the coverage floor and the workflow on GitHub run on the pull request or by hand |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `LimitNames.Mailbox` (`LimitNames.cs:24`), `SecurityHeaders.ReferrerPolicy` (`SecurityHeaders.cs:28`), `WriteMailboxLimitEvent` (`BookingRequestService.cs:344`), the helper `TakesAnEmail` of the test (`SecurityEventValuesTests.cs:38`); the locals `events`, `withEmail` and `policy`; no field is added |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | No async method is added under `src/`; the new test methods are named `Method_Condition_Expected`, as every test of the project (criterion 12); no file and no type is added |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Fail | F1: two new `var` in `tests/NFHotel.Web.Tests/SecurityHeadersTests.cs` lines 91 and 94. The format check and the build with warnings as errors are clean, and no suppression is added |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Fail | F2: two operators without their reason in `tests/NFHotel.Api.Tests/SecurityEventValuesTests.cs` lines 39 and 40. The change removes one from `BookingRequestService.AskAsync`, which had its reason |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | No disposable object is added; the changed tests keep `using` on their clients and answers |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | None in an added line |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | `BookingFlowEvents.LimitRefused` keeps its guard (`BookingFlowEvents.cs:47`); no catch is added |
| 8 | Public types and members have XML documentation comments | Pass | The new and changed public members have their summaries: `LimitNames.cs:20-24`, `SecurityHeaders.cs:22-28` with ASVS 5.0 3.4.5, `IBookingRequestEvents.cs:15-26`; the projects build their documentation files with warnings as errors. Stale words in three summaries of tests: F10 |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | Constants and the read-only lists `LimitNames.All` (`LimitNames.cs:37`) and `SecurityHeaders.All` |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | N-A | No money and no point in time is added |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | N-A | No public async API is added or changed |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | `Events_EveryLogMessage_TakesNoEmail` (`SecurityEventValuesTests.cs:20`) reads every message and parameter of the log and first asserts that it found all 8, so it cannot pass on an empty list; `Booking_Made_WritesOneEventWithTheAddressAndNoEmail` and `GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheReference` (`SenderLimitApiTests.cs:512` and `540`) find no `@` in the whole entry; `Refusals_OfEveryLimit_WriteOneEventEachWithTheName` (line 569) holds one event per refusal with its name; `ReferrerPolicy_Value_SendsNoAddressToAnotherSite` (`SecurityHeadersTests.cs:88`), with the test of the pages that checks every header of `SecurityHeaders.All` (line 150) and `Answers_OfThePage_CarryNoReferrerAndNoStore` (`BookingVerifyPageTests.cs:109`), whose single `no-referrer` would turn into `same-origin` if the two lines of `Program.cs` changed places; abuse case 6 (`BookingRequestServiceTests.cs:850`, F9) and finding 5 (`GuestBookingServiceTests.cs:60` and `207`). The world of the tests has a fixed clock and no network |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Fail | F3: the class diagram (`docs/dcd.md` lines 409, 411 and 412, and its source `docs/dcd.puml` lines 396, 398 and 399) still draws the email in the methods of `IBookingRequestEvents`, and [SD-001] and [OC-002] still give the events the email and name the limit `email`; the change does not touch them, and no deviation is recorded |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge, not where it merely looks alike | Pass | One constant per policy: `SecurityHeaders.ReferrerPolicy` for every answer and `VerifyPageHeaders.ReferrerPolicy` (`VerifyPageHeaders.cs:17`) for the page of the link, two policies for two pages; the name of the limit once in `LimitNames.Mailbox`; one `WriteMailboxLimitEvent` for both paths of the use case (`BookingRequestService.cs:127` and `207`). The test pins the value `same-origin` on purpose |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | The port in Application loses its parameters and gains no reference; the adapter stays in the API; no project reference changes |
| 16 | SOLID holds: a class has one reason to change; new behaviour is added by extension, not by editing a type test in several places; subtypes honour the contract of their base; interfaces are narrow; high-level code depends on interfaces and receives its implementations by injection | Pass | `BookingFlowEventFilter` no longer reads the arguments of the action (`BookingFlowEventFilter.cs:29-57`), so it has one reason to change; `IBookingRequestEvents` is narrower (`IBookingRequestEvents.cs:16-26`) |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure (the projects `<App>.Domain`, `<App>.Application`, `<App>.Infrastructure` and `<App>.Presentation` under `src/`, and `unit`, `integration` and `e2e` under `tests/`), in `PascalCase`, and project references between the layers point inward | Pass | No file is added or moved; the projects as the earlier reviews read them |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | No file or folder is added or renamed |
| 19 | Observers are notified through the `event` keyword and delegates: events are `EventHandler<TEventArgs>` (or a justified custom delegate), named in the past tense, raised only by the publisher through `Handler?.Invoke(this, e)`; there is no hand-rolled observer list and no public delegate field; a subscriber that outlives its need unsubscribes | N-A | No observer; `IBookingRequestEvents` is a port |
| 20 | Code optimised with SIMD (for example AVX2) or CUDA keeps a scalar reference implementation that a test compares with the accelerated result, guards each intrinsic path with `IsSupported` and falls back, handles the tail elements, and reaches CUDA through a port in Infrastructure; the improvement is documented (hardware and runtime, input sizes, a checked-in benchmark, scalar and accelerated times, speed-up) | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names (`Fields`, `Constructors`, `Properties`, `Events`, `Public methods`, `Protected methods`, `Private methods`, `Nested types`), each closed by a named `#endregion`, none nested and none inside a method body | Pass | The changed types keep their regions: `SecurityEvents` has `Fields`, `Properties` and `Public methods` (lines 17 to 187), and its region `Private methods` went with its last members; `BookingFlowEvents`, `LimitNames`, `IBookingRequestEvents`, `BookingRequestService` and the two filters as before. `SecurityHeaders` has no region and is only changed, so it keeps the layout of its file, as [RC-428] reads the criterion; `Program.cs` holds top-level statements |

## Checklist Results: QC-SH-001 (the three changed scripts)

`ops/scripts/load-test.sh`, `ops/tests/check-e2e-stack.sh` and `ops/tests/test-check-log-privacy.sh`.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | `load-test.sh` lines 1 and 16; the two tests keep `set -uo pipefail` (`check-e2e-stack.sh:23`, `test-check-log-privacy.sh:15`), the convention of `ops/tests`, with each check counted |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | Every expansion of `load-test.sh` is quoted (lines 52 to 88), and it has no list; the new checks quote their counts (`check-e2e-stack.sh:341-348`); shellcheck clean |
| 3 | Names follow the conventions: `kebab-case.sh` files, `snake_case` functions and variables, `UPPER_SNAKE` constants and environment variables | Pass | The functions `setting` and `cleanup`, the variables `stack_env`, `api_network`, `secrets` and `container`, the constants `OPS` and `K6` |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck v0.11.0 in `lint.sh` (author) and `bash -n` here on the three; no `disable` comment is added |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `die` writes `error:` to standard error and exits 1 (`load-test.sh:22-25`, `52-62`); a missing or unknown argument prints the usage line and exits 2 (lines 27 to 30 and 51); both seen with the fake docker. A failure of docker before k6 starts (lines 80 and 84) ends the script with the code of docker under `set -e`, as `docker run` did on `main` |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | `mktemp -d` under `umask 077` (`load-test.sh:66`) and `trap cleanup EXIT` (lines 68 to 72), which removes the folder and the container |
| 7 | No secret is written in the script, echoed, or put on a command line; secrets come from the environment or a gitignored file | Pass | The token, the user and the password come from the `stack.env` that `e2e-stack.sh` wrote outside the repository; they reach k6 in `load.json`, written by the builtin `printf` (lines 74 and 75) into a folder that only the user can read, mounted read-only (line 81). The arguments of docker hold paths and names only, and the errors name the key, never the value; seen with the fake docker |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | `load-test.sh` acts only on the throwaway stack that the required `--stack-env` names, with a container of its own that it removes, and writes only into the folder of the required `--results`; its header says so (lines 2 to 12) |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | `load-test.sh:2-14`: the purpose, the usage with both options, the files it writes, the exit codes and what it needs; it reads no environment variable. `check-e2e-stack.sh:14-17` names the checks of the log |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | `load-test.sh:3` cites MIL-036 task 3 and MIL-089 task 4; `check-e2e-stack.sh` cites MIL-089 (line 17) and decision 6 of [ADR-0051] (line 337) |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pending | F6: the five new checks of the log in `check-e2e-stack.sh` (lines 337 to 348) have not run; they run in the end-to-end job of the pull request. `load-test.sh` has no test of its own: its recorded runs are the two load runs of the evidence (success, both modes) and the runs with the fake docker above (a broken threshold, a missing or invalid setting, no arguments). `test-check-log-privacy.sh`: 10 passed |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 in `lint.sh` (author) |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | Each run has its own container (`nfhotel-load-$$`) and secrets folder and overwrites only its own summary; a second run on the same stack makes six rooms of its own (`nfhotel.js:127-138`), which the workflow relies on |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | "Needs: bash 4+, docker" (`load-test.sh:14`); `id`, `sed -n`, `mktemp -d` and `tee` with no GNU-only option |
| 15 | A command sequence, constant, path or message used more than once is defined once (a function, a variable or a sourced helper); a helper is shared between scripts only where it expresses the same knowledge | Pass | In `load-test.sh` one function reads a key of `stack.env` (line 33) for the five keys (lines 53 to 62), where `main` read its one key inline. Across the scripts the readers stay apart as [RC-436] F9 recorded, now with one more: F12, a deviation recorded as stand-in |

## Checklist Results: QC-YML-001 (the two changed YAML files)

`.github/workflows/load-test.yaml` and `ops/deploy/monitoring/prometheus/rules-test.yml`.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys in any mapping | Pass | PyYAML 6.0.3 with a loader that refuses duplicate keys: one document each, no duplicate key |
| 2 | Indentation is 2 spaces everywhere, never tabs; lines end with LF; the file ends with one newline and has no trailing spaces | Pass | yamllint clean; no tab, no CR, no trailing space, one final newline |
| 3 | Passes `yamllint` (or the project's YAML linter) with its configuration committed next to the code and no unexplained `disable` comments | Pass | yamllint 1.35.1 in `lint.sh` (author) and 1.37.1 here, with `ops/.yamllint`; no `disable` comment |
| 4 | A scalar that YAML would coerce to another type is quoted: `yes`, `no`, `on`, `off`, `null`, `~`, numbers with a leading zero, versions such as `1.10`, and values that contain `: ` or ` #` | Pass | The cron (`load-test.yaml:17`) and the values of the series (`rules-test.yml:617` and `704`) are quoted; `timeout-minutes`, `retention-days` and `cancel-in-progress` are a number and a boolean by intent; `on` is the key that GitHub Actions reads |
| 5 | Only safe, plain types are used: no language-specific tags (`!!python/object`, `!ruby/object`) and no `!include`-style extension unless the consuming tool documents it | Pass | No tag and no include |
| 6 | No secret is written in the file: no password, token, key or private connection string; a secret is referenced by name and supplied by the consuming tool | Pass | "Secrets: none" (`load-test.yaml:11`): the stack makes its own token and accounts on the runner |
| 7 | Embedded program text (shell, Python, SQL, `jq`, and so on) in a value is at most five lines; a longer one is moved to a file of its own, in its own language, and the YAML calls it | Fail | F4: `load-test.yaml` lines 45 to 52 and 61 to 68 hold eight lines of shell each; on `main` the one block had six |
| 8 | A block of keys or values repeated in the file or across files is defined once (an anchor, a template, an include or a reusable unit the consumer supports) | Fail | F4: the two `run` blocks repeat the same eight lines with only the folder and the title changed, and `STACK_DIR` is set in the `env:` of four steps (lines 39, 43, 55 and 59). In `rules-test.yml` each test has its own series, as promtool needs |
| 9 | Anchors, aliases and merge keys (`&`, `*`, `<<`) are used only where the consuming tool is known to support them | N-A | None |
| 10 | The file validates against the schema of the consuming tool where one exists, and the schema or tool version is stated | Pass | actionlint 1.7.7 (stated at `load-test.yaml:12`) and 1.7.12 here; `promtool test rules` in `check-monitoring.sh` for the rule tests |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision or design is recorded | Pass | `load-test.yaml:1-12` with MIL-036 task 3 and MIL-089 task 4; `rules-test.yml:1-2`, unchanged |
| 12 | Key names follow the convention of the consuming tool; one case style is used within the file (kebab-case or snake_case, never mixed) | Pass | The keys as GitHub Actions names them (`timeout-minutes`, `persist-credentials`, the event `workflow_dispatch`) and as promtool names them (`input_series`, `alert_rule_test`) |
| 13 | Multi-line strings use a block scalar (`\|` or `>`) and state the chomping indicator (`\|-`, `\|+`) where the trailing newline matters | Pass | `run: \|` for the scripts and `series: >-` for the series |
| 14 | The file name is lowercase kebab-case with one extension style (`.yml` or `.yaml`) used throughout the repository | Pass | Names unchanged: `.yaml` as every workflow in `.github/workflows`, `.yml` as under `ops/`, as [RC-436] read it |
| 15 | A change is exercised by a check that runs without the real target: a parse and schema test, or a dry run of the consuming tool | Pass | actionlint on the workflow; `promtool test rules` on the series with `limit="mailbox"` (`rules-test.yml:616` and `703`), which the alert counts by kind only (`rules/security.yml:26-27`) |

## Checklist Results: QC-WF-001 (`.github/workflows/load-test.yaml`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Fail | QC-YML-001 7 and 8 fail (F4) |
| 2 | The file is in `.gitea/workflows/` or `.github/workflows/` with the extension `.yml` or `.yaml`, and the same workflow does not exist in both directories | Pass | `.github/workflows/load-test.yaml`; the repository has no `.gitea/workflows/` |
| 3 | The file declares `name`, `on` and `jobs`; `on` lists explicit events and, where it matters, `branches` or `paths` | Pass | Lines 13, 15 to 18 (`schedule`, `workflow_dispatch`) and 27 |
| 4 | A header comment states the purpose, the triggers, the inputs, the secrets it needs, what it changes outside the repository, and the decision or milestone it implements | Pass | Lines 1 to 12: the purpose with both modes and the burst, the triggers, "Secrets: none", "nothing is pushed", MIL-036 task 3 and MIL-089 task 4. The workflow has no inputs, which the header leaves unsaid, as on `main` |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | The job (line 29) and its seven steps (lines 33, 37, 41, 53, 57, 69 and 77) |
| 6 | The file passes `actionlint` (or the project's workflow validation test) and the tool version is stated | Pass | actionlint 1.7.7 in `lint.sh`, stated at line 12, and 1.7.12 here: clean |
| 7 | Every job declares `permissions` explicitly with the least access that works (`contents: read` or none by default); write access only on the job that needs it | Pass | `permissions: contents: read` at the top (lines 20 and 21) for the one job, as rule 2 of the policy of [ADR-0037] asks; unchanged; `test-workflow-policy.sh` passes |
| 8 | Every action used is pinned to a full commit SHA or an exact release tag, never to a branch; a moving major tag (`@v1`) is accepted only for an action published by the same organisation under a stated release policy | Pass | `actions/checkout` and `actions/upload-artifact` at full SHAs (lines 34 and 71); k6 by digest (`load-test.sh:20`) |
| 9 | No secret is written in the file; a secret is read as `secrets.NAME`, passed to a step through `env:`, never placed on a command line or echoed | Pass | No secret |
| 10 | Untrusted text (`inputs.*`, event fields such as a title, branch name or label, step outputs derived from them) is never interpolated into a `run:` script; it is passed through `env:` and used as a quoted variable | Pass | The scripts use `$STACK_DIR` from `env:` (from `runner.temp`) and fixed folders; no expression in a `run:` |
| 11 | A trigger that gives a run secrets or a write token on code from a fork or an untrusted branch (`pull_request_target`, `workflow_run`) does not check out or execute that code | N-A | The triggers are `schedule` and `workflow_dispatch` |
| 12 | A workflow that changes state outside the repository (release, tag, publish, sync, deploy) is started by hand or by a protected branch, and a manual run defaults to a dry run with an explicit input to apply | N-A | The workflow changes nothing outside its run: the stack lives on the runner and is removed (lines 77 to 79) |
| 13 | The token the workflow uses has only the scope the job needs, and where the host's built-in token is enough it is used instead of a personal token | Pass | The built-in token with `contents: read`, and `persist-credentials: false` (line 36) |
| 14 | Every job sets `runs-on` to an explicit label and `timeout-minutes` | Pass | `ubuntu-24.04` and 30 minutes (lines 30 and 31); that 30 minutes hold the stack and the two runs is seen only on GitHub (F5) |
| 15 | `concurrency` is set where two runs would conflict: a check cancels the older run (`cancel-in-progress: true`); a publishing job queues (`cancel-in-progress: false`) | Pass | One group per workflow; a second run waits (lines 23 to 25) |
| 16 | Triggers are filtered (`branches`, `paths`, `types`) so the workflow runs only for the changes it concerns | Pass | Weekly and by hand only |
| 17 | Tools and dependencies are installed at pinned versions; a version is a workflow input with a default or a value in one place | Pass | The actions by SHA; k6 by digest in one place (`load-test.sh:19-20`) |
| 18 | A dependency cache is used where the install is slow enough to matter | N-A | The workflow installs nothing; `e2e-stack.sh` builds the images of the stack, as before |
| 19 | A failing step names the tool, the file and the reason in its log (annotation or `::error::` line) | Pass | k6 names each broken threshold in its summary, which the step summary and the artifact keep; `load-test.sh` writes `error:` lines |
| 20 | A `run:` block is within the limit on embedded program text of criterion 7 of [QC-YML-001]; a longer script is a file (`tools/`, or a shared repository fetched at a pinned ref) that the step calls with one command, and the workflow passes its settings through `env:` | Fail | F4: lines 44 to 52 and 60 to 68 |
| 21 | Logic used by more than one workflow or repository is a reusable workflow (`workflow_call`) or a composite action; each input has a `description`, a `type` and a `default` where one is sensible | Pass | The stack and the load test are scripts (`e2e-stack.sh`, `load-test.sh`) that the end-to-end action and this workflow share; no logic is copied into another workflow |
| 22 | Every workflow feature used is verified on the instance that runs it, and the host and version are recorded; a feature the target hosts differ on is noted in the header | Pending | F5: the workflow as changed has not run on GitHub's hosted runner |
| 23 | A reusable workflow states, in its header, how a caller pins it and the script it fetches, so the workflow and its script are the same release | N-A | Not a reusable workflow |
| 24 | A static test of the repository's workflows exists and runs on every pull request: valid YAML, required keys, explicit permissions and runner, no inline script above the limit | Fail | F7 (Optional): `lint.sh` and `test-workflow-policy.sh` check the YAML, the keys, the permissions and the pins, but none reads the length of a `run:` script, so F4 passed them |

## The k6 script: the rules for every language

`ops/load/nfhotel.js`, read against the rules of the coding-conventions skill that apply to it and for security. No
checklist of the framework covers it, so these rows do not decide the verdict.

| Rule | Status | Evidence/Notes |
| --- | --- | --- |
| 1, the existing code wins | Pass | camelCase constants as the file's `target` (line 15), two spaces, double quotes |
| 2 to 4, names | Pass | `searchLimit`, `bookingLimit`, `burstAnswers`, `burstRefusals`, `book`, `count`, `guest`, `booking` |
| 8, comments say why | Pass | Why a new address per visit in a /64 of its own (lines 157 and 158), why the nights never overlap (21, 22 and 175), why 202 or 200 (179 and 180), and why the burst gets exactly its numbers (25 to 27, 187 and 188) |
| 9, no unexplained magic numbers | Fail | F11: `86400000` (line 89), `30` and `60` (line 176) and `1500` (line 221) have no name |
| 10, errors handled | Pass | `setup()` stops the test when the Manager cannot sign in, a room is not made or the mode cannot be read (lines 123 to 142); `count()` sorts every answer of the burst into one of four counters, each with a threshold (lines 69 to 72 and 189 to 205), and reads the body only of a 429 |
| 11, each piece of knowledge once | Fail | F11: the address of the search with its dates is written twice (lines 167 and 168, 213 and 214). The limits of production are named once (lines 28 and 29) and the expected counts of the burst are computed from them (32 and 33) |
| 15, LF | Pass | LF only |
| Security | Pass | The token and the account come from a file mounted read-only (line 19), never from the environment; `X-NFHotel-Client-Address` goes with the service token, as the web application sends it; the addresses are of the documentation range 2001:db8::/32 and the emails of the domain example.com; the defaults are the names of the throwaway stack (lines 15 and 16) |
| 17, tests | Pass | The thresholds are the test (lines 62 to 73); `node --check` exit 0; the two recorded runs of the evidence match the current thresholds and names of the checks |

## The data files

| File | Status | Evidence/Notes |
| --- | --- | --- |
| `ops/security/zap-rules.tsv` | Pass | The nine rules (lines 9 to 17) keep their levels and expire on 2026-12-18, at most 30 days ahead; each reason says "checked again in MIL-089", and three reasons are longer (10031, 10049 and 10111), backed by the scans of [SAR-002] section 5 ([RC-438] reviews that report). `--check-rules` passes until 2026-12-18. [MIL-090] leaves these lines to task 3 of MIL-089 and renews them itself if this pull request is not merged by 2026-12-01; it also adds lines for other rules to the same file, so the later of the two pull requests may meet a conflict |
| `Directory.Build.props` | Pass | `<Version>0.3.0</Version>`, decision 6 of [MIL-089]; no build step or code reads the number |

## The criteria of MIL-089 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pending | [SAR-002] is reviewed in [RC-438]: No-Go, Proposed |
| 2 | Pending | The tests of the abuse cases that this change adds or touches pass here, and `without-their-control.txt` shows the tests of cases 8, 7, 2 and 6 failing without their control; the test of case 6 asserts the name of the lock only (F9). The list of the cases and their tests is in [SAR-002] ([RC-438]) |
| 3 | Pending | The rules of the scan hold until 2026-12-18, so "no rule lapses within 14 days" holds for a gate on or before 2026-12-04, the target date. The scans are in [SAR-002] section 5. The code scan of the pull request still has to show the alerts 677 to 681 closed and no new alert in the changed code |
| 4 | Pending | The two runs of the evidence passed both thresholds, and the burst got only answers and refusals of the limits, but on `main` with the load test of task 4, before the C# changes of this branch; the run on the final code is F5 |
| 5 | Pending | The coverage floor runs on the pull request |
| 6 | Pending | The verdicts are in [SAR-002]. The code of finding 5: `GetAsync_CancelledBooking_ReadsTheReceiptWithTheStatusCancelled` and `RescheduleAsync_CancelledBooking_KeepsTheCodeOfTheBookingAndTheDates` (`GuestBookingServiceTests.cs:60` and `207`) hold decision 7 of [ADR-0047] and pass |
| 7 | Pending | `Directory.Build.props` says 0.3.0. The wiki is reviewed in [RC-439], No-Go, and the page Alerts has a sentence that this change missed (F8). This review is No-Go. The required checks run on the pull request |
| 8 | Pending | After the last deploy (task 7) |

## Findings

- **F1, Fail (QC-CS-001 3): two new `var`.** `tests/NFHotel.Web.Tests/SecurityHeadersTests.cs` line 91
  (`var header = ...`) and line 94 (`var policy = header.Value;`). On line 91 the parameter of the lambda also has the
  name of the local that it initializes. As [RC-436] F6 read the criterion, a `var` in an added line fails it, in a test
  too. Smallest fix: `KeyValuePair<string, string> header = SecurityHeaders.All.Single(entry => entry.Key ==
  "Referrer-Policy");` and `string policy = header.Value;`.
- **F2, Fail (QC-CS-001 4): two null-forgiving operators without their reason.**
  `tests/NFHotel.Api.Tests/SecurityEventValuesTests.cs` line 39 (`GetCustomAttribute<LoggerMessageAttribute>()!`) and
  line 40 (`parameter.Name!`). The comment on line 37 says what the helper does, not why the values cannot be null, and
  [RC-436] F14 read the criterion for tests too. Smallest fix: the reason in that comment (the list holds only the
  methods that carry the attribute; every parameter of a compiled method has a name), or no operator, with
  `is { } attribute` and `parameter.Name?.Contains(...) == true`.
- **F3, Fail (QC-CS-001 13): the design still gives the events the email.** The class diagram draws
  `+requestMade(email)`, `+publicBookingMade(email)` and `+limitRefused(limit, email)` in `docs/dcd.md` (lines 409, 411
  and 412) and in its source `docs/dcd.puml` (lines 396, 398 and 399); [SD-001] draws
  `LimitRefused("email", claimed.Email)` and `PublicBookingMade(claimed.Email)` (`docs/use-cases/uc-011/sd.md` lines
  101 and 111); [OC-002] names "the limit `email`" (`docs/use-cases/uc-011/oc.md` lines 97 and 98, 163 and 164). The
  port is `RequestMade()`, `PublicBookingMade()` and `LimitRefused(string limit)` (`IBookingRequestEvents.cs:16-26`),
  and the use case writes `LimitRefused(LimitNames.Mailbox)` (`BookingRequestService.cs:348`) and `PublicBookingMade()`
  (line 212). None of these documents is in the change, and no deviation is recorded. [RC-439] F11 found the same
  drift from the side of the documents. Smallest fix: `+requestMade()`, `+publicBookingMade()` and
  `+limitRefused(limit)` in both files of the class diagram; `LimitRefused("mailbox")` and `PublicBookingMade()` in
  [SD-001]; "the limit `mailbox`" in [OC-002]; [SD-001] and [OC-002] each with a row in its Version History and a
  review.
- **F4, Fail (QC-YML-001 7 and 8, QC-WF-001 1 and 20): the summary of the load test is shell in the workflow, twice.**
  `.github/workflows/load-test.yaml` lines 44 to 52 and 60 to 68 are two `run` blocks of eight lines each, which differ
  only in the folder and the title; the limit is five, and on `main` the one block had six. Smallest fix:
  `load-test.sh` writes the titled summary itself, for example with an option `--title` whose text it appends to
  `$GITHUB_STEP_SUMMARY` with the summary when that variable is set, so each step is one call of the script; and
  `STACK_DIR` is set once, in the `env:` of the job. The new option then needs its run, as QC-SH-001 11 asks.
- **F5, Pending (QC-WF-001 22 and criterion 4 of MIL-089): the workflow as changed has not run on GitHub.** Its new
  steps (the second run, the verification turned on between the runs) and the timeout of 30 minutes for the stack and
  both runs are seen only on the hosted runner. The two runs of the evidence were made on `main` with the load test of
  task 4, before the C# changes of this branch; those changes leave the limits and the booking paths as they are, so the
  numbers are expected to hold, but they are not measured on the final code. Condition: a run of the workflow by hand on
  the branch (`workflow_dispatch`) after F4, with its summary kept as the evidence of criterion 4.
- **F6, Pending (QC-SH-001 11): the new checks of the log in `check-e2e-stack.sh` have not run.** Lines 337 to 348 run
  only against the throwaway stack, in the end-to-end job of NF Hotel CI. Read here: the five public bookings of the
  limit check come from 192.0.2.20 (lines 314 and 315), while the two bookings of the guest smoke test send no client
  address (lines 133 to 137), so the count of 5 holds; the API writes each message on a line of its own with the simple
  console formatter, so `( |$)` matches the end of the address; and the check without an email reads the lines of the
  four events of the booking flow for an `@`. Condition: the end-to-end job of the pull request green.
- **F7, Fail of an Optional criterion (QC-WF-001 24): no static test reads the length of a `run:` script.** `lint.sh`
  and the seven rules of `test-workflow-policy.sh` ([ADR-0037]) passed F4. A rule for the limit would also find blocks
  that are older than this change, `running-image-scan.yaml` line 85 among them. Not blocking; a candidate for a later
  change.
- **F8, Low, outside the checklists: the wiki page Alerts still says the events hold the email masked.**
  `docs/wiki/Alerts.md` line 99 changes the names of the limits to `mailbox` and `verification_mail` in this branch,
  but its next sentence still says that each event is a line of the log "with the address that nginx saw and the
  email masked to its first character and its domain". Since this change the line holds the address and no email.
  [RC-439] D1 records the same. Smallest fix: "with the address that nginx saw and no email, whole, masked or hashed
  ([ADR-0051] decision 6 since MIL-089)".
- **F9, Low (QC-CS-001 12, a note): the test of abuse case 6 cannot see where the save runs.**
  `CreatePublicAsync_FreeRoom_ChoosesAndSavesUnderThePublicBookingLock` (`BookingRequestServiceTests.cs:850-863`)
  asserts the name of the lock. The fake unit of work records the name and runs the work
  (`tests/NFHotel.TestSupport/ServiceWorld.cs:110-125`), so the test passes whether the room is chosen and saved inside
  the work or after it; its name promises more. It fails without the lock (`without-their-control.txt`), and the race
  itself is held in PostgreSQL by the test of MIL-088 with twenty bookings at once. Smallest fix: a name and summary
  that say what it holds (`..._TakesThePublicBookingLock`), or a second case with `FailUnderLock` set for
  `public_booking` that asserts no booking and no save.
- **F10, Low (QC-CS-001 8 and 12, notes): words that the rename left behind in the tests.**
  `SenderLimitApiTests.cs:566` names the limits "the search, the hour, the email and the guest sign-in"; the limit is
  `mailbox`. `LimitsTableTests.cs:73-74` says "the limit per email has none, and the limit per mailbox has the hour and
  the day", and lines 99 and 100 say "the limit per mailbox tells nothing about the mailbox of somebody else"; since
  the rename the limit named `mailbox` is the one without a window, and both sentences mean `verification_mail`.
  `GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheReference` (line 540) also holds that the event has no email,
  which its name does not say. Smallest fix: name the limits by their names in the three summaries, and
  `..._WithoutTheEmailOrTheReference`.
- **F11, Low (rules 9 and 11 of the coding-conventions skill, the k6 script): unnamed numbers and an address written
  twice.** `ops/load/nfhotel.js` line 89 (`86400000`), line 176 (`30` and `60`, the first night and the nights of each
  virtual guest) and line 221 (`1500`, the first night of the burst); the address of the search at lines 167 and 168,
  and 213 and 214. Smallest fix: named constants (`firstNight`, `nightsPerGuest`, `firstBurstNight`,
  `millisecondsPerDay`) and one function or constant for the address of the search.
- **F12, recorded as stand-in (QC-SH-001 15): a fourth reader of a key of `stack.env`.** `load-test.sh:33` reads every
  line of a key, so a key written twice fails the check of its value, and the script stops. Beside it,
  `lib/stack.sh:137` (`stack_setting`) takes the last line and strips a CR, and `dast-scan.sh` (lines 226 to 333),
  `run-e2e-tests.sh:60` and `check-e2e-stack.sh:35` read their keys inline. [RC-436] F9 kept the readers apart and named
  one reader in `lib/common.sh` as a candidate for a later change; the AI keeps this one too, as stand-in for S01: it
  is fail-closed, the stack writes each key once, and sourcing `lib/stack.sh` would bring in `lib/common.sh` and the
  rest of the stack. S01 may decide otherwise.
- **F13, notes that need no change.**
  - No security event can hold an email or another value that a caller typed. The events take the address of the
    caller, which `HttpCallerAddress` parses and writes in its normal form or as `-`
    (`HttpCallerAddress.cs:56`), the name of a limit (a constant of `LimitNames`; the filters and the use case pass
    nothing else), the kind of a mail and a reason from a closed set; `input_refused` holds the method, the route
    template, the status and the codes. The new test reads all 8 messages, and two tests of the API find no `@` in
    the whole entry.
  - No test that cannot fail: the reflection of `Events_EveryLogMessage_TakesNoEmail` is held by the count of 8, and
    `ReferrerPolicy_Value_SendsNoAddressToAnotherSite` fails if the header leaves `SecurityHeaders.All` or changes.
  - The rename is complete in the code: no `limit="email"`, `LimitNames.Email`, `MaskEmail` or `EmailOf` is left in
    `src/`, `tests/`, `ops/`, the rules of Prometheus or the wiki, and no dashboard reads the events. The words that
    remain are in the records and the evidence of MIL-087 and MIL-088, in the design (F3) and in the page Alerts (F8).
    "The limit per email" stays as the name of the concept, as [ADR-0051] decision 3 and the business rules use it;
    only the name of the event and the label changed. The series of Prometheus with `limit="email"` that production
    already holds stay until their retention ends; no rule or dashboard reads that label.
  - The order of the two middlewares (`Program.cs:62-67`): a response runs its `OnStarting` callbacks in the reverse
    order of their registration, so the headers of the page of the link, registered first, are written last; the comment
    says so, and `Answers_OfThePage_CarryNoReferrerAndNoStore` holds it with a single `no-referrer`. nginx sets no
    referrer policy of its own.
  - Changes outside the milestone, with no change of behaviour: `BookingRequestCleanupWorkerTests.cs:74` uses
    `IsType<OperationCanceledException>(thrown, exactMatch: false)` for `IsAssignableFrom`, the same assertion
    (`ReturnUrlTests.cs:75` keeps the older form); two unused `using` lines left `SenderLimitApiTests.cs` and one left
    `BookingRequestApiTests.cs`.
  - The pace of the load test changed with task 4: a virtual guest opens the three pages one after the other with one
    pause after them, where `main` paused after each page (`nfhotel.js:161-165`), and the thresholds of duration and
    failures count only the guests (lines 64 and 65), so the planned refusals of the burst are not failures. Both fit
    decision 5; the first is not said in a comment.
  - `test-check-log-privacy.sh:73-78`: the near miss of the fixture, a masked email, is gone because the API writes
    none; the case still shows that a line without an email of the tests does not match.

## Overall Verdict

No-Go. Seven Mandatory criteria fail and no Risk Exception covers them: QC-CS-001 3, 4 and 13 (F1 to F3), and
QC-YML-001 7 and 8 with QC-WF-001 1 and 20 (F4). Each fix is small: two lines in each of two tests, a few lines in
each of four design documents, and the summary of the load test moved from the workflow into `load-test.sh`. The
behaviour of the change is sound: no security event can hold an email or another typed value, the limit per mailbox is
renamed in the code, the rule tests and the checks of the stack, the referrer policy reaches every page while the page
of the link keeps `no-referrer`, and the load test measures the booking flow in both modes with a burst that gets only
the refusals of the limits. After the fixes a delta re-review of the seven criteria is due; while F5 and F6 are pending
it can give Go-with-conditions at most. F7 fails an Optional criterion and does not block. F8 to F11 are Low; F12 is a
deviation kept as stand-in. The reviewer is not independent: the AI that wrote the code wrote this record, as stand-in
for S01 in autonomous mode, and S01 may challenge any decision of it, F12 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| F1: explicit types in `SecurityHeadersTests.cs` lines 91 and 94, and a lambda parameter of its own name | S01 | Before the pull request is opened |
| F2: the reason of each null-forgiving operator in `SecurityEventValuesTests.cs` lines 39 and 40, or no operator | S01 | Before the pull request is opened |
| F3: the events without the email and the limit `mailbox` in `docs/dcd.md`, `docs/dcd.puml`, [SD-001] and [OC-002], the last two with a row in their Version History and a review, as [RC-439] F11 also asks | S01 | Before the pull request is opened |
| F4: the titled summary written by `load-test.sh`, one call per step in `load-test.yaml`, `STACK_DIR` in the `env:` of the job, and a recorded run of the changed script | S01 | Before the pull request is opened |
| A delta re-review of QC-CS-001 3, 4 and 13, QC-YML-001 7 and 8 and QC-WF-001 1 and 20 after the fixes | S01 | After the fixes |
| F5: the load test workflow run by hand on the branch, its summary kept as the evidence of criterion 4 | S01 | Before the merge |
| F6: the end-to-end job of the pull request green, with the new checks of the log | S01 | Before the merge |
| The code scan of the pull request: the alerts 677 to 681 closed by the code, not dismissed, and no new alert in the changed code | S01 | Before the merge |
| The third version of [ADR-0051] and the new row of [ADR-0052], which this code implements, Go in their review | S01 | Before the merge |
| F7: a rule for the length of a `run:` script in the static test of the workflows, with the older blocks it finds | S01 | A later change |
| F8: the sentence of `docs/wiki/Alerts.md` line 99 ([RC-439] D1) | S01 | Before the re-review of [RC-439] |
| F9 to F11: the name or the second case of the test of the lock, the words of the tests, and the numbers and the address of the k6 script | S01 | With the fixes of F1 and F2, or a later change |
| F12: keep the fourth reader of `stack.env`, or one reader in `lib/common.sh` | S01 | At the review of the pull request |
| Accept this record after the delta re-review, or return it | S01 | At the review of the pull request |

## Delta review

A second pass, in the same worktree, still uncommitted, after the fixes of the findings above and the fix of finding
BF-02 of [SAR-002] that the first pass left out. Its checks ran on 2026-11-21; on 2026-11-26, when this section was
written, no file of the worktree had changed since. Reason: the No-Go of the first pass, whose sections above stay as
that pass left them (row 1 of the Version History). It covers the seven criteria that failed there (QC-CS-001 3, 4 and
13, QC-YML-001 7 and 8, QC-WF-001 1 and 20), every criterion of the four checklists for the files that changed or came
since the first pass, and the status of F1 to F12.

- New: `src/NFHotel.Web/Security/CircuitLimit.cs`, `src/NFHotel.Web/Security/CircuitLimitMiddleware.cs` and
  `tests/NFHotel.Web.Tests/CircuitLimitTests.cs`.
- Changed since the first pass: `src/NFHotel.Web/Program.cs` (lines 52 and 83); the tests of F1, F2, F9 and F10, with
  `tests/NFHotel.TestSupport/ServiceWorld.cs`; `ops/scripts/load-test.sh` and `.github/workflows/load-test.yaml` (F4);
  `ops/load/nfhotel.js` (F11); `ops/deploy/nginx/templates/public.conf.template` with `ops/tests/test-nginx.sh`;
  `ops/deploy/monitoring/prometheus/rules/security.yml` with `rules-test.yml`; the fifth transcript of
  `docs/sqa/evidence/mil-089/without-their-control.txt`.
- Read for the trace only: the class diagram, [SD-001] and [OC-002] (F3) and the page Alerts (F8); their review is
  [RC-439]'s. `src/NFHotel.Web/appsettings.json` is unchanged: the number of circuits is a constant, as decision 8 of
  [ADR-0051] says.
- Reviewer and mode as in the first pass: AI as stand-in for S01, autonomous, not independent.

### Checks of the delta

| Check | Result |
| --- | --- |
| `dotnet test --project tests/NFHotel.Web.Tests -c Release`, here | 993 passed, none failed: the 981 of the first pass and the twelve cases of `CircuitLimitTests`. The build treats warnings as errors (`Directory.Build.props:7`) |
| The classes of the Api and Application tests that the fixes touch, here (`--filter-class`) | Api 53 (`SecurityEventValuesTests`, `SenderLimitApiTests`, `LimitsTableTests`, `BookingRequestApiTests`, `BookingRequestCleanupWorkerTests`) and Application 55 (`BookingRequestServiceTests` with the new case of F9, `GuestBookingServiceTests`) passed, none failed |
| `dotnet format whitespace NFHotel.sln --verify-no-changes` on the ten C# files of the delta, here | exit 0; no file changed |
| `**/packages.lock.json` | unchanged by these runs, and no change against `main` |
| `load-test.sh` here with the fake `docker` of the first pass and `GITHUB_STEP_SUMMARY` set | A run that passes: exit 0, the summary under its title on the page. k6 exits 99: exit 1, the summary under its title all the same. An empty title, or no `GITHUB_STEP_SUMMARY`: nothing on a page, exit 0. A title with `$(...)` and backticks: written as text. `--title` without a text: the usage line, exit 2. No argument of docker held the token or the password, and the secrets folder was removed |
| The author's recorded run of `--title`, before and after lines 94 to 101 of `load-test.sh` took their present form | The same page both times |
| `bash ops/tests/lint.sh` (author), after that change | Clean: the check of the layout, actionlint, shellcheck, shfmt and yamllint. The first form of those lines, a `printf` format that held the fence, had given SC2016 (info) |
| Here: actionlint 1.7.12 (without shellcheck) on the workflow; yamllint 1.37.1 with `ops/.yamllint` and PyYAML 6.0.3 with the loader that refuses duplicate keys on the three YAML files; `bash -n` on the two scripts; `node --check` on a copy of `nfhotel.js` as a module | exit 0 each; one document per YAML file and no duplicate key |
| Here: actionlint 1.7.12 on a probe workflow that sets `STACK_DIR: ${{ runner.temp }}/e2e-stack` in the `env:` of its job | Refused: the context `runner` is not allowed in the `env:` of a job |
| Line endings, trailing spaces, the final newline and a byte-order mark in the files of the delta, here | LF only, no trailing space, one final newline, no mark |
| `bash ops/tests/test-nginx.sh` (author) | 59 passed, "the public listener waits 60 seconds for a slow client" among them |
| `bash ops/tests/check-monitoring.sh` (author) | 7 passed, `promtool test rules` with the new test of the refusals of the web application among them |
| Case 11 without its control (author, the fifth transcript of `without-their-control.txt`) | Without `app.UseCircuitLimit();` the test of the web application fails (OK where TooManyRequests is expected); with the line back it passes, and the file was compared with its copy afterwards |
| `test-workflow-policy.sh`, `test-check-log-privacy.sh` and `test-dast-rules.sh` (author, after the fixes) | Passed; 10 passed; 33 passed |
| Not run here | Anything that needs Docker: `lint.sh`, `test-nginx.sh`, `check-monitoring.sh`, `check-e2e-stack.sh` with the checks of F6, the browser tests, the Infrastructure tests and the load test of F5 |

### The findings of the first pass

| Finding | Status | Evidence/Notes |
| --- | --- | --- |
| F1 | Fixed | `SecurityHeadersTests.cs` lines 91 and 94 declare `KeyValuePair<string, string> referrer` and `string policy`, and the parameter of the lambda has a name of its own |
| F2 | Fixed | `SecurityEventValuesTests.cs:39-42` has no null-forgiving operator: with `is not { } attribute` and `parameter.Name?.Contains(...) != false`, a method without the attribute, or a parameter without a name, counts as one that takes an email and fails the test; the comment on lines 37 and 38 says so |
| F3 | Fixed | `+requestMade()`, `+publicBookingMade()` and `+limitRefused(limit)` in `docs/dcd.md` (lines 409, 411 and 412) and `docs/dcd.puml` (396, 398 and 399); `LimitRefused("mailbox")` and `PublicBookingMade()` in [SD-001] (`sd.md:102` and `112`); the limit `mailbox` in [OC-002] (`oc.md:98-99` and `164-165`); a Proposed row in the Version History of each of the two (line 13). Their review is the re-review of [RC-439] |
| F4 | Fixed | `load-test.sh` writes the summary under its title itself (`--title`: lines 11 to 13, 49 to 53 and 93 to 101), also when a threshold broke, which the step on `main` did not; each `run` of `load-test.yaml` is one command (lines 40, 42 to 44, 46 and 48 to 50); the option has its recorded runs. There is no `STACK_DIR` now: the four commands name `$RUNNER_TEMP/e2e-stack` (lines 40, 43, 46 and 49) instead of a variable in the `env:` of the job, which the fix of the first pass asked for and GitHub does not allow (QC-YML-001 8 below) |
| F5 | Pending | Unchanged: the workflow runs by hand on the branch after the push |
| F6 | Pending | Unchanged: the end-to-end job of the pull request runs the checks of `check-e2e-stack.sh:337-348` |
| F7 | Open (Optional) | Not built; a later change for S01, as its action item says |
| F8 | Fixed | `docs/wiki/Alerts.md:101`: each event is a line of the log "with the address that nginx saw and no email, whole, masked or hashed (since MIL-089)"; the same paragraph adds the refusals of the circuits by the web application |
| F9 | Fixed | `CreatePublicAsync_LockNotTaken_BooksAndSavesNothing` (`BookingRequestServiceTests.cs:871-884`) sets `FailUnderLock` for `public_booking` and finds no booking and no save; the first case (line 850) also asserts that every save ran under the lock, through the new `FakeUnitOfWork.SavesUnderLock` (`ServiceWorld.cs:80-81` and `127-135`) |
| F10 | Fixed | The summaries name the limits `mailbox` and `verification_mail` (`SenderLimitApiTests.cs:566`, `LimitsTableTests.cs:73-74` and `99-100`), and the test at line 540 is `GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheEmailOrTheReference` |
| F11 | Fixed | `nfhotel.js:25-33` names `firstNightOfTheGuests`, `nightsPerGuest`, `firstNightOfTheBurst`, `searchFrom`, `searchNights` and `millisecondsPerDay`, with a comment; one function, `search(address, params)` (lines 155 to 162), holds the address of the search for the guests (line 185) and for the burst (line 228). The numbers left are status codes, the stages and the thresholds |
| F12 | Kept as stand-in | As recorded; S01 may decide otherwise |

### Checklist Results: QC-CS-001 (delta)

`CircuitLimit.cs`, `CircuitLimitMiddleware.cs`, `CircuitLimitTests.cs` and the lines 52 and 83 of `Program.cs`, with the
tests of F1, F2, F9 and F10 and `ServiceWorld.cs`.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | `CircuitLimit`, `CircuitLimitMiddleware`, `UseCircuitLimit`, `TryOpen`, `Close`, `OpenOf` and `CallerCount`; the constants `OpenPerCaller`, `CircuitPath` and `LimitName`; the private fields `_open` and `_lock`; the locals `limit`, `logger`, `events`, `address` and `caller`; `SavesUnderLock` (`ServiceWorld.cs:81`) |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | One type per file, named after it; no async method is added under `src/` (the step is a lambda); the async helpers of the tests end in `Async` (`SendAsync`, `EndCircuitsAsync`), and the test methods are named as criterion 12 asks, as in the first pass |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | F1 is fixed, and no `var` is in the new files or in an added line of the delta. The build with warnings as errors and the whitespace check of the ten C# files are clean; no suppression is added |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | F2 is fixed. The four `null!` of `CircuitLimitTests.cs:107-110` pass the null that the guards must refuse, as the comment on line 106 says; a request without an address is written as `-` (`CircuitLimitMiddleware.cs:82`) |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | Pass | Nothing disposable is made under `src/`: the factory owns the meter (`CircuitLimitMiddleware.cs:62`); the tests dispose the pipeline, the factory, the client and the answers with `using` |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | Pass | `await next(context).ConfigureAwait(false)` (`CircuitLimitMiddleware.cs:74` and `91`); no `.Result`, `.Wait()` or `async void` |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | `ArgumentNullException.ThrowIfNull` on every public member (`CircuitLimit.cs:52`, `72` and `97`, `CircuitLimitMiddleware.cs:57`); closing a request that was never counted throws `InvalidOperationException` (`CircuitLimit.cs:76-79`); `finally` gives the place back on every way out of the request (`CircuitLimitMiddleware.cs:89-96`); no catch |
| 8 | Public types and members have XML documentation comments | Pass | Every public type and member has its summary, with the decision and the finding it implements (`CircuitLimit.cs:3-13`, `CircuitLimitMiddleware.cs:7-13`); the new property of the fake has one (`ServiceWorld.cs:80`) |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | Constants, the read-only property `CallerCount` and `readonly` fields; the count is private and behind the lock |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | N-A | No money and no point in time |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | N-A | No public async API: the step passes the request on |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | `CircuitLimitTests`, nine methods and twelve cases: fifty counted and the next refused; places given back and the caller forgotten; 400 attempts at once that count exactly the limit of each caller; the guards; the step with 429, the event and the counter; the addresses of one IPv6 /64 as one caller, and the path in any letter case; other paths and callers that pass; a request that ends or fails; and the web application at the limit, which refuses the negotiation and still serves the page. That last test fails without `app.UseCircuitLimit();` (case 11 of the evidence). The circuits are held open by a task that the test ends, never by a delay, and the web application runs on the in-memory test server. F9 and F10 are fixed |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | F3 is fixed: the class diagram draws the port as the code has it. `CircuitLimit` and `CircuitLimitMiddleware` implement no class of the diagram, as the other security steps of the web application and the API ([RC-428] 13); their summaries cite decision 8 of [ADR-0051] and BF-02 of [SAR-002] |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge, not where it merely looks alike | Fail | D1: the web application writes the words of the security events of the API a second time, and two of them are held by no test |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | The step uses `CallerAddressKey` of Application (`CircuitLimitMiddleware.cs:3` and `79`) and `ClientAddressMiddleware` of the web application; no project reference changes |
| 16 | SOLID holds: a class has one reason to change; new behaviour is added by extension, not by editing a type test in several places; subtypes honour the contract of their base; interfaces are narrow; high-level code depends on interfaces and receives its implementations by injection | Pass | `CircuitLimit` counts, and the step maps a request to it; the step is an extension of `IApplicationBuilder`, as `UseClientAddress` and `UseSecurityHeaders` are, and takes the singleton (`Program.cs:52`), the logger and the meter once, when it is added |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure (the projects `<App>.Domain`, `<App>.Application`, `<App>.Infrastructure` and `<App>.Presentation` under `src/`, and `unit`, `integration` and `e2e` under `tests/`), in `PascalCase`, and project references between the layers point inward | Pass | `src/NFHotel.Web/Security/`, beside `ClientAddressMiddleware` and `SecurityHeaders`, and `tests/NFHotel.Web.Tests/`: the projects as the earlier reviews read them |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | `CircuitLimit.cs`, `CircuitLimitMiddleware.cs` and `CircuitLimitTests.cs` |
| 19 | Observers are notified through the `event` keyword and delegates: events are `EventHandler<TEventArgs>` (or a justified custom delegate), named in the past tense, raised only by the publisher through `Handler?.Invoke(this, e)`; there is no hand-rolled observer list and no public delegate field; a subscriber that outlives its need unsubscribes | N-A | No observer |
| 20 | Code optimised with SIMD (for example AVX2) or CUDA keeps a scalar reference implementation that a test compares with the accelerated result, guards each intrinsic path with `IsSupported` and falls back, handles the tail elements, and reaches CUDA through a port in Infrastructure; the improvement is documented (hardware and runtime, input sizes, a checked-in benchmark, scalar and accelerated times, speed-up) | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names (`Fields`, `Constructors`, `Properties`, `Events`, `Public methods`, `Protected methods`, `Private methods`, `Nested types`), each closed by a named `#endregion`, none nested and none inside a method body | Pass | `CircuitLimit` has `Fields`, `Properties` and `Public methods` (lines 16 to 105), `CircuitLimitMiddleware` has `Fields`, `Public methods` and `Private methods` (lines 16 to 109), in the standard order, each closed by a named `#endregion` and none nested; `Program.cs` holds top-level statements |

### Checklist Results: QC-SH-001 (delta)

`ops/scripts/load-test.sh` as changed since the first pass, and lines 12, 13 and 172 to 175 of
`ops/tests/test-nginx.sh`.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | `load-test.sh` lines 1 and 17, unchanged; `test-nginx.sh` keeps `set -uo pipefail` (line 29), the convention of `ops/tests`, with each check counted |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | `"### $title"`, `"$(<"$results/summary.txt")"` and `>>"$GITHUB_STEP_SUMMARY"` (`load-test.sh:94-100`); the new check quotes its expansion and reads the loaded configuration through `<<<"$loaded"` (`test-nginx.sh:172-175`) |
| 3 | Names follow the conventions: `kebab-case.sh` files, `snake_case` functions and variables, `UPPER_SNAKE` constants and environment variables | Pass | The variable `title`; `GITHUB_STEP_SUMMARY` is the runner's |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | `bash -n` here; shellcheck v0.11.0 in `lint.sh` (author) is clean since lines 94 to 101 took the form of `build-release-images.sh:224-232`; no `disable` comment |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `--title` without a text prints the usage line and exits 2 (`load-test.sh:50`), seen here; the usage line names the option (line 29) |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | Unchanged: `mktemp -d` and `trap cleanup EXIT` (`load-test.sh:72-78`); the new check writes no file |
| 7 | No secret is written in the script, echoed, or put on a command line; secrets come from the environment or a gitignored file | Pass | The title and the summary hold no secret; no argument of docker held the token or the password, seen here |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | The option appends only to the page of the job that the runner names, and only when the caller gives a title; the new check reads the configuration of its own throwaway container |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | `load-test.sh:11-13` give the option, what it adds and `$GITHUB_STEP_SUMMARY`; `test-nginx.sh:12-13` name the client timeouts |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | `load-test.sh:3` and the comment on line 93; `test-nginx.sh:13` cites abuse case 11 of [SAR-002] |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pending | F6, as in the first pass. The option `--title` has recorded runs with a fake docker, here and by the author: success, a broken threshold, no page, no text, an empty title and an odd one; `test-nginx.sh`: 59 passed (author) |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 in `lint.sh` (author), after the change |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | A second run adds a second summary under its own title, as the page of a job collects them; each run of the workflow has its own folder and title |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | Unchanged: "Needs: bash 4+, docker" (`load-test.sh:15`); `$(<file)` is bash |
| 15 | A command sequence, constant, path or message used more than once is defined once (a function, a variable or a sourced helper); a helper is shared between scripts only where it expresses the same knowledge | Pass | One block writes the summary for both runs, where the workflow wrote it twice (F4), in the form of `build-release-images.sh:224-232` and `scan-running-release.sh:86-88`. The new check reads the configuration that is read once into `loaded` (`test-nginx.sh:168`), as the check above it does and as [RC-424] read the criterion for this script |

### Checklist Results: QC-YML-001 (delta)

`.github/workflows/load-test.yaml` as changed since the first pass,
`ops/deploy/monitoring/prometheus/rules/security.yml` lines 22 to 38, and the new test of `rules-test.yml` (lines 611
to 631).

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys in any mapping | Pass | PyYAML 6.0.3 with a loader that refuses duplicate keys: one document each, no duplicate key |
| 2 | Indentation is 2 spaces everywhere, never tabs; lines end with LF; the file ends with one newline and has no trailing spaces | Pass | yamllint clean; no tab, no CR, no trailing space, one final newline |
| 3 | Passes `yamllint` (or the project's YAML linter) with its configuration committed next to the code and no unexplained `disable` comments | Pass | yamllint 1.37.1 here and 1.35.1 in `lint.sh` (author), with `ops/.yamllint`; no `disable` comment |
| 4 | A scalar that YAML would coerce to another type is quoted: `yes`, `no`, `on`, `off`, `null`, `~`, numbers with a leading zero, versions such as `1.10`, and values that contain `: ` or ` #` | Pass | The values of the new series are quoted (`rules-test.yml:618`); the pattern of the job is inside the folded expression (`security.yml:27-28`) |
| 5 | Only safe, plain types are used: no language-specific tags (`!!python/object`, `!ruby/object`) and no `!include`-style extension unless the consuming tool documents it | Pass | No tag and no include |
| 6 | No secret is written in the file: no password, token, key or private connection string; a secret is referenced by name and supplied by the consuming tool | Pass | No secret; "Secrets: none" (`load-test.yaml:11`) |
| 7 | Embedded program text (shell, Python, SQL, `jq`, and so on) in a value is at most five lines; a longer one is moved to a file of its own, in its own language, and the YAML calls it | Pass | Each `run` of `load-test.yaml` is one command: one line (lines 40 and 46) or one command folded over three lines with `>-` (lines 42 to 44 and 48 to 50) |
| 8 | A block of keys or values repeated in the file or across files is defined once (an anchor, a template, an include or a reusable unit the consumer supports) | Pass | The two blocks of eight lines of F4 are gone. The folder of the stack, `$RUNNER_TEMP/e2e-stack`, is a path in four commands of one line (lines 40, 43, 46 and 49), not a repeated block; the end-to-end action names it in the `env:` of each of its steps (`.github/actions/e2e-tests/action.yml:33`, `42` and `51`). The `env:` of the job that the first pass proposed cannot hold it, since GitHub has no context `runner` there, and actionlint refuses it. The new test of `rules-test.yml` has its own series and repeats the annotations of the alert, as promtool needs |
| 9 | Anchors, aliases and merge keys (`&`, `*`, `<<`) are used only where the consuming tool is known to support them | N-A | None |
| 10 | The file validates against the schema of the consuming tool where one exists, and the schema or tool version is stated | Pass | actionlint 1.7.7 (stated at `load-test.yaml:12`) in `lint.sh` (author) and 1.7.12 here; `promtool test rules` in `check-monitoring.sh` (author) |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision or design is recorded | Pass | `load-test.yaml:1-12` with the comment of lines 37 and 38; `security.yml:22-24` names decision 8 of [ADR-0051]; `rules-test.yml:611` names it for the new test |
| 12 | Key names follow the convention of the consuming tool; one case style is used within the file (kebab-case or snake_case, never mixed) | Pass | The keys as GitHub Actions and promtool name them |
| 13 | Multi-line strings use a block scalar (`\|` or `>`) and state the chomping indicator (`\|-`, `\|+`) where the trailing newline matters | Pass | `run: >-` for the folded commands, `expr: >-` and `series: >-`; no trailing newline matters there |
| 14 | The file name is lowercase kebab-case with one extension style (`.yml` or `.yaml`) used throughout the repository | Pass | Names unchanged |
| 15 | A change is exercised by a check that runs without the real target: a parse and schema test, or a dry run of the consuming tool | Pass | actionlint on the workflow; `promtool test rules` with 30 refusals of the circuits by `nfhotel-web` in 10 minutes, which fire `BookingFlowRefusals` (`rules-test.yml:611-631`), beside the tests of the refusals of the API. The series of the web application carry the labels `host` and `job` that the rule reads, as `rules/applications.yml:76` reads them |

### Checklist Results: QC-WF-001 (delta)

`.github/workflows/load-test.yaml` as changed since the first pass.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Pass | QC-YML-001 passes for it (above) |
| 2 | The file is in `.gitea/workflows/` or `.github/workflows/` with the extension `.yml` or `.yaml`, and the same workflow does not exist in both directories | Pass | Unchanged |
| 3 | The file declares `name`, `on` and `jobs`; `on` lists explicit events and, where it matters, `branches` or `paths` | Pass | Unchanged: lines 13, 15 to 18 and 27 |
| 4 | A header comment states the purpose, the triggers, the inputs, the secrets it needs, what it changes outside the repository, and the decision or milestone it implements | Pass | Lines 1 to 12: the purpose with both runs and the burst, the triggers, "Secrets: none", "nothing is pushed", MIL-036 task 3 and MIL-089 task 4; no inputs, as on `main` |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | The job (line 29) and its seven steps (lines 33, 39, 41, 45, 47, 51 and 59), each name unique |
| 6 | The file passes `actionlint` (or the project's workflow validation test) and the tool version is stated | Pass | actionlint 1.7.7 in `lint.sh` (author), stated at line 12, and 1.7.12 here: clean |
| 7 | Every job declares `permissions` explicitly with the least access that works (`contents: read` or none by default); write access only on the job that needs it | Pass | `permissions: contents: read` (lines 20 and 21), unchanged; `test-workflow-policy.sh` passes (author) |
| 8 | Every action used is pinned to a full commit SHA or an exact release tag, never to a branch; a moving major tag (`@v1`) is accepted only for an action published by the same organisation under a stated release policy | Pass | `actions/checkout` and `actions/upload-artifact` at full SHAs (lines 34 and 53); k6 by digest (`load-test.sh:21`) |
| 9 | No secret is written in the file; a secret is read as `secrets.NAME`, passed to a step through `env:`, never placed on a command line or echoed | Pass | No secret |
| 10 | Untrusted text (`inputs.*`, event fields such as a title, branch name or label, step outputs derived from them) is never interpolated into a `run:` script; it is passed through `env:` and used as a quoted variable | Pass | No expression in a `run:`; the commands use `$RUNNER_TEMP`, which the runner sets, and fixed folders and titles |
| 11 | A trigger that gives a run secrets or a write token on code from a fork or an untrusted branch (`pull_request_target`, `workflow_run`) does not check out or execute that code | N-A | The triggers are `schedule` and `workflow_dispatch` |
| 12 | A workflow that changes state outside the repository (release, tag, publish, sync, deploy) is started by hand or by a protected branch, and a manual run defaults to a dry run with an explicit input to apply | N-A | The workflow changes nothing outside its run: the stack lives on the runner and is removed (lines 59 to 61) |
| 13 | The token the workflow uses has only the scope the job needs, and where the host's built-in token is enough it is used instead of a personal token | Pass | The built-in token with `contents: read`, and `persist-credentials: false` (line 36) |
| 14 | Every job sets `runs-on` to an explicit label and `timeout-minutes` | Pass | `ubuntu-24.04` and 30 minutes (lines 30 and 31); that 30 minutes hold the stack and both runs is seen only on GitHub (F5) |
| 15 | `concurrency` is set where two runs would conflict: a check cancels the older run (`cancel-in-progress: true`); a publishing job queues (`cancel-in-progress: false`) | Pass | One group per workflow; a second run waits (lines 23 to 25) |
| 16 | Triggers are filtered (`branches`, `paths`, `types`) so the workflow runs only for the changes it concerns | Pass | Weekly and by hand only |
| 17 | Tools and dependencies are installed at pinned versions; a version is a workflow input with a default or a value in one place | Pass | The actions by SHA; k6 by digest in one place (`load-test.sh:20-21`) |
| 18 | A dependency cache is used where the install is slow enough to matter | N-A | The workflow installs nothing |
| 19 | A failing step names the tool, the file and the reason in its log (annotation or `::error::` line) | Pass | k6 names each broken threshold in its summary, which the page of the job now shows for a failed run too, and the artifact keeps; `load-test.sh` writes `error:` lines |
| 20 | A `run:` block is within the limit on embedded program text of criterion 7 of [QC-YML-001]; a longer script is a file (`tools/`, or a shared repository fetched at a pinned ref) that the step calls with one command, and the workflow passes its settings through `env:` | Pass | Each step calls a script with one command, and its settings are the options of that command; no expression is interpolated |
| 21 | Logic used by more than one workflow or repository is a reusable workflow (`workflow_call`) or a composite action; each input has a `description`, a `type` and a `default` where one is sensible | Pass | The stack and the load test are scripts that the end-to-end action and this workflow share |
| 22 | Every workflow feature used is verified on the instance that runs it, and the host and version are recorded; a feature the target hosts differ on is noted in the header | Pending | F5: the workflow as changed has not run on GitHub's hosted runner |
| 23 | A reusable workflow states, in its header, how a caller pins it and the script it fetches, so the workflow and its script are the same release | N-A | Not a reusable workflow |
| 24 | A static test of the repository's workflows exists and runs on every pull request: valid YAML, required keys, explicit permissions and runner, no inline script above the limit | Fail | F7 (Optional), unchanged |

### The configuration of nginx

| File | Status | Evidence/Notes |
| --- | --- | --- |
| `ops/deploy/nginx/templates/public.conf.template` | Pass | Lines 25 to 31 name `client_header_timeout`, `client_body_timeout` and `send_timeout` at 60 seconds in the public server, the defaults of nginx, so nothing changes in what it does; the comment cites abuse case 11 of [SAR-002] and decision 8 of [ADR-0051], and `test-nginx.sh:172-175` holds the three in the configuration that nginx loaded (59 passed, author) |

### The criteria of MIL-089, after the fixes

As the first pass reads them above, with two changes. Criterion 2: the tests of cases 8, 7, 2, 6 and 11 each fail
without their control (`without-their-control.txt`), and the test of case 6 now holds that every save runs under the
lock (F9). Criterion 7: the page Alerts is fixed (F8), and this review is still No-Go (D1). The others are as above.

### Findings of the delta

- **D1, Fail (QC-CS-001 14): the words of the security events are written a second time, and two of them are held by
  no test.** `CircuitLimitMiddleware.cs` lines 24 to 43, 65 to 68 and 105 to 107 write again what `SecurityEvents` and
  `SecurityMetrics` of the API define: the kind `limit_refused`, the log category, the meter `NFHotel.Security`, the
  counter `nfhotel.security.events` with its unit and description, the labels `kind` and `limit`, the `-` of no
  address, and the id 8604 with its message (`SecurityEvents.cs:20`, `32`, `53` and `155-157`;
  `SecurityMetrics.cs:17-26` and `46-49`). It is one piece of knowledge: decision 8 of [ADR-0051] asks for the
  category and the words of decision 6 and the counter of the other limits, and `BookingFlowRefusals` adds up the two
  services. Both services reference `NFHotel.External`, whose `TelemetryRegistration.Meters` already names the meter.
  [RC-436] N2 accepts a value written twice when a test on each side pins it by its value. `CircuitLimitTests` pins the
  category, the id, the message, the labels and the name of the limit by value (lines 136 to 138 and 376), but selects
  the counter by the web's own `EventsCounterName` and the meter by its factory alone (line 360), and no test holds
  the web's meter among `TelemetryRegistration.Meters`, as `Telemetry_Meters_IncludeTheMeterOfTheSecurityEvents` does
  for the API (`SenderLimitApiTests.cs:661-665`). A change of `CircuitLimitMiddleware.MeterName` or of
  `EventsCounterName` leaves every test green while the alert stops counting the refusals of the circuits; on the side
  of the API, the listener of `SenderLimitApiTests.cs:615` also selects the counter by its own constant. Smallest fix:
  either the words in one class of `NFHotel.External.Telemetry` that `SecurityEvents`, `SecurityMetrics`,
  `CircuitLimitMiddleware` and `TelemetryRegistration.Meters` read (`[LoggerMessage]` takes a constant of another
  assembly); or, as N2 accepts, the two names pinned by value: the listeners of `CircuitLimitTests.cs:360` and
  `SenderLimitApiTests.cs:615` select `nfhotel.security.events` on the meter `NFHotel.Security`, and a test of the web
  application asserts that `CircuitLimitMiddleware.MeterName` is among `TelemetryRegistration.Meters`.
- **D2, Low (rule 1 of the coding-conventions skill, a note): the lock of the count.** `CircuitLimit.cs:25` locks an
  `object`, as `MarketAnalysisRunTracker` does, while the limits of the API lock the `Lock` of .NET 9
  (`InMemorySenderLimits.cs:25`, `InMemoryGuestSignInLimits.cs:25`). Both are correct. Smallest fix, if the limits
  should be alike: `private readonly Lock _lock = new();`.
- **D3, a note that needs no change: the report of a closed tab.** Every request under `/_blazor` counts while it runs,
  the report with which the browser ends a circuit (`/_blazor/disconnect`) among them. At the limit that report gets
  429 too, and the server then keeps the circuit until its retention ends (three minutes by default in ASP.NET Core)
  instead of ending it at once. It matters only at the limit, and the step stays simple.
- **D4, a note on the review itself: the fix of F4 that the first pass proposed could not be built.** `STACK_DIR` in
  the `env:` of the job needs the context `runner`, which GitHub does not offer there (actionlint 1.7.12). The
  workflow names the folder in each command instead, which QC-YML-001 8 accepts above.

### Verdict of the delta

No-Go. F1 to F4 and F8 to F11 are fixed, and the seven criteria that failed in the first pass pass now. The fix of
BF-02 does what decision 8 of [ADR-0051] says: it counts the open requests to `/_blazor` per caller under the key of
the limits, refuses the next one over 50 with 429 before the sign-in reads its cookie, gives every place back in
`finally`, and the test of the web application fails without the line in `Program.cs`. One Mandatory criterion fails
and no Risk Exception covers it: QC-CS-001 14 (D1), whose fix is a few lines of tests or one class in
`NFHotel.External`. After D1 the best verdict is Go-with-conditions, because F5 and F6 stay Pending. F7 fails an
Optional criterion and does not block; D2 to D4 are notes. This verdict replaces the Overall Verdict of the first pass.
The reviewer is not independent: the AI wrote this record as stand-in for S01 in autonomous mode, and S01 may
challenge any decision of it, D1 and F12 above all.

### Action items of the delta

The items of the first pass for F5, F6, F7 and F12, the review of [SD-001] and [OC-002] (F3), the code scan of the pull
request and the review of [ADR-0051] and [ADR-0052] stay open as written above; the other items of the first pass are
done.

| Action | Owner | Due |
| --- | --- | --- |
| D1: the words of the security events in one class of `NFHotel.External`, or the counter and the meter pinned by value on both sides and the meter of the web application held among `TelemetryRegistration.Meters` | S01 | Before the pull request is opened |
| A delta re-review of QC-CS-001 14 after the fix of D1 | S01 | After the fix |
| D2: the `Lock` of .NET 9 in `CircuitLimit`, if the limits should be alike | S01 | With D1, or a later change |
| Accept this record after that re-review, or return it | S01 | At the review of the pull request |

### Round 3: the fixes of D1 and D2

A third pass, in the same worktree, still uncommitted, on 2026-11-26, after the fixes of D1 and D2. Reason: the No-Go
of the delta (D1), whose subsections above stay as that pass left them; row 2 of the Version History records both
passes. It checks again the criteria of QC-CS-001 that the changed lines touch, 14 above all, and the rows of
QC-SH-001 that the block of the summary in `ops/scripts/load-test.sh` touches.

- Changed since the delta, for D1 and D2: `src/NFHotel.Web/Security/CircuitLimit.cs` (line 25),
  `tests/NFHotel.Web.Tests/CircuitLimitTests.cs` (lines 11, 31 to 38, 260 to 269, 381 and 397) and
  `tests/NFHotel.Api.Tests/SenderLimitApiTests.cs` (lines 44 to 51, 624, 632, 637 and 641).
- The same as in the delta: `CircuitLimitMiddleware.cs` and `SecurityMetrics.cs`, whose times changed when the
  author's mutation run put them back, each compared with its copy. `SecurityMetrics.cs` has no change against
  `main`, and the lines of `CircuitLimitMiddleware.cs` that the delta cites hold what it says.
- `ops/scripts/load-test.sh` has not changed since the delta. It was last written on 2026-11-21, before the checks of
  the delta ran, and the delta already read lines 94 to 101 in their present form: the `echo` lines that took the
  place of the `printf` format of SC2016. Its rows are checked again below all the same.
- Reviewer and mode as before: AI as stand-in for S01, autonomous, not independent.

#### Checks of round 3

| Check | Result |
| --- | --- |
| `dotnet test --project tests/NFHotel.Web.Tests -c Release`, here | 994 passed, none failed: the 993 of the delta and the new case. `CircuitLimitTests` alone (`--filter-class`): 13 cases of ten methods, all passed |
| `dotnet test --project tests/NFHotel.Api.Tests -c Release --filter-class NFHotel.Api.Tests.SenderLimitApiTests`, here | 24 passed, none failed |
| `dotnet format whitespace NFHotel.sln --verify-no-changes` on `CircuitLimit.cs`, `CircuitLimitMiddleware.cs` and the two test files, here | exit 0; no file changed |
| `**/packages.lock.json` | unchanged by these runs, and no change against `main` |
| Line endings, tabs, trailing spaces, the final newline and a byte-order mark in those four files and `load-test.sh`, here | LF only, no tab, no trailing space, one final newline, no mark |
| The mutation run of D1 (author): its script and its log, read here | Each name was changed in its file, the test project built with warnings as errors and the tests of its class run; then the file was put back and compared with its copy. The counter of the web application renamed: `UseCircuitLimit_CallerWithFiftyOpen_RefusesTheNextWith429AndTheEvent` fails (1 of 13). The meter of the web application renamed: that test and `Telemetry_Meters_IncludeTheMeterOfTheRefusals` fail (2 of 13). The counter of the API renamed: `Events_OfTheBookingFlow_AreCountedOnceWithTheirKindAndLimit` fails (1 of 24). With the names as they are: 13 of 13 and 24 of 24, and the builds are clean |
| The meter of the API, which the mutation run left out, read here | A change of `SecurityMetrics.MeterName` leaves the listener of `SenderLimitApiTests.cs:624` without the counter, so `Events_OfTheBookingFlow_AreCountedOnceWithTheirKindAndLimit` fails, and `Telemetry_Meters_IncludeTheMeterOfTheSecurityEvents` (line 671) fails too, since `TelemetryRegistration.Meters` holds the name as a literal |
| `load-test.sh` here with the fake `docker` of the first pass, again | As in the delta: a run that passes exits 0 with the summary under its title; k6 exits 99: exit 1, with the summary under its title all the same; an empty title, or no `GITHUB_STEP_SUMMARY`: nothing on a page, exit 0; a title with `$(...)` and backticks is written as text; `--title` without a text: the usage line, exit 2. No argument of docker held the token or the password, and the secrets folder was removed |
| `bash ops/tests/lint.sh` (author) | Clean after the last change of `load-test.sh`, as the delta records: the check of the layout, actionlint, shellcheck, shfmt and yamllint |
| Not run here | As in the delta: anything that needs Docker |

#### Checklist Results: QC-CS-001 (round 3)

`CircuitLimit.cs`, `CircuitLimitTests.cs` and `SenderLimitApiTests.cs` as changed since the delta. The rows not listed
here stand as the delta table above says.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | The constants `SecurityMeter` and `EventsCounter` of both tests (`CircuitLimitTests.cs:35` and `38`, `SenderLimitApiTests.cs:48` and `51`), the test `Telemetry_Meters_IncludeTheMeterOfTheRefusals` and the private field `_lock` (`CircuitLimit.cs:25`) |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | No `var` in the three files; the builds with warnings as errors (here and in the mutation run) and the whitespace check are clean; no suppression is added |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | No null-forgiving operator is added. The four `null!` of `CircuitLimitTests.cs:117-120` pass the null that the guards must refuse, as the comment on line 115 says |
| 8 | Public types and members have XML documentation comments | Pass | The new constants and the new test have their summaries (`CircuitLimitTests.cs:31-37` and `260-263`, `SenderLimitApiTests.cs:44-50`), which say why a name is written out or what the test holds; D5 is about the words of one of them |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | Pass | The lock stays a private `readonly` field, now the `Lock` of .NET 9 (D2); the four `lock` statements (lines 36, 54, 74 and 99) are as they were |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | `Telemetry_Meters_IncludeTheMeterOfTheRefusals` (`CircuitLimitTests.cs:264-269`) holds the meter of the refusals among the meters that the telemetry sends, as the test of the API does (`SenderLimitApiTests.cs:669-675`); the listeners select the counter by the names written out. The thirteen cases pass here, and none depends on order, time or the network |
| 14 | Each piece of knowledge (a business rule, constant, format, validation or query) is defined in one place; code is merged only where it expresses the same knowledge, not where it merely looks alike | Pass | D1 is fixed as [RC-436] N2 accepts: the two names that no test held are pinned by value on both sides. Each listener selects `nfhotel.security.events` on the meter `NFHotel.Security` by the literals of its test (`CircuitLimitTests.cs:35`, `38` and `381`; `SenderLimitApiTests.cs:48`, `51` and `624`) and reads the labels by the keys `kind` and `limit` (`CircuitLimitTests.cs:397`; `SenderLimitApiTests.cs:637` and `641`), and `Telemetry_Meters_IncludeTheMeterOfTheRefusals` holds the meter of the web application among `TelemetryRegistration.Meters`. The mutation run shows that a change of the counter on either side, or of the meter of the web application, fails a test; the meter of the API is held twice (checks above). The category, the kind, the words of the message and the name of the limit were already pinned by value on both sides (`CircuitLimitTests.cs:146` and `148`; `SenderLimitApiTests.cs:41` and `665`, `BookingRequestApiTests.cs:265`). What is still written twice without a pin on each side is read by nothing (D1 below) |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names (`Fields`, `Constructors`, `Properties`, `Events`, `Public methods`, `Protected methods`, `Private methods`, `Nested types`), each closed by a named `#endregion`, none nested and none inside a method body | Pass | `CircuitLimit` keeps `Fields`, `Properties` and `Public methods` in the standard order (lines 16 to 105), each closed by a named `#endregion`; the tests are exempt |

#### Checklist Results: QC-SH-001 (round 3)

`ops/scripts/load-test.sh`, the rows that lines 93 to 101 touch. The file has not changed since the delta, and its
other rows stand as the delta table above says.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | `[[ -n "$title" && -n "${GITHUB_STEP_SUMMARY:-}" ]]` (line 94), `"### $title"`, `"$(<"$results/summary.txt")"` and `>>"$GITHUB_STEP_SUMMARY"` (lines 96 to 100); the fence is a word in single quotes, so nothing in it expands |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | `bash -n` here; shellcheck v0.11.0 in `lint.sh` (author) is clean since the `echo` lines took the place of the `printf` format that gave SC2016 (info); no `disable` comment |
| 7 | No secret is written in the script, echoed, or put on a command line; secrets come from the environment or a gitignored file | Pass | The block writes the title and the summary of k6, which hold no secret; no argument of docker held the token or the password, here |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | The block appends to the page of the job only when the caller gives a title and the runner names the page (line 94), as the header says (lines 11 to 13) |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | For this file: the option and the block ran again here with the fake docker, for success, a broken threshold, no page, an empty title, an odd title and no text (above), and the author's recorded run before and after the change of lines 94 to 101 gives the same page. The run on the runner of GitHub is F5 (QC-WF-001 22). The row stays Pending in the tables above for `check-e2e-stack.sh` until F6 |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt v3.14.1 in `lint.sh` (author), after the change |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | A second run adds a second summary under its own title, as the page of a job collects them; each run of the workflow has its own folder and title |
| 15 | A command sequence, constant, path or message used more than once is defined once (a function, a variable or a sourced helper); a helper is shared between scripts only where it expresses the same knowledge | Pass | One block writes the summary for both runs, in the form of `build-release-images.sh:224-232` and `scan-running-release.sh:86-88`, where the workflow wrote it twice (F4) |

#### Findings of round 3

- **D1, fixed.** The second way of its smallest fix: the counter and the meter are pinned by value on both sides, the
  labels are read by their keys, and a test of the web application holds its meter among
  `TelemetryRegistration.Meters` (QC-CS-001 14 above). Still written on both sides, with a pin on one side or on none:
  the id 8604 (pinned by `CircuitLimitTests.cs:146` only), the `-` of a request without an address, and the unit and
  the description of the counter. No rule, alert, dashboard or page of the wiki reads them: the alert reads the
  counter by its name and the label `kind` (`security.yml:27-28`), and the wiki finds the events in the log by the
  words `limit_refused` and the category (`docs/wiki/Booking-Flow-Protection.md:58` and `76`), which are pinned on
  both sides. They are no condition of this verdict. If the id should stay the same in both services, as the comment
  of `CircuitLimitMiddleware.cs:104` says, a test of the API can pin 8604 too.
- **D2, fixed.** `CircuitLimit.cs:25` is `private readonly Lock _lock = new();`, as in the limits of the API
  (`src/NFHotel.Infrastructure/Security/InMemorySenderLimits.cs:25` and `InMemoryGuestSignInLimits.cs:25`).
- **D3 and D4** stay notes, with nothing to change.
- **D5, Low (a note): the summary of a pin says more than its class holds.** `CircuitLimitTests.cs:31-34` says that
  a change of a constant "of the web application or of the API fails here". This class sees only the web
  application: its project references `NFHotel.Web` and `NFHotel.TestSupport`, and its listener reads only the meters
  of its own pipeline. A change on the side of the API fails in `SenderLimitApiTests`, as the mutation run shows.
  Smallest fix: "so a change of the constant of the web application fails here, as one of the API fails in
  `SenderLimitApiTests`, and not only in the alert".
- **D6, Low (a note): the summary of the meters names the API alone.** `TelemetryRegistration.cs:27-29`
  (`src/NFHotel.External/Telemetry/`, unchanged by this branch) says that the API counts the security events on
  `NFHotel.Security`. Since BF-02 the web application counts its refusals of the circuits there too, and the new test
  holds that meter among `Meters`. Smallest fix: name the web application and MIL-089 in that sentence.

#### Verdict of round 3

Go-with-conditions. D1 and D2 are fixed, and QC-CS-001 14 passes: the counter and the meter that no test held are
pinned by value on both sides, as [RC-436] N2 accepts, and the mutation run shows a test failing when one of them
changes. No Mandatory criterion fails. The conditions are F5 and F6, which only the pull request can show: the load
test workflow run by hand on the branch (QC-WF-001 22, criterion 4 of [MIL-089]) and the end-to-end job of the pull
request green with the checks of `check-e2e-stack.sh` (QC-SH-001 11). F7 fails an Optional criterion and does not
block; F12 is kept as stand-in; D3 to D6 are notes. For criterion 7 of [MIL-089] this review is now
Go-with-conditions; its other criteria are as the delta reads them. This verdict replaces the verdicts of the first
pass and of the delta, and the status stays Proposed until F5 and F6 are seen and the record is accepted. The
reviewer is not independent: the AI wrote this record as stand-in for S01 in autonomous mode, and S01 may challenge
any decision of it, F12 above all.

#### Action items of round 3

The items of the delta for D1, its re-review and D2 are done, and its last item is replaced by the last one below. The
items of the first pass that the delta left open stay as it says.

| Action | Owner | Due |
| --- | --- | --- |
| D5: the summary of `SecurityMeter` in `CircuitLimitTests.cs:31-34` limited to what the class holds | S01 | With the pull request, or a later change |
| D6: the web application in the summary of `TelemetryRegistration.Meters` (`TelemetryRegistration.cs:27-29`) | S01 | With the pull request, or a later change |
| Accept this record once F5 and F6 are seen, or return it | S01 | At the review of the pull request |

---

[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[SAR-002]: ../security-audit-2026-12-booking-flow.md
[RC-438]: ./rc-438-sar-002.md
[RC-439]: ./rc-439-mil-089-documents.md
[RC-436]: ./rc-436-mil-088-code.md
[RC-428]: ./rc-428-mil-087-code.md
[RC-424]: ./rc-424-mil-086-code.md
[RC-204]: ./rc-204-mil-036-code.md
[SD-001]: ../../use-cases/uc-011/sd.md
[OC-002]: ../../use-cases/uc-011/oc.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[MIL-090]: ../../milestones/mil-090-scan-findings-of-19-november.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
