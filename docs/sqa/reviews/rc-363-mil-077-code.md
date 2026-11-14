# RC-363 Code review of MIL-077: hostile input against the API

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-363 |
| CrossReference | [MIL-077], [RC-337], [MIL-027], [MIL-044], [ADR-0002], [ADR-0004], [ADR-0015], [ADR-0017], [ADR-0032], [ADR-0037], [QC-CS-001], [QC-SH-001], [QC-YML-001], [QC-WF-001], [RC-278], [RC-341] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | [bd11790] |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-077] tasks 1 to 4 on the branch `mil-077-hostile-input`:
  - task 1: `InputGuardFilter.cs`, `InputRules.cs` and `ApiSecurityHeaders.cs` in `src/NFHotel.Api/Security`,
    `src/NFHotel.Api/ApiErrorCodes.cs`, `Program.cs`, the controllers of bookings, guests and staff, the request
    records of `src/NFHotel.Application/Dto` with the new `SearchLimits.cs`, the search boxes of `BookingList.razor`
    and `GuestList.razor`, the tests `HostileInputTests`, `HostileInputRegressionTests`, `InputRulesTests` and
    `ApiSecurityHeadersTests`, the architecture tests of `NFHotel.Application.Tests`, and `RepositoryRoot`, moved to
    `NFHotel.TestSupport`;
  - task 2: `src/NFHotel.Api/NFHotel.Api.csproj`, the lock files of the API and its tests,
    `src/NFHotel.Api/Startup/OpenApiDocumentBuild.cs` and `tests/NFHotel.Api.Tests/OpenApiDocumentTests.cs`;
  - task 3: `ops/scripts/dast-scan.sh`, `ops/scripts/e2e-stack.sh`, `ops/tests/test-dast-rules.sh`,
    `ops/tests/check-e2e-stack.sh`, `.github/actions/dast-scan/action.yml` and `.github/workflows/e2e-dast.yaml`;
  - task 4: `ops/security/zap-rules.tsv` and the wiki pages Security-Controls, Workflows, DevOps-Lifecycle and
    Technology-Choices.
- Checklist used: [QC-CS-001] (`qc-programming-csharp`) for the C# and the two attributes of Razor markup,
  [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`) and [QC-WF-001] (`qc-workflow`) for the action and
  the workflow. The rules file and the wiki pages have no checklist of their own; they are read against the deliverable
  of [MIL-077] below.
- Scope: full review of the changed files, at 2903e09 of the branch
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig", with the merge and the approval of its CD run). [MIL-077] was
  accepted by S01 in chat on 2026-11-12 ([RC-337]).

## Checklist Results

### QC-CS-001: the C# change and the two search boxes

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | `InputGuardFilter`, `InputRules`, `ApiSecurityHeaders`, `ApiErrorCodes`, `OpenApiDocumentBuild`, `SearchLimits`; the constants `MaxTextLength`, `NoSniff`, `InvalidRequest`; `PropertiesByType` is a private static readonly field in PascalCase, as the other such fields of `src/` are |
| 2 | `Async` suffix, one type per file | Pass | One top-level type per file; the test classes keep their records and factories nested, as the tests of the folder do; the tests are named `Method_Condition_Expected`, as every test of the solution |
| 3 | Formatter, no suppressions, explicit local types | Pass | `dotnet format NFHotel.sln --verify-no-changes --include` over the 26 changed C# files: no change; no suppression added. Five `var` in new lines of `src/` were found and replaced with their types (F1); the tests keep `var`, as the test projects do ([RC-341]) |
| 4 | Nullable references, `!` justified | Pass | No `!` in `src/`. Five `!` without a reason in `HostileInputTests` were replaced (F2) |
| 5 | Disposal | Pass | Every client, request and response of the tests is disposed with `using`; `ProductionFactory` and `StandInFactory` too |
| 6 | Async throughout | Pass | `await` throughout; no `.Result`, `.Wait()` or `async void`; the middleware of the header awaits the next component |
| 7 | Specific exceptions, arguments validated | Pass | `ArgumentNullException.ThrowIfNull` on the context of the filter, the text of `IsAcceptedText` and the application of `UseApiSecurityHeaders`; a refusal is a 400 with `api.invalid_request`, never an exception; the test helpers throw `InvalidOperationException` with a message |
| 8 | XML documentation | Pass | Every public type and member, with the reason for each rule: why a NUL, a bidirectional control or an undefined enum is refused, why the date window, why the header |
| 9 | Properties, immutability | Pass | `EarliestDate`, `LatestDate` and the cache are `static readonly`; `IsRunning` is a get-only property |
| 10 | `decimal` for money, `DateTimeOffset` for points in time | N-A | No money; dates are `DateOnly`, and a `DateTime` or `DateTimeOffset` of a request is checked by its date |
| 11 | `CancellationToken` | Pass | The tests pass `TestContext.Current.CancellationToken`; no new public async API in `src/` |
| 12 | Tests | Pass | Named `Method_Condition_Expected`; the test API with no network; no sleep. Api 316, Application 585 and Web 785 tests pass. The plausible dates of the hostile-input test start on a fixed day (F11) |
| 13 | Trace to the DCD | Pass | No class of the domain design changes; the filter, the rules and the header are of the API, which the class diagram does not draw, and the request records keep their fields |
| 14 | One place for each piece of knowledge | Pass | The text limit of a search is `SearchLimits.MaxTextLength`, read by the query of the booking overview, the guest search of `GuestsController` and the two search boxes; the error codes of the API are `ApiErrorCodes`, read by the filter, the model-state answer and the exception handler; the rules of text, enums and dates are `InputRules` alone. The web's `nosniff` (MIL-033) and the API's stay apart: they change with the pages and with the API |
| 15 | References point inward | Pass | The request records reference `System.ComponentModel.DataAnnotations` of the base library and the Domain; no framework, database or web code reaches Application or Domain |
| 16 | SOLID | Pass | One filter for every action, so a new endpoint needs no change; the rules are pure functions that the filter calls; the header is one middleware |
| 17 | Layers and folders | Pass | The guard and the header in `Api/Security`, the build of the document in `Api/Startup`, the limits beside the request records, the tests beside the tests of the API |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | Pass, on condition | No file under `src/` has regions, and the new types keep that layout; the risk exception of [RC-278] for this criterion runs to 2026-12-31 |

### QC-SH-001: `dast-scan.sh`, `e2e-stack.sh`, `test-dast-rules.sh` and `check-e2e-stack.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The two scripts keep both; the two tests keep `set -uo pipefail`, the convention of `ops/tests`, so one failed check does not stop the others |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | shellcheck clean; the lines of the header file are written with `printf '%s\n'` from quoted values |
| 3 | Naming | Pass | `api_sign_in`, `zap_api`, `keep_report`, `ignored_rules`, `gate`; the constants `API_TARGET`, `API_SCAN_MINUTES` and `ANSWER_ALERT` |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 `-x` over the four files: clean; `bash -n` passes; the one new `disable` (SC2016 in `api_sign_in`) says that the variables are expanded by the shell inside the curl container |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `die` writes `error:` to standard error; the full mode names a missing OpenAPI document or a missing line of `stack.env`, the baseline refuses the document, and `--gate` prints its usage |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The work folder is `mktemp -d` with `trap 'rm -rf "$work"' EXIT`; the file of the two headers is written there with `umask 027` and removed when ZAP ends |
| 7 | No secret in the script, its output or a command line | Pass | The API token, the password of the scan account and its staff token never reach a command line: the curl container gets them by name in its environment (`-e NAME`) and writes the headers and the body to files with the builtin `printf`, and ZAP reads them from a configuration file (`-configfile`), not from its arguments; `stack.env` is written with `umask 077`. F7 is an older line of the web scan |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The scans touch only the throwaway stack on the Docker networks of the runner; ZAP joins the internal API network with `-silent`, so it sends nothing outside the stack |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The headers name the API scan, `--api-document`, `--gate`, the new lines of `stack.env` and the checks of the API network and settings |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-077] task 3; no deviation |
| 11 | Success, failure and bypass paths tested | Pass | `test-dast-rules.sh` passes 33 of 33 in `debian:12-slim`, 16 of them new: the SARIF keeps only the answers 500 of the answer alert; the gate passes a clean report, fails an answer 500, a scan that was not signed in and a High alert, and lets an IGNORE rule pass; `--gate` refuses bad arguments; the full mode needs the document and the API lines of `stack.env`, and the baseline refuses the document. `check-e2e-stack.sh` checks that the API network is internal and that `stack.env` holds the token and the account of the scan. The runs by hand ran the whole path (below) |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci`: no difference |
| 13 | Safe to re-run | Pass | `e2e-stack.sh up` removes an old stack first; each scan writes its reports anew |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs: bash 4+, docker, jq, GNU date` |
| 15 | Each sequence, constant, path or message defined once | Pass | `keep_report` serves both kinds of scan; `ignored_rules` serves the SARIF and the gate; `API_TARGET` and `ANSWER_ALERT` are defined once |

### QC-YML-001: `.github/actions/dast-scan/action.yml` and `.github/workflows/e2e-dast.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`) |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`: clean, after four long lines were folded |
| 4 | Scalars YAML would coerce are quoted | Pass | `"true"` and `"false"` stay quoted; the title of the issue is a quoted string |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | The tokens of the scan are made per run and stay in files of the runner |
| 7 | Embedded program text of five lines at most | Pass | The scan step had six lines and now has four (F3); the build step has three, the step of the issue five |
| 8 | A repeated block defined once | Pass | The four upload steps differ in their file and category only, as before; a composite action has no loop |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | actionlint 1.7.7; GitHub ran every new step of the action in the runs by hand |
| 11 | A header comment states purpose, tool and decision | Pass | Both headers name [MIL-077] and the API scan |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | `>-` for the long description, the conditions and the command of the issue; `\|` for the scripts |
| 14 | Lowercase kebab-case file names | Pass | `e2e-dast.yaml`; `action.yml` is the name GitHub requires |
| 15 | A check runs without the real target | Pass | `test-dast-rules.sh` checks the argument handling and the gate with report fixtures |

### QC-WF-001: `e2e-dast.yaml` and the action `dast-scan`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | Above |
| 2 | Location and extension | Pass | |
| 3 | `name`, `on`, `jobs` | Pass | Unchanged |
| 4 | Header comment | Pass | The workflow names the API scan with the OpenAPI document of the build; the action names [MIL-077] task 3 and the two scans of the full mode |
| 5 | Descriptive, unique names | Pass | "Set up .NET for the OpenAPI document", "Build the API for its OpenAPI document", "Upload the API scan to code scanning" |
| 6 | actionlint | Pass | actionlint 1.7.7: clean |
| 7 | Explicit least permissions | Pass | Unchanged: `contents: read` at the top, `security-events: write` and `issues: write` only on the job of the full scan |
| 8 | Actions pinned | Pass | `actions/setup-dotnet` at its full commit with `# v6.0.0` |
| 9 | No secret | Pass | |
| 10 | No untrusted text in `run:` | Pass | `inputs.app-path` and `inputs.mode` reach the scripts through `env:`; no `${{ }}` inside `run:` |
| 11 | No fork code under a privileged trigger | N-A | No `pull_request_target` or `workflow_run` |
| 12 | State changes outside the repository | Pass | The issue of a failed full scan, by schedule or by hand only, as before |
| 13 | Token scope | Pass | `github.token` with `issues: write`, unchanged |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04`, 90 minutes; the active part of the API scan stops after 45 minutes, and the whole job took 9 minutes in the run by hand |
| 15 | `concurrency` | Pass | Unchanged |
| 16 | Filtered triggers | Pass | Unchanged |
| 17 | Pinned tools | Pass | The SDK of `global.json`; the packages of the document in the lock files (`--locked-mode`); the ZAP image by digest |
| 18 | Dependency cache | N-A | One restore per weekly run |
| 19 | A failing step names the tool, the file and the reason | Pass | The gate prints a `FAIL` line for each High alert, answer 500 or signed-out answer, with its rule and address |
| 20 | `run:` within the limit | Pass | After F3 |
| 21 | Logic used in several places is shared | Pass | The logic is in `ops/scripts/`, called by the action |
| 22 | Features verified on the instance that runs them | Pass | The run by hand 43601706848 on GitHub's runner ran the SDK, the build that wrote the document, the API scan and the upload of `api.sarif` |
| 23 | Reusable workflow header | N-A | |
| 24 | A static test of the workflows on every pull request | Pass | `test-workflow-policy.sh`: the 14 workflows and actions follow [ADR-0037] |

### The documents against the deliverable of [MIL-077]

- **Security-Controls**: the row "Hostile input refused at the API" names the guard, the rules, the code
  `api.invalid_request`, the hostile-input test, the API scan, the document that the running API does not serve and
  the header; the row of the weekly scans names the API scan; A03 names the guard, the test and the API scan.
- **Workflows**: the runs, the checks of the API scan (the internal network, the document, the two tokens of an
  account of its own, what fails the job, what goes to code scanning), the two alternatives that were rejected, the
  decision and the row of the action.
- **DevOps-Lifecycle** and **Technology-Choices** name the API scan beside the baseline and the full scan.
- **The rules file** has a line with a reason and an expiry within 30 days for each rule that the first API scan
  warned (2, 40014) or found informational (10104, and 10111 with its reason widened); 10021 is fixed instead (F4); the
  header says why the answer alert 100000 is not listed.
- `test-wiki-coverage.sh` passes 117 of 117, and `dast-scan.sh --check-rules` passes.

## The criteria of MIL-077 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | `HostileInputTests` takes the endpoints from the test API and asserts that it attacked as many as the API maps. Before the fixes it failed 350 of 901 requests: 328 refusals named a .NET type, and 22 values were taken (text with a NUL, a right-to-left override or of one megabyte in a search, undefined enum values, a floor or a page below one, dates far from any date of the hotel, a NUL or an override in a room number). The final test sends 898 requests and fails none. With `POST api/rooms/search` changed to throw `InvalidOperationException` on its input, it failed 1 of 898: "plausible values -> 500, a fault" |
| 2 | Pass | `HostileInputRegressionTests`: 14 of the 21 cases of the first group failed before their fixes, then 2 of 3 (an excluded booking key of 0, a guest key below one), 1 of 2 (a public booking for 0 guests) and 2 of 3 (a benchmark session of 201 characters, a schema version of 0); all pass after. `ApiSecurityHeadersTests`: 5 of 5 failed before the header (F4) |
| 3 | Pass | The build writes `g/openapi/nfhotel-api.json` (the log of the run by hand: "Writing document named 'v1'"); `OpenApiDocumentTests` holds the document to every endpoint that the API maps and the API in Production to a 404 at `/openapi/v1.json`; the first failed when the document left out `/health`, the second when `MapOpenApi` was added |
| 4 | Pending | The run by hand 43601706848 at bc7e722: the API scan imported 67 addresses of the document, 496 in all, from 13:55 to 13:58 UTC; no High alert, no answer 500 (400: 236, 401: 1, 404: 347, 405: 188, 409: 2, 415: 39, 422: 11, 429: 40); the one 401 was the sign-in with the example values of the document, so the scan was signed in. Its warnings are handled in the rules file or fixed (F4, F5). The run by hand 43603868538 at 2903e09, and the weekly run of 2026-11-16 for the gate (task 5) |
| 5 | Pending | The documents, the rules and the linters above pass, and this review is Go-with-conditions; the checks of the pull request |

## Findings of the review

- **F1, fixed: `var` in new lines of `src/`.** Five: the loop variables and the `out` argument of `InputGuardFilter`
  and `InputRules`, and the response in `ApiSecurityHeaders`. They now have their types.
- **F2, fixed: `!` without a reason.** Five in `HostileInputTests`: the HTTP method of an endpoint is read with
  `GetRequiredMetadata`, and a missing plausible body or node throws `InvalidOperationException` with a message.
- **F3, fixed: six lines of program text in the scan step of the action.** Now four.
- **F4, fixed: no `X-Content-Type-Options` on the answers of the API (ZAP alert 10021, 12 addresses).** The sign-in
  and the rooms reach a client through nginx ([ADR-0015]), which adds no header, so the gap was real ([ADR-0032]:
  fixed, not accepted). `ApiSecurityHeaders` sets `nosniff` when each response starts, registered first, so the
  refusals of the token middleware and the answers of the exception handler get it too; `ApiSecurityHeadersTests`
  failed 5 of 5 before it.
- **F5, accepted with a line in the rules file.** 2, Private IP Disclosure, in the audit trail: it records the
  address of each call, as [ADR-0017] decided, for a Manager only. 40014, the weakness of persistent cross-site
  scripting in a JSON answer: the API returns the text that it stored as JSON with `nosniff`, and the pages encode
  every value; as WARN, a High alert of the same rule still fails the gate. 10104, informational: two answers to the
  same request differ whatever the User-Agent, on the pages by their Blazor state and antiforgery token, at the
  sign-in of the API by its throttle. 100000 is not listed: IGNORE would let an answer 500 pass.
- **F6, note: requests that the API took before and refuses now.** A page below one or a page size outside 1 to 200
  of the audit trail; a public booking for fewer than one guest, which was counted as one; a guest key or an excluded
  booking key below one; a floor below one; a date before 1900-01-01 or after 2199-12-31; a search text longer than
  `SearchLimits.MaxTextLength` (the two search boxes of the back office now stop there); a benchmark session longer
  than a name of a run and a schema version below one; an undefined enum value; a control character other than tab,
  line feed and carriage return, a bidirectional control or a lone surrogate in any text. The pages of the web
  application pass the browser tests; the mobile app calls only the sign-in and the rooms, where the floor and the
  text rules now hold too.
- **F7, note: the older sign-in of the web scan.** `sign_in` of [MIL-027] passes the password of the Manager of the
  stack as an argument of curl inside its throwaway container. The password is made for the run and the container
  lives for one call; it is outside this milestone and left as it is.
- **F8, note: the write limit.** The writes of staff and prices allow 30 calls per account a minute, so 40 attacks
  of the API scan got 429; the scan reaches those writes only in part.
- **F9, note: the web full scan.** It warns 40025 Proxy Disclosure (Medium) and reports 10027 and 90027 as
  informational without a line in the rules file, as in the weekly run of 2026-11-09. They belong to the web scan of
  [MIL-027] and [MIL-044], not to this milestone; left for S01.
- **F10, note: the linters ran by hand.** `lint.sh` cannot list the files of a worktree under the Windows temporary
  folder from WSL; shellcheck, shfmt, actionlint and yamllint ran with the images and versions of `lint.sh`, and the
  job Ops linters runs `lint.sh` itself.
- **F11, note: a fixed first day.** The plausible dates of the hostile-input test start on 2030-06-10, as in the other
  tests of the API, while the test API reads the real clock; the plausible bookings stay in the future until 2030.
- **F12, note: two test-support changes.** The pattern of `AsyncRulesTests` now allows an attribute with arguments
  in a parameter list, such as `[Range(1, 10)]`, which the changed controllers have; `RepositoryRoot` moved to
  `NFHotel.TestSupport`, so the API tests use it too.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-CS-001], [QC-SH-001], [QC-YML-001] and [QC-WF-001] passes,
criterion 21 of [QC-CS-001] on the condition of [RC-278], after F1 to F4 were fixed on the branch; criteria 1 to 3
of [MIL-077] pass. The conditions: every check of the pull request is green before the merge, and criterion 4 is
read again on the run by hand at the head and on the weekly run of 2026-11-16, in the gate (task 5). The reviewer is
not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge, a code change of the API | S01 (the AI as stand-in) | At the merge |
| Read the run by hand 43603868538 and the weekly run of 2026-11-16 with the API scan; write the gate record (task 5) | S01 (the AI as stand-in) | 2026-11-17 |
| Renew or remove the lines 2, 10104, 10111 and 40014 of the rules file before they expire | S01 | 2026-12-08 |
| Decide on the warnings of the web full scan that have no line (F9) | S01 | Open |

---

[MIL-077]: ../../milestones/mil-077-hostile-input-against-the-api.md
[RC-337]: ./rc-337-mil-077.md
[MIL-027]: ../../milestones/mil-027-dast-and-end-to-end-tests.md
[MIL-044]: ../../milestones/mil-044-security-overview.md
[ADR-0002]: ../../adr/adr-0002-validation-attributes-on-entities.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0017]: ../../adr/adr-0017-staff-passwords-and-audit.md
[ADR-0032]: ../../adr/adr-0032-what-code-scanning-shows.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-341]: ./rc-341-mil-079-code.md
[bd11790]: https://github.com/NF-Hotel/NFHotel/commit/bd11790faa51533044da78369b0789049ebd51e6
