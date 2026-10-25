# ADR-0023 Application metrics over OpenTelemetry, and errors as a metric instead of an error tracker

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0023 |
| CrossReference | [ADR-0020], [MIL-034] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-034, which S01 accepted in chat on 2026-10-24 | [f105724] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** S01 asked on 2026-10-24 for the gaps of the DevOps review to be closed. The first gap was that NF Hotel does not measure what its users experience. The options and the decision below were written by the AI assistant.

The monitoring of ADR-0020 measures the host and the containers, and probes three pages from outside. It does not see the requests inside the web application and the API: how many there are, how long they take, and how many fail. An unhandled exception is a line of text in Loki, and nothing counts it. ADR-0020 decision 9 deferred error tracking with Sentry or GlitchTip.

ASP.NET Core in .NET 10 already measures every request on its own meters: the duration with the route and the status code, the unhandled exceptions by type, and the Kestrel connections. .NET measures memory, garbage collection and threads. These values only need to leave the process.

Options considered:

1. **A `/metrics` endpoint in each application, scraped by Alloy** (prometheus-net, or the Prometheus exporter of OpenTelemetry). This is the classic pull model. But the exporter of OpenTelemetry for ASP.NET Core is still a pre-release, and an endpoint needs a route that the public site must not serve, or a second port.
2. **OpenTelemetry metrics pushed over OTLP to Alloy.** The applications send the meters every 30 seconds to Alloy on their internal network `nfhotel-api`, and Alloy forwards them to Prometheus with the other metrics of `nf-hotel`. It uses the stable OTLP exporter, opens no port in the applications, and the same pipeline could carry traces later.
3. **A self-hosted error tracker** (GlitchTip or Sentry), on the monitoring LXC. It groups exceptions with their stack traces. But it adds a database, a cache and a worker to the LXC, and an SDK in the code. It answers "which error", but not "how slow" or "how many requests failed".
4. **Traces as well as metrics.** Tempo with OpenTelemetry traces would show one slow request end to end. It is more than the objectives need now, and it adds a store to the LXC.

## Decision

We follow option 2, and we count errors as a metric instead of running an error tracker.

1. **The meters.** `Microsoft.AspNetCore.Hosting`, `Microsoft.AspNetCore.Diagnostics`, `Microsoft.AspNetCore.Server.Kestrel` and `System.Runtime`. The web application reports as `nfhotel-web` and the API as `nfhotel-api` (the label `job`).
2. **The path.** OTLP/HTTP from each application to `http://alloy:4318` on the internal network `nfhotel-api`. Alloy joins that network, adds `host="nf-hotel"`, and remote-writes to Prometheus. No port is published, and the network has no route out.
3. **Off by default.** The export runs only when `Telemetry:OtlpEndpoint` is set. Tests and local development send nothing. In production, compose sets it. Without Alloy, the export fails quietly, and the application is not affected.
4. **Errors.** An unhandled exception is counted by `aspnetcore_diagnostics_exceptions_total`, with its type, and alerted on through the error rate of the objectives. The stack trace stays in the container's log in Loki, which Grafana searches.
5. **Not now.** Traces (option 4) and an error tracker (option 3) are not built. Either can follow on the same OTLP path, if the objectives show a need that the metrics and the logs cannot answer.

## Consequences

**Positive:**

- The objectives of MIL-034 can be measured from what the guests and the staff do, not only from three probes.
- No new service on the LXC, no new port, and two stable packages in the code.

**Negative:**

- An exception is counted, but not grouped with others of the same cause. Finding the cause means reading the log in Grafana.
- A metric of a request that fails before ASP.NET Core handles it, such as a refused TLS handshake, is not seen. The probes of ADR-0020 cover that case.

## Affected Artifacts

- [ADR-0020]: decision 9 (error tracking deferred) is decided here, as option 3 rejected for now.
- [MIL-034]: tasks 2 to 4.

---

[ADR-0020]: ./adr-0020-monitoring-stack.md
[MIL-034]: ../milestones/mil-034-application-observability.md
[f105724]: https://github.com/NF-Hotel/NFHotel/commit/f10572471cf3c195934b767081a1e6166cbf7f0f
