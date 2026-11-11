# MIL-071 Price suggestions from the market analysis service

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-071 |
| CrossReference | [BC-001], [US-001], [MIL-055], [MIL-028] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Accepted | Jens Tirsvad Nielsen | S01 | Initial version; S01 chose in chat on 2026-11-11 that a fixed rule in the service makes the suggestions, the AI only explains, and the rule of MIL-055 is replaced; accepted by S01 in chat on 2026-11-11 after RC-323 Go | [4ddf975] |

---

## Purpose

Decide whether the back office can present the price suggestions that the market analysis service makes, instead of working them out itself. [MIL-055] built the rule inside the website: a Manager approves a suggestion with one click at `/admin/pricing/suggestions`. S01 now wants the service (`notebook-market-analysis` MIL-002) to find the holidays of the next two months, to suggest a higher price where the history shows an occupancy above 80 percent on the same days, and to suggest discounts and other changes inside the same two months, while the website only presents them to the Manager. The decision this gate supports: the website holds no pricing rule of its own, so there is one place where a suggestion is worked out; the page, the approval, the undo, the settings, the audit trail and the effective price of MIL-055 stay; no price changes until a Manager approves it; a text written by an AI is labelled as such and never sets a price.

## Deliverable

- **Contract first.** The `pricing` block of the run request and `price_suggestions` of result schema 1.3, as agreed in task 1 with the service, in the client of `src/NFHotel.External/MarketAnalysis`; the settings of MIL-055 (thresholds, percentages, sizes that qualify) are sent in the request.
- **One source.** `SeasonRule` and `PriceSuggestionRule` are removed from the application layer; the suggestions list is read from the latest completed report. A suggestion keeps what MIL-055 showed (size, period, old and new nightly price, percentage, the figures it rests on) and gains the reason (`holiday`, `high_history`, `low_history`), the name of the holiday and the occupancy of each earlier year.
- **Limits stay in the website.** The cap on a change and the minimum price of MIL-055 are applied by the website to the percentage from the service, and the new price is worked out from the room rate of the size; the service never sees a room rate.
- **Presentation.** `/admin/pricing/suggestions` shows the suggestions of the next two months with their holiday, their figures, the AI explanation and the further ideas without a price, each marked as written by an AI, and keeps the Approve and Revert buttons of MIL-055.
- **When the service has no suggestions.** A report of schema 1.2, or of 1.3 with no `price_suggestions`, shows an empty page with the reason, and never a suggestion made by the website.
- **Tests and documents.** Tests of the client, of the mapping, of the cap and the minimum price, a role test, the wiki page, the user story US-001.12 and the traceability matrix brought up to date.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The client reads `price_suggestions` of a sample file of the service, and a test fails when the sample and the DTO differ; a result of schema 1.2 is read without error and gives an empty list | Yes | No |
| 2 | No type named `SeasonRule` or `PriceSuggestionRule` is left in `src/`, and an architecture test fails if a class computes a season or a suggestion in the application layer | Yes | No |
| 3 | A suggestion of 20 percent on a base rate of 100 shows 120; a percentage that exceeds the cap is cut to the cap, and a price below the minimum price is raised to it | Yes | No |
| 4 | The page shows the holiday name, the reason and the occupancy of each earlier year for a suggestion, and the AI text and the further ideas are marked as written by an AI; a test shows that no price on the page comes from an AI text | Yes | No |
| 5 | Approve creates one price change with the dates shown, a second approve of the same suggestion creates none, and Revert ends it, as in MIL-055 (the tests of MIL-055 pass unchanged) | Yes | No |
| 6 | A Manager reaches the page; a Receptionist and a Housekeeper get 403 on the API and no menu entry; no call answers without a sign-in | Yes | No |
| 7 | With the service stopped or without a completed report the page says so and offers no approval | Yes | No |
| 8 | Build has 0 warnings, format is clean, all tests and the architecture tests pass and the `qc-programming-csharp` review has the verdict `Go` | Yes | No |
| 9 | A look at the page in the browser pane on the dev VM with a real run of the service: the holidays of the next two months appear, one suggestion is approved and reverted, and the audit page shows both | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| notebook-market-analysis MIL-002 | Makes the suggestions; the contract of its task 1 is agreed before any code here |
| MIL-055 | The page, the approval and undo, the settings, the price changes and the audit events that stay |
| MIL-028 | The run API and result schema 1.2 that schema 1.3 extends |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 29 of BC-001 (price suggestions; worded again by task 1) | All tasks |
| US-001.12 Approve a price suggestion (worded again by task 1) | Tasks 3 and 4 |
| US-001.01 View the market analysis | Task 2 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S04 for the Business Case and the user story text |

## Target Date

2026-12-11 — two weeks after notebook-market-analysis MIL-002 (2026-11-27); for S01 to confirm.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Business Case and user story | Reword objective 29 of BC-001 and US-001.12 so that the service makes the suggestions and the Manager approves them (holidays, history above 80 percent, a horizon of two months); note in MIL-055 that its rule is replaced; review against QC-BC-001 and QC-US-001. | Yes | US-001.12 |
| 2 | Contract and client | Agree the contract with notebook-market-analysis MIL-002 task 1; add the `pricing` block to the request and `price_suggestions` to the client DTOs of `src/NFHotel.External/MarketAnalysis`; send the settings of MIL-055; tests with the sample file and with schema 1.2. | No | |
| 3 | Replace the rule | Remove `SeasonRule` and `PriceSuggestionRule`; map the suggestions of the report to the suggestion list; apply the cap and the minimum price and work out the new price from the room rate; keep the approve and revert code; update the tests; add the architecture test of criterion 2. | No | |
| 4 | Suggestions page | Show the holiday, the reason, the occupancy of each earlier year, the AI explanation and the further ideas marked as written by an AI, a horizon of two months and the empty states of criterion 7, on `/admin/pricing/suggestions`. | No | |
| 5 | Wiki and traceability | Update the wiki page of the feature (English, the PO language, as `framework/process/wiki.md` says) and the traceability matrix. | No | |
| 6 | Check and review | Build, format, tests, architecture tests, a look at the page on the dev VM, review against `qc-programming-csharp`; record the RC. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-028]: ./mil-028-market-analysis-run-api.md
[MIL-055]: ./mil-055-room-price-suggestions.md
[4ddf975]: https://github.com/NF-Hotel/NFHotel/commit/4ddf9755c4a6b473d2981de48d15e5c75805caad
