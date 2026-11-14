# RC-375 Review of the code of MIL-026 tasks 20 and 21 against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-375 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the review of the merged code of tasks 20 and 21; No-Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-026] tasks 20 and 21 as it is merged on `main` at `9de9be4`, read in the worktree of the branch `mil-026-gate`.
  - Task 20, the number of rooms and the occupancy: `CountRoomsAsync` of the port `IMarketAnalysisBookingQueries` and of `MarketAnalysisBookingQueries`, the room count in `MarketAnalysisRunner` and in the request (`MarketAnalysisRunRequest.RoomCount`, sent as `room_count`), and the occupancy analyses in the report model and the charts: the holiday days and the other days as one series of two separate rates (`FindingsSeries.RootFigures`), the level of the whole period as the reference of the month series (`FindingsSeries.AddReference`), and how the rule of [RC-374] draws them.
  - Task 21, the heat map of correlations and the captions: `CorrelationHeatmap` (`.razor` and `.razor.cs`), `HeatColor.Diverging`, the matrix read by `FindingsSeries.ExtractMatrices`, `ChartCaption` (`.razor` and `.razor.cs`), `ChartCaptionText` with `ChartCaptionModel`, and the AI observations (`MarketAnalysisObservationDto`) matched to the charts by the id of their series; and the loop over the matrices on the report page.
  - The tests: `CountRoomsAsync_SeededRooms_AreCounted` (`MarketAnalysisBookingQueriesTests`), the room tests of `MarketAnalysisRunnerTests`, `AnalyzeAsync_Bookings_StartsTheRunOfTheContract`, the occupancy, matrix and observation tests of `MarketAnalysisResultMapperTests`, the tests of the captions, of the heat map and of the occupancy charts in `MarketChartComponentTests`, and the colour tests of `ChartGeometryTests`.
- Commits that built the tasks: task 20 in `9b517b0` (2026-10-29, with tasks 13 and 14, pull request #406: `CountRoomsAsync`, the room count in the runner and the request, `RootFigures` and `ExtractMatrices`) and `058976c` (2026-10-30, pull request #407: the reference level of the month series and the separate rates as columns); task 21 in `058976c` (with the trailer `Task: MIL-026#21`: `CorrelationHeatmap`, `HeatColor`, `ChartCaption`, `ChartCaptionText`) and its follow-up `c74ef19` (2026-11-01, pull request #427). Later commits that changed the same files are part of what is reviewed: `3d2f1f7` (the observation and its evidence as Markdown, task 23), `2399ffc` (the code of each component moved unchanged into a `.razor.cs` file) and `b892400` (the caption and the report page of the admin overhaul).
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`, 21 criteria)
- Scope: full review of the files above as they stand; where a count is given, it names the lines that the task commits wrote. The review also judges the Go / No-Go criteria 20 and 21 of [MIL-026].
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent — the plan of 2026-11-14 was accepted by the AI as stand-in for S01 at S01's word in chat; the same AI built the code in October.

### Checks that were run

| Check | Result |
| --- | --- |
| CI of `main` at `9de9be4` (run 43621113638, NF Hotel CI), job Build: `dotnet build NFHotel.sln --configuration Release --no-restore`, with warnings as errors and the XML documentation generated for every project under `src/` | 0 Warning(s), 0 Error(s) |
| Same run, job Formatting: `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore` | passed; it checks white space only, not style rules such as `var` |
| Same run, job Tests and coverage | total 2689, failed 0, succeeded 2676, skipped 13 (the E2E tests, which need `E2E_BASE_URL`) |
| Greps over the reviewed files (`var`, `!`, `#region`, top-level types, `#pragma`, `.Result`, `.Wait(`, `async void`), each hit traced to its commit with `git blame` and, for `CorrelationHeatmap.razor.cs`, matched line by line to the `@code` block of `058976c` | see the rows 2 to 4 and 21 of [QC-CS-001] |
| The observations of the sample results of `docs/test-data/` | only the analysis `cancellations` of `market-analysis-result.json` has an insight with observations (two, on `splits.by_deposit_type.groups` and `splits.by_market_segment.groups`), and both name a chart of the analysis |
| How the service names the series of an observation (`src/app/application/chart_series.py` of `NF-Hotel/notebook-market-analysis` on its `main`, read with `gh api`) | the same rule as `FindingsSeries`: the dotted path of a list, and the names joined by `+` for figures in the root (`holiday_days+other_days`); the file says that the two must be changed together (F7) |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PascalCase, camelCase, `_camelCase`, `I` prefix | Pass | The build is clean; `CorrelationHeatmap`, `ChartCaptionText.ForSeries`, `ForMatrix`, `NoObservationText`, `_id`, `MaxCell`, `CountRoomsAsync` |
| 2 | `Async` suffix; `Exception` suffix; one top-level type per file | Fail | `ChartCaptionText.cs` holds two public top-level types, `ChartCaptionModel` and `ChartCaptionText`, both written by `058976c`; the other reviewed files hold one each. `CountRoomsAsync` has its suffix (F1) |
| 3 | `.editorconfig`, no unexplained suppressions; locals have an explicit type, never `var` | Fail | `058976c` wrote `var` in 24 lines of `CorrelationHeatmap` (21 now in `CorrelationHeatmap.razor.cs`, moved unchanged by `2399ffc`, and the three loops of `CorrelationHeatmap.razor`) and in the loop of `ChartCaption.razor:29` (kept by `b892400`); `9b517b0` wrote `var rooms` at `MarketAnalysisRunner.cs:84`. The page's loop over the matrices (`MarketAnalysisReport.razor:190`, `b892400`) is `var` too. `c74ef19` gave `ChartCaptionText` and `HeatColor` their types but not the component. `CountRoomsAsync_SeededRooms_AreCounted` has four `var`; `var` in tests waits for S01's decision on F3 of [RC-266]. No `#pragma` and no suppression attribute (F2) |
| 4 | Nullable enabled; `!` justified in a comment | Fail | No `!` in the reviewed code under `src/`. In `MarketChartComponentTests` five `!` without a comment, all in tests of this record: line 225 (`CorrelationHeatmap_RealMatrix_DrawsEveryCellWithItsValue`, `c74ef19`), 290 and 291 (`ForSeries_Occupancy_TakesTheLimitationsAndTheFormulaOfTheAnalysis`, `058976c`), 309 (`ForSeries_ObservationOfTheSeries_IsMatchedById`, `058976c`) and 373 (`ForMatrix_Correlations_ExplainsTheScale`, `c74ef19`). `ChartCaptionText.cs:40` declares `string?` for three texts that are never null (F3) |
| 5 | `IDisposable` disposed | Pass | The components hold no disposable; the test of the room count disposes its two contexts with `await using`, and the component tests dispose the renderer and its service provider the same way |
| 6 | `await` throughout; no `.Result`, `.Wait()`, `async void` | Pass | `CountRoomsAsync` returns the task of `CountAsync`, and the runner awaits it with `ConfigureAwait(false)`; the grep finds no blocking call |
| 7 | Specific exceptions; arguments validated; `throw;`; no empty `catch` | Pass | `ForSeries` and `ForMatrix` check their arguments with `ArgumentNullException.ThrowIfNull`; the runner refuses a hotel without rooms before it calls the service (`ExecuteAsync_NoRooms_FailsAsInputRejectedWithoutCallingTheService`); no `catch` in the reviewed code of these tasks. `HeatColor.TextOn` takes its colour without a check, like the other chart helpers (F5 of [RC-374]) |
| 8 | Public types and members have XML documentation | Pass | Every project under `src/` generates its documentation with warnings as errors, and the build has 0 warnings; the summary of `ChartKind.Correlation` says that the captions use the value, but nothing passes it (F6) |
| 9 | Properties, not fields; immutability | Pass | `ChartCaptionModel` is a positional record; `Matrix` and `Model` are component parameters |
| 10 | Money is `decimal`; instants are `DateTimeOffset` | Pass | No money and no instant: correlations, rates and the room count (`int`) |
| 11 | Public async APIs take a `CancellationToken` | Pass | `CountRoomsAsync(CancellationToken)` passes it to `CountAsync`, and the runner passes its own |
| 12 | Tests cover new behaviour, `Method_Condition_Expected`, no order, time or network | Pass | `CountRoomsAsync_SeededRooms_AreCounted` (an empty and a seeded database), `ExecuteAsync_RoomsAndPeriod_AreSentWithTheBookings`, `ExecuteAsync_NoRooms_FailsAsInputRejectedWithoutCallingTheService`, `Map_Sample_OccupancyGivesTheMonthlySeries`, `Map_Sample_OccupancyOnHolidaysGivesTwoRates`, `Map_Sample_CorrelationsAreAMatrix`, `Map_Sample_EveryObservationNamesAChartOfItsAnalysis`, the nine caption and heat map tests of `MarketChartComponentTests`, `Diverging_Values_AreBlueWhiteRed`, `Diverging_NearZero_IsCloserToWhiteThanToTheEnds`, `TextOn_Fill_IsReadable`; no clock and no network. No test has an observation of the holiday series or of the matrix (F7) |
| 13 | Classes trace to the DCD; deviations recorded | Fail (known, not new) | `docs/dcd.md` and `docs/dcd.puml` draw the domain package only and name none of these classes; no deviation is recorded. The same row and reasoning as [RC-270] and [RC-373] (F4) |
| 14 | Each piece of knowledge once | Pass | The diverging scale is defined once in `HeatColor.Diverging` and used by the cells and by the gradient of the legend; the caption texts of each kind are in `ChartCaptionText` only, and the observation is matched in `Build` only. The unused `ChartKind.Correlation` arm repeats the matrix caption of `ForMatrix` in shorter words (F6); the rule that names the series is kept in step with the service by hand (F7) |
| 15 | References point inward, no cycles | Pass | The room query is in Infrastructure behind the port of Application; the components read the DTOs of Application |
| 16 | SOLID | Pass | One component for the heat map and one for the caption; the caption text is apart from its markup (`ChartCaptionText` and `ChartCaption`) |
| 17 | Layers and folders of the clean architecture | Pass | The components in `Web/Features/Admin/Shared`, the query in `NFHotel.Infrastructure/Repositories`, the port in `NFHotel.Application/Ports` |
| 18 | File and folder names | Pass | English, PascalCase, no space |
| 19 | Observers through `event` | N-A | No observer in the reviewed code |
| 20 | SIMD / CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail | None of the 514 C# files under `src/` holds a `#region`, and these types follow them; the risk exception of [RC-278] does not cover these files and was approved by the stand-in, which [PRC-001] does not allow (F4) |

## Go / No-Go criteria of MIL-026 (20 and 21)

| # | Criterion | Status | Evidence (test class and method) |
| --- | --- | --- | --- |
| 20 | The run request carries `room_count`, the number of rooms in the database; a month chart of the occupancy and a comparison of holidays and other days are shown | Pass | `CountRoomsAsync_SeededRooms_AreCounted`, `ExecuteAsync_RoomsAndPeriod_AreSentWithTheBookings` (the counted rooms in the request), `AnalyzeAsync_Bookings_StartsTheRunOfTheContract` (`room_count` in the body, checked against the contract); `Map_Sample_OccupancyGivesTheMonthlySeries`, `MarketChart_RealOccupancy_IsALineWithTheOverallLevel`; `Map_Sample_OccupancyOnHolidaysGivesTwoRates`, `MarketChart_RealOccupancyOnHolidays_IsTwoColumnsAndNoDonut`. Task 20 asked for a column chart per month; the month series is a line with the level of the whole period, as criterion 18 asks for months. A service built at the pinned commit is expected to refuse the run ([RC-373], F5) |
| 21 | The correlation matrix is a heat map with a diverging scale from -1 to +1, the value in every cell and a legend; every chart has a caption with what it shows, why that kind, its limits from `limitations`, and the AI observation labelled AI-generated; a chart with no observation says why and never shows an empty box | Pass | The heat map: `CorrelationHeatmap_RealMatrix_DrawsEveryCellWithItsValue`, `CorrelationHeatmap_LongFieldName_IsShortenedOnTheAxis`, `CorrelationHeatmap_NoField_SaysThereIsNoData`, `Diverging_Values_AreBlueWhiteRed`, `Diverging_NearZero_IsCloserToWhiteThanToTheEnds`; read in the code: the colour is clamped to -1..+1, every cell writes its value with two decimals (a dash and "not calculated" where a pair has none), the legend is a gradient labelled -1, 0 and +1 with a line of words, and the text alternative is `role="img"` with a title, a description and a hidden table. The captions: `ForSeries_Occupancy_TakesTheLimitationsAndTheFormulaOfTheAnalysis`, `ForMatrix_Correlations_ExplainsTheScale`, `ForSeries_ObservationOfTheSeries_IsMatchedById`, `ChartCaption_WithAndWithoutObservation_LabelsOrSaysNone` (the label "AI-generated", or the short line without a box), `ForSeries_InsightNotAvailable_SaysWhy`, `ForSeries_NoInsight_SaysNoneWasAsked`, `MarketChart_EverySeriesOfTheRealSample_HasATextAlternativeAndACaption`. Since `b892400` the why and the limits sit in a collapsed "About this chart" under the visible text and observation |

## Findings

- **F1, Fail on criterion 2 (Mandatory), blocking: `ChartCaptionText.cs` holds two public types.**
- **F2, Fail on criterion 3 (Mandatory), blocking: `var` in the heat map, the caption, the page and the runner.** The lines are listed in row 3.
- **F3, Fail on criterion 4 (Mandatory), blocking: five `!` without a comment in the tests of the captions and the heat map.**
- **F4, Fail on criteria 13 and 21 (Mandatory), blocking, known and not new: the DCD and the regions.** The same findings as F3 and F4 of [RC-373].
- **F5, High, carried from [RC-373] (F5): the pinned service refuses the run request.** Criterion 20 passes in the code and its tests, but a service built at the pinned commit is expected to refuse every run, so no new occupancy chart would be shown until the pin moves.
- **F6, Low: `ChartKind.Correlation` is never used.** The selector never returns it, `ForMatrix` writes its own texts, and no caller passes the value to `ForSeries`; its summary says that the captions use it, and its arm in `ChartCaptionText.Texts` repeats the matrix caption in shorter words.
- **F7, Low: the names of the series are kept in step with the service by hand.** An observation reaches its chart only when its `series` equals the id that `FindingsSeries` gives, including `holiday_days+other_days` for the comparison of the occupancy; the service computes the same ids in `chart_series.py`, which says so, but `FindingsSeries` does not name that file, and the only test of the match reads the observations of `cancellations`. An observation of another chart that misses would show "No AI observation for this chart." under a chart whose analysis has one.
- **F8, Low: a hotel without rooms gets the text of a refusal by the service.** The runner marks the run `input_rejected` when the database has no room, and the page then says that the service refused the bookings, although the service was never called.

## Overall Verdict

No-Go. Criteria 2, 3, 4, 13 and 21 of [QC-CS-001] fail, and each is Mandatory, so each is a blocking finding (F1 to F4). No valid risk exception covers them, and the stand-in may not approve one ([PRC-001]). The Go / No-Go criteria 20 and 21 of [MIL-026] pass by their tests; F5 of [RC-373] stands in the way of criterion 20 on the released service. The build has 0 warnings, the white-space check passes and none of the 2676 tests that ran fails.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Move `ChartCaptionModel` into a file of its own (F1) | S01 | before the gate of [MIL-026] |
| Give the locals and loop variables of `CorrelationHeatmap`, `ChartCaption.razor`, the page and `MarketAnalysisRunner.cs:84` their types (F2) | S01 | before the gate of [MIL-026] |
| Explain the five `!` of the tests in a comment or remove them, and declare the three caption texts as `string` (F3) | S01 | before the gate of [MIL-026] |
| Criteria 13 and 21: the decisions of the action items of [RC-373] (F4) | S01 | before the gate of [MIL-026] |
| Move the pin of the service, as the action item of F5 of [RC-373] says (F5) | S01 | before the next release |
| Remove `ChartKind.Correlation` and its arm, or let `ForMatrix` use it (F6) | S01 | with the next change of these files |
| Name `chart_series.py` of the service in `FindingsSeries`, and add a test with an observation of the holiday comparison and of the matrix (F7) | S01 | with the next change of these files |
| Give a hotel without rooms its own code and text (F8) | S01 | with the next change of these files |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[RC-266]: ./rc-266-mil-055-re-review-of-rc-265.md
[RC-270]: ./rc-270-mil-026-code-of-tasks-22-to-27.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-373]: ./rc-373-mil-026-code-of-tasks-13-to-16.md
[RC-374]: ./rc-374-mil-026-code-of-tasks-17-to-19.md
