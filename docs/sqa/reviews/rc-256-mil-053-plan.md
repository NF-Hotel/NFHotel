# RC-256 Review of MIL-053

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-256 |
| CrossReference | [MIL-053], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [de27b09] |

---

## Artifact Under Review

- Instance reviewed: [MIL-053]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it
- Language reviewer: none (the reviewer reads English and knows the IT domain)
- Mode: supervised, not independent. S01 asked in chat on 2026-11-01 for the plan and its implementation ("Ja og implementere det også"), which is the go-ahead; the reviewer is the AI that wrote the plan, so the review stays open to S01's challenge.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The exporter with its sources, the token file, the scrape job, the rules, the dashboard, the tests and three wiki pages |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 7 criteria: test results in Ops CI, `up{job="scans"}`, the container settings, a search for the token, and the dashboard compared with GitHub and sonarcloud.io |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | ADR-0020, ADR-0007; MIL-051 named as not required, with the manual deploy as the path until then |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | BC-001 objectives 24 and 25, mapped to tasks |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01 and S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-08 |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` / `it` |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English; the Danish words in the Version History quote S01 |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Executive English |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | N-A | No hotel domain terms |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | First version |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | See Mode |
| 10 | Abbreviations are spelled out on first use, in the stated language | Fail | Optional. SAST, DAST and CI are not spelled out |

## Overall Verdict

Go: every Mandatory criterion passes; the one failed criterion (QC-LANG-001 #10) is Optional.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Spell out SAST (static application security testing) and DAST (dynamic application security testing) on the Monitoring page where the dashboard is described (task 6) | S01 | 2026-11-08 |

---

[MIL-053]: ../../milestones/mil-053-security-scan-metrics.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[de27b09]: https://github.com/NF-Hotel/NFHotel/commit/de27b09fe2cff91b1db4c40ec2f609b0bf0ff29d
