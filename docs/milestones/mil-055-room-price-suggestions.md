# MIL-055 Room price suggestions with one-click approval

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-055 |
| CrossReference | [BC-001], [US-001], [MIL-026], [MIL-028], [MIL-030] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | Claude (AI) | S01 | Note only: the pricing rule of tasks 4 and 7 is replaced by the suggestions of the market analysis service ([MIL-071]); the page, the approval, the undo, the settings and the audit trail stay; accepted by S01 in chat on 2026-11-11 | [f674e8a] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Gate passed: RC-379 Go on criteria 1 to 11 (1 to 3 passed in RC-265 and RC-266 and superseded by MIL-071; 11 on the run of RC-329); the code at the gate in RC-378 (Go, risk exception for the regions), UC-007 revised in RC-377; at S01's word in chat ("Du godkender for mig"); mode autonomous, not independent | pending |

---

## Purpose

Decide whether the back office can suggest room price changes that a Manager approves with one click. The owner (S01) wants a surcharge of 20 percent in high season and a discount on single rooms in low season, instead of a Manager reading the market analysis and working out the prices by hand. The decision this gate supports: the suggestions follow from the figures of the market analysis report ([MIL-026]; result schema 1.2 of the service of [MIL-028]) by a fixed rule, so the same figures always give the same suggestion; no price changes until a Manager approves it; every approval and every undo is kept in the audit trail. A room has no price today (only a booking has a `PricePerNight`, in `BookingMarketDetail`), so the milestone also adds the nightly rate per room size that the suggestions change.

## Deliverable

- **Rule, from the figures.** A pure rule in the domain and application layers that reads the monthly occupancy and seasonality of the market analysis report and gives each coming month a season: `high` (occupancy at or above the high threshold), `low` (at or below the low threshold) or `normal`. No AI decides a price or a season. The AI insight of the report may be shown beside a suggestion only as a labelled explanation.
- **Suggestions.** For a `high` month a surcharge of 20 percent on all room sizes; for a `low` month a discount on single rooms only (default 15 percent, for S01 to confirm). A suggestion states the room size, the period (first and last day of the season run), the old and the new nightly price, the percentage and the figures it rests on (the occupancy of the month and the threshold it crossed). A change is capped by a maximum percentage, and the new price never goes below a minimum price.
- **Settings page for a Manager.** The surcharge percentage, the discount percentage, the two occupancy thresholds, the maximum change, the minimum price and the sizes that qualify for a discount (single rooms by default) are stored in the database and changed only by a Manager in the back office; each change is written to the audit trail with the old and the new value.
- **Room rates and price changes.** A nightly base rate for each room size, set by a Manager, and price changes (size, percentage, first day, last day, state `approved` or `reverted`, who approved, when). The effective price of a size on a day is the base rate with the approved changes of that day; two approved changes for the same size never overlap.
- **One-click approval.** The page `/admin/pricing/suggestions` lists the open suggestions. Approve creates the price change from a start date to an end date; the dates default to the first and the last day of the season run, and the Manager may change them before approving. A Manager can revert an approved change with one click: a change in force ends on that day, a future change is withdrawn, and the record stays in the list as `reverted`.
- **Audit trail.** `StaffAuditAction` gets `PriceChangeApproved`, `PriceChangeReverted`, `PricingSettingsChanged` and `RoomRateChanged`; each event keeps the staff member, the time, the size, the period and the percentage.
- **Access.** A new permission `Prices.Manage`, held by the Manager role only; other roles get no menu entry and the API answers 403. The writes use the existing write limit policy of the Manager.
- **Tests and documents.** Unit tests of the rule against hand calculated figures, integration tests of approval and undo, a role test, the wiki page of the feature, and the user story US-001.12.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | With the default thresholds (80 and 40 percent) the rule gives `high` for a month with occupancy 85 percent, `low` for 30 percent, and `normal` for 60 percent and for a month without a figure; a test at exactly 80 and exactly 40 fixes which side counts | Yes | No |
| 2 | For a base rate of 100 the high season suggestion for a double room is 120; the low season suggestion for a single room is the rate less the discount; no suggestion is made for a double room or a suite in low season; a result above the cap or below the minimum price is cut to it | Yes | No |
| 3 | Two runs over the same report give identical suggestions, and a test in which only the AI text of the report is changed gives the same suggestions (no AI in the rule) | Yes | No |
| 4 | Approve creates one price change with the dates shown; the effective price on the first day, the last day and the day after the last day is 120, 120 and 100; a second approve of the same suggestion creates no second change | Yes | No |
| 5 | Revert of a future change removes it from the effective price at once; revert of a change in force ends it on that day and the next day is at the base rate; the record stays in the list as `reverted` | Yes | No |
| 6 | Every approval, revert, change of a rate and change of a setting writes an audit event with the staff member and the old and the new value; a test reads the events through the audit page | Yes | No |
| 7 | A Manager reaches the page, the settings, approve and revert; a Receptionist and a Housekeeper get 403 on the API and no menu entry; no call answers without a sign-in | Yes | No |
| 8 | The page shows the figures behind each suggestion and, when the report has an AI insight, shows it labelled as an explanation; no price on the page comes from AI text | Yes | No |
| 9 | The rates, the settings and the price changes are in a migration that applies to an empty database and to one with the 187 seeded rooms, and the migration test passes | Yes | No |
| 10 | Build has 0 warnings, format is clean, all tests and the architecture tests pass (the web layer reaches the data only through the API), and the `qc-programming-csharp` review has the verdict `Go` | Yes | No |
| 11 | A look at the page in the browser pane on the dev VM with the report of the real service: a suggestion is approved and reverted, and the audit page shows both | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-026 | The market analysis report and its page, from which the suggestions are read |
| MIL-028 | Result schema 1.2 with seasonality and occupancy by month |
| MIL-030 | Staff roles, the Manager write limit policy and the audit trail |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 29 of BC-001 (price suggestions; added by task 1) | All tasks |
| US-001.12 Approve a price suggestion (added by task 1) | Tasks 2, 5, 7 and 8 |
| US-001.07 Manage rooms | Task 3 |
| US-001.11 Manage staff accounts (roles, audit trail) | Tasks 7 and 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S04 for the Business Case and the user story text |

## Target Date

2026-11-27 — four weeks from the plan, after [MIL-026] is merged; for S01 to confirm.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Business Case objective and user story | Add objective 29 to BC-001 (suggest room price changes from the market analysis that a Manager approves with one click) and the story US-001.12 with its acceptance criteria; review against QC-BC-001 and QC-US-001. Why: the plan-first gate needs the objective before code. | Yes | US-001.12 |
| 2 | Terms, model and open points | Add season, price suggestion, price change and room rate to the Domain Dictionary and the Domain Model, with their rules (no overlap of two approved changes for a size, a revert never deletes). Confirm with S01: default thresholds 80 and 40 percent, discount 15 percent, cap 30 percent, how many past years of the report the season is read from, and whether a price change touches bookings already made (proposed: no, only new bookings). | Yes | US-001.12 |
| 3 | Room rate | `RoomRate` entity, repository, migration and API for the nightly base rate per room size, a Manager page to set it, audit event `RoomRateChanged`. Why: a room has no price today. | Yes | US-001.07 |
| 4 | Pricing rule | Pure `SeasonRule` and `PriceSuggestionRule` over the monthly occupancy and seasonality of the report, taking the settings and the date as parameters; boundaries, cap and minimum price; unit tests against hand calculated figures. No AI and no clock inside the rule. | No | |
| 5 | Pricing settings | Settings entity, API and Manager page for the surcharge, the discount, the two thresholds, the cap, the minimum price and the sizes that qualify for a discount, with validation (discount below 100, low threshold below high threshold) and audit event `PricingSettingsChanged`. | Yes | US-001.12 |
| 6 | Price change and effective price | `PriceChange` entity with its state, a service that gives the effective price of a size on a day, and a migration; tests of the overlap rule and of the days around the period. | No | |
| 7 | Suggestions API, approval and undo | Read the open suggestions from the latest completed report, approve (the dates may be changed) and revert; permission `Prices.Manage` for the Manager only; the write limit policy; audit events `PriceChangeApproved` and `PriceChangeReverted`; approve is idempotent. | Yes | US-001.12 |
| 8 | Suggestions page | `/admin/pricing/suggestions` in the style of the market analysis page: a table of the suggestions with the figures they rest on, the AI insight shown only as a labelled explanation, one Approve button and a Revert button on the changes in force, a menu entry for the Manager only; page and view model tests. | Yes | US-001.12 |
| 9 | Use the effective price | Use the effective price of the size on the arrival date when a booking is made (new bookings only) and show it on the room page; existing bookings keep their price. | No | |
| 10 | Wiki and traceability | Wiki page of the feature in English, the PO language, as `framework/process/wiki.md` says; update the traceability matrix. | No | |
| 11 | Check and review | Build, format, tests, architecture tests, a look at the page on the dev VM, review against `qc-programming-csharp`; record the RC. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-026]: ./mil-026-market-analysis-report.md
[MIL-028]: ./mil-028-market-analysis-run-api.md
[MIL-030]: ./mil-030-staff-accounts-and-password.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
[MIL-071]: ./mil-071-price-suggestions-from-the-service.md
[f674e8a]: https://github.com/NF-Hotel/NFHotel/commit/f674e8a3fe47dfe3c36bbc59f4eeae6180985b0d
