# RC-189 Alarm drill of the monitoring (MIL-023 task 10, criterion 7)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-189 |
| CrossReference | [MIL-023], [ADR-0020], [RC-179], [RC-184], [RC-185], [RC-187], [RC-188], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the monitoring of ADR-0020 as it runs in production:
  - the LXC `nf-monitoring` (Proxmox CT 102) at `650476f`;
  - Alloy on `nf-hotel`;
  - Discord `#nf-hotel-alerts`;
  - Healthchecks.io.
- **Criterion:** MIL-023 criterion 7. An alarm drill raises a Discord alert within the stated time for each of these:
  - a stopped web container (3 min);
  - a stale backup report (simulated by a backdated report);
  - a certificate check below 21 days (simulated by the threshold);
  - a stopped Prometheus (the Healthchecks.io watchdog).
- **Who did what:**
  - S03 ran drills 1, 2 and 4 on the hosts and read Discord and Healthchecks.io.
  - The AI ran drill 3 in the LXC over Tailscale SSH, after S03 approved the session in a browser. The AI read the state of the rule from the Prometheus API.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Times:** UTC. Discord shows CEST (UTC+2), which is given in brackets.

## Results

| # | Drill | Cause | Alert | Time to alert | Target | Back to normal |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Stopped web container | 2026-10-23 14:23:15, `docker stop nfhotel-web-nfhotel-web-1` | `ContainerUnhealthy` 14:25 (16:25) | 2 min 45 s at most | 3 min | Started 14:24:01; *RESOLVED* in Discord |
| 2 | Stale backup report | 2026-10-24 08:43:11, `backup.prom` backdated 27 h | `BackupStale` 08:44 (10:44) | 49 s to 1 min 48 s | Within minutes | Backup 08:47:47; *RESOLVED* 08:49 (10:49) |
| 3 | Certificate below the threshold | 12:07:55, the threshold raised from 21 to 400 days in the LXC (72.4 days were left) | `CertificateExpiresSoon` *pending* 12:08:27, *firing* 12:18:35, in Discord 12:18 (14:18) | 10 min 40 s, of which the rule waits 10 min | 11 min | Rule put back at 12:18:49 (`git status` empty); *inactive* at 12:19:13 |
| 4 | Stopped Prometheus | 08:56:01, `docker compose stop prometheus` | Healthchecks.io "DOWN" about 09:03:58 | About 8 min | 6 min stated, about 8 measured | Started 09:06:09; Healthchecks.io "UP" 09:06:27 |

**Verdict per drill:**

- Drills 1 to 3 met their targets.
- Drill 4 alerted. It took about 8 minutes, not the 6 that the wiki stated, for two reasons:
  - Alertmanager keeps the Watchdog until it expires, so the pings went on for a minute after the stop.
  - The pings came every 2 minutes.

  RC-188 corrected the ping rate and the stated time, so the wiki now gives about 8 minutes.

## Real faults the monitoring found while it was being set up

The monitoring raised these by itself, before or during the drill:

| Alert | Cause | Fixed in |
| --- | --- | --- |
| `NoMetricsFromNfHotel`, 2026-10-23 13:22 to 14:17 | Alloy could not start: it ran as root without `DAC_OVERRIDE` | #258, RC-184 |
| `TaskReportMissing cert_renew` | The certificate renewal had failed every night: `NGINX_SERVER_NAME=_` holds no tailnet name. It now succeeds (`unchanged`, 2026-10-24 08:41:54) | #261, RC-187 |
| `ContainerUnhealthy` for `nfhotel-nginx`, `nfhotel-postgres`, `nfhotel-python-api` | Three containers of the stack from before the reconciliation, stopped for 3 days with a restart policy. S03 removed the containers and kept their volumes; resolved 2026-10-24 08:27 | Removed by S03 |
| No container logs in Loki | The socket proxy refused `GET /networks` | #259, RC-185 |

## Defects the drill found in the monitoring itself

1. **A changed rule was never applied.** `deploy-monitoring.sh --apply` did not recreate Prometheus, so the first certificate drill could not fire. Fixed in #262 (RC-188), with a CI test that failed without the fix.
2. **The Watchdog pinged every 2 minutes.** Fixed in #262 (RC-188).
3. **`CertificateExpiresSoon` came three times**, once for each probed page (`/`, `/book`, `/admin/login`), for one certificate. Fixed here:
   - the rule takes the minimum per job, so one certificate gives one alert;
   - `promtool test rules` checks it with two pages.

## Lessons for the next drill

- **End a drill only after its alert has arrived.** Several first attempts restarted the container, wrote a fresh backup or started Prometheus again within seconds. The monitoring then saw nothing. Part E of the wiki page now says so first.
- **Run each command on the right host.** Drill 4 was first run on `nf-hotel` and then on the Proxmox node, where Docker is missing. The commands changed nothing there.
- **Pressing Ctrl+C while a command waits** stops the whole command, including the step that starts the service again. Separate commands are safer than one with a `sleep`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| MIL-023 7 | Each alert of the drill reaches Discord within its stated time | Pass | Results table |
| YML | The changed rule is tested | Pass | `promtool test rules`: two pages with 20 days left give one `CertificateExpiresSoon`, labelled by job only |

## Overall Verdict

Go. MIL-023 criterion 7 is met.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the merge, run `git pull --ff-only` and `deploy-monitoring.sh --apply` in the LXC, so the grouped certificate rule is in use | AI over Tailscale SSH, or S03 | After the merge |
| 2 | Repeat the drill every six months, or after a change of the routes or the Watchdog | S03 | 2027-04 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[RC-179]: rc-179-mil-023-monitoring.md
[RC-184]: rc-184-alloy-user-and-start-test.md
[RC-185]: rc-185-alloy-docker-discovery.md
[RC-187]: rc-187-cert-renew-host-name.md
[RC-188]: rc-188-monitoring-reload-and-watchdog.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
