# RC-379 Gate review of MIL-055 Room price suggestions with one-click approval

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-379 |
| CrossReference | [MIL-055], [MIL-071], [UC-007], [RC-262], [RC-265], [RC-266], [RC-326], [RC-327], [RC-328], [RC-329], [RC-377], [RC-378], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-055 on criteria 1 to 11, after MIL-071 replaced its pricing rule; Go; mode autonomous, not independent | [852803e] |

---

## Artifact Under Review

- Instance reviewed: [MIL-055] at its gate. It covers the eleven Go/No-Go criteria, with this evidence:
  - the code on `main` at e4681a9 with the six commits of the gate branch, reviewed in [RC-378];
  - the reviews of the code before the gate: [RC-265] (No-Go) and [RC-266] (Go-with-conditions), whose conditions [RC-378] settles;
  - the revision of [UC-007] after [MIL-071], reviewed in [RC-377];
  - the gate of [MIL-071], [RC-329], with its run on the dev VM against the real market analysis service (2026-11-11);
  - the wiki page Price Suggestions, accepted in [RC-328] and checked on the published wiki on 2026-11-14.

  Before the gate: [RC-262] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-055]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. [MIL-071] replaced the pricing rule of tasks 4 and 7 on 2026-11-11 (the note row of MIL-055 of that date): the website no longer works out a season or a suggestion, the market analysis service does. Criteria 1 to 3 describe that rule, so they are scored as they stood when the rule was reviewed and are marked superseded. Criteria 4 to 11 describe the page, the approval, the undo, the settings, the room rates, the audit trail and the checks, which MIL-071 kept.
- Language and domain: en / it (the Metadata rows of MIL-055). The language of the milestone was reviewed in RC-262, and the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-055 in its third wave, to the AI in chat ("Du godkender for mig"). The AI built the code of MIL-055 and of MIL-071, made the run of RC-329 and wrote this record.

**Evidence of 2026-11-14 on the gate branch:** `dotnet build NFHotel.sln -warnaserror` with 0 warnings; the changed files formatted; the tests of Domain (485), Application (586), Api (318) and Web (785) passed with 0 failed, and the three new tests of `LoggerWarningLogTests`. The integration tests against PostgreSQL run in NF Hotel CI on the pull request.

**Evidence of the run on the dev VM (from [RC-329], 2026-11-11):** `scripts/dev-up.sh` on the dev VM, the service `notebook-market-analysis` at 8bf20104, signed in as the Manager. A suggestion for single rooms on Christmas Day (2026-12-25) was approved and shown as Coming (15.00 to 18.00); Revert made it Withdrawn, and the prices of the day stayed 15, 20 and 25; the audit page showed `PriceChangeApproved` and `PriceChangeReverted` by the Manager, with the dates and the prices.

## Checklist Results

| # | Criterion (MIL-055) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The season rule with the thresholds 80 and 40; exactly 80 and exactly 40 fixed | Pass when reviewed; superseded by [MIL-071] | [RC-265]: `SeasonRuleTests` passed. MIL-071 task 3 removed `SeasonRule`, and `NoPriceRuleTests` proves that no pricing rule is left under `src/` ([RC-326], [RC-329] criterion 2); the thresholds are now settings sent to the service |
| 2 | Rate 100: 120 for a double room in high season; the single room discount; no discount for a double room or a suite; the cap and the lowest price cut the result | Pass when reviewed; superseded by [MIL-071], the cut still holds | [RC-266] re-scored it Pass. Since MIL-071 the service makes the suggestions; the website still applies the largest change and the lowest price ([RC-326]: 20 percent of 100 gives 120, the cap and the lowest price), and since 1f0819d a discount is never more than 99.99 percent ([RC-378] N1) |
| 3 | Two runs give the same suggestions; a changed AI text changes nothing | Pass when reviewed; superseded by [MIL-071] | [RC-265]: `PriceSuggestionRuleTests` passed. Since MIL-071 no AI text sets a period, a percentage or a price ([RC-329] criterion 4; UC-007 rule of step 2, [RC-377]) |
| 4 | Approve creates one change with the dates shown; the effective price is 120, 120 and 100 on the first day, the last day and the day after; a second approve creates no second change | Pass | `PriceChangeServiceTests.GetEffectivePriceAsync_AroundThePeriod_AppliesTheChangeOnlyInside`, `PriceSuggestionServiceTests.ApproveAsync_Repeated_CreatesNothingAndNoSecondAuditEvent`, `PriceSuggestionsEndpointTests.Approve_Twice_CreatesOneChangeAndOneAuditEvent`; all passed on the gate branch. On the dev VM the approved change was shown with its dates ([RC-329]) |
| 5 | Revert of a future change removes it at once; revert of a change in force ends it that day; the record stays as reverted | Pass | `PriceChangeServiceTests.RevertAsync_FutureChange_WithdrawsItAndTheBaseRateAppliesAtOnce`, `RevertAsync_ChangeInForce_EndsItTodayAndKeepsYesterday`; on the dev VM the reverted future change became Withdrawn and stayed in the list ([RC-329]) |
| 6 | Every approval, revert, rate change and settings change writes an audit event with the staff member and the old and the new value; a test reads them through the audit page | Pass | [RC-266] criterion 6; `PriceSuggestionsEndpointTests.Flow_Manager_ApprovesRevertsAndSeesBothInTheAudit`, `RoomRatesEndpointTests.Audit_AfterTwoChanges_ShowsTheOldAndTheNewPrice`, `PricingSettingsEndpointTests.Audit_AfterTwoSaves_ShowsOnlyTheChangedFields`; the audit page of the dev VM showed both events ([RC-329]) |
| 7 | A Manager reaches the page, the settings, approve and revert; a Receptionist and a Housekeeper get 403 and no menu entry; no call answers without a sign-in | Pass | [RC-265] criterion 7; `PriceSuggestionsEndpointTests.EveryRoute_RoleWithoutThePermission_Returns403` and `EveryRoute_NoStaffToken_Returns401`, `ManagePricesPermissionTests.HasPermission_OtherStaffRoleAndManagePrices_IsFalse`, the 403 and 401 tests of the room rates and the settings; all passed on the gate branch |
| 8 | The page shows the figures behind each suggestion and the AI insight labelled as an explanation; no price comes from AI text | Pass | [RC-265] criterion 8 for the page of MIL-055; since MIL-071 the page shows the reason, the holiday and the earlier years, and marks every AI text ([RC-327]; [RC-329] criterion 4, seen on the real page) |
| 9 | The rates, the settings and the price changes are in a migration that applies to an empty database and to one with the 187 seeded rooms | Pass | [RC-266] criterion 9: `PricingMigrationTests`, which runs against PostgreSQL in NF Hotel CI on the pull request |
| 10 | Build 0 warnings, format clean, all tests and the architecture tests pass, and the `qc-programming-csharp` review is `Go` | Pass | [RC-378]: Go, with a risk exception for criterion 21 of QC-CS-001 (the `#region` blocks, as [RC-278]); the evidence of the gate branch above; NF Hotel CI on the pull request runs the full set |
| 11 | A look at the page on the dev VM with the report of the real service: a suggestion is approved and reverted, and the audit page shows both | Pass, with a note | [RC-329], above. The page was opened in Claude in Chrome, not in the browser pane the criterion names (F2) |

## Findings

- **F1, note: three criteria describe a rule that is gone.** Criteria 1 to 3 held when [RC-265] and [RC-266] reviewed them, and [MIL-071] then replaced the rule with the suggestions of the service, as the note row of MIL-055 of 2026-11-11 records. The gate does not test the removed code again; it relies on `NoPriceRuleTests` and the gate of MIL-071 for what took its place. The milestone text is left as it was planned, with the note row, so the history stays readable.
- **F2, note: another browser.** Criterion 11 names the browser pane; the run of [RC-329] used Claude in Chrome (`https://localhost:8443`, signed in as the Manager). The criterion is about what is seen (approve, revert, both on the audit page), and that was seen; the tool is a detail.
- **F3, note: the run is older than the gate commits.** The run on the dev VM was made on 2026-11-11, before the six commits of the gate branch. None of them changes the approval or the revert of a surcharge: 1f0819d acts only on a discount above 99.99 percent, and the other five change a log text, test code and the types of locals. The run therefore still shows the behaviour of criterion 11.

## Overall Verdict

Go. Criteria 4 to 11 pass on the code of the gate branch, the tests that ran on it and the run of [RC-329]; criteria 1 to 3 passed when the rule was reviewed and are superseded by [MIL-071], which passed its own gate. The conditions of [RC-266] are settled in [RC-378], and UC-007 states what the system does now ([RC-377]). S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted this record and the gate row of MIL-055 as stand-in for S01; the gate of MIL-055 has passed once NF Hotel CI is green on its pull request. MIL-055 has no milestone or issues on GitHub, so its pull request closes none. The reviewer is not independent: the AI built the code and wrote this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-055, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read NF Hotel CI of the gate pull request, with the integration tests, before the merge | AI as stand-in for S01 | Before the merge |
| The follow-ups of [RC-378] (F5, F14, the regions) and the decisions of [RC-377] (F1, F3) | S01 | As set in those records |

---

[MIL-055]: ../../milestones/mil-055-room-price-suggestions.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[UC-007]: ../../use-cases/uc-007/uc.md
[RC-262]: ./rc-262-mil-055-room-price-suggestions.md
[RC-265]: ./rc-265-mil-055-tasks-3-to-9-code.md
[RC-266]: ./rc-266-mil-055-re-review-of-rc-265.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-326]: ./rc-326-mil-071-task-3-code.md
[RC-327]: ./rc-327-mil-071-task-4-code.md
[RC-328]: ./rc-328-mil-071-task-5-wiki-and-traceability.md
[RC-329]: ./rc-329-mil-071-gate.md
[RC-377]: ./rc-377-uc-007-revision.md
[RC-378]: ./rc-378-mil-055-code-at-the-gate.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[852803e]: https://github.com/NF-Hotel/NFHotel/commit/852803eae5904913edb95063c33eb964bcd73205
