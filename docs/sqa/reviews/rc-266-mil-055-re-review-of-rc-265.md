# RC-266 Delta re-review of the code of MIL-055 after the fixes of RC-265

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-266 |
| CrossReference | [MIL-055], [RC-265], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Deprecated | Claude (AI) | S01 | Initial version; delta re-review by the AI, not independent, for S01 to accept | [3a17eee] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Accepted with its three conditions settled: the code at the gate in RC-378 (F3 closed for MIL-055, F4 under a risk exception, F11 closed, F5, F10 and F14 accepted, N1, N2, N4 and N6 fixed) and UC-007 in RC-377 (N3, N5); at S01's word in chat ("Du godkender for mig"); mode autonomous, not independent | [852803e] |

---

## Artifact Under Review

- Instance reviewed: the fixes made after [RC-265] to the code of [MIL-055], uncommitted in the working tree of the branch `mil-055-price-suggestions`: `PriceMath` (new), `PriceSuggestionRule`, `PriceChangeService`, `PriceSuggestionService`, `RoomSizeText`, `MoneyText` (new), `RoomRateService`, `PricingSettingsService`, `BookingService`, the port `IWarningLog` and the adapter `LoggerWarningLog` with its registration, `PricingSettingConfiguration`, `ErrorMessages`, and the tests `PricingMigrationTests`, `PriceSuggestionRuleTests`, `PriceSuggestionServiceTests`, `PriceChangeServiceTests`, `BookingPriceTests`, `RoomRateServiceTests`, `PricingSettingsServiceTests`. Not part of this review: task 10, criterion 11 of the milestone (the look on the dev VM).
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`), only the criteria that the fixes touch (4, 7, 9, 11, 12, 14, 15, 17) and the two that stay open (3, 21); criterion 13 re-stated.
- Scope: delta re-review of the findings F1, F2, F6, F7, F8, F9, F12 and F13 of [RC-265], and of the milestone criteria 2, 6, 9 and 10; earlier record [RC-265]. Skipped by decision of the caller and therefore not re-checked: F3, F4 (S01 to decide), F5, F10, F11, F14.
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised (the AI reviews; S01 accepts or challenges in chat). Not independent: the same AI built the code and the fixes. The review changed, committed and fixed no code.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln -warnaserror --no-incremental` | Build succeeded, 0 Warning(s), 0 Error(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | exit 0, no change needed |
| `dotnet test NFHotel.sln` | Passed: total 2208, failed 0, succeeded 2196, skipped 12 (the E2E tests, which need `E2E_BASE_URL`); PostgreSQL ran in Docker. RC-265 had 2185 / 2173 / 12: 23 tests were added |
| `src/NFHotel.Web/packages.lock.json` | unchanged by the restore (`git status` shows no change) |
| Brute-force check of `LargestDiscountKeeping` | 293,653 random rates (0.01 to 1,000,000.00) and minima, plus corner cases (0.01 / 0.00, 1.00 / 0.90, 999,999.99 / 999,999.98, 1,000,000 / 999,999.99): the price of the returned percentage was never below the minimum and never more than 0.01 % of the rate plus half a cent above it. Done with Python `decimal` (28 digits, same formula), not with the C# code; the C# code is covered by the theory `Suggest_MinimumLimitsTheDiscount_ApprovedChangeGivesAtLeastTheMinimum` (8 pairs) |
| Not run | a browser or the dev VM; a real HTTP 409 for F13; `dotnet ef migrations has-pending-model-changes` (now covered by `HasPendingModelChanges()` in `PricingMigrationTests`) |

## Re-check of the findings of RC-265

| # | RC-265 finding | Result | Evidence |
| --- | --- | --- | --- |
| F1 | Major: minimum price cuts the price, not the percentage | **Fixed** | `PriceMath.LargestDiscountKeeping` (`PriceMath.cs:30-41`) gives `floor((rate - floor) * 10000 / rate) / 100`; `PriceSuggestionRule.Build` (`:80-96`) replaces the percentage with it when the capped discount gives a price below the minimum, sets `WasLimited`, returns no suggestion when it is 0, and takes `NewPrice` from `PriceMath.ApplyPercentage` with the stored percentage. The approval (`PriceSuggestionService.cs:94`, `match.Percentage`) and the effective price (`PriceChangeService.cs:223`) use the same `ApplyPercentage`, so the suggested price, the audit text (`:187`) and the booking price are one function. Proofs: `Suggest_DiscountBelowTheMinimum_PercentageGivesTheCutPrice` (10 %, 90.00), `Suggest_MinimumLimitsTheDiscount_ApprovedChangeGivesAtLeastTheMinimum`, `ApproveAsync_DiscountCutByTheMinimumPrice_EffectivePriceIsTheSuggestedPriceAndNotBelowTheMinimum` (approve, then the effective price is 90.00). See "Attempt to break F1" |
| F2 | Major: second save swallowed silently | **Fixed (accepted design, with a note)** | `BookingService.cs:412-418` catches, calls `_marketDetails.Discard(detail)` and `_warnings.Warn(...)` with the booking id, the exception type and message. Port `IWarningLog`, adapter `LoggerWarningLog` (source-generated `[LoggerMessage]`, level Warning), `AddSingleton` in `Infrastructure/DependencyInjection.cs:165`; a singleton that needs only `ILogger<T>` has no lifetime problem (BookingService is scoped and may use a singleton). Tests: `CreateAsync_SavingTheDetailFails_WritesAWarningWithTheBookingIdAndTheException`, `CreateAsync_DetailIsSaved_WritesNoWarning`, `CreateAsync_SavingTheDetailFails_KeepsTheBookingAndDiscardsTheDetail`. Judgement on the two saves: acceptable. The booking must not be lost because a price could not be stored; the failed save is rolled back by the database; the detail is detached so the context is clean; the loss is now visible in the log. Residual: it is not atomic, there is no retry or repair, and a lost price is found only by reading the log. For a rare failure straight after a successful save this is proportionate. See N2 |
| F6 | Major (14): knowledge written more than once | **Fixed, with residuals** | Formula once: `PriceMath.cs:19` (callers `PriceSuggestionRule.cs:83,96`, `PriceChangeService.cs:187,223`; no other price rounding in Application). Size parser once: `RoomSizeText.TryParse` (callers `PriceChangeService.cs:64,149`, `PricingSettingsService.cs:141`, `RoomRateService.cs:66`); `TryParseSize` is gone, `TryParseSizes` only loops over it. Money text once in Application: `MoneyText.Format` (`RoomRateService.cs:86,99`, `PricingSettingsService.cs:176`, `PriceChangeService.cs:187`). Entity constants are used in `ErrorMessages` for 1,000,000 (`RoomRate.MaxNightlyPrice`, `PricingSetting.MaxMinimumPrice`), 1000 and 366 (`PriceChange.MaxPercentage`, `MaxPeriodDays`). Residual, Minor: (a) `NumberInput.Format` in Web writes `0.00` again (Web cannot see the internal `MoneyText`; a second place in another layer); (b) `ErrorMessages.cs:116-121` still writes "100 percent" and "0" in text for `PricingSetting.MaxPercent`, and `:127` writes "just under 100 percent" for `MinPercentage` -99.99; (c) the `Annulleringstoken` param texts in `BookingService.cs` are older code, not new |
| F7 | Major: migration over the 187 rooms untested | **Fixed** | `PricingMigrationTests.cs` (own database per test): `Migrate_EmptyDatabaseToTheLatest_...` (objects exist, `HasPendingModelChanges()` is false, no pending migration), `Migrate_DatabaseWith187Rooms_KeepsTheRoomsAndTheAuditEventAndAddsEmptyTables` (migrate to the migration before `AddRoomRateAndAuditDetail`, insert 187 rooms and one audit event, migrate to the latest, rooms and event kept, `detail` null, new tables empty), `Migrate_DownOverDatabaseWith187Rooms_...AndAppliesAgain` (Down to the earlier migration removes tables, column, three indexes and the check; rooms kept; Up again). The three migrations are asserted to be consecutive. The 187 rooms are inserted by SQL (`generate_series`), not by the seeder, so the proof is of the schema path, not of the real seed file. The old tests `Migrations_AppliedToAnEmptyDatabase_MatchTheModel*` (3 files) keep their names, which RC-265 asked to change; they now sit beside a test that does check the model (N6) |
| F8 | Minor: `!` without a comment | **Fixed** | `PricingSettingConfiguration.cs:33-34` explains why (the column is required, so EF never compares a null list). The comment is two lines above the `!` pair at `:37`, not on its line; acceptable |
| F9 | Minor: wrong remark | **Fixed** | The remark of `FindPricePerNightAsync` in `BookingService.cs` now says a failed result or a missing room is "no price" and that a thrown exception is not caught and fails the request before the booking is saved. It matches the code |
| F12 | Minor: period wholly in the past | **Fixed** | `PriceChangeService.cs:81-84` returns `price_change.period_invalid` when `lastDay < _clock.Today`, after the domain check and before the overlap check and the save. Test: `ApproveAsync_PeriodWhollyInThePast_ReturnsThePeriodCodeAndSavesNothing` (clock 2027-05-01, period April); the tests use `FakeClock`, so they do not depend on the date. The suggestion horizon starts the month after today, so a suggestion's own dates are never past; only edited dates can be. No E2E test touches pricing. The text of `PriceChangeErrors.PeriodInvalid` names "before today" (`ErrorMessages.cs:128`). UC-007 extension 3b does not name the new rule (N3). The repeated-click check comes after the rule, so a click on a change whose last day has passed answers `period_invalid`, not "exists"; harmless |
| F13 | Minor: unique-index race gives 500 | **Fixed (service level)** | `RoomRateService.cs:101-109` and `PricingSettingsService.cs:123-131` catch `UniqueConstraintViolationException` around the one save and return `room_rate.concurrency_conflict` / `pricing_setting.concurrency_conflict`. This is the right type: `UnitOfWork.cs` translates a `DbUpdateException` with a `PostgresException` 23505 to `UniqueConstraintViolationException`. `ResultResponses.cs:14` maps a code that ends in `.concurrency_conflict` to 409. The texts exist in `ErrorMessages.cs`. A retry is a new request with a new scope and context, so the entities of the failed save do not matter. Tests: `SetAsync_UniqueIndexBrokenBySimultaneousSave_ReturnsTheConflictCode` (both services) with a fake unit of work. Not proven: the real index with two parallel saves, and the HTTP 409 (N4) |

### Attempt to break F1

| Case | Result |
| --- | --- |
| Very small rate (0.01), minimum 0.01 or 0.00, discount 15 % | `ApplyPercentage` gives 0.01 (rounding away from zero), which is not below a minimum of 0.01, so the branch is not entered; with a minimum of 0.00 it is never entered. No path below the minimum |
| Rate 1.00, minimum 0.90, discount 15 % | 0.85 is below 0.90, so the largest discount is `floor(0.10 * 10000 / 1.00) = 1000` hundredths = 10.00 %, price 0.90 |
| Minimum equal to the rate, or above it | `LargestDiscountKeeping` returns 0 (`floor >= rate`), so the rule returns no suggestion (`Suggest_RateAtTheMinimum_GivesNoDiscount`). Before the fix a rate below the minimum was raised to the minimum; now there is no suggestion, which is right for a discount |
| The cap cuts and the minimum cuts | The cap is applied first (`Math.Min`), then the minimum only if the capped price is below it. The percentage from `LargestDiscountKeeping` is below the capped one, because the capped price is below the minimum. `WasLimited` is true in both cases |
| Surcharge | The minimum is not applied to a surcharge (`!isHigh` guard); `ApplyPercentage` is the same function. A surcharge cannot go below the rate |
| Effective percentage that rounds to 0.00 (rate 1,000,000, minimum 999,999.99) | `hundredths` is 0, the percentage is 0, the rule returns null: no suggestion with a 0 % change (`Suggest_MinimumLeavesNoRoomForTheSmallestDiscount_GivesNoSuggestion`) |
| Rate near 1,000,000 | `(rate - floor) * 10000` is about 10^10, far inside `decimal`; the brute force included 1,000,000 |
| Rounding | The result is rounded down to a hundredth of a percent, so the exact price is at least the minimum; the minimum has at most two decimals (the entity rounds it), and rounding to two decimals is monotonic, so the rounded price is at least the minimum. The `decimal` division is rounded at 28 digits: at worst an integer boundary is crossed upward by 10^-25, which moves the exact price below the minimum by about 10^-25 times the rate, and the rounding to two decimals brings it back to the minimum |
| Key format between list and approve | `KeyOf` writes the signed effective percentage with `0.00`; the approval recomputes the items and matches by that key, so the same inputs give the same key. If the rate or the settings changed in between, the key is not found and the answer is `price_suggestion.not_found` (the page says to reload). `IsSameChange` compares the stored (effective) percentage |
| Edited date range | `PriceSuggestionApproveRequest` holds only the key and the dates; the percentage is `match.Percentage` of the server computation (`PriceSuggestionService.cs:94`). `ApproveAsync_ChangedDates_AreUsed` passes. The cut percentage does not depend on the dates |
| Audit text | `ApprovedDetail` writes old and new price with the same `ApplyPercentage` of the stored percentage, so it equals the suggestion's `NewPrice` (`ApproveAsync_RateWithDecimals_AuditDetailShowsOldAndNewPrice`, and the F1 service test) |
| Not covered: base rate or minimum changed after the approval | The stored percentage is applied to the then current rate (`PriceChangeService.cs:223`); the minimum is checked only when a suggestion is made. A Manager who later lowers the rate or raises the minimum can have an effective price below the new minimum. This is by design of the milestone ("new bookings only", the suggestion is cut), not a defect of the fix, but it is not written anywhere (N5) |
| Residual edge: discount of 100 % | The settings allow a discount and a largest change of 100 % (`PricingSetting.MaxPercent`); with a minimum of 0 the capped discount gives price 0.00, which is not below 0, so the rule suggests -100 %, and `PriceChange` refuses -100 (`MinPercentage` -99.99, `PriceChange.cs:19`) with `price_change.percentage_invalid` when the Manager approves. Not a price below the minimum, but a suggestion that cannot be approved (N1) |

No path was found that produces an effective price below the minimum, or different from the `NewPrice` of the suggestion, at the time of the approval.

## Checklist Results (criteria that the fixes touch, re-scored)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | `.editorconfig`, no unexplained suppressions; locals have an explicit type, never `var` | **OPEN, for S01's decision (F3)** | Unchanged: the new code still uses `var` (for example `PriceMath.cs:38`, `PriceSuggestionService.cs`, `PriceChangeService.cs`); the repository-wide practice and the `.editorconfig:105-108` severity `suggestion` are as in RC-265. No new `#pragma` or suppression. Waive by a recorded decision, or convert in a separate mechanical change |
| 4 | Nullable enabled; `!` justified in a comment | Pass | The one `!` pair has a comment (F8). The other (`change.CountedLastDay!.Value`, `PriceSuggestionService.cs`) has one. No new `!` in the fixes |
| 7 | Specific exceptions; arguments validated; no empty catch | Pass | The new catches name their types: `UniqueConstraintViolationException` (F13); `catch (Exception) when (not OperationCanceledException)` in `BookingService.cs:412` is deliberate, not empty, logs and discards. `ArgumentNullException.ThrowIfNull` on the new constructor argument `warnings` and in `LoggerWarningLog`. After a cancellation the detail stays tracked until the scope ends; harmless |
| 9 | Properties, not fields; immutability | Pass | `PriceMath`, `MoneyText`, `RoomSizeText` are static and stateless; `IWarningLog` has one method |
| 11 | Public async APIs take a `CancellationToken` | Pass | No new public async method; `StorePricePerNightAsync` and the service methods pass the token. `IWarningLog.Warn` is synchronous and takes none |
| 12 | Tests cover new behaviour, three-part names, no order, time or network | Pass with note | New tests for F1 (rule: 3 methods and a theory; service: approve then effective price), F2 (3), F7 (3), F12 (1), F13 (2). All use `FakeClock`; the new tests contain no `DateTime.Now`, `UtcNow` or `Today` of the system. Gaps: `LoggerWarningLog` has no test; no HTTP-level 409 test; `ErrorCodeTextTests` scans only `ErrorCodes`, not the Domain error classes of this milestone (I checked by hand that all 24 constants of `RoomRateErrors`, `PricingSettingErrors`, `PriceChangeErrors` and `PriceSuggestionErrors` have a text) |
| 13 | Classes trace to the DCD; deviations recorded | **Fail, open (F11, skipped by decision)** | Not re-checked and not fixed. The new `PriceMath`, `RoomSizeText`, `MoneyText`, `IWarningLog` and `LoggerWarningLog` are also not in `docs/dcd.md`. S01 to accept as a follow-up, or the DCD to be updated |
| 14 | Each piece of knowledge once | Pass with note | The price formula, the size parser and the money text in Application are each in one place (F6). Residuals (Web `NumberInput.Format`, percent limits written in the `ErrorMessages` text) are Minor |
| 15 | References point inward, no cycles | Pass | `IWarningLog` is in `Application/Ports`; `LoggerWarningLog` in `Infrastructure/Logging` uses `Microsoft.Extensions.Logging` and implements the port; Application gained no logging package. `StructureTests` pass (2208 tests, 0 failed) |
| 17 | Layers and folders | Pass | `Infrastructure/Logging` is a normal infrastructure folder; the port is in `Application/Ports` with the other ports |
| 21 | Every type under `src/` groups its members in `#region` blocks | **OPEN, for S01's decision (F4)** | Unchanged: none of the new `src/` files has a `#region`, as in the 350 tracked files. The new test file `PricingMigrationTests.cs` does use them |

## Go / No-Go criteria of MIL-055 re-scored

| # | Criterion | Status | Evidence |
| --- | --- | --- | --- |
| 2 | Rate 100: high double 120; low single rate less discount; no suggestion for double or suite in low season; a result above the cap or below the minimum is cut | **Pass** (was Partly) | The suggestion and the approved price now agree: `PriceSuggestionRuleTests.Suggest_DiscountBelowTheMinimum_PercentageGivesTheCutPrice`, `Suggest_MinimumLimitsTheDiscount_ApprovedChangeGivesAtLeastTheMinimum` (theory), `Suggest_RateAtTheMinimum_GivesNoDiscount`, `Suggest_MinimumLeavesNoRoomForTheSmallestDiscount_GivesNoSuggestion`; end to end `PriceSuggestionServiceTests.ApproveAsync_DiscountCutByTheMinimumPrice_EffectivePriceIsTheSuggestedPriceAndNotBelowTheMinimum`; the earlier tests of RC-265 criterion 2 still pass |
| 6 | Every approval, revert, rate change and setting change writes an audit event with the actor and old and new value; a test reads the events through the audit page | Pass with note (unchanged) | `PriceChangeAuditTests.ApproveAsync_RateWithDecimals_AuditDetailShowsOldAndNewPrice` (the new price is the effective price), `PriceSuggestionServiceTests.ApproveAsync_KeyFromTheList_CreatesTheChangeAndOneAuditEvent`, `RoomRateServiceTests.SetAsync_RateExists_ReplacesItAndRecordsOldAndNew`, `PricingSettingsServiceTests.SetAsync_SecondSave_ReplacesTheRowAndListsOnlyTheChangedFields`, `PriceSuggestionsEndpointTests.Flow_Manager_ApprovesRevertsAndSeesBothInTheAudit`. A failed first save (F13) writes no audit event because the event is in the same failed save. Still no test renders the audit page |
| 9 | The rates, settings and changes are in a migration that applies to an empty database and to one with the 187 seeded rooms; the migration test passes | **Pass** (was Partly) | `PricingMigrationTests.Migrate_EmptyDatabaseToTheLatest_CreatesTheNewTablesColumnIndexesAndCheck`, `Migrate_DatabaseWith187Rooms_KeepsTheRoomsAndTheAuditEventAndAddsEmptyTables`, `Migrate_DownOverDatabaseWith187Rooms_RemovesTheNewObjectsKeepsTheRoomsAndAppliesAgain`; all pass in the run. Note: the rooms are inserted by SQL, not by the seeder |
| 10 | Build 0 warnings, format clean, all tests and architecture tests pass; the `qc-programming-csharp` review is `Go` | Partly | Build 0 warnings, format clean, 2208 tests with 0 failed. The review verdict is Go-with-conditions (below), not yet an accepted `Go` |

## New findings

| # | Severity | File:line | What | Suggested fix |
| --- | --- | --- | --- | --- |
| N1 | Minor | `PriceSuggestionRule.cs:80-96`; `PriceChange.cs:19` | With a discount and a largest change of 100 % and a minimum of 0, the rule suggests -100 % (price 0.00); `PriceChange` refuses -100 (limit -99.99), so the Manager sees a suggestion that the approval rejects with `price_change.percentage_invalid`. Found by reading, not run | Cap the discount at `-PriceChange.MinPercentage` in the rule, or limit `DiscountPercent` and `MaxChangePercent` to 99.99 for a discount; add a rule test |
| N2 | Minor | `BookingService.cs:417`; `IWarningLog.cs` (remark); `LoggerWarningLog.cs` | The warning text holds `exception.Message`, but the remark of the port says an id and the type of the exception are enough and that nothing from a guest may be written. For this insert the message holds no guest data, but the rule in the remark and the code differ. `LoggerWarningLog` has no test | Either drop `exception.Message` or change the remark; add a test with a fake `ILogger` that the level is Warning and the text is logged |
| N3 | Minor | `docs/use-cases/uc-007/uc.md:50` (extension 3b) | The new rule "the last day is before today" (F12) is in the code, the page text and the tests, but 3b names only "before the first day" and the overlap | Add it to 3b (a document change; not done in this review) |
| N4 | Minor | `tests/NFHotel.Api.Tests/RoomRatesEndpointTests.cs`, `PricingSettingsEndpointTests.cs` | The conflict of F13 is proven with a fake unit of work only; nothing proves that the real `UnitOfWork` raises the exception for `ux_room_rate_size` and `ux_pricing_settings_slot`, nor the HTTP 409 | One Infrastructure test with two contexts that insert the same size; one endpoint test with a failing unit of work returning 409 |
| N5 | Minor (documentation) | `PriceChangeService.cs:223`; UC-007 3c | The minimum price is enforced when a suggestion is made, not on later changes of the base rate or of the minimum; the effective price of an approved change can end below a minimum set later | Say so in the use case or the Domain Model; or let `EffectiveAsync` floor the price at the stored minimum (a design decision for S01) |
| N6 | Minor | `PriceChangeRepositoryTests.cs:32`, `PricingSettingRepositoryTests.cs:33`, `RoomRateRepositoryTests.cs:31` | The names `Migrations_AppliedToAnEmptyDatabase_MatchTheModel*` still promise more than the bodies check (RC-265 F7 asked to rename them) | Rename them, for example `..._CreateTheTableAndTheIndex` |

No Blocker and no Major finding was made in this re-review.

## Overall Verdict

Go-with-conditions — the Major findings F1, F2, F6 and F7 of [RC-265] are fixed in the code and proven by tests, and F8, F9, F12 and F13 are fixed. The money defect is closed: the percentage, the suggested price, the audit text and the booking price now come from one function, the percentage under the minimum is the largest discount that keeps the minimum, and no path was found to an effective price below the minimum or different from the suggestion at the time of approval (argument above, a brute force over 293,653 cases, and the tests). Milestone criteria 2 and 9 move from Partly to Pass. The second save of the booking price is not atomic, but a logged warning with the booking id is a proportionate answer to a rare failure that must not lose the booking. Build 0 warnings, format clean, 2208 tests with 0 failed and 12 E2E skipped.

Conditions for the pull request of MIL-055:

1. S01 decides F3 (`var`, criterion 3) and F4 (`#region`, criterion 21) for the whole repository: waive by a recorded decision, or order a mechanical conversion as its own change. Both stay OPEN here, and the Mandatory checklist is not fully passed until then.
2. S01 accepts, as follow-ups, the findings skipped by decision: F5 (no database guard for overlapping changes), F10, F11 (criterion 13, the DCD does not hold the new classes), F14.
3. The Minor findings N1 to N6 are either fixed or accepted as follow-ups. N1 (a suggestion that cannot be approved at a discount of 100 %) and N3 (the use case does not name the new rule) are the two to do first because a Manager or a reader can meet them.

Not verified: the page in a browser, the real unique-index race and the HTTP 409 (N4), the C# `decimal` arithmetic at the extremes (the brute force used Python `decimal`; the C# theory covers eight pairs), and the 187 rooms as the real seed file. The review is not independent (the same AI built the code and the fixes).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide F3 and F4: waive `var` and `#region` for this repository in a recorded decision, or order a mechanical conversion as its own milestone | S01 | 2026-11-06 |
| Accept or schedule F5, F10, F11, F14 (skipped by decision) | S01 | 2026-11-06 |
| Fix N1 (cap the discount below 100 %) with a rule test | AI as stand-in for S01 | Before the pull request of MIL-055, or accepted as a follow-up |
| Fix N3 (UC-007 extension 3b) and N5 (say where the minimum applies) | AI as stand-in for S01 | With the wiki task |
| Fix N2, N4, N6 (log text and test, the 409 tests, test names) | AI as stand-in for S01 | Before the pull request of MIL-055, or accepted as follow-ups |
| Accept this record, or challenge a finding | S01 | 2026-11-06 |

---

[MIL-055]: ../../milestones/mil-055-room-price-suggestions.md
[RC-265]: ./rc-265-mil-055-tasks-3-to-9-code.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
[852803e]: https://github.com/NF-Hotel/NFHotel/commit/852803eae5904913edb95063c33eb964bcd73205
