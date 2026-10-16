# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 |
| CrossReference | [BC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Proposed | Claude (AI) | AI as stand-in for S04 | Initial version | [b690b57] |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003] | [RC-008] |
| [SA-001] | Stakeholder Analysis | en | it | [BC-001] | - | [RC-009] |
| [MIL-001] | Milestone | en | it | [BC-001] | - | [RC-003] |
| [MIL-002] | Milestone | en | it | [BC-001] | - | [RC-006] |
| [MIL-003] | Milestone | en | it | [BC-001] | - | [RC-010] |
| [PP-001] | Project Plan | en | it | [BC-001], [SA-001] | [MIL-001], [MIL-002], [MIL-003] | - |
| `tests/NFHotel.Proxmox.Tests`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-001] | - | [RC-004] |
| `deploy/`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-002] | - | [RC-007] |
| `deploy/proxmox`, `scripts/pve-token.sh`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-003] | - | [RC-011] |

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
[MIL-003]: ../milestones/mil-003-throwaway-proxmox.md
[MIL-002]: ../milestones/mil-002-nginx-proxy-and-web-compose.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
