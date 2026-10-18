# RC-084 Review of MIL-018

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-084 |
| CrossReference | [MIL-018], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-018]
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat request closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent (reviewer is `AI at the request of S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Deliverable lists ADR-0007, the sign-in use case, the API, the guest pages, the smoke test and the deploy files |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Eight objectively checkable criteria (status codes, token ownership, cookie attributes, test results) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Dependencies table: MIL-016, MIL-017 and ADR-0005 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Traceability maps to new objective 21 and success criterion 24 |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01 for both; S03 for the token, the cookie and the deploy changes |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2027-03-19 follows MIL-017 (2027-03-05) by two weeks |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | `docs/dictionary.md` has no term for test tooling; terms (unit test, integration test, fixture, fake) are used consistently |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The terms are those of the framework's test rules and of the user's request of 2026-10-18 (AI at the request of S01, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (CI, SDK) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes. Criterion 9 of QC-LANG-001 rests on the user's request in chat on 2026-10-18 and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-018]: ../../milestones/mil-018-guest-login.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
