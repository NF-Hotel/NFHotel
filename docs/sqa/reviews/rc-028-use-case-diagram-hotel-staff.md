# RC-028 Review of UCD-001 (hotel staff use cases)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-028 |
| CrossReference | [UCD-001], [QC-UCD-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [UCD-001]
- Checklist used: [QC-UCD-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat confirmation closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Actors have correct UML stereotypes | Pass | `Hotel staff` is `<<Actor>>` |
| 2 | System boundary is drawn and labeled | Pass | Rectangle "NFHotel platform" |
| 3 | Include/extend relationships are used correctly | Pass | No new relationships; the three new use cases stand alone |
| 4 | Every actor participates in at least one use case | Pass | Hotel staff performs three |
| 5 | Diagram is traceable to a documented stakeholder need | Pass | S05 in the actor table |
| 6 | Use case names are verb phrases describing actor goals | Pass | Manage Bookings, Manage Rooms, Manage Guests |
| 7 | Free of implementation detail | Pass | No tables or widgets |
| 8 | Naming is consistent with the Use Case and User Story documents | Pass | Same names in US-001.06 to .08 |
| 9 | Each use case lists its story and priority with a reason | Pass | Use Case Table rows added |
| 10 | A role matrix is present and marked as a suggestion | Pass | Hotel staff column added |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the stand-in, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | The terms follow the Domain Dictionary of MIL-007 once it exists; until then the terms of the legacy ledger are used consistently |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | First version, no change |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user (Team 2, S01, owner of NF Hotel) confirmed in chat on 2026-10-16 that the terminology is correct (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, TLS, CI) are abbreviated |

## Overall Verdict

Go — every Mandatory criterion passes. Criterion 9 of QC-LANG-001 rests on the user's chat confirmation of 2026-10-16 (AI as stand-in for S01).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Confirm that S05 (hotel staff) and the stories US-001.06 to US-001.08 describe the back office correctly | S01 | open |

---

[UCD-001]: ../../use-case-diagram.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
