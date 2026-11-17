# RC-399 Gate review of MIL-052 Read-only Grafana access for the AI assistant

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-399 |
| CrossReference | [MIL-052], [ADR-0045], [RC-255], [RC-357], [RC-385], [RC-398], [RC-384], [MIL-051], [PRC-001], [QC-MIL-001], [QC-WIKI-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-17 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-052 after S01 set up the service account and the MCP server, with the tools of the server, a past alert, the search for the token and the account disabled and enabled again; No-Go for now, criterion 6 fails on the live tailnet filter, as F5 of RC-384; mode autonomous, not independent | [7fdb18c] |

---

## Artifact Under Review

- Instance reviewed: [MIL-052] at its gate, as amended on 2026-11-17 ([RC-398]). It covers the seven Go/No-Go
  criteria, with this evidence:
  - [ADR-0045], Accepted after [RC-385] Go and, amended, after [RC-398] Go;
  - the MCP server `grafana` (`mcp-grafana` v2.1.1) that S01 registered in the user-level settings of Claude Code on
    2026-11-17, as the session listed it after S01 started the app again at 10:13 UTC, and its answers;
  - reads of Grafana with the token by the scripts of the check, `GET` requests only, which print status codes,
    names, counts and booleans, never the token;
  - two searches for the token, in the repository and on S01's PC, which print counts, booleans and the paths of the
    files that hold it;
  - the packet filter that `nf-monitoring` had received from the tailnet, read on the LXC on 2026-11-17 at about
    07:10 UTC with `tailscale debug netmap` and reduced there to tags, owners and ports, as for [RC-384].

  Before the gate: [RC-255] (the plan, Go), [RC-357] (the revision, Go), [RC-385] ([ADR-0045], Go) and [RC-398] (the
  amendments to S01's decisions, Go).
- Checklist used: the Go/No-Go criteria of [MIL-052]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criterion 1 rests on [RC-385] and [RC-398], criterion 2 on the check of the logs and the server,
  criteria 3, 4 and 7 on the server and the reads of Grafana, criterion 5 on [ADR-0045] and the searches, and
  criterion 6 on the filter of the LXC, [ADR-0045] point 7 and A3 of the wiki page Monitoring.
- Language and domain: en / it (the Metadata rows of MIL-052). The language of the document was reviewed in [RC-357]
  and [RC-398], and the gate changes nothing in it. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-17 that MIL-052 be finished ("kan du gøre denne
  færdig"), and gave the AI the acceptance of the plan of 2026-11-14 ("Du godkender for mig"). The AI wrote
  [ADR-0045], its amendment, the checks and this record; S01 created the service account and the token, registered
  the server, and disabled the account and enabled it again.

**Evidence of 2026-11-17 (UTC):**

- **The set-up.** `mcp-grafana` v2.1.1 for Windows, its checksum checked against the release, is the entry `grafana`
  in the top-level `mcpServers` of `~/.claude.json`, with the three flags of [ADR-0045] point 5 and the address of
  Grafana on `nf-monitoring`; S01 put the token in it with a text editor. The service account `ai-assistant-read` has
  the Editor role and its token `claude-code` no expiry date, both S01's decisions ([RC-398]).
- **The tools.** After the restart at 10:13 the session listed the server as connected, with 16 tools:
  `search_dashboards` and `search_folders`; `get_dashboard_by_uid`, `get_dashboard_summary`,
  `get_dashboard_property`, `get_dashboard_panel_queries` and `list_dashboard_versions`; `query_prometheus`,
  `query_prometheus_histogram`, `list_prometheus_metric_names`, `list_prometheus_metric_metadata`,
  `list_prometheus_label_names` and `list_prometheus_label_values`; `alerting_rules_read` and
  `alerting_silences_read`; and `alerting_manage_routing`, which offers only its get operations. None of them creates,
  changes or deletes anything. No tool reads Loki, and `grafana_api_request`, `run_panel_query` and `get_panel_image`
  are not registered.
- **No log line.** A Prometheus query through the server against the data source `loki` got 404 and no log line, as
  Loki answered the query paths of Prometheus on 2026-11-15.
- **A past alert.** Asked through the tools of the server when `OffVmCopyStale` last fired, for how long and why, the
  assistant found that it fired on 2026-11-12 from 02:04 to 16:11, about 14 hours. The last copy off the VM had
  succeeded on 2026-11-09 at 02:03:43; the rule fires after 72 hours without a copy, and it fired 72 hours later. It
  resolved when a copy succeeded at 16:11:28. The metric `backup_host_seen`, which `BackupTargetDown` uses, had no
  data from 2026-11-08 to 2026-11-13, so the 72-hour alert was the only warning then. The queries it named:
  `max_over_time(ALERTS{alertstate="firing", alertname=~"ContainerUnhealthy|DiskAlmostFull|OffVmCopyStale"}[15m])`
  over 15 days; the rule, read with `alerting_rules_read`,
  `time() - nfhotel_task_last_success_timestamp_seconds{host="nf-hotel",task="offvm_copy"} > 72 * 3600`;
  `ALERTS{alertname="OffVmCopyStale",alertstate="firing"}` at the start and at the end; and
  `nfhotel_task_last_success_timestamp_seconds` for the tasks `offvm_copy` and `backup_host_seen`.
- **The search for the token.** First from 08:48 to 08:52, before the server ran: every object in the store of the
  repository (17,193 objects), the files of every worktree of the clone, tracked or not, apart from those that git
  ignores, the folder `~/.claude` of Claude Code with the transcripts of its sessions, and the copies of
  `~/.claude.json` in the home folder. Again from 10:28 to 10:42, after the server had run, adding the logs of Claude
  Code and of its MCP servers, the folders of the desktop app, and the scratchpads and task outputs of the sessions.
  Every file was read whole, whatever its size. Both times the token was in no object of the repository, so in no
  commit; in no file of a worktree; and in no transcript, so in no chat message. The second search found it in
  `~/.claude.json` and in one copy of that file that Claude Code had made in `~/.claude/backups` when the app started
  again at 10:13 (F2).
- **Stopping the access.** S01 disabled the service account in Grafana. At 10:25:34 a read with the token
  (`GET /api/search`) got 401, and a query through the server failed with
  `[GET /datasources/uid/{uid}][401] getDataSourceByUidUnauthorized`. S01 enabled the account again; at 10:26:32
  `count(up)` through the server gave 10, with the same token. No token was deleted: S01 keeps the access ([RC-398]).
- **The tailnet filter.** Read on the LXC at about 07:10 and reduced there to tags, owners and ports: unchanged since
  [RC-384]. The devices of the members of the tailnet who are not admins reach every port of `nf-monitoring`,
  Prometheus (9090) and Loki (3100) among them; the devices of the admins reach Grafana on 3000 and the LXC by SSH as
  root in check mode, and one of them also without a check. [ADR-0045] point 7 and A3 of the wiki page Monitoring
  record, for a person, only port 3000 and SSH in check mode.

## Checklist Results

| # | Criterion (MIL-052) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ADR of task 3 is `Accepted` with a `Go` review, and states which data the assistant reads, why, and the GDPR reasoning for the logs | Pass | [ADR-0045] is Accepted after [RC-385] Go and, amended, after [RC-398] Go; point 1 and the check of the logs give the data, the reason and the reasoning on the logs |
| 2 | The logs the assistant may read were checked for personal data of guests and staff, and the result is recorded; if any is found, logs are left out or the data is removed at the source before access is granted | Pass | Task 2 (2026-11-15) found personal data of staff and of visitors in the logs, recorded in [ADR-0045]; the logs are left out: the server registers no tool that reads them, and a query against Loki through it returned no log line |
| 3 | The service account has the role that S01 decided (Editor, 2026-11-17, recorded in [ADR-0045]), and the MCP server registers no tool that creates, changes or deletes anything, shown by the list of its tools | Pass | Editor, as S01 decided; the 16 tools above, none of which writes; no write request was sent (F1) |
| 4 | From the chat, the assistant answers a question about a past alert from a Prometheus query and the alert history, and names the query it ran | Pass | `OffVmCopyStale` of 2026-11-12, from the series `ALERTS`, the rule and the metric of the copies, with the queries above |
| 5 | The token has an expiry date, or S01's recorded decision to keep it without one; it is stored only in S01's user-level MCP settings, and appears in no file of the repository, no commit and no chat message | Pass | No expiry date, S01's decision recorded in [ADR-0045] point 4; the token is in `~/.claude.json` and the copy of it that Claude Code keeps, and in no object, file or transcript searched (F2) |
| 6 | The tailnet policy and `docs/wiki/Monitoring.md` agree, and a person reaches no port of `nf-monitoring` that the ADR does not record | Fail | The live filter lets the devices of members who are not admins reach every port of `nf-monitoring`, and one device of an admin reach it by SSH without a check; [ADR-0045] point 7 and A3 record only port 3000 and SSH in check mode (F4) |
| 7 | Disabling the service account, or deleting the token, in Grafana stops the access at once, shown by a refused request after it | Pass | 401 at 10:25:34 from a read and from the server, right after S01 disabled the account; the same token worked again after the enable |

## Findings

- **F1, note: no write request was sent.** Before the checks of criterion 3, a check of the entry read the
  permissions of the token (`GET /api/access-control/user/permissions`), found the rights of an Editor and stopped,
  so the four writes of criterion 3 as first planned were never sent ([RC-398] F4). Every request to Grafana in this
  gate was a read.
- **F2, note: Claude Code keeps copies of its settings file.** The second search found the token in one more file
  than the first: a copy of `~/.claude.json` that Claude Code made in `~/.claude/backups` when the app started again.
  It is a copy of the same user-level settings, in the same profile on the same PC, so criterion 5 holds. [ADR-0045]
  point 4 and the wiki page now name the copies, and the wiki page says that an old token is stopped only by deleting
  it in Grafana. 38 files of the folders of the desktop app could not be opened while the app ran: listed afterwards,
  they are the caches and the cookies of its built-in browser and a lock file. The token is sent by the MCP server, a
  process of its own, not by that browser.
- **F3, note: the app had to be quit, not closed.** Closing the window of the desktop app left it running by the
  clock, so the server loaded only after S01 quit the app from its icon and started it again. Step 5 of the section
  "AI read access" of the wiki page Monitoring now says so.
- **F4, blocking (criterion 6): the tailnet grants more than the ADR records.** It is F5 of [RC-384], unchanged on
  2026-11-17, and MIL-052 added no grant. On 2026-11-15 S01 chose a risk exception in place of narrowing the grant
  now: owner S01, expiry 2026-11-29. By [PRC-001] its approver is S04, never the author or the owner, and the AI, his
  stand-in, may not approve it. It covers criterion 6 of MIL-052 only if S04's approval names it as well as criterion
  7 of [MIL-051].
- **F5, note: the wiki section is reviewed at the delta re-review.** The section "AI read access" of the wiki page
  Monitoring was changed to S01's decisions on this branch ([RC-398]), and task 1 changes A3 on the same page. The
  page is reviewed against [QC-WIKI-001] once, at the delta re-review, with the filter read again.

## Overall Verdict

**No-Go for now.** Criteria 1 to 5 and 7 pass; criterion 6 fails (F4), and as a Go/No-Go criterion of the gate it
blocks. When S01 narrows the grant to the grants of A3, or S04 approves the risk exception for this criterion in his
own words, the gate is checked again in a delta re-review: the filter read again, the wiki page reviewed (F5), and a
gate row of [MIL-052] that becomes `Accepted`. Tasks 4 and 5 are done; tasks 1 and 6 stay open, and so does the
milestone.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Narrow the grant so that `tag:monitoring` is reached only through the grants of A3 of the wiki page Monitoring, or keep the risk until the expiry of the exception (F4) | S01 | 2026-11-29 |
| Approve or reject the risk exception of F5 of [RC-384], naming criterion 6 of MIL-052 as well as criterion 7 of MIL-051, in his own words | S04 | 2026-11-29 |
| Read the filter again, review the wiki page against [QC-WIKI-001] (F5), and give the gate its verdict in a delta re-review | S01 (the AI as stand-in) | After either of the two above |

---

[MIL-052]: ../../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[ADR-0045]: ../../adr/adr-0045-read-only-monitoring-access-for-the-ai-assistant.md
[RC-255]: ./rc-255-mil-052-plan.md
[RC-357]: ./rc-357-mil-052-revision.md
[RC-384]: ./rc-384-mil-051-gate.md
[RC-385]: ./rc-385-adr-0045.md
[RC-398]: ./rc-398-mil-052-amendments.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[7fdb18c]: https://github.com/NF-Hotel/NFHotel/commit/7fdb18c25de137c04985b3fd0f66798157834164
