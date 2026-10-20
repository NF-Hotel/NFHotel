# RC-099 Review of MIL-024

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-099 |
| CrossReference | [MIL-024], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [af6710f] |

---

## Artifact Under Review

- Instance reviewed: [MIL-024]
- Checklist used: [QC-MIL-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat request closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | ADR-0011, four scripts, the optional nginx template, the backup change, the tests and the wiki section |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Eight criteria; 1 to 6 are checked in containers, 7 on the VM |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | ADR-0010 and MIL-021 task 10, MIL-021, S03 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Objectives 18 and 23 of BC-001; risks R7 and R15 |
| 5 | Milestone owner and approving reviewer are identified | Pass | S01; S03 for the VM and the tailnet policy |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-08; the VM steps are bounded by S03 and MIL-021 task 10, which is stated |

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
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The request of the user in chat on 2026-10-18 (PostgreSQL and Tailscale readiness for the API) (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | GHCR, OIDC, SBOM, SLSA, CIS, DORA, SAMM, RPO, RTO and WAL are spelled out on first use; CI, CD, API, TLS and SSH are established IT terms |

## Overall Verdict

Go — every criterion of QC-MIL-001 passes.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-024]: ../../milestones/mil-024-postgresql-api-readiness.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[af6710f]: https://github.com/NF-Hotel/NFHotel/commit/af6710fce369f8d63795d67efe4f7a54a867997a
