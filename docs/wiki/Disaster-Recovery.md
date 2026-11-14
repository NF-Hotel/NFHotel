# Disaster Recovery

This page rebuilds NF Hotel after its VM `nf-hotel` or its monitoring LXC is lost: from nothing, using the scripts of the repository and the backups on the legion laptop.

**For:** S03 (the owner of the Proxmox node and of the age key) with one member of S01.
**Decision:** [ADR-0025](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0025-recovery-objectives.md). **Work:** MIL-037.

**The objectives:** the site answers again within **4 hours** (RTO), with at most the last **24 hours** of bookings lost (RPO), when the Proxmox node works. A lost node needs new hardware first.

**Contents**
1. [Before a disaster: keep these ready](#before-a-disaster-keep-these-ready)
2. [How long the backup target keeps the copies](#how-long-the-backup-target-keeps-the-copies)
3. [The copy every hour and the backup target](#the-copy-every-hour-and-the-backup-target)
4. [Every backup is read back](#every-backup-is-read-back)
5. [Rebuild the VM](#rebuild-the-vm)
6. [Rebuild the monitoring LXC](#rebuild-the-monitoring-lxc)
7. [The drill](#the-drill)
8. [Why it is done this way](#why-it-is-done-this-way)

---

## Before a disaster: keep these ready

| What | Where | Why |
| --- | --- | --- |
| The private age key of S03 | Offline, in two places (for example a USB stick at home and a printout elsewhere) | Every backup is encrypted to it; without it no backup can be read |
| The Proxmox root login | S03 | Creating a VM needs it |
| The backups | The legion laptop, folder of the backup target (ADR-0008) | The VM's own copies are lost with the VM |
| Admin of the tailnet and of the GitHub organization | S01 | The new VM must take the name `nf-hotel` and the CD must reach it |

Check once a month that the newest `nfhotel-daily-*.dump.age` and `nfhotel-settings-daily-*.tar.age` on the laptop are less than two days old.

## How long the backup target keeps the copies

The key of the VM may only add files to the backup target (`rrsync -wo -no-del -no-overwrite`), so the VM cannot delete there, not even its own old copies. The service `prune` beside the target deletes them instead (MIL-069), at its start and then once a day:

- A daily file (`nfhotel-daily-*.dump.age`, `nfhotel-settings-daily-*.tar.age`) is kept for 30 days, and a weekly file (`nfhotel-weekly-*.dump.age`, `nfhotel-settings-weekly-*.tar.age`) for 12 weeks, counted from the time in its name, not from the date of the file.
- The newest 14 daily and 8 weekly files of each kind are kept whatever their age. The VM keeps as many, and the copy sends every file that the target lacks, so a file deleted while the VM still has it would come back.
- A copy is kept for its full time even when the VM is taken over: the prune goes by the time in the name of each copy, and the key of the VM can neither rename nor replace a file on the target. The floors keep the last copies when the backups stop, for example after the VM is lost; they guard against that accident, not against an attacker on the VM, who could fill them with files of newer names.
- No other name is touched: not the temporary files of `rsync`, not `.last-copy`.
- To change the limits, set `PRUNE_DAILY_DAYS` and `PRUNE_WEEKLY_WEEKS` in `backup-target.env` and run `docker compose --env-file backup-target.env up -d` in `ops/deploy/backup-target` on the backup host.
- To see what it did: `docker compose --env-file backup-target.env logs prune` names each file it deleted and counts each kind. Without `--env-file`, Compose stops at the settings that the target requires and shows no log. `docker compose --env-file backup-target.env run --rm prune --dry-run` names what it would delete now, and deletes nothing.

## The copy every hour and the backup target

The copy off the VM is tried every hour, not only in the nightly backup (MIL-074), so the newest copy on the backup host stays within the 72 hours of `OffVmCopyStale` whenever the host is on at least once in that time.

- **On the VM.** `nfhotel-offvm-copy.timer` runs `nfhotel-backup --copy-only` at half past each hour (at most 5 minutes later, and at once after a boot that missed a run). It sends only what the target lacks; it dumps, archives and deletes nothing. One lock keeps it apart from the backup: the backup waits for the lock, and a copy that finds it taken skips its turn, because the backup copies at its end. Check it with `systemctl list-timers nfhotel-offvm-copy.timer` and `journalctl -u nfhotel-offvm-copy -n 20`; a failed copy is a warning there, and the next hour tries again.
- **The signal.** After a failed copy the VM pings the backup host over Tailscale. When the host answers more than 2 hours after the last copy, `BackupTargetDown` fires: the host is on, but its target is down ([Alerts](Alerts)). A host that is off raises nothing new.
- **On the backup host.** The scheduled task `\NFHotel\Backup target up` runs `start-target.js` from the folder of the target at sign-in and every hour: `docker compose up -d` with `backup-target.env`, which changes nothing while both services run and starts a service that has stopped. It runs as the user who signed in, so no password is stored, and through Windows Script Host, so no window opens and the last result of the task is the result of `docker compose`. To set it up, copy `start-target.js` and `register-start-task.ps1` from `ops/deploy/backup-target` into the folder of the target and run `powershell -NoProfile -ExecutionPolicy Bypass -File .\register-start-task.ps1` there; running it again replaces the task. To check it: `Get-ScheduledTaskInfo -TaskPath '\NFHotel\' -TaskName 'Backup target up'` shows `LastTaskResult` 0 after a run that worked, and `docker compose --env-file backup-target.env ps` shows both services running.
- **Docker Desktop.** The target runs only while Docker Desktop runs. Whether Docker Desktop starts at sign-in is a setting of Docker Desktop that S01 decides. A start of Docker Desktop 4.89 can stop on the 0-byte `.stale` files that an earlier start left in `%LOCALAPPDATA%\Docker\run` and `%LOCALAPPDATA%\docker-secrets-engine`; delete those files, then start it again.
- **Why.** On 2026-11-10 the engine of Docker Desktop stopped at 11:25:59 UTC and was running again at 11:31 UTC, but the two services of the target stayed stopped until they were started by hand the next day, so two nightly copies failed. The log of Docker Desktop from that day keeps only the user interface; the logs of the engine had rotated away by 2026-11-14, so why the services stayed stopped is not known. The task starts them within the hour, whatever the cause.

## Every backup is read back

Each dump is read back to its end while it is made (MIL-076), so a dump that cannot be read is found the next morning, not at the monthly drill.

- **How.** `pg_dump` runs in the postgres container as before. `tee` sends its stream to `age` and, through two named pipes in a private temporary folder, to two readers in the same container: `pg_restore --list` lists the archive and counts its tables, and `pg_restore --file=/dev/null` reads every block to the end. Neither reader connects to the database. No file holds the clear dump, and `age` encrypts the same bytes as before. A reader that hangs is stopped after 30 minutes, and the backup goes on.
- **What it proves.** The archive is whole, the `pg_restore` of the same PostgreSQL can read it, and it holds tables. The journal says `read back to its end: <n> tables` (`journalctl -u nfhotel-backup -n 20`).
- **What only the drill proves.** That the dump restores into a database, that the private key decrypts the copy, and the row counts. The monthly drill ([The drill](#the-drill), and the restore drill on [CI-CD](CI-CD#back-up-and-restore)) keeps proving those on another machine.
- **The reports.** `backup_check` gets the time of the backup when both readers ended well, and 0 until a dump has been read back; `nfhotel_backup_tables` gets the tables of that dump, and `nfhotel_backup_bytes` the size of every encrypted dump ([Monitoring](Monitoring)).
- **When a read-back fails.** The backup keeps its dump and succeeds, so a deploy goes on, and `BackupUnreadable` fires ([Alerts](Alerts)). The journal of `nfhotel-backup` names the exit code and the last lines of each reader. Run the backup again with `sudo systemctl start nfhotel-backup.service`; if that read-back fails too, S03 runs the restore drill on the newest copy on the laptop. After seven nights S01 decides whether a failed read-back from then on fails the backup, and so stops a deploy (MIL-076 task 5).
- **A smaller dump.** `BackupShrunk` fires when the newest dump has fewer tables than the most of the last 8 days, or is under half the size of the largest of them. A migration that removed a table explains the first, and the alert then stays for 8 days; otherwise compare the tables of the live database with the dumps before it.

## Rebuild the VM

Write down the time at each step; the drill compares them with the RTO. This order was tested in containers on 2026-11-07, and part 10 of `ops/tests/test-database.sh` runs its database part in CI (MIL-066 task 9).

1. **Stop the old VM** if it still runs, so two machines never answer as `nf-hotel`: on the node, `qm stop <id>`, and note its size with `qm config <id>` (`cores`, `memory` and the size of `scsi0`). In the [Tailscale admin console](https://login.tailscale.com/admin/machines), remove the old `nf-hotel` machine.
   *Why:* the tailnet gives the name to the first machine that asks; the old one would keep it.
2. **Create the VM** from the Debian cloud image, with the name and the size of the old one, as root on a machine with ssh to the node:
   ```bash
   bash ops/scripts/create-docker-vm.sh --ssh root@<node> --name nf-hotel --cores <n> --memory <MiB> --disk <GiB> --start
   ```
   It prints the VM id. It builds the Debian release of the old VM, 13 (`VERSION_ID` in its `/etc/os-release`), with Docker and the user `nfhotel`, and `--start` boots it, so that cloud-init runs. *Why this script:* the VM gets the same disk and cloud-init as before (ADR-0003); nothing is installed by hand.
3. **Sign in over ssh** as `nfhotel`, with the key that cloud-init installed (`--ssh-key-file`, by default `~/.ssh/id_ed25519.pub`). Cloud-init sets no password, so the serial console has no login. The guest agent gives the LAN address on the node, and so does the DHCP list of the router:
   ```bash
   qm guest cmd <id> network-get-interfaces
   ssh nfhotel@<LAN address>
   ```
4. **Join the tailnet** with the name and the tag of the old VM. Install Tailscale from its signed apt repository, as `create-monitoring-lxc.sh` does, and sign in:
   ```bash
   curl -fsSL https://pkgs.tailscale.com/stable/debian/trixie.noarmor.gpg | sudo tee /usr/share/keyrings/tailscale-archive-keyring.gpg >/dev/null
   curl -fsSL https://pkgs.tailscale.com/stable/debian/trixie.tailscale-keyring.list | sudo tee /etc/apt/sources.list.d/tailscale.list >/dev/null
   sudo apt-get update && sudo apt-get install -y tailscale
   sudo tailscale up --ssh --hostname=nf-hotel --advertise-tags=tag:nfhotel-vm
   ```
   Approve the login link in the browser. From here on, `ssh root@nf-hotel` works over Tailscale SSH, as it did on the old VM. *Why the tag:* the policy of the tailnet gives CD and the team their access by `tag:nfhotel-vm`, and the certificate of the site is issued to the name.
5. **Harden the host** with the [apply steps of Host Hardening](Host-Hardening#apply-steps-on-the-vm), except its step 5: the containers come with the CD of step 10. *Why before the data:* the settings archive holds every secret; it should land on a closed host.
6. **Check out the repository** as `nfhotel`, which owns `/opt/nfhotel`:
   ```bash
   sudo install -d -o nfhotel -g nfhotel /opt/nfhotel
   git clone https://github.com/NF-Hotel/NFHotel.git /opt/nfhotel
   ```
   *Why `nfhotel`:* CD runs git and `docker compose` as this user, so it must own the checkout.
7. **Restore the settings.** Copy the newest settings archive from the laptop to your own machine, and decrypt it there (the VM never holds the private key):
   ```bash
   age --decrypt --identity <key file> --output settings.tar nfhotel-settings-daily-<time>.tar.age
   scp settings.tar root@nf-hotel:/tmp/
   ```
   On the VM, check that `/tmp` is a `tmpfs`, then unpack the archive in place and remove it:
   ```bash
   findmnt /tmp
   sudo tar --extract --file /tmp/settings.tar --directory / && sudo rm -f /tmp/settings.tar
   ```
   It brings back `ops/deploy/*/*.env`, the proxy certificate, `/etc/nfhotel` with the passwords of the database roles, and the ssh key of the backup copy. *Why on your machine, and why /tmp:* decrypting on the VM would put the private key on it, and Debian 13 keeps `/tmp` in memory, so the decrypted secrets never reach the disk of the VM.
8. **Install the deploy user and the backup:**
   ```bash
   cd /opt/nfhotel
   sudo bash ops/scripts/install-deploy-user.sh --apply
   sudo bash ops/scripts/install-backup.sh --apply
   ```
   The first gives CD its user `deploy` and its one sudo rule (ADR-0007), and prints the Tailscale steps that it needs; the second reads the restored `/etc/nfhotel/backup.env`, so the first night is covered (ADR-0008). *Why now:* the ownership transfer of step 9 and the migrate step of CD each take a backup first, and stop without `/usr/local/sbin/nfhotel-backup`.
9. **Restore the database.** Start PostgreSQL alone, empty:
   ```bash
   sudo -u nfhotel bash ops/scripts/deploy-services.sh --no-build postgres
   ```
   Decrypt the newest dump on your own machine (`age --decrypt --identity <key file> --output nfhotel.dump nfhotel-daily-<time>.dump.age`) and copy it to `/tmp` on the VM, as in step 7. Then restore it, give the roles back and check:
   ```bash
   sudo docker exec -i nfhotel-postgres-postgres-1 sh -c 'pg_restore --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --no-owner --no-privileges' < /tmp/nfhotel.dump
   sudo rm -f /tmp/nfhotel.dump
   sudo bash ops/scripts/prepare-database-roles.sh --container nfhotel-postgres-postgres-1 --apply --transfer-ownership
   sudo bash ops/scripts/verify-database.sh --container nfhotel-postgres-postgres-1 --expect-transfer --expect-network
   ```
   `prepare-database-roles.sh` creates the roles with the passwords of the restored `/etc/nfhotel/secrets`, so the connection strings of the API and the migrator fit, and moves the restored tables to `nfhotel_owner` after a backup (ADR-0011). `verify-database.sh` prints only `PASS` lines. *Why before CD:* a dump holds no roles, so on a new server the migrate step of CD stops at its backup, which signs in as `nfhotel_backup`.
10. **Deploy:** re-run the latest successful `NF Hotel CD` run, and approve it (ADR-0038). Its migrate step skips every migration that the dump has; then every service starts from the released images with the restored settings.
11. **Add the team roles again**, one per person, with a new password that S01 gives in private (ADR-0034):
    ```bash
    sudo bash ops/scripts/manage-team-db-users.sh --apply --add <name>
    ```
    *Why again:* the roles of the team belong to the server, not to the database, and `pg_dump` does not keep them.
12. **Check:** open `https://nf-hotel.taildb84b5.ts.net/`, sign in at `/admin/login`, and find a booking from the day before the loss. In Discord, the alerts turn *RESOLVED*.

## Rebuild the monitoring LXC

Nothing in it is backed up: its secrets are re-issued and its history is accepted as lost (ADR-0025).

1. On the node, as root: `bash ops/scripts/create-monitoring-lxc.sh <id> --apply` (the steps of [Monitoring](Monitoring#part-b-the-container-proxmox-node-as-root)).
2. Re-issue the three secrets: a new Discord webhook in #nf-hotel-alerts, the ping address of the Healthchecks.io check, and a new Grafana password ([Monitoring](Monitoring#part-a-accounts-and-access-browser)).
3. In the LXC: check out the repository in `/opt/nfhotel-monitoring`, write the secrets as [Monitoring](Monitoring#part-c-the-monitoring-inside-the-container) says, and run `bash ops/scripts/deploy-monitoring.sh --apply`.
4. If the LXC got a new tailnet address, write it in `ops/deploy/alloy/alloy.env` on `nf-hotel` and re-run CD.

## The drill

The drill proves the RTO and the RPO without touching production: S03 creates a throwaway VM with another name (`create-docker-vm.sh` with `--name nf-hotel-drill`), follows steps 2 to 12 with the newest backups from the laptop, but joins the tailnet in step 4 without the name `nf-hotel` and its tag, and skips the CD of step 10 (the deploy runs with `ops/scripts/deploy.sh --skip-vm` against the drill VM instead), checks the site on the drill VM's address, records the times in an RC, and removes the VM with `qm destroy <id>`.

## Why it is done this way

- **Rebuild instead of a VM image backup.** The VM is fully described by scripts and released images, so only its data and its secrets need a backup; an image backup would need storage the node does not have.
- **Decrypt on your own machine.** The VM cannot read its own backups (ADR-0008); keeping it that way means a stolen VM disk reveals nothing, even during a restore.
- **The database before CD.** A dump holds no roles, and the ownership transfer and the migrate step of CD each take a backup as `nfhotel_backup` first. So the backup is installed and the roles are made on the restored database before CD, whose migrations then skip. The order that the page had before MIL-066 ran CD first, and its migrate step stopped at the backup; part 10 of `ops/tests/test-database.sh` shows both.
- **Read back every night, restored once a month.** A restore every night would need a second PostgreSQL on the VM and the time of a whole restore. Reading the stream while it is made finds a dump that is cut short or damaged the next morning, without a clear copy on the disk; the monthly drill keeps proving the restore, the key and the row counts.
- **Settings in the same backup as the database.** One timer, one copy, one retention and one alert (`BackupStale`) cover both, and they are always from the same night.
- **Not the monitoring.** Its secrets come back from their providers in minutes, and two weeks of logs are not worth a second backup path.
