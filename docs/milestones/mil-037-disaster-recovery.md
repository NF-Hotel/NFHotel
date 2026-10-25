# MIL-037 Disaster recovery of the whole host

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-037 |
| CrossReference | [BC-001], [ADR-0008], [MIL-021], [MIL-023] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Deprecated | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-200 Go | [737399d] |
| 2026-10-25 | Accepted | mikemike | S01 | Criterion 3 (the rebuild drill) removed; S01 decided in chat on 2026-10-25 ("Ja og begrunde med et acceptabelt hvorfor og valg"); accepted after RC-213 Go | pending |

---

## Purpose

Decide whether NF Hotel may claim that it can bring the whole service back after the loss of its VM, its monitoring or its Proxmox node, within a stated recovery time and with a stated loss of data, proven by a drill. Today a restore of the database is drilled, but not a rebuild of the VM from nothing, and the configuration of the monitoring LXC and of the node is not backed up. The node stands in one home, so power, network and hardware are one point of failure.

## Deliverable

- An ADR that states the recovery time objective (RTO) and the recovery point objective (RPO), and what is accepted as a single point of failure, with the reasons.
- A runbook that rebuilds `nf-hotel` on a new VM from the scripts and the off-VM backup, and the monitoring LXC from its checkout and its secrets.
- A drill: a throwaway VM is built from the runbook, the latest off-VM backup is restored, and the site answers; the times are recorded.
- A backup of what is not yet backed up: the settings and secrets of the VM and of the LXC, encrypted, off the host.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the RTO, the RPO and the accepted single points of failure, with the reasons | Yes | No |
| 2 | The runbook is in the wiki and each step names its command | Yes | No |
| 3 | A drill rebuilt the service on a throwaway VM within the RTO, from a backup within the RPO, recorded in an RC | Yes | No |
| 4 | The settings and secrets of the VM and the LXC are backed up encrypted off the host, and a restore of them was tested | Yes | No |
| 5 | The reviews are Go | Yes | No |

## Scope change of 2026-10-25

**Criterion 3** (a drill rebuilt the service on a throwaway VM within the RTO) is removed. Task 4 (#297) is not done.

- **Why.** The drill needs S03 at the Proxmox node for half a day, because creating a VM needs its root login. Each step of the runbook is already proven on its own: the VM is built by `create-docker-vm.sh`, as `nf-hotel` was; the restore of the database is drilled (RC-505 from the VM, RC-166 from the laptop); the settings archive is decrypted and unpacked by a test in CI; and the services are deployed by CD several times a day. What stays unproven is the whole sequence in one go, and its time against the RTO.
- **Choice.** That is accepted for a hotel without real guests. The drill stays described in the wiki page Disaster-Recovery and is run when the hotel goes live or when the node is replaced.

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0008 | The backup and restore of the database |
| MIL-021 | The off-VM backup target |
| MIL-023 | The restore drills and the monitoring |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 26 (operations): the hotel is back within a known time after a loss | Tasks 1 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-05. The drill needs a throwaway VM on the Proxmox node.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for RTO and RPO | Write the ADR: the objectives, the single points of failure that are accepted, and why. | No | ADR-0008 |
| 2 | Rebuild runbook | Write the runbook for the VM and the LXC from the scripts and the backups. | No |  |
| 3 | Backup of settings and secrets | Back up the settings and secrets of the VM and the LXC, encrypted with age, to the off-VM target; test a restore. | No | MIL-021 |
| 4 | Rebuild drill | Build a throwaway VM from the runbook, restore the latest backup, check the site, record the times, remove the VM. | No |  |
| 5 | Check and review | Prove the criteria; reviews. | No |  |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-023]: ./mil-023-operational-quality.md
[737399d]: https://github.com/NF-Hotel/NFHotel/commit/737399d58692d9cf086cedcd8641a2d5f352e7d7
