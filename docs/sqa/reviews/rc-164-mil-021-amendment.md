# RC-164 Review of the amendment of MIL-021 and of DSA-001 decision D7

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-164 |
| CrossReference | [MIL-021], [DSA-001], [ADR-0016], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - [MIL-021] criteria 6, 7, 8 and 10, and tasks 7 and 8;
  - [DSA-001] decision D7.
- **Checklist used:** [QC-MIL-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan in chat on 2026-10-21 and chose the legion laptop as the backup host.

## Changes

| Where | Was | Is | Why |
| --- | --- | --- | --- |
| MIL-021 criterion 10 | A checkout of `NF-Hotel/DevOps` (ADR-0012) | A checkout of `NF-Hotel/NFHotel` (ADR-0016) | ADR-0012 is superseded, and the VM was switched on 2026-10-21 |
| MIL-021 criteria 6 and 7 | "A forced failure"; "a test from the runner to another tailnet node" | Named means: a manual CD run with `rollback_drill`, and the step "Check the tailnet scope of this node" | Each criterion is now checked by a step that leaves a record in the run |
| MIL-021 criterion 8 | A dump restored | A dump **from the off-VM copy** restored | It proves the copy, not only the dump |
| MIL-021 tasks 7 and 8 | Off-VM copy, target open | The write-only target on the legion laptop; the drill and the scope | The decision of S01 |
| DSA-001 D7 | The Proxmox node, with a residual gap R7 | The legion laptop, outside the Proxmox host; the weekly `vzdump` stays | S01 in chat. It closes R7, at the cost that the laptop is off at times, which the catch-up covers |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable | Pass | Unchanged, with the target named |
| 2 | Explicit Go/No-Go criteria | Pass | Criteria 6, 7 and 8 now name the step that checks them |
| 3 | Dependencies mapped | Pass | Unchanged. ADR-0016 is added to CrossReference for criterion 10 |
| 4 | Traceable to the Business Case | Pass | Unchanged |
| 5 | Owner and approving reviewer | Pass | Unchanged |
| 6 | Target date | Pass | 2026-11-15 is kept |

## Overall Verdict

Go.

## Action Items

None.

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
