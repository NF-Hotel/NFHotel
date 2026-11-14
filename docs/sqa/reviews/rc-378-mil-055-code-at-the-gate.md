# RC-378 Delta review of the code of MIL-055 at its gate against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-378 |
| CrossReference | [MIL-055], [RC-265], [RC-266], [RC-278], [RC-377], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Deprecated | mikemike | AI as stand-in for S01 | b0ee6ac added: the two locals of the region of the gate in `ConflictPathTests` typed; the search for `var` and F3 now cover the test files of the milestone; Go; mode autonomous, not independent | [8758ab7] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | bdfa319 added: three page tests render the rows of the three pricing pages that 68502d4 typed, which the SonarCloud gate of the pull request found without coverage (G4); criterion 12 counts them; Go; mode autonomous, not independent | [91c23c6] |

---

## Artifact Under Review

- Instance reviewed: the ten code commits of the branch `mil-055-gate` on top of `main` at e4681a9, 48 files under `src/` and `tests/`:
  - 68502d4: explicit types in place of `var` on the three pricing pages (`PriceSuggestions.razor`, `PricingSettings.razor`, `RoomRates.razor`);
  - 07c2daf: the warning of a lost price names the type of the exception, not its message (`BookingService.cs`, `BookingPriceTests.cs`);
  - c779f4d: the three migration tests of the pricing tables named by what they check;
  - 1f0819d: a discount of the whole price is cut to the largest discount a price change takes (`PriceSuggestionService.cs`, `PriceSuggestionServiceTests.cs`);
  - 51d59a7: the conflict of the two unique indexes of the pricing tables proven at the database and at the API (`ConflictPathTests.cs`, `RoomRatesEndpointTests.cs`, `PricingSettingsEndpointTests.cs`);
  - bd96853: tests of the adapter `LoggerWarningLog`;
  - 17d67cc: `#region` blocks in the 31 files of the types MIL-055 created that hold more than one kind of member (F4);
  - 96b93dc: the three pricing tabs defined once, in `PricingTabs.cs`, for the three pricing pages (G3);
  - b0ee6ac: explicit types for the two locals of the region that 51d59a7 added to `ConflictPathTests` (F3);
  - bdfa319: three page tests that render the rows of the three pricing pages: a coming price change with its Revert button, the room rates and the discount check boxes (G4).
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`), the criteria the commits touch (3, 4, 7, 11, 12, 14, 15, 21) and the three left open by [RC-266] (3, 13, 21).
- Scope: delta review. The earlier records are [RC-265] (the full review of tasks 3 to 9) and [RC-266] (the delta re-review, Go-with-conditions). This record settles the three conditions of RC-266. The suggestions themselves were replaced by [MIL-071] and reviewed there; the code of MIL-055 that stays is the page, the approval, the undo, the settings, the room rates and the audit trail. The second version re-scores criteria 14 and 21 after 17d67cc and 96b93dc.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-055 in its third wave, to the AI in chat ("Du godkender for mig"). The AI wrote the ten commits and this record.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -warnaserror`, on 96b93dc and on bdfa319 | Build succeeded, 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | One `IDE0005` in `tests/NFHotel.Web.Tests/BookingCalendarViewModelTests.cs`, a file the gate does not touch; the same as [RC-325] to [RC-327] recorded. The changed files are clean: with `--include`, the 31 files of 17d67cc, the four C# files of 96b93dc and the three test files of bdfa319 give exit 0 |
| `dotnet test`, NFHotel.Domain.Tests | 485 passed, 0 failed (on the regions of 17d67cc) |
| `dotnet test`, NFHotel.Application.Tests | 586 passed, 0 failed (on the regions of 17d67cc) |
| `dotnet test`, NFHotel.Api.Tests | 318 passed, 0 failed (on the regions of 17d67cc) |
| `dotnet test`, NFHotel.ApiClient.Tests | 105 passed, 0 failed (on the regions of 17d67cc) |
| `dotnet test`, NFHotel.Web.Tests | 785 passed, 0 failed (on 96b93dc); 788 passed, 0 failed (on bdfa319) |
| `dotnet test`, NFHotel.Infrastructure.Tests, class `LoggerWarningLogTests` | 3 passed, 0 failed (on the regions of 17d67cc) |
| The regions of the 31 files of 17d67cc | Every region named, in the standard order, closed by a named `#endregion`, none nested and none inside a method body (a script that follows the depth of the braces). Apart from the region lines, the lines that are not blank are the same and in the same order, except three members moved into their region: `OpenLoaded` and the enum `ChangeStatus` of `PriceSuggestionsViewModel`, and the class `Fields` of `PricingSettingsViewModel`; none of them has an initializer |
| `src/**/packages.lock.json` | unchanged |
| Search for `var` in the 67 files under `src/` whose names hold `Pric`, `RoomRate` or `PriceChange` (without the migrations) | 2 lines, both in `MarketSeedPricing.cs`, which [MIL-026] added on 2026-10-20. The test files MIL-055 created have none, and the region that the gate added to `ConflictPathTests` has none since b0ee6ac; the rest of that shared file keeps its `var` |
| The five texts of the imported bookings (`src/NFHotel.DataAnalysis/data/nf_hotel_bookings.csv`) | 40,060 rows; meal, market segment, room type, deposit type and customer type are never empty, so no row is all empty (F10) |
| The SonarCloud gate of the pull request, on 4e5bee5 | Failed on the coverage of new code: 66.7 percent against 80 (18 lines, 6 not covered, all in the loops of the three pricing pages that 68502d4 typed); the other five conditions OK. bdfa319 renders those rows (G4); the gate is read again on the pull request |
| Not run here | the integration tests against PostgreSQL (Testcontainers; Docker does not run on this machine): the two new tests of `ConflictPathTests` and the three renamed tests. NF Hotel CI runs them on the pull request |

## The conditions of RC-266

### Condition 1: F3 and F4 of RC-265

| # | Finding | Decision | Evidence |
| --- | --- | --- | --- |
| F3 | Criterion 3: the new code used `var` | **Closed for MIL-055** | The code of the milestone has no `var` left: 68502d4 took out 13 uses on the three pricing pages, b0ee6ac the two of the test region that 51d59a7 added to `ConflictPathTests`, and the search above finds none in the other pricing files or in the test files of MIL-055. The rest of the repository follows rule 1 of the coding conventions (the convention of the file wins): `RoomListViewModel.cs`, which MIL-055 changed by one column, keeps its `var`. A conversion of the whole repository would be a task of its own; none is ordered here |
| F4 | Criterion 21: no `#region` blocks under `src/` | **Fixed for MIL-055** in 17d67cc | The types MIL-055 created are in 64 files: the 61 it added and the three code-behind files that 2399ffc split from its pages on 2026-11-05. The 31 that hold more than one kind of member now group them in the standard order and names. The 30 other C# files hold one kind only (positional records without a body, interfaces with methods only, classes of constants or of methods only, an enum), and the three `.razor` files have no `@code` block; the C# reference of the coding conventions says that a type small enough to hold one kind of member "still follows the rule when it has more than one kind". The internal method `KeyOf` of `PriceSuggestionService` is in the region Public methods, where the order of StyleCop SA1202 puts internal members (after the public ones). The shared types that MIL-055 only changed, for example `BookingService.cs` and `RoomListViewModel.cs`, keep the layout of their file (G1) |

### Condition 2: the findings skipped by decision

| # | Finding | Decision | Reason |
| --- | --- | --- | --- |
| F5 | No database guard against two approved changes of one size that overlap | **Accepted as a follow-up** | The guard is the overlap check of `PriceSuggestionService` under a `SemaphoreSlim` of the process; the comment at `PriceSuggestionService.cs:21` says that one API instance is assumed. The API runs as one container: `ops/deploy/nfhotel-api/docker-compose.yml` has one service `nfhotel-api` and sets no replicas. An exclusion constraint like the one of the bookings is needed before a second instance runs (action item) |
| F10 | The heuristic that reads a price-only row as unknown | **Accepted** | The text half of F10 (permission and page path in the milestone) was fixed in ac36d7f on 2026-11-02. The link between `IsPriceOnly` (`MarketAnalysisBookingQueries.cs:94-108`) and the rows that `BookingService` stores is now written in the comment at `:64-65`. A row is read as unknown only when every count is 0, the flag is off and all five texts are empty; in the data set of the repository no row has an empty text (check above), so no imported row is misread |
| F11 | The DCD lacks the classes of the milestone | **Closed** | ac36d7f added `RoomRate`, `PriceChange`, `PricingSetting` and `PriceChangeState` to `docs/dcd.md` on 2026-11-02 (the block "Price suggestions (MIL-055)"). The DCD draws the Domain layer only: its sections Application, Infrastructure, Web and API are empty for every class, so `PriceMath`, `MoneyText`, `RoomSizeText`, `IWarningLog` and `LoggerWarningLog` are outside it like the other classes of those layers |
| F14 | `RoomListViewModel` uses the formatters of `PriceSuggestionsViewModel` | **Accepted as a follow-up** | Still at `RoomListViewModel.cs:482`. Both view models are in the Web layer and the price move is written by one function, so the knowledge is in one place; the cost is a dependency between two features of the same layer. Moving `FormatChange` and `FormatMove` to `NFHotel.Web.Common` is a small refactoring for later (action item) |

### Condition 3: the new findings N1 to N6

| # | Finding | Result | Evidence |
| --- | --- | --- | --- |
| N1 | A discount of 100 percent could be suggested but not approved | **Fixed** in 1f0819d | `PriceSuggestionService` cuts a discount to `-PriceChange.MinPercentage` (99.99 percent) after the largest change, and marks the suggestion as cut. The limit is the constant of the entity, not a second number. Test `ApproveAsync_DiscountOfTheWholePrice_IsCutToTheLargestDiscountAndApproved`: discount and largest change 100, lowest price 0, a suggestion of the service of -100 percent; the list shows -99.99 percent, the new price 0.01 and the mark, and the approval stores -99.99 |
| N2 | The warning wrote the message of the exception; `LoggerWarningLog` had no test | **Fixed** in 07c2daf and bd96853 | The warning names the booking id and the type of the exception, as the remark of `IWarningLog` asks; `CreateAsync_SavingTheDetailFails_WritesAWarningWithTheBookingIdAndTheExceptionType` asserts that the message stays out. `LoggerWarningLogTests`: one entry at the level Warning with the text as given, braces in the text not read as a template, and the null check of the constructor |
| N3 | UC-007 3b did not name a last day before today | **Fixed** in the revision of UC-007 | [RC-377] |
| N4 | The conflict of the unique indexes was proven with a fake unit of work only | **Fixed** in 51d59a7 | `ConflictPathTests`, region "Unique indexes of the pricing tables": a second rate for one size and a second row of settings, saved against a real PostgreSQL, raise `UniqueConstraintViolationException` with the constraint names `ux_room_rate_size` and `ux_pricing_settings_slot`. `Put_UniqueIndexBrokenBySimultaneousSave_Returns409WithTheConflictCode` in both endpoint test classes: a unit of work that fails with that exception gives 409 and the conflict code. Together they cover both ends of the race; the index decides the winner whatever the timing |
| N5 | Not written where the lowest price applies | **Fixed** in the revision of UC-007 | [RC-377]; whether the effective price is floored at the lowest price is left to S01 (RC-377 F1) |
| N6 | Three test names promised more than the bodies check | **Fixed** in c779f4d | `Migrations_AppliedToAnEmptyDatabase_CreateTheEmptyTableAndTheIndex` and twice `Migrations_AppliedToAnEmptyDatabase_CreateTheEmptyTable` |

## Findings

- **G1, note: criterion 21 and rule 1 of the coding conventions meet in the older files.** Criterion 21 asks for regions in every type under `src/`. Rule 1 of the coding conventions says that the convention of an existing file or project wins "even if the sub-skill says otherwise", and before this branch none of the 514 `.cs` files under `src/` had a region. This record applies criterion 21 to the types MIL-055 created and rule 1 to the shared types it only changed, so no file mixes the two layouts. Every gate with C# code meets the same question; a conversion of `src/` as a planned task, or a clarification of the framework proposed upstream, is S01's decision (action item).
- **G2, note: the risk exception of the first version is withdrawn.** The first version of this record took a risk exception for criterion 21 and accepted it as stand-in, as [RC-278] did for [MIL-058]. `framework/process/review-checklist-process.md` says that the stand-in may not approve a risk exception, and that the approver is the escalation role of the governance document, never the author or the owner. This version fixes the finding instead. [RC-341] and [RC-363] score criterion 21 "Pass, on condition" of the exception of [RC-278], a status the process does not have; they are listed for S01 (action item).
- **G3, fixed: the pricing tabs were written once per page.** The three pricing pages each held the same array of the three pricing tabs, which came with the admin overhaul (63f3f48, 2026-11-08) when it moved the pages under one menu item. Since 96b93dc `PricingTabs.All` defines them once. The Benchmark and Staff pages repeat their tab sets in the same way; they are outside MIL-055 and are left to the reviews of their own code.
- **G4, fixed: the typed rows of the pages had no test.** The SonarCloud gate of the pull request failed on the coverage of new code (66.7 percent against 80). The six lines it found are the loops that 68502d4 typed: the table of price changes of `PriceSuggestions.razor`, the room rates of `RoomRates.razor` and the discount sizes of `PricingSettings.razor`. Their view models were tested, but no test rendered those rows. bdfa319 adds `PriceSuggestionsPageTests.Render_ComingChange_ListsItWithItsRevertButton`, `RoomRatesPageTests.Render_OneRateSet_ShowsItAndMarksTheOthersNotSet` and `PricingSettingsPageTests.Render_SavedSettings_ChecksOnlyTheDiscountedSizes`, which read each cell of the rows.

## Checklist Results

| # | Criterion (QC-CS-001) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | Formatter, no suppressions, explicit local types | Pass | Build with `-warnaserror` clean; the changed files are formatted; no `#pragma` or suppression added; the code of MIL-055 has no `var` (F3) |
| 4 | Nullable enabled; `!` justified in a comment | Pass | No `!` added under `src/`; the one `null!` in `LoggerWarningLogTests` has its comment |
| 7 | Specific exceptions; arguments validated; no empty catch | Pass | No `catch` added or widened; the catch of RC-265 F2 keeps its filter and now logs less |
| 11 | Public async APIs take a `CancellationToken` | Pass | No public async member added |
| 12 | Tests cover new behaviour; three-part names; no order, time or network | Pass | 11 new tests (1 service, 2 database, 2 endpoint, 3 adapter, 3 page) and 4 renamed; all named `Method_Condition_Expected`; the endpoint and service tests use the fake clock; `TestRulesTests` passes in the Application run. 17d67cc and 96b93dc change no behaviour, and the tests of the pages pass on them; the rows that 68502d4 typed are rendered in tests since bdfa319 (G4) |
| 13 | Classes trace to the DCD; deviations recorded | Pass | F11: the classes of the Domain layer are in the DCD; the DCD draws no other layer, so the classes of those layers implement no DCD |
| 14 | Each piece of knowledge once | Pass | The largest discount is read from `PriceChange.MinPercentage`; the conflict codes and the constraint names are the existing ones; the pricing tabs are defined once (G3) |
| 15 | References point inward, no cycles | Pass | No project reference added; the architecture tests pass in the Application run |
| 21 | Every type under `src/` groups its members in `#region` blocks | Pass | F4: the 31 files of the types MIL-055 created that hold more than one kind of member, checked by script; the shared types it only changed keep the layout of their file (G1) |

## Overall Verdict

Go. The three conditions of [RC-266] are settled: F3 is closed and F4 fixed for the code of the milestone; F11 is closed, and F5, F10 and F14 are accepted with their reasons; N1, N2, N4 and N6 are fixed in the code and proven by tests, and N3 and N5 are fixed in UC-007 ([RC-377]). Every Mandatory criterion of [QC-CS-001] that the gate touches passes, and no risk exception is taken (G2); criterion 21 is read as G1 says. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted this record and the follow-ups as stand-in for S01; the reading of criterion 21 stays open to S01's challenge. Not verified here: the integration tests against PostgreSQL, which run in NF Hotel CI on the pull request. The reviewer is not independent: the AI wrote the commits and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the follow-ups F5, F10 and F14, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request of MIL-055 |
| Read the integration tests of `ConflictPathTests` and the renamed migration tests in NF Hotel CI of the gate pull request | AI as stand-in for S01 | Before the merge |
| Add a database guard (an exclusion constraint) against two approved price changes of one size that overlap (F5) | S01 | Before a second instance of the API is started |
| Move `FormatChange` and `FormatMove` to `NFHotel.Web.Common` (F14) | S01 | 2026-12-31 |
| Decide how criterion 21 meets rule 1 in the files that existed before it (G1): a planned conversion of `src/`, or a clarification of the framework proposed upstream | S01 | 2026-12-31 |
| Decide on the stand-in exception of [RC-278] and the records that lean on it, [RC-341] and [RC-363] (G2) | S01 | 2026-12-31 |

---

[MIL-055]: ../../milestones/mil-055-room-price-suggestions.md
[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[MIL-058]: ../../milestones/mil-058-codeql-code-quality-findings.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[RC-265]: ./rc-265-mil-055-tasks-3-to-9-code.md
[RC-266]: ./rc-266-mil-055-re-review-of-rc-265.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-325]: ./rc-325-mil-071-task-2-code.md
[RC-327]: ./rc-327-mil-071-task-4-code.md
[RC-341]: ./rc-341-mil-079-code.md
[RC-363]: ./rc-363-mil-077-code.md
[RC-377]: ./rc-377-uc-007-revision.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[8758ab7]: https://github.com/NF-Hotel/NFHotel/commit/8758ab71c8c3d135abb0c87b8e4ee1ac72fa6167
[91c23c6]: https://github.com/NF-Hotel/NFHotel/commit/91c23c6a268389c8aedd4a4d77a307e8719074bb
