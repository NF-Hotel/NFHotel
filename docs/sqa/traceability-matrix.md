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
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (instructed by the user) | The traceability matrix covers BC, SA, PP, MIL-001 to MIL-005, US, UCD, ADR and RC-001 to RC-020 | pending |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003], [MIL-004], [MIL-005], [UCD-001], [US-001] | [RC-017] |
| [SA-001] | Stakeholder Analysis | en | it | [BC-001] | - | [RC-009] |
| [MIL-001] | Milestone | en | it | [BC-001] | - | [RC-003] |
| [MIL-002] | Milestone | en | it | [BC-001] | - | [RC-006] |
| [MIL-003] | Milestone | en | it | [BC-001] | - | [RC-010] |
| [MIL-004] | Milestone | en | it | [BC-001], [US-001] | - | [RC-015] |
| [MIL-005] | Milestone | en | it | [BC-001] | - | [RC-018] |
| [ADR-0001] | Architecture Decision Record | - | - | [BC-001] | [MIL-005] | [RC-019] |
| `src/` (Domain, Application, Infrastructure) | Source code | - | - | [ADR-0001], [MIL-005] | - | [RC-020] |
| [PP-001] | Project Plan | en | it | [BC-001], [SA-001] | [MIL-001], [MIL-002], [MIL-003] | - |
| [UCD-001] | Use Case Diagram | en | it | [SA-001], [BC-001] | [US-001] | [RC-013] |
| [US-001] | User Story | en | it | [UCD-001], [BC-001] | - | [RC-012] |
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
[ADR-0001]: ../adr/adr-0001-clean-architecture-structure.md
[MIL-005]: ../milestones/mil-005-clean-architecture.md
[MIL-004]: ../milestones/mil-004-market-analysis-service.md
[UCD-001]: ../use-case-diagram.md
[US-001]: ../user-stories.md
[MIL-003]: ../milestones/mil-003-throwaway-proxmox.md
[MIL-002]: ../milestones/mil-002-nginx-proxy-and-web-compose.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
