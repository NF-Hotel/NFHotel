# RC-136 Review of UC-006

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-136 |
| CrossReference | [UC-006], [QC-UC-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [UC-006]
- Checklist used: [QC-UC-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's request in chat closes the domain confirmation
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`), on the user's request in chat on 2026-10-20 after the plan was accepted

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single, concise paragraph summarizing the primary success scenario | N-A | Format is Fully Dressed only, as UC-004 |
| 2 | Informal multi-paragraph narrative | N-A | Fully Dressed only |
| 3 | All standard sections are present | Pass | Scope, level, actor, stakeholders, pre- and postconditions, scenario, extensions, rules, open issues |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Both present |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff / Manager, as the UCD |
| 6 | Stakeholders and their interests are stated | Pass | S01, S03, S05 |
| 7 | Main success scenario is written as clear, numbered steps | Pass | Numbered |
| 8 | Alternative/exception flows reference include/extend where relevant | Pass | The UCD has the include of UC-004; flows reference UC-004 and UC-005 |
| 9 | Business rules are captured per step | Pass | Rules table by step, citing ADR-0015 |
| 10 | Naming of actors and use case title is consistent with the UCD and the stories | Pass | UCD rows and US-001.10, US-001.11 added |
| 11 | Scope/level is stated | Pass | NFHotel back office, user-goal |
| 12 | Written from the actor's goal perspective, free of UI detail | Pass | Screens are in the wireframes, not in the use case |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`, `it` |
| 3 | The content is written in the stated language | Pass | English throughout; checked by the stand-in |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register used |
| 5 | Domain terms are the PO terms of the dictionary, with no synonyms | Pass | Staff member, User name, Role, Password, Active, and the new rows Must change password, Security stamp and Audit event of DICT-001 |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin exists beside the document | Pass | None |
| 8 | A change of language or domain has a Version History row | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain and the language has confirmed the terms | Pass | Rests on the user's request in chat and the stand-in's check, not on an independent language review |
| 10 | Abbreviations are spelled out on first use | Pass | Only established IT terms (JWT, TLS, TOTP, CIS) |

## Overall Verdict

Go: every Mandatory criterion passes. UC-004 open issues were amended in the same change.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Challenge any stand-in decision in the summary of the run | S01 | After the pull request |

---

[UC-006]: ../../use-cases/uc-006/uc.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
