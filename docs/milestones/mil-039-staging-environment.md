# MIL-039 A staging environment before production

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-039 |
| CrossReference | [BC-001], [ADR-0021], [MIL-027] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-202 Go | [737399d] |

---

## Purpose

Decide whether NF Hotel deploys a release to a staging environment that is like production before production, or keeps the throwaway stack of CI as the only test before production, and implement the choice. Today a release that passes every check of CI goes straight to production, guarded by the smoke test and the automatic rollback of ADR-0021.

## Deliverable

- An ADR that decides between a persistent staging VM, a staging stack on the production VM, and the throwaway stack of CI only, weighed by the capacity of the node, the cost, and what each would have caught, with the reasons.
- The chosen option implemented: for a staging environment, CD deploys there first and goes on to production only when its checks pass.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the decision and the alternatives, with the reasons and the capacity of the node | Yes | No |
| 2 | If staging is chosen: CD deploys each release to staging, runs the smoke and end-to-end tests there, and deploys to production only when they pass; a release that fails in staging does not reach production | Yes | No |
| 3 | If staging is not chosen: the ADR names the risk that remains and the check that covers it | Yes | No |
| 4 | CI is green and the reviews are Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0021 | The automatic deploy and its gate |
| MIL-027 | The end-to-end tests that run against an environment |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 26 (operations): a release is tested where it will run | Tasks 1 to 2 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-19.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for staging | Measure the free capacity of the node, weigh the options, and write the ADR. | No | ADR-0021 |
| 2 | Implement the decision | Build what the ADR chooses, with its tests, or record the remaining risk. | No |  |
| 3 | Check and review | Prove the criteria; reviews. | No |  |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[737399d]: https://github.com/NF-Hotel/NFHotel/commit/737399d58692d9cf086cedcd8641a2d5f352e7d7
