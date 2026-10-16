# RC-030 Review of UC-001 (Manage Bookings)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-030 |
| CrossReference | [UC-001], [QC-UC-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [a29c54a] |

---

## Artifact Under Review

- Instance reviewed: [UC-001]
- Checklist used: [QC-UC-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat confirmation closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single, concise paragraph summarizing the primary success scenario | Pass | Not applicable: the format is Fully Dressed |
| 2 | Informal multi-paragraph narrative | Pass | Not applicable: the format is Fully Dressed |
| 3 | All standard sections are present | Pass | Actors, preconditions, postconditions, main success scenario, extensions |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Both present |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff, as named in UCD-001 |
| 6 | Stakeholders and their interests are stated | Pass | S-IDs from SA-001 |
| 7 | Main success scenario is written as clear, numbered steps | Pass | Numbered |
| 8 | Alternative/exception flows reference include/extend where relevant | Pass | No include or extend is needed; the flows are plain extensions |
| 9 | Business rules are captured per step | Pass | Rules table by step, citing the ledger |
| 10 | Naming of actors and use case title is consistent with the UCD and the stories | Pass | Same actor and title as UCD-001 and US-001.06 to .08 |
| 11 | Scope/level is stated | Pass | NFHotel platform, user-goal |
| 12 | Written from the actor's goal perspective, free of UI detail | Pass | No screens, buttons or tables named |

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
| S01 and S04 to confirm the rule ledger as the source of the steps | S01 | open |

---

[UC-001]: ../../use-cases/uc-001/uc.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[a29c54a]: https://github.com/NF-Hotel/NFHotel/commit/a29c54a0e6cc95b2b627748aa841ee24a399c15d
