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

Write down the time at each step; the drill compares them with the RTO.

1. **Stop the old VM** if it still runs, so two machines never answer as `nf-hotel`: on the node, `qm stop <id>`. In the [Tailscale admin console](https://login.tailscale.com/admin/machines), remove the old `nf-hotel` machine.
   *Why:* the tailnet gives the name to the first machine that asks; the old one would keep it.
2. **Create the VM** from the Debian cloud image, as root on a machine with ssh to the node:
   ```bash
   bash ops/scripts/create-docker-vm.sh --ssh root@<node>
   ```
   It prints the VM id. *Why this script:* the VM gets the same size, disk and cloud-init as before (ADR-0003); nothing is installed by hand.
3. **Join the tailnet** in the VM's console (`qm terminal <id>` on the node): `tailscale up --ssh --hostname nf-hotel`, and approve the login link in the browser.
4. **Harden the host** with the steps of [Host Hardening](Host-Hardening): the firewall, the Docker daemon settings and the patching. *Why before the data:* the settings archive holds every secret; it should land on a closed host.
5. **Restore the settings.** Copy the newest settings archive from the laptop to your own machine, and decrypt it there (the VM never holds the private key):
   ```bash
   age --decrypt --identity <key file> nfhotel-settings-daily-<time>.tar.age > settings.tar
   scp settings.tar root@nf-hotel:/root/
   ```
   On the VM, after the checkout of step 6 exists, unpack it in place and remove it:
   ```bash
   tar --extract --file /root/settings.tar --directory / && shred -u /root/settings.tar
   ```
   It brings back `ops/deploy/*/*.env`, the proxy certificate, `/etc/nfhotel` and the ssh key of the backup copy. *Why on your machine:* decrypting on the VM would put the private key on it.
6. **Check out the repository** as the user `nfhotel` in `/opt/nfhotel` (Proxmox Deployment, "Deploy everything", step 4 does this), then repeat the `tar --extract` of step 5 if you ran it before.
7. **Install the backup** again, so the first night is covered: `bash ops/scripts/install-backup.sh --recipient <age1...> --target <the target> --apply` (the values are in the restored `/etc/nfhotel/backup.env`).
8. **Deploy:** re-run the latest successful `NF Hotel CD` run. It pulls the released images and starts every service with the restored settings; the database starts empty.
9. **Restore the database** from the newest dump on the laptop, on your own machine first:
   ```bash
   age --decrypt --identity <key file> nfhotel-daily-<time>.dump.age > nfhotel.dump
   scp nfhotel.dump root@nf-hotel:/root/
   ```
   Then on the VM, with the API stopped so nothing writes in between:
   ```bash
   sudo docker stop nfhotel-api-nfhotel-api-1
   sudo docker exec -i nfhotel-postgres-postgres-1 sh -c 'pg_restore --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --clean --if-exists --no-owner --no-acl' < /root/nfhotel.dump
   sudo docker start nfhotel-api-nfhotel-api-1 && shred -u /root/nfhotel.dump
   ```
   Then run `sudo bash ops/scripts/prepare-database-roles.sh` (ADR-0011), so the roles of the API get their rights on the restored tables.
10. **Check:** open `https://nf-hotel.taildb84b5.ts.net/`, sign in at `/admin/login`, and find a booking from the day before the loss. In Discord, the alerts turn *RESOLVED*.

## Rebuild the monitoring LXC

Nothing in it is backed up: its secrets are re-issued and its history is accepted as lost (ADR-0025).

1. On the node, as root: `bash ops/scripts/create-monitoring-lxc.sh <id> --apply` (the steps of [Monitoring](Monitoring#part-b-the-container-proxmox-node-as-root)).
2. Re-issue the three secrets: a new Discord webhook in #nf-hotel-alerts, the ping address of the Healthchecks.io check, and a new Grafana password ([Monitoring](Monitoring#part-a-accounts-and-access-browser)).
3. In the LXC: check out the repository in `/opt/nfhotel-monitoring`, write the secrets as [Monitoring](Monitoring#part-c-the-monitoring-inside-the-container) says, and run `bash ops/scripts/deploy-monitoring.sh --apply`.
4. If the LXC got a new tailnet address, write it in `ops/deploy/alloy/alloy.env` on `nf-hotel` and re-run CD.

## The drill

The drill proves the RTO and the RPO without touching production: S03 creates a throwaway VM with another name (`create-docker-vm.sh` with `--name nf-hotel-drill`), follows steps 2 to 10 with the newest backups from the laptop, but skips the Tailscale name and the CD (the deploy runs with `ops/scripts/deploy.sh --skip-vm` against the drill VM instead), checks the site on the drill VM's address, records the times in an RC, and removes the VM with `qm destroy <id>`.

## Why it is done this way

- **Rebuild instead of a VM image backup.** The VM is fully described by scripts and released images, so only its data and its secrets need a backup; an image backup would need storage the node does not have.
- **Decrypt on your own machine.** The VM cannot read its own backups (ADR-0008); keeping it that way means a stolen VM disk reveals nothing, even during a restore.
- **Settings in the same backup as the database.** One timer, one copy, one retention and one alert (`BackupStale`) cover both, and they are always from the same night.
- **Not the monitoring.** Its secrets come back from their providers in minutes, and two weeks of logs are not worth a second backup path.
