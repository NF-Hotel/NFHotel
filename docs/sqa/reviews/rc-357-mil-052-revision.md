# RC-357 Review of the revision of MIL-052 of 2026-11-14

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-357 |
| CrossReference | [MIL-052], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; accepted at S01's word in chat ("Du godkender for mig") | [62301c4] |

---

## Artifact Under Review

- Instance reviewed: [MIL-052], its row of 2026-11-14: the ADR of task 3 without a fixed number, and the target date moved from 2026-11-08 to 2026-12-02
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: partial review, the changed parts; the plan as a whole was reviewed in RC-255
- Language and domain: en / it (the Metadata rows of MIL-052)
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-14 for every unfinished milestone to be done ("Tjek nu og lav alle milestones") and then gave the acceptance of the plan to the AI ("Du godkender for mig"); the AI walked the checklists and accepted as stand-in for S01.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Unchanged: the ADR, the tailnet policy reconciled with the documentation, the service account and the MCP server set up by S01, the check that the access is read-only; the ADR takes the next free number, because ADR-0036 went to the start from Visual Studio of MIL-057 |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Unchanged, seven criteria; criterion 1 names the ADR of task 3 instead of ADR-0036 |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged: ADR-0020 and MIL-051 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-12-02, after MIL-051 in the second wave of the plan of 2026-11-14; the date of 2026-11-08 had passed with nothing built |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | Unchanged |
| 3 | Content in the stated language | Pass | English |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | Unchanged |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | No new term |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | No change of language or domain |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | The AI as stand-in for S01, not independent |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | No new abbreviation |

## Findings

- **The number in the text was stale.** ADR-0036 went to MIL-057 after this plan was accepted; naming the ADR by its task keeps the plan true until the ADR is created with the next free number.
- **Two steps stay S01's.** Task 4: S01 creates the Viewer service account and its token and registers `mcp-grafana`; the AI never sees the token. Task 1: if the real tailnet policy is wider than the documented one, narrowing it is a change of a security setting, which is S01's.
- **Not independent.** The AI wrote the change and walked the checklists; S01 gave the acceptance to the AI in chat.

## Overall Verdict

Go: every Mandatory criterion of QC-MIL-001 passes for the changed parts. The row of 2026-11-14 of MIL-052 is Accepted and the row of e8af9ea Deprecated.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Publish the milestone and its six issues (`sync-project.sh --apply --accepted-only --milestone MIL-052`) after the merge | The AI, in autonomous mode | 2026-11-14 |
| Create the Viewer service account and its token, and register `mcp-grafana` (task 4) | S01 | When task 3 is accepted |

---

[MIL-052]: ../../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[62301c4]: https://github.com/NF-Hotel/NFHotel/commit/62301c485e2ba5305b590865c00d02ad24988e59
