# RC-294 Gate review of MIL-023 Operational quality and maturity

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-294 |
| CrossReference | [MIL-023], [ADR-0020], [CIS-001], [RC-244], [RC-284], [RC-292], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-023, held on 2026-11-05 by S01's decision in chat | [803ceae] |

---

## Artifact Under Review

- Instance reviewed: [MIL-023] at its gate: the eight Go/No-Go criteria, with the records of its tasks ([RC-244] for the host settings, [RC-284] and RC-505 for the two restore drills, RC-189 for the alarm drill, RC-177 for Scorecard, SAMM and DORA, RC-178 and RC-179 for the port and the monitoring stack), the read-only checks of `nf-hotel` and `nf-monitoring` of 2026-11-05, the test of the public address from outside the home network of the same day ([RC-292]) and the reads of Loki and Prometheus over the tailnet at 19:00 UTC. With the gate, two documents are amended: [CIS-001] (rows 5.3, 5.28 and 7.1.11/7.1.12, the summary and the closing sentence of criterion 1) and [ADR-0020] (the sentence on the container metrics); both are reviewed here as delta changes.
- Checklist used: the Go/No-Go criteria of [MIL-023]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate. For the two amendments, the rows of QC-ADR-001 and of the checklist of CIS-001 that the changed text touches (status, evidence, consistency of the counts).
- Scope: the gate, and the two delta reviews.
- Language and domain: en / it (the Metadata rows of MIL-023); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. The AI wrote the milestone's later tasks, most of its records and this one; S01 asked in chat on 2026-11-05 to finish the open milestones, answered the questions of the AI the same day (keep `claudeops` and `/opt/nfhotel-legacy`, "Behold hvis det er nemmest"; the metrics since 2026-10-23 are enough, "23 okt er fint"; the container metric is the health series, "OK"), ran the test from outside, and in the evening asked to hold this gate today rather than on 2026-11-06 ("denne mangler 1 issue, lav den og færdiggør og luk også den"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence of 2026-11-05:**

- **The VM, 09:56 UTC** (Tailscale SSH in check mode as `root@nf-hotel`, read-only, under the approval S01 gave at 07:09 UTC): all 11 containers run with `no-new-privileges`, `cap_drop ALL` (nginx and PostgreSQL add their few), a `pids_limit` (Alloy 256, the socket proxy 128), a read-only root, memory and CPU limits and the `local` log driver with 5 files of 10 MB; all healthy, the socket proxy without a health check by design; `unattended-upgrades` installed updates on 2026-11-04 (a kernel) and 2026-11-05; `daemon.json` as designed. `/opt/nfhotel-legacy` still holds the one file with no group (owner root, mode 440), and the user `claudeops` exists with its sudoers rule and no login since the records began; S01 decided to keep both.
- **The LXC, 09:59 UTC**: from `nf-monitoring`, on the LAN and the tailnet, nothing answers on the LAN address of the VM on 22, 80, 443, 5432, 50023 or 50024; on the tailnet address 80 and 443 answer, 22 and 5432 do not (22 is Tailscale SSH, which answers only a device the policy allows; the PC of S01 is one, and this review used it).
- **The internet, 18:35 UTC** ([RC-292]): S01, on another network than the home's, tested the public address of the home network with `Test-NetConnection` of PowerShell 7.6.9 on the ports 22, 80, 443, 5432, 50023 and 50024: none answers. The public site is served through the Cloudflare tunnel, so no port of the home network is open.
- **Loki and Prometheus, 19:00 UTC** (HTTP over the tailnet from the PC of S01): Loki holds a stream for every one of the 12 containers that ran on the VM in the period and for the systemd journal; the oldest lines are of 2026-10-23 (09:51 UTC for PostgreSQL, Ollama, nginx and the market analysis, 14:16 to 14:48 UTC for Alloy, the API, the web application and the benchmark, 07:16 UTC for the journal), of 2026-10-25 for cloudflared and 2026-10-28 for the relay, their first days; every stream reaches the minute of the read. The running configuration has `retention_enabled: true` and `retention_period: 2w`. Prometheus runs with `--storage.tsdb.retention.time=30d`; the oldest samples of the 1 193 `node_*` series and of `nfhotel_container_healthy` (11 series) are of 2026-10-23; no `container_*` series exists. Grafana 12.2.1 answers `database: ok`.
- **CI**: the job `Ops monitoring configuration (MIL-023)` (`promtool`, `amtool` and `alloy fmt` on the configuration) is green on the pull requests of the day (#514, #521, #522, #523, among them the Ops CI run 42076568878) and the job `Ops host hardening (Debian 13, MIL-023)` with it; the Scorecard workflow ran green on `main` on 2026-11-05 (run 42089133368).

## Checklist Results

| # | Criterion (MIL-023) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The CIS checklist covers every selected item, each with a status and evidence; no "applied" item is without evidence | Pass | CIS-001 amended at this gate: rows 5.3 and 5.28, Pending VM since RC-244, are Applied with the read of 2026-11-05 (all 11 containers); no row is Pending VM; the Docker summary is 46 Applied, 0 Pending VM, 6 Deferred and 6 Not applicable; row 7.1.11/7.1.12 stays Deferred with S01's decision of 2026-11-05 as its reason; every Applied row names its evidence |
| 2 | From outside the tailnet, only 80 and 443 of the VM answer (nmap); 22 answers only over Tailscale; this holds after a Docker restart | Pass | From the internet none of the six ports answers (RC-292; a TCP connection test from another network stood in for nmap, port by port); from the LAN nothing answers, because nginx binds to the tailnet address; on the tailnet 80 and 443 answer and 22 only through Tailscale SSH for an allowed device. After a Docker restart: `test-nftables.sh` proves that the rules of `docker-user` survive Docker replacing its table (RC-178, RC-244), and the chain still holds its rules on the VM (RC-285, RC-292) |
| 3 | `docker inspect` shows `no-new-privileges` and dropped capabilities for every service on the checklist, and all services stay healthy | Pass | The read of 09:56 UTC, all 11 containers; the reads of 16:59 and 18:47 UTC (the gates of MIL-061 and MIL-062) found every container with a health check healthy after two deploys the same day |
| 4 | `unattended-upgrades` has installed security updates (log entry), and container logs are size-limited | Pass | The log of `unattended-upgrades` holds the installs of 2026-11-04 and 2026-11-05; `daemon.json` sets the `local` log driver with `max-size` 10m and `max-file` 5, and every container runs with it (the read of 09:56 UTC) |
| 5 | The Scorecard workflow runs and its result is recorded; the SAMM baseline and the first DORA report exist | Pass | RC-177 (the workflow, the script, SAMM-001 and DORA-001); the Scorecard run of 2026-11-05 on `main` green; the two documents are in `docs/sqa/` |
| 6 | Two monthly restore drills are recorded | Pass | RC-505 (2026-10-20) and RC-284 (2026-11-05), both Go |
| 7 | An alarm drill raises a Discord alert within the stated time each: a stopped web container (3 min), a stale backup report (27 h), a certificate check below 21 days, and a stopped Prometheus (the watchdog) | Pass | RC-189: the stopped web container alerted in 2 min 45 s at most, the stale backup report in 49 s to 1 min 48 s, the certificate rule in 10 min 40 s of which the rule waits 10 min, and the stopped Prometheus reached Healthchecks.io in about 8 min; RC-188 corrected the stated time of the watchdog to about 8 min, so every alert is within its stated time |
| 8 | The logs of every container on `nf-hotel` can be searched in Grafana for 14 days, and the host and container metrics for 30 days; `promtool`, `amtool` and `alloy fmt` pass in CI | Pass | Loki keeps 14 days (`retention_period: 2w`) and holds every container's logs and the journal since the stack started on 2026-10-23, 13 days and 9 hours at the gate for the oldest streams and the whole life of each container for the others; nothing has been dropped, and the fourteenth day completes on 2026-11-06 at 09:51 UTC. Prometheus keeps 30 days (`30d`) and holds the host metrics and the container health series since 2026-10-23; S01 accepted the configuration plus the data since that day as the evidence of the metrics ("23 okt er fint"), and ADR-0020 is corrected at this gate: the container metric is the health series of `report-health.sh`, not a cAdvisor exporter. The three tools pass in the job `Ops monitoring configuration (MIL-023)` on every pull request of the day. S01 decided in chat on 2026-11-05 to hold the gate today |

**Delta review of the two amendments:**

| Document | Change | Status | Notes |
| --- | --- | --- | --- |
| CIS-001 | Rows 5.3 and 5.28 to Applied with the evidence of 2026-11-05; row 7.1.11/7.1.12 with S01's decision; the summary counts (Docker 46, 0, 6, 6) and the closing sentence of criterion 1; the Version History row, with the two-row rule kept and the unused commit links removed | Pass | The counts equal the rows (46 Applied, 6 Deferred, 6 Not applicable, 0 Pending VM); every changed row names the read it comes from; no evidence is copied from the VM but the fields `cis-evidence.sh` reads |
| ADR-0020 | Consequences, "What is collected": the container metric is `nfhotel_container_healthy`, written by `report-health.sh` through the text-file collector of the node exporter; there is no cAdvisor exporter and the per-container CPU and memory series were never built; the Version History row | Pass | QC-ADR-001 criteria 8 and 11: a new row with the change and the reason, the retained row moved to Deprecated, statuses from the allowed set; the decision itself is unchanged, the consequence now says what runs |

## Findings

- **The fourteenth day is tomorrow morning.** The oldest log lines are 13 days and 9 hours old at the gate; the retention is configured to two weeks and nothing has been removed in the life of the stack, so the capability the criterion names exists. S01 chose to close the milestone today; this record says so, and the full 14 days are there on 2026-11-06 at 09:51 UTC without anyone doing anything.
- **The metrics have 13 days of data, by design.** The stack started on 2026-10-23; 30 days of data cannot exist before 2026-11-22. The 30-day retention and the data since the start are the evidence S01 accepted on 2026-11-05.
- **ADR-0020 said cAdvisor; the VM has the health series.** The sentence promised per-container CPU and memory series that were never built; the alert rules use the health series, which exists for every container. S01 accepted the correction ("OK") rather than a cAdvisor exporter; the ADR now says what runs.
- **Two Deferred items by decision.** The file with no group under `/opt/nfhotel-legacy` and the unused user `claudeops` stay, by S01's decision of 2026-11-05 ("Behold hvis det er nemmest"); the CIS row records the decision as the reason. They are reviewed again when the folder or the user goes.
- **The test from outside was a TCP connection test, not nmap.** The criterion names nmap; S01 tested each port with `Test-NetConnection` from another network, which answers the same question for the six ports that matter. An nmap run from outside stays an option for the next change of the router or the firewall (the action item of RC-292).

## Overall Verdict

Go: the eight criteria pass on the records of the tasks, the reads of 2026-11-05 and the test from outside; the two amendments are accepted with this record. The milestone closes: issue #140 and the GitHub milestone #13 close through the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run the next monthly restore drill and record it (ADR-0025) | S01 (AI as stand-in) | 2026-12-05 |
| Produce the DORA report of November with `ops/scripts/dora-metrics.sh` and record it | S01 (AI as stand-in) | 2026-12-01 |
| Review the two Deferred CIS items again when `/opt/nfhotel-legacy` or `claudeops` goes | S01 | Open |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[CIS-001]: ../cis-checklist.md
[RC-244]: ./rc-244-mil-023-host-settings.md
[RC-284]: ./rc-284-restore-drill-2026-11-05.md
[RC-292]: ./rc-292-mil-048-gate-re-review.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[803ceae]: https://github.com/NF-Hotel/NFHotel/commit/803ceae78c9552d30230894be4f86c724a730859
