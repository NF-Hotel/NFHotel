# RC-378 Delta review of the code of MIL-055 at its gate against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-378 |
| CrossReference | [MIL-055], [RC-265], [RC-266], [RC-278], [RC-377], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the six commits of the gate branch of MIL-055 (RC-266 N1, N2, N4 and N6, F3 on the pricing pages) and the decisions on the conditions of RC-266; Go with a risk exception for criterion 21; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the six commits of the branch `mil-055-gate` on top of `main` at e4681a9, 14 files:
  - 68502d4: explicit types in place of `var` on the three pricing pages (`PriceSuggestions.razor`, `PricingSettings.razor`, `RoomRates.razor`);
  - 07c2daf: the warning of a lost price names the type of the exception, not its message (`BookingService.cs`, `BookingPriceTests.cs`);
  - c779f4d: the three migration tests of the pricing tables named by what they check;
  - 1f0819d: a discount of the whole price is cut to the largest discount a price change takes (`PriceSuggestionService.cs`, `PriceSuggestionServiceTests.cs`);
  - 51d59a7: the conflict of the two unique indexes of the pricing tables proven at the database and at the API (`ConflictPathTests.cs`, `RoomRatesEndpointTests.cs`, `PricingSettingsEndpointTests.cs`);
  - bd96853: tests of the adapter `LoggerWarningLog`.
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`), the criteria the commits touch (3, 4, 7, 11, 12, 14, 15) and the three left open by [RC-266] (3, 13, 21).
- Scope: delta review. The earlier records are [RC-265] (the full review of tasks 3 to 9) and [RC-266] (the delta re-review, Go-with-conditions). This record settles the three conditions of RC-266. The suggestions themselves were replaced by [MIL-071] and reviewed there; the code of MIL-055 that stays is the page, the approval, the undo, the settings, the room rates and the audit trail.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-055 in its third wave, to the AI in chat ("Du godkender for mig"). The AI wrote the six commits and this record.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -warnaserror` | Build succeeded, 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | One `IDE0005` in `tests/NFHotel.Web.Tests/BookingCalendarViewModelTests.cs`, a file the gate does not touch; the same as [RC-325] to [RC-327] recorded. The changed files are clean |
| `dotnet test`, NFHotel.Domain.Tests | 485 passed, 0 failed |
| `dotnet test`, NFHotel.Application.Tests | 586 passed, 0 failed |
| `dotnet test`, NFHotel.Api.Tests | 318 passed, 0 failed |
| `dotnet test`, NFHotel.Web.Tests | 785 passed, 0 failed |
| `dotnet test`, NFHotel.Infrastructure.Tests, class `LoggerWarningLogTests` | 3 passed, 0 failed |
| `src/**/packages.lock.json` | unchanged |
| Search for `var` in the 67 files under `src/` whose names hold `Pric`, `RoomRate` or `PriceChange` (without the migrations) | 2 lines, both in `MarketSeedPricing.cs`, which [MIL-026] added on 2026-10-20 |
| The five texts of the imported bookings (`src/NFHotel.DataAnalysis/data/nf_hotel_bookings.csv`) | 40,060 rows; meal, market segment, room type, deposit type and customer type are never empty, so no row is all empty (F10) |
| Not run here | the integration tests against PostgreSQL (Testcontainers; Docker does not run on this machine): the two new tests of `ConflictPathTests` and the three renamed tests. NF Hotel CI runs them on the pull request |

## The conditions of RC-266

### Condition 1: F3 and F4 of RC-265

| # | Finding | Decision | Evidence |
| --- | --- | --- | --- |
| F3 | Criterion 3: the new code used `var` | **Closed for MIL-055** | The code of the milestone has no `var` left: 68502d4 took out the last 13 uses, on the three pricing pages, and the search above finds none in the other pricing files. The rest of the repository follows rule 1 of the coding conventions (the convention of the file wins): `RoomListViewModel.cs`, which MIL-055 changed by one column, keeps its `var`. A conversion of the whole repository would be a task of its own; none is ordered here |
| F4 | Criterion 21: no `#region` blocks under `src/` | **Risk exception**, below | None of the 514 `.cs` files under `src/` has a region. [RC-278] took the same exception for [MIL-058], with the expiry 2026-12-31 and the action to plan the regions as one task or to decide that the criterion applies to new files only. The gate adds no type under `src/` |

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

## Checklist Results

| # | Criterion (QC-CS-001) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | Formatter, no suppressions, explicit local types | Pass | Build with `-warnaserror` clean; the changed files are formatted; no `#pragma` or suppression added; the code of MIL-055 has no `var` (F3) |
| 4 | Nullable enabled; `!` justified in a comment | Pass | No `!` added under `src/`; the one `null!` in `LoggerWarningLogTests` has its comment |
| 7 | Specific exceptions; arguments validated; no empty catch | Pass | No `catch` added or widened; the catch of RC-265 F2 keeps its filter and now logs less |
| 11 | Public async APIs take a `CancellationToken` | Pass | No public async member added |
| 12 | Tests cover new behaviour; three-part names; no order, time or network | Pass | 8 new tests (1 service, 2 database, 2 endpoint, 3 adapter) and 4 renamed; all named `Method_Condition_Expected`; the endpoint and service tests use the fake clock; `TestRulesTests` passes in the Application run |
| 13 | Classes trace to the DCD; deviations recorded | Pass | F11 |
| 14 | Each piece of knowledge once | Pass | The largest discount is read from `PriceChange.MinPercentage`; the conflict codes and the constraint names are the existing ones |
| 15 | References point inward, no cycles | Pass | No project reference added; the architecture tests pass in the Application run |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail, covered by the risk exception | F4 |

## Risk Exceptions

| Finding | Owner | Approver | Expiry | Compensating controls |
| --- | --- | --- | --- | --- |
| QC-CS-001 criterion 21 (Mandatory): the files of MIL-055 under `src/`, like all 514 `.cs` files there, have no `#region` blocks; a layout change of every file is outside the plan of MIL-055 | S01 | S01 (the AI as stand-in, autonomous mode; open to S01's challenge) | 2026-12-31 | The same exception and expiry as [RC-278]; the gate adds no type under `src/`; the test classes the gate adds or changes have their regions; the regions of `src/` are one planned task (the action item of RC-278) |

## Overall Verdict

Go. The three conditions of [RC-266] are settled: F3 is closed for the code of the milestone and F4 is under the risk exception above; F11 is closed, and F5, F10 and F14 are accepted with their reasons; N1, N2, N4 and N6 are fixed in the code and proven by tests, and N3 and N5 are fixed in UC-007 ([RC-377]). Every Mandatory criterion of [QC-CS-001] that the gate touches passes, except 21, which the exception covers and which the gate does not make worse. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted this record, the risk exception and the follow-ups as stand-in for S01. Not verified here: the integration tests against PostgreSQL, which run in NF Hotel CI on the pull request. The reviewer is not independent: the AI wrote the commits and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, the risk exception and the follow-ups F5, F10 and F14, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request of MIL-055 |
| Read the integration tests of `ConflictPathTests` and the renamed migration tests in NF Hotel CI of the gate pull request | AI as stand-in for S01 | Before the merge |
| Add a database guard (an exclusion constraint) against two approved price changes of one size that overlap (F5) | S01 | Before a second instance of the API is started |
| Move `FormatChange` and `FormatMove` to `NFHotel.Web.Common` (F14) | S01 | 2026-12-31 |
| Plan the `#region` layout of `src/`, or decide that criterion 21 applies to new files only (as [RC-278]) | S01 | 2026-12-31 |

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
[RC-377]: ./rc-377-uc-007-revision.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
