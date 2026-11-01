# RC-260 Code review of MIL-054

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-260 |
| CrossReference | [MIL-054], [ADR-0033], [QC-CS-001], [QC-SH-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [4395227] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-054] tasks 1 to 9. The new and changed tests are in `tests/NFHotel.Web.Tests` (bUnit page tests, ViewModel tests, `PageHost`, `AdminSamples`), `tests/NFHotel.Api.Tests` (`MarketAnalysisRunWorkerTests`, `SeedMarketDataCommandTests`), `tests/NFHotel.Application.Tests` (`RoomAndGuestMappingTests`, new cases in `BookingServiceTests`) and `tests/NFHotel.Domain.Tests` (`BookingMarketDetailTests`). The rest is the coverage scope in `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml`, `scripts/check-coverage-floor.sh` with `scripts/test-check-coverage-floor.sh`, and the two steps in `.github/workflows/ci.yaml`.
- Checklist used: [QC-CS-001] for the C# tests, [QC-SH-001] for the two scripts, and [QC-WF-001] for the workflow change (criteria the change touches)
- Scope: full review
- Language and domain: n/a (code)
- Language reviewer: none
- Mode: autonomous, not independent. S01 accepted MIL-054 in chat on 2026-11-01 ("Ja, godkend og byg"). The reviewer is the AI that wrote the code, so this review stays open to S01's challenge.

**Evidence run on 2026-11-01** (S01's PC, .NET 10 SDK, Release build):
- `dotnet build NFHotel.sln -c Release`: 0 warnings, 0 errors (warnings are errors).
- Tests: Web 589 passed, Application 424 passed (including the test rules in `Architecture/TestRulesTests.cs`), Api 184 passed and Domain 392 passed. The four worker tests ran eight times in a row with no failure.
- The Infrastructure tests need Docker (Testcontainers) and were not run on the PC; CI runs them.
- `bash scripts/test-check-coverage-floor.sh`: 20 checks, 0 failed. shellcheck v0.11.0 and actionlint 1.7.7 (Docker in WSL) report nothing. `bash scripts/check-coverage-floor.sh` on the Cobertura report of the last green CI run on `main` (run 41318802916) prints 82.14 % in total, Api 88.19 % and Web 61.36 %, and exits 1.
- Coverage merged from that CI report and the local runs of the changed projects. Infrastructure, External and ApiClient have no test changes, so their CI figures stand.

| Project | Lines covered | Line coverage |
| --- | --- | --- |
| NFHotel.Api | 485 of 508 | 95.5 % |
| NFHotel.Api.Contracts | 3 of 3 | 100.0 % |
| NFHotel.ApiClient | 290 of 294 | 98.6 % |
| NFHotel.Application | 1,843 of 1,869 | 98.6 % |
| NFHotel.Domain | 588 of 588 | 100.0 % |
| NFHotel.External | 982 of 1,022 | 96.1 % |
| NFHotel.Infrastructure | 1,329 of 1,369 | 97.1 % |
| NFHotel.Web | 3,529 of 3,590 | 98.3 % |
| Total | 9,049 of 9,243 | 97.9 % |

The CI run of the pull request is the measurement that counts (criterion 2 of MIL-054). Criterion 1 is checked on SonarQube Cloud after the merge.

## Checklist Results

### QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | Test classes `PascalCase`, private fields `_camelCase`, constants `PascalCase` (`RunId`, `TemporaryPassword`) |
| 2 | Async suffix, one type per file | Pass | Helpers end in `Async` where they await (`RunAsync`). Exceptions to one type per file: `RoomAndGuestPageTests.cs` and `BookingFlowViewModelTests.cs` hold two test classes of the same page, and `PageHost.cs` holds its query extensions. The test code already did this (`MarketChartComponentTests`) |
| 3 | Formatted, no unexplained suppressions, explicit types | Pass | The build has no warnings, and no suppression was added. The tests use `var`, as all existing test and source code of the repository does; per rule 1 of the coding conventions, the existing code wins |
| 4 | Nullable enabled, `!` justified | Pass | `null!` only to test a null argument (`Mappings_Null_Throw`, `Create_NoValues_Throws`), and in `_scopes = null!`, which `Worker()` sets before any use |
| 5 | `IDisposable` disposed | Pass | `PageHost` and `BunitContext` are disposed by `IAsyncDisposable` test classes; the worker and the service providers sit in `using`; the temporary file of `SeedMarketDataCommandTests` is deleted in `Dispose` |
| 6 | `await` throughout | Pass | No `.Result` or `.Wait()`; the bUnit tests are synchronous because the fakes return completed tasks |
| 7 | Specific exceptions, no empty catch | Pass | No catch in the tests |
| 8 | XML documentation | Pass | Every test has a `<summary>` naming its kind of test (positive, negative, boundary, edge, equivalence, state transition, error handling, concurrency, asynchronous behaviour) |
| 9 | Properties, immutability | Pass | Fakes and samples are `readonly` or records |
| 10 | Money and time types | Pass | `decimal` prices, `DateTimeOffset` times, `DateOnly` dates |
| 11 | Cancellation tokens | Pass | `TestContext.Current.CancellationToken` is passed (xUnit1051) |
| 12 | Tests named `Method_Condition_Expected`, no order, time or network | Pass | The test rules pass: no `Task.Delay`, `Thread.Sleep` or system clock. The worker tests wait on the end of the run's scope, not on time. `FakeClock` dates are used, and every service is faked or in memory |
| 13 | Traces to the DCD | N-A | Tests and a CI check of a plain technical milestone; no class of `src/` changed |
| 14 | DRY | Pass | `PageHost` and `AdminSamples` hold the page host and the sample rows once for every page test; the floor values are set once in the script |
| 15 | References point inward | Pass | Only test projects changed; NFHotel.Web.Tests gained the `bunit` package |
| 16 | SOLID | Pass | `PageHost` renders and signs in; the query helpers find elements; the signalling scope factory only wraps the real one |
| 17 | Clean architecture folders | Pass | Tests sit in the test project of the layer they test |
| 18 | File and folder names | Pass | English, no spaces; the scripts are `kebab-case.sh` |
| 19 | Observers through `event` | N-A | No observer added |
| 20 | SIMD or CUDA | N-A | None |
| 21 | Member regions in `src/` types | N-A | No type under `src/` changed. The long test classes have named regions, as the test rule `LongTestClasses_InEveryTestProject_HaveNamedRegions` requires |

### QC-SH-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and strict mode | Pass | `check-coverage-floor.sh` uses `set -euo pipefail`. The test script uses `set -uo pipefail` without `-e`, with the reason in its header, as `test-check-comment-language.sh` does |
| 2 | Quoting, arrays, `[[ ]]` | Pass | Every expansion is quoted; tests use `[[ ]]` |
| 3 | Names | Pass | `kebab-case.sh`, `snake_case` functions and variables |
| 4 | shellcheck and `bash -n` | Pass | shellcheck v0.11.0 (the version of `ops/tests/lint.sh`, run in Docker in WSL) reports nothing on both scripts |
| 5 | Errors to stderr, usage on bad arguments | Pass | `error:` lines to stderr; exit 2 with the usage for a bad option, a bad percent, a missing report or a report without lines |
| 6 | Temporary files | Pass | The test script uses `mktemp -d` with `trap ... EXIT` |
| 7 | No secret | Pass | None is read |
| 8 | Dry run for changes outside | N-A | The check only reads a file |
| 9 | Header comment | Pass | Purpose, usage, options, exit codes and needs |
| 10 | Implements a cited task | Pass | MIL-054 task 9 and ADR-0033 decision 5 |
| 11 | Success, failure and bypass tested | Pass | Every floor met; one project under; the total under; values exactly on and just under a floor; a line counted twice; an empty report; a missing report; bad options |
| 12 | shfmt | Pass | shfmt v3.14.1 lists every script of `scripts/`, the existing ones too: they indent with 4 spaces where `.editorconfig` says 2, and `scripts/` is not in `lint.sh`. The new scripts follow the existing `scripts/` layout (rule 1 of the coding conventions); reformatting the folder is left to a change of its own |
| 13 | Safe to re-run | Pass | It only reads |
| 14 | Bash and tools stated | Pass | bash and POSIX awk (gawk or mawk); the output is sorted with `sort` |
| 15 | Defined once | Pass | The floors are set once, at the top |

### QC-WF-001 (the criteria the change touches)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 5 | Descriptive, unique step names | Pass | "Test the coverage floor check" and "Coverage floor" |
| 6 | actionlint | Pass | actionlint 1.7.7 reports nothing on `ci.yaml` |
| 9 | No secret | Pass | None is used |
| 17 | Pinned tools | Pass | Uses only bash and awk from the runner image |
| 19 | A failing step names the reason | Pass | Each project or total under its floor is named on an `error:` line |
| 20 | `run:` within the inline limit | Pass | One line each; the logic is in the scripts |

## Lines still not covered, and why

These are the lines the merged report still shows as not run (criterion 7 of MIL-054).

- **Polling loops of three pages** (`ModelsPanel.razor` 148 to 169, `BenchmarkStart.razor` 204 to 220, `MarketAnalysisReport.razor` 206 to 224). Each loop waits three real seconds with `Task.Delay` before it calls ViewModel methods that are tested on their own. A test would have to wait on time, which the test rules forbid. Making the interval injectable is a change to `src/`, which criterion 6 does not allow for coverage alone.
- **Generated code**: `RegexGenerator.g.cs` of NFHotel.External (25 lines) and NFHotel.Web (4 lines), written by the compiler's regex source generator.
- **The Blazor shell**: `Routes.razor`, `RedirectToLogin.razor` and `Pages/Error.razor` (12 lines). These are the router, the redirect to sign-in and the error page of the host; the end-to-end tests run them, the unit tests do not.
- **Needs PostgreSQL**:
  - `ApiStartup.cs` 55 to 57 (migration on start) and 155 to 176 (writing a seed);
  - `Program.cs` 68 to 80 (the dispatch of the command line);
  - the repository and constraint lines in Infrastructure: `BookingRepository` 112 to 127, `RoomRepository`, `Repository`, `BookingOverlapConstraint`, `UnitOfWork` and `GuestRepository`.

  The seeder and the repositories are tested against PostgreSQL in `NFHotel.Infrastructure.Tests`. These branches, among them an excluded booking id and a conflict on save, are not among its cases.
- **Defensive branches that the domain prevents**:
  - `BookingReceiptViewModel`'s fallback for a failure without an error: `Result.Failure` refuses an empty error list;
  - `Book.razor`'s room step while busy;
  - the `_` arm of the sort and caption switches (`ChartCaptionText` 119 to 122);
  - `AuthenticationStateStaffTokenSource` 29 and 32, before the first render;
  - `UseCaseRunner` 61 and 85, an exception filter that the cancellation tests reach only on one side;
  - `Result.cs` 42 and 47;
  - `ModelsPanel.razor` line 90: the Done state of a download, which the panel replaces at once with the refreshed list.
- **Reachable but not tested in this milestone**, left because every floor is met with a margin:
  - `BookingService` 483 and 720 to 763: a refused room, a guest saved in a concurrency conflict, and departure cleaning skipped for a missing room or one already being cleaned;
  - `MarketAnalysisRunner` 117 to 131: a domain refusal of the stored run, and a duplicate run id;
  - single lines in `RoomService`, `GuestService`, `StaffAccountService`, `StaffAuthService`, `StaffAuthorizationService`, `BookingMapping`, `ResultResponses`, `StaffAuthentication`, the two service clients of External, `ApiGateway` and the four chart components.

## Deviations from the plan

- The plan named the `HtmlRenderer` for the page tests. The tests use **bUnit 2.11.3** instead. With the `HtmlRenderer` a page can only be rendered, so the buttons, inputs and confirmation panels, the larger part of the uncovered lines, could not be run. bUnit renders the same components and can click them. The package is a test-only reference, released 2026-09-13, and pinned in the lock file.
- No defect was found by the new tests, so no line under `src/` changed (criterion 6).

## Overall Verdict

Go: every Mandatory criterion passes. The floors are met in the merged measurement with a margin of 2.9 points in total and 5.5 points for the lowest project, and the CI run of the pull request has to pass the new floor step before the merge.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Check 95.0 % or more on SonarQube Cloud after the merge (criterion 1 of MIL-054) | S01 | 2026-11-22 |

---

[MIL-054]: ../../milestones/mil-054-test-coverage.md
[ADR-0033]: ../../adr/adr-0033-sonarqube-cloud-coverage.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[4395227]: https://github.com/NF-Hotel/NFHotel/commit/4395227efef641b6f8939f00e52c3640334b1032
