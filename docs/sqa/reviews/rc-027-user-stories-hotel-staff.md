# RC-027 Review of US-001 (hotel staff stories)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-027 |
| CrossReference | [US-001], [QC-US-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [a29c54a] |

---

## Artifact Under Review

- Instance reviewed: [US-001]
- Checklist used: [QC-US-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat confirmation closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Follows INVEST criteria | Pass | INVEST Check updated; US-001.06 is flagged as larger than one iteration |
| 2 | Written in "As a / I want / So that" form | Pass | US-001.06 to .08 |
| 3 | Clear, testable acceptance criteria are included | Pass | Three Given/When/Then each |
| 4 | Traceable to a use case or epic | Pass | Each traces to a UCD-001 use case |
| 5 | Story is sized to fit within a single iteration | Fail | Optional criterion: US-001.06 is large; the code exists, so the story is kept whole. Action item 2 |
| 6 | Story statement avoids technical implementation detail | Pass | No technology named |
| 7 | Role named in the story matches an actor in the Use Case Diagram | Pass | "Hotel staff" is an actor of UCD-001 |
| 8 | A story with more than one actor goal is split | Pass | One actor goal per story |
| 9 | The story has a priority with the reason beside it | Pass | Priority and Reason in every table |

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
| Confirm that S05 (hotel staff) and the stories US-001.06 to US-001.08 describe the back office correctly | S01 | Closed 2026-10-16: confirmed by the user in chat |
| Split US-001.06 if the Product Owner wants stories of one iteration | S01 | open |

---

[US-001]: ../../user-stories.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[QC-US-001]: ../../../framework/qc/artifacts/qc-user-story.md
[a29c54a]: https://github.com/NF-Hotel/NFHotel/commit/a29c54a0e6cc95b2b627748aa841ee24a399c15d
