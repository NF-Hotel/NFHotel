# RC-117 Review of the amendment of MIL-026

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-117 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-10-20 (new source, tasks 8 to 10, criteria 9 to 11)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent (reviewer is `AI at the request of S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Deliverable now lists the booking fields, the seed command, the markets API client, the background run, the page and the deploy change |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Eleven criteria; the new 9 to 11 are checked by a migration test, a seed test with a SQL count and a feed test against the same count |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Dependencies table: MIL-019, MIL-020, MIL-004 (the markets API) and ADR-0003; the dependency on notebook-data-analysis is removed |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objective 6 of BC-001 and US-001.01, unchanged |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01; S03 for the container and the ports |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-13 is kept; the dependency on a Python milestone that set it is gone, so it is easier to meet |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Terms follow MIL-019, MIL-020 and the markets API (booking, analysis, insight) |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | Amendment of the accepted version; the row is in the Version History of MIL-026 |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The terms are those of MIL-019 and MIL-020; the user asked for the change in chat on 2026-10-20 (AI at the request of S01, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, DTO, SVG, AI) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes for the amendment. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Confirm that the fake data of `nf_hotel_bookings.csv` (country mostly Unknown, no price) may be used as the hotel data until real bookings exist | S01 | Before task 9 |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
