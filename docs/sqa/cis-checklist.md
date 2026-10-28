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
| 2026-10-28 | Deprecated | mikemike | AI as stand-in for S01 | Every Pending VM row given its status from the evidence run of 2026-10-28, after the sshd, sudo and sysctl settings were installed; 2.1.x Deferred by S01's decision to keep the desktop; 5.3 and 5.28 wait for the release of the Alloy limits; accepted after RC-244 Go | [6ca419b] |
| 2026-10-28 | Accepted | mikemike | AI as stand-in for S01 | Row 4.3.8: the forward chain also accepts the published 5432 of the database relay, from `tailscale0` only (ADR-0034, MIL-048) | pending |

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

Every Pending VM row names the section of `ops/scripts/cis-evidence.sh` that proves it. The script is read-only. It ran on `nf-hotel` on 2026-10-24 (RC-191 to RC-193) and on 2026-10-28 (RC-244); a row that the run proves says "Verified on nf-hotel" with the date and the file in `/root` that holds the output.

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
| 1.1.2 | Only trusted users control the Docker daemon | Applied | The group `docker` holds one member, the service account `nfhotel`: system UID 997, password locked, no SSH key, no sudo rule. `ops/scripts/deploy-release.sh` runs `deploy-services.sh` as this account (`as_app`), so it needs the group; root administers, and `deploy` may run only `nfhotel-deploy` through sudo and is in no other group. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker (`getent group docker`); `getent passwd nfhotel`, `passwd -S nfhotel` |
| 1.1.3 to 1.1.18 | Auditing of the Docker files and daemon | Deferred | `auditd` is a Level 2 item of the Debian Benchmark (6.2) and needs central logging to be read, which is phase 3 of DSA-001 | — |
| 1.2.1 | The container host is hardened | Applied | The Debian part of this checklist, with its Deferred rows. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | This document |
| 1.2.2 | The version of Docker is up to date | Deferred | Docker 29.8.0 runs and 29.9.2 is in Docker's repository. With the kernel that waits for a reboot (6.12.113), it is installed in the first monthly window (wiki page Host-Hardening, Reboots), which S01 plans; unattended-upgrades installs Debian security updates only, not Docker's | ev:host (`apt-get -s upgrade`, reboot required), ev:docker (`docker version`) |

### 2 Docker daemon configuration

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 2.1 | Run the Docker daemon as a non-root user, if possible | Deferred | Rootless Docker changes the networking (no `DOCKER-USER`, no published 80 and 443 without extra capabilities) and the ownership of the volumes. User namespaces were evaluated (DSA-001 section 7) and not chosen for this milestone; the containers themselves drop their capabilities | — |
| 2.2 | Network traffic is restricted between containers on the default bridge | Applied | `"icc": false`. The services use their own networks (`nfhotel-proxy`, `nfhotel-api`, `nfhotel-db`, `nfhotel-llm`), not the default bridge. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker |
| 2.3 | The logging level is set to `info` | Applied | Not set in `daemon.json`, so the default `info` holds. `debug=false`. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker |
| 2.4 | Docker is allowed to make changes to iptables | Applied | Not turned off. The host firewall is a separate nftables table that runs beside Docker's rules (firewall). Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:firewall (`nft list tables`) |
| 2.5 | Insecure registries are not used | Applied | None configured; images come from Docker Hub, GHCR and MCR over TLS, by digest. Only the loopback defaults of Docker. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | ev:docker (`insecure-registries`) |
| 2.6 | The aufs storage driver is not used | Applied | Debian 13 with a current Docker uses `overlay2` or the containerd snapshotter. `storage=overlayfs`. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | ev:docker (`storage`) |
| 2.7 | TLS authentication for the Docker daemon is configured | Not applicable | The daemon listens only on its Unix socket; no TCP listener is configured (`ss -tln` shows no 2375 or 2376) | ev:docker, ev:firewall |
| 2.8 | The default ulimit is configured appropriately | Not applicable | A manual item: the defaults of Docker are kept and no service needs another limit; PIDs are limited per container (5.28) | — |
| 2.14 | Containers are restricted from acquiring new privileges | Applied | `"no-new-privileges": true`, and per service in compose (5.25). Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker |
| 2.15 | Live restore is enabled | Applied | `"live-restore": true`: a restart of the daemon no longer stops the containers. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker (`live-restore=true`) |
| 2.16 | The userland proxy is disabled | Applied | `"userland-proxy": false`: published ports are forwarded by the kernel, which the firewall sees. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker |
| 2.18 | Experimental features are not used in production | Applied | Not set. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | ev:docker (`experimental=false`) |

### 3 Docker daemon configuration files

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 3.1, 3.2 | `docker.service`: owner `root:root`, mode 644 or stricter | Applied | `root:root 644`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker (`stat`) |
| 3.3, 3.4 | `docker.socket`: owner `root:root`, mode 644 or stricter | Applied | `root:root 644`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker |
| 3.5, 3.6 | `/etc/docker`: owner `root:root`, mode 755 or stricter | Applied | `root:root 755`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker |
| 3.7 to 3.14 | Registry certificates and the daemon's TLS files | Not applicable | No registry certificate and no TLS listener (2.7) | — |
| 3.15, 3.16 | The Docker socket: owner `root:docker`, mode 660 or stricter | Applied | `root:docker 660`; the group holds only the service account (1.1.2). Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker |
| 3.17, 3.18 | `daemon.json`: owner `root:root`, mode 644 or stricter | Applied | Installed `root:root` 0644 by `ops/scripts/install-docker-daemon.sh`. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | daemon; ev:docker |
| 3.21 to 3.24 | The containerd socket: owner `root:root`, mode 660 or stricter | Applied | `root:root 660`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:docker |

### 4 Container images and build files

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 4.1 | A user for the container has been created | Applied | web and API run as `app`, benchmark and market analysis as `app`, PostgreSQL drops to `postgres`, nginx runs its workers as `nginx` | images; ev:containers (`user`) |
| 4.1 | (same) for the nginx master process and Ollama | Deferred | The nginx master starts as root to bind 80 and 443; `nginxinc/nginx-unprivileged` listens on 8080 and 8443, which changes the templates, the published ports and the tests, and is left for a change of its own. Ollama runs as root upstream; it is on an internal network, has no capability and a read-only root | compose |
| 4.2 | Containers use only trusted base images | Applied | Official images (nginx, postgres, python, .NET from MCR, ollama), pinned by digest, built in CI with provenance (MIL-022) | images |
| 4.3 | Unnecessary packages are not installed | Applied | `slim` and `alpine` bases with `--no-install-recommends`; the benchmark adds only `git`, `ca-certificates` and `socat` | images |
| 4.4 | Images are scanned and rebuilt to include security patches | Applied | Every release rebuilds the images on their pinned bases, and the scan gate of MIL-022 checks them (the last on 2026-10-27, the CD of `408e831`). The monthly rebuild of the wiki page Host-Hardening covers a month without a release; its first run is December 2026 | images; the CD log; the monthly DORA report |
| 4.6 | `HEALTHCHECK` instructions are added to the container images | Applied with one exception | 10 of 11 containers are `healthy`. The socket proxy of `ops/deploy/alloy` has no health check: its image has no tool to ask HAProxy, and Alloy, which depends on it, has its own health check and turns unhealthy when the proxy fails. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`health`) |
| 4.7 | Update instructions are not used alone in the Dockerfile | Applied | `apt-get update` and `install` are in one `RUN` | images |
| 4.9 | `COPY` is used instead of `ADD` | Applied | No `ADD` in any Dockerfile | images |
| 4.10 | Secrets are not stored in Dockerfiles | Applied | Secrets come from git-ignored settings files at run time; the secret scan of MIL-021 runs on every pull request | images |

### 5 Container runtime

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 5.1 | An AppArmor profile is enabled, if applicable | Applied | `docker-default` on all 11 containers; AppArmor is loaded with 22 profiles in enforce mode. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:containers (`apparmor`), ev:mac-fs |
| 5.3 | Linux kernel capabilities are restricted within containers | Pending VM | `cap_drop: [ALL]` for every service; only nginx and PostgreSQL add the ones they need, measured one by one (wiki page Host-Hardening). On 2026-10-28, 10 of 11 containers had it. The socket proxy of `ops/deploy/alloy` gets it in the change of RC-244 (tested: the read calls answer 200, a POST 403), which the next release deploys; checked again at the MIL-023 gate | compose; ev:containers |
| 5.4 | Privileged containers are not used | Applied | No service is privileged (`privileged=false` on all 11). `ops/deploy/proxmox` is privileged but is a test container for a developer's machine only and never runs on the VM. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers |
| 5.5 | Sensitive host system directories are not mounted | Applied | Only named volumes and the configuration folders of `deploy/`, read-only | compose; ev:containers (`mounts`) |
| 5.6 | sshd is not run within containers | Applied | No image runs sshd | images |
| 5.7 | Privileged ports are not mapped within containers | Not applicable | nginx publishes 80 and 443 on purpose: they are the public service. No other service publishes a port | compose |
| 5.8 | Only needed ports are open on the container | Applied | Only nginx publishes ports; the others only `expose` on their networks. Checked by `ops/tests/check-e2e-stack.sh` | compose; ev:containers (`ports`) |
| 5.9 | The host's network namespace is not shared | Applied | No `network_mode: host` | compose; ev:containers |
| 5.10 | Memory usage for containers is limited | Applied | `mem_limit` on all 11. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`memory`) |
| 5.11 | CPU priority is set appropriately | Applied | `cpus` on all 11. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`nano_cpus`) |
| 5.12 | The container's root filesystem is mounted read-only | Applied | `read_only: true` with `tmpfs` on all 11. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`read_only`) |
| 5.13 | Incoming container traffic is bound to a specific host interface | Applied | nginx publishes 80 and 443 on the Tailscale address `100.72.194.127` and 8081 on `127.0.0.1`; no other container publishes a port. The public site comes in through the Cloudflare Tunnel (MIL-040), which connects outwards. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`ports`), ev:firewall (`ss -tln`) |
| 5.14 | The `on-failure` restart policy is set to 5 | Not applicable | `unless-stopped` is chosen so that the services come back after a reboot and a Docker restart; a crash loop shows in the health check and fails the deploy check | compose |
| 5.15, 5.16, 5.20, 5.30 | The host's process, IPC, UTS and user namespaces are not shared | Applied | No `pid`, `ipc`, `uts` or `userns_mode` in any compose file | compose; ev:containers |
| 5.17 | Host devices are not directly exposed to containers | Applied | No `devices`; the GPU of Ollama is a commented option | compose |
| 5.19 | Mount propagation mode is not set to shared | Applied | No propagation option | compose; ev:containers |
| 5.21 | The default seccomp profile is not disabled | Applied | No `seccomp=unconfined` | compose; ev:containers (`security_opt`) |
| 5.24 | The cgroup usage is confirmed | Applied | No `cgroup_parent` | compose |
| 5.25 | The container is restricted from acquiring additional privileges | Applied | `security_opt: [no-new-privileges:true]` per service, and the daemon default (2.14). All 10 running containers. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | compose; ev:containers |
| 5.26 | Container health is checked at runtime | Applied with one exception | As 4.6: 10 of 11 are `healthy`, and the deploy checks the site after each release. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | compose; ev:containers (`health`) |
| 5.27 | Docker commands always use the latest version of their image | Applied | CD deploys the image digests of the release, never a moving tag | `ops/scripts/deploy-release.sh` |
| 5.28 | The PIDs cgroup limit is used | Pending VM | `pids_limit` per service. On 2026-10-28, 9 of 11 containers had it. Alloy (256; 15 in use) and its socket proxy (128; 5 in use) get it in the change of RC-244, which the next release deploys; checked again at the MIL-023 gate | compose; ev:containers (`pids_limit`) |
| 5.31 | The Docker socket is not mounted inside any container | Applied with one exception | One container mounts it: `socket-proxy` of `ops/deploy/alloy` (ADR-0020). The `:ro` flag does not limit the API, so the limit is the proxy's own rules: GET of containers (list, inspect, logs) and networks (list) only, every POST refused, and no image, volume, exec or info call. The proxy is on an internal network with no route out, and only Alloy reaches it. Alloy has no socket. `ops/tests/test-alloy-start.sh` checks the allowed and the refused calls in CI | compose; ev:containers (`mounts`); CI |

### 6 and 7 Operations and Swarm

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 6.1, 6.2 | Image and container sprawl is avoided | Deferred | 222 images (22.4 GB, 63 % reclaimable) and one stopped container on 2026-10-28. `docker image prune --all --filter until=720h` runs in the first monthly window (wiki page Host-Hardening, Monthly image rebuild, step 5), which S01 plans | `docker system df` |
| 7.x | Swarm mode items | Not applicable | Swarm is not used | ev:docker (`swarm=inactive`) |

## CIS Debian Linux 12 Benchmark v2.0.0, Level 1 (Server)

### 1 Initial setup

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 1.1.1.x | The kernel modules cramfs, freevxfs, hfs, hfsplus, jffs2, udf and usb-storage are not available | Deferred | A `modprobe.d` file is the follow-up once the evidence shows which are loadable; low risk on a VM without removable media | ev:mac-fs (`module ...`) |
| 1.1.2.1.x | `/tmp` is a separate partition with `nodev`, `nosuid`, `noexec` | Deferred | `tmp.mount` as tmpfs is the default of Debian 13; the options are checked first, then set by a drop-in in a maintenance window | ev:mac-fs (`findmnt`) |
| 1.1.2.2.x | `/dev/shm` has `nodev`, `nosuid`, `noexec` | Deferred | As above | ev:mac-fs |
| 1.2.1.1, 1.2.1.2 | GPG keys and package repositories are configured | Applied | The Debian archive and security archive, and Docker's and Tailscale's repositories, all signed; no pinned package. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:host (`apt-cache policy`) |
| 1.2.2.1 | Updates, patches and additional security software are installed | Applied | `unattended-upgrades` 2.12 installs Debian security updates daily: the log shows the runs of 2026-10-24 and 2026-10-25 ("All upgrades installed") and a run every day since. The kernel that waits for a reboot, and Docker and Tailscale, which are not from the security archive, go in the monthly window (Docker row 1.2.2). Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | patching; ev:host (`apt-get -s upgrade`), ev:patching |
| 1.3.1.1 to 1.3.1.4 | AppArmor is installed, enabled in the boot loader, and its profiles are in enforce or complain mode | Applied | Enabled (`Y`), 123 profiles loaded, 22 in enforce mode. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:mac-fs (`aa-status`) |
| 1.4.1 | A boot loader password is set | Not applicable | The console of the VM is reached only through the Proxmox node, whose access S03 controls; a GRUB password adds no protection beyond that | — |
| 1.4.2 | Access to the boot loader configuration is configured | Deferred | Checked with the first evidence run, then fixed with the other file permissions (7.1) | — |
| 1.5.x | ASLR is enabled; ptrace is restricted; core dumps are restricted; prelink and automatic error reporting are not installed | Applied | `kernel.randomize_va_space = 2`, `kernel.yama.ptrace_scope = 1` and `fs.suid_dumpable = 0`, from `ops/deploy/host/sysctl/60-nfhotel.conf`, installed by `ops/scripts/install-host-settings.sh`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:sysctl |
| 1.6.x | Login banners are configured | Deferred | Low value on a VM reached only by its administrators over Tailscale | — |

### 2 Services

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 2.1.x | Server services not in use: avahi, DHCP server, DNS server, FTP, LDAP, mail access (IMAP, POP3), NFS, NIS, print, rpcbind, rsync daemon, Samba, SNMP, telnet, TFTP, web proxy, web server on the host, xinetd, X | Deferred | **S01 decision, 2026-10-28:** the VM was installed with the GNOME desktop, and it stays as it is for now. It runs `gdm` (X), `cups` and `cups-browsed` (print, on the loopback), `avahi-daemon` (UDP 5353) and desktop services (colord, ModemManager, fwupd, upower and others). The firewall drops every input except 80, 443, Tailscale and SSH on `tailscale0`, so the network reaches none of them. Removing the desktop (`task-gnome-desktop`, then `apt autoremove`) is a new decision of S01 | ev:services, ev:firewall (`ss -tln`, `ss -uln`) |
| 2.1.21 | The mail transfer agent is local-only | Applied | No MTA listens: `ss -tln` shows no port 25. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:firewall (`ss -tln`) |
| 2.2.x | Clients not installed: NIS, rsh, talk, telnet, LDAP, FTP | Deferred | None is installed except `inetutils-telnet`, which the desktop install brought; it goes with the desktop (2.1.x) | `dpkg-query` on 2026-10-28 |
| 2.3.x | Time synchronisation is in use and configured | Applied | `systemd-timesyncd`, `NTPSynchronized=yes`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:services (`timedatectl`) |
| 2.4.1.x | cron is enabled and its files have the right permissions | Deferred | Checked with the first evidence run; the backup uses a systemd timer, not cron | — |

### 3 Network

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 3.1.2, 3.1.3 | Wireless interfaces and Bluetooth are disabled | Not applicable | A KVM VM has neither | — |
| 3.3.1 | IP forwarding is disabled | Not applicable | Docker needs `net.ipv4.ip_forward=1` to route its networks; the forwarded traffic is filtered by the chain `docker-user` of the firewall instead | firewall; ev:sysctl |
| 3.3.2 to 3.3.11 | Packet redirects are not sent or accepted; bogus and broadcast ICMP is ignored; source routing is refused; suspicious packets are logged; TCP SYN cookies are on; IPv6 router advertisements are not accepted | Applied with two exceptions | `ops/deploy/host/sysctl/60-nfhotel.conf` sets the redirect, source routing, martian logging, ICMP and SYN cookie items. Reverse path filtering stays loose (`rp_filter=2`), which Tailscale needs. `net.ipv6.conf.all.accept_ra` stays 1, because NetworkManager handles router advertisements itself and `ens18` already has `accept_ra = 0`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:sysctl |

### 4 Host based firewall

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 4.1.1 | A single firewall configuration utility is in use | Applied | nftables. Docker and Tailscale write their own rules through `iptables-nft`, which is the nftables backend; `ufw` is not used Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall; ev:firewall |
| 4.2.x | ufw items | Not applicable | ufw is not used (4.3.2) | — |
| 4.3.1 | nftables is installed | Applied | `ops/scripts/apply-firewall.sh` refuses to run without `nft` Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | ev:firewall |
| 4.3.2 | ufw is uninstalled or disabled with nftables | Applied | | ev:firewall (`ufw: enabled=`) | Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`).
| 4.3.3 | iptables are flushed with nftables | Not applicable | Docker and Tailscale keep their rules in iptables-nft tables; flushing them breaks the containers and the tailnet. The NF Hotel rules are in a table of their own instead | firewall |
| 4.3.4 | A nftables table exists | Applied | `table inet nfhotel`. Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall; ev:firewall |
| 4.3.5 | nftables base chains exist | Applied | `input` (hook input) and `docker-user` (hook forward, before Docker's chains) Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall; ev:firewall |
| 4.3.6 | nftables loopback traffic is configured | Applied | `iif lo` accepted; `127.0.0.0/8` and `::1` from another interface dropped Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall |
| 4.3.7 | nftables outbound and established connections are configured | Applied | Established and related traffic is accepted on input and forward; output is not filtered (4.3.8) Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall |
| 4.3.8 | nftables default deny firewall policy | Applied | Input: policy drop. Forward: everything from outside that is not a published 80 or 443, or the published 5432 of the database relay from `tailscale0` (ADR-0034, MIL-048, checked at its gate), ends in a drop; Docker's own chains decide the rest. Output: accept, see the next row Verified on nf-hotel 2026-10-24 (RC-192, `/root/cis-evidence-after-firewall.txt`). | firewall; `ops/tests/test-nftables.sh` |
| 4.3.8 | (same) for output | Deferred | An egress allow-list would have to follow Debian mirrors, the container registries, Tailscale's coordination and DERP servers, and the model downloads of Ollama; left to a change of its own | — |
| 4.3.9 | nftables rules are permanent | Applied | `nfhotel-firewall.service` is enabled and active, and loads `/etc/nfhotel/nftables.nft` before the network at boot. Debian's `nftables.service` is disabled, because its `flush ruleset` removes Docker's rules. The table survived a Docker restart (RC-193). Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | firewall; ev:firewall |

### 5 Access control

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 5.1.1 to 5.1.3 | Permissions on `sshd_config` and the host keys | Applied | `sshd_config` `root:root 644`; the private host keys `root:root 600`, the public ones `644`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:ssh; `stat /etc/ssh/ssh_host_*` |
| 5.1.x | sshd settings: `PermitEmptyPasswords no`, `HostbasedAuthentication no`, `IgnoreRhosts yes`, `PermitUserEnvironment no`, `MaxAuthTries` 4 or less, `LoginGraceTime` 60 or less, `UsePAM yes`, `LogLevel` INFO or VERBOSE, `DisableForwarding yes`, strong ciphers, MACs and key exchange | Applied | `ops/deploy/host/ssh/nfhotel.conf`, installed by `ops/scripts/install-host-settings.sh`, sets the ones Debian 13 does not: `PermitRootLogin no`, `PasswordAuthentication no`, `MaxAuthTries 4`, `LoginGraceTime 60`, `DisableForwarding yes`, `LogLevel VERBOSE`, and ciphers, MACs and key exchange without SHA-1 and 64-bit MACs. **Finding:** until 2026-10-28 sshd accepted passwords (`passwordauthentication yes`), and six personal accounts have one. sshd was not reachable: Tailscale SSH serves port 22 of the tailnet address, and the firewall drops 22 everywhere else. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:ssh (`sshd -T`) |
| 5.1.20 | `PermitRootLogin` is disabled | Deferred | S01 and S03 administer as root over Tailscale today, by key. A named administrator account with sudo comes first; until then root logs in by key only (`prohibit-password`) and port 22 answers only on `tailscale0` (4.3) | ev:ssh; firewall |
| 5.1.x | Access to sshd is limited (`AllowUsers` or `AllowGroups`) | Deferred | With the named administrator account above | ev:ssh |
| 5.2.1 to 5.2.3 | sudo is installed, uses a pty and logs to a file | Applied | `Defaults use_pty` (Debian) and `Defaults logfile="/var/log/sudo.log"` from `ops/deploy/host/sudo/nfhotel-logging`, rotated by `logrotate/nfhotel-sudo`. The deploy rule of `deploy` still works (`sudo -n -l`). Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:accounts |
| 5.3.x, 5.4.1.x | PAM password quality, lockout and password aging | Not applicable | No account logs in with a password over the network: Tailscale SSH authenticates by the tailnet identity, sshd accepts keys only (`passwordauthentication no` since 2026-10-28, row 5.1.x), and `deploy` has no password. The passwords of the six personal accounts are used only at the Proxmox console. Holds while `sshd -T` shows `passwordauthentication no` | ev:ssh |
| 5.4.2.1 | root is the only UID 0 account | Applied | Only `root`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:accounts |
| 5.4.2.x | root's path, umask and the shell timeout | Deferred | Checked with the first evidence run | — |

### 6 Logging and auditing

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 6.1.1.x | journald: the service is enabled, its logs are compressed, written to persistent disk, and rotated | Applied | `Storage=persistent`, `Compress=yes`, `SystemMaxUse=1G`, `SystemKeepFree=2G`, `MaxFileSec=1month`. Verified on nf-hotel 2026-10-24 (RC-193, `/root/cis-evidence-after.txt`). | patching; ev:patching (`systemd-analyze cat-config`) |
| 6.1.x | logrotate is configured; access to the log files is configured | Applied | logrotate 3.22.0 with Debian's rules and `nfhotel-sudo`; the journal is persistent and bounded (1 GB); the container logs use the `local` driver, 5 files of 10 MB. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | patching; daemon; ev:patching |
| 6.3.1 | AIDE is installed | Deferred | File integrity checks need a place to report to; planned with central logging (DSA-001 phase 3) | — |

### 7 System maintenance

| # | Item | Status | How, or why not | Evidence |
| --- | --- | --- | --- | --- |
| 7.1.1 to 7.1.10 | Permissions on `/etc/passwd`, `/etc/shadow`, `/etc/group`, `/etc/gshadow` and their backups | Applied | `passwd`, `group` and `passwd-` `root:root 644`; `shadow`, `gshadow` and `shadow-` `root:shadow 640`. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:accounts (`stat`) |
| 7.1.11, 7.1.12 | No world-writable files, no unowned files or folders | Deferred | No world-writable file. One file has no group: `/opt/nfhotel-legacy/deployment/secrets/ConnectionStrings__HotelDatabase` (owner root, group 1654, mode 440), left from the deployment before MIL-021. S01 decides whether `/opt/nfhotel-legacy` is still needed; until then, `chgrp root` on the file closes the item | ev:accounts (`find`) |
| 7.2.x | Shadowed passwords, no empty password fields, no duplicate UIDs, GIDs, user or group names | Applied | No empty password field; `pwck -r` and `grpck -r` pass. Verified on nf-hotel 2026-10-28 (RC-244, `/root/cis-evidence-after-host-settings.txt`). | ev:accounts; `pwck -r`, `grpck -r` |

## Summary

| Benchmark | Applied | Pending VM | Not applicable | Deferred |
| --- | --- | --- | --- | --- |
| Docker | 44 | 2 | 6 | 6 |
| Debian 12 | 24 | 0 | 6 | 14 |

The counts are by row; a row that covers several items counts once. Applied counts the rows Applied with an exception too. What closes criterion 1: the two Pending VM rows (5.3, 5.28) are Applied once the release with the Alloy limits runs on `nf-hotel`, checked by `ops/scripts/cis-evidence.sh` at the MIL-023 gate.

---

[MIL-023]: ../milestones/mil-023-operational-quality.md
[DSA-001]: ./devsecops-assessment.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[RC-176]: ./reviews/rc-176-mil-023-host-hardening.md
[717dc18]: https://github.com/NF-Hotel/NFHotel/commit/717dc18e7bd3a6064272ef8fc9eb9ae123b89e55
[4ff81cc]: https://github.com/NF-Hotel/NFHotel/commit/4ff81ccd0f1d43a160f2f45d72845e942cf5728e
[6ca419b]: https://github.com/NF-Hotel/NFHotel/commit/6ca419bfc4ecf3eddee0c5244acea0009cc564d5
