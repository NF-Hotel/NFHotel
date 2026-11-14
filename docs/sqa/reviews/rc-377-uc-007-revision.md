# RC-377 Review of the revision of UC-007 after MIL-071 and RC-266

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-377 |
| CrossReference | [UC-007], [UCD-001], [MIL-055], [MIL-071], [RC-264], [RC-266], [RC-329], [QC-UC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the revision of UC-007 (the suggestions of [MIL-071], RC-266 N1, N3 and N5); Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the revision of [UC-007] of 2026-11-14, its second Version History row: the line of S01 under the stakeholders, the precondition, step 2, the extensions 2a, 3b and 3c, the rules of steps 2, 3 and 4, and the open issues. The rest of the use case is as [RC-264] reviewed it.
- Checklist used: [QC-UC-001], Fully Dressed. Criteria 1 and 2 are for the Brief and the Casual format and do not apply.
- Scope: delta review. Each changed sentence was compared with the code on `main` at e4681a9, with the commits of the gate branch of [MIL-055], and with [MIL-071] and its gate [RC-329].
- Language and domain: en / it (the Metadata rows of UC-007).
- Language reviewer: none; the reviewer reads the language and knows the domain.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-055 in its third wave, to the AI in chat ("Du godkender for mig"). The AI wrote the revision and this record.

**Why the revision was needed:**

- [MIL-071] replaced the rule of MIL-055 with the suggestions of the market analysis service. UC-007 still described months of high and low season worked out by the website.
- [RC-266] N3: the code refuses a last day before today (RC-265 F12), but extension 3b did not say so.
- [RC-266] N5: the use case did not say when the lowest price is checked.
- [RC-266] N1: the settings allow a discount of 100 percent, which an approval refuses. The gate branch now cuts a discount to 99.99 percent, and 3c says so.

**What each change rests on:**

| Change | Evidence |
| --- | --- |
| Step 2 and the first and third rule of step 2: the service works out the suggestions of the next two months; the website has no pricing rule; no AI text sets a number | [MIL-071] criteria 2 and 4, passed in [RC-329]; `PriceSuggestionService.ComputeAsync` reads `PriceSuggestions` of the latest report and passes the AI texts on as texts |
| The second rule of step 2: the settings and their defaults | `PricingSettings`: surcharge 20, discount 15, thresholds 80 and 40, largest change 30, lowest price 0, single rooms; the settings are sent with each run ([MIL-071] deliverable) |
| Extension 2a: no report, or a report without suggestions | `ComputeAsync` returns the basis `NoBasis` when the report or its `price_suggestions` is missing; [MIL-071] criterion 7 |
| Extension 3b: a last day before today (N3) | `PriceChangeService.ApproveAsync` returns `price_change.period_invalid` when the last day is before today; test `ApproveAsync_PeriodWhollyInThePast_ReturnsThePeriodCodeAndSavesNothing` |
| Extension 3c and the first rule of steps 3 and 4: a discount of at most 99.99 percent (N1) | Commit 1f0819d; test `ApproveAsync_DiscountOfTheWholePrice_IsCutToTheLargestDiscountAndApproved`; the limit is `PriceChange.MinPercentage` |
| The second rule of steps 3 and 4: when the limits apply (N5) | The approval works the suggestions out again and matches them by key, so the limits of that moment apply; `PriceChangeService` applies the stored percentage to the room rate of the day |

## Checklist Results

| # | Criterion (QC-UC-001) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Brief: one paragraph | N-A | Fully Dressed |
| 2 | Casual: a narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | Unchanged: scope, level, actor, stakeholders, conditions, scenario, extensions, rules, open issues |
| 4 | Pre- and postconditions defined | Pass | The precondition names a completed run whose report holds price suggestions; the postcondition is unchanged |
| 5 | Primary actor stated | Pass | Manager, unchanged |
| 6 | Stakeholders and interests | Pass | The interest of S01 now names the busy and the quiet days of the next two months, as [MIL-071] states the owner's wish |
| 7 | Numbered steps | Pass | Seven steps, unchanged in number |
| 8 | Include and extend used correctly | Pass | No include or extend added; the references to UC-004 and UC-006 are unchanged |
| 9 | Business rules per step | Pass | The rules of step 2 are rewritten, and a rule of steps 3 and 4 is added (F1) |
| 10 | Names match the diagram and the story | Pass | "Approve a Price Suggestion" and the Manager, as in [UCD-001] and US-001.12 |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Actor goal, no UI or implementation detail | Pass with note | The page is named only as "Price suggestions"; the first rule of step 2 says that the system holds no pricing rule of its own, which is the business decision of [MIL-071] and names no technology (F2) |

## Findings

- **F1, note: one rule more.** The new rule of steps 3 and 4 says that an approved change keeps its percentage, so a later lower room rate or a higher lowest price can give a price below the new lowest price. This is how the code works and was not written anywhere ([RC-266] N5). Whether the effective price should be floored at the lowest price of the day is a design question for S01; nothing in [MIL-055] asks for it.
- **F2, note: the source of the suggestions.** "The system holds no pricing rule of its own" is close to a design statement. It stays because it is the decision [MIL-071] was made for, and a reader of the use case needs it to see why the system never suggests a price on its own.
- **F3, note: an open issue kept.** The rule that a booking already made keeps its price still says "to be confirmed by S01". The code of [MIL-055] task 9 works that way; the confirmation is S01's and is not part of this revision.

## Overall Verdict

Go. The revision states what the system does after [MIL-071], and the three findings N1, N3 and N5 of [RC-266] that concern the use case are written into it. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted the revision and this record as stand-in for S01. The reviewer is not independent: the AI wrote the revision and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the revision of UC-007, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request of MIL-055 |
| Decide whether the effective price of an approved change is floored at the lowest price of the day (F1) | S01 | 2026-12-31 |
| Confirm that a booking already made keeps its price (F3) | S01 | 2026-12-31 |

---

[UC-007]: ../../use-cases/uc-007/uc.md
[UCD-001]: ../../use-case-diagram.md
[MIL-055]: ../../milestones/mil-055-room-price-suggestions.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[RC-264]: ./rc-264-mil-055-task-2-terms-and-model.md
[RC-266]: ./rc-266-mil-055-re-review-of-rc-265.md
[RC-329]: ./rc-329-mil-071-gate.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
