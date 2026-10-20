# RC-126 Review of the amendment of MIL-026 (charts by the shape of the data)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-126 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [5bc5cf0] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-10-20 (tasks 17 to 19, criteria 18 and 19)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent (reviewer is `AI at the request of S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The deliverable has a new bullet: charts that fit the data, with the six kinds and the rule that picks them; all as SVG, no script library |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Nineteen criteria. The new 18 is checked by a test that states the rule (donut for 2 to 4 parts, ranked bars above 4, columns, line, heat map, tile, bar chart as the fallback); the new 19 is checked by a component test of the markup (no donut above 4 parts, a text alternative, a second cue besides colour) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged. Tasks 17 to 19 need no run API: they are built and tested against the sample result of `docs/test-data/`, so they do not wait for MIL-028 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objective 6 of BC-001 and US-001.01; the review added tasks 17 and 18 to the Traceability row of US-001.01 |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01; S03 for the container and the ports, unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-27 is kept; the chart tasks are independent of MIL-028 and add about a week of work inside the two weeks that follow it. BC-001 sets no date for this milestone |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Terms for the chart kinds (donut, ranked bars, columns, line, heat map, tile) are used the same way in the deliverable, criteria and tasks |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user asked for the change and the review in chat on 2026-10-20 (AI at the request of S01, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, SVG, AI) are abbreviated; bUnit is named as a test library |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes after the review added tasks 17 and 18 to the Traceability row. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Look at the charts on the page in the browser pane before task 19 is closed; the rule is tested, the look is not | S01 | Task 19 |
| Decide whether a heat map is read well by the owner, or whether two lines (one per year) are better for the months | S01 | After the first look |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[5bc5cf0]: https://github.com/NF-Hotel/NFHotel/commit/5bc5cf054789ce61d82e0f429092a4e485ae7c27
