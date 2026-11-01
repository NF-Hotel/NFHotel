# MIL-052 Read-only Grafana access for the AI assistant

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-052 |
| CrossReference | [BC-001], [ADR-0020], [MIL-051] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-11-01 that the AI assistant can look into Grafana, Prometheus and Loki from the chat, beside MIL-051 for the changes ("planlæg begge veje"); accepted after RC-255 Go | pending |

---

## Purpose

Decide whether the AI assistant that S01 uses in Claude Code may read the monitoring of NF Hotel (metrics, logs, alerts and dashboards) from the chat, so that a question such as "why did `SiteDown` fire last night?" is answered from the real data, and whether it can do that without being able to change anything.

Changes stay in git and reach the LXC through MIL-051; this gate gives read access only. Grafana already refuses changes made in its interface to the provisioned dashboards (`allowUiUpdates: false` in `grafana/provisioning/dashboards/dashboards.yml`), so a Viewer role fits that design.

A check from S01's PC on 2026-11-01 found that `nf-monitoring` answers on port 3000 (Grafana), 9090 (Prometheus) and 3100 (Loki). `docs/wiki/Monitoring.md` grants a person (`autogroup:admin`) only port 3000. Either the real tailnet policy is wider than the documented one, or the documentation is out of date. This gate settles that first.

## Deliverable

- **ADR-0036, AI read access to the monitoring.** It records:
  - which data the assistant may read: metrics, alerts and dashboards; and whether logs are included, after task 2 has checked that the logs carry no personal data of guests or staff (GDPR: data sent to the assistant leaves the server);
  - the access path: the Grafana MCP server (`mcp-grafana`, pinned version) on S01's PC, through Grafana only, with a Grafana service account in the **Viewer** role; not Prometheus or Loki directly, so that one token and one audit trail cover all reading;
  - where the token lives: only in S01's user-level Claude Code MCP settings on the PC, never in the repository, a chat message, a command line or a log; with an expiry date and a way to revoke it;
  - who may use it, and that the access is removed when the project ends.
- **The tailnet policy reconciled with the documentation**: a person reaches Grafana on port 3000 only, unless ADR-0036 records a reason for more; the policy in `docs/wiki/Monitoring.md` matches the real one.
- **The service account and the MCP server set up** by S01, following a new section of `docs/wiki/Monitoring.md`: create the Viewer service account, create a token with an expiry date, register the MCP server with the token in the environment, check, revoke.
- **A check that the access is read-only**: with the token, a request to create a dashboard, change an alert rule, add a data source or silence an alert is refused (HTTP 403), while a Prometheus query, a Loki query (if allowed) and the list of firing alerts succeed.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0036 is `Accepted` with a `Go` review, and states which data the assistant reads, why, and the GDPR reasoning for the logs | Yes | No |
| 2 | The logs the assistant may read were checked for personal data of guests and staff, and the result is recorded; if any is found, logs are left out or the data is removed at the source before access is granted | Yes | No |
| 3 | The service account has the Viewer role; creating a dashboard, changing an alert rule, adding a data source and creating a silence are each refused with HTTP 403 | Yes | No |
| 4 | From the chat, the assistant answers a question about a past alert from a Prometheus query and the alert history, and names the query it ran | Yes | No |
| 5 | The token has an expiry date, is stored only in S01's user-level MCP settings, and appears in no file of the repository, no commit and no chat message | Yes | No |
| 6 | The tailnet policy and `docs/wiki/Monitoring.md` agree, and a person reaches no port of `nf-monitoring` that the ADR does not record | Yes | No |
| 7 | Revoking the token in Grafana stops the access at once, shown by a refused request after the revocation | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0020 | The monitoring stack, Grafana and its data sources |
| MIL-051 | Changes reach the monitoring through git; this gate deliberately gives no write access |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25: observe the server; here the observations are read from the chat to find the cause of a fault | Tasks 2 to 6 |
| BC-001 objective 23: a private connection to the server; here the tailnet policy for the monitoring host | Task 1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-08. It changes no application code and no hosted service, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Reconcile the tailnet policy | Compare the real tailnet policy for `tag:monitoring` with `docs/wiki/Monitoring.md`, explain why S01's PC reaches ports 9090 and 3100, and narrow the policy or correct the page. | No | criterion 6 |
| 2 | Check the logs for personal data | Search the Loki streams of the last 14 days for names, e-mail addresses, phone numbers and booking details; record the result and whether logs may be read by the assistant. | No | criterion 2 |
| 3 | ADR-0036: AI read access to the monitoring | Decide the data in scope, the path through Grafana with a Viewer service account and `mcp-grafana`, where the token lives, its expiry and its revocation. | No | criteria 1, 5 |
| 4 | Set up the service account and the MCP server | S01 creates the Viewer service account and a token with an expiry date in Grafana and registers the pinned `mcp-grafana` in the user-level Claude Code settings; the AI never sees the token. | No | criteria 3, 5 |
| 5 | Prove read-only and revocation | Run the refused write requests and the allowed queries of criterion 3, a real question about a past alert (criterion 4), and a revocation test (criterion 7). | No | criteria 3, 4, 7 |
| 6 | Wiki and gate review | A section in `docs/wiki/Monitoring.md` on the AI read access: what it can see, how to set it up, how to revoke it; review record; gate review. | No | criteria 1 to 7 |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-051]: mil-051-monitoring-continuous-deployment.md
