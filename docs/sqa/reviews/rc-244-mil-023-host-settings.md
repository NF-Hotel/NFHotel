# RC-244 sshd, sudo and sysctl settings on nf-hotel, and the CIS evidence run of 2026-10-28 (MIL-023 tasks 2 and 11, criteria 1 and 4)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-244 |
| CrossReference | [MIL-023], [CIS-001], [RC-191], [RC-192], [RC-193], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-28 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/scripts/install-host-settings.sh` with `ops/deploy/host/ssh/nfhotel.conf`, `sudo/nfhotel-logging`, `logrotate/nfhotel-sudo` and `sysctl/60-nfhotel.conf`, and its tests in `ops/tests/test-host-setup.sh`;
  - the fix of the AppArmor counts in `ops/scripts/cis-evidence.sh`;
  - `cap_drop` and `pids_limit` for the Alloy stack (`ops/deploy/alloy/docker-compose.yml`);
  - the CIS-001 rows that were Pending VM, against the evidence run of 2026-10-28.
- **Checklists used:** [QC-SH-001], [QC-YML-001], [QC-WF-001].
- **Who did what:** S01 asked in chat on 2026-10-28 to finish MIL-023 ("gør denne færdig"). The AI ran the script as root over Tailscale SSH, from commit `3c60823` of the branch `mil-023-host-settings`, copied to `/root/host-settings-3c60823` on the VM, as RC-191 did with its fix.
- **S01 decisions in chat, 2026-10-28:** the GNOME desktop on the VM stays for now ("behold det som det er for nuværende"); S01 removes the router forwards 50023 and 50024 later.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Times:** UTC.

## Baseline (02:47)

`ops/scripts/cis-evidence.sh` at release `2d767d7`, `/root/cis-evidence-20261028.txt` (exit 0). Findings against the Pending VM rows:

- **sshd accepted passwords** (`passwordauthentication yes`, `maxauthtries 6`, `logingracetime 120`, `disableforwarding no`, `permitrootlogin without-password`), and six personal accounts have a password. sshd was not reachable: Tailscale SSH serves port 22 of the tailnet address, and the firewall drops 22 on every other interface (RC-192). Row 5.3.x rested on `passwordauthentication no`, which did not hold.
- **sudo** had `use_pty` and no log file.
- **sysctl** had the Debian defaults: redirects sent and accepted, no martian logging, `ptrace_scope 0`.
- **The VM runs the GNOME desktop:** `gdm`, `cups`, `cups-browsed`, `avahi-daemon`, colord, ModemManager, fwupd and more, with LibreOffice and Firefox.
- **The Alloy stack** lacks `pids_limit` on both services and `cap_drop` on the socket proxy.
- One unowned file and 222 images (63 % reclaimable). Docker 29.8.0 with 29.9.2 available, and a kernel waiting for a reboot.

## The change on nf-hotel

| Time | Step | Result |
| --- | --- | --- |
| 03:00:5x | Dry run of `install-host-settings.sh` | Four files to install, `systemctl reload ssh`, `sysctl --load` |
| 03:00:58 | `--apply` | `sshd -t` accepted; sshd reloaded and active; `visudo -cq` passes |
| 03:01 | `sshd -T` | `permitrootlogin no`, `passwordauthentication no`, `maxauthtries 4`, `disableforwarding yes`; MACs without SHA-1 and 64-bit |
| 03:01 | New Tailscale SSH session as root | Works; `sudo -n true` is logged in `/var/log/sudo.log` |
| 03:01 | `tailscale ssh deploy@nf-hotel sudo -n -l` (the CD path) | `(root) NOPASSWD: /usr/local/sbin/nfhotel-deploy` |
| 03:01 | Site | Tailnet `https://100.72.194.127/` 200; public `https://nfhotel.devmatiker.com/` 200 |
| 03:01 | Containers | 10 of 11 `healthy`; the socket proxy has no health check and is up |

`cis-evidence.sh` after the change: `/root/cis-evidence-after-host-settings.txt` (exit 0). It shows the sysctl values of `60-nfhotel.conf`, `Defaults logfile="/var/log/sudo.log"`, and the three files `root:root` with modes 440, 644 and 644.

Undo: the wiki page Host-Hardening, step 6.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The script is tested | Pass | `test-host-setup.sh` in Debian 13: 32 passed, 16 of them new. They cover the dry run, the sshd, sudo and sysctl settings, no weak algorithm, a second run that changes nothing, and a refused sshd configuration that is taken back |
| SH 1–4, 12 | Lint | Pass | `ops/tests/lint.sh` clean (layout, actionlint, shellcheck, shfmt, yamllint) |
| SH 7 | Safe by default | Pass | Dry run without `--apply`; `sshd -t` before the reload; `visudo -c` before the sudo rule is installed |
| YML | Alloy compose | Pass | `docker compose config` passes. The socket proxy with `cap_drop ALL` and a PID limit was tested on Docker: GET of containers and networks 200, POST 403 |
| WF 22 | Verified on GitHub | Not yet | Closes with the green Ops CI of this pull request |
| MIL-023 1 | Every selected CIS item has a status and evidence | Partly | CIS-001 now: Docker 44 Applied, 2 Pending VM, 6 Not applicable, 6 Deferred; Debian 24 Applied, 6 Not applicable, 14 Deferred. 5.3 and 5.28 are Applied once the release with the Alloy limits runs |
| MIL-023 4 | Security updates by unattended-upgrades; container logs size-limited | Pass | The log shows the runs of 2026-10-24 15:53 and 2026-10-25 06:06 ("All upgrades installed"), and a run every day since; all 11 containers log with `local`, 5 files of 10 MB |
| MIL-023 8 | Logs 14 days, metrics 30 days, searchable in Grafana | Partly | Loki holds the logs of all 11 containers (label `container`), retention 14 days; Prometheus retention `30d`. The data starts on 2026-10-23, so the full 14 days can be read from 2026-11-06 |

## Findings

- **Accounts for S01 to review, not changed here:**
  - `/etc/sudoers.d/claudeops` gives the account `claudeops` `NOPASSWD: ALL`;
  - six personal accounts have a password, and `nfadmin` and `michael` are in the group `sudo`.

  With sshd on keys only, these passwords serve the Proxmox console only. Whether `claudeops` is still needed is S01's decision.
- **`/opt/nfhotel-legacy`** holds an old connection string in a file with no group, left from the deployment before MIL-021. The AI's `chgrp root` on it was refused by the safety check of the AI's tool, so it is left for S01. CIS 7.1.11 and 7.1.12 stay Deferred.
- **The desktop stays** by S01's decision (CIS 2.1.x and 2.2.x Deferred). The firewall keeps its services off the network.
- **A monthly window is due.** It installs Docker 29.9.2 and Tailscale 1.104.2, reboots into kernel 6.12.113, and prunes the images (CIS 1.2.2, 6.1 and 6.2).

## Overall Verdict

Go-with-conditions. QC-WF-001 criterion 22 closes with the green Ops CI of this pull request. MIL-023 criteria 1 and 8 close at the gate:

- criterion 1, when the Alloy limits run on `nf-hotel`;
- criterion 8, from 2026-11-06.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Check CIS 5.3 and 5.28 on `nf-hotel` after the release of this change | AI | MIL-023 gate |
| 2 | Remove the router forwards 50023 and 50024, then scan the public address from outside the home network | S01 | MIL-023 gate |
| 3 | Restore drill no. 2 (MIL-023 task 6) | S03 | 2026-11-20 |
| 4 | Decide on `claudeops` and on `/opt/nfhotel-legacy`; `chgrp root` on its secret file until then | S01 | MIL-023 gate |
| 5 | Plan the monthly window: Docker and Tailscale update, reboot, image prune | S01 | November 2026 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[RC-191]: rc-191-host-hardening-apply-part-1.md
[RC-192]: rc-192-firewall-tailscale-only.md
[RC-193]: rc-193-docker-daemon-applied.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
