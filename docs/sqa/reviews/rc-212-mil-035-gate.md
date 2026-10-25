# RC-212 Review of the scope change and gate review of MIL-035

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-212 |
| CrossReference | [MIL-035], [RC-198], [RC-208], [ADR-0024], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-035] after the scope change that S01 decided in chat on 2026-10-25, and its criteria as they now read.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The scope change itself is S01's decision, not the stand-in's.

## The scope change

The change: the passport key is excluded from criterion 4, with the reason (a half-done re-encryption loses data) and a trigger (real passport numbers). The milestone records it under "Scope change of 2026-10-25", with the reason and the choice, and ADR-0024 states it in its decision.

| Check | Status | Notes |
| --- | --- | --- |
| The change is S01's, in chat | Pass | Quoted in the Version History of the milestone |
| A reason and a choice are written down | Pass | In the milestone and in ADR-0024 |
| The remaining risk and when to decide again are named | Pass | In the same places |
| Nothing already built is undone | Pass | Only criteria that were not met are narrowed or removed |

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR with the alternatives | Go | ADR-0024 (RC-208) |
| 2 | No secret in `Config.Env` in production | Go | `check-secret-env.sh` passes for PostgreSQL, the API and the web application after every deploy since `df162f1` (RC-208) |
| 3 | The containers read `/run/secrets`, and the site and the API work | Go | RC-208; the site and `/book` answer 200, and the API logged no error |
| 4 | Rotation of each secret except the passport key, in the wiki and drilled in CI (narrowed) | Go | The CI job rotates the database password, the API token, the signing key and the first Manager's password (10 passed); the limit of the passport key is accepted in ADR-0024 decision 7 |
| 5 | CI green and reviews Go | Go | #308 and #313 green; RC-205 Go |

## Overall Verdict

Go. MIL-035 is complete; this pull request closes #288.

## Action Items

None.

---

[MIL-035]: ../../milestones/mil-035-secrets-as-files.md
[RC-198]: rc-198-mil-035.md
[RC-208]: rc-208-mil-035-gate.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
