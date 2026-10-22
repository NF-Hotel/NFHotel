# ADR-0020 Monitoring with Prometheus, Grafana, Loki and Alloy, watched from outside

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0020 |
| CrossReference | [DSA-001], [ADR-0003], [ADR-0007], [ADR-0008], [ADR-0009], [ADR-0010], [ADR-0018], [MIL-023] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-22 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the stack was chosen by S01 in chat on 2026-10-22, accepted after RC-168 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** The choice of tools is S01's, made in chat on 2026-10-22 after a comparison that the AI assistant prepared. S01 asked whether Prometheus and Grafana, with Loki and Alloy, would cover the needs, and answered "yes" to that stack with an external watchdog. The placement and the alert channel follow the assistant's recommendation, which S01 can change by a new row of this ADR. The reasons below were written by the AI assistant.

Nothing watches production today. Faults happen in silence:

- From 2026-10-21 the certificate renewal on `nf-hotel` failed every night (RC-162). Its journal said so, but no one read it.
- The off-VM copy of the backups was missing for days. It showed only as a warning in a log (RC-161).
- No one is told when the site is down, the disk fills, a container turns unhealthy or a timer stops.

Forces:

- The site is reachable only on the tailnet (ADR-0009), so a hosted uptime service on the internet cannot reach it.
- The team is small, and the tools must be free.
- The configuration should be in git, reviewed and reproducible, like the rest of the operations (ADR-0010).
- A monitor that runs on `nf-hotel` itself is blind when that VM is down.
- A monitor cannot report its own death.

Options considered:

1. **Prometheus, Alertmanager, Grafana, blackbox_exporter, Loki and Alloy**, with an external watchdog (Healthchecks.io). The industry standard: metrics, logs, dashboards and alerts in one place. Its configuration is files, and Grafana provisioning keeps the dashboards in git. It costs more parts to run and a learning curve (PromQL, LogQL).
2. **Gatus**, one service whose checks are one YAML file in git. It covers uptime, the certificate and job heartbeats. It has no disk, CPU or container metrics, no dashboards over time and no logs.
3. **Uptime Kuma**, a single service set up through a web interface. It is the quickest to start, but its configuration is in its own database and not in git.
4. **A hosted service** (UptimeRobot, Better Stack, Grafana Cloud). Nothing to run, but it cannot reach a tailnet-only site without pushing data out, and the data leaves the team.
5. **Nothing.** This is how the faults above went unseen.

Sub-choices:

- **Logs:**
  - Loki with Alloy, chosen: it is read in Grafana and indexes labels only, so it is light.
  - Elastic or OpenSearch with Kibana: several GB of memory.
  - Graylog: needs OpenSearch and MongoDB.
  - VictoriaLogs: lighter, but less used.
- **The watchdog:**
  - Healthchecks.io, hosted, chosen: free, and it receives only a ping.
  - Dead Man's Snitch or Cronitor.
  - A check from the legion laptop, rejected because the laptop is often off.
- **Errors in the application:**
  - Sentry (hosted) or GlitchTip (self-hosted) group exceptions into issues. This is deferred: until then, Loki shows the exceptions that the application logs.
- **Placement:**
  - A small LXC container on the Proxmox node, chosen: always on, and another machine than `nf-hotel`.
  - The legion laptop: often off.
  - `nf-hotel` itself: blind when it is down.
- **Alert channel:**
  - A Discord channel of the team (`#nf-hotel-alerts`), chosen.
  - E-mail: easily missed.
  - ntfy: another app to install.

## Decision

We follow option 1. Production is watched by Prometheus, Alertmanager, Grafana, blackbox_exporter and Loki in an LXC container on the Proxmox node, fed by Alloy on `nf-hotel`. Alerts go to Discord, and Healthchecks.io watches the watchers.

1. **Where.** One LXC container on the Proxmox node, reachable on the tailnet only, runs these with Docker Compose from `ops/deploy/monitoring/`:
   - Prometheus;
   - Alertmanager;
   - Grafana;
   - blackbox_exporter;
   - Loki.

   Its address and size are recorded in the wiki page Proxmox Deployment.
2. **What is collected.**
   - **Alloy** runs on `nf-hotel` (`ops/deploy/alloy/`). It sends the logs of every container and of the systemd units to Loki. It also sends the host metrics and the container metrics (its built-in node and cAdvisor exporters) to Prometheus.
   - **blackbox_exporter** checks over the tailnet:
     - the HTTPS pages, the redirect on port 80 and the API health;
     - the days left on the certificate.
3. **Alerts** (rules in git, sent by Alertmanager to Discord):
   - the site or the API down for 2 minutes;
   - the certificate valid for less than 21 days;
   - the disk over 80 %;
   - a container unhealthy or restarting;
   - the last backup older than 26 hours;
   - the last off-VM copy older than 72 hours;
   - the last certificate renewal run older than 26 hours.
4. **The jobs report.** The backup, the copy and the certificate renewal write the time of their last success where Alloy reads it (a metrics text file). A missing report raises the alert of point 3.
5. **The watchdog.** Alertmanager sends an always-firing `Watchdog` alert to Healthchecks.io every minute, and Healthchecks.io alerts on Discord when it stops. This covers a dead Prometheus, Alertmanager, LXC or Proxmox node.
6. **Configuration as code.** The Prometheus configuration and rules, the Alertmanager routes, the Grafana data sources and dashboards (provisioning), the Loki and Alloy configuration are files in `ops/`. They are checked in CI (`promtool check`, `amtool check-config`, `alloy fmt`) and installed by CD for `nf-hotel` (ADR-0018) or by a script for the LXC.
7. **Secrets.** The Discord webhook and the Healthchecks ping URL live in git-ignored settings files, never in git.
8. **Retention.** Metrics 30 days, logs 14 days. The deployment log on `nf-hotel` stays 90 days (MIL-023).
9. **Deferred.** Error tracking with Sentry or GlitchTip is a later decision. It needs a change in the application.

## Consequences

**Positive:**

- A fault is reported on Discord within minutes, including a job that did not run, which is the fault of 2026-10-21.
- The disk, the memory, the containers and the logs are in one Grafana, with history, so a fault can be traced.
- The configuration is in git, reviewed, tested in CI and reproducible.
- The industry-standard tools are worth learning for the team.
- The watchdog covers the death of the monitoring itself and of the Proxmox node.

**Negative:**

- About six services to run and to update in the LXC, about 2–3 GB of memory, and the learning of PromQL and LogQL.
- The LXC is on the same physical machine as `nf-hotel`. A dead Proxmox node is reported only through the watchdog, not with detail.
- Healthchecks.io and Discord are third parties. Healthchecks.io receives only a ping; Discord receives the alert texts, which must hold no personal data or secrets.
- Exceptions of the application are text lines in Loki, not grouped issues, until error tracking is decided.
- Alloy on `nf-hotel` is one more agent on the production VM. It runs with read-only access to the Docker socket and the journal.

## Affected Artifacts

- [MIL-023] — amended: the observability deliverable becomes this stack, with tasks and criteria (RC-169).
- [ADR-0008] — the backup and the off-VM copy report their last success.
- [ADR-0009] — the certificate renewal reports its last run, and the days left are checked.
- [ADR-0010] — the monitoring configuration is in the repository, like the deployment.
- [ADR-0018] — CD installs Alloy and its configuration on `nf-hotel`.
- [DSA-001] — closes the observability gaps of the assessment.

---

[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ./adr-0008-database-migration-and-backup.md
[ADR-0009]: ./adr-0009-tls-certificates.md
[ADR-0010]: ./adr-0010-production-vm-as-deployed.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
