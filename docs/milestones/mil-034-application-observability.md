# MIL-034 Application observability and service level objectives

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-034 |
| CrossReference | [BC-001], [ADR-0020], [MIL-023] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-197 Go | pending |

---

## Purpose

Decide whether NF Hotel may claim that it measures what a guest and a member of staff experience, the speed and the errors of the web application and the API, against stated objectives, and alerts when an objective is at risk. The monitoring of MIL-023 measures the host and the containers, and probes three pages; it does not see the requests inside the applications, and an unhandled exception is only a line of text in Loki. Error tracking with Sentry or GlitchTip was deferred in ADR-0020 decision 9.

## Deliverable

- An ADR that chooses how the applications report: OpenTelemetry metrics pushed over OTLP to Alloy, compared with a scraped `/metrics` endpoint and with a self-hosted error tracker, with the reasons.
- The web application and the API send the request rate, duration and status (`http.server.request.duration`) and the count of unhandled exceptions to Alloy, which forwards them to Prometheus. No port is opened: the applications push to Alloy on their internal network.
- Service level objectives written down, with their SLIs: availability of the site, latency of the pages, error rate of the API.
- Alert rules on the objectives (error rate, latency, exceptions), tested with `promtool test rules`, and Grafana panels for each.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the choice of OpenTelemetry over OTLP and the alternatives, with the reasons | Yes | No |
| 2 | In production, Prometheus has `http_server_request_duration_seconds` series for `nfhotel-web` and `nfhotel-api`, and an exception counter for each | Yes | No |
| 3 | The service level objectives and their SLIs are in the wiki page Monitoring, each with its PromQL | Yes | No |
| 4 | `promtool test rules` shows that each objective alert fires on a breach and stays quiet otherwise | Yes | No |
| 5 | The dashboard shows the rate, the 95th percentile of the duration and the error ratio per application | Yes | No |
| 6 | CI is green, a request to the site after CD appears in Prometheus within two minutes, and the code review against `qc-programming-csharp` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-023 | The monitoring stack and Alloy that receive the telemetry |
| ADR-0020 | Decision 9 deferred error tracking; this milestone decides it |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 26 (operations): problems are seen before a guest reports them | Tasks 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-14. The change reaches the VM through CD; the monitoring LXC is updated with `deploy-monitoring.sh`.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for application telemetry | Write the ADR: OpenTelemetry metrics over OTLP to Alloy, compared with a scraped endpoint and a self-hosted error tracker. | No | ADR-0020 |
| 2 | Telemetry in the applications | Add OpenTelemetry to `NFHotel.Web` and `NFHotel.Api`: the ASP.NET Core and runtime meters and the exception counter, exported over OTLP when an endpoint is configured; no export in tests. | No | criterion 2 |
| 3 | Alloy receives OTLP | Alloy joins the internal network of the applications, receives OTLP and forwards the metrics to Prometheus; the start test checks it. | No | criterion 2 |
| 4 | Objectives and alerts | Write the SLOs and SLIs, add the alert rules with promtool tests, and add the panels to the dashboard. | No | criteria 3 to 5 |
| 5 | Check and review | Prove the criteria in production; code review against `qc-programming-csharp`. | No | criterion 6 |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-023]: ./mil-023-operational-quality.md
