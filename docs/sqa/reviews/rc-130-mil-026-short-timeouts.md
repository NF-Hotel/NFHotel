# RC-130 Review of the amendment of MIL-026 (short timeouts)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-130 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | [c9f8d5c] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-10-20 (task 13 and criterion 14: short timeouts)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the change of 2026-10-20 (a stall timeout and a total limit for the request to the model; short timeouts for the website)
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Task 13 now says each call of the client has a short timeout and the 90000 second setting goes back to a few seconds once the run API is followed by its run id |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 14 now ends with a checkable limit: no call of the client has a timeout above 60 seconds (added during this review, because the change had no criterion) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged: the tasks wait for MIL-028 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objective 6 of BC-001 and US-001.01 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged; 2026-11-27 holds |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | The terms stall timeout and total limit are used the same way in all three documents |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user asked for the change and the review in chat on 2026-10-20 (AI at the request of the user, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, HTTP, AI) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes after criterion 14 was made checkable. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Set `MarketAnalysisService__TimeoutSeconds` back to a few seconds in the examples and the dev VM when task 13 is done | S01 | Task 13 |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[c9f8d5c]: https://github.com/NF-Hotel/NFHotel/commit/c9f8d5c7a01ebdbf4c89ce2ce4a046971d85d694
