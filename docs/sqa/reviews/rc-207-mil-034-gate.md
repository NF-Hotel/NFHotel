# RC-207 Gate review of MIL-034

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-207 |
| CrossReference | [MIL-034], [RC-197], [RC-203], [ADR-0023], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the six Go/No-Go criteria of [MIL-034], after its pull request #306 was merged (`5bcda24`), deployed by CD (run 40131290647), and the monitoring LXC was deployed at that commit.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records OpenTelemetry over OTLP and the alternatives | Go | ADR-0023, with four options and the reasons |
| 2 | In production, request series for `nfhotel-web` and `nfhotel-api`, and an exception counter for each | Partly | The request series exist for both jobs; the p95 read 4.75 ms (API) and 7.7 ms (web). **No exception series exists:** OpenTelemetry exports a counter only after its first measurement, and production has had no unhandled exception since the deploy |
| 3 | The objectives and their SLIs are in the wiki page Monitoring, each with its PromQL | Go | "Service level objectives": latency, errors and exceptions, each with its recording rule or query |
| 4 | `promtool test rules` shows that each objective alert fires on a breach and stays quiet otherwise | Go | Six tests in `rules-test.yml`, green in CI; in production the four alerts are loaded, healthy and inactive |
| 5 | The dashboard shows the rate, the p95 and the error ratio per application | Go | Panels 7, 8 and 9 of "NF Hotel overview" |
| 6 | CI green; a request after CD is in Prometheus within two minutes; the code review is Go | Go | 32 checks green; five requests to `/admin/login` raised the counter from 49 to 55 within 36 seconds; RC-203 Go |

## Overall Verdict

Go-with-conditions. Criteria 1 and 3 to 6 are met. Criterion 2 is met for the request series, but the exception counter has not been seen, so `ApplicationExceptions` is proven by its rule test only. MIL-034 stays open (#283) until the condition is met.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Show `aspnetcore_diagnostics_exceptions_total` for both jobs: after the first unhandled exception in production, or on the throwaway stack with a request that throws, then record the result in a new gate review | AI | Next session |

---

[MIL-034]: ../../milestones/mil-034-application-observability.md
[RC-197]: rc-197-mil-034.md
[RC-203]: rc-203-mil-034-code.md
[ADR-0023]: ../../adr/adr-0023-application-telemetry.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
