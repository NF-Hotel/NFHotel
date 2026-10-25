# RC-215 Review of the scope change and gate review of MIL-039

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-215 |
| CrossReference | [MIL-039], [RC-202], [RC-210], [ADR-0026], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [199bc05] |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-039] after the scope change that S01 decided in chat on 2026-10-25, and its criteria as they now read.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The scope change itself is S01's decision, not the stand-in's.

## The scope change

The change: criterion 1 asks for the capacity of the VM instead of the node, because the incidents decide the question whatever the node's capacity. The milestone records it under "Scope change of 2026-10-25", with the reason and the choice, and ADR-0026 states it in its decision.

| Check | Status | Notes |
| --- | --- | --- |
| The change is S01's, in chat | Pass | Quoted in the Version History of the milestone |
| A reason and a choice are written down | Pass | In the milestone and in ADR-0026 |
| The remaining risk and when to decide again are named | Pass | In the same places |
| Nothing already built is undone | Pass | Only criteria that were not met are narrowed or removed |

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR with the decision, the alternatives and the capacity of the production VM (narrowed) | Go | ADR-0026: three options, the incident table, and the measured capacity of `nf-hotel` |
| 2 | If staging is chosen | Not applicable | Staging was not chosen |
| 3 | If not: the remaining risk and the check that covers it | Go | ADR-0026 decision 3 (RC-210) |
| 4 | CI green and reviews Go | Go | #314 green; RC-210 Go |

## Overall Verdict

Go. MIL-039 is complete; this pull request closes #305.

## Action Items

None.

---

[MIL-039]: ../../milestones/mil-039-staging-environment.md
[RC-202]: rc-202-mil-039.md
[RC-210]: rc-210-mil-039-gate.md
[ADR-0026]: ../../adr/adr-0026-no-staging-environment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[199bc05]: https://github.com/NF-Hotel/NFHotel/commit/199bc05466d450b92157aa4daead871e7e6d76ce
