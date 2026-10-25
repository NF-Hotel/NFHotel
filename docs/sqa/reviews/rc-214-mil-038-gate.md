# RC-214 Review of the scope change and gate review of MIL-038

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-214 |
| CrossReference | [MIL-038], [RC-201], [RC-211], [ADR-0027], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-038] after the scope change that S01 decided in chat on 2026-10-25, and its criteria as they now read.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The scope change itself is S01's decision, not the stand-in's.

## The scope change

The change: criteria 2 and 3 are deferred, with the reason (one administrator, machines that do not change, upkeep larger than the drift found) and the triggers that bring them back. The milestone records it under "Scope change of 2026-10-25", with the reason and the choice, and ADR-0027 states it in its decision.

| Check | Status | Notes |
| --- | --- | --- |
| The change is S01's, in chat | Pass | Quoted in the Version History of the milestone |
| A reason and a choice are written down | Pass | In the milestone and in ADR-0027 |
| The remaining risk and when to decide again are named | Pass | In the same places |
| Nothing already built is undone | Pass | Only criteria that were not met are narrowed or removed |

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR with the alternatives | Go | ADR-0027 (RC-211) |
| 2 | The tailnet policy tested and applied from git (deferred) | Not applicable | Deferred until a second administrator, a policy incident, or more machines of NF Hotel |
| 3 | The Proxmox definitions compared with the node (deferred) | Not applicable | Deferred with the same triggers |
| 4 | CI green and reviews Go | Go | #315 green; RC-211 Go |

## Overall Verdict

Go. MIL-038 is complete; this pull request closes #300 and #301 (not planned), #302.

## Action Items

None.

---

[MIL-038]: ../../milestones/mil-038-configuration-as-code.md
[RC-201]: rc-201-mil-038.md
[RC-211]: rc-211-adr-0027.md
[ADR-0027]: ../../adr/adr-0027-configuration-as-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
