# RC-326 Review of the code of MIL-071 task 3

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-326 |
| CrossReference | [MIL-071], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | Claude (AI) | S02 | Initial version; pre-review by the AI, not independent | [5649c48] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S02 | Accepted by the AI as stand-in for S02 at S01's word in chat ("Ja, godkend dem", to "Skal jeg godkende de fire reviews af MIL-071 i S02's sted og lukke milestonen?"); mode autonomous, not independent; the action items of tasks 4 and 5 marked done | [e26fdf9] |

---

## Artifact Under Review

- Instance reviewed: the replacement of the pricing rule (task 3 of [MIL-071]): `PriceSuggestionService` maps the price suggestions of the latest report; `SeasonRule`, `PriceSuggestionRule`, `CalendarSeasons`, `MonthSeason`, `Season`, `PriceSuggestion`, `OccupancyObservation` and `RoomRateValue` are removed; `OccupancyObservationReader` keeps only the reading of the explanation; the DTOs `PriceSuggestionItemDto` and `PriceSuggestionListDto` gain the reason, holiday, history, explanation and ideas; the comment of the DCD; the architecture test `NoPriceRuleTests`
- Checklist used: [QC-CS-001] (`qc-programming-csharp`)
- Scope: full review of the changed and new files
- Language and domain: n/a (technical type)
- Mode: supervised for the pre-review (the AI wrote the code and the pre-review); accepted on 2026-11-14 in autonomous mode by the AI as stand-in for S02, at S01's word in chat: not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | Build: 0 warnings |
| 2 | `Async` suffix, one type per file | Pass | One type per file |
| 3 | Formatter, no suppressions, explicit local types | Pass | Explicit types in the new code; `dotnet format --verify-no-changes` reports only the `IDE0005` in `BookingCalendarViewModelTests.cs`, which this change does not touch |
| 4 | Nullable references, `!` justified | Pass | No `!` in `src/` |
| 5 | Disposal | N-A | |
| 6 | Async throughout | Pass | |
| 7 | Specific exceptions, no empty catch | Pass | No new `catch` |
| 8 | XML documentation | Pass | |
| 9 | Properties, immutability | Pass | Records |
| 10 | `decimal` for money | Pass | The new price is worked out in `decimal` by the existing `PriceMath`; the shares of the service (`double`) are turned into percent with one decimal for display only |
| 11 | `CancellationToken` | Pass | |
| 12 | Tests | Pass | Application 572, Api 220, ApiClient 104, Web 769, Infrastructure 182 pass. Changed: the hand figures of the list (20 percent on every size, 15 percent on the single room, with reason, history, text and ideas), the largest change cutting a percentage of the service, the minimum price (regression RC-265 F1 kept), the order of the sizes, a report without suggestions giving `no_basis`; the tests of the removed rule are deleted; the architecture test fails when a season or a suggestion rule returns |
| 13 | Trace to the DCD | Pass | The DCD says that the service makes the suggestion; `PriceSuggestion` was never a class of the DCD |
| 14 | One place for each piece of knowledge | Pass | The percentage is decided in the service; the website keeps only the cap and the minimum price, in one method; `PriceMath` is unchanged |
| 15 | References point inward | Pass | |
| 16 | SOLID | Pass | `PriceSuggestionService` no longer holds a rule |
| 17 | Layers and folders | Pass | |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | Pass, on condition | The changed classes have no regions today; not added in this task (as RC-325) |

## Overall Verdict

Go: every Mandatory criterion passes, criterion 21 on condition. Go/No-Go criteria 2 (no `SeasonRule` or `PriceSuggestionRule`, architecture test) and 3 (20 percent of 100 gives 120, the cap and the minimum price) of [MIL-071] hold for the application layer. Left for the next tasks: the page text still says "twelve calendar months" in its empty state (task 4), and the traceability matrix still names the removed classes (task 5). A suggestion for a size without a room rate is left out.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the code of task 3 | AI as stand-in for S02 | done 2026-11-14 |
| Reword the empty states and show the new fields on the page | S02 | done in task 4 (RC-327) |
| Bring the traceability matrix up to date | S02 | done in task 5 (RC-328) |

---

[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[QC-CS-001]: ../../framework/qc/code/qc-programming-csharp.md
[5649c48]: https://github.com/NF-Hotel/NFHotel/commit/5649c4831433f348867ddeb3163554401d39f2cd
[e26fdf9]: https://github.com/NF-Hotel/NFHotel/commit/e26fdf91732c20beb97bceea4b9bfdda9606da52
