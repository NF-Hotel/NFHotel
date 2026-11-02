# RC-271 Review of MIL-056 Local development stack without KVM

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-271 |
| CrossReference | [MIL-056], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; S01 accepted the plan in chat on 2026-11-02 ("Yes, accept and build") | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-056]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it (the Metadata rows of MIL-056)
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: autonomous, not independent (the AI wrote the milestone and this review; S01 accepted the plan in chat)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | An ADR, the shared library, `dev-up.sh --lite`, portable scripts and a better check, tests in Ops CI and on macOS, and the docs |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 8 criteria, each a test that runs or a record that can be read: the two addresses, 6 GiB, the Mac test by S02, the macOS job, the shared library, the secrets, the Proxmox mode, the code review |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | MIL-027, ADR-0003, ADR-0015 and ADR-0024, each with its reason |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Developer tooling for the guilds (S02) of BC-001 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01, approving reviewer S01, tester on macOS S02 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-16; developer tooling only, no date of the Business Case moves |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | |
| 3 | Content in the stated language | Pass | English; the owner's words are quoted in Danish in the Version History only |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | Light mode and Proxmox mode are named once and used the same way throughout |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | First version |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01 read the plan and accepted it in chat |

## Overall Verdict

Go: every Mandatory criterion passes. Criterion 3 of MIL-056 needs a Mac, which neither S01 nor the AI has, so task 7 stays open until a developer of S02 has run the light mode and the result is written in the review of the code.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `dev-up.sh --lite` on a Mac and sign in from the back office and the mobile app | S02 | 2026-11-16 |

---

[MIL-056]: ../../milestones/mil-056-dev-stack-without-kvm.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
