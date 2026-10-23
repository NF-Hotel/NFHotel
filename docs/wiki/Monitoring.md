# Monitoring

How NF Hotel is watched, why it is built this way, how to install it step by step, and what to do when an alert arrives. The decision and the options that were weighed are in ADR-0020; the work is MIL-023 tasks 7 to 10.

## What it answers

The team should learn that something is wrong before a guest does, without anyone watching a screen:

- Is the site up, does port 80 redirect, and is the certificate valid?
- Is a container stopped or unhealthy, or is the disk filling up?
- Did the backup, the copy off the VM and the certificate renewal succeed?
- What did a container log before it failed, even after a deploy recreated it?
- Is the monitoring itself alive?

## The parts, and why each was chosen

| Part | Where | What it does | Why this one |
| --- | --- | --- | --- |
| [Prometheus](https://prometheus.io/docs/introduction/overview/) | Monitoring LXC | Keeps the metrics for 30 days and evaluates the alert rules | The standard for metrics and alert rules. The rules are plain files in git that CI tests with `promtool test rules`. It is free and self-hosted, so no data leaves the tailnet |
| [blackbox_exporter](https://github.com/prometheus/blackbox_exporter) | Monitoring LXC | Probes `https://nf-hotel.taildb84b5.ts.net/` (status 200 and the certificate days) and port 80 (301) | It checks the site the way a user meets it, from outside the VM, so a dead VM is seen too |
| [Alertmanager](https://prometheus.io/docs/alerting/latest/alertmanager/) | Monitoring LXC | Sends the alerts to Discord `#nf-hotel-alerts` | It groups alerts, repeats them every 4 hours while they last, and sends "resolved". It also silences the follow-on alerts: a dead site does not also report a broken redirect |
| [Loki](https://grafana.com/docs/loki/latest/) | Monitoring LXC | Keeps the logs for 14 days | It indexes only labels (container, host), so it is light enough for a small LXC. The logs survive a container that is recreated |
| [Grafana](https://grafana.com/docs/grafana/latest/) | Monitoring LXC, port 3000 on the tailnet | The dashboard "NF Hotel overview", and log search (Explore, data source Loki) | One place for metrics and logs. The data sources and the dashboard are provisioned from git, so nothing is clicked together by hand |
| [Grafana Alloy](https://grafana.com/docs/alloy/latest/) | `nf-hotel` (`ops/deploy/alloy/`) | Sends the container logs and the journal to Loki, and the host metrics and the reports in `/var/lib/nfhotel-metrics` to Prometheus | One agent for logs and metrics instead of three (node_exporter, cAdvisor, Promtail). Promtail is deprecated in favour of Alloy |
| [Healthchecks.io](https://healthchecks.io/docs/) | SaaS (free plan) | Alerts when the `Watchdog` stops arriving | A monitor cannot report its own death. Healthchecks.io runs somewhere else and alerts when the minute signal from Alertmanager stops |
| Discord `#nf-hotel-alerts` | SaaS | Where alerts arrive | The team already uses Discord, and its phone app gives a push message. Nobody needs a new tool |

### Why these choices

- **The monitoring runs off the VM.** It runs in an LXC on the Proxmox node, not on `nf-hotel`, so it still reports when the VM is down. A VM monitoring itself goes silent at the exact moment it is needed.
- **An LXC, not a second VM.**
  - It shares the node's kernel, so it needs about 4 GiB of memory instead of a full guest.
  - It is unprivileged (root inside is not root on the node).
  - *nesting* and *keyctl* are the only features Docker needs.
- **Tailnet only.**
  - Grafana, Prometheus and Loki are published on the LXC's tailnet address only.
  - The tailnet policy lets only the needed pairs talk.
  - No port is opened on the internet, and no password protects a public page.
- **Push from nf-hotel.**
  - Alloy sends to Prometheus (remote write) and to Loki; the LXC never logs in to the VM.
  - Nothing new listens on `nf-hotel`, and the firewall of MIL-023 task 3 stays as it is.
- **Docker is read through a proxy.** Alloy reads the Docker API through a socket proxy that allows only read calls on containers. A bug in Alloy cannot start, stop or exec containers.
- **The jobs report their success.** "Is the timer enabled?" is not enough: a timer can run a job that fails every night.
  - Each job writes the time of its last success.
  - An alert fires when that time grows old, or when the report is missing.
- **Everything is code.**
  - Every rule, route, dashboard and pipeline is a file in `ops/`.
  - CI checks them with the tools of the pinned images on every pull request.
  - CD installs the parts on `nf-hotel` (ADR-0018).

### The jobs on nf-hotel

| File in `/var/lib/nfhotel-metrics` | Written by | Alert when |
| --- | --- | --- |
| `backup.prom` | The database backup (`nfhotel-backup.timer`, and before every deploy) | Older than 26 hours (`BackupStale`) or missing (`TaskReportMissing`) |
| `offvm_copy.prom` | The copy to the backup target on the legion laptop | Older than 72 hours (`OffVmCopyStale`); the laptop may be off for a weekend |
| `cert_renew.prom` | The certificate renewal (`nfhotel-cert-renew.timer`) | Older than 26 hours (`CertificateRenewalStale`) |
| `containers.prom` | `nfhotel-report-health.timer`, every minute | A container stopped or unhealthy for 30 s (`ContainerUnhealthy`), or no report (`HealthReportMissing`) |

CD installs and enables these. They already run, before the monitoring exists.

## Installation, step by step

S03 does these steps once. They are in this order because each step needs the one before it. Every value that is a secret goes into a file and nowhere else: not into a chat, an issue, a commit or a screenshot.

### Part A: in the browser (about 15 minutes)

**A1. The Discord webhook.**
1. In Discord, create the text channel `#nf-hotel-alerts` if it does not exist.
2. Open its settings (the gear), then *Integrations*, then *Webhooks*, then *New Webhook*.
3. Name it `NF Hotel monitoring` and click *Copy Webhook URL*. Keep the URL in your password manager until step C4.

*Why:* anyone who has the URL can post in the channel, so it is a secret.

**A2. The Healthchecks.io check.**
1. Sign up at [healthchecks.io](https://healthchecks.io/) (the free plan is enough).
2. Add a check named `NF Hotel monitoring watchdog` with *Period* 1 minute and *Grace time* 5 minutes.
3. Under *Integrations*, add Discord (the same channel) or your e-mail.
4. Copy the check's ping URL (`https://hc-ping.com/...`) to your password manager.

*Why:* Alertmanager pings the URL every minute. When the pings stop for 5 minutes, Healthchecks.io alerts you that the monitoring itself is down.

**A3. The tailnet policy.**
1. In the [Tailscale admin console](https://login.tailscale.com/admin/acls), open *Access controls*.
2. Add `tag:monitoring` to `tagOwners`, and add the three grants below to the existing `grants`:

   ```json
   "tagOwners": { "tag:monitoring": ["autogroup:admin"] },
   "grants": [
     { "src": ["tag:nfhotel-vm"], "dst": ["tag:monitoring"], "ip": ["tcp:9090", "tcp:3100"] },
     { "src": ["tag:monitoring"], "dst": ["tag:nfhotel-vm"], "ip": ["tcp:80", "tcp:443"] },
     { "src": ["autogroup:admin"], "dst": ["tag:monitoring"], "ip": ["tcp:3000"] }
   ]
   ```
3. Save. The console refuses a policy that breaks its own tests, so a typing error cannot lock anyone out.

*Why:*
- Alloy on the VM may write to Prometheus (9090) and Loki (3100).
- The probes may reach the site (80, 443).
- Only admins may open Grafana (3000).
- No other traffic is allowed.

The tag must exist before step C1, which gives it to the LXC.

Also add this entry to the existing `ssh` list. It lets an admin open a shell in the LXC with Tailscale SSH, after a fresh sign-in (`check`):

```json
{ "action": "check", "src": ["autogroup:admin"], "dst": ["tag:monitoring"], "users": ["root"] }
```

### Part B: on the Proxmox node, as root (about 10 minutes)

Open the node's *Shell* in the Proxmox web interface.

**B1. Choose a free container ID.**

```bash
pct list; qm list
```

Use an ID that neither list shows, for example `120`. The VM `nf-hotel` is 100.

**B2. Download the Debian 13 template.**

```bash
pveam update
pveam available --section system | grep debian-13
pveam download local debian-13-standard_13.1-2_amd64.tar.zst
```

If the list shows a newer file name, download that one, and later set `MONITORING_TEMPLATE=local:vztmpl/<file name>` in front of the script.

*Why:* Debian 13 is the release `nf-hotel` runs, so the same commands and fixes apply to both.

**B3. Get the scripts.**

```bash
apt-get install -y git
git clone --depth 1 https://github.com/NF-Hotel/NFHotel.git /root/nfhotel
```

**B4. Check, then create.**

```bash
pvesm status
bash /root/nfhotel/ops/scripts/create-monitoring-lxc.sh 120
```

The first command lists the storages. The second command only prints what it would do.

- The storage defaults to `local-lvm`. If `pvesm status` shows another name, set `MONITORING_STORAGE=<name>` in front of the script.
- The bridge defaults to `vmbr0`; set `MONITORING_BRIDGE` the same way if yours differs.

When the printout looks right, create the container:

```bash
bash /root/nfhotel/ops/scripts/create-monitoring-lxc.sh 120 --apply
```

The container gets the name `nf-monitoring`, 2 cores, 4 GiB of memory and a 32 GiB disk, and it starts at boot. The script also installs Docker, git and Tailscale inside it, from Tailscale's signed apt repository.

*Why the dry run first:* `pct create` changes the node. The printout lets you check the storage, the bridge and the ID before anything happens.

### Part C: inside the LXC (about 15 minutes)

```bash
pct enter 120
```

**C1. Join the tailnet.**

```bash
tailscale up --ssh --advertise-tags=tag:monitoring --hostname=nf-monitoring
```

Open the sign-in link it prints, in your own browser, and approve the machine. Then run:

```bash
tailscale ip -4
```

It prints the LXC's tailnet address, for example `100.x.y.z`. You need it in C3 and D1.

*Why:* `--ssh` lets you reach the LXC later with `tailscale ssh root@nf-monitoring` without an open ssh port. The tag puts it under the grants of A3.

**C2. Get the configuration.**

```bash
git clone https://github.com/NF-Hotel/NFHotel.git /opt/nfhotel-monitoring
cd /opt/nfhotel-monitoring/ops/deploy/monitoring
```

**C3. The settings.**

```bash
cp monitoring.env.example monitoring.env
sed -i "s/^MONITORING_BIND=.*/MONITORING_BIND=$(tailscale ip -4)/" monitoring.env
cat monitoring.env
```

**C4. The secrets.** These commands read the values without showing them, and keep them out of the shell history:

```bash
install -d -m 700 secrets
umask 077
read -rsp 'Discord webhook URL: ' v && printf '%s' "$v" > secrets/discord-webhook-url; echo
read -rsp 'Healthchecks ping URL: ' v && printf '%s' "$v" > secrets/healthchecks-watchdog-url; echo
unset v
head -c 18 /dev/urandom | base64 | tr -d '\n' > secrets/grafana-admin-password
chmod 600 secrets/*
cat secrets/grafana-admin-password; echo
```

The last command shows the Grafana password once. Save it in your password manager.

*Why:*
- The files are git-ignored, and readable only by root (0600). The start script refuses a file that a group or another user can read.
- In C5, the start script gives each file to the one container that reads it, mode 0400:
  - the two URLs to Alertmanager, which runs as `nobody`;
  - the password to Grafana, which runs as user 472.

  The containers drop every capability, so they cannot read a file of root. The folder becomes 0711, so it can be passed through but not listed.

**C5. Check, then start.**

```bash
bash /opt/nfhotel-monitoring/ops/scripts/deploy-monitoring.sh
bash /opt/nfhotel-monitoring/ops/scripts/deploy-monitoring.sh --apply
docker compose --env-file monitoring.env ps
```

The first command checks the settings, the secrets and the configuration with the pinned tools, and starts nothing. When it ends with "configuration valid", the second command starts the stack. It returns when every service is healthy, and `ps` lists five services, all `healthy` or `running`.

**C6. Look.**
1. On your own computer, which is on the tailnet, open `http://<the address of C1>:3000`.
2. Sign in as `admin` with the password of C4.
3. The dashboard "NF Hotel overview" shows the site probes at once. The host data appears after part D.

Within a few minutes, the Healthchecks.io check turns green, because the Watchdog arrives.

### Part D: on nf-hotel (about 5 minutes)

**D1. Point Alloy at the LXC.**

```bash
cd /opt/nfhotel/ops/deploy/alloy
sudo -u nfhotel cp alloy.env.example alloy.env
sudo -u nfhotel sed -i "s/100\.64\.0\.2/<the address of C1>/" alloy.env
sudo -u nfhotel cat alloy.env
```

The next CD run starts Alloy by itself. Until the file exists, CD leaves Alloy out with a note, so CD never breaks because the monitoring is missing.

*Why it is not started by hand:* CD owns what runs on `nf-hotel` (ADR-0018). A container started by hand would be replaced, or forgotten, at the next deploy.

**D2. Check.** After that CD run, open *Explore* in Grafana, choose *Loki*, and run `{host="nf-hotel"}`: the container logs appear. Within 10 minutes, the dashboard shows the disk, the memory and the containers.

### Part E: the alarm drill (MIL-023 task 10)

Raise each alert on purpose once, and record when it arrives in Discord (the record is an RC):

| Alert | How to raise it | Must arrive within |
| --- | --- | --- |
| A stopped web container | `sudo docker stop nfhotel-web-nfhotel-web-1`, then start it again | 3 minutes |
| A stale backup | Backdate `backup.prom` by 27 hours: `sudo sed -i 's/ [0-9]*$/ '"$(($(date +%s) - 97200))"'/' /var/lib/nfhotel-metrics/backup.prom` | The next evaluation; the next backup writes a fresh file |
| The certificate check | Raise the threshold from 21 to 400 days in a branch of the rules, deploy it to the LXC, then set it back | 10 minutes |
| A stopped Prometheus | `docker compose --env-file monitoring.env stop prometheus` in the LXC, then start it | 5 minutes (Healthchecks.io) |

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
| `TaskReportMissing` | The job never succeeded, or its file in `/var/lib/nfhotel-metrics` was removed; run the job's service and read its journal |
| `HealthReportMissing` | `systemctl status nfhotel-report-health.timer` |
| Healthchecks.io: watchdog down | The monitoring LXC or Alertmanager is down: run `pct status 120` on the node, then `docker ps` in the LXC |

## Updating the monitoring

A change of `ops/deploy/monitoring/` is reviewed in a pull request, and the job "Ops monitoring configuration" checks it. After the merge, run this in the LXC:

```bash
cd /opt/nfhotel-monitoring && git pull --ff-only
bash ops/scripts/deploy-monitoring.sh --apply
```

The parts on `nf-hotel` (Alloy, the reports) are updated by CD.

## Checks in CI

The job "Ops monitoring configuration" runs `ops/tests/check-monitoring.sh` on every pull request. It uses the tools of the pinned images:

- `promtool check config`, and `promtool test rules` (`prometheus/rules-test.yml`);
- `amtool check-config`;
- `loki -verify-config` and `blackbox_exporter --config.check`;
- `alloy fmt`;
- a check that the dashboards are valid JSON.
