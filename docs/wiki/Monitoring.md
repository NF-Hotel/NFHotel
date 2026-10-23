# Monitoring

How NF Hotel is watched, and what to do when an alert arrives (ADR-0020, MIL-023 tasks 7 to 10).

## What runs where

| Part | Where | What it does |
| --- | --- | --- |
| [Prometheus](https://prometheus.io/docs/introduction/overview/) | Monitoring LXC on the Proxmox node | Keeps the metrics for 30 days and evaluates the alert rules (`ops/deploy/monitoring/prometheus/rules/nfhotel.yml`) |
| [blackbox_exporter](https://github.com/prometheus/blackbox_exporter) | Monitoring LXC | Probes `https://nf-hotel.taildb84b5.ts.net/` (200 and the certificate) and port 80 (301) from outside the VM |
| [Alertmanager](https://prometheus.io/docs/alerting/latest/alertmanager/) | Monitoring LXC | Sends the alerts to Discord `#nf-hotel-alerts`, and the always-firing `Watchdog` to Healthchecks.io |
| [Loki](https://grafana.com/docs/loki/latest/) | Monitoring LXC | Keeps the logs for 14 days |
| [Grafana](https://grafana.com/docs/grafana/latest/) | Monitoring LXC, port 3000 on the tailnet | The dashboard "NF Hotel overview", and the log search (Explore, data source Loki) |
| [Grafana Alloy](https://grafana.com/docs/alloy/latest/) | `nf-hotel` (`ops/deploy/alloy/`) | Sends the container logs and the journal to Loki, and the host metrics and the text files of `/var/lib/nfhotel-metrics` to Prometheus |
| [Healthchecks.io](https://healthchecks.io/docs/) | SaaS | Alerts when the `Watchdog` stops arriving, which means that the monitoring itself is down |

The monitoring runs off the VM, so it still reports when `nf-hotel` is down.

On `nf-hotel`, three jobs write the time of their last success to `/var/lib/nfhotel-metrics`:

- the backup writes `backup.prom`;
- the copy off the VM writes `offvm_copy.prom`;
- the certificate renewal writes `cert_renew.prom`.

`nfhotel-report-health.timer` writes `containers.prom` every minute. CD installs all of them (ADR-0018), so they need no manual step.

## Setting it up (once)

S03 does these steps; nothing here is done by CD.

1. **The LXC.** On the Proxmox node, as root:
   `bash ops/scripts/create-monitoring-lxc.sh 120` (a dry run), then again with `--apply`.
2. **The tailnet.** In the LXC, run `tailscale up --advertise-tags=tag:monitoring` and sign in. Then add these entries to the tailnet policy in the Tailscale admin console:

   ```json
   "tagOwners": { "tag:monitoring": ["autogroup:admin"] },
   "grants": [
     { "src": ["tag:nfhotel-vm"], "dst": ["tag:monitoring"], "ip": ["tcp:9090", "tcp:3100"] },
     { "src": ["tag:monitoring"], "dst": ["tag:nfhotel-vm"], "ip": ["tcp:80", "tcp:443"] },
     { "src": ["autogroup:admin"], "dst": ["tag:monitoring"], "ip": ["tcp:3000"] }
   ]
   ```

   Alloy writes to Prometheus (9090) and Loki (3100), and the probes reach the site (80, 443). Only admins open Grafana (3000).
3. **The secrets.** In the LXC, clone `NF-Hotel/NFHotel` and copy `ops/deploy/monitoring/monitoring.env.example` to `monitoring.env`. Set `MONITORING_BIND` to the output of `tailscale ip -4`. Then make the three files of `secrets.example/README.md` in `secrets/`, each with mode 0600:
   - **The Discord webhook.** In Discord, open the settings of `#nf-hotel-alerts`, then Integrations, then Webhooks, then New Webhook, and copy its URL.
   - **The Healthchecks.io check.** Create a check named "NF Hotel monitoring watchdog", with a period of 1 minute and a grace time of 5 minutes, and copy its ping URL. Under Integrations, add Discord or e-mail.
   - **The Grafana password.** A random password of at least 16 characters.

   Never paste these values in a chat, an issue or a commit.
4. **Start it.** Run `bash ops/scripts/deploy-monitoring.sh`. The checks run first, and nothing starts yet. When they pass, run it again with `--apply`.
5. **Alloy on nf-hotel.** Copy `ops/deploy/alloy/alloy.env.example` to `alloy.env` in `/opt/nfhotel/ops/deploy/alloy/`, with the address of the LXC. From then on, every CD also starts Alloy. Without the file, CD leaves Alloy out and prints a note.

## When an alert arrives

| Alert | First step |
| --- | --- |
| `SiteDown` | Open the site. If it is down, run `sudo docker ps` on `nf-hotel`, then read the latest CD run (wiki page [CI-CD](CI-CD)) |
| `RedirectBroken` | `curl -sI http://nf-hotel.taildb84b5.ts.net/` must answer 301; check the nginx container |
| `CertificateExpiresSoon` | `systemctl status nfhotel-cert-renew.service` and its journal (ADR-0009) |
| `NoMetricsFromNfHotel` | The VM, Alloy (`sudo docker ps` for `alloy`) or the tailnet path is down |
| `DiskAlmostFull` | `df -h /`, then `sudo docker image prune` |
| `ContainerUnhealthy` | `sudo docker logs --tail 50 <container>` |
| `BackupStale` | `systemctl status nfhotel-backup.timer` and `journalctl -u nfhotel-backup` (ADR-0008) |
| `OffVmCopyStale` | The backup target on the legion laptop is off or unreachable; the backups wait on the VM |
| `CertificateRenewalStale` | `journalctl -u nfhotel-cert-renew` |
| Healthchecks.io: watchdog down | The monitoring LXC or Alertmanager is down: `pct status 120` on the node, then `docker ps` in the LXC |

## Checks in CI

The job "Ops monitoring configuration" runs `ops/tests/check-monitoring.sh` on every pull request. It uses the tools of the pinned images:

- `promtool check config`, and `promtool test rules` (`prometheus/rules-test.yml`);
- `amtool check-config`;
- `loki -verify-config` and `blackbox_exporter --config.check`;
- `alloy fmt`;
- a check that the dashboards are valid JSON.
