# RC-327 Review of the code of MIL-071 task 4

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-327 |
| CrossReference | [MIL-071], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Proposed | Claude (AI) | S02 | Initial version; pre-review by the AI, not independent | [e68bde9] |

---

## Artifact Under Review

- Instance reviewed: the suggestions page (task 4 of [MIL-071]): `PriceSuggestions.razor` and `PriceSuggestionsViewModel` in `src/NFHotel.Web/Features/Admin/Pricing`. The page shows the reason (name of the holiday, or busy or quiet on the same days in earlier years), the occupancy of each earlier year, the explanation of each suggestion and the further ideas, each text labelled "Written by an AI" or "Built from the figures"; the empty states are reworded; Approve, Revert and the other lists are unchanged
- Checklist used: [QC-CS-001] (`qc-programming-csharp`)
- Scope: full review of the changed files and the new tests
- Language and domain: n/a (technical type)
- Mode: supervised (the AI wrote the code and the pre-review; the reviewer reads the pull request)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | Build: 0 warnings |
| 2 | `Async` suffix, one type per file | Pass | |
| 3 | Formatter, no suppressions, explicit local types | Pass | `dotnet format --verify-no-changes` reports only the `IDE0005` in `BookingCalendarViewModelTests.cs`, which this change does not touch |
| 4 | Nullable references, `!` justified | Pass | No `!` in `src/` |
| 5 | Disposal | N-A | |
| 6 | Async throughout | Pass | |
| 7 | Specific exceptions, no empty catch | Pass | No new `catch` |
| 8 | XML documentation | Pass | The new members and records are documented |
| 9 | Properties, immutability | Pass | Records; `Ideas` has a private setter |
| 10 | `decimal` for money | Pass | The page shows the prices from the application layer as before |
| 11 | `CancellationToken` | Pass | Unchanged |
| 12 | Tests | Pass | Web 778 (3 new page tests with bUnit and 4 view model tests), Application 572, Api 220, ApiClient 104 pass. Criterion 4 of [MIL-071]: a test gives an AI text with "999 kroner" and "90 percent" and shows that change, move and basis of the row are the same; criterion 7: the page without a report says that nothing can be approved and has no Approve button |
| 13 | Trace to the DCD | Pass | No design class changes |
| 14 | One place for each piece of knowledge | Pass | The labels are two constants of the view model; the percentage and price are not computed in the page |
| 15 | References point inward | Pass | |
| 16 | SOLID | Pass | Markup and binding only; the texts are made in the view model |
| 17 | Layers and folders | Pass | |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | Pass, on condition | The view model has no regions today; not added in this task (as RC-325) |

## Overall Verdict

Go: every Mandatory criterion passes, criterion 21 on condition. Not yet done: the look at the page on the dev VM with a real run of the service (criterion 9 of [MIL-071]), which is task 6; and the wiki and the traceability matrix (task 5). The weeks without enough history that the service lists are not shown on the page; the page shows only what can be approved.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the code of task 4 | S02 | with the pull request |
| Look at the page on the dev VM with a real run | S02 | task 6 |

---

[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[QC-CS-001]: ../../framework/qc/code/qc-programming-csharp.md
[e68bde9]: https://github.com/NF-Hotel/NFHotel/commit/e68bde92fb7c7286fb911e6ab4b711a7577debe4
