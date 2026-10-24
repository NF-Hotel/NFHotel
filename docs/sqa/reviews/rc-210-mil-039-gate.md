# RC-210 Review of ADR-0026 and gate review of MIL-039

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-210 |
| CrossReference | [MIL-039], [RC-202], [ADR-0026], [ADR-0021], [QC-ADR-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** ADR-0026 against [QC-ADR-001], and the four Go/No-Go criteria of [MIL-039], with the section "Why there is no staging environment" of the wiki page CI-CD.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the decision and the alternatives, with the reasons and the capacity of the node | Partly | ADR-0026 weighs a staging VM, a staging stack on the VM and the throwaway stack, against the four production incidents of October. The capacity of the VM is measured (4 vCPU, 20 GiB with 19 GiB free, 111 GB with 75 GB free); **the free capacity of the node is not**, because reading it needs the root login of Proxmox |
| 2 | If staging is chosen: CD deploys to staging first | Not applicable | Staging was not chosen |
| 3 | If staging is not chosen: the ADR names the remaining risk and the check that covers it | Go | Decision 3: a release that starts and passes the smoke test but serves guests badly; covered by `ApplicationErrors`, `ApplicationSlow` and `ApplicationExceptions` within 5 to 10 minutes, and the rollback command |
| 4 | CI green and reviews Go | Go | The pull request's checks; this review of the ADR is Go |

## Checklist Results (ADR)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| ADR | Context with facts | Pass | The measured capacity and the incident table |
| ADR | Options with their costs | Pass | Three options, each with what it would have caught |
| ADR | Decision with reasons, and when to decide again | Pass | Decision 4 names three triggers |
| ADR | Consequences, negative ones included | Pass | Minutes of exposure before an alert; differences of the runner found only by the smoke test |

## Overall Verdict

Go-with-conditions. The decision and its remaining risk are recorded and the checks it relies on are in place. Criterion 1 lacks the free capacity of the node, which only S03 can read; MIL-039 stays open (#305) until it is added to ADR-0026.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Read the free CPU, memory and disk of the node (`pvesh get /nodes/<node>/status`) and add them to the context of ADR-0026 | S03 | When S03 is available |

---

[MIL-039]: ../../milestones/mil-039-staging-environment.md
[RC-202]: rc-202-mil-039.md
[ADR-0026]: ../../adr/adr-0026-no-staging-environment.md
[ADR-0021]: ../../adr/adr-0021-automatic-deploy-when-green.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
