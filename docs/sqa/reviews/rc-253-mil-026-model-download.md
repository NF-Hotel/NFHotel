# RC-253 Review of the amendment of MIL-026 (download of missing models)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-253 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Proposed | Claude (AI) | AI at the request of the user, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-11-01 (task 22 and criterion 22: download of missing Ollama models)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the change of 2026-11-01 (a deliverable bullet, criterion 22 and task 22)
- Language and domain: en / it
- Language reviewer: none; the user's request and go-ahead in chat on 2026-11-01 close criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Task 22 names the port `IModelInstaller`, the Ollama adapter, the check in `AiDefaultService`, the background job, the state on the page and the tests with a fake Ollama |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 22 is checkable: a model that Ollama does not list is downloaded before it is saved, the states are `downloading`, `ready` and `failed`, a failure or a bad name saves nothing, and the Ollama that the service uses is the one asked |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged: the task needs the service of MIL-028 and the default model of ADR-0014, both already listed |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objective 6 of BC-001 and US-001.01; the task serves the AI analysis of that story |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged; 2026-11-27 holds |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | The added text is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | The terms default model, Ollama and download are used the same way in the deliverable, the criterion and the task |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user asked for the change and gave the go-ahead in chat on 2026-11-01 (AI at the request of the user, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (AI, API, HTTP) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes. Criterion 9 of QC-LANG-001 rests on the user's go-ahead in chat and the AI check. The Go opens only task 22; the deploy script that pulls a missing model is not part of it.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide the single Ollama setting for the analysis service and the benchmark when task 22 is built | S01 | Task 22 |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
