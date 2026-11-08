# MIL-068 The production VM without a desktop

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-068 |
| CrossReference | [BC-001], [CIS-001], [DSA-001], [ADR-0010], [MIL-023], [MIL-066], [RC-313] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version: the desktop that came with the operating system of the VM, found again at the gate of MIL-066 (RC-313 finding 6), with the survey of the VM of 2026-11-08; planned at S01's request in chat ("ja til alle", to "Skal jeg lave en plan for at fjerne skrivebordsmiljøet fra VM'en?") | [7ce3ef7] |

---

## Purpose

Decide that the production virtual machine (VM) runs as a server: it starts without a graphical desktop and runs no print, discovery, office or other desktop software, while its network, Tailscale, Docker, the firewall, the deploy account and the backups keep working.

On 2026-10-28 S01 decided to keep the GNOME desktop that came with the operating system for the time being ([CIS-001], row 2.1.x): the firewall drops every input but 80, 443, Tailscale and Secure Shell (SSH) on `tailscale0`, so the network reaches none of its services. The gate of [MIL-066] found it again ([RC-313], finding 6), and on 2026-11-08 S01 asked for this plan. A read-only survey of the VM the same day (17:48 to 17:53 UTC, nothing changed) gave these findings:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The VM starts `graphical.target` with the tasks `task-desktop` and `task-gnome-desktop` of the Debian installer: `gdm3` shows a login screen on the console, and `gnome-shell`, `mutter`, `Xwayland`, `ibus` and `fwupd` run, about 570 MB of memory together | The VM (Debian 13) | Software that the hotel does not use runs on the machine that holds the customer data, and has to be patched |
| 2 | Services outside the architecture run: `cupsd` and `cups-browsed` (print; `cupsd` on the loopback), `avahi-daemon` (User Datagram Protocol port 5353, on every address), ModemManager, `wpa_supplicant`, `colord`, PackageKit, `power-profiles-daemon`, `accounts-daemon`, `udisks2` and `switcheroo-control`; Bluetooth is enabled | The VM | The firewall keeps the network from them today; each one still runs, and a mistake in the firewall would expose it |
| 3 | 32 security updates, published at 17:39 UTC, wait for the next run of `unattended-upgrades` (2026-11-09, 06:28 CET); 29 of them are LibreOffice and its runtime, which is there only because of the desktop task | The VM | Most of the patching of the VM is for software nobody uses |
| 4 | Purging the two tasks with `--auto-remove`, as row 2.1.x of [CIS-001] proposes, would purge 12 packages, `sudo` among them, and leave GNOME installed (`apt-get -s`) | The package marks of the VM | The deploy account may run only `nfhotel-deploy`, through `sudo` (`install-deploy-user.sh`), so the next continuous deployment (CD) run would fail, and the desktop would stay |
| 5 | NetworkManager manages `ens18` (one connection, with an address leased from the local network); `/etc/network/interfaces` holds only the loopback, and `systemd-networkd` is off | The network of the VM | Removing NetworkManager with the desktop takes the VM off the local network and the tailnet; only the Proxmox console would reach it |

The survey followed the update of Tailscale (1.102.4 to 1.104.5) and of `docker-buildx-plugin` (0.37.1 to 0.38.1) on the VM, made the same evening at S01's request (an action item of [RC-313]): `tailscaled` came back online with `tag:nfhotel-vm`, the 11 containers kept running, the firewall stayed active, and the smoke test gave 301 and 200. The update command of the wiki page Host-Hardening still names four of Docker's packages and not Tailscale; task 3 corrects it.

## Deliverable

- **The removal list:** the packages to purge (the two tasks, GNOME, `gdm3`, LibreOffice, CUPS, Avahi, ModemManager, Bluetooth, PackageKit, `colord`, `fwupd`, `ibus`, `inetutils-telnet` and the rest of the desktop), and the packages to keep: `sudo`, `openssh-server`, `network-manager`, `tailscale`, Docker's five packages, `nftables`, `qemu-guest-agent`, `unattended-upgrades`, `age`, `rsync`, `jq`, `curl` and `ca-certificates`, and any other package that a script of `ops/` or a unit on the VM needs.
- **A script `ops/scripts/remove-desktop.sh`**, a dry run by default: it marks the packages to keep as installed by hand, so that no removal takes them along, simulates the purge, and refuses when a package to keep would go; with `--apply` it purges the list, removes what nothing needs any more, and sets `multi-user.target` as the default.
- **NetworkManager stays** and keeps `ens18`. Moving the network to `ifupdown` is not needed for the decision and would add the one change that can take the VM off the network.
- **The wiki page Host-Hardening** says that the VM has no desktop, how it was removed and how to check it, and its update command names Docker's five packages and Tailscale. **Rows 2.1.x and 2.2.x of [CIS-001]** become Applied, with the evidence; row 2.2.x already says that `inetutils-telnet` came with the desktop and goes with it.
- **The proof:** the test of the script, the code review, a Proxmox snapshot before the change, the run on the VM, one restart, and the checks after it.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch, task 4 with its pull request, task 5 on the VM, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The dry run of `remove-desktop.sh` on the VM lists the packages it would purge and none of the packages to keep; the review record keeps the list | Yes | No |
| 2 | `ops/tests/test-remove-desktop.sh` proves in `debian:13-slim`, with a few of the listed packages installed, that the dry run changes nothing, that `--apply` purges them and keeps the packages to keep, and that the script refuses when a package to keep would be purged | Yes | No |
| 3 | After the run and one restart of the VM: `systemctl get-default` is `multi-user.target`; none of `task-desktop`, `task-gnome-desktop`, `gdm3`, `gnome-shell`, `libreoffice-core`, `cups`, `avahi-daemon`, `modemmanager`, `bluez`, `packagekit` and `inetutils-telnet` is installed; `ss -lntup` shows only `sshd`, `tailscaled` and the ports of Docker | Yes | No |
| 4 | After the restart: `ens18` has its address from NetworkManager, Tailscale is online with `tag:nfhotel-vm`, the 11 containers are healthy, `nfhotel-firewall.service` is active with the table `inet nfhotel`, the smoke test gives 301 and 200, `sudo -l -U deploy` lists `nfhotel-deploy`, and one run of `nfhotel-backup.service` copies to the backup target | Yes | No |
| 5 | The wiki page Host-Hardening and rows 2.1.x and 2.2.x of [CIS-001] say what criterion 3 shows, the update command names Docker's five packages and Tailscale, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` (and `QC-WF-001` when a workflow changes) is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-066 | The gate that found the desktop again ([RC-313] finding 6), and the backup copy that criterion 4 checks |
| MIL-023 | The hardening of the VM and the checklist of the Center for Internet Security (CIS) Benchmark, whose rows 2.1.x and 2.2.x this milestone closes |
| CIS-001 | Row 2.1.x holds S01's decision of 2026-10-28 to keep the desktop for the time being, which this plan replaces; row 2.2.x ties `inetutils-telnet` to the desktop |
| ADR-0010 | The production VM as deployed: Debian 13, its network and its accounts |
| DSA-001 | A finding is fixed, or accepted by S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25 and success criterion 28: hardened server (every selected CIS item has a status and evidence; security updates install unattended) | Criteria 3 and 5: rows 2.1.x and 2.2.x of CIS-001 Applied with their evidence, and far fewer packages to patch |
| BC-001 objective 23 and success criterion 26: release only through an approved pipeline | Criterion 4: the deploy account keeps its `sudo` rule, so CD still deploys |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-22, the date of the action item of RC-313; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The removal list | A read-only survey of the VM over Tailscale SSH: the installed packages of the two tasks and of the desktop, what each removal would take along (`apt-get -s`), and the packages to keep, among them every package that a script of `ops/` or a unit on the VM needs. The list goes into the script and into the review record. | No | criterion 1 |
| 2 | The script and its test | `ops/scripts/remove-desktop.sh` as in the deliverable, and `ops/tests/test-remove-desktop.sh` in `debian:13-slim`, run by Ops CI; the case that refuses a purge of a package to keep is written first. | No | criteria 1, 2 |
| 3 | The wiki and the CIS checklist | Host-Hardening: the VM has no desktop, how it was removed and how to check it; the update command with `docker-ce`, `docker-ce-cli`, `containerd.io`, `docker-compose-plugin`, `docker-buildx-plugin` and `tailscale`. Rows 2.1.x and 2.2.x of CIS-001 Applied, with the evidence of task 5. | No | criterion 5 |
| 4 | Code review and pull request | The review record of the script, its test and any workflow change against `QC-SH-001` (and `QC-WF-001`) before the pull request, which closes one issue per task it completes. | No | criterion 6 |
| 5 | The change on the VM | S01 takes a Proxmox snapshot of the VM and names a time for a short outage. The artificial intelligence (AI) assistant runs the script over Tailscale SSH, first as a dry run, then with `--apply` in a transient unit that outlives the session, and restarts the VM; then the checks of criteria 3 and 4. When a check fails, S01 rolls back to the snapshot from the Proxmox console. S01 deletes the snapshot after the gate. | No | criteria 3, 4 |
| 6 | Gate and review record | The gate review record against criteria 1 to 6 with the output of the dry run, the run and the checks, the row of MIL-068 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-066]: ./mil-066-findings-of-7-november.md
[RC-313]: ../sqa/reviews/rc-313-mil-066-gate.md
[7ce3ef7]: https://github.com/NF-Hotel/NFHotel/commit/7ce3ef7252f8780770d87bd2fa5c19c3f5ba9b99
