# MIL-083 The monitoring LXC patched as the VM is

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-083 |
| CrossReference | [BC-001], [ADR-0020], [CIS-001], [MIL-023], [MIL-080], [MIL-082] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the finding of 2026-11-14 in the deploy of the monitoring (a `runc` with three known container escapes, no unattended security updates), planned at S01's word in chat; S01 accepted the plan in chat | [742af62] |

---

## Purpose

Decide how the monitoring Linux container (LXC) `nf-monitoring` gets its container runtime and its security fixes, so that it runs no `runc` with a known container escape and installs Debian's security updates unattended, as the virtual machine (VM) does.

On 2026-11-14 the deploy of the monitoring (the condition of [MIL-080] and [MIL-082]) was run in the LXC over Tailscale SSH, and the artificial intelligence (AI) assistant read its packages:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | The LXC runs the Docker packages of Debian 13: Docker Engine 26.1.5, containerd 1.7.24 and `runc` 1.1.15. The Debian security tracker lists this `runc` as affected by CVE-2025-31133, CVE-2025-52565 and CVE-2025-52881, the escapes of November 2025 through the mounts that `runc` sets up for a container and its writes to `/proc`, fixed upstream in `runc` 1.2.8 and 1.3.3; Debian 13 has no fixed version | The LXC (`dpkg-query`, `apt-cache policy runc`); step 2 of `ops/scripts/create-monitoring-lxc.sh` installs `docker.io` and `docker-compose` | A container that a crafted image or crafted mounts let out reaches the LXC as root, and a rebuilt LXC gets the same runtime |
| 2 | `unattended-upgrades` is not installed in the LXC. The VM has it since [MIL-023]: the Debian security origin only, no automatic reboot | The LXC; [CIS-001] row 1.2.2.1 is verified on the VM only | Debian's security updates reach the LXC only when someone installs them by hand |
| 3 | The wiki page Container-Escape says that in the LXC "the same packages are updated inside the LXC", and [CIS-001] row 1.2.2 covers the VM only | `docs/wiki/Container-Escape.md`, [CIS-001] | The records describe a way to patch the LXC that it does not have |

An attack needs a container started from a crafted image or with crafted mounts. The LXC starts only the pinned images of its own compose file, and it is unprivileged, so its root is an unprivileged user of the Proxmox node: the escape reaches the LXC, with the data and the secrets of the monitoring, not the node. Until the fix is installed, this document, its review and its issues stay on a local branch, because the repository is public; they are pushed, and the issues synced, after task 3.

## Deliverable

- **The decision (ADR-0043, new).** Where the container runtime of the LXC comes from and how its fixes are installed: Docker's signed repository as on the VM, compared with the Debian packages and a newer `runc` from a later Debian release, and with keeping them under a recorded risk. It names what the Proxmox node needs for a current `runc` in an unprivileged LXC (users reported in November 2025 that the node's AppArmor rules refused the new writes of `runc` to `/proc` until Proxmox VE 9.1.1) and one update path for both hosts.
- **The LXC on Docker's packages.** Docker Engine, containerd and `runc` in the current versions of Docker's repository, the line the VM runs, with the four volumes of the monitoring and their data kept. `unattended-upgrades` with the Debian security origin and no automatic reboot, as on the VM. A snapshot of the LXC on the Proxmox node before the change is the way back.
- **The script that builds the LXC.** `ops/scripts/create-monitoring-lxc.sh` installs the same packages from the same signed repositories and `unattended-upgrades`, so a rebuilt LXC is patched as this one is; its test `ops/tests/test-create-monitoring-lxc.sh` changes first.
- **A floor for `runc` in the runtime check.** The runtime check of [MIL-080] fails a host whose `runc` is older than the first version of its line without these three escapes, so the deploy of either host stops on such a runtime, and the check every 15 minutes on the VM raises its alert.
- **The records.** The wiki pages Container-Escape (how the LXC gets its fixes), Monitoring and Host-Hardening, and [CIS-001] rows 1.2.2 and 1.2.2.1 for the LXC.

Order: S01 accepts this plan in chat; task 1; task 2 (S03); task 3, the change in the LXC; tasks 4 to 6 on one branch, pushed with this document after task 3; the deploy of the merge (the AI approves its CD run, as this plan says); task 7; then the gate of [MIL-082], with the host-access rows of the LXC read in the deploy of task 3.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0043 is Accepted with a Go review and names the source of the runtime, the requirement of the node and the update path of both hosts | Yes | No |
| 2 | In the LXC, `dpkg-query` shows `docker-ce`, `containerd.io` and `docker-compose-plugin` and no Debian `docker.io`, `containerd` or `runc`; `apt-cache policy` shows Docker's signed repository; `runc --version` is at or above the floor of criterion 5 | Yes | No |
| 3 | After the change, `deploy-monitoring.sh --apply` exits 0, the runtime check passes the six containers and prints the new versions, the four volumes are there, Prometheus answers a query for samples written before the change, and the targets of the VM are up | Yes | No |
| 4 | `unattended-upgrades` is installed in the LXC with the Debian security origin and no automatic reboot, and its log shows a run of the daily timer | Yes | No |
| 5 | In their tests, the runtime check fails a `runc` below the floor, passes the versions of the VM and the LXC and warns on an unknown one, and `create-monitoring-lxc.sh` installs the packages of criterion 2 and `unattended-upgrades` | Yes | No |
| 6 | The wiki pages and [CIS-001] say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-SH-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-080 | The runtime check that reads the containers and prints the versions; task 5 adds the floor to it |
| MIL-082 | Its gate waits for this milestone: the host-access rows of the LXC are read in the deploy of task 3, and the page Container-Escape is corrected here |
| ADR-0020 | The monitoring in an unprivileged LXC with nesting and keyctl, in which the new runtime has to work |
| MIL-023 | The patching of the VM (`unattended-upgrades`, the monthly window) that the LXC follows |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25: harden and observe the server | Criteria 2, 3 and 5: no runtime with a known escape on either host, and the deploy stops on one |
| BC-001 success criterion 28: hardened server; security updates install unattended | Criteria 4 and 6: `unattended-upgrades` in the LXC, [CIS-001] rows 1.2.2 and 1.2.2.1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 (the AI as stand-in, autonomous mode, after S01 has accepted the plan in chat) |
| The snapshot of the LXC on the Proxmox node | S03 (held by S01) |

## Target Date

2026-11-16: the change in the LXC once S03 has taken the snapshot, a run of `unattended-upgrades` the morning after, and the gate; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The decision | ADR-0043: Docker's signed repository for the runtime of the LXC, as on the VM, against the Debian packages with a newer `runc` from a later Debian release and against keeping them under a recorded risk; what the Proxmox node needs for a current `runc` in an unprivileged LXC; one update path for both hosts (Debian's security updates unattended, Docker's packages in the monthly window or within a week for a critical escape). Its review record. | No | criterion 1 |
| 2 | The snapshot of the LXC | S03 on the Proxmox node: `pveversion` for the requirement of task 1, and a snapshot of LXC 102 (`pct snapshot 102 before-mil-083`), the way back if task 3 fails. The AI cannot log in to the node. | No | criteria 2, 3 |
| 3 | Docker's packages and unattended-upgrades in the LXC | The AI over Tailscale SSH (S01's allow rule of 2026-11-14): the state before (packages, volumes, the oldest sample in Prometheus); the Debian `docker.io`, `docker-cli`, `containerd`, `runc` and `docker-compose` removed with the volumes kept; Docker's signed repository added and `docker-ce`, `docker-ce-cli`, `containerd.io` and `docker-compose-plugin` installed; `unattended-upgrades` as on the VM; `deploy-monitoring.sh --apply` and the checks of criterion 3. When a step fails, the AI stops and S03 rolls back to the snapshot. | No | criteria 2, 3, 4 |
| 4 | The script that builds the LXC | Step 2 of `create-monitoring-lxc.sh` installs Docker's packages from the signed repository and `unattended-upgrades`, as task 3 did by hand; the change to `ops/tests/test-create-monitoring-lxc.sh` comes first. | No | criterion 5 |
| 5 | A floor for runc in the runtime check | The unit test first: a `runc` older than the first version of its line that fixes CVE-2025-31133, CVE-2025-52565 and CVE-2025-52881 fails the check, the versions of the VM and the LXC pass, and an unknown version warns. Then the rule in `ops/scripts/check-container-runtime.sh`, with the advisory it comes from. | No | criterion 5 |
| 6 | The records, code review and pull request | The wiki pages Container-Escape, Monitoring and Host-Hardening, [CIS-001] rows 1.2.2 and 1.2.2.1, the review record against `QC-SH-001`, and the pull request, which closes one issue per task it completes. It is pushed after task 3, with this document, its review and the issues synced then. | No | criterion 6 |
| 7 | Gate and review record | The gate review record against criteria 1 to 6, the row of MIL-083 in the traceability matrix, the Accepted row of this document and the closing of the GitHub milestone; then the gate of [MIL-082]. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[CIS-001]: ../sqa/cis-checklist.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-080]: ./mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-082]: ./mil-082-a-compromised-container-kept-away-from-the-host.md
[742af62]: https://github.com/NF-Hotel/NFHotel/commit/742af62489a1876a93666e37724573d01cc573c3
