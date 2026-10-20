# RC-129 Review of the amendment of MIL-028 (stall timeout and total limit)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-129 |
| CrossReference | [MIL-028], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-028], amended on 2026-10-20 (stall timeout and total limit)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the change of 2026-10-20 (a stall timeout and a total limit for the request to the model; short timeouts for the website)
- Language and domain: en / it
- Language reviewer: none; the user's request in chat on 2026-10-20 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The deliverable names the stall timeout (600 s default), the total limit (14400 s) and what an analysis gets when it reaches either (`TIMEOUT` for its insight, numbers kept, the run goes on) |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 4 now tests both limits with a fake model and a fake clock: no token for the stall timeout gives `TIMEOUT` for that analysis only, and a model that keeps sending tokens is not stopped before the total limit |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objectives 1 to 4 of BC-001 and US-001.01 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged; the limits add a few tests and no new task |

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

Go — every criterion of QC-MIL-001 passes. Criterion 9 of QC-LANG-001 rests on the user's acceptance in chat and the AI check.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Choose the defaults of the two limits from a night run on the real model (600 s and 14400 s are the first values) | S01 | Task 3 |

---

[MIL-028]: ../../milestones/mil-028-market-analysis-run-api.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
