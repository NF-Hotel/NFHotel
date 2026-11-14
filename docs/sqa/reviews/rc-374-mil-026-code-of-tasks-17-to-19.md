# RC-374 Review of the code of MIL-026 tasks 17 to 19 against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-374 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the review of the merged code of tasks 17 to 19; No-Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-026] tasks 17 and 18 as it is merged on `main` at `9de9be4`, read in the worktree of the branch `mil-026-gate`; task 19 is this review.
  - Task 17, the rule: `ChartKindSelector` and `ChartKind` (`src/NFHotel.Web/Features/Admin/Shared`), and what a series carries for the rule: `IsOrdered` and `IsPartsOfWhole` of `MarketAnalysisSeriesDto`, set by `FindingsSeries`.
  - Task 18, the charts: `MarketChart`, which picks the component, `DonutChart`, `RankedBarChart`, `ColumnChart`, `LineChart`, `HeatmapChart`, `FigureTile` and `ChartAlternative` (each a `.razor` file with its `.razor.cs`), their base `SeriesChartBase` and the helpers `ChartFormat`, `HeatColor`, `DonutGeometry`, `LinearScale`, `MonthGrid` and `LabelThinner`; and how the report page uses them (`MarketAnalysisViewModel.Sections` with `MarketAnalysisChart`, and the loop over the charts in `MarketAnalysisReport.razor`).
  - The tests: `ChartKindSelectorTests`, `ChartGeometryTests`, the chart tests of `MarketChartComponentTests` and `MarketSample` (`tests/NFHotel.Web.Tests`), and `FindingsSeriesShapeTests` (`tests/NFHotel.External.Tests/MarketAnalysis`). The tests of the captions, of the heat map of correlations and of the occupancy charts are judged in [RC-375].
- Commits that built the tasks: `058976c` (2026-10-30, "Draw the market analysis charts as SVG in the website", with the trailers `Task: MIL-026#17`, `#18` and `#21`, pull request #407, merged as `843878e`) and its follow-up `c74ef19` (2026-11-01, explicit types in the `.cs` helpers and a tolerance for the sum of the parts, pull request #427, merged as `891fa82`). No commit names task 19: its review was not recorded before this record. Later commits that changed the same files are part of what is reviewed: `3d2f1f7` (the bands of `LineChart`, task 25), `2399ffc` (the code of each component moved unchanged from its `@code` block into a `.razor.cs` file), `5756e58` (LINQ in `FindingsSeries`) and `b892400` (the report page of the admin overhaul).
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`, 21 criteria)
- Scope: full review of the files above as they stand; where a count is given, it names the lines that the task commits wrote. The review also judges the Go / No-Go criteria 18 and 19 of [MIL-026].
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent — the plan of 2026-11-14 was accepted by the AI as stand-in for S01 at S01's word in chat; the same AI built the code in October.

### Checks that were run

| Check | Result |
| --- | --- |
| CI of `main` at `9de9be4` (run 43621113638, NF Hotel CI), job Build: `dotnet build NFHotel.sln --configuration Release --no-restore`, with warnings as errors and the XML documentation generated for every project under `src/` | 0 Warning(s), 0 Error(s) |
| Same run, job Formatting: `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore` | passed; it checks white space only, not style rules such as `var` |
| Same run, job Tests and coverage | total 2689, failed 0, succeeded 2676, skipped 13 (the E2E tests, which need `E2E_BASE_URL`) |
| The checks of pull request #407 | "Release build (no push)" success; the SonarQube Cloud quality gate failed on a C reliability rating of new code, one bug S1244 (an exact comparison of floating-point numbers) in `FindingsSeries.cs`, and the pull request was merged with it. `c74ef19` compares with a tolerance (`WholeTolerance`), and the quality gate of pull request #427 passed |
| Greps over the reviewed files (`var`, `!`, `#region`, top-level types, `#pragma`, `.Result`, `.Wait(`, `async void`), each hit traced to its commit with `git blame` and, for the `.razor.cs` files, matched line by line to the `@code` blocks of `058976c` | see the rows 2 to 4 and 21 of [QC-CS-001] |
| A look at the page in the browser pane with the sample result of `docs/test-data/` (task 19) | Not checked: this review starts no browser. Pull request #407 says that the page was seen on the dev VM with a real run of all bookings |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PascalCase, camelCase, `_camelCase`, `I` prefix | Pass | The build is clean; `ChartKindSelector.Select`, `MaxDonutParts`, `HeatmapMinYears`, `SeriesChartBase.OpenSvg`, `LabelThinner.Step` |
| 2 | `Async` suffix; `Exception` suffix; one top-level type per file | Fail | `DonutGeometry.cs` (`DonutSlice` and `DonutGeometry`), `LinearScale.cs` (`NiceAxis` and `LinearScale`) and `MonthGrid.cs` (`MonthRow` and `MonthGrid`) each hold two public top-level types, all six written by `058976c`. `MarketAnalysisViewModel.cs` holds four; the file had several before these tasks, and `058976c` changed `MarketAnalysisChart` and `MarketAnalysisSectionView` in place. No async method and no exception type in the chart code (F1) |
| 3 | `.editorconfig`, no unexplained suppressions; locals have an explicit type, never `var` | Fail | `058976c` wrote `var` in 63 lines of the components: `ColumnChart` 18, `HeatmapChart` 15, `LineChart` 14, `RankedBarChart` 9 and `DonutChart` 4 (now in the `.razor.cs` files, moved unchanged by `2399ffc`), and `DonutChart.razor:22`, `MarketChart.razor:11` and `ChartAlternative.razor:16`. `c74ef19` gave explicit types to the `.cs` helpers but not to the components. The page's loop over the charts (`MarketAnalysisReport.razor:186`, last written by `b892400`) is `var` too. The chart tests have none. `.editorconfig:106-108` sets `csharp_style_var_*` to `false:suggestion`; no `#pragma` and no suppression attribute (F2) |
| 4 | Nullable enabled; `!` justified in a comment | Fail | `FindingsSeriesShapeTests.cs:158` (`c74ef19`) and `:212` (`058976c`) use `!` without a comment. Under `src/`, `LineChart.razor.cs:48` uses two `!` on the band without a comment of its own: it was written with the bands of task 25 (`3d2f1f7`) and sits above the comment of line 94, which speaks of the operators below it; [RC-270] counted it as covered (F3) |
| 5 | `IDisposable` disposed | Pass | The components build their SVG in a `StringBuilder` and hold no disposable; the tests dispose the `HtmlRenderer` and its service provider with `await using` |
| 6 | `await` throughout; no `.Result`, `.Wait()`, `async void` | Pass | The components render without async code; the tests await `RenderComponentAsync` through the dispatcher |
| 7 | Specific exceptions; arguments validated; `throw;`; no empty `catch` | Pass | `ChartKindSelector.Select` checks its series with `ArgumentNullException.ThrowIfNull`; no `catch` in the chart code. The public helpers (`DonutGeometry.Slices`, `MonthGrid.AreMonths`, `YearCount`, `Rows` and `TryParse`, `HeatColor.TextOn`, `ChartFormat.Shorten` and `Readable`) take references without a check; they are public for the components and the tests only (F5) |
| 8 | Public types and members have XML documentation | Pass | Every project under `src/` generates its documentation with warnings as errors, and the build has 0 warnings |
| 9 | Properties, not fields; immutability | Pass | The component inputs are `[Parameter]` properties; `DonutSlice`, `NiceAxis`, `MonthRow` and `MarketAnalysisChart` are records |
| 10 | Money is `decimal`; instants are `DateTimeOffset` | Pass | The charts draw `double` values and hold no money; the months are labels read as year and month, not instants |
| 11 | Public async APIs take a `CancellationToken` | N-A | No public async API in the chart code |
| 12 | Tests cover new behaviour, `Method_Condition_Expected`, no order, time or network | Pass | 15 tests of the rule (each kind, and the limits of 4 and 5 parts, 1, 2 and 3 points, months of one year and of several), 16 tests of the geometry, the scale and the colours, 12 tests of the shape of a series, and the component tests of each chart with its empty and one-point cases (`DonutChart_NoPoint_SaysThereIsNoData`, `ColumnChart_OneColumnAndZero_AreDrawn`, `LineChart_OnePointAndNoPoint_AreHandled`); no clock and no network. Task 18 named bUnit; these tests render with the `HtmlRenderer` of ASP.NET Core, which gives the same markup for components without events |
| 13 | Classes trace to the DCD; deviations recorded | Fail (known, not new) | `docs/dcd.md` and `docs/dcd.puml` draw the domain package only and name none of the chart classes; no deviation is recorded. The same row and reasoning as [RC-270] and [RC-373] (F4) |
| 14 | Each piece of knowledge once | Pass | The rule is written once, in `ChartKindSelector`; the number formats, the encoding and the readable names in `ChartFormat`; the scales in `LinearScale`; the colours in `HeatColor`. The kind is computed twice from the same rule, by the view model and again by `MarketChart` (F6) |
| 15 | References point inward, no cycles | Pass | The components read the DTOs of Application; nothing in Application or External refers to Web |
| 16 | SOLID | Pass | One component per kind of chart over a common base; the rule is apart from the drawing. A new kind changes the enumeration, the rule, the switch of `MarketChart` and the texts of `ChartCaptionText`, a closed set of eight |
| 17 | Layers and folders of the clean architecture | Pass | The components and helpers in `Web/Features/Admin/Shared`, the view model in `Web/Features/Admin/MarketAnalysis`, the series in `NFHotel.External`, the DTO in `NFHotel.Application` |
| 18 | File and folder names | Pass | English, PascalCase, no space |
| 19 | Observers through `event` | N-A | No observer in the reviewed code |
| 20 | SIMD / CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail | None of the 514 C# files under `src/` holds a `#region`, and the chart types follow them; the risk exception of [RC-278] does not cover these files and was approved by the stand-in, which [PRC-001] does not allow (F4) |

## Go / No-Go criteria of MIL-026 (18 and 19)

| # | Criterion | Status | Evidence (test class and method) |
| --- | --- | --- | --- |
| 18 | The kind of chart follows the shape of the series by one rule that a test states | Pass | `ChartKindSelectorTests`: `Select_PartsOfAWhole_AreADonutOnlyUpToFourParts` (2, 3 and 4 parts a donut, 5 and 12 ranked bars), `Select_FiveCategories_AreRankedBars`, `Select_OrderedBands_AreColumnsInTheirOrder`, `Select_MonthsOfOneYear_AreALine`, `Select_FewMonths_FollowTheLimits`, `Select_TwentyFourMonthsOverThreeYears_AreAHeatmap`, `Select_OnePoint_IsATile`, `Select_NegativeValue_KeepsTheGroupedBars`, `Select_NoPoint_KeepsTheGroupedBars`, `Select_RealSample_GivesTheExpectedKind`; the drawing: `RankedBarChart_Points_AreLargestFirst`, `ColumnChart_OrderedBands_KeepTheirOrder`, `HeatmapChart_RealMonths_AreYearsAgainstMonths`, `FigureTile_OneFigure_ShowsTheFigure`, `MarketChart_NegativeValue_KeepsTheGroupedBars`. Two to four separate rates (not parts of one whole, not ordered) are columns side by side, not the grouped bars, as task 20 asks for the holiday rates (`Select_SeparateRates_AreColumnsAndNeverADonut`); the grouped bars remain for a series with no point or a negative value |
| 19 | No donut for more than four parts or for figures that are not parts of one whole; a text alternative for every chart; colours never carry the meaning alone | Pass | `Select_RealSample_NeverDrawsADonutForMoreThanFourPartsOrForSeparateRates`, `Select_SeparateRates_AreColumnsAndNeverADonut`; `FindingsSeriesShapeTests`: `Extract_RatesWithTheirOwnDenominators_AreNotPartsOfAWhole`, `Extract_SharesThatDoNotAddUp_AreNotPartsOfAWhole`, `Extract_CutSeries_IsNotPartsOfAWhole`; `MarketChart_EverySeriesOfTheRealSample_HasATextAlternativeAndACaption` (`role="img"`, a title and a description, and a hidden table of the values from `ChartAlternative`); the donut writes its values on the slices and in the legend (`DonutChart_TwoParts_DrawsTheSlicesTheLegendAndTheTable`), the bars and columns write their values, a small sample is marked with a star and explained (`ColumnChart_SmallSample_IsMarkedAndExplained`), and the level of a line is dashed and labelled (`LineChart_ReferenceValue_IsADashedLineWithALabel`) |

## Findings

- **F1, Fail on criterion 2 (Mandatory), blocking: three helper files hold two public types each.** `DonutGeometry.cs`, `LinearScale.cs` and `MonthGrid.cs`, all new in `058976c`.
- **F2, Fail on criterion 3 (Mandatory), blocking: `var` in the components.** 63 lines of `058976c`, listed in row 3; the follow-up `c74ef19` was meant to declare the types in the chart code and reached only the `.cs` helpers.
- **F3, Fail on criterion 4 (Mandatory), blocking: `!` without a comment.** Two lines of `FindingsSeriesShapeTests`, and `LineChart.razor.cs:48`, which the comment of the band does not cover.
- **F4, Fail on criteria 13 and 21 (Mandatory), blocking, known and not new: the DCD and the regions.** The same findings as F3 and F4 of [RC-373].
- **F5, Low: public chart helpers do not check their arguments.** A null from a caller in this assembly gives a `NullReferenceException` instead of an `ArgumentNullException`; the entry points that pages use (`Select`, the component parameters) are checked.
- **F6, Low: the kind is computed twice.** `MarketAnalysisViewModel.Sections` stores `ChartKindSelector.Select(series)` in `MarketAnalysisChart.Kind`, but the page passes only `chart.Series` to `MarketChart`, which calls the rule again; `Kind` is read by one test only (`InitializeAsync_ReportStored_LoadsTheSectionsWithChartsAndInsights`). The rule is one, so the two cannot differ today, but the view model does not drive what the page draws.
- **F7, Low, closed: pull request #407 was merged with its quality gate failed.** The S1244 bug in `FindingsSeries.cs` was fixed two days later by `c74ef19` (pull request #427, quality gate passed).

## Overall Verdict

No-Go. Criteria 2, 3, 4, 13 and 21 of [QC-CS-001] fail, and each is Mandatory, so each is a blocking finding (F1 to F4). No valid risk exception covers them, and the stand-in may not approve one ([PRC-001]). The Go / No-Go criteria 18 and 19 of [MIL-026] pass by their tests. The build has 0 warnings, the white-space check passes and none of the 2676 tests that ran fails; the look at the page of task 19 was not checked.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Move `DonutSlice`, `NiceAxis` and `MonthRow` into files of their own (F1) | S01 | before the gate of [MIL-026] |
| Give the locals and loop variables of the chart components and the page their types (F2) | S01 | before the gate of [MIL-026] |
| Explain the `!` of `LineChart.razor.cs:48` and of `FindingsSeriesShapeTests` in a comment, or remove them (F3) | S01 | before the gate of [MIL-026] |
| Criteria 13 and 21: the decisions of the action items of [RC-373] (F4) | S01 | before the gate of [MIL-026] |
| Check the arguments of the public chart helpers (F5), and let the page draw the kind of the view model or drop `MarketAnalysisChart.Kind` (F6) | S01 | with the next change of these files |
| Look at `/admin/market-analysis` with the sample result of `docs/test-data/` (task 19) | S01 | before the gate of [MIL-026] |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[RC-270]: ./rc-270-mil-026-code-of-tasks-22-to-27.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-373]: ./rc-373-mil-026-code-of-tasks-13-to-16.md
[RC-375]: ./rc-375-mil-026-code-of-tasks-20-and-21.md
