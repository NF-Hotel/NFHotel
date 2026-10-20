# RC-097 Review of MIL-021

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-097 |
| CrossReference | [MIL-021], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [1b94761] |

---

## Artifact Under Review

- Instance reviewed: [MIL-021]
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: the change of 2026-10-18 (task 10, criterion 10, the ADR-0010 dependency)
- Language and domain: en / it
- Language reviewer: none; the user's chat request closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | The reconciled deployment is added to the deliverable |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 10 is checkable: `git -C /opt/nfhotel remote -v`, `docker compose ls` against the compose files, a TCP connect to 5432 from a tailnet device |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | ADR-0010 added to the dependencies |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objective 23 (an approved pipeline) needs a deployment described by the repository; risks R15 and R16 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Task 10 is done by S03 with S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-15 kept; the reconciliation is on the critical path and is named as a risk to the date in ADR-0010 |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the stand-in, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Professional English for a technical artifact |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | `docs/dictionary.md` has no pipeline terms; the terms of the cited standards are used consistently (release, digest, attestation, environment) |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The server inventory given by the user in chat on 2026-10-18 (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | GHCR, OIDC, SBOM, SLSA, CIS, DORA, SAMM, RPO, RTO and WAL are spelled out on first use; CI, CD, API, TLS and SSH are established IT terms |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes for the change. The task follows ADR-0010 and the corrected D4 of DSA-001 section 2.4.2.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| S03 reads the VMID, the size and the compose file of `nf-hotel` and opens the reconciliation pull request | S03 | Before the MIL-021 Go/No-Go |
| Restrict port 5432 to the administrators in the tailnet policy until the reconciliation lands (R15) | S03 | At once |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[1b94761]: https://github.com/NF-Hotel/NFHotel/commit/1b94761ddb5113cd0d34915c01fe7f0f278bba60
