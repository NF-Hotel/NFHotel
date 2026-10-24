# MIL-038 Configuration as code for the tailnet and the Proxmox node

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-038 |
| CrossReference | [BC-001], [ADR-0016], [RC-161] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-201 Go | [737399d] |

---

## Purpose

Decide whether NF Hotel may claim that the access rules of its tailnet and the settings of its Proxmox node are files in git, reviewed in pull requests and checked in CI, so that no access rule is changed by hand. Today the tailnet policy is edited in the Tailscale admin console and the node in the Proxmox web interface; the LAN rule that MIL-023 removed had grown there unseen.

## Deliverable

- An ADR that chooses how: the tailnet policy in git, tested and applied by the Tailscale GitOps action, compared with keeping the console, with the reasons.
- The tailnet policy as `ops/tailnet/policy.hujson`, with its tests, checked on every pull request and applied on a merge to main.
- The settings of the Proxmox node that matter for NF Hotel (the VM and LXC definitions, the firewall of the node if used) recorded in git and compared with the node by a script.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the choice and the alternatives, with the reasons | Yes | No |
| 2 | A pull request that changes the policy runs the policy tests in CI; a merge to main applies it, and the console shows the same policy | Yes | No |
| 3 | A script compares the VM and LXC definitions in git with the node and reports a difference | Yes | No |
| 4 | CI is green and the reviews are Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0016 | The operations live in `ops/` |
| RC-161 | Traceability between the documents and production |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 21 (security): every access rule is reviewed | Tasks 1 to 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-12. Needs a Tailscale OAuth client for the GitOps action, stored as a repository secret by S01.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for configuration as code | Write the ADR: the tailnet policy and the node settings in git, compared with the consoles. | No | RC-161 |
| 2 | Tailnet policy in git | Export the current policy into `ops/tailnet/policy.hujson`, add the GitOps workflow that tests it on a pull request and applies it on main. | No |  |
| 3 | Proxmox definitions in git | Record the definitions of VM 100 and LXC 102 (VM 101, sports-club, is not part of NF Hotel), and a script that compares them with the node. | No |  |
| 4 | Check and review | Prove the criteria; reviews. | No |  |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0016]: ../adr/adr-0016-one-repository-for-application-and-operations.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
[737399d]: https://github.com/NF-Hotel/NFHotel/commit/737399d58692d9cf086cedcd8641a2d5f352e7d7
