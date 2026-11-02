# RC-254 Review of MIL-051

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-254 |
| CrossReference | [MIL-051], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [e8af9ea] |

---

## Artifact Under Review

- Instance reviewed: [MIL-051]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it
- Language reviewer: none (the reviewer reads English and knows the IT domain)
- Mode: supervised, not independent. S01 asked in chat on 2026-11-01 for the review and the acceptance ("Yes, review them"); the reviewer is the AI that wrote the plan, so the review is not independent and stays open to S01's challenge.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | ADR-0035, the automatic deploy with its path filter, checks, health check and rollback, the alert, the tests in `ops/tests` and the wiki section |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 8 criteria, each checkable by a log, a test result, the deployed commit on the LXC or the tailnet policy; criterion 2 has a time limit of 15 minutes |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | ADR-0020, ADR-0007 and MIL-023; MIL-052 is cross-referenced as the read-only counterpart, without an ordering dependency |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | BC-001 objectives 23 and 25, mapped to tasks. The first draft named only "BC-001" with a paraphrase; corrected during this review |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01 and S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-13; no application code or hosted service changes, so no Business Case date moves |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` / `it` |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English; the Danish words in the Version History are a quote of S01's request |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Executive English: purpose and decision first, technical detail in the deliverable |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | N-A | No hotel domain terms; the IT terms are used consistently (LXC, `nf-monitoring`, `tag:ci`, `tag:monitoring`) |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | First version |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The reviewer knows the IT domain; S01 may challenge it, see Mode |
| 10 | Abbreviations are spelled out on first use, in the stated language | Fail | Optional. LXC, OIDC and CD are not spelled out; they are spelled out in ADR-0007 and ADR-0020, which the milestone cites |

## Overall Verdict

Go: every Mandatory criterion passes; the one failed criterion (QC-LANG-001 #10) is Optional.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Spell out LXC, OIDC and CD on first use in ADR-0035 (task 1), where a reader meets them first | S01 | 2026-11-13 |

---

[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[e8af9ea]: https://github.com/NF-Hotel/NFHotel/commit/e8af9eaddb849b4dcf173376f581a0d690a2b24e
