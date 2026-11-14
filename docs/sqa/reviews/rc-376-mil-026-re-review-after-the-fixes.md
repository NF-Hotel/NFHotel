# RC-376 Re-review of the code of MIL-026 after the fixes of RC-373 to RC-375

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-376 |
| CrossReference | [MIL-026], [RC-373], [RC-374], [RC-375], [RC-380], [QC-CS-001], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: the re-review of the code of MIL-026 after the fixes of the findings of RC-373 to RC-375, and the review of the pin check of the service; Go; mode autonomous, not independent | [b371b85] |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | The checks of pull request #755: SonarQube Cloud failed the security rating of the new code on one vulnerability of the pin check (shell:S6506); `c789690` keeps the fetch on HTTPS and changes the eight lines of its code smells (IDE0028, IDE0200); N6 added; Go kept; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-026] on the branch `mil-026-gate` at `b653743`, after the fixes of the blocking findings of [RC-373], [RC-374] and [RC-375], and the check of the pin of the service that F3 of [RC-380] asked for.
  - `fddce96`: 83 files (55 changed and 4 added under `src/`, 24 changed under `tests/`). The type written instead of `var` for 227 locals under `src/` and 458 under `tests/`; a comment on each `!` that stays; the regions of 43 files under `src/`; the split files `DonutSlice.cs`, `NiceAxis.cs`, `MonthRow.cs` and `ChartCaptionModel.cs`; the comments of F7 of [RC-373]; the cleaned problem code of F10 of [RC-373] with its test.
  - `3e67e04`: `MarketAnalysisSectionView`, `MarketAnalysisChart` and `MarketAnalysisForecastChart` moved out of `MarketAnalysisViewModel.cs`, and `MarketSeedPlacement` out of `MarketSeedPlan.cs`, each to a file of its own (N1).
  - `5912442`: the four test doubles of `MarketAnalysisTestDoubles.cs` each to a file of its own (N1).
  - `f4aa8a6`: the summary of `ErrorMessages` no longer calls its texts Danish, the last comment of F7 of [RC-373].
  - `b653743`: the query of `MarketAnalysisBookingQueries` selects a named row instead of an anonymous type, which held the last `var` (N2).
  - `559fb60` and `4da4513`: `ops/tests/test-market-analysis-pin.sh`, the job `Ops market analysis pin (MIL-026)` of `.github/workflows/ops-ci.yaml`, and the job on the wiki page Workflows.
  - `3d197a8` merges `main` at `5846e91` (the gate of MIL-055, pull request #753) into the branch, without a conflict.
  - `5797e58` merges `main` at `3ca75e5` (the use cases of MIL-019 and MIL-020, pull request #756); the one conflict, in the Version History of the traceability matrix, keeps its two latest rows.
  - `c789690`: the findings of SonarQube Cloud on pull request #755 (N6).
- Checklists used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`, 21 criteria) for the C# code; [QC-SH-001] (`framework/qc/code/qc-programming-shell.md`, 15 criteria) for the pin check; [QC-WF-001] (`framework/qc/code/qc-workflow.md`, 24 criteria) and [QC-YML-001] (`framework/qc/code/qc-yaml.md`, 15 criteria) for the new job.
- Scope: a delta review. Criteria 2, 3, 4, 13 and 21 of [QC-CS-001] are checked over every file that the commits of MIL-026 created under `src/` and `tests/`: 130 files (115 C# and 15 Razor), plus the eight files that `3e67e04` and `5912442` split from them, less the test doubles file they replaced, 137 in all. The other criteria are checked on the lines that the fix commits changed; for the rest of the code the scores of [RC-373], [RC-374] and [RC-375] stand. The shared files that MIL-026 only changed are judged on the lines that MIL-026 wrote.
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent: the plan of 2026-11-14 was accepted by the AI as stand-in for S01 at S01's word in chat ("Du godkender for mig"); the same AI wrote the code, the fixes and this record.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -warnaserror -nologo --no-restore` on `3d197a8` and on `b653743`, with the XML documentation of every project under `src/` | 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes --no-restore --include` over the 130 C# files that the branch changes (with those of the merge of `main`); it checks the style and analyzer rules of `.editorconfig` (`IDE0005` among them), not only white space | exit 0 |
| `dotnet test` on `3d197a8`: Domain, Application, Api, ApiClient, External and Web | 485, 586, 318, 105, 217 and 788 tests; 0 failed |
| `dotnet test` of Infrastructure without Docker (`*MarketSeed*`) | 24 passed. The database tests (`MarketAnalysisBookingQueriesTests`, `BookingPriceTests` and the others on PostgreSQL) need Docker, which this PC does not run; they run in the job Tests and coverage of the pull request, a required check, so `b653743` cannot merge unless they pass |
| Lock files after the build and the tests | no `packages.lock.json` changed |
| Search for a declaration that starts with `var` in the 137 files, outside comments | none since `b653743`; before it, one: `MarketAnalysisBookingQueries.cs:44`, an anonymous type (N2) |
| Every null-forgiving operator in the 137 files, listed by a Roslyn syntax walk (`SuppressNullableWarningExpression`), and the comment of each line | 89 operators on 59 lines, each with a comment that says why the value is not null; none without |
| A count of the top-level types of each C# file of the 137 | one in each |
| The regions of the C# files under `src/` of the 137, by a script that checks the names, the order, the pairs, that none is nested and that none is in a method | 43 files group their members in the standard regions, 0 problems; the other 46 hold one kind of member only (positional records, constant classes, components with parameters only) |
| `bash ops/tests/test-market-analysis-pin.sh` on the branch, and four runs on a copy with one thing changed each | as is: 5 passed, exit 0 (pin `97ca890f`); the contract copy changed: FAIL, exit 1; the settings template with another commit: FAIL, exit 1; a commit that the service does not have (HTTP 404): FAIL, exit 1; an argument: `error:` and the usage line on standard error, exit 2 |
| `ops/tests/lint.sh` in WSL: actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1 and yamllint 1.35.1 | all linters clean on `4da4513` |
| `ops/tests/test-workflow-policy.sh` | PASS for the 14 workflows and actions |
| `ops/tests/test-wiki-coverage.sh` | 123 passed |
| Line endings of the changed files | 0 CR in every file |
| A run on the dev VM with the real service (task 16), and a look at the page with the sample result (task 19) | Not checked: left for the gate (N5) |

## Checklist Results

### QC-CS-001: the C# code of MIL-026

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `PascalCase` and `camelCase`, `_camelCase` fields, `I` for interfaces | Pass | Unchanged; the new files and the record `BookingRow` follow it |
| 2 | `Async` and `Exception` suffixes; one top-level type per file, named after the file | Pass | F1 of [RC-374] and [RC-375] are fixed by the split files of `fddce96`. The last files of MIL-026 with more than one top-level type were split by `3e67e04` and `5912442` (N1); the count finds one type in each of the 137 files |
| 3 | Formatted, no unexplained suppression; explicit local types, never `var` | Pass | F1 of [RC-373] and F2 of [RC-374] and [RC-375]: `fddce96` writes the types, and `b653743` removes the last `var` (N2). The format check with the style rules exits 0; no suppression is added. Lines that other milestones wrote in shared files are outside this review (N3) |
| 4 | Nullable on; every `!` justified in a comment | Pass | F2 of [RC-373] and F3 of [RC-374] and [RC-375]: 89 `!` on 59 lines, each with its comment, among them `MarketAnalysisResultMapper.cs` (the filter on `JsonValueKind.String`) and `LineChart.razor.cs` (the band) |
| 5 | `using` for disposables | Pass | Unchanged; no disposable is added |
| 6 | `await` throughout | Pass | Unchanged; no blocking call is added |
| 7 | Specific exceptions; arguments checked at public boundaries | Pass | Unchanged. F10 of [RC-373]: `FailAsync` logs the problem code through `CleanCode` (ASCII letters, digits and `_ . -`, at most 64 characters, "no problem code" when nothing is left), proven by `AnalyzeAsync_ProblemCodeWithForbiddenCharacters_IsLoggedCleaned`. F5 of [RC-374] stays open (Low) |
| 8 | XML documentation | Pass | Every new file and `BookingRow` have their summaries; the comments of F7 of [RC-373] are corrected (`fddce96`, `f4aa8a6`) |
| 9 | Properties and immutability | Pass | The moved types and `BookingRow` are records |
| 10 | `decimal` and `DateTimeOffset` | Pass | Unchanged |
| 11 | `CancellationToken` | Pass | Unchanged |
| 12 | Tests cover new behaviour; `Method_Condition_Expected`; no order, time or network | Pass | One new test for F10; the other fixes change no behaviour, and the existing tests pass on them. The database tests that read through `BookingRow` run in the CI of the pull request |
| 13 | Classes trace to the DCD; deviations recorded | Pass | The deviations are recorded in D1 |
| 14 | Each piece of knowledge once | Pass | Unchanged; each moved type is defined once, in its new file |
| 15 | References point inward | Pass | Unchanged; the moved types stay in their projects and namespaces |
| 16 | SOLID | Pass | Unchanged |
| 17 | Clean architecture folders | Pass | Each new file is in the folder of the file it came from |
| 18 | File and folder names | Pass | Each new file is named after its type, in English, without a space |
| 19 | Observers through `event` | N-A | No observer |
| 20 | SIMD or CUDA | N-A | No acceleration |
| 21 | `#region` blocks in the standard order under `src/` | Pass | F3 of [RC-373] and F4 of [RC-374] and [RC-375]: the 43 files of the types that MIL-026 created and that hold more than one kind of member group them in the standard regions, with 0 problems; the 46 others hold one kind only. The shared types that MIL-026 only changed keep the layout of their files, as G1 of [RC-378] reads criterion 21 against rule 1 of the coding conventions |

### QC-SH-001: `ops/tests/test-market-analysis-pin.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and strict mode | Pass | `#!/usr/bin/env bash` and `set -uo pipefail`; a comment explains why `-e` is left out: every check runs and is counted, and the exit code says whether one failed |
| 2 | Quoting, arrays, `[[ ]]` | Pass | The pins are read into arrays with `mapfile`; every expansion is quoted; shellcheck clean |
| 3 | Names | Pass | `kebab-case.sh`; `UPPER_SNAKE` for the paths and the address that do not change (`ROOT`, `DEPLOY`, `COPY`, `SERVICE`); `snake_case` for the rest |
| 4 | shellcheck and `bash -n` | Pass | shellcheck 0.11.0 through `lint.sh`; the one `disable` (SC2015) has its reason on the line above it |
| 5 | Errors to standard error with `error:`; usage on a bad argument | Pass | An argument gives `error:` and the usage line on standard error, exit 2. The results of the checks are PASS and FAIL lines on standard output, as in the other tests of `ops/tests`, with exit 1 when one fails |
| 6 | `mktemp` and `trap ... EXIT` | Pass | `mktemp -d` for the fetched contract, removed by `trap 'rm -rf "$work"' EXIT` |
| 7 | No secret | Pass | The service is a public repository, read without a token |
| 8 | Dry run for a change outside its directory | N-A | It changes nothing outside its temporary directory |
| 9 | Header with purpose, usage, options, environment and exit codes | Pass | Purpose, the command, the exit codes 0, 1 and 2, and what it needs; it takes no option and reads no environment variable |
| 10 | Implements a cited task | Pass | [MIL-026] and F3 of [RC-380] in the header |
| 11 | Success, failure and bypass tested | Pass | The five recorded runs of the checks table: success, three kinds of failure and a refused argument; the script has no bypass |
| 12 | shfmt | Pass | shfmt 3.14.1 through `lint.sh` |
| 13 | Safe to re-run | Pass | It only reads, and removes its temporary directory |
| 14 | Bash version and tools stated | Pass | "bash 4 or later (mapfile)", `cmp`, `curl`, `mktemp` and `sed` (`-E`, which GNU and BSD `sed` both take), since `4da4513` |
| 15 | Each path and message defined once | Pass | The paths, the address of the service and the counting are each defined once; the pin is read from the two files, not written in the script |

### QC-WF-001: the job `market-analysis-pin` of `.github/workflows/ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | See the table below |
| 2 | Directory and extension | Pass | Unchanged |
| 3 | `name`, `on`, `jobs` | Pass | Unchanged |
| 4 | Header comment | Pass | The header names the new check with [MIL-026] |
| 5 | Unique, descriptive names | Pass | `Ops market analysis pin (MIL-026)`; the steps `Check out` and `Compare the pin of the service and the contract copy with the service` |
| 6 | actionlint | Pass | actionlint 1.7.7 through `lint.sh` |
| 7 | Explicit least `permissions` | Pass | Unchanged: `contents: read` for the workflow; the job asks for nothing more |
| 8 | Actions pinned to a full commit SHA | Pass | `actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1` (v7.0.1), the pin of the other jobs |
| 9 | No secret | Pass | None is used; `persist-credentials: false` |
| 10 | No untrusted text in `run:` | Pass | The step runs a fixed command |
| 11 | No checkout of untrusted code with secrets | N-A | No `pull_request_target` or `workflow_run` |
| 12 | A state-changing workflow starts by hand or on a protected branch | N-A | It changes nothing outside the runner |
| 13 | Least token scope | Pass | The built-in token with `contents: read`, not kept by the checkout |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04` and 5 minutes |
| 15 | `concurrency` | Pass | Unchanged: the workflow cancels the older run |
| 16 | Triggers filtered | Pass | Unchanged, on purpose: the header says that the jobs of Ops CI are required checks and therefore run on every pull request |
| 17 | Tools at pinned versions | Pass | No tool is installed; `curl`, `cmp` and `sed` of the runner image `ubuntu-24.04` |
| 18 | Dependency cache | N-A | Nothing is installed |
| 19 | A failing step names the reason | Pass | A FAIL line names the file and what to change |
| 20 | `run:` within the limit | Pass | One line; the logic is in the script |
| 21 | Shared logic is reusable | N-A | One workflow uses the script |
| 22 | Features verified on the host | Pass | The same features as the other jobs of the workflow on GitHub; the first run of the job is in the checks of the pull request |
| 23 | A reusable workflow states how it is pinned | N-A | Not a reusable workflow |
| 24 | A static test of the workflows | Pass | `test-workflow-policy.sh` runs in Ops CI |

### QC-YML-001: the change of `.github/workflows/ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Safe parse, no duplicate keys | Pass | yamllint 1.35.1 (`key-duplicates`) and actionlint 1.7.7 |
| 2 | Two spaces, LF, one newline at the end | Pass | 0 CR, no tab, no trailing space |
| 3 | yamllint with its committed configuration | Pass | `-s -c ops/.yamllint` through `lint.sh` |
| 4 | Scalars that YAML would coerce are quoted | Pass | `timeout-minutes: 5` and `persist-credentials: false` are meant as a number and a boolean |
| 5 | No language-specific tags | Pass | None |
| 6 | No secret | Pass | None |
| 7 | Embedded program text at most five lines | Pass | One line |
| 8 | A repeated block defined once | Pass | The checkout step has the form of the other jobs of the file, which repeat it the same way; the job adds no other repeated block |
| 9 | Anchors only where supported | N-A | No anchor |
| 10 | Schema | N-A | actionlint checks the keys of a workflow |
| 11 | Header comment | Pass | Updated, as criterion 4 of [QC-WF-001] says |
| 12 | Key names | Pass | The keys of GitHub Actions; the job id is kebab-case like the others |
| 13 | Block scalars | N-A | No multi-line string |
| 14 | File name | Pass | Unchanged |
| 15 | Exercised by a check without the real target | Pass | actionlint and `test-workflow-policy.sh` |

## The findings of RC-373 to RC-375

| Record | Finding | Result | Evidence |
| --- | --- | --- | --- |
| [RC-373] | F1 `var` | Fixed | `fddce96`, `b653743` |
| [RC-373] | F2 `!` without a comment | Fixed | `fddce96` |
| [RC-373] | F3 no `#region` | Fixed for the types of MIL-026 | `fddce96`; the shared types follow G1 of [RC-378] |
| [RC-373] | F4 the DCD | Recorded as a deviation | D1 |
| [RC-373] | F5 the pinned service refuses the request | Fixed | Pull request #754 moved the pin to `97ca890f` ([RC-380]); the pin check fails if the pin and the contract copy part again. Production is not observed yet (F2 of [RC-380]) |
| [RC-373] | F6 criterion 16 of [MIL-026] | Open, for the gate | The sample here is still not the file of the service's contract test, and no test compares them; a change in the service's repository, or a deviation |
| [RC-373] | F7 out-of-date comments | Fixed | `fddce96` (the mapper, `MarketBookingDto`, the codes `Unauthorized` and `InputRejected`, `ErrorMessages.Describe`), `f4aa8a6` (the summary of `ErrorMessages`) |
| [RC-373] | F8 the pin written twice | Mitigated | Still in two files, but the pin check fails when they differ |
| [RC-373] | F9 [ADR-0500] and [ADR-0030] read as in force | Open, for the gate | No change |
| [RC-373] | F10 the logged problem code | Fixed | `fddce96`, with its test |
| [RC-374] | F1 three files with two public types | Fixed | `fddce96`: `DonutSlice.cs`, `NiceAxis.cs`, `MonthRow.cs` |
| [RC-374] | F2 `var` | Fixed | `fddce96` |
| [RC-374] | F3 `!` without a comment | Fixed | `fddce96` |
| [RC-374] | F4 criteria 13 and 21 | As F3 and F4 of [RC-373] | |
| [RC-374] | F5 public chart helpers without null checks (Low) | Open | No change |
| [RC-374] | F6 the kind computed twice (Low) | Open | No change |
| [RC-374] | F7 pull request #407 merged with its gate failed | Closed | Closed in [RC-374] |
| [RC-375] | F1 `ChartCaptionText.cs` with two public types | Fixed | `fddce96`: `ChartCaptionModel.cs` |
| [RC-375] | F2 `var` | Fixed | `fddce96` |
| [RC-375] | F3 `!` without a comment | Fixed | `fddce96` |
| [RC-375] | F4 criteria 13 and 21 | As F3 and F4 of [RC-373] | |
| [RC-375] | F5 the pinned service refuses the run request | Fixed | As F5 of [RC-373] |
| [RC-375] | F6 `ChartKind.Correlation` unused (Low) | Open | No change |
| [RC-375] | F7 the names of the series kept in step by hand (Low) | Open | No change |
| [RC-375] | F8 a hotel without rooms gets the text of a refusal (Low) | Open | No change |

## Findings

- **D1, deviation of criterion 13, recorded: the DCD does not draw the classes of MIL-026.** `docs/dcd.md` and `docs/dcd.puml` draw the Domain layer only. Of the Domain types that MIL-026 created, `MarketAnalysisRun` and `BookingMarketDetail` (`Entities`), `BookingMarketValues` (`ValueObjects`) and `MarketAnalysisErrors` (`Errors`) are not in it; `Booking.marketSegment` is. The sections Application, Infrastructure, Web and API are empty for every class, so the classes of MIL-026 in those layers implement no DCD, as F11 of [RC-378] found for MIL-055. Whether the DCD is brought up to date is S01's decision, the open action item of [RC-270].
- **N1, fixed: criterion 2 in files that the three records did not flag.** `MarketAnalysisViewModel.cs` held three records after the view model (row 9 of [RC-374] names `MarketAnalysisChart` but not its file), `MarketSeedPlan.cs` held the placement, and `MarketAnalysisTestDoubles.cs` held four test doubles. `3e67e04` and `5912442` give each type its file; no code changed.
- **N2, fixed: the last `var` held an anonymous type.** `MarketAnalysisBookingQueries.GetBookingsAsync` selected an anonymous type, so its local could not be given a type. `b653743` selects the private record `BookingRow` with the same six members, in the region `Nested types`; the query reads the same columns.
- **N3, note: shared files keep the `var` of other milestones.** `src/NFHotel.External/DependencyInjection.cs` (14 lines) and `src/NFHotel.Web/Common/ErrorMessages.cs` (2 lines) still declare locals with `var`, in lines that the merge of the benchmark work of MIL-019 and MIL-020 and the line-ending commit of 2026-10-18 wrote (by `git blame`). The fixes changed only the lines that MIL-026 wrote, as G1 of [RC-378] does for the regions; a conversion of the shared files belongs to that decision.
- **N4, note for S01: the new job is not a required check.** `Ops market analysis pin (MIL-026)` runs on every pull request, but the ruleset of `main` decides which checks must pass, and a change of it is S01's.
- **N5, note: tasks 16 and 19 have parts that this review cannot do.** Task 16 asks for a run on the dev VM with the real service, and task 19 for a look at the page with the sample result; both belong to the gate, after the release with the pin of #754 is deployed (F2 of [RC-380]).
- **N6, fixed: the findings of SonarQube Cloud on pull request #755.** The quality gate failed the security rating of the new code (C, where the gate asks for A) on one vulnerability: shell:S6506 at line 56 of `ops/tests/test-market-analysis-pin.sh`, where `curl -L` would follow a redirect to plain HTTP. `c789690` adds `--proto '=https'`, so the fetch and any redirect stay on HTTPS; after it the check passed 5 of 5 and `lint.sh` is clean. The same analysis listed eight code smells in lines that the fixes wrote: seven lists made with `new List<T>()` where the settings of the solution prefer a collection expression (IDE0028, `dotnet_style_prefer_collection_expression`), and one lambda that can be a method group (IDE0200, `csharp_style_prefer_method_group_conversion`); `c789690` changes the eight lines. The build has 0 warnings, the format check exits 0, and Domain (485), External (217) and the 24 seeding tests of Infrastructure pass.

## Overall Verdict

Go. Every Mandatory criterion of [QC-CS-001] passes for the code of MIL-026: the blocking findings of [RC-373], [RC-374] and [RC-375] on criteria 2, 3, 4 and 21 are fixed, and the deviations of criterion 13 are recorded (D1). The pin check passes [QC-SH-001], and its job passes [QC-WF-001] and [QC-YML-001]. No risk exception is taken. Open for the gate of [MIL-026], and not blocking this record: criterion 16 (F6 of [RC-373]), criterion 17 as a deviation, F9 of [RC-373], tasks 16 and 19 (N5) and the run on production of F2 of [RC-380]. The reviewer is not independent: the AI wrote the code, the fixes and this record, as stand-in for S01.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide criterion 16 of [MIL-026]: a change in `NF-Hotel/notebook-market-analysis` so that its contract test reads the sample of this repository, a test that fails when the two differ, or a deviation at the gate (F6 of [RC-373]) | S01 | at the gate of [MIL-026] |
| Decide whether the DCD is brought up to date for the classes of D1, or the deviation stands | S01 | 2026-12-31 |
| Make `Ops market analysis pin (MIL-026)` a required check of `main` (N4) | S01 | 2026-11-21 |
| Run a market analysis on production after the deploy of #754, and look at the page with the sample result (N5, F2 of [RC-380]) | S01 | at the gate of [MIL-026] |
| Mark [ADR-0500] and [ADR-0030] as no longer in force, and record criterion 17 as a deviation (F9 of [RC-373]) | S01 | at the gate of [MIL-026] |
| The Low findings F5 and F6 of [RC-374] and F6 to F8 of [RC-375] | S01 | with the next change of those files |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[RC-373]: ./rc-373-mil-026-code-of-tasks-13-to-16.md
[RC-374]: ./rc-374-mil-026-code-of-tasks-17-to-19.md
[RC-375]: ./rc-375-mil-026-code-of-tasks-20-and-21.md
[RC-380]: ./rc-380-mil-026-service-pin.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[RC-270]: ./rc-270-mil-026-code-of-tasks-22-to-27.md
[RC-378]: ./rc-378-mil-055-code-at-the-gate.md
[ADR-0030]: ../../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[b371b85]: https://github.com/NF-Hotel/NFHotel/commit/b371b851b7cf3d83d587639cf7533afdeaa3fc6b
