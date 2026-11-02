# Review of the code of MIL-026 tasks 22 to 27 against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-270 |
| CrossReference | [MIL-026], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Proposed | Claude (AI) | S01 | Initial version; review by the AI, not independent, for S01 to accept | [913ed5f] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-026] tasks 22 to 27 in the working tree of the branch `markdown-texts` (merged with `origin/main`): the download of a missing model on the Benchmark page (`BenchmarkRunListViewModel`, `BenchmarkRuns.razor`, task 22), `MarkdownText` and `MarkdownConverter` (Markdig, task 23), `MarketAnalysisInterpretation`, `RoomRevenuePanel`, `MarketAnalysisForecast`, the bands of `LineChart`, the DTOs and `MarketAnalysisResultMapper` of result schema 1.2 (tasks 24 and 25), `MarketAnalysisRunTracker.Current`, the endpoint `runs/current` and `MarketAnalysisViewModel.PickUpCurrentRunAsync` (task 26), the Markdown in the evidence (task 27), and their tests.
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`, 21 criteria)
- Scope: full review; the review also judged the Go / No-Go criteria 22 to 27 of [MIL-026]. Criterion 28 (the look at the page on the dev VM) is not judged here.
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised (the AI reviews; S01 accepts or challenges in chat). The review is not independent: the same AI built the code. The review fixed three findings of its own (criteria 3 and 4, see below) and committed nothing.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -warnaserror --no-incremental` | 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | exit 0 |
| `dotnet test --solution NFHotel.sln` | Passed: total 2502, failed 0, succeeded 2490, skipped 12 (the E2E tests, which need `E2E_BASE_URL`) |
| Greps over the changed and new files under `src/` | `var` in added lines: 3 found and fixed (criterion 3); `#pragma`, `.Result`, `.Wait(`, `async void`, `DateTime.Now/UtcNow/Today`, `TODO`, `FIXME`: none; the `!` operator: 4 added uses in `LineChart.razor`, now explained by one comment (criterion 4) |
| `packages.lock.json` of Web and Web.Tests | changed on purpose by the new package reference Markdig 1.4.1 (and the test project); no other package moved |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PascalCase, camelCase, `_camelCase`, `I` prefix | Pass | Build and format are clean; `MarkdownConverter`, `RoomRevenuePanel`, `_service`, `_models` |
| 2 | `Async` suffix; `Exception` suffix; one top-level type per file | Pass | `GetCurrentRunAsync`, `PickUpCurrentRunAsync`, `FollowDefaultDownloadAsync`; one top-level type per new file |
| 3 | `.editorconfig`, no unexplained suppressions; locals have an explicit type, never `var` | Pass | The review found three `var` locals in added lines (`MarketAnalysisController.cs:54`, `ApiGateway.cs:188`, `LineChart.razor:72`) and made them explicit; no suppression and no `#pragma`; no `var` in the new files. Older `var` in files that were only touched is not part of this change |
| 4 | Nullable enabled; `!` justified in a comment | Pass | The four `!` in `LineChart.razor` rest on `HasBand` and now carry one comment that says so (`BandText`) |
| 5 | `IDisposable` disposed | Pass | No new `IDisposable`; the `HttpResponseMessage` of `GetOptionalAsync` is in a `using` like the existing methods |
| 6 | `await` throughout; no `.Result`, `.Wait()`, `async void` | Pass | The grep finds none; `ConfigureAwait(false)` on every await outside Razor |
| 7 | Specific exceptions; arguments validated; `throw;`; no empty `catch` | Pass | `ArgumentNullException.ThrowIfNull` in the new constructors; a failed call is a `Result` failure, not an exception; no empty `catch` |
| 8 | Public types and members have XML documentation | Pass | The build with warnings as errors enforces it |
| 9 | Properties, not fields; immutability | Pass | New DTOs are records; the view model exposes properties with private setters |
| 10 | Money is `decimal`; instants are `DateTimeOffset` | Pass with note | The new DTOs carry the figures of the service as `double` (estimates and chart values, not money held by the system); the run start and end are `DateTimeOffset` |
| 11 | Public async APIs take a `CancellationToken` | Pass | `GetCurrentRunAsync`, `GetOptionalAsync`, `PickUpCurrentRunAsync` take it and pass it on |
| 12 | Tests cover new behaviour, `Method_Condition_Expected`, no order, time or network | Pass | `MarkdownTextTests` (7), `MarketAnalysisReportSectionsTests` (12), `MarketAnalysisResultMapper12Tests` (11), `GetCurrentRun_*` (API, ApiClient, Application), `InitializeAsync_*` (4), `SetDefaultAsync_*` and `FollowDefaultDownloadAsync_*` (5); three-part names; no clock or network |
| 13 | Classes trace to the DCD; deviations recorded | Fail (known, not new) | `docs/dcd.md` and `docs/dcd.puml` hold none of `MarkdownText`, `MarketAnalysisRunTracker` or `BenchmarkRunListViewModel`; the DCD has not been kept up to date for the Web features. No deviation is recorded; it is an action item |
| 14 | Each piece of knowledge once | Pass | One Markdown component for every AI text; one result mapper; the chart kind rule is not copied |
| 15 | References point inward, no cycles | Pass | The structure tests pass; Markdig is referenced by `NFHotel.Web` only; the DTOs are in Application |
| 16 | SOLID | Pass | One component per section; the view model asks the service through its port |
| 17 | Layers and folders of the clean architecture | Pass | Components in `Web/Features/Admin/MarketAnalysis` and `Admin/Shared`; DTOs in `Application/Dto/MarketAnalysis`; the mapper in `External` |
| 18 | File and folder names | Pass | English, PascalCase, no space |
| 19 | Observers through `event` | N-A | No observer in the new code |
| 20 | SIMD / CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail (known, not new) | 0 of the files under `src/` hold a `#region` (grep over the whole tree); the new files follow the code that is there. The rule is not met anywhere in `src/` and is not made so one change at a time; it is an action item |

## Go / No-Go criteria of MIL-026 (22 to 27)

| # | Criterion | Status | Evidence (test class and method) |
| --- | --- | --- | --- |
| 22 | A default model that Ollama does not list is downloaded first | Pass | `BenchmarkRunListViewModelTests`: `SetDefaultAsync_ModelNotInstalled_StartsTheDownloadAndSavesNothing`, `FollowDefaultDownloadAsync_Done_SavesTheDefault`, `FollowDefaultDownloadAsync_Failed_SavesNothingAndShowsTheReason`, `SetDefaultAsync_NotAModelName_StartsNoDownload`, `SetDefaultAsync_ListUnavailable_SavesTheDefault` |
| 23 | AI texts as Markdown, raw HTML as text, links not clickable | Pass | `MarkdownTextTests`: `Render_ParagraphsListAndBold_AreHtml`, `Render_ScriptTag_IsShownAsTextAndNotRun`, `Render_Link_IsTextAndNotClickable` |
| 24 | Section "Overall interpretation" | Pass | `MarketAnalysisReportSectionsTests`: `Interpretation_AiText_ShowsMarkdownTheAiLabelAndTheRecommendations`, `Interpretation_FiguresFallback_SaysItIsNotByAnAi`, `Contents_WithoutOverallAndForecast_HasNeitherLink` |
| 25 | Room revenue, recommendations and forecast; schema 1.1 unchanged | Pass | `RoomRevenue_Version12_ShowsTilesColumnsAndCaption`, `Forecast_Version12_ShowsLinesBandsAndHistory`, `Forecast_HistoryTooShortAndOpenBand_SaysSoAndDrawsNoBand`, `ViewModel_Version11_HasNoneOfThem`, `Map_Version11Sample_HasNoNewFields` |
| 26 | The page follows the run that is going on | Pass | `MarketAnalysisViewModelTests`: `InitializeAsync_RunGoesOn_FollowsItAndClosesTheStart`, `InitializeAsync_RunEnded_IsNotFollowed`, `InitializeAsync_NoRun_StartIsOpen`; `GetCurrentRun_*` and `Tracker_Current_*` |
| 27 | Markdown in the evidence | Pass | `MarkdownTextTests`: `Render_EvidenceWithBoldFigure_IsBoldAndHasNoAsterisks`, `Render_EvidenceWithScript_IsShownAsText` |

## Overall Verdict

Go with conditions. The code builds without warnings, the format check and all 2502 tests pass, and the six Go / No-Go criteria are covered by tests. Criteria 13 and 21 fail as they fail for the rest of `src/` and are not caused by this change. The condition is that S01 accepts the two known failures as action items and does the look at the page on the dev VM (criterion 28 of [MIL-026]).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Bring `docs/dcd.md` up to date for the Web features and the market analysis classes (criterion 13) | S01 | before the next milestone |
| Decide whether the `#region` rule (criterion 21) is applied to `src/` as a whole or dropped from the checklist | S01 | before the next milestone |
| Look at `/admin/market-analysis` on the dev VM with a result of schema 1.2 (criterion 28 of [MIL-026]) | S01 | before the milestone is closed |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[913ed5f]: https://github.com/NF-Hotel/NFHotel/commit/913ed5f581493e449ad76bedfba8a1cc2daf6ada
