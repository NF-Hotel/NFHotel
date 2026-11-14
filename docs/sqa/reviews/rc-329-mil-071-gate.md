# RC-329 Gate review of MIL-071 (price suggestions from the service)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-329 |
| CrossReference | [MIL-071], [QC-CS-001], [QC-WIKI-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | Claude (AI) | S02 | Initial version; pre-review by the AI, not independent | [95d7856] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S02 | Criterion 14 of QC-WIKI-001 passed on the published wiki (2026-11-13 and 2026-11-14); issue 31 of the service closed on 2026-11-11; accepted by the AI as stand-in for S02 at S01's word in chat ("Ja, godkend dem", to "Skal jeg godkende de fire reviews af MIL-071 i S02's sted og lukke milestonen?"), with RC-326 to RC-328; mode autonomous, not independent; the milestone closes | pending |

---

## Artifact Under Review

- Instance reviewed: the whole of [MIL-071] against its Go/No-Go criteria 1 to 9: the contract and client (task 2), the replacement of the rule (task 3), the page (task 4), the wiki and the traceability matrix (task 5) and a real run on the dev VM (this task)
- Checklist used: [QC-CS-001] (`qc-programming-csharp`) for the code, read in [RC-325], [RC-326] and [RC-327]; [QC-WIKI-001] (`qc-wiki`) for criterion 14 left open in [RC-328]
- Scope: gate of the milestone; no source file changed in task 6, only the wiki page (labels) and the documents
- Mode: supervised for the pre-review (the AI ran the check and wrote it); accepted on 2026-11-14 in autonomous mode by the AI as stand-in for S02, at S01's word in chat: not independent

## The real run (criterion 9)

Stack: `scripts/dev-up.sh` on the dev VM; service `notebook-market-analysis` at `8bf20104`; model `phi4` on the host; 156,686 bookings; browser: Claude in Chrome on `https://localhost:8443`, signed in as the Manager.

| Step | Result |
| --- | --- |
| Run 1 with the default thresholds (80 and 40 percent) | Finished; no suggestion, because the occupancy of the data is 25 to 66 percent. The page showed the empty state and no Approve button |
| Thresholds set to 55 and 45, run 2 (about 33 minutes) | Finished; the page listed suggestions for weeks of 2026-11-11 to 2026-12-27, and for the holidays Christmas Day (2026-12-25) and Second Day of Christmas (2026-12-26), one row for each room size |
| What a row shows | Reason (name of the holiday, or "Busy on the same days in earlier years"), 20 percent, old and new price, the occupancy and the threshold, the occupancy of each year 2018 to 2025, the text labelled "Written by an AI" or "Built from the figures" |
| Further ideas | Three ideas, labelled "Written by an AI", with no price and no Approve button |
| Approve of Single, Christmas Day | The change "Single +20 % 2026-12-25" appeared in Price changes as Coming (15.00 to 18.00); the row left the open list |
| Revert | The change became Withdrawn ("had not begun, so it never counted"); prices today stayed 15, 20 and 25 |
| Audit page | `PriceChangeApproved` and `PriceChangeReverted` by `manager`, with the dates and the prices, and `PricingSettingsChanged` for the thresholds |
| Afterwards | The thresholds were set back to 80 and 40 and saved |

Criterion 9: met.

## Go / No-Go criteria of MIL-071

| # | Criterion | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Client reads `price_suggestions`; sample and DTO tied by a test; schema 1.2 gives an empty list | Pass | [RC-325]: `MarketContractTests`, `MarketAnalysisResultMapper13Tests`, byte-identical sample files |
| 2 | No `SeasonRule` or `PriceSuggestionRule` in `src/`; architecture test | Pass | [RC-326]: `NoPriceRuleTests` |
| 3 | 20 percent of 100 gives 120; the cap and the minimum price | Pass | [RC-326]: `PriceSuggestionServiceTests` |
| 4 | Holiday, reason and history on the page; AI texts marked; no price from an AI text | Pass | [RC-327]: three page tests and four view model tests; seen on the real page |
| 5 | Approve creates one change, a second approve none, Revert ends it; MIL-055 tests unchanged | Pass | Tests of MIL-055 pass; Approve and Revert done on the real page |
| 6 | Manager only; 403 for the other roles; no call without sign-in | Pass | The role tests of MIL-055 are unchanged and pass; the Manager reached the page |
| 7 | Service stopped or no report: says so, no approval | Pass | [RC-327] page test; run 1 showed the empty state |
| 8 | Build 0 warnings, format clean, tests and architecture tests pass, `qc-programming-csharp` is `Go` | Pass | [RC-325] to [RC-327]; last full runs (task 4): Application 572, Api 220, ApiClient 104, Web 778, Infrastructure 182, External 216 (before task 3 removed tests); `dotnet format --verify-no-changes` reports only `IDE0005` in `BookingCalendarViewModelTests.cs`, which no task of this milestone touched |
| 9 | Real run on the dev VM | Pass | The table above |

## Wiki (criterion 14 of [RC-328])

The wiki page had two inaccuracies, found while comparing it with the page: the field names of the settings (the page says "Surcharge in a high season", "High season from occupancy" and so on) and the names of the states (In force, Coming, Finished, Reverted, Withdrawn). Both are corrected in this task. Criterion 14 (the published wiki, every link) could be checked only after the merge, when the publish workflow had copied the page.

The published wiki, checked on 2026-11-13 (the wiki of 611909b) and again on 2026-11-14 at 05:21Z (the wiki at 08bda09, published from 1a1796b): the 25 pages are the files of `docs/wiki` with the same blobs; the sidebar, `Home` and `Appendix` link the page; its ten anchors and its link to `Home` resolve on the rendered page. Criterion 14 passes ([RC-328]).

## Findings

| # | Finding | Severity | Action |
| --- | --- | --- | --- |
| F1 | Some AI explanations say "consistently above" or "exceeds 55 %" although an earlier year was below (for example 2018: 41.5 % for the week of 2026-11-11). The guardrail of the service checks numbers and months only. The texts are labelled and never set a price, and the figures are shown beside each text | Low | Accepted as it is; raise it to the service as a possible rule on comparisons (MIL-002) |
| F2 | The occupancy is of the whole hotel, not of one room size, so each size of a week gets the same suggestion | Low | Known limit, in the wiki page |
| F3 | History years 2018 to 2025 come from the shifted data set of the dev VM | Info | None |
| F4 | No discount and no `low_history` suggestion appeared with this data, so the discount path was seen only in tests | Info | Covered by the application tests (15 percent on the single room) |
| F5 | The first run found nothing with the default thresholds of 80 and 40 percent | Info | Correct for this data; the empty state is clear |

## Overall Verdict

Go: all nine criteria of [MIL-071] are met, every Mandatory criterion of [QC-CS-001] passes in the reviews of the tasks, and criterion 14 of [QC-WIKI-001] passes on the published wiki (2026-11-13 and 2026-11-14). The documentation task of notebook-market-analysis MIL-002 (task 6, issue 31) was closed on 2026-11-11 by its pull request 35, after the first version of this review.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this review and the milestone [MIL-071] (status Closed when the pull request is merged) | AI as stand-in for S02 | done 2026-11-14 |
| Check the published wiki page and its links | AI as stand-in for S02 | done 2026-11-13 and 2026-11-14 |
| Raise F1 to the service | S02 | MIL-002 |

---

[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[QC-CS-001]: ../../framework/qc/code/qc-programming-csharp.md
[QC-WIKI-001]: ../../framework/qc/artifacts/qc-wiki.md
[RC-325]: ./rc-325-mil-071-task-2-code.md
[RC-326]: ./rc-326-mil-071-task-3-code.md
[RC-327]: ./rc-327-mil-071-task-4-code.md
[RC-328]: ./rc-328-mil-071-task-5-wiki-and-traceability.md
[95d7856]: https://github.com/NF-Hotel/NFHotel/commit/95d78565e09da482a609091883a4f4700c61b533
