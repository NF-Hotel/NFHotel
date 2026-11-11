# RC-328 Review of the wiki page and the traceability matrix of MIL-071 task 5

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-328 |
| CrossReference | [MIL-071], [QC-WIKI-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Proposed | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the wiki page `docs/wiki/Price-Suggestions.md` (the rule is now the rule of the market analysis service, the settings are sent to it, the page shows the reason, the history and the labelled AI texts, the empty states, the limits), the line in `docs/wiki/Home.md`, and the traceability matrix [TM-001] (the rows of [MIL-071], its code and tests, the reviews RC-323 to RC-327, the removed rule)
- Checklist used: [QC-WIKI-001] (`qc-wiki`) for the wiki; the matrix is checked for rows and links
- Scope: delta review of the changed text and rows
- Language and domain: en / it; the PO language of the project is English (`docs/artifact-registry.md`), so the wiki exists once, in English
- Mode: supervised (the AI wrote the change and the pre-review; S01 reviews)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The wiki has a `_Sidebar.md` | Pass | Unchanged |
| 2 | The sidebar starts with the title, then one list, `Home` first | Pass | Unchanged |
| 3 | The page list links every English page, and no link is dead | Pass | No page added or removed; the new links in the page point to `Home` and to files that exist |
| 4 | A `Related` section only when there are related wikis | N-A | Unchanged |
| 5 | A `Languages` section last | N-A | Unchanged |
| 6 | The sidebar does not repeat the list per language | Pass | |
| 7 | A generated sidebar names its generator | N-A | The sidebar is written by hand |
| 8 | A language line on each page | Pass | `**English**` at the top of the page |
| 9 | The wiki exists in English and in the PO language | Pass | The PO language is English |
| 10 | The page sets are identical in every language | N-A | One language |
| 11 | Every page starts with the language line | Pass | |
| 12 | Page names carry the language code | N-A | English has no suffix |
| 13 | Code, commands, file names and IDs stay in English | Pass | |
| 14 | The sidebar, every page and every link were checked on the published wiki | Open | Done after the merge, when the publish workflow has copied the page; recorded in the review of task 6 |
| 15 | A change to the layout rules is made in the template | N-A | No change to the layout |

The facts of the page were read against the code and the service documentation: the horizon, the two earlier years at least, the holidays by name, the weeks without holiday days, the thresholds and percentages of the settings, the cap and the minimum price applied by the website, the labels "Written by an AI" and "Built from the figures", the empty states and the limits (whole-hotel occupancy, no suggestion without a base rate or with too little history).

Traceability matrix: [MIL-071] has its row with the reviews RC-323 to RC-328 and the link in the row of [BC-001] and [MIL-055]; the rows of the code say that `SeasonRule` and `PriceSuggestionRule` are removed; the contract, client and page tests have rows; the version history keeps its two latest rows.

## Overall Verdict

Go: every Mandatory criterion that applies passes, except criterion 14, which cannot be checked before the merge. The matrix holds every artifact of the milestone. The notebook-market-analysis repository has its own documentation task (its MIL-002 task 6); the wiki page describes the rule in plain words and does not copy it.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the wiki page and the matrix rows | S01 | with the pull request |
| Check the published wiki page and its links after the merge | S02 | task 6 |

---

[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[TM-001]: ../traceability-matrix.md
[BC-001]: ../business-case.md
[MIL-055]: ../milestones/mil-055-room-price-suggestions.md
[QC-WIKI-001]: ../../framework/qc/artifacts/qc-wiki.md
