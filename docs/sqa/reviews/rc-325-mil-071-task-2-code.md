# RC-325 Review of the code of MIL-071 task 2

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-325 |
| CrossReference | [MIL-071], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Accepted | Claude (AI) | S02 | Initial version; pre-review by the AI, not independent; accepted by S02 in chat on 2026-11-11 | [3ebb51e] |

---

## Artifact Under Review

- Instance reviewed: the contract and the client of the price suggestions (task 2 of [MIL-071]): the DTOs `MarketAnalysisPricingRequest`, `MarketAnalysisPriceSuggestionsDto` and its parts in `NFHotel.Application`, `MarketAnalysisRunner` (sends the settings of MIL-055), `MarketAnalysisServiceClient` (`pricing` in the request) and `MarketAnalysisResultMapper` (`price_suggestions`) in `NFHotel.External`, and the byte-identical copies of `market-contract.yaml` and the sample `price-suggestions-sample.json` in `docs/test-data/`
- Checklist used: [QC-CS-001] (`qc-programming-csharp`)
- Scope: full review of the changed and new files
- Language and domain: n/a (technical type)
- Mode: supervised (the AI wrote the code and the pre-review; the reviewer reads the pull request)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | Build with analyzers: 0 warnings |
| 2 | `Async` suffix, one type per file | Pass | `PricingRequestAsync`; one record per file |
| 3 | Formatter, no suppressions, explicit local types | Pass | The new locals have explicit types; `dotnet format --verify-no-changes` reports one `IDE0005` in `BookingCalendarViewModelTests.cs`, a file this change does not touch |
| 4 | Nullable references, `!` justified | Pass | No `!` in `src/`; in tests only after a value was checked |
| 5 | Disposal | Pass | No new disposable |
| 6 | Async throughout | Pass | |
| 7 | Specific exceptions, no empty catch | Pass | No new `catch`; a result the mapper cannot read gives `null`, and an entry without its dates is left out |
| 8 | XML documentation | Pass | |
| 9 | Properties, immutability | Pass | Records |
| 10 | `decimal` for money, `DateTimeOffset` for instants | Pass | Percentages and thresholds are `decimal`; the dates of a period are `DateOnly`, as in the contract |
| 11 | `CancellationToken` | Pass | Passed to the settings read and the client |
| 12 | Tests | Pass | External 216, Application 628, Api 220, ApiClient 104, Web 769, all green; new tests: `pricing` sent and left out, the mapper of schema 1.3 (suggestions, gaps, ideas, schema 1.2 without them, incomplete entries left out, no horizon), the contract properties of `RunRequest`, the runner sending the defaults and the saved settings |
| 13 | Trace to the DCD | Pass, on condition | No change to the design classes of the DCD beyond the DTOs; the DTOs mirror the schemas of the contract |
| 14 | One place for each piece of knowledge | Pass | The contract file is one copy of the service file, read by the fake and the contract test; the defaults come from `PricingSettings.Defaults` |
| 15 | References point inward | Pass | `Application` reads the settings through `IPricingSettingsService`; `External` maps to the DTOs of `Application` |
| 16 | SOLID | Pass | The runner takes the settings service by injection; without it no `pricing` is sent |
| 17 | Layers and folders | Pass | |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | Pass, on condition | The two changed classes (`MarketAnalysisRunner`, `MarketAnalysisResultMapper`) have no regions today; the new members follow the layout of those files, and regions are not added in this task |

## Overall Verdict

Go: every Mandatory criterion passes, criteria 13 and 21 on condition. The client reads schema 1.3 and still reads 1.2 (`PriceSuggestions` is `null`). When the saved settings cannot be read the run asks for the defaults. Not yet done: the page (task 4), the removal of the rule of MIL-055 (task 3) and a run against the real service.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the code of task 2 | S02 | with the pull request |
| Keep `docs/test-data/market-contract.yaml` and the sample equal to the files of notebook-market-analysis | S02 | each time the contract changes |

---

[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[QC-CS-001]: ../../framework/qc/code/qc-programming-csharp.md
[3ebb51e]: https://github.com/NF-Hotel/NFHotel/commit/3ebb51ef70966765a9acf6c81c893318fe657ade
