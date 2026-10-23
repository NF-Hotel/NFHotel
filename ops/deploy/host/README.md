# Host configuration of the NF Hotel VM (MIL-023)

The files the VM `nf-hotel` reads outside Docker Compose, kept here so that a change to the host
is reviewed like any other change. Each is installed by a script in `scripts/` that is a dry run
by default and changes the VM only with `--apply`. The CIS checklist that selects these
controls is `docs/sqa/cis-checklist.md` (CIS-001); the order of applying them on the VM is in
the wiki page `Host-Hardening.md`.

| File | Installed as | By |
| --- | --- | --- |
| `docker/daemon.json` | `/etc/docker/daemon.json` | `ops/scripts/install-docker-daemon.sh` |
| `nftables/nfhotel.nft` | `/etc/nfhotel/nftables.nft`, loaded by `nfhotel-firewall.service` | `ops/scripts/apply-firewall.sh` |
| `apt/20auto-upgrades`, `apt/52nfhotel-unattended-upgrades` | `/etc/apt/apt.conf.d/` | `ops/scripts/install-patching.sh` |
| `journald/nfhotel.conf` | `/etc/systemd/journald.conf.d/nfhotel.conf` | `ops/scripts/install-patching.sh` |
