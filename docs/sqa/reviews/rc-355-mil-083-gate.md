# RC-355 Gate review of MIL-083 The monitoring LXC patched as the VM is

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-355 |
| CrossReference | [MIL-083], [ADR-0043], [RC-352], [RC-353], [RC-354], [CIS-001], [MIL-080], [MIL-082], [RC-350], [RC-351], [ADR-0038], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-083 on the evidence of 2026-11-14; mode autonomous, not independent; Go | [38e03ae] |

---

## Artifact Under Review

- Instance reviewed: [MIL-083] at its gate: the six Go/No-Go criteria, with the evidence of the switch of the monitoring Linux container (LXC) on 2026-11-14 (task 3), of pull request #689 (merged as fb36be8 on 2026-11-14 at 07:59 UTC), of the release run 43573972277 and the continuous deployment (CD) run 43574446338 of that commit, and of the deploy of the monitoring from `main` on `nf-monitoring`. Before the gate: [RC-352] (the plan), [RC-353] (ADR-0043) and [RC-354] (the code, Go-with-conditions).
- Checklist used: the Go/No-Go criteria of [MIL-083]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 2 to 4 cite the evidence of task 3 in [RC-354] and read it again after the merge.
- Language and domain: en / it (the Metadata rows of MIL-083); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. On 2026-11-14 S01 asked in chat for this milestone ("Ja, planlæg den"), accepted its plan with the approval of the CD run of its merge by the artificial intelligence (AI) assistant ("Ja, godkend planen"), and asked the AI to take the snapshot of task 2 itself, in S01's terminal and browser ("ta selv snapshottet", "du har adgang via min terminal og browser"). On 2026-11-09 S01 asked that the open milestones be finished now, not at a gate scheduled for a later date ("du skal færdiggøre dem der er på nuværende tidspunkt").

**Evidence of 2026-11-14:**

- **The switch of the LXC (task 3).** As [RC-354] records it: the monitoring was stopped at 07:23 UTC; the snapshot `before-mil-083` of LXC 102 was taken at 07:28 UTC on a node with Proxmox VE 9.2.11, which meets the requirement of ADR-0043 (9.1.1 or later); the Debian packages were removed and the four volumes kept; Docker's repository was added once its key's fingerprint had been compared; and Docker's packages were installed, with Debian's `docker-buildx` removed as well (finding F1 of [RC-354]). `deploy-monitoring.sh --apply` exited 0 at 07:34 UTC, and `install-patching.sh --apply` installed `unattended-upgrades` 2.12. No rollback was needed.
- **The pull request.** #689: on its last commit 5152634, 54 checks passed and 2 were skipped by design (`DAST full scan` and `OWASP ZAP`, which run by hand and on schedule). In Ops CI run 43573063079, the matrix job `Ops script tests` passed `test-check-container-runtime.sh` 119 of 119 and `test-create-monitoring-lxc.sh` 15 of 15; `Ops wiki coverage` passed 116 of 116; `Ops monitoring configuration (MIL-023)` passed `promtool check config` and `promtool test rules`, with the test of `ContainerRuntimeKnownEscape`; and `Ops linters` passed. The merge closed #682 to #687; #688 stays open for this gate.
- **The release and the deploy of the VM.** Release run 43573972277 of fb36be8 built the six release images (`nfhotel-web`, `nfhotel-api`, `market-analysis`, `benchmark`, `nfhotel-nginx` and `nfhotel-socket-proxy`), passed each through the scan gate between 08:04 and 08:06 UTC, pushed them and attested their build provenance and SBOM by 08:09 UTC. CD run 43574446338, approved by the AI at 08:09 UTC under S01's words above ([ADR-0038] gives the approval to S01, who gave it to the AI for this run), ended with success; its deploy step ran from 08:09 to 08:13 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest. The update command installed the operations commands of fb36be8 ("the commands match fb36be8…"), so the timer of the VM runs the check with the floor. The deploy took a backup of the database before the migrations and another before the deploy (each copy to the backup target failed, and the backups stayed on the VM, as finding 3 of [RC-351] reports), applied the migrations as `nfhotel_migrator` and started the services from the release images. Then "==> the runtime of the containers against container-profiles.tsv (MIL-080)" printed one PASS line for each of the 11 containers of the VM and "==> 11 container(s): 11 passed, 0 failed, 0 warned", then "==> Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2; AppArmor on, seccomp on" and "==> runc 1.5.2: at or above the floor of its line". The smoke test answered 301 on port 80 and 200 on port 443, the release was recorded ("==> deployed fb36be8…"), and the container check service passed once.
- **The monitoring from `main`.** Over Tailscale SSH at 08:03 UTC, `git pull --ff-only` moved the checkout on `nf-monitoring` from 0432281 to fb36be8, the merge of #689, and `bash ops/scripts/deploy-monitoring.sh --apply` exited 0 at 08:04 UTC. It recreated Prometheus for its new rule and left the other five containers running. The check passed the six containers ("6 passed, 0 failed, 0 warned") and printed "Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.3; AppArmor off, seccomp on" and "==> runc 1.5.3: at or above the floor of its line".
- **Prometheus.** At 08:04 UTC `/api/v1/rules` of Prometheus on `nf-monitoring` listed 27 alert rules, among them `ContainerRuntimeKnownEscape` (critical) in the group `nfhotel-security`, with health `ok`, inactive, last evaluated at 08:04:37 UTC. 10 of 10 targets were up, and the only alert that fired was `Watchdog`, which always fires. At 08:13:55 UTC, 46 seconds after the container check service of the deploy had run on the VM, Prometheus had `nfhotel_container_runtime_runc_below_floor{host="nf-hotel"}` 0, `nfhotel_container_runtime_failed` 0 for 11 containers, and `nfhotel_container_runtime_info` with `runc` 1.5.2: the check of the VM reports its floor, and `ContainerRuntimeKnownEscape` stays inactive.
- **The daily timer in the LXC.** `systemctl list-timers` gave the last run of `apt-daily-upgrade.timer` at 06:25:40 UTC and the next at 06:26:42 UTC on 2026-11-15. Its journal shows the timer starting `apt-daily-upgrade.service` every morning: at 06:37 UTC on 2026-11-12, at 06:52 UTC on 2026-11-13 and at 06:25 UTC on 2026-11-14, before `unattended-upgrades` was installed, when the unit had nothing to install. Started by hand at 07:37 UTC, the same unit ran `unattended-upgrade`, and `/var/log/unattended-upgrades/unattended-upgrades.log` reads "Allowed origins are: origin=Debian,codename=trixie-security,label=Debian-Security" at 07:37:39 UTC and "All upgrades installed" at 07:38:24 UTC, for the 31 security updates that were waiting.

## Checklist Results

| # | Criterion (MIL-083) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0043 is Accepted with a Go review and names the source of the runtime, the requirement of the node and the update path of both hosts | Pass | ADR-0043 is Accepted with [RC-353] Go. Its decision names Docker's signed repository for Docker Engine, containerd and `runc` on both hosts (point 1), Proxmox VE 9.1.1 or later on the node for a current `runc` in an unprivileged LXC (point 4), and one update path: Debian's security updates unattended on both hosts, and Docker's packages in the monthly window, or within a week for a critical advisory, after a snapshot of the LXC (points 2 and 3) |
| 2 | In the LXC, `dpkg-query` shows `docker-ce`, `containerd.io` and `docker-compose-plugin` and no Debian `docker.io`, `containerd` or `runc`; `apt-cache policy` shows Docker's signed repository; `runc --version` is at or above the floor of criterion 5 | Pass | Criterion 2 of [RC-354]: Docker's `docker-ce`, `docker-ce-cli`, `containerd.io`, `docker-compose-plugin`, `docker-buildx-plugin` and `docker-ce-rootless-extras`, no Debian `docker.io`, `docker-cli`, `containerd`, `runc`, `docker-compose` or `docker-buildx`; `https://download.docker.com/linux/debian trixie/stable` signed with the compared key; `runc` 1.5.3. The deploy from `main` read it again: "runc 1.5.3: at or above the floor of its line" |
| 3 | After the change, `deploy-monitoring.sh --apply` exits 0, the runtime check passes the six containers and prints the new versions, the four volumes are there, Prometheus answers a query for samples written before the change, and the targets of the VM are up | Pass | At 07:34 UTC: exit 0, 6 of 6 containers, "Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.3", the storage driver `overlay2` and the four volumes; `count(up)` answered 10 for 07:23:15 UTC, before the change, and the oldest sample was the same as before; 10 of 10 targets up, among them the jobs of the VM. Again at 08:04 UTC from `main`: exit 0, 6 of 6, 10 of 10 targets up |
| 4 | `unattended-upgrades` is installed in the LXC with the Debian security origin and no automatic reboot, and its log shows a run of the daily timer | Pass | `unattended-upgrades` 2.12; `apt-config dump` shows the Debian security origin only and `Automatic-Reboot "false"` ([RC-354]). The log shows a run of the timer's unit, `apt-daily-upgrade.service`, started by hand at 07:37 UTC, with that origin and "All upgrades installed". The timer starts that unit every morning, as its journal shows; its first run with `unattended-upgrades` installed is due at 06:26 UTC on 2026-11-15 and is not yet observed (finding 1) |
| 5 | In their tests, the runtime check fails a `runc` below the floor, passes the versions of the VM and the LXC and warns on an unknown one, and `create-monitoring-lxc.sh` installs the packages of criterion 2 and `unattended-upgrades` | Pass | `test-check-container-runtime.sh` 119 of 119 and `test-create-monitoring-lxc.sh` 15 of 15 in Ops CI of #689: a `runc` below the floor fails, 1.5.2 (the VM) and 1.5.3 (the LXC) pass, a version the check cannot read warns, and the LXC script installs Docker's packages from the signed repository and `unattended-upgrades` and none of the Debian packages |
| 6 | The wiki pages and [CIS-001] say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-SH-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Pass | The wiki pages Container-Escape, Monitoring, Host-Hardening and Alerts and [CIS-001] rows 1.2.2 and 1.2.2.1 ([RC-354]); `test-wiki-coverage.sh` 116 of 116 and `Ops linters` green in Ops CI of #689. [RC-354] is Go-with-conditions, and its conditions are met: every check of #689 passed before the merge (54 passed, 2 skipped by design), the floor's line was read in the deploy of the merge on the VM and in the LXC, and the run of the daily timer is criterion 4 |

## Findings

1. **The first run of the daily timer with `unattended-upgrades` is not yet observed.** The timer had run at 06:25 UTC, before the package was installed, so its next run is on 2026-11-15 (finding F3 of [RC-354]): at 06:00 UTC with a random delay of up to an hour (`RandomizedDelaySec=60m`), which `systemctl list-timers` gave as 06:26 UTC at 08:04 UTC; [RC-354] has 06:35 UTC from an earlier read. Criterion 4 asks for a run of the daily timer in the log; the log has the run of the timer's own unit, started by hand at 07:37 UTC, and the journal shows the timer starting that unit every morning. [RC-354] planned this gate with the timer's own run; S01 asked on 2026-11-09 to finish the milestones now, without a gate scheduled for a later date, so this gate reads the criterion on the run by hand and the runs of the timer before it. This is a stand-in decision that S01 can challenge in the summary of the run. `grep -E "Starting|origins|All upgrades" /var/log/unattended-upgrades/unattended-upgrades.log` in the LXC shows the timer's run when it comes; a failed run shows there and in `journalctl -u apt-daily-upgrade.service`.
2. **The snapshot `before-mil-083` is still on the node.** It is the way back of task 3, which was not needed. The thin pool of the node is overcommitted, and the snapshot keeps the old blocks of every write of Prometheus and Loki (finding F9 of [RC-354]), so it should be removed now that the gate is Go. Its removal deletes it for good, and [MIL-083] gives the snapshot to S03 (held by S01), so the AI removes it only when S01 says so in chat.
3. **The VM runs `runc` 1.5.2 and the LXC 1.5.3.** Both are above the floor. 1.5.3 fixes CVE-2026-119101, which needs a checkpoint and restore of a container, and NF Hotel does neither (finding F5 of [RC-350]), so the floor does not name it. The VM gets 1.5.3 with Docker's packages in the next monthly window ([RC-354]), and the check of the timer of [MIL-080] reads its version every 15 minutes.

## Overall Verdict

Go: criteria 1 to 6 pass on the evidence of 2026-11-14, and the conditions of [RC-354] are met; the first run of the daily timer with `unattended-upgrades` is still to come (finding 1). #688 closes at the merge of the pull request that carries this record, and the GitHub milestone #58 is closed after it. The gate of [MIL-082] follows in the same pull request ([RC-351]). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Read the first run of the daily timer in `/var/log/unattended-upgrades/unattended-upgrades.log` of the LXC | S01 | From 2026-11-15 06:26 UTC |
| Remove the snapshot `before-mil-083` of LXC 102 | S03 (held by S01) | After this gate |
| Install `runc` 1.5.3 on `nf-hotel` with the packages of Docker's repository | S01, S03 for the reboot | The next monthly window |

---

[MIL-083]: ../../milestones/mil-083-the-monitoring-lxc-patched-as-the-vm-is.md
[ADR-0043]: ../../adr/adr-0043-docker-packages-and-security-updates-in-the-monitoring-lxc.md
[RC-352]: ./rc-352-mil-083.md
[RC-353]: ./rc-353-adr-0043.md
[RC-354]: ./rc-354-mil-083-code.md
[CIS-001]: ../cis-checklist.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-082]: ../../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-350]: ./rc-350-mil-082-code.md
[RC-351]: ./rc-351-mil-082-gate.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[38e03ae]: https://github.com/NF-Hotel/NFHotel/commit/38e03ae60dd0a00a28fff2551c5f14e6864e34cf
