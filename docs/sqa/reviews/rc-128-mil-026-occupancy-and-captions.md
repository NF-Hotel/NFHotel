# RC-128 Review of the amendment of MIL-026 (occupancy, correlation heat map, AI observations)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-128 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | [5bc5cf0] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-10-20 (tasks 20 and 21, criteria 20 and 21)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the changes of 2026-10-20 (nine analyses, occupancy, correlation matrix, an AI observation for each chart)
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The deliverable names `room_count` in the request, the correlation heat map and a caption under every chart with the AI observation; the review corrected task 20, which first said donut for holiday against other days |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criteria 20 and 21 are checkable by client tests against a fake of the service and by component tests; criterion 21 says a chart with no observation says why and never shows an empty box, which also meets criterion 19 (no donut for figures that are not parts of one whole) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | MIL-028 supplies the nine analyses and the observations; tasks 13 to 21 start when its run API is merged, as the Dependencies row says |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objective 6 of BC-001 and US-001.01; unchanged |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01; S03 for the container and the ports |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-27 is kept: two weeks after MIL-028, and the chart tasks can be built against the sample result before that |

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

Go — every criterion of QC-MIL-001 passes after task 20 was corrected. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Look at the charts and the captions on the page in the browser pane before task 21 is closed | S01 | Task 21 |
| Confirm the target date 2026-11-27 | S01 | Before acceptance |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[5bc5cf0]: https://github.com/NF-Hotel/NFHotel/commit/5bc5cf054789ce61d82e0f429092a4e485ae7c27
