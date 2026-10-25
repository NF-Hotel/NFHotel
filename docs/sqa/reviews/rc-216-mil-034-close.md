# RC-216 Review of the scope change and gate review of MIL-034

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-216 |
| CrossReference | [MIL-034], [RC-197], [RC-207], [ADR-0023], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [0174f76] |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-034] after the scope change that S01 decided in chat on 2026-10-25, and the condition of RC-207.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The scope change itself is S01's decision.

## The scope change

Criterion 2 no longer requires the exception counter to exist before the first unhandled exception. The milestone records the reason and the choice under "Scope change of 2026-10-25", and ADR-0023 decision 4 states it.

| Check | Status | Notes |
| --- | --- | --- |
| The change is S01's, in chat | Pass | Quoted in the Version History of the milestone |
| A reason and a choice are written down | Pass | The counter cannot exist before its first measurement; forcing an exception in production is refused |
| What still proves the path | Pass | The meter is registered and tested; the request series on the same OTLP path are in production; `ApplicationExceptions` passes its rule test |

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR with the alternatives | Go | ADR-0023 (RC-207) |
| 2 | Request series for both services, and the exception counter once an exception happens (narrowed) | Go | The request series of `nfhotel-web` and `nfhotel-api` are in production (RC-207); on 2026-10-25 there was still no exception series, as expected with no exception |
| 3 | The objectives and their PromQL in the wiki | Go | RC-207 |
| 4 | The rule tests | Go | RC-207 |
| 5 | The dashboard | Go | RC-207 |
| 6 | CI green, a request in Prometheus within two minutes, the code review Go | Go | RC-207: 36 seconds; RC-203 Go |

## Overall Verdict

Go. The condition of RC-207 is resolved by the scope change. MIL-034 is complete; this pull request closes #283.

## Action Items

None.

---

[MIL-034]: ../../milestones/mil-034-application-observability.md
[RC-197]: rc-197-mil-034.md
[RC-207]: rc-207-mil-034-gate.md
[ADR-0023]: ../../adr/adr-0023-application-telemetry.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[0174f76]: https://github.com/NF-Hotel/NFHotel/commit/0174f76d093566e3bc40bb45ced66bcc2d932f74
