# RC-255 Review of MIL-052

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-255 |
| CrossReference | [MIL-052], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [e8af9ea] |

---

## Artifact Under Review

- Instance reviewed: [MIL-052]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it
- Language reviewer: none (the reviewer reads English and knows the IT domain)
- Mode: supervised, not independent. S01 asked in chat on 2026-11-01 for the review and the acceptance ("Yes, review them"); the reviewer is the AI that wrote the plan and is also the assistant that will receive the access, so the review is not independent and stays open to S01's challenge.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | ADR-0036, the reconciled tailnet policy, the Viewer service account and the MCP server set up by S01, the read-only proof and the wiki section |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 7 criteria; criteria 3 and 7 are checked by HTTP status codes, criterion 5 by a search of the repository and the chat, criterion 6 by comparing the policy with the page. Criterion 4 is checked by the query the assistant names, which S01 can run again |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | ADR-0020 and MIL-051 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | BC-001 objectives 25 and 23, mapped to tasks. The first draft named only "BC-001" with a paraphrase; corrected during this review |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01 and S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-08; no application code or hosted service changes |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` / `it` |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English; the Danish words in the Version History are a quote of S01's request |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Executive English |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | N-A | No hotel domain terms |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | First version |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | See Mode |
| 10 | Abbreviations are spelled out on first use, in the stated language | Fail | Optional. MCP and GDPR are not spelled out |

## Overall Verdict

Go: every Mandatory criterion passes; the one failed criterion (QC-LANG-001 #10) is Optional. The plan grants no access by itself: the token is created by S01 (task 4) only after the tailnet check, the log check and ADR-0036 (tasks 1 to 3).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Spell out MCP (Model Context Protocol) and GDPR (General Data Protection Regulation) on first use in ADR-0036 (task 3) | S01 | 2026-11-08 |

---

[MIL-052]: ../../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[e8af9ea]: https://github.com/NF-Hotel/NFHotel/commit/e8af9eaddb849b4dcf173376f581a0d690a2b24e
