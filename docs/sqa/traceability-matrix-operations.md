# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 (NF-Hotel/DevOps) |
| CrossReference | [BC-001], [ADR-0012] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the documents moved from NFHotel (MIGRATION.md) | [c1dc5f7] |

---

## Purpose

Tracks the backward and forward links of the artifact instances in this repository. The project-level matrix, with the Business Case and the application documents, is TM-001 of NF-Hotel/NFHotel. A row is added or updated whenever an instance here is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [DSA-001] | CI/CD and DevSecOps Assessment (no catalog type) | en | it | [BC-001] | [ADR-0007], [ADR-0008], [ADR-0009], [ADR-0010], [ADR-0011], [MIL-021], [MIL-022], [MIL-023], [MIL-024] | [RC-500] |
| [MIL-021] | Milestone | en | it | [BC-001], [DSA-001], [ADR-0007], [ADR-0008], [ADR-0010] | [MIL-022] | [RC-089], [RC-094], [RC-095], [RC-097], [RC-500] |
| [MIL-022] | Milestone | en | it | [BC-001], [MIL-021] | [MIL-023] | [RC-090] |
| [MIL-023] | Milestone | en | it | [BC-001], [MIL-022] | - | [RC-091] |
| [MIL-024] | Milestone | en | it | [BC-001], [ADR-0010], [ADR-0011], [MIL-021] | - | [RC-099], [RC-101] |
| [ADR-0007] | ADR | en | it | [BC-001], [DSA-001] | [MIL-021], [MIL-022] | [RC-092] |
| [ADR-0008] | ADR | en | it | [BC-001], [DSA-001] | [MIL-021], [MIL-022] | [RC-093] |
| [ADR-0009] | ADR | en | it | [DSA-001] | [MIL-021], [MIL-023] | [RC-096] |
| [ADR-0010] | ADR | en | it | [DSA-001], [ADR-0012] | [MIL-021], [ADR-0011] | [RC-098], [RC-500] |
| [ADR-0011] | ADR | en | it | [ADR-0010], [DSA-001] | [MIL-024] | [RC-100] |
| [ADR-0500] | ADR | en | it | [ADR-0007], [DSA-001] | [MIL-021] | [RC-502] |
| Actions, scripts, compose files, CI | Source code (SH, YML, WF) | - | - | [ADR-0007], [ADR-0011], [ADR-0012] | - | [RC-094], [RC-095], [RC-101], [RC-501] |

---

[BC-001]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/business-case.md
[ADR-0012]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0012-devops-repository-ownership.md
[DSA-001]: ./devsecops-assessment.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[MIL-024]: ../milestones/mil-024-postgresql-api-readiness.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0009]: ../adr/adr-0009-tls-certificates.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[RC-089]: ./reviews/rc-089-milestone-021.md
[RC-090]: ./reviews/rc-090-milestone-022.md
[RC-091]: ./reviews/rc-091-milestone-023.md
[RC-092]: ./reviews/rc-092-adr-0007.md
[RC-093]: ./reviews/rc-093-adr-0008.md
[RC-094]: ./reviews/rc-094-mil-021-shell-code.md
[RC-095]: ./reviews/rc-095-mil-021-workflows.md
[RC-096]: ./reviews/rc-096-adr-0009.md
[RC-097]: ./reviews/rc-097-milestone-021-reconciliation.md
[RC-098]: ./reviews/rc-098-adr-0010.md
[RC-099]: ./reviews/rc-099-milestone-024.md
[RC-100]: ./reviews/rc-100-adr-0011.md
[RC-101]: ./reviews/rc-101-mil-024-code-and-gate.md
[RC-500]: ./reviews/rc-500-migration-documents.md
[RC-501]: ./reviews/rc-501-migration-code.md
[RC-502]: ./reviews/rc-502-adr-0500-frozen-image.md
[ADR-0500]: ../adr/adr-0500-freeze-market-analysis-image.md
[c1dc5f7]: https://github.com/NF-Hotel/DevOps/commit/c1dc5f74e11ce4e10f7f1dbdf5ef304e4db8429e
