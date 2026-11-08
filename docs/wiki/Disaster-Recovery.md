# Disaster Recovery

This page rebuilds NF Hotel after its VM `nf-hotel` or its monitoring LXC is lost: from nothing, using the scripts of the repository and the backups on the legion laptop.

**For:** S03 (the owner of the Proxmox node and of the age key) with one member of S01.
**Decision:** [ADR-0025](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0025-recovery-objectives.md). **Work:** MIL-037.

**The objectives:** the site answers again within **4 hours** (RTO), with at most the last **24 hours** of bookings lost (RPO), when the Proxmox node works. A lost node needs new hardware first.

**Contents**
1. [Before a disaster: keep these ready](#before-a-disaster-keep-these-ready)
2. [Rebuild the VM](#rebuild-the-vm)
3. [Rebuild the monitoring LXC](#rebuild-the-monitoring-lxc)
4. [The drill](#the-drill)
5. [Why it is done this way](#why-it-is-done-this-way)

---

## Before a disaster: keep these ready

| What | Where | Why |
| --- | --- | --- |
| The private age key of S03 | Offline, in two places (for example a USB stick at home and a printout elsewhere) | Every backup is encrypted to it; without it no backup can be read |
| The Proxmox root login | S03 | Creating a VM needs it |
| The backups | The legion laptop, folder of the backup target (ADR-0008) | The VM's own copies are lost with the VM |
| Admin of the tailnet and of the GitHub organization | S01 | The new VM must take the name `nf-hotel` and the CD must reach it |

Check once a month that the newest `nfhotel-daily-*.dump.age` and `nfhotel-settings-daily-*.tar.age` on the laptop are less than two days old.

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
- **Settings in the same backup as the database.** One timer, one copy, one retention and one alert (`BackupStale`) cover both, and they are always from the same night.
- **Not the monitoring.** Its secrets come back from their providers in minutes, and two weeks of logs are not worth a second backup path.
