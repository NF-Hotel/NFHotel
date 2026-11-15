# ADR-0045 Read-only access to the monitoring for the AI assistant

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0045 |
| CrossReference | [MIL-052], [ADR-0020], [ADR-0044], [MIL-051], [RC-384] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: task 3 of MIL-052, with the check of the logs of task 2; planned with the plan of 2026-11-14 and accepted at S01's word in chat ("Du godkender for mig"); written by the AI | [76bff8d] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-052] asks whether the artificial intelligence (AI) assistant that S01 uses in Claude Code may
read the monitoring of NF Hotel from the chat, and whether it can do that without being able to change anything. S01
gave the acceptance of the plan of 2026-11-14 to the AI assistant in chat ("Du godkender for mig"), and on 2026-11-15
asked that the open milestones be finished without a decision of theirs ("uden at jeg skal ta stilling til noget").
This record is task 3 of [MIL-052]; it takes the result of task 2, the check of the logs, as its input.

**The problem.** Today the assistant reads the monitoring over Tailscale Secure Shell (SSH) as root on
`nf-monitoring`, the Linux container (LXC) of the monitoring, with `docker exec` into the Prometheus container
([RC-384]). That works, but every read is a root
shell on the host: the same session could change or stop the stack, and nothing keeps reading apart from changing.
Since [ADR-0044] a change of the monitoring reaches the host only through a merge to `main`, so the assistant needs no
write path at all. It needs to read metrics, alerts and dashboards, and perhaps logs, to answer a question such as
"why did `SiteDown` fire last night?" from the real data.

**The options.**
1. *Keep the root shell.* Nothing new to run. But root is far more than reading: a wrong command, or a command the
   assistant is talked into, runs with every right on the host, and there is no identity to take away but the SSH
   access itself.
2. *Prometheus and Loki directly over the tailnet.* Both answer on the tailnet address of the LXC (9090 and 3100)
   with no login ([ADR-0020]): Prometheus also takes remote writes, and Loki runs with `auth_enabled: false`. A reader
   there is also a writer, there are two endpoints and no token to revoke, and A3 of the wiki page Monitoring gives a
   person no grant to those ports.
3. *Grafana, with a service account in the Viewer role, through the Grafana MCP server.* `mcp-grafana`, the Model
   Context Protocol (MCP) server that Grafana Labs publishes, runs on S01's PC and talks to Grafana on port 3000, which
   A3 already grants to the admins. Grafana checks the role on every request: a Viewer can query the data sources and
   read the dashboards, and cannot create or change a dashboard, a data source, an alert rule or a silence. The
   dashboards are provisioned from git with `allowUiUpdates: false`, so not even an Editor could change them for good.
   One token covers all reading, and deleting it in Grafana stops the access at once.
4. *A hosted service, such as Grafana Cloud with its remote MCP server.* The data would be copied to a third party,
   and NF Hotel has no such account. Left out.
5. *Grafana as in option 3, with the service account in an organization of its own.* Grafana keeps data sources per
   organization, so the token of an organization that holds only the Prometheus data source and copies of the
   dashboards could never reach Loki, in any client. But the file provisioning of the stack does not create an
   organization, so S01 would create it by hand, the data source and the dashboards would be provisioned twice, and
   that change of the monitoring stack is not part of [MIL-052]. It is the next step if the limit has to hold in
   every client, not only in the MCP server.

**Forces.**
- What the assistant reads leaves the server: it goes with the chat to the provider of the AI model. The General Data
  Protection Regulation (GDPR) asks that personal data of guests and staff is not sent there without a need. Metrics
  and alert names hold none; logs may.
- Least privilege: read only, one identity, an expiry, and a revocation that works at once.
- A token is a secret. It may not be in git, in a chat message, on a command line or in a log ([MIL-052] criterion
  5), and the assistant must never see it.
- The tailnet: the live filter of `nf-monitoring` grants more than A3 records (F5 of [RC-384]). That decision belongs
  to S01 and S04, not to this record.

**The check of the logs (task 2).** On 2026-11-15 the assistant searched every Loki stream of the last 14 days, the
whole retention, from the LXC itself, and printed only counts, label sets, e-mail domains, the field of each match and
masked samples: 2,396,155 lines from 12 sources, eleven containers and the systemd journal of `nf-hotel`, the virtual
machine (VM) that runs NF Hotel. It found:
- *Guests:* no personal data of a guest in a value. No e-mail address in the log of a container, except the contact
  addresses of crawlers in their user agents (72 lines in the log of nginx, at two domains); none in a web address
  (no `%40`, no `email=` parameter, and no query string in the 206,111 lines of the web container with a web
  address); no booking number, and no field of a postal address. The application programming interface (API) of NF
  Hotel logs its database queries (SQL) through Entity Framework Core with every parameter value hidden as `'?'`:
  3,646 lines with a list of parameters, none with a value. Its lines that name a guest's name, phone number or passport number name the columns of those
  queries.
- *Visitors:* the access log of nginx holds the Internet Protocol (IP) address of every visitor, which nginx takes
  from `CF-Connecting-IP`, with the user agent. An IP address is personal data under the GDPR. Its 290 lines with
  `phone` or `Mobile` are user agents and paths of `/mobile-api/`.
- *Staff:* 376 lines of `tailscaled.service` in the journal hold the Tailscale login of the person who opened an SSH
  session, an e-mail address, with the full command line of the session.
- The other e-mail-like text is names of systemd units, such as `user@1000.service`, and three mailing-list
  addresses in kernel messages.

So the logs hold personal data of visitors and of staff, though none of guests in a value.

## Decision

The AI assistant reads the monitoring of NF Hotel only through Grafana, with `mcp-grafana` v2.1.1 on S01's PC limited
to its tools for metrics, alerts and dashboards, and the token of a Grafana service account in the Viewer role: it
reads no logs.

1. **Data.** Metrics (the Prometheus data source); alerts, which this stack raises with Prometheus rules and
   Alertmanager rather than with Grafana alerting, so the rules and the series `ALERTS` and `ALERTS_FOR_STATE` of
   Prometheus; and the dashboards. Logs are left out: the MCP server registers no tool that reads them (point 5),
   because the logs hold personal data of staff and of visitors (the check of the logs above). Logs come in only
   after the source removes that data, which is not part of [MIL-052].
2. **Path.** The MCP server on S01's PC reaches Grafana on port 3000 of `nf-monitoring` over the tailnet, through the
   grant to `autogroup:admin` of A3. Not Prometheus or Loki directly, and no new grant.
3. **Identity.** A service account `ai-assistant-read` in the Viewer role, in no team, and with no permission of its
   own on a folder or a data source.
4. **Token.** One token with an expiry of 30 days, created by S01 in Grafana. It lives only in S01's user-level Claude
   Code MCP settings on the PC (`~/.claude.json`), in the `env` of the server entry, entered with a text editor: never
   on a command line, in a chat message, in the repository or in a log. The assistant never sees it. A new token
   replaces it when it expires.
5. **The server.** The release binary of `mcp-grafana` v2.1.1 (2026-10-20) for Windows, checked against the checksums
   of the release, run over standard input and output (stdio) with three flags:
   - `--enabled-tools=prometheus,alerting,dashboard,search`, an allowlist: only those groups of tools are registered.
     Every other group is not, among them the Loki tools; `grafana_api_request`, which calls any API of Grafana, the
     proxy of the Loki data source included, so that its own documentation calls it a full bypass of a limit on Loki;
     `run_panel_query` and `get_panel_image`, which would show the panel of logs on the overview dashboard; and any
     group that a later release adds.
   - `--disable-write`: no tool that creates or changes anything is registered, on top of the Viewer role.
   - `--usage-stats=disabled`: the server sends no usage report to Grafana Labs. The report holds no query and no
     log line, but it is data that does not need to leave.

   A newer release is taken only once it is at least 14 days old, with the same check of its checksums and of these
   flags against its documentation.
6. **Who, and for how long.** S01 only, on S01's PC. Deleting the token, or the service account, stops the access at
   once; both are deleted when the project ends, or sooner if S01 stops using the access.
7. **The tailnet.** A person reaches `nf-monitoring` on port 3000 and by SSH in check mode, as A3 records, and on no
   other port. The live grant that reaches further (F5 of [RC-384]) is narrowed by S01, or kept under the risk
   exception that S04 approves; criterion 6 of [MIL-052] waits for that.

## Consequences

**Positive:**
- The assistant answers questions about alerts and metrics from the real data, and can name the query it ran.
- Reading and changing are apart: the Viewer role refuses every write, and the configuration stays in git
  ([ADR-0044]).
- One identity with an expiry, which S01 can revoke at once; no root shell is needed to read a metric.
- No personal data of guests or staff leaves the server through this path, because the logs are out.

**Negative:**
- Metrics, alert names and host names still leave the server with the chat. They hold no personal data, but they
  describe the system.
- In the open-source edition of Grafana, which NF Hotel runs, a Viewer can query every data source, so leaving the
  logs out rests on the configuration of the MCP server, not on Grafana: the token, used in any other client, could
  read Loki (option 5 would close that). That is one more reason it is kept only in the MCP settings. Within the
  server, the Prometheus tools take any data source; Loki itself answers 404 to the query paths of Prometheus
  (checked on 2026-11-15), and task 5 shows through the server that a Prometheus query against the Loki data
  source returns no log line.
- The token is a secret on S01's PC for up to 30 days, and it has to be renewed by hand.
- Grafana keeps the last use of a token, not a log of each request, so the trail is thin.
- Through this path the assistant cannot read a log line; for that it still needs the root shell, on S01's word.

**Quality attributes.** The decision serves security: confidentiality (no log line, so no personal data, leaves with
the chat), integrity (the token has no write right, and the configuration stays in git) and accountability (one
identity for the assistant, which S01 can revoke). It costs a little maintainability: a token to renew every 30 days,
and a pinned release of the MCP server to keep up to date.

## Affected Artifacts

[MIL-052], [ADR-0020], [ADR-0044], [MIL-051], [RC-384]

---

[MIL-052]: ../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[MIL-051]: ../milestones/mil-051-monitoring-continuous-deployment.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[ADR-0044]: ./adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[RC-384]: ../sqa/reviews/rc-384-mil-051-gate.md
[76bff8d]: https://github.com/NF-Hotel/NFHotel/commit/76bff8de1dc7acaa5eab0c31eef2acd7358de036
