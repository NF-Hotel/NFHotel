# Container Escape

How NF Hotel keeps a compromised container away from the resources of its Docker host (MIL-082): each path out of a container, the measure that closes it, the evidence that the measure is in force, what is left, and the decision with its owner. A container shares the kernel of its host, so every measure narrows what a process inside can ask of that kernel, of Docker and of the network. The settings of each service are on [Containers and Images](Containers-and-Images) and [Host Hardening](Host-Hardening); the runtime check that proves them at every deploy and every 15 minutes is in [Host Hardening](Host-Hardening#runtime-check-of-the-containers).

## Where an escape lands

| Host | What it is | A process that leaves a container there is |
| --- | --- | --- |
| `nf-hotel` | The VM of the application, a KVM virtual machine on the Proxmox node | A process of the VM, as the user it had in the container: root in a container is root of the VM, because no user namespace maps it to another user. The hypervisor still stands between it and the Proxmox node |
| `nf-monitoring` | The monitoring, in an unprivileged LXC container on the Proxmox node (`ops/scripts/create-monitoring-lxc.sh`: `--unprivileged 1`, `nesting=1`, `keyctl=1`) | A process of the LXC, whose root is an unprivileged user of the node. The LXC shares the kernel of the node, so a bug of that kernel reached from a container there reaches the node |
| The backup host | The laptop of S03 with `backup-target` and `prune`, reached only over the tailnet | A process of the laptop ([Disaster Recovery](Disaster-Recovery)) |

## The paths out of a container

The decisions of MIL-082 were made by the AI as stand-in for S01 in autonomous mode (RC-350), and they stay open to S01's challenge.

| Path | Measure | Evidence | What is left | Decision (owner) |
| --- | --- | --- | --- | --- |
| A bug of the kernel, reached through a system call | Docker's default seccomp profile in filter mode on every container: it refuses what a container does not need, such as `kexec_load`, `keyctl`, `bpf` and new namespaces without `SYS_ADMIN`. AppArmor `docker-default` in enforce mode where the host has AppArmor (the VM). Debian's security updates of the kernel every day, and the reboot that loads them ([Host Hardening](Host-Hardening#reboots)) | The rules `seccomp` and `apparmor` of the runtime check; CIS-001 rows 5.1 and 5.21; `/var/run/reboot-required` in the evidence of `cis-evidence.sh` | A bug in a system call that the profile allows reaches the kernel of the VM, which every container there shares | Keep the default profiles, with no profile per service (S01, MIL-023) |
| A capability | `cap_drop: [ALL]` everywhere. Added back: nginx `CHOWN`, `SETGID`, `SETUID`, `NET_BIND_SERVICE`, `DAC_READ_SEARCH`; PostgreSQL `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `SETGID`, `SETUID`; the one-shot owners `results-owner` and `models-owner` `CHOWN`, `DAC_READ_SEARCH`. No container holds `SYS_ADMIN`, `SYS_PTRACE`, `SYS_MODULE` or `NET_ADMIN`, and `no-new-privileges` stops a setuid binary from gaining more | The rules `cap_drop`, `cap_add`, `cap_bounding` (the bounding set of the main process, in the kernel's view) and `no_new_privileges`; CIS-001 rows 2.14, 5.3 and 5.25 | The capabilities of nginx and PostgreSQL, which their entrypoints need | Keep (S01, MIL-023) |
| Privileged mode and devices | No service is privileged and none has a device; the GPU of Ollama is a commented option | The rules `privileged` and `device`; CIS-001 rows 5.4 and 5.17 | `proxmox` is privileged, in CI and on a developer machine only, never on a host of production | Keep (S01, MIL-023) |
| A namespace of the host | No service shares the network, PID, IPC, UTS or user namespace of the host | The rule `host_namespace`; CIS-001 row 5.9 | None | Keep (S01, MIL-023) |
| `/proc` and `/sys` | Docker masks `/proc/kcore`, `/proc/keys` and other paths, and makes `/proc/sys` and `/proc/sysrq-trigger` read-only; no service sets `systempaths=unconfined`. Alloy reads the host's `/proc` and `/sys` through read-only mounts, as user 473 | The rule `system_paths` (MIL-082), with a container that sets `systempaths=unconfined` in the live test; the rule `host_path` for the mounts; CIS-001 row 5.5 | Alloy reads the process list and the counters of the kernel of the VM | Keep; the rule closes the gap the check had until MIL-082 (S01, MIL-082) |
| A writable host mount | Named volumes for data; host folders only read-only (the configuration under `ops/deploy`) | The rules `writable_mount` and `host_path`; CIS-001 row 5.5 | `backup-target` writes its folder of backups on the backup host, which is its purpose | Keep (S01, MIL-023) |
| The Docker socket or API | The daemon listens only on its Unix socket. One container mounts it, `socket-proxy`, whose allowlist lets Alloy list the containers and the networks and read the inspect and the logs of a container, and answers 403 to every other call; the proxy is on an internal network that only Alloy reaches. Alloy sees the socket through its mount of `/` and cannot open it | The rules `docker_socket` and `proxy_allowlist` (asked from inside Alloy at every run); `ops/tests/test-alloy-start.sh`: 12 allowed calls answer 200 and 69 others 403; ADR-0042; CIS-001 rows 2.7 and 5.31 | The proxy holds the socket: code that runs inside the proxy has the whole API, which is root of the VM | Keep (S01, ADR-0042) |
| A bug of the container runtime (`runc`, containerd, Docker Engine) | The versions are read at every run of the check, and a fix is installed as [below](#when-a-fix-for-an-escape-is-published): in the monthly window, or within a week when it is critical | The last lines of the check (the CD log, the journal of the timer) and `nfhotel_container_runtime_info` in Prometheus (MIL-082); CIS-001 row 1.2.2 | Docker's repository is outside `unattended-upgrades`, so a fix waits for a person | Install by hand, as below (S01; S03 on the VM) |
| Root inside the container | A user that is not root where the image allows it: the web application and the API (1654), the market analysis and the benchmark (10001), Ollama (10001 since MIL-082), Alloy (473), cloudflared (65532) and the relay (99); PostgreSQL drops to 70 after its start, and the nginx workers run as `nginx` | The rule `user` (the effective user of the main process, in the kernel's view); `ops/tests/test-ollama-user.sh`; CIS-001 row 4.1 | Root runs the nginx master (it binds 80 and 443), the socket proxy (it opens the socket), the entrypoint of PostgreSQL before it drops, and the one-shot owners, each without a capability outside its row; one of them that escapes is root of the VM | Keep, with user namespaces off, as [below](#user-namespaces-and-rootless-docker) (S01, MIL-082) |
| Exhaustion of memory, processes or disk | Memory, CPU and PID limits on every service; logs limited by the daemon (5 files of 10 MB per container); a read-only root, so only the volumes and `tmpfs` grow, and `tmpfs` counts against the memory limit | The rule `limits`; CIS-001 rows 5.10, 5.11 and 5.28; the alert `DiskAlmostFull` | A volume can fill the disk of the VM (the database, the models, the results of the benchmark); `DiskAlmostFull` warns first | Keep (S01, MIL-023) |
| The networks and the ports of the host | One network per relation (ADR-0039), `icc: false`, `userland-proxy: false`. The firewall of the VM drops what a container sends to the host itself, except 80 and 443, so SSH (22, on the tailnet only) and the other services of the host are out of reach. Only nginx (80, 443) and the relay (5432, on the tailnet) publish a port | The networks and ports in each line of the check; `ops/tests/test-service-networks.sh`; CIS-001 row 4.3.8; [Network Isolation](Network-Isolation) | 80 and 443 of the host answer a container: nginx, as for any visitor | Keep (S01, MIL-061) |
| The secrets of other containers | Secrets are files under `/run/secrets`, each container mounts only its own, and none is an environment variable | The secret check of every deploy ("has no secret in its environment", ADR-0024); CIS-001 row 4.10 | A process with the Docker API or with root of the VM reads them all | Keep (S01, ADR-0024) |

## The host-access map

What each container can reach, as the runtime check read it. No container has a device or shares a namespace of the host (the rules `device` and `host_namespace`), and each one has a read-only root, `no-new-privileges` in force, seccomp in filter mode, and memory, CPU and PID limits.

**`nf-hotel`**, from the check in the CD run 43513939028, the deploy of 052c163 on 2026-11-13: AppArmor `docker-default` in enforce mode for every container; Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2.

| Container | Service (folder) | User | Capabilities | Host paths (read-only) | Docker socket | Networks | Published ports |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `nfhotel-nginx-nginx-1` | `nginx` (`nginx`) | 0, the workers `nginx` | `CHOWN`, `DAC_READ_SEARCH`, `SETGID`, `SETUID`, `NET_BIND_SERVICE` | none | none | `nfhotel-api`, `nfhotel-proxy` | 80, 443 |
| `nfhotel-web-nfhotel-web-1` | `nfhotel-web` (`nfhotel-web`) | 1654 | none | none | none | `nfhotel-api`, `nfhotel-proxy`, `nfhotel-python` | none |
| `nfhotel-api-nfhotel-api-1` | `nfhotel-api` (`nfhotel-api`) | 1654 | none | none | none | `nfhotel-api`, `nfhotel-db`, `nfhotel-python` | none |
| `nfhotel-postgres-postgres-1` | `postgres` (`postgres`) | 70 | `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `SETGID`, `SETUID` | none | none | `nfhotel-db` | none |
| `nfhotel-postgres-tailnet-relay-1` | `relay` (`postgres-tailnet`) | 99 | none | none | none | `nfhotel-db`, `nfhotel-postgres-tailnet` | 5432, on the tailnet |
| `market-analysis-market-analysis-api-1` | `market-analysis-api` (`market-analysis`) | 10001 | none | none | none | `nfhotel-llm`, `nfhotel-python` | none |
| `benchmark-benchmark-1` | `benchmark` (`benchmark`) | 10001 | none | none | none | `nfhotel-llm`, `nfhotel-python` | none |
| `ollama-ollama-1` | `ollama` (`ollama`) | 10001 | none | none | none | `nfhotel-llm` | none |
| `nfhotel-cloudflared-cloudflared-1` | `cloudflared` (`cloudflared`) | 65532 | none | none | none | `nfhotel-proxy` | none |
| `nfhotel-alloy-alloy-1` | `alloy` (`alloy`) | 473 | none | `/`, `/proc`, `/sys`, `/var/log/journal`, `/etc/machine-id` | seen through `/`, cannot open it | `nfhotel-alloy_egress`, `nfhotel-alloy_internal`, `nfhotel-api` | none |
| `nfhotel-alloy-socket-proxy-1` | `socket-proxy` (`alloy`) | 0 | none | none | mounted, behind the allowlist of ADR-0042: from inside Alloy the list and the networks answer 200, archive, export and attach 403 | `nfhotel-alloy_internal` | none |

The one-shot services `results-owner` and `models-owner` exit before their service starts, so the check does not find them running; their profiles hold them when they run (`CHOWN` and `DAC_READ_SEARCH`, no network).

**`nf-monitoring`**: the check reads its six containers at every deploy of the monitoring (`deploy-monitoring.sh --apply`), and the job Ops monitoring configuration prints the same lines for the stack it starts on every pull request. Until the next deploy of the monitoring reads them on the LXC, the rows below are from that job for 052c163 on 2026-11-13 (Ops CI run 43512388551): the same compose file started by `deploy-monitoring.sh --apply` and checked against the same profiles, "6 passed, 0 failed, 0 warned"; AppArmor `docker-default` in enforce mode; the runner had Docker Engine 28.0.4, containerd v2.4.2 and runc 1.5.2. On the LXC the three published ports are bound to its tailnet address (`MONITORING_BIND`), and `blackbox` runs as root until that deploy, as it did before MIL-080.

| Container | Service (folder) | User | Capabilities | Host paths (read-only) | Docker socket | Networks | Published ports |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `nfhotel-monitoring-prometheus-1` | `prometheus` (`monitoring`) | 65534 | none | none | none | `nfhotel-monitoring_default` | 9090, on the tailnet |
| `nfhotel-monitoring-alertmanager-1` | `alertmanager` (`monitoring`) | 65534 | none | none | none | `nfhotel-monitoring_default` | none |
| `nfhotel-monitoring-blackbox-1` | `blackbox` (`monitoring`) | 65534 | none | none | none | `nfhotel-monitoring_default` | none |
| `nfhotel-monitoring-loki-1` | `loki` (`monitoring`) | 10001 | none | none | none | `nfhotel-monitoring_default` | 3100, on the tailnet |
| `nfhotel-monitoring-grafana-1` | `grafana` (`monitoring`) | 472 | none | none | none | `nfhotel-monitoring_default` | 3000, on the tailnet |
| `nfhotel-monitoring-scan-exporter-1` | `scan-exporter` (`monitoring`) | 10001 | none | none | none | `nfhotel-monitoring_default` | none |

## User namespaces and rootless Docker

Both would make root inside a container an unprivileged user of the VM, so that an escape from the nginx master, the socket proxy or PostgreSQL would not land as root. Evaluated again in MIL-082, both stay off (S01, MIL-082):

| Option | What it gives | What it costs here |
| --- | --- | --- |
| User namespaces (`userns-remap` in `daemon.json`) | Root in every container becomes a user of a range such as 100000 to 165535 of the VM | Every named volume and every host folder of a container is given to the mapped users, with an outage of every service, and a restore has to know the mapping. The socket proxy needs root of the VM to open the socket, and Alloy needs the host's journal group, so both would run with `userns_mode: host`: the two containers with the most access keep it, and the gain is the nginx master and PostgreSQL |
| Rootless Docker | The daemon itself runs as a user, so neither a container nor the daemon is root of the VM | The networking changes: no `DOCKER-USER` chain, ports 80 and 443 need a lower `ip_unprivileged_port_start` or a capability for the port driver, and with the default port driver nginx no longer sees the address of the client. The networks of ADR-0039, the firewall, the deploy and the backup all change, and the socket proxy still gives the whole API of that daemon |

What would change the decision:

- a container that must run as root and parses input from outside; today the nginx master only binds the ports and starts the workers, which parse the requests as `nginx`;
- an escape in `runc`, containerd or the kernel that the steps below cannot fix in time;
- a second tenant on the VM, or a service that runs code that NF Hotel neither wrote nor reviewed.

The cheaper step comes first: the nginx master as a user (`nginxinc/nginx-unprivileged` on 8080 and 8443, CIS-001 row 4.1, Deferred) would remove the one long-running root container that faces the internet.

## When a fix for an escape is published

`unattended-upgrades` installs Debian's security updates, the kernel among them, but not Docker Engine, containerd and `runc`, which come from Docker's own repository on both hosts, the VM and the monitoring LXC ([Host Hardening](Host-Hardening#reboots), ADR-0043). A person watches and installs them:

1. **Know what runs.** The last lines of each run of the check give the versions of Docker Engine, containerd and `runc`, and Prometheus has those of `nf-hotel` from the timer as the labels of one series:

   ```promql
   nfhotel_container_runtime_info{host="nf-hotel"}
   ```

   The monitoring LXC prints the same line at each deploy of the monitoring.
2. **Know what is fixed.** The security advisories of `opencontainers/runc`, `containerd/containerd` and `moby/moby` on GitHub name the versions that fix an escape; the Debian security tracker does the same for the kernel. The runtime check holds `runc` to a floor, `RUNC_FLOOR` in `ops/scripts/check-container-runtime.sh` (MIL-083): the first version of each line without the escapes that apply to NF Hotel, today 1.2.8 and 1.3.3 for CVE-2025-31133, CVE-2025-52565 and CVE-2025-52881. When an advisory applies to NF Hotel, its fixed versions go into the floor with the unit test at once: the check of the timer then fails, and `ContainerRuntimeKnownEscape` fires, until the VM is updated, and the deploy of the monitoring stops until the LXC is. The deploy of the VM only warns, because neither a release nor its rollback changes the runtime.
3. **Install.** In the monthly window, or within a week when the advisory is critical or the escape needs nothing that NF Hotel does not already do. On `nf-hotel`, as root:

   ```bash
   apt-get update
   ```

   ```bash
   apt-get install --only-upgrade docker-ce docker-ce-cli containerd.io
   ```

   `live-restore` keeps the containers running while the daemon restarts. `runc` is used when a container starts and when `docker exec` enters it, so the fixed version applies from the next start; a reboot, which a kernel update needs anyway, starts every container with it.
4. **Check.** The check prints the new versions and passes every container, and `nfhotel_container_runtime_info` shows them within 15 minutes:

   ```bash
   sudo nfhotel-container-check --profiles /opt/nfhotel/ops/security/container-profiles.tsv
   ```

**In the monitoring LXC** the same packages come from the same repository (ADR-0043). S03 first takes a snapshot of the LXC on the Proxmox node, the way back when the new runtime does not start there; then the packages are updated inside the LXC with the same two commands, and `deploy-monitoring.sh --apply` runs the check ([Monitoring](Monitoring#change-the-monitoring)). The node must run Proxmox VE 9.1.1 or later: users reported that the AppArmor rules of older versions refused the writes of a current `runc` to `/proc` in an unprivileged LXC, so no container started. The kernel of the LXC is the kernel of the node, which the updates of the node cover.

**The first case, runc 1.5.3.** It was published on 2026-11-12; the VM ran runc 1.5.2 on 2026-11-13, and the monitoring LXC has run 1.5.3 since its switch to Docker's packages on 2026-11-14 (MIL-083). It fixes CVE-2026-119101, which lets a crafted image make `runc restore` mount a directory of the host into the container; only a host that checkpoints and restores containers is affected, and NF Hotel does neither. It also keeps the flags `nosuid`, `nodev`, `noexec` and `nosymfollow` of the root mount when it makes the root read-only without a user namespace, as every container here runs; `no-new-privileges` stops a setuid program and Docker's device cgroup a device node either way. So it is not critical for NF Hotel, does not go into the floor, and is installed on the VM in the next monthly window (S01, with S03 for the reboot). Docker's repository has had `containerd.io` 2.4.2-4, which carries it, since 2026-11-12; the line of the check after the update shows the version.
