# RC-090 Review of MIL-022

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-090 |
| CrossReference | [MIL-022], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [f4649ad] |

---

## Artifact Under Review

- Instance reviewed: [MIL-022]
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat request closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Trivy, SBOM, provenance and its verification, pinning, CodeQL, coverage to SonarQube Cloud and migrations by CD |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Seven criteria; criterion 3 is the condition for any SLSA Build L2 claim |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | MIL-021 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objective 24 and success criterion 27 of BC-001; SSDF PS.2, PS.3, PW.4, PW.7 and RV.1 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner and approving reviewer S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-12-06, three weeks after MIL-021 |

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
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user's request of 2026-10-18 and the approval of decisions D1 to D10 in chat (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | GHCR, OIDC, SBOM, SLSA, CIS, DORA, SAMM, RPO, RTO and WAL are spelled out on first use; CI, CD, API, TLS and SSH are established IT terms |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes. Criterion 9 of QC-LANG-001 rests on the user's approval in chat on 2026-10-18 and the stand-in's check, not on an independent review.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-022]: ../../milestones/mil-022-supply-chain-security.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
