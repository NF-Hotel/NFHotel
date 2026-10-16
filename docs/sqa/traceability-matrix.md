# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 |
| CrossReference | [BC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S04 | Initial version | [b690b57] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (instructed by the user) | The traceability matrix covers BC, SA, PP, MIL-001 to MIL-005, US, UCD, ADR and RC-001 to RC-020 | [3db0f51] |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003], [MIL-004], [MIL-005], [MIL-006], [MIL-007], [MIL-008], [MIL-009], [UCD-001], [US-001] | [RC-021], [RC-024], [RC-037], [RC-040] |
| [SA-001] | Stakeholder Analysis | en | it | [BC-001] | - | [RC-009], [RC-026] |
| [MIL-001] | Milestone | en | it | [BC-001] | - | [RC-003] |
| [MIL-002] | Milestone | en | it | [BC-001] | - | [RC-006] |
| [MIL-003] | Milestone | en | it | [BC-001] | - | [RC-010] |
| [MIL-004] | Milestone | en | it | [BC-001], [US-001] | - | [RC-015] |
| [MIL-005] | Milestone | en | it | [BC-001] | - | [RC-018] |
| [MIL-006] | Milestone | en | it | [BC-001] | - | [RC-022], [RC-023] |
| [MIL-007] | Milestone | en | it | [BC-001] | [DM-004] | [RC-025] |
| [MIL-008] | Milestone | en | it | [BC-001], [MIL-005] | - | [RC-038], [RC-039] |
| [MIL-009] | Milestone | en | it | [BC-001], [ADR-0002], [MIL-008] | - | [RC-041] |
| [ADR-0002] | ADR | en | it | [BC-001], [ADR-0001], [DM-004] | [MIL-009] | [RC-042] |
| [ADR-0001] | Architecture Decision Record | - | - | [BC-001] | [MIL-005] | [RC-019] |
| `src/` (Domain, Application, Infrastructure) | Source code | - | - | [ADR-0001], [MIL-005] | - | [RC-020] |
| [PP-001] | Project Plan | en | it | [BC-001], [SA-001] | [MIL-001], [MIL-002], [MIL-003] | - |
| [UCD-001] | Use Case Diagram | en | it | [SA-001], [BC-001] | [US-001] | [RC-013], [RC-028] |
| [US-001] | User Story | en | it | [UCD-001], [BC-001] | - | [RC-012], [RC-027] |
| [DICT-001] | Domain Dictionary | en | it | [BC-001], [SA-001] | [DM-004] | [RC-029] |
| [UC-001] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-001] | [RC-030] |
| [UC-002] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-002] | [RC-031] |
| [UC-003] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-003] | [RC-032] |
| [DM-001] | Domain Model | en | it | [UC-001], [UCD-001], [DICT-001] | [DM-004] | [RC-033] |
| [DM-002] | Domain Model | en | it | [UC-002], [UCD-001], [DICT-001] | [DM-004] | [RC-034] |
| [DM-003] | Domain Model | en | it | [UC-003], [UCD-001], [DICT-001] | [DM-004] | [RC-035] |
| [DM-004] | Domain Model | en | it | [UC-001], [UC-002], [UC-003], [DM-001], [DM-002], [DM-003], [DICT-001] | - | [RC-036] |
| `tests/NFHotel.Proxmox.Tests`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-001] | - | [RC-004] |
| `deploy/`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-002] | - | [RC-007] |
| `deploy/proxmox`, `scripts/pve-token.sh`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-003] | - | [RC-011] |
| `deploy/market-analysis`, `deploy/nginx`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-004] | - | [RC-016] |

---

[BC-001]: ../business-case.md
[SA-001]: ../stakeholder-analysis.md
[MIL-001]: ../milestones/mil-001-proxmox-test-env.md
[PP-001]: ../project-plan.md
[RC-001]: ./reviews/rc-001-business-case.md
[RC-002]: ./reviews/rc-002-stakeholder-analysis.md
[RC-003]: ./reviews/rc-003-milestone-001.md
[RC-004]: ./reviews/rc-004-proxmox-tests-code.md
[RC-005]: ./reviews/rc-005-business-case-amendment.md
[RC-006]: ./reviews/rc-006-milestone-002.md
[RC-007]: ./reviews/rc-007-compose-and-smoke-test.md
[RC-008]: ./reviews/rc-008-business-case-throwaway-proxmox.md
[RC-009]: ./reviews/rc-009-stakeholder-analysis-update.md
[RC-010]: ./reviews/rc-010-milestone-003.md
[RC-011]: ./reviews/rc-011-throwaway-proxmox-code.md
[RC-012]: ./reviews/rc-012-user-stories.md
[RC-013]: ./reviews/rc-013-use-case-diagram.md
[RC-014]: ./reviews/rc-014-business-case-market-analysis.md
[RC-015]: ./reviews/rc-015-milestone-004.md
[RC-016]: ./reviews/rc-016-market-analysis-service.md
[RC-017]: ./reviews/rc-017-business-case-clean-architecture.md
[RC-018]: ./reviews/rc-018-milestone-005.md
[RC-019]: ./reviews/rc-019-adr-0001.md
[RC-020]: ./reviews/rc-020-clean-architecture-code.md
[RC-021]: ./reviews/rc-021-business-case-english-documents.md
[RC-022]: ./reviews/rc-022-milestone-006.md
[RC-023]: ./reviews/rc-023-legacy-documents-english.md
[RC-024]: ./reviews/rc-024-business-case-domain-model.md
[RC-025]: ./reviews/rc-025-milestone-007.md
[RC-026]: ./reviews/rc-026-stakeholder-analysis-hotel-staff.md
[RC-027]: ./reviews/rc-027-user-stories-hotel-staff.md
[RC-028]: ./reviews/rc-028-use-case-diagram-hotel-staff.md
[RC-029]: ./reviews/rc-029-dictionary.md
[RC-030]: ./reviews/rc-030-use-case-001.md
[RC-031]: ./reviews/rc-031-use-case-002.md
[RC-032]: ./reviews/rc-032-use-case-003.md
[RC-033]: ./reviews/rc-033-domain-model-uc001.md
[RC-034]: ./reviews/rc-034-domain-model-uc002.md
[RC-035]: ./reviews/rc-035-domain-model-uc003.md
[RC-036]: ./reviews/rc-036-domain-model.md
[RC-037]: ./reviews/rc-037-business-case-domain-folders.md
[RC-038]: ./reviews/rc-038-milestone-008.md
[RC-039]: ./reviews/rc-039-domain-rules-in-entities.md
[RC-040]: ./reviews/rc-040-business-case-validation-attributes.md
[RC-041]: ./reviews/rc-041-milestone-009.md
[RC-042]: ./reviews/rc-042-adr-0002.md
[DICT-001]: ../dictionary.md
[UC-001]: ../use-cases/uc-001/uc.md
[UC-002]: ../use-cases/uc-002/uc.md
[UC-003]: ../use-cases/uc-003/uc.md
[DM-001]: ../use-cases/uc-001/dm.md
[DM-002]: ../use-cases/uc-002/dm.md
[DM-003]: ../use-cases/uc-003/dm.md
[DM-004]: ../domain-model.md
[MIL-006]: ../milestones/mil-006-english-legacy-documents.md
[MIL-007]: ../milestones/mil-007-domain-model.md
[MIL-008]: ../milestones/mil-008-domain-rules-in-entities.md
[MIL-009]: ../milestones/mil-009-validation-attributes.md
[ADR-0002]: ../adr/adr-0002-validation-attributes-on-entities.md
[ADR-0001]: ../adr/adr-0001-clean-architecture-structure.md
[MIL-005]: ../milestones/mil-005-clean-architecture.md
[MIL-004]: ../milestones/mil-004-market-analysis-service.md
[UCD-001]: ../use-case-diagram.md
[US-001]: ../user-stories.md
[MIL-003]: ../milestones/mil-003-throwaway-proxmox.md
[MIL-002]: ../milestones/mil-002-nginx-proxy-and-web-compose.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
[3db0f51]: https://github.com/NF-Hotel/NFHotel/commit/3db0f51b7d7769bfc99dbbd173fc67266ee70c99
