# RC-193 Docker daemon settings on nf-hotel, and the firewall across a Docker restart (MIL-023 tasks 2 and 3, criterion 2)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-193 |
| CrossReference | [MIL-023], [CIS-001], [RC-176], [RC-191], [RC-192] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [a919da3] |

---

## Artifact Under Review

- **Instances reviewed:**
  - step 4 of the wiki page Host-Hardening on `nf-hotel`: `ops/deploy/host/docker/daemon.json`, installed by `ops/scripts/install-docker-daemon.sh --apply --restart` at release `65e28de`;
  - the CIS-001 rows that it covers.
- **Who did what:** S01 asked in chat on 2026-10-24 for the step to run at once. The AI ran it as root over Tailscale SSH.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Times:** UTC.

## The change

| Time | Step | Result |
| --- | --- | --- |
| 15:56:30 | Dry run | The planned `daemon.json`; no file existed before, so nothing was overwritten |
| 15:56:44 | `--apply --restart` | `dockerd --validate` accepted the file; Docker restarted |
| 15:56:49 | Containers | 10 of 10 up, every one with a health check `healthy`; the outage lasted about 15 to 20 seconds |

`docker info` after the restart: `live-restore=true`, `log-driver=local`, `security` with `no-new-privileges`, `experimental=false`. `/etc/docker/daemon.json` is `root:root 0644`.

## Checks after the restart

- **Site over the tailnet:** `/` 200, `/book` 200, HTTP 301.
- **Published ports without the userland proxy:**
  - on the VM, `https://100.72.194.127/` answered 200 and the internal analysis route `http://127.0.0.1:8081/` answered 200;
  - the firewall logged no dropped forwarded packet.
- **Firewall across the Docker restart (MIL-023 criterion 2):**
  - the table `inet nfhotel` is still loaded with its `docker-user` rule;
  - from the LAN, 22, 80 and 443 are closed;
  - over the tailnet, the site answers.
- **Monitoring:** the four probes are `1`, and only the Watchdog fires.
- **Evidence:** `/root/cis-evidence-after.txt` (exit 0).

## Finding

CIS 1.1.2 expects the group `docker` to be empty, because membership equals root on the host. On `nf-hotel` it holds the user `nfhotel`, the owner of the checkout that the deploy runs as. Removing it needs a check of what runs as `nfhotel` and calls Docker without sudo, for example `deploy-services.sh` through the deploy command, so it is left as an open action. The row stays Pending VM, with the finding.

## Not covered

- **The scan from outside the tailnet.** The VM has no public port forward, and its LAN address answers on no TCP port. A scan from the internet needs a device outside the home network.
- **The size limit of the logs.** It applies to containers that are created again; the next deploy recreates them with the `local` driver.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| MIL-023 2 | The firewall holds after a Docker restart | Pass | Table and rule present after the restart; LAN closed; site open over the tailnet |
| MIL-023 3 | `no-new-privileges` for every service, and all services healthy | Pass | 10 of 10 containers |
| CIS-001 | Rows 2.2 to 2.6, 2.14 to 2.16, 2.18, 3.17, 3.18, 5.25 and 6.1.1.x | Applied | `/root/cis-evidence-after.txt` |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Decide on the user `nfhotel` in the group `docker` (CIS 1.1.2): find what needs it, and move it to sudo for the deploy command | S01, with the AI | MIL-023 gate |
| 2 | Scan the public side of the home network from outside | S01 | MIL-023 gate |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[RC-176]: rc-176-mil-023-host-hardening.md
[RC-191]: rc-191-host-hardening-apply-part-1.md
[RC-192]: rc-192-firewall-tailscale-only.md
[a919da3]: https://github.com/NF-Hotel/NFHotel/commit/a919da31b9a9ca2296643766efc3c651d28af4fa
