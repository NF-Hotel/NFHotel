# RC-268 Review of the amendment of MIL-026 (follow the run that is going on)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-268 |
| CrossReference | [MIL-026], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Proposed | Claude (AI) | AI at the request of the user, not independent | Initial version | [e5aa58d] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026], amended on 2026-11-01 (task 26 and criterion 26: the page follows the run that is going on)
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review of the change of 2026-11-01 (a deliverable entry, criterion 26 and task 26)
- Language and domain: en / it
- Language reviewer: none; the user's go-ahead in chat on 2026-11-01 closes criterion 9
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Task 26 names the read on the tracker, the endpoint `GET /api/v1/market-analysis/runs/current` with `204`, the changes in the service, client and OpenAPI file, and the view model that follows a run that is `queued` or `running`; the Deliverable has an entry for it |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 26 is checkable by tests: a run going on is followed at once with its start time and a closed start button, an ended run and no run show the stored report as before |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | None needed: the task changes only the web API and the page of this milestone; the service of MIL-028 is not changed |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objective 6 of BC-001 and US-001.01; the row "All tasks" covers task 26 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged; 2026-11-27 holds |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | The added text is English; checked by the AI, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Run, tracker, report and state are used as in the earlier tasks |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user asked for the change and gave the go-ahead in chat on 2026-11-01 (AI at the request of the user, not independent) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, OpenAPI) are abbreviated |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes. Criterion 9 of QC-LANG-001 rests on the user's go-ahead in chat and the AI check. The tracker is in memory, so a restart of the API still forgets the run; the task says so.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Reload the page during a run on the dev VM and check that the state shows | S01 | After task 26 |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[e5aa58d]: https://github.com/NF-Hotel/NFHotel/commit/e5aa58daf3105bf7f3dfa68a284ef2bc40861aeb
