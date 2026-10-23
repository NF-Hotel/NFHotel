# CIS-001 CIS Benchmark checklist of the NF Hotel VM

## Metadata
| Key | Value |
| --- | --- |
| ID | CIS-001 |
| CrossReference | [MIL-023], [DSA-001], [ADR-0010], [RC-176] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | Initial selection (MIL-023 task 1); the evidence on `nf-hotel` is pending | pending |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Ported from NF-Hotel/DevOps#12 into NF-Hotel/NFHotel under ops/ (MIL-031 task 7, ADR-0016); IDs renumbered; accepted after RC-176 Go, ported in RC-178 | pending |

---

## Purpose

MIL-023 task 1 (issue NF-Hotel/NFHotel#134): choose the Level 1 items of the CIS Docker Benchmark v1.9.0 and the CIS Debian Linux 12 Benchmark v2.0.0 that apply to the NF Hotel VM `nf-hotel`, and record for each its status and evidence. MIL-023 criterion 1 is met when every selected item has a final status and no Applied item is without evidence.

**Scope.** `nf-hotel` runs Debian 13, which has no CIS Benchmark yet. The Debian 12 Benchmark is used, as DSA-001 section 4 decided; its items hold for Debian 13 unless a row says otherwise. Level 2 items are listed only where DSA-001 or MIL-023 names them, as not selected.

**Item numbers.** The numbers and titles below are those of the CIS Docker Benchmark as cited by DSA-001 section 7 (the numbering of v1.6.0) and of the CIS Debian Linux 12 Benchmark v2.0.0. v1.9.0 of the Docker Benchmark may number some items of section 5 differently. The title of each row is the reference; S01 maps the numbers against the v1.9.0 document before criterion 1 is checked (RC-176, action item).

## Status

| Status | Meaning |
| --- | --- |
| Applied | In force on `nf-hotel`, with the evidence named in the row |
| Pending VM | Selected, and the repository holds the change and its test; it becomes Applied when the apply steps of the wiki page Host-Hardening have run and `ops/scripts/cis-evidence.sh` shows it on the VM |
| Not applicable | The item does not fit this VM; the row gives the reason |
| Deferred | Selected, not done in this milestone; the row gives the reason and what would do it |

Every Pending VM row names the section of `ops/scripts/cis-evidence.sh` that proves it. The script is read-only. Its first run on the VM was not done in this change, so no row is Applied on the VM's own evidence yet. A row is Applied when it is a property of the images or of the compose files that the VM already runs from this repository, unchanged by MIL-023; what MIL-023 adds to those files is Pending VM until the release that holds it is deployed.

Repository evidence used below:

| Short name | Evidence |
| --- | --- |
| daemon | `ops/deploy/host/docker/daemon.json`, installed by `ops/scripts/install-docker-daemon.sh`; tested by `ops/tests/test-host-setup.sh` (CI job "Host hardening") |
| compose | `deploy/*/docker-compose.yml`; every container of the throwaway stack checked by `ops/tests/check-e2e-stack.sh` (CI job "End-to-end stack"); ollama, market analysis and benchmark by `ops/tests/test-optional-services.sh` (local run in RC-176) |
| firewall | `ops/deploy/host/nftables/nfhotel.nft` and `ops/deploy/systemd/nfhotel-firewall.service`, applied by `ops/scripts/apply-firewall.sh`; tested by `ops/tests/test-nftables.sh` (CI job "Host hardening") |
| patching | `ops/deploy/host/apt/*` and `ops/deploy/host/journald/nfhotel.conf`, installed by `ops/scripts/install-patching.sh`; tested by `ops/tests/test-host-setup.sh` |
| images | The Dockerfiles of NF-Hotel/NFHotel and `deploy/*/Dockerfile`, images pinned by digest and scanned in the release (MIL-022, RC-503) |

The evidence sections of `ops/scripts/cis-evidence.sh` are written `ev:<section>`: `ev:host`, `ev:docker`, `ev:containers`, `ev:firewall`, `ev:patching`, `ev:ssh`, `ev:sysctl`, `ev:mac-fs`, `ev:services`, `ev:accounts`.

## CIS Docker Benchmark, Level 1

### 1 Host configuration

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 1.1.1 | A separate partition for containers | Deferred | The VM has one disk; a separate `/var/lib/docker` needs a new disk and a move of the volumes, the database included. Taken up with the next disk change of S03 | ev:docker (`findmnt -T /var/lib/docker`) |
| 1.1.2 | Only trusted users control the Docker daemon | Pending VM | The group `docker` should be empty: root administers, and the user `deploy` may run only `nfhotel-deploy` through sudo and is in no other group (`ops/scripts/install-deploy-user.sh`, `ops/tests/test-vm-setup.sh`) | ev:docker (`getent group docker`) |
| 1.1.3 to 1.1.18 | Auditing of the Docker files and daemon | Deferred | `auditd` is a Level 2 item of the Debian Benchmark (6.2) and needs central logging to be read, which is phase 3 of DSA-001 | — |
| 1.2.1 | The container host is hardened | Pending VM | The Debian part of this checklist | This document |
| 1.2.2 | The version of Docker is up to date | Pending VM | Updated in the monthly window (wiki page Host-Hardening, Reboots) | ev:host, ev:docker (`docker version`, `apt-cache policy`) |

### 2 Docker daemon configuration

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 2.1 | Run the Docker daemon as a non-root user, if possible | Deferred | Rootless Docker changes the networking (no `DOCKER-USER`, no published 80 and 443 without extra capabilities) and the ownership of the volumes. User namespaces were evaluated (DSA-001 section 7) and not chosen for this milestone; the containers themselves drop their capabilities | — |
| 2.2 | Network traffic is restricted between containers on the default bridge | Pending VM | `"icc": false`. The services use their own networks (`nfhotel-proxy`, `nfhotel-api`, `nfhotel-db`, `nfhotel-llm`), not the default bridge | daemon; ev:docker |
| 2.3 | The logging level is set to `info` | Pending VM | Not set in `daemon.json`, so the default `info` holds | daemon; ev:docker |
| 2.4 | Docker is allowed to make changes to iptables | Pending VM | Not turned off. The host firewall is a separate nftables table that runs beside Docker's rules (firewall) | daemon; ev:firewall (`nft list tables`) |
| 2.5 | Insecure registries are not used | Pending VM | None configured; images come from Docker Hub, GHCR and MCR over TLS, by digest | ev:docker (`insecure-registries`) |
| 2.6 | The aufs storage driver is not used | Pending VM | Debian 13 with a current Docker uses `overlay2` or the containerd snapshotter | ev:docker (`storage`) |
| 2.7 | TLS authentication for the Docker daemon is configured | Not applicable | The daemon listens only on its Unix socket; no TCP listener is configured (`ss -tln` shows no 2375 or 2376) | ev:docker, ev:firewall |
| 2.8 | The default ulimit is configured appropriately | Not applicable | A manual item: the defaults of Docker are kept and no service needs another limit; PIDs are limited per container (5.28) | — |
| 2.14 | Containers are restricted from acquiring new privileges | Pending VM | `"no-new-privileges": true`, and per service in compose (5.25) | daemon; ev:docker |
| 2.15 | Live restore is enabled | Pending VM | `"live-restore": true`: a restart of the daemon no longer stops the containers | daemon; ev:docker (`live-restore=true`) |
| 2.16 | The userland proxy is disabled | Pending VM | `"userland-proxy": false`: published ports are forwarded by the kernel, which the firewall sees | daemon; ev:docker |
| 2.18 | Experimental features are not used in production | Pending VM | Not set | ev:docker (`experimental=false`) |

### 3 Docker daemon configuration files

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 3.1, 3.2 | `docker.service`: owner `root:root`, mode 644 or stricter | Pending VM | The package defaults | ev:docker (`stat`) |
| 3.3, 3.4 | `docker.socket`: owner `root:root`, mode 644 or stricter | Pending VM | The package defaults | ev:docker |
| 3.5, 3.6 | `/etc/docker`: owner `root:root`, mode 755 or stricter | Pending VM | The package defaults | ev:docker |
| 3.7 to 3.14 | Registry certificates and the daemon's TLS files | Not applicable | No registry certificate and no TLS listener (2.7) | — |
| 3.15, 3.16 | The Docker socket: owner `root:docker`, mode 660 or stricter | Pending VM | The package defaults; the group is empty (1.1.2) | ev:docker |
| 3.17, 3.18 | `daemon.json`: owner `root:root`, mode 644 or stricter | Pending VM | Installed `root:root` 0644 by `ops/scripts/install-docker-daemon.sh` | daemon; ev:docker |
| 3.21 to 3.24 | The containerd socket: owner `root:root`, mode 660 or stricter | Pending VM | The package defaults | ev:docker |

### 4 Container images and build files

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 4.1 | A user for the container has been created | Applied | web and API run as `app`, benchmark and market analysis as `app`, PostgreSQL drops to `postgres`, nginx runs its workers as `nginx` | images; ev:containers (`user`) |
| 4.1 | (same) for the nginx master process and Ollama | Deferred | The nginx master starts as root to bind 80 and 443; `nginxinc/nginx-unprivileged` listens on 8080 and 8443, which changes the templates, the published ports and the tests, and is left for a change of its own. Ollama runs as root upstream; it is on an internal network, has no capability and a read-only root | compose |
| 4.2 | Containers use only trusted base images | Applied | Official images (nginx, postgres, python, .NET from MCR, ollama), pinned by digest, built in CI with provenance (MIL-022) | images |
| 4.3 | Unnecessary packages are not installed | Applied | `slim` and `alpine` bases with `--no-install-recommends`; the benchmark adds only `git`, `ca-certificates` and `socat` | images |
| 4.4 | Images are scanned and rebuilt to include security patches | Pending VM | Scanned in every release (MIL-022 scan gate); rebuilt monthly by the plan in the wiki page Host-Hardening, whose first run is due in December 2026 | images; the monthly DORA report |
| 4.6 | `HEALTHCHECK` instructions are added to the container images | Pending VM | Every service has a health check in its compose file; nginx gets one in MIL-023 | compose; ev:containers (`health`) |
| 4.7 | Update instructions are not used alone in the Dockerfile | Applied | `apt-get update` and `install` are in one `RUN` | images |
| 4.9 | `COPY` is used instead of `ADD` | Applied | No `ADD` in any Dockerfile | images |
| 4.10 | Secrets are not stored in Dockerfiles | Applied | Secrets come from git-ignored settings files at run time; the secret scan of MIL-021 runs on every pull request | images |

### 5 Container runtime

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 5.1 | An AppArmor profile is enabled, if applicable | Pending VM | Docker applies `docker-default` when AppArmor is on (Debian 1.3) | ev:containers (`apparmor`), ev:mac-fs |
| 5.3 | Linux kernel capabilities are restricted within containers | Pending VM | `cap_drop: [ALL]` for every service; only nginx and PostgreSQL add the ones they need, measured one by one (wiki page Host-Hardening) | compose; ev:containers |
| 5.4 | Privileged containers are not used | Pending VM | No service is privileged. `ops/deploy/proxmox` is privileged but is a test container for a developer's machine only and never runs on the VM | compose; ev:containers |
| 5.5 | Sensitive host system directories are not mounted | Applied | Only named volumes and the configuration folders of `deploy/`, read-only | compose; ev:containers (`mounts`) |
| 5.6 | sshd is not run within containers | Applied | No image runs sshd | images |
| 5.7 | Privileged ports are not mapped within containers | Not applicable | nginx publishes 80 and 443 on purpose: they are the public service. No other service publishes a port | compose |
| 5.8 | Only needed ports are open on the container | Applied | Only nginx publishes ports; the others only `expose` on their networks. Checked by `ops/tests/check-e2e-stack.sh` | compose; ev:containers (`ports`) |
| 5.9 | The host's network namespace is not shared | Applied | No `network_mode: host` | compose; ev:containers |
| 5.10 | Memory usage for containers is limited | Pending VM | `mem_limit` per service | compose; ev:containers (`memory`) |
| 5.11 | CPU priority is set appropriately | Pending VM | `cpus` per service | compose; ev:containers (`nano_cpus`) |
| 5.12 | The container's root filesystem is mounted read-only | Pending VM | `read_only: true` with `tmpfs` for every service | compose; ev:containers (`read_only`) |
| 5.13 | Incoming container traffic is bound to a specific host interface | Pending VM | The internal listener of nginx is bound to `127.0.0.1`. 80 and 443 are bound to all addresses because they are the public service; the firewall decides who reaches them | compose; firewall |
| 5.14 | The `on-failure` restart policy is set to 5 | Not applicable | `unless-stopped` is chosen so that the services come back after a reboot and a Docker restart; a crash loop shows in the health check and fails the deploy check | compose |
| 5.15, 5.16, 5.20, 5.30 | The host's process, IPC, UTS and user namespaces are not shared | Applied | No `pid`, `ipc`, `uts` or `userns_mode` in any compose file | compose; ev:containers |
| 5.17 | Host devices are not directly exposed to containers | Applied | No `devices`; the GPU of Ollama is a commented option | compose |
| 5.19 | Mount propagation mode is not set to shared | Applied | No propagation option | compose; ev:containers |
| 5.21 | The default seccomp profile is not disabled | Applied | No `seccomp=unconfined` | compose; ev:containers (`security_opt`) |
| 5.24 | The cgroup usage is confirmed | Applied | No `cgroup_parent` | compose |
| 5.25 | The container is restricted from acquiring additional privileges | Pending VM | `security_opt: [no-new-privileges:true]` per service, and the daemon default (2.14) | compose; ev:containers |
| 5.26 | Container health is checked at runtime | Pending VM | Health checks per service; the deploy checks 80 and 443 after each release | compose; ev:containers (`health`) |
| 5.27 | Docker commands always use the latest version of their image | Applied | CD deploys the image digests of the release, never a moving tag | `ops/scripts/deploy-release.sh` |
| 5.28 | The PIDs cgroup limit is used | Pending VM | `pids_limit` per service | compose; ev:containers (`pids_limit`) |
| 5.31 | The Docker socket is not mounted inside any container | Applied | No service mounts it | compose; ev:containers (`mounts`) |

### 6 and 7 Operations and Swarm

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 6.1, 6.2 | Image and container sprawl is avoided | Pending VM | `docker image prune` in the monthly window (wiki page Host-Hardening) | ev:containers |
| 7.x | Swarm mode items | Not applicable | Swarm is not used | ev:docker (`swarm=inactive`) |

## CIS Debian Linux 12 Benchmark v2.0.0, Level 1 (Server)

### 1 Initial setup

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 1.1.1.x | The kernel modules cramfs, freevxfs, hfs, hfsplus, jffs2, udf and usb-storage are not available | Deferred | A `modprobe.d` file is the follow-up once the evidence shows which are loadable; low risk on a VM without removable media | ev:mac-fs (`module ...`) |
| 1.1.2.1.x | `/tmp` is a separate partition with `nodev`, `nosuid`, `noexec` | Deferred | `tmp.mount` as tmpfs is the default of Debian 13; the options are checked first, then set by a drop-in in a maintenance window | ev:mac-fs (`findmnt`) |
| 1.1.2.2.x | `/dev/shm` has `nodev`, `nosuid`, `noexec` | Deferred | As above | ev:mac-fs |
| 1.2.1.1, 1.2.1.2 | GPG keys and package repositories are configured | Pending VM | The Debian archive and, if used, Docker's repository with its signing key | ev:host (`apt-cache policy`) |
| 1.2.2.1 | Updates, patches and additional security software are installed | Pending VM | `unattended-upgrades` installs Debian security updates daily | patching; ev:host (`apt-get -s upgrade`), ev:patching |
| 1.3.1.1 to 1.3.1.4 | AppArmor is installed, enabled in the boot loader, and its profiles are in enforce or complain mode | Pending VM | Debian enables AppArmor by default | ev:mac-fs (`aa-status`) |
| 1.4.1 | A boot loader password is set | Not applicable | The console of the VM is reached only through the Proxmox node, whose access S03 controls; a GRUB password adds no protection beyond that | — |
| 1.4.2 | Access to the boot loader configuration is configured | Deferred | Checked with the first evidence run, then fixed with the other file permissions (7.1) | — |
| 1.5.x | ASLR is enabled; ptrace is restricted; core dumps are restricted; prelink and automatic error reporting are not installed | Deferred | ASLR is on by default. `kernel.yama.ptrace_scope` and `fs.suid_dumpable` need a `sysctl.d` file, the follow-up after the evidence run (with 3.3) | ev:sysctl |
| 1.6.x | Login banners are configured | Deferred | Low value on a VM reached only by its administrators over Tailscale | — |

### 2 Services

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 2.1.x | Server services not in use: avahi, DHCP server, DNS server, FTP, LDAP, mail access (IMAP, POP3), NFS, NIS, print, rpcbind, rsync daemon, Samba, SNMP, telnet, TFTP, web proxy, web server on the host, xinetd, X | Pending VM | The VM runs sshd, Tailscale and Docker; every service is a container. `ss` and the running units show any other | ev:services, ev:firewall (`ss -tln`, `ss -uln`) |
| 2.1.21 | The mail transfer agent is local-only | Pending VM | No MTA is expected; if one is installed it must listen on the loopback only | ev:firewall (`ss -tln`) |
| 2.2.x | Clients not installed: NIS, rsh, talk, telnet, LDAP, FTP | Pending VM | Not part of a Debian server install | ev:services |
| 2.3.x | Time synchronisation is in use and configured | Pending VM | `systemd-timesyncd` (Debian default) | ev:services (`timedatectl`) |
| 2.4.1.x | cron is enabled and its files have the right permissions | Deferred | Checked with the first evidence run; the backup uses a systemd timer, not cron | — |

### 3 Network

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 3.1.2, 3.1.3 | Wireless interfaces and Bluetooth are disabled | Not applicable | A KVM VM has neither | — |
| 3.3.1 | IP forwarding is disabled | Not applicable | Docker needs `net.ipv4.ip_forward=1` to route its networks; the forwarded traffic is filtered by the chain `docker-user` of the firewall instead | firewall; ev:sysctl |
| 3.3.2 to 3.3.11 | Packet redirects are not sent or accepted; bogus and broadcast ICMP is ignored; source routing is refused; suspicious packets are logged; TCP SYN cookies are on; IPv6 router advertisements are not accepted | Deferred | A `sysctl.d` file after the evidence run. Reverse path filtering must stay loose (`rp_filter=2`), which Tailscale needs, and Docker's bridges need their own settings, so the values are tested on the VM before they are set | ev:sysctl |

### 4 Host based firewall

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 4.1.1 | A single firewall configuration utility is in use | Pending VM | nftables. Docker and Tailscale write their own rules through `iptables-nft`, which is the nftables backend; `ufw` is not used | firewall; ev:firewall |
| 4.2.x | ufw items | Not applicable | ufw is not used (4.3.2) | — |
| 4.3.1 | nftables is installed | Pending VM | `ops/scripts/apply-firewall.sh` refuses to run without `nft` | ev:firewall |
| 4.3.2 | ufw is uninstalled or disabled with nftables | Pending VM | | ev:firewall (`ufw: enabled=`) |
| 4.3.3 | iptables are flushed with nftables | Not applicable | Docker and Tailscale keep their rules in iptables-nft tables; flushing them breaks the containers and the tailnet. The NF Hotel rules are in a table of their own instead | firewall |
| 4.3.4 | A nftables table exists | Pending VM | `table inet nfhotel` | firewall; ev:firewall |
| 4.3.5 | nftables base chains exist | Pending VM | `input` (hook input) and `docker-user` (hook forward, before Docker's chains) | firewall; ev:firewall |
| 4.3.6 | nftables loopback traffic is configured | Pending VM | `iif lo` accepted; `127.0.0.0/8` and `::1` from another interface dropped | firewall |
| 4.3.7 | nftables outbound and established connections are configured | Pending VM | Established and related traffic is accepted on input and forward; output is not filtered (4.3.8) | firewall |
| 4.3.8 | nftables default deny firewall policy | Pending VM | Input: policy drop. Forward: everything from outside that is not a published 80 or 443 ends in a drop; Docker's own chains decide the rest. Output: accept, see the next row | firewall; `ops/tests/test-nftables.sh` |
| 4.3.8 | (same) for output | Deferred | An egress allow-list would have to follow Debian mirrors, the container registries, Tailscale's coordination and DERP servers, and the model downloads of Ollama; left to a change of its own | — |
| 4.3.9 | nftables rules are permanent | Pending VM | `nfhotel-firewall.service` loads `/etc/nfhotel/nftables.nft` before the network at boot; Debian's `nftables.service` is not used because its `flush ruleset` removes Docker's rules | firewall; ev:firewall |

### 5 Access control

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 5.1.1 to 5.1.3 | Permissions on `sshd_config` and the host keys | Pending VM | The package defaults | ev:ssh |
| 5.1.x | sshd settings: `PermitEmptyPasswords no`, `HostbasedAuthentication no`, `IgnoreRhosts yes`, `PermitUserEnvironment no`, `MaxAuthTries` 4 or less, `LoginGraceTime` 60 or less, `UsePAM yes`, `LogLevel` INFO or VERBOSE, `DisableForwarding yes`, strong ciphers, MACs and key exchange | Pending VM | Most are the Debian 13 defaults. A `sshd_config.d` file sets the rest once `sshd -T` shows which differ | ev:ssh (`sshd -T`) |
| 5.1.20 | `PermitRootLogin` is disabled | Deferred | S01 and S03 administer as root over Tailscale today, by key. A named administrator account with sudo comes first; until then root logs in by key only (`prohibit-password`) and port 22 answers only on `tailscale0` (4.3) | ev:ssh; firewall |
| 5.1.x | Access to sshd is limited (`AllowUsers` or `AllowGroups`) | Deferred | With the named administrator account above | ev:ssh |
| 5.2.1 to 5.2.3 | sudo is installed, uses a pty and logs to a file | Pending VM | sudo is used by `deploy` (`ops/scripts/install-deploy-user.sh`); `use_pty` and `logfile` are checked in the evidence | ev:accounts |
| 5.3.x, 5.4.1.x | PAM password quality, lockout and password aging | Not applicable | No account logs in with a password: SSH accepts keys only, and `deploy` has no password. Holds while `sshd -T` shows `passwordauthentication no` | ev:ssh |
| 5.4.2.1 | root is the only UID 0 account | Pending VM | | ev:accounts |
| 5.4.2.x | root's path, umask and the shell timeout | Deferred | Checked with the first evidence run | — |

### 6 Logging and auditing

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 6.1.1.x | journald: the service is enabled, its logs are compressed, written to persistent disk, and rotated | Pending VM | `Storage=persistent`, `Compress=yes`, `SystemMaxUse=1G`, `SystemKeepFree=2G`, `MaxFileSec=1month` | patching; ev:patching (`systemd-analyze cat-config`) |
| 6.1.x | logrotate is configured; access to the log files is configured | Pending VM | logrotate is installed by `ops/scripts/install-patching.sh` with Debian's own rules; container logs are rotated by the `local` log driver of the daemon (5 files of 10 MB) | patching; daemon; ev:patching |
| 6.3.1 | AIDE is installed | Deferred | File integrity checks need a place to report to; planned with central logging (DSA-001 phase 3) | — |

### 7 System maintenance

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 7.1.1 to 7.1.10 | Permissions on `/etc/passwd`, `/etc/shadow`, `/etc/group`, `/etc/gshadow` and their backups | Pending VM | The Debian defaults | ev:accounts (`stat`) |
| 7.1.11, 7.1.12 | No world-writable files, no unowned files or folders | Pending VM | The evidence prints the first one found, or nothing | ev:accounts (`find`) |
| 7.2.x | Shadowed passwords, no empty password fields, no duplicate UIDs, GIDs, user or group names | Pending VM | | ev:accounts |

## Summary

| Benchmark | Applied | Pending VM | Not applicable | Deferred |
| --- | --- | --- | --- | --- |
| Docker | 17 | 31 | 6 | 4 |
| Debian 12 | 0 | 25 | 6 | 13 |

The counts are by row; a row that covers several items counts once. What closes criterion 1: S01 runs the apply steps of the wiki page Host-Hardening, records `ops/scripts/cis-evidence.sh` before and after, moves every Pending VM row to Applied (or to Deferred, with the reason, where the evidence shows a gap), and adds the column "Verified on nf-hotel" with the date of the run.

---

[MIL-023]: ../milestones/mil-023-operational-quality.md
[DSA-001]: ./devsecops-assessment.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[RC-176]: ./reviews/rc-176-mil-023-host-hardening.md
