# Host Hardening

How the NF Hotel VM `nf-hotel` (Debian 13) and its containers are hardened along the selected CIS Benchmark Level 1 items (MIL-023 tasks 1 to 4). The selection, with the status and evidence of each item, is the CIS checklist `docs/sqa/cis-checklist.md` (CIS-001). The host files are in `ops/deploy/host/` and each has a script that is a dry run by default.

Applying any step on the VM is the decision of S01, with S03 for the firewall and the reboots (MIL-023, Ownership). Nothing here runs by itself.

## What changes

| Area | Change | Script | Outage |
| --- | --- | --- | --- |
| Containers | Every service: `cap_drop: [ALL]` and only the capabilities it needs, `no-new-privileges`, a read-only root filesystem with `tmpfs` for what it writes, memory, CPU and PID limits; nginx gets a health check | The next deploy of a release (`deploy/*/docker-compose.yml`) | Each container is recreated once |
| Docker daemon | `/etc/docker/daemon.json`: `live-restore`, `no-new-privileges`, the `local` log driver (5 files of 10 MB per container), `userland-proxy: false`, `icc: false` | `ops/scripts/install-docker-daemon.sh` | One Docker restart |
| Firewall | nftables table `inet nfhotel`: input dropped by default, 80 and 443 open, 22 only on `tailscale0`; from outside, only the published 80 and 443 reach a container | `ops/scripts/apply-firewall.sh` | None; reverted by itself unless confirmed |
| Patching | `unattended-upgrades`: Debian security updates every day, no automatic reboot | `ops/scripts/install-patching.sh` | None |
| Logs | A persistent journal of at most 1 GB; container logs limited by the daemon | `ops/scripts/install-patching.sh`, `ops/scripts/install-docker-daemon.sh` | None |
| sshd, sudo, kernel | sshd takes keys only, no root login and no forwarding, with strong algorithms; sudo logs to `/var/log/sudo.log`; no ICMP redirects or source routing, martians logged, ptrace restricted | `ops/scripts/install-host-settings.sh` | None |
| Desktop | The GNOME desktop of the installer is purged with its print, discovery and other services; the packages that the VM needs are marked as installed by hand; the VM starts `multi-user.target` (MIL-068, [Without a desktop](#without-a-desktop)) | `ops/scripts/remove-desktop.sh` | One restart |

What each container needs, measured with the throwaway stack:

| Service | Capabilities added after `ALL` is dropped | `tmpfs` | Memory, CPUs, PIDs |
| --- | --- | --- | --- |
| nginx | `CHOWN`, `SETGID`, `SETUID`, `NET_BIND_SERVICE`, `DAC_READ_SEARCH` (reads the 0600 key of the checkout user) | `/etc/nginx/conf.d`, `/var/cache/nginx`, `/run`, `/tmp` | 256 MB, 1, 256 |
| nfhotel-web, nfhotel-api | none | `/tmp` | 1 GB, 2, 512 |
| postgres | `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `SETGID`, `SETUID` | `/var/run/postgresql`, `/tmp` | 2 GB, 2, 512 |
| ollama | none | `/tmp` | 8 GB, 4, 1024 |
| market-analysis API | none | `/tmp`, `/home/app` | 2 GB, 2, 256 |
| benchmark | none | `/tmp`, `/home/app` | 1 GB, 1, 256 |

The limits are first estimates, well above what the stack used in the tests (nginx 19 MB, web 42 MB, API 130 MB, PostgreSQL 40 MB with empty data). Check them on the VM with `docker stats --no-stream` a week after the deploy and change them by pull request.

With a read-only root filesystem, ASP.NET Core keeps its Data Protection keys in memory. Before, they lived in the container's writable layer, which a deploy already threw away. Now a restart of the container also renews them, so a staff member signs in again after a restart, as after a deploy.

## Apply steps on the VM

Run them as root in `/opt/nfhotel`, at the DevOps commit that holds this change, logged in over Tailscale (`ssh root@nf-hotel`). Keep the output of each step for the review record.

1. **Baseline.** These change nothing.

   ```bash
   bash ops/scripts/cis-evidence.sh > /root/cis-evidence-before.txt
   ```

   From a device outside the tailnet, scan the public address of the VM, and from the tailnet run the exposure check:

   ```bash
   nmap -Pn -p- <public address of nf-hotel>
   ```

   ```bash
   bash ops/scripts/check-tailnet-exposure.sh --host nf-hotel
   ```

2. **Patching and logs.** No outage.

   ```bash
   bash ops/scripts/install-patching.sh
   ```

   Read the dry run, then:

   ```bash
   bash ops/scripts/install-patching.sh --apply
   ```

   Undo: `rm /etc/apt/apt.conf.d/52nfhotel-unattended-upgrades /etc/systemd/journald.conf.d/nfhotel.conf` and `systemctl restart systemd-journald`.
3. **Firewall**, by S03. Check the dry run first; it warns when Debian's own `nftables.service` is enabled, whose `flush ruleset` would also remove Docker's rules, and when the session does not come over Tailscale.

   ```bash
   bash ops/scripts/apply-firewall.sh
   ```

   ```bash
   bash ops/scripts/apply-firewall.sh --apply
   ```

   Within two minutes, open a **new** ssh session over Tailscale and confirm. If it cannot log in, wait: the old rules come back by themselves.

   ```bash
   bash ops/scripts/apply-firewall.sh --confirm
   ```

   Then repeat the scan from outside the tailnet: only 80 and 443 answer. Undo: `nft delete table inet nfhotel` and `systemctl disable nfhotel-firewall.service`.
4. **Docker daemon**, in a maintenance window: the restart stops every container once, and they come back by their restart policy.

   ```bash
   bash ops/scripts/install-docker-daemon.sh
   ```

   ```bash
   bash ops/scripts/install-docker-daemon.sh --apply --restart
   ```

   Then check that the firewall survives a Docker restart (MIL-023 criterion 2): `nft list table inet nfhotel` still shows the table, and the scan from outside still shows only 80 and 443. Undo: copy the `daemon.json.<time>.bak` that the script printed back, and `systemctl restart docker`.
5. **Containers.** Deploy the release that holds the hardened compose files through CD (`NF Hotel CD`, with the approval). Compose recreates every container, now with the hardening and the size-limited log driver. Check:

   ```bash
   docker ps --format '{{.Names}} {{.Status}}'
   ```

   Every service is `healthy`. Undo: roll back to the previous release (`nfhotel-deploy rollback`).
6. **sshd, sudo and sysctl.** No outage. The administrators and CD come in by Tailscale SSH, which this does not touch; sshd is the fallback, and after this it takes keys only and no root login.

   ```bash
   bash ops/scripts/install-host-settings.sh
   ```

   ```bash
   bash ops/scripts/install-host-settings.sh --apply
   ```

   The script checks the sshd configuration with `sshd -t` before the reload, and takes it back if sshd refuses it. Undo: `rm /etc/ssh/sshd_config.d/nfhotel.conf /etc/sudoers.d/nfhotel-logging /etc/logrotate.d/nfhotel-sudo /etc/sysctl.d/60-nfhotel.conf`, `systemctl reload ssh` and `sysctl --system`.
7. **Evidence.**

   ```bash
   bash ops/scripts/cis-evidence.sh > /root/cis-evidence-after.txt
   ```

   Fill the column "Verified on nf-hotel" of CIS-001 from it, and record the run in an RC.

A unit that fails to start after step 5 most likely needs one capability more. The log names it (`Operation not permitted`). Add it to `cap_add` of that service by pull request, never `privileged`. If nginx cannot read the private key, check its owner: the master process reads it as root without `DAC_OVERRIDE`, so the key must belong to root.

## Without a desktop

The VM was installed with the GNOME desktop of the Debian installer (`task-desktop` and `task-gnome-desktop`). MIL-068 removed it on 2026-11-08, so that the VM runs as a server: no display manager, no print, discovery, office or other desktop programs, and `multi-user.target` as the default target. The run purged the 1,092 packages of its dry run, no more and no fewer. After the restart only `sshd`, `tailscaled` and Docker listen, and the VM has 510 packages instead of 1,602 and uses about 1.1 GB of memory instead of 2.1 GB. Rows 2.1.x and 2.2.x of the CIS checklist are Applied. Row 2.1.x has one exception: the `nginx` package of the host, installed by hand, whose unit is disabled and does not run (nginx runs in its container).

`ops/scripts/remove-desktop.sh` does it. Its two lists come from a read-only survey of the VM on 2026-11-08:

- **Purged:** the two tasks, GNOME and `gdm3`, LibreOffice, CUPS, Avahi, ModemManager, Bluetooth, PackageKit, `colord`, `fwupd`, `ibus`, `inetutils-telnet`, the other desktop services (`accounts-daemon`, `udisks2`, `upower`, `power-profiles-daemon`, `switcheroo-control`, `low-memory-monitor`, `wpa_supplicant`) and programs (Firefox, GNOME Terminal, `xterm`, PipeWire, Plymouth), and what nothing else needs any more. The dry run of 2026-11-08 would purge 1,092 packages, about 3.1 GB.
- **Kept**, and marked as installed by hand, so that no later `apt autoremove` takes them: `sudo`, `openssh-server`, `openssh-client`, `network-manager`, `tailscale`, Docker's five packages, `nftables`, `qemu-guest-agent`, `unattended-upgrades`, `age`, `rsync`, `jq`, `curl`, `ca-certificates`, `git`, `logrotate`, `iproute2`, `util-linux`, `diffutils`, `procps`, `openssl`, `apparmor`, `systemd-timesyncd` and `locales-all`: what MIL-068 names, and what a script of `ops/` or a unit on the VM uses.

The mark is what makes the purge safe. `sudo`, NetworkManager and `jq` came in as dependencies of the desktop tasks, so `apt-get purge --auto-remove` of the tasks alone would take the `sudo` of the deploy account and the network of `ens18` along. NetworkManager stays and keeps `ens18`. `locales-all` stays because it holds the locale of the VM, `en_US.UTF-8`, which is not generated in the locale archive.

Run it as root, first as a dry run. It marks the packages to keep in a copy of the apt state only, lists what the purge would take, changes nothing, and refuses when a package to keep would go:

```bash
sudo bash /opt/nfhotel/ops/scripts/remove-desktop.sh
```

Take a Proxmox snapshot of the VM first. Then purge in a transient unit, which goes on when the SSH session drops (the output is in `journalctl -u nfhotel-remove-desktop`), and restart:

```bash
sudo systemd-run --unit=nfhotel-remove-desktop --collect --wait bash /opt/nfhotel/ops/scripts/remove-desktop.sh --apply
```

```bash
sudo systemctl reboot
```

Check after the restart:

```bash
systemctl get-default
dpkg-query -W -f='${db:Status-Abbrev} ${Package}\n' task-desktop task-gnome-desktop gdm3 gnome-shell libreoffice-core cups avahi-daemon modemmanager bluez packagekit inetutils-telnet 2>/dev/null | grep '^ii'
sudo ss -lntup
```

`systemctl get-default` gives `multi-user.target`, the `dpkg-query` line prints nothing, and `ss` shows only `sshd`, `tailscaled` and the ports of Docker. Then the checks of step 5 apply, and `sudo -l -U deploy` still lists `nfhotel-deploy`. When a check fails, roll back to the snapshot from the Proxmox console.

## No secret in the log of sudo

`sudo` writes a line for every command to `/var/log/sudo.log` and to the journal (row 5.2.3 of the CIS checklist), and Alloy sends the journal to Loki. The line names the user, the folder and the command with its arguments, and its `ENV=` field holds every variable that `sudo` keeps: those of `--preserve-env`, those of `-E`, and `NAME=value` before the command. Whatever crosses `sudo` that way can be read by everyone who reads these logs.

Until MIL-070, `deploy-release.sh` handed the connection string of the API to `deploy-services.sh` with `--preserve-env`, so every deploy from 2026-10-21 to 2026-11-08 wrote the password of `nfhotel_app` into the log. Now `as_app` hands it on standard input to the shell that `sudo` starts, which puts it in the environment of the command and runs the command with nothing on its standard input (ADR-0041). Only `NFHOTEL_API_MIGRATE_ON_START`, a flag, still crosses in the environment. `sudo` stays for the change of user, so the line of every command is still written.

`ops/tests/test-sudo-log.sh` proves it in Debian 13 with the `sudo` settings of the VM, and refuses a `sudo` call in `ops/` that keeps a variable outside its short list of names that hold no secret. Never pass a secret to `sudo` in its environment or as an argument; pass it on standard input, as `as_app` does.

The entries written before MIL-070 stay until their retention ends: the journal keeps at most 1 GB, `sudo.log` is rotated weekly and keeps 13 old files, and Loki keeps 14 days. On 2026-11-08 `nfhotel_app` got a new password (task 5 of MIL-070), so the passwords in those entries open nothing.

To check the log for a secret, count by its value, never by its name, and print no line. The text of the shell that `as_app` starts names `NFHOTEL_API_CONNECTION_STRING`, so every call adds a line with the name and without the value. Count the form `NFHOTEL_API_CONNECTION_STRING=Host=`, the field `Password=`, or the password itself with `grep -c -F -f` and the password file.

## Firewall: SSH over Tailscale only

**Decision (S01, 2026-10-24).** SSH (TCP 22) is allowed only on `tailscale0`. SSH from the LAN (`192.168.10.0/24`) and from the internet is dropped, and the input policy stays drop. The reason: the server stands in S01's home, so the Proxmox console is the way in when Tailscale is down, and SSH from the LAN adds an access path without a need. This is the least privilege that MIL-023 criterion 2 asks for.

**State on nf-hotel since 2026-10-24 15:37 UTC** (RC-192):
- The ruleset is `ops/deploy/host/nftables/nfhotel.nft`, installed as `/etc/nfhotel/nftables.nft`.
- `nfhotel-firewall.service` from `ops/deploy/systemd/` loads it at boot, before the network and Docker.
- It replaced a ruleset set up by hand during MIL-021 (`/etc/nftables.d/nfhotel.nft`), which also allowed SSH from the LAN and had a unit whose `systemctl stop` removed the whole table.
- The backup of the old state is in `/root/firewall-backup-20261024T153330Z/` on the VM:
  - `ruleset.nft`, the complete ruleset;
  - `table-inet-nfhotel.nft`;
  - `nftables.d/`;
  - the old `nfhotel-firewall.service`.

**Check it** (read only), as root on the VM:

```bash
nft list chain inet nfhotel input
```

`tcp dport 22` appears only with `iifname "tailscale0"`, and no rule names `192.168.10.0/24`.

### Rollback

From an SSH session over Tailscale or from the Proxmox console, as root:

1. Load the old table again, and keep it across reboots with the old unit:
   ```bash
   b=/root/firewall-backup-20261024T153330Z
   nft -f "$b/nftables.d/nfhotel.nft"
   cp "$b/nfhotel-firewall.service" /etc/systemd/system/nfhotel-firewall.service
   systemctl daemon-reload
   ```
2. Record why in an RC. The planned state is the ruleset in git, so a rollback is temporary until a pull request changes it.

To take a change back while it is still pending, use `bash ops/scripts/apply-firewall.sh --revert`. Without a confirmation, a pending change reverts by itself.

### Recovery when Tailscale is down

The firewall then lets no SSH in from anywhere. Use the console:

1. In the Proxmox web interface, open VM 100 (`nf-hotel`), then *Console*, and sign in as `nfadmin`.
2. Bring Tailscale back: `sudo systemctl restart tailscaled`, then `tailscale status`.
3. Only if SSH is needed before Tailscale works, remove the table for the session. This opens the VM until the next boot or `systemctl restart nfhotel-firewall.service`:
   ```bash
   sudo nft delete table inet nfhotel
   ```

## Monthly image rebuild

DSA-001 risk R11 asks for images that are rebuilt with the latest security fixes, even when no code changes. Plan, from the first working day of December 2026, each month:

1. **Base images.** Merge the open Dependabot pull requests that move the pinned digests (`Images:` in this repository: nginx, PostgreSQL 16, Ollama and the benchmark base). The .NET images of NFHotel (`mcr.microsoft.com/dotnet/aspnet:10.0`) follow a tag, so a rebuild takes the latest patch of .NET 10 by itself.
2. **Rebuild.** Run `NF Hotel Release` on the head of `main` of NF-Hotel/NFHotel. Today it starts only after `NF Hotel CI` on `main`, so a rebuild needs a merge to `main`. A `workflow_dispatch` and a monthly `schedule` trigger on `release.yaml` in NFHotel would remove that need (follow-up in NF-Hotel/NFHotel). The market-analysis image is rebuilt too, as a patch layer on its frozen base (ADR-0030).
3. **Gate.** The scan gate of MIL-022 blocks a release with a critical finding, or a high one with a fix, so a rebuild that still has one is fixed before the deploy.
4. **Deploy** through CD with the approval, as any release.
5. **Clean up** on the VM: `docker image prune --all --filter until=720h`. The deploy log keeps the digests of the previous release, so a rollback pulls it again if it is gone.
6. **Record** the release in the monthly DORA report (MIL-023 task 5).

## Reboots

`unattended-upgrades` never reboots the VM. A kernel, libc or systemd update writes `/var/run/reboot-required`; `cis-evidence.sh` shows it. S03 reboots in the monthly window above, after the deploy, or within a week when the update fixes a critical issue:

```bash
cat /var/run/reboot-required.pkgs
```

```bash
systemctl reboot
```

After the reboot, `nfhotel-firewall.service` loads the firewall before the network, Docker starts the containers, and the checks of step 5 apply. Docker and Tailscale come from their own repositories (`apt-cache policy` in the evidence shows the origin), which the security origin does not cover: update them in the same window with `apt-get install --only-upgrade docker-ce docker-ce-cli containerd.io docker-compose-plugin docker-buildx-plugin tailscale`, then check that `tailscale status` shows the VM online.
