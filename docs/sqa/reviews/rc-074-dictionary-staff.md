# RC-074 Review of DICT-001 (staff terms)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-074 |
| CrossReference | [DICT-001], [QC-DICT-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [DICT-001]
- Checklist used: [QC-DICT-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat request closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every row has a PO term, its language, an IT term and a definition | Pass | 29 rows, all four cells filled |
| 2 | Each PO term maps to exactly one IT term and the reverse (no synonyms) | Pass | 29 distinct PO terms and 29 distinct IT terms; Role maps to StaffRole, Password to PasswordHash, Active to IsActive |
| 3 | Every Domain Model concept has a row, and the Domain Model uses its PO term | Pass | Staff member, Role and every attribute of DM-005 (user name, password, active) have a row |
| 4 | OC, SD, DCD and ERD use the IT term, not the PO term | Pass | None of these exists for sign-in; the rule is stated for them |
| 5 | Definitions are written in the PO language and are one sentence | Pass | English, one sentence each |
| 6 | The "used in" columns name artifact types that exist | Pass | DM, UC and US exist |
| 7 | `Language` and `Domain` rows and every row match the registry | Pass | `en`, `it` |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the stand-in, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | The terms Staff member, User name, Role, Password and Active are rows of `docs/dictionary.md` (DICT-001) and are used as defined; no synonym |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The terms are those of the framework's test rules and of the user's request of 2026-10-17 (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (CI, SDK) are abbreviated |

## Overall Verdict

Go — every Mandatory criterion passes. Criterion 9 of QC-LANG-001 rests on the user's request in chat on 2026-10-17 and the stand-in's check, not on an independent language review.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[DICT-001]: ../../dictionary.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
