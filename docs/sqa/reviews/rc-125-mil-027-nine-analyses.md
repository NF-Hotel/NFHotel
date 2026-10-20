# RC-125 Review of the amendment of MIL-027 (nine analyses, chart observations)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-125 |
| CrossReference | [MIL-027], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-027], amended on 2026-10-20 (nine analyses, `room_count`, an observation per chart)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the changes of 2026-10-20 (nine analyses, occupancy, correlation matrix, an AI observation for each chart)
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The deliverable now names the nine analyses, `room_count`, the formula and limitations of each analysis, and an observation per chart |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | The criteria stay checkable: criterion 3 has three hand calculations (50 % occupancy for two rooms with one taken, occupancy on holidays differing from other days, correlation +1 and -1); criterion 4 has the observation test (the numbers it names are found in the series, an invented number is dropped) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged; NFHotel MIL-026 tasks 13 to 21 depend on this milestone and say so |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objectives 1 to 4 of BC-001 and US-001.01; unchanged |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01 owns; S02 and S03 approve |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-13 is kept but the work grew by three analyses and the observations; the date is a target of S01 and not a constraint of BC-001, so it passes with the action item to confirm it |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Terms for the new analyses (occupancy, occupancy on holidays, correlation) and for the observation of a chart are used the same way in every section and in both repositories |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user asked for the change and the review in chat on 2026-10-20 (AI at the request of the user, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, AI, SVG) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes with two action items. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Confirm the target date 2026-11-13 now that three analyses and the chart observations are added | S01 | Before task 2 starts |
| Measure the run time of the nine analyses with the AI observations on the real model; if a run exceeds 12 minutes, ask for the observations only on request | S01 | Task 3 |

---

[MIL-027]: ../../milestones/mil-027-market-analysis-run-api.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
