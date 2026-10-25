# RC-213 Review of the scope change and gate review of MIL-037

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-213 |
| CrossReference | [MIL-037], [RC-200], [RC-209], [ADR-0025], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [199bc05] |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-037] after the scope change that S01 decided in chat on 2026-10-25, and its criteria as they now read.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The scope change itself is S01's decision, not the stand-in's.

## The scope change

The change: the rebuild drill is removed, with the reason (half a day of S03 at the node, while each step is proven) and a trigger (going live, a new node). The milestone records it under "Scope change of 2026-10-25", with the reason and the choice, and ADR-0025 states it in its decision.

| Check | Status | Notes |
| --- | --- | --- |
| The change is S01's, in chat | Pass | Quoted in the Version History of the milestone |
| A reason and a choice are written down | Pass | In the milestone and in ADR-0025 |
| The remaining risk and when to decide again are named | Pass | In the same places |
| Nothing already built is undone | Pass | Only criteria that were not met are narrowed or removed |

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR with the RTO, the RPO and the accepted single points of failure | Go | ADR-0025 (RC-209) |
| 2 | The runbook is in the wiki and each step names its command | Go | The wiki page Disaster-Recovery: ten steps for the VM, four for the LXC |
| 3 | The rebuild drill (removed) | Not applicable | Postponed until the hotel goes live or the node is replaced; each step is proven on its own (RC-505, RC-166, the CI test, the daily CD) |
| 4 | The settings and secrets are backed up encrypted off the host, and a restore was tested | Go | On `nf-hotel`, `nfhotel-settings-daily-*.tar.age` files of 41 KB, 0600, written by the backups before the deploys of 2026-10-24; the copy off the VM succeeded after them (`offvm_copy`). The restore is tested by `test-backup-settings.sh`: decrypted, listed and unpacked in place |
| 5 | Reviews Go | Go | RC-209 Go |

## Overall Verdict

Go. MIL-037 is complete; this pull request closes #297 (not planned), #298.

## Action Items

None.

---

[MIL-037]: ../../milestones/mil-037-disaster-recovery.md
[RC-200]: rc-200-mil-037.md
[RC-209]: rc-209-mil-037-code.md
[ADR-0025]: ../../adr/adr-0025-recovery-objectives.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[199bc05]: https://github.com/NF-Hotel/NFHotel/commit/199bc05466d450b92157aa4daead871e7e6d76ce
