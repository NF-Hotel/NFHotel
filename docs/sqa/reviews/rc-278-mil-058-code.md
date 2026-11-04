# RC-278 Code review of MIL-058, with the findings report of the 57 CodeQL code quality findings

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-278 |
| CrossReference | [MIL-058], [QC-CS-001], [QC-PY-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; S01 accepted the plan of MIL-058 in chat on 2026-11-04 ("Ja, inkl. CodeQL CLI") | [457ff29] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-058] tasks 2 to 5, on the branch `mil-058-codeql-code-quality-findings`: the test paths of `tests/` (18 files), `tests/NFHotel.ApiClient.Tests/StubHandler.cs`, the disposables of `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs` and of four Web tests, the view models `BookingFormViewModel`, `BookingListViewModel`, `GuestListViewModel`, `RoomFormViewModel`, `RoomListViewModel` and `BookingFlowViewModel` of `src/NFHotel.Web`, `BenchmarkExport`, the Razor loops of `BenchmarkStart`, `ModelsPanel` and `PricingSettings`, `FindingsSeries` and `MarketAnalysisResultMapper` of `src/NFHotel.External`, and `JsonClient.get_json` of `ops/deploy/monitoring/scan-exporter/` (Python)
- Checklist used: [QC-CS-001] (C# and Razor) and [QC-PY-001] (the Python change)
- Scope: full review of the changed code, and the findings report of task 1 (the baseline) and task 6 (the fresh analysis)
- Language and domain: n/a (code)
- Language reviewer: none
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-04 to resolve the findings and accepted the plan; the reviewer is the AI that wrote the code, so every decision below stays open to S01's challenge.

**Evidence run on 2026-11-04** (S01's PC, Windows 11, Docker Desktop 29.7.2, WSL for the exporter test):

- Baseline: CodeQL CLI 2.27.4 with the query packs `codeql/csharp-queries` 1.11.0, `codeql/javascript-queries` 2.5.0 and `codeql/python-queries` 1.9.0, the suites `csharp-code-quality.qls`, `javascript-code-quality.qls` and `python-code-quality.qls`, run on databases built from a clean git worktree of the branch at `e02a0a8` (the code of `main` at `42b4dce` plus the planning documents). The C# database was built without a build (`--build-mode=none`); the extractor's `dotnet restore` of the solution exited with code 1 on this PC and the extractor fell back to its own download of the 150 packages from nuget.org, as it does on GitHub's runners when a restore fails. Result: 51 C# findings, 5 JavaScript findings and 1 Python finding, 57 in all, with exactly the counts per rule of the code quality tab for `42b4dce`. The first attempt, built from the checkout itself, was discarded: the checkout holds three old worktrees under `.claude/worktrees/`, which GitHub never sees, and the extractor's restore rewrote two `packages.lock.json` files, which were reverted.
- Fresh analysis after the fixes, same CLI, packs and suites, databases built from the same clean worktree. The first rerun, at `5756e58`, reported 7 C# findings: the 6 recorded below as accepted or false positive, and one new `cs/linq/missed-where` in `WebFactory.HiddenFields`, where the `Select` of F14 had left a loop with a lone `if`; commit `3e99730` filters with `Where` as well. The rerun at `3e99730` reports 6 C# findings, 5 JavaScript findings and 0 Python findings, 11 in all, each one a finding recorded below as accepted or false positive; none recorded as fixed remains, and no new finding remains.
- `dotnet restore NFHotel.sln --locked-mode` fails on this PC with NU1004 for `NFHotel.Web`, because the installed SDK is 10.0.403 and the lock files are made with the 10.0.400 of `global.json` (the reason the comment in `Directory.Build.props` gives); the restore was re-evaluated locally for the build and the tests, and the 17 rewritten lock files were reverted before every commit (`git status` shows none changed). CI restores in locked mode with the pinned SDK.
- `dotnet build NFHotel.sln --configuration Release --no-restore`: build succeeded, warnings as errors, 0 warnings.
- `dotnet test NFHotel.sln --configuration Release --no-build`: 2502 tests, 2490 passed, 12 skipped (the E2E tests without `E2E_BASE_URL`), 0 failed, 39 s; the integration tests ran against Docker.
- `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore`: no change.
- `bash ops/tests/test-scan-exporter.sh` (from WSL, because the Git Bash of Windows rewrites the container paths): ruff check and format clean, `mypy --strict` clean, pytest 32 passed, line coverage 92.63 % (floor 80 %).
- `bash ops/tests/test-wiki-coverage.sh`: 91 passed; `bash scripts/check-comment-language.sh`: exit 0.
- The checks of the pull request (NF Hotel CI, CodeQL, SonarQube Cloud) are read after the push and reported on the pull request; they are the gate of MIL-058 criterion 2 that this record cannot hold.

## Findings report

The 57 findings of the code quality tab for commit `42b4dce` (`main` on 2026-11-04), reproduced with the CodeQL CLI as the baseline. Line numbers are those of `42b4dce`. Decisions: **Fixed** (the code changed and the fresh analysis no longer reports it), **False positive** (the analysis is wrong about the code), **Accepted** (the finding is right but the code stays, with the reason). No finding was already resolved before this milestone, and none is unresolved.

| Decision | Findings |
| --- | ---: |
| Fixed | 46 |
| False positive | 1 |
| Accepted | 10 |
| Already resolved | 0 |
| Unresolved | 0 |
| **Baseline** | **57** |
| Fresh analysis of the branch | 11 (the 10 accepted and the 1 false positive) |

Per rule: `cs/path-combine` 18 fixed; `cs/local-not-disposed` 13 fixed, 1 false positive; `cs/dereferenced-value-may-be-null` 5 fixed; `cs/linq/missed-select` 6 fixed; `cs/linq/missed-firstordefault` 1 fixed; `cs/linq/missed-where` 2 fixed, 3 accepted; `cs/catch-of-all-exceptions` 2 accepted; `js/trivial-conditional` 2, `js/unneeded-defensive-code` 2 and `js/redundant-assignment` 1 accepted (third-party code); `py/ineffectual-statement` 1 fixed.

| # | Finding (rule, location) | Root cause | Decision | Change | Verification | Remaining risk |
| --- | --- | --- | --- | --- | --- | --- |
| F01 | `cs/catch-of-all-exceptions` `src/NFHotel.Api/Startup/MarketAnalysisRunWorker.cs:71` | `RunAsync` of the hosted worker catches `Exception` without a filter | Accepted | None. A hosted worker has no caller to throw to: the exception is logged with the run id and the run is marked failed with `MarketAnalysisRunner.Unavailable`; the `CA1031` pragma on the clause states that reason, and a narrower list of types would miss the programming error that must not stop the API | The worker tests of `NFHotel.Api.Tests` pass; the failed run is visible on the market analysis page and in the alert on failed runs | A defect in the runner is logged and shown, not raised; the same as before |
| F02 | `cs/catch-of-all-exceptions` `tests/NFHotel.Api.Tests/ContractParityTests.cs:254` | The helper `Throws` catches `Exception` | Accepted | None. The helper describes which exception type a call throws, for the parity table of the API client and the services; catching every type is its purpose | `ContractParityTests` pass | None: test code |
| F03 | `cs/dereferenced-value-may-be-null` `src/NFHotel.Web/Features/Admin/Bookings/BookingFormViewModel.cs:276` | `SelectedRoomId.Value` was read inside the `Any` lambda; the check `is not null` was on the property, which the lambda reads again | Fixed | `SelectedRoomId is { } selectedRoomId` binds the value once and the lambda compares with it | `BookingFormViewModelTests` pass; the fresh analysis reports nothing in the file | None |
| F04 | `cs/dereferenced-value-may-be-null` `src/NFHotel.Web/Features/Admin/Bookings/BookingListViewModel.cs:304` | `StatusFilter.Value` read inside the `Where` lambda after `StatusFilter is null` | Fixed | `StatusFilter is { } status ? _allBookings.Where(booking => booking.Status == status).ToArray() : _allBookings` | `BookingListViewModelTests` pass | None |
| F05 | `cs/dereferenced-value-may-be-null` `src/NFHotel.Web/Features/Admin/Guests/GuestListViewModel.cs:271` | `guestId.Value` read inside the lambda given to `RunAsync` | Fixed | `guestId is { } editedGuestId` chooses the update, with `editedGuestId`; otherwise the create | `GuestListViewModelTests` pass (create and update) | None |
| F06 | `cs/dereferenced-value-may-be-null` `src/NFHotel.Web/Features/Admin/Rooms/RoomFormViewModel.cs:200` | `roomId.Value` read inside the lambda given to `RunAsync` | Fixed | `roomId is { } editedRoomId` chooses the update, with `editedRoomId`; otherwise the create | `RoomViewModelTests` and `RoomAndGuestPageTests` pass | None |
| F07 | `cs/dereferenced-value-may-be-null` `src/NFHotel.Web/Features/Admin/Rooms/RoomListViewModel.cs:384` | `selectedId.Value` read inside the `FirstOrDefault` lambda after `selectedId is null` | Fixed | `SelectedRoom?.RoomId is not { } selectedId` returns null; the lambda compares with `selectedId` | `RoomListViewModelTests` pass | None |
| F08 | `cs/linq/missed-firstordefault` `src/NFHotel.Web/Features/Booking/BookingFlowViewModel.cs:612` | `Find` looped to return the first room with the id | Fixed | `rooms.FirstOrDefault(room => room.RoomId == roomId)` | `BookingFlowViewModelTests` pass | None |
| F09 | `cs/linq/missed-select` `src/NFHotel.External/MarketAnalysis/FindingsSeries.cs:255` | The loop of `ToSeries` first mapped each item to its unwrapped figure object | Fixed | `array.EnumerateArray().Select(Unwrapped)`, with the helper `Unwrapped` that names the mapping | `FindingsSeriesShapeTests` pass | None |
| F10 | `cs/linq/missed-select` `src/NFHotel.Web/Features/Admin/Benchmark/BenchmarkExport.cs:33` | The loop of `ToCsv` first mapped each result to its cells | Fixed | `results.Select(Cells)`, with the helper `Cells` that holds the cell list in the order of `Columns` | `BenchmarkExportTests` pass | None |
| F11 | `cs/linq/missed-select` `src/NFHotel.Web/Features/Admin/Benchmark/BenchmarkStart.razor:65` | The loop copied its variable (`var name = model`), a habit from before C# 5, when a loop variable was shared by the closures | Fixed | The loop uses `model` | `BenchmarkStartPageTests` pass | None: since C# 5 each iteration has its own variable |
| F12 | `cs/linq/missed-select` `src/NFHotel.Web/Features/Admin/Benchmark/ModelsPanel.razor:43` | As F11 | Fixed | The loop uses `model` | `ModelsPanelTests` pass | None |
| F13 | `cs/linq/missed-select` `src/NFHotel.Web/Features/Admin/Pricing/PricingSettings.razor:83` | As F11 (`var current = size`) | Fixed | The loop uses `size` | The Web project builds the page; `PricingSettingsViewModelTests` pass | None |
| F14 | `cs/linq/missed-select` `tests/NFHotel.Web.Tests/WebFactory.cs:75` | The loop of `HiddenFields` first mapped each match to its value, then kept the hidden inputs with an `if` | Fixed | `InputTag().Matches(html).Select(input => input.Value).Where(tag => tag.Contains("type=\"hidden\"", ...))`; the `Where` came with the second commit, after the first rerun reported the lone `if` as a missed-where | Every Web test that signs in uses `HiddenFields`; all pass | None |
| F15 | `cs/linq/missed-where` `prototypes/SecurityPrototype/Data/DbSeeder.cs:34` | The loop creates each role that does not exist | Accepted | None. The filter is `await roleManager.RoleExistsAsync(role)`, an asynchronous call that `Where` cannot take; the file is the security prototype, not the product | Not run | None: a prototype outside `src/` |
| F16 | `cs/linq/missed-where` `src/NFHotel.External/MarketAnalysis/FindingsSeries.cs:145` | The loop of `RootFigures` keeps the properties that are objects with a group and a figure | Accepted | None. The condition binds `group` and `figure`, which the body uses; a `Where` would have to compute the figure twice or carry a tuple | `FindingsSeriesShapeTests` pass | None |
| F17 | `cs/linq/missed-where` `src/NFHotel.External/MarketAnalysis/MarketAnalysisResultMapper.cs:130` | The loop kept the months with a group and a value | Fixed | `months.EnumerateArray().Select(RevenueMonth).OfType<MarketAnalysisPointDto>()`, the parse-or-null form the file already uses for suggestions and forecast months | `MarketAnalysisResultMapperTests` pass | None |
| F18 | `cs/linq/missed-where` `src/NFHotel.External/MarketAnalysis/MarketAnalysisResultMapper.cs:264` | The loop kept the observations with a series and a text | Fixed | `list.EnumerateArray().Select(Observation).OfType<MarketAnalysisObservationDto>()` | `MarketAnalysisResultMapperTests` pass | None |
| F19 | `cs/linq/missed-where` `src/NFHotel.Web/Features/Admin/Rooms/RoomListViewModel.cs:467` | The loop keeps the prices whose size parses | Accepted | None. The condition is `Enum.TryParse` with an `out` variable the body uses; a `Where` would parse twice | `RoomListViewModelTests` pass | None |
| F20 | `cs/local-not-disposed` `ops/tests/NFHotel.Proxmox.Tests/ReadOnlyHandlerTests.cs:35` | `OkHandler.SendAsync` creates the response inside `Task.FromResult` | False positive | None. The response is handed to the `HttpClient` of the test, whose caller disposes it (`using var response`); the analysis does not follow a value through `Task.FromResult` | `ReadOnlyHandlerTests` pass | None |
| F21 | `cs/local-not-disposed` `tests/NFHotel.ApiClient.Tests/StubHandler.cs:39` | `Json` created the response inside `Task.FromResult` | Fixed | The responder is `Func<HttpRequestMessage, HttpResponseMessage>`; `Json` returns the response, `SendAsync` returns what the responder gives | `ServiceRequestTests` and the other ApiClient tests pass | None: the client under test disposes the response |
| F22 | `cs/local-not-disposed` `tests/NFHotel.ApiClient.Tests/StubHandler.cs:49` | As F21, `Text` | Fixed | As F21 | As F21 | None |
| F23 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:19` | The `StringReader` was created in the `Parse` call and never disposed | Fixed | `using var reader = new StringReader(text)` | `MarketSeedCsvAndPricingTests` pass | None |
| F24 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:36` | As F23 | Fixed | As F23 | As F23 | None |
| F25 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:47` | As F23 | Fixed | As F23 | As F23 | None |
| F26 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:62` | As F23 | Fixed | `using var badDateReader` | As F23 | None |
| F27 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:63` | As F23 | Fixed | `using var shortRowReader` | As F23 | None |
| F28 | `cs/local-not-disposed` `tests/NFHotel.Infrastructure.Tests/Seeding/MarketSeedCsvAndPricingTests.cs:64` | As F23 | Fixed | `using var emptyReader` | As F23 | None |
| F29 | `cs/local-not-disposed` `tests/NFHotel.Web.Tests/AdminAccessTests.cs:416` | The `FormUrlEncodedContent` of the sign-out was never disposed; `HttpClient` does not dispose the request content | Fixed | `using var form` | `AdminAccessTests` pass | None |
| F30 | `cs/local-not-disposed` `tests/NFHotel.Web.Tests/AdminAccessTests.cs:437` | As F29, created in the `PostAsync` call | Fixed | `using var emptyForm` | `AdminAccessTests` pass | None |
| F31 | `cs/local-not-disposed` `tests/NFHotel.Web.Tests/HttpsRequirementTests.cs:120` | As F30 | Fixed | `using var emptyForm` | `HttpsRequirementTests` pass | None |
| F32 | `cs/local-not-disposed` `tests/NFHotel.Web.Tests/StaffPasswordPageTests.cs:205` | As F30 | Fixed | `using var form` | `StaffPasswordPageTests` pass | None |
| F33 | `cs/local-not-disposed` `tests/NFHotel.Web.Tests/WebFactory.cs:66` | As F30, in `SignInAsync` | Fixed | `using var form` | Every Web test that signs in passes | None |
| F34 | `cs/path-combine` `tests/NFHotel.Api.Tests/SeedMarketDataCommandTests.cs:36` | `Path.Combine` returns a later absolute part alone; the analysis flags every call | Fixed | `Path.Join`; the second part is a constant | `SeedMarketDataCommandTests` pass | None: no part comes from a user |
| F35 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/AsyncRulesTests.cs:119` | As F34 | Fixed | `Path.Join` | `AsyncRulesTests` pass | None |
| F36 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/AsyncRulesTests.cs:122` | As F34 | Fixed | `Path.Join` | `AsyncRulesTests` pass | None |
| F37 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/DatabaseIsolationTests.cs:85` | As F34 | Fixed | `Path.Join` | `DatabaseIsolationTests` pass | None |
| F38 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/DatabaseIsolationTests.cs:88` | As F34 | Fixed | `Path.Join` | `DatabaseIsolationTests` pass | None |
| F39 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/ErrorCodeTextTests.cs:17` | As F34 | Fixed | `Path.Join` | `ErrorCodeTextTests` pass | None |
| F40 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/RepositoryRoot.cs:10` | As F34 | Fixed | `Path.Join` | Every architecture test that reads the repository passes | None |
| F41 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/StructureTests.cs:153` | As F34 | Fixed | `Path.Join` | `StructureTests` pass | None |
| F42 | `cs/path-combine` `tests/NFHotel.Application.Tests/Architecture/TestRulesTests.cs:80` | As F34 | Fixed | `Path.Join` | `TestRulesTests` pass | None |
| F43 | `cs/path-combine` `tests/NFHotel.E2E.Tests/BrowserTest.cs:42` | As F34 | Fixed | `Path.Join` | The E2E project builds; the tests are skipped without a target | None: the name of a screenshot is the test's own |
| F44 | `cs/path-combine` `tests/NFHotel.E2E.Tests/BrowserTest.cs:45` | As F34 | Fixed | `Path.Join` | As F43 | None |
| F45 | `cs/path-combine` `tests/NFHotel.E2E.Tests/E2ESettings.cs:33` | As F34 | Fixed | `Path.Join` | As F43 | None |
| F46 | `cs/path-combine` `tests/NFHotel.External.Tests/MarketAnalysis/FindingsSeriesShapeTests.cs:209` | As F34 | Fixed | `Path.Join` | `FindingsSeriesShapeTests` pass (they read the test data) | None |
| F47 | `cs/path-combine` `tests/NFHotel.External.Tests/MarketAnalysis/MarketAnalysisResultMapperTests.cs:299` | As F34 | Fixed | `Path.Join` | `MarketAnalysisResultMapperTests` pass | None |
| F48 | `cs/path-combine` `tests/NFHotel.External.Tests/MarketAnalysis/MarketAnalysisServiceClientTests.cs:499` | As F34 | Fixed | `Path.Join` | `MarketAnalysisServiceClientTests` pass | None |
| F49 | `cs/path-combine` `tests/NFHotel.External.Tests/MarketAnalysis/MarketContractFile.cs:18` | As F34 | Fixed | `Path.Join` | The contract tests pass | None |
| F50 | `cs/path-combine` `tests/NFHotel.Infrastructure.Tests/Integration/Persistence/MarketAnalysisBookingQueriesTests.cs:189` | As F34 | Fixed | `Path.Join` | `MarketAnalysisBookingQueriesTests` pass (integration, Docker) | None |
| F51 | `cs/path-combine` `tests/NFHotel.Infrastructure.Tests/Integration/Persistence/MarketDataSeederTests.cs:249` | As F34 | Fixed | `Path.Join` | `MarketDataSeederTests` pass (integration, Docker) | None |
| F52 | `js/redundant-assignment` `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js:2227` | Third-party code: the Popper part of the published Bootstrap 5.3.3 bundle assigns `offsetParent` to itself | Accepted | None. The file is the published build of Bootstrap, vendored under `wwwroot/lib`; a change by hand is lost at the next upgrade. S01 dismisses it on the tab as third-party code; `ops/load/nfhotel.js`, the project's own JavaScript, has no finding | The fresh analysis reports the same 5 findings and no other JavaScript finding | None for NF Hotel: the code runs unchanged in every site that ships Bootstrap 5.3.3 |
| F53 | `js/trivial-conditional` `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js:2425` | As F52 | Accepted | As F52 | As F52 | As F52 |
| F54 | `js/trivial-conditional` `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js:3188` | As F52 | Accepted | As F52 | As F52 | As F52 |
| F55 | `js/unneeded-defensive-code` `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js:2697` | As F52 | Accepted | As F52 | As F52 | As F52 |
| F56 | `js/unneeded-defensive-code` `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js:3171` | As F52 | Accepted | As F52 | As F52 | As F52 |
| F57 | `py/ineffectual-statement` `ops/deploy/monitoring/scan-exporter/scan_exporter/application/collector.py:35` | The body of the `Protocol` method `JsonClient.get_json` was `...`, which the analysis counts as a statement without effect | Fixed | A docstring is the body and says what the method does; the `Protocol` is unchanged for mypy and for the fake client of the tests | `test-scan-exporter.sh`: ruff, `mypy --strict` and 32 tests pass; the fresh analysis reports no Python finding | None |

## Checklist Results

### QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing of types, members, parameters, locals and fields | Pass | `editedGuestId`, `editedRoomId`, `selectedRoomId`, `status`, `Unwrapped`, `RevenueMonth`, `Observation`, `Cells` |
| 2 | `Async` suffix, `Exception` suffix, one type per file | Pass | No type added; `StubHandler.SendAsync` keeps its name and is still asynchronous (it reads the request body) |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions, explicit types | Pass | `dotnet format whitespace --verify-no-changes` passes; no pragma or suppression added; the changed lines keep the explicit types of their files and `var` where the file already uses it (`BenchmarkExport`, `WebFactory`, the Razor loops), as rule 1 of the coding conventions says; the project-wide use of `var` predates this milestone (RC-266 records it) |
| 4 | Nullable reference types, `!` justified | Pass | No `!` added; the five null findings are fixed with `is { }` patterns that bind the value once |
| 5 | `IDisposable` disposed with `using`, no leak on exception paths | Pass | 11 `using` declarations in the tests; `StubHandler` returns its responses to the client that disposes them; F20 is owned by the caller |
| 6 | `await` throughout, no `.Result`, `.Wait()` or `async void` | Pass | `StubHandler.SendAsync` is `async` and awaits the request body; the responder is synchronous, so no task is wrapped |
| 7 | Specific exceptions, validation at the boundary, `throw;`, no empty `catch` | Pass | No `catch` changed; F01 and F02 keep their generic catch with a reason, neither is empty |
| 8 | XML documentation comments on public members | Pass | No public member added; the private helpers have a comment where the reason is not obvious (`Unwrapped`, `Cells`) |
| 9 | Properties, `readonly`, `init`, `record` | N-A | No state added |
| 10 | `decimal` for money, `DateTimeOffset` for instants | N-A | No money or instant touched |
| 11 | `CancellationToken` on public async APIs | N-A | No signature changed |
| 12 | Tests cover new behaviour, named `Method_Condition_Expected`, independent of order, time and network | Pass | No behaviour changed, so no test was added; the 2490 existing tests of the changed members are the regression tests and pass unchanged (18 test classes named in the report) |
| 13 | Traces to the Design Class Diagram | N-A | Plain technical tasks of MIL-058; no class or operation added to the design |
| 14 | Each piece of knowledge once | Pass | `RevenueMonth` and `Observation` follow the parse-or-null helpers the mapper already has; `Cells` holds the cell order once, next to `Columns`; `Unwrapped` names a mapping that was inline |
| 15 | References point inward, no cycles | Pass | No project reference changed |
| 16 | SOLID | Pass | No responsibility moved; the helpers are private to their types |
| 17 | Clean architecture layout | Pass | No file moved or added |
| 18 | File and folder names | Pass | No file added |
| 19 | Observer through `event` | N-A | No event |
| 20 | SIMD and CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail | None of the ten source files this milestone changes has a region, like 428 of the files under `src/`: the criterion of 2026-10-23 was never applied to the code that predates it (RC-266 records the same for MIL-055). This milestone adds no type and splits no member. Covered by the risk exception below; adding the regions is a task of its own |

### QC-PY-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PEP 8 casing | Pass | Unchanged names |
| 2 | Names state purpose | Pass | `get_json`, unchanged |
| 3 | Formatter and linter, no unexplained suppressions | Pass | ruff format and check clean in `test-scan-exporter.sh` |
| 4 | Every signature annotated | Pass | The signature is unchanged and annotated |
| 5 | No bare except, cause kept | N-A | No exception handling touched |
| 6 | No mutable defaults, no shadowed builtins | N-A | |
| 7 | Context managers | N-A | |
| 8 | Docstrings say what, not how | Pass | The docstring is the change: it says what `get_json` returns and raises |
| 9 | Logging, no secrets | N-A | |
| 10 | Traces to the Design Class Diagram | N-A | The protocol of MIL-053 is unchanged |
| 11 | Tests for new behaviour, no network | Pass | No behaviour changed; the 32 tests with the fake client pass unchanged |
| 12 | Strict type checking | Pass | `mypy --strict`: no issues |
| 13 | Dependencies declared and pinned | N-A | |
| 14 | DRY | N-A | |
| 15 | Imports point inward | Pass | No import changed |
| 16 | SOLID | N-A | |
| 17 | Clean architecture folders | Pass | Unchanged |
| 18 | File and folder names | Pass | Unchanged |

## Risk Exceptions

| Finding | Owner | Approver | Expiry | Compensating controls |
| --- | --- | --- | --- | --- |
| QC-CS-001 criterion 21 (Mandatory): the ten source files changed by MIL-058, like 428 files under `src/`, have no `#region` blocks; a layout change of every file is outside the plan of MIL-058 | S01 | S01 (the AI as stand-in, autonomous mode; open to S01's challenge) | 2026-12-31 | The changed members are small and commented; the files are the same as before this milestone in that respect; the regions of `src/` are planned as a task of their own (action item below) |

## Overall Verdict

Go: every Mandatory criterion passes except 21, which the risk exception covers and which this milestone does not make worse; the 57 findings each have a decision with its reason, and the fresh analysis holds only the 11 findings recorded as accepted or false positive. The checks of the pull request are the remaining gate (MIL-058 criterion 2) and are reported on the pull request.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Dismiss on the code quality tab the 5 findings in `bootstrap.bundle.js` (F52 to F56) with the reason "third-party code", and read the 6 remaining C# findings against F01, F02, F15, F16, F19 and F20 | S01 | 2026-11-11 |
| Read the checks of the pull request of MIL-058 and merge it (ADR-0019: the owner approves) | S01 | 2026-11-11 |
| Plan the `#region` layout of the files under `src/` (QC-CS-001 criterion 21) as a task of its own, or decide that the criterion applies to new files only | S01 | 2026-12-31 |

---

[MIL-058]: ../../milestones/mil-058-codeql-code-quality-findings.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[457ff29]: https://github.com/NF-Hotel/NFHotel/commit/457ff29bff4226e3bb212b88cd961626631d1245
