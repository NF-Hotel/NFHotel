# RC-203 Review of the code of MIL-034 (application telemetry and service level objectives)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-203 |
| CrossReference | [MIL-034], [ADR-0023], [ADR-0020], [QC-CS-001], [QC-SH-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 4 of MIL-034 (#279 to #282), pull request #306:
  - `src/NFHotel.External/Telemetry/TelemetryRegistration.cs` (new), the two OpenTelemetry package references, and `Program.cs` of the web application and the API;
  - `tests/NFHotel.External.Tests/Telemetry/TelemetryRegistrationTests.cs` (new);
  - the compose files of the web application, the API and Alloy, and `ops/deploy/alloy/config.alloy`;
  - `ops/deploy/monitoring/prometheus/rules/applications.yml` (new), `rules-test.yml` and the dashboard `nfhotel-overview.json`;
  - `ops/tests/test-alloy-start.sh`, ADR-0023 and the wiki page Monitoring.
- **Checklists used:** [QC-CS-001], [QC-SH-001] and [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| External tests | 105 passed locally, among them the four new tests: with an address, without one, an invalid one, and the meter list |
| Rule tests | `promtool test rules` passes the six new tests (slow, errors, few requests, exceptions, missing metrics, a recording rule) |
| Alloy start test | Alloy starts on the new network and answers 200 to an OTLP metric pushed to `/v1/metrics` |
| CI of the pull request | 32 checks green, among them the build with warnings as errors, the test rules, the end-to-end tests, the DAST baseline, the monitoring checks, CodeQL and SonarCloud |
| Linters | `ops/tests/lint.sh` clean (actionlint, shellcheck, shfmt, yamllint) |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS 1, 2 | Names, one type per file | Pass | `TelemetryRegistration`, `AddServiceTelemetry`, `OtlpEndpointSetting` |
| CS 3 | Formatting, analyzers | Pass | `dotnet format` clean; no warning with warnings as errors |
| CS 7 | Arguments validated at public boundaries | Pass | `ArgumentNullException.ThrowIfNull` and `ArgumentException.ThrowIfNullOrWhiteSpace`; an address that is not absolute http or https throws at start |
| CS 8 | XML documentation | Pass | The public method and constants, with why the export is off without an address |
| CS 9, 14 | Knowledge in one place | Pass | The setting name, the interval and the meter list are constants of `TelemetryRegistration`, read by the tests |
| CS 12 | Tests cover the new behaviour, named in three parts | Pass | Positive, negative (invalid address), edge case (no address) and equivalence (the meters) |
| CS 15, 17 | Layers | Pass | Telemetry is an outer concern, in `NFHotel.External`; the domain and application layers do not know it |
| CS 16 | Single responsibility | Pass | One extension method that only registers the exporter |
| SH | `test-alloy-start.sh` | Pass | Its own network, removed by the trap; the linters are clean |
| YML | Rules and compose | Pass | Each alert has a `for`, a severity, a summary with the value and a runbook row; yamllint clean |
| Privacy | What is exported | Pass | Only metric names and the route template; no query string, user or body (the log policy of the wiki page Incidents) |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run and the deploy of the monitoring, read the request series of both services in Prometheus and close MIL-034 with its gate review | AI | After the merge |

---

[MIL-034]: ../../milestones/mil-034-application-observability.md
[ADR-0023]: ../../adr/adr-0023-application-telemetry.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
