# ADR-0043 Docker's packages and Debian's security updates in the monitoring LXC

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0043 |
| CrossReference | [MIL-083], [ADR-0020], [MIL-023], [MIL-080], [MIL-082], [CIS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-083, planned in autonomous mode at S01's word in chat on 2026-11-14 (RC-352 Go); written by the AI; accepted after RC-353 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** On 2026-11-14 S01 asked in chat for a milestone for what the deploy of the monitoring had shown that morning ("Ja, planlæg den"), and accepted its plan, with the approval of the continuous deployment (CD) run of its merge by the artificial intelligence (AI) assistant ("Ja, godkend planen"). [MIL-083] is that plan, reviewed by the AI as a stand-in for S01 (RC-352, Go). This record is task 1 of the milestone; S01 can challenge it in the summary of the run.

**The problem.** The monitoring runs in an unprivileged Linux container (LXC) on the Proxmox node ([ADR-0020]), with Docker Compose. Step 2 of `ops/scripts/create-monitoring-lxc.sh` installed the Docker packages of Debian 13 (`docker.io`, `docker-compose`): Docker Engine 26.1.5, containerd 1.7.24 and `runc` 1.1.15. The Debian security tracker lists that `runc` as affected by CVE-2025-31133, CVE-2025-52565 and CVE-2025-52881, three container escapes of November 2025 through the mounts that `runc` sets up for a container and its writes to `/proc`. The advisories of `opencontainers/runc` name the fixed versions 1.2.8 and 1.3.3; Debian 13 has none. `unattended-upgrades` was not installed in the LXC, so no security update of Debian reached it without a person. The virtual machine (VM) `nf-hotel` already takes its runtime from Docker's signed repository (`ops/scripts/create-docker-vm.sh`) and installs Debian's security updates every day ([MIL-023]).

**Forces.**

- An escape needs a container started from a crafted image or with crafted mounts. The LXC starts only the pinned images of its compose file, and it is unprivileged, so an escape reaches the LXC, with the data and the secrets of the monitoring, and not the node. The risk is lower than on the VM, not absent.
- The LXC runs with `nesting` and `keyctl` ([ADR-0020]). Its kernel is the node's, and the node gives it an AppArmor profile. A `runc` with these fixes opens its files under `/proc` in a new way; users of Proxmox VE reported in November 2025 that the AppArmor rules of the node refused those writes in an unprivileged LXC, so no container started there, until Proxmox VE 9.1.1.
- The monitoring keeps its data in four named volumes (`prometheus-data`, `alertmanager-data`, `loki-data`, `grafana-data`) under `/var/lib/docker`, which the packages of Debian and of Docker both use. A removal of a package, not a purge, keeps them.
- `unattended-upgrades` installs packages of the Debian security origin only. The runtime from Docker's repository is installed by a person on purpose: an update of Docker Engine restarts the daemon, and the monthly window has S03 at hand.
- The wiki page Container-Escape ([MIL-082]) gives the steps that install a fix of an escape on the VM, and the runtime check of [MIL-080] prints the versions of both hosts. One source and one set of steps for both hosts means that an advisory is read once.
- The repository is public: the change is made before its records are pushed.

Options considered:

1. **Keep Debian's packages under a recorded risk,** until Debian 13 has a fixed `runc`. The three escapes stay for an unknown time, and a rebuilt LXC keeps getting them.
2. **Debian's packages with a `runc` from a later Debian release** (forky has a fixed one). It mixes two releases of Debian in one system, the package of the testing release has no support of Debian's security team, and Docker Engine 26 stays, a line that Docker no longer maintains.
3. **Docker's signed repository, as on the VM:** `docker-ce`, `docker-ce-cli`, `containerd.io`, which carries `runc`, and `docker-compose-plugin`, in the versions the VM runs.
4. **Another runtime from Debian,** such as Podman with `crun`. The deploy of the monitoring, the runtime check and the socket proxy of Alloy are written for the application programming interface (API) of Docker Engine; all would change.
5. **The monitoring in a VM.** [ADR-0020] chose the LXC for its small footprint; a VM would still need a runtime from somewhere, so the question stays.

## Decision

We follow option 3: the monitoring LXC takes Docker Engine, containerd and `runc` from Docker's signed repository, as the VM does, and installs Debian's security updates unattended, as the VM does.

1. **The source.** Docker's key in `/etc/apt/keyrings/docker.asc` and its list for the Debian release of the LXC, as `create-docker-vm.sh` writes them for the VM; the packages `docker-ce`, `docker-ce-cli`, `containerd.io` and `docker-compose-plugin`. Debian's `docker.io`, `docker-cli`, `containerd`, `runc` and `docker-compose` are removed, not purged, so `/var/lib/docker` and the four volumes stay.
2. **Debian's security updates.** `unattended-upgrades` with the files of the VM, installed by the same script, `ops/scripts/install-patching.sh --apply`, run in the LXC from its checkout: the Debian security origin only, every day, no automatic reboot. The LXC has no kernel of its own; the updates of the node (S03) cover it.
3. **One update path for Docker's packages on both hosts.** A person installs them with the steps of the page Container-Escape, in the monthly window, or within a week when the advisory is critical for NF Hotel. In the LXC, S03 first takes a snapshot of it on the node, the way back when the new runtime does not start there; the deploy of the monitoring then runs the runtime check.
4. **What the node needs.** A current `runc` in the unprivileged LXC needs Proxmox VE 9.1.1 or later on the node. A node below it is updated first (S03). The LXC gets no weaker AppArmor profile, and Docker's packages are not held at a version with a known escape to get around it.
5. **A floor for `runc`.** The runtime check knows the first version of each line of `runc` without the escapes that apply to NF Hotel: for the three of November 2025, 1.2.8 for the line 1.2 and 1.3.3 for the line 1.3; a line below the oldest row fails, a line above the newest passes. A `runc` below the floor fails the check, and a version that the check cannot read is a warning. In the deploy of the VM (`--deploy`) it is a warning, as a container outside the release is: the release cannot change the runtime, and neither can its rollback; the check of the timer fails every 15 minutes, and its alert fires, until the runtime is updated. The deploy of the monitoring runs the check without `--deploy`, so it stops on such a runtime. A new escape that applies to NF Hotel adds its fixed versions to the floor, with its test, when its advisory is published.
6. **The script that builds the LXC.** `create-monitoring-lxc.sh` installs the same packages from the same repository and `unattended-upgrades`, so a rebuilt LXC is patched as this one is.

## Consequences

**Positive:**

- Neither host runs a `runc` with the three escapes, and a rebuilt LXC does not bring them back.
- One source, one set of steps and one check for the runtime of both hosts; the LXC runs the line of the VM, so an advisory is read once.
- Debian's security updates reach the LXC every day without a person.
- A runtime below the floor no longer depends on a person who reads the line of the versions: the deploy of the monitoring stops, and the alert of the VM fires.

**Negative:**

- Docker's packages are still installed by a person, now on two hosts, and the one of the LXC needs S03 for the snapshot. The floor knows only the escapes it lists; a person still reads the advisories and changes it.
- A third repository and its signing key in the LXC: a compromised repository of Docker reaches the LXC as it reaches the VM.
- The runtime of the LXC depends on the AppArmor rules of the node: an update of `runc` or of the node can stop the containers of the monitoring again. The deploy shows it, the watchdog of [ADR-0020] reports a dead monitoring, and the snapshot is the way back.
- The switch stops the monitoring for some minutes, and Prometheus has no samples of them. Docker Engine moves from 26 to the line of the VM, and the deploy pulls the images again.
- A package of a distribution that backports the fixes into an older line, as Debian may do for 1.1.15, fails the floor. NF Hotel uses none; one would need the floor changed with its test.
- In the deploy of the VM the floor is only a warning: a release deploys on a runtime below it, and the alert is the control.

**Quality:** security improves (resistance and integrity of the LXC against a container escape with a known fix); reliability of the monitoring now depends on the AppArmor rules of the node, with a snapshot as the way back (availability, recoverability); maintainability improves with one update path and a check that tests it (analysability, testability) (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-083]: tasks 3 to 6 carry out this record.
- [ADR-0020]: the LXC of the monitoring takes its runtime from Docker's repository and installs Debian's security updates unattended.
- [MIL-023]: the patching of the VM covers the LXC too.
- [MIL-080]: the runtime check gets the floor of `runc` and its alert.
- [MIL-082]: the page Container-Escape says how the LXC gets its fixes.
- [CIS-001]: rows 1.2.2 and 1.2.2.1 cover the LXC.

---

[MIL-083]: ../milestones/mil-083-the-monitoring-lxc-patched-as-the-vm-is.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[MIL-080]: ../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-082]: ../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[CIS-001]: ../sqa/cis-checklist.md
