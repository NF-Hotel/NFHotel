# MIL-080 Every container runs with the rights of its profile

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-080 |
| CrossReference | [BC-001], [CIS-001], [DSA-001], [ADR-0018], [ADR-0020], [MIL-023], [MIL-031] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | AI as stand-in for S01 | Initial version: S01 asked in chat for three controls (runtime security, the Docker socket, container escape) and, where one is missing, for a plan carried out in autonomous mode ("Du behøver ik spørg mig om noget"); this is the first of the three | pending |

---

## Purpose

Decide that NF Hotel knows, at every deploy and every 15 minutes, that each container runs with the rights and the security profiles it is meant to have, and that a container that does not stops the deploy and raises an alert.

On 2026-11-12 S01 asked whether all containers actually run with the expected rights and security profiles. The artificial intelligence (AI) assistant read the repository and the history of GitHub, and nothing in production:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | Every compose file drops all capabilities, sets `no-new-privileges`, a read-only root and memory, CPU and PID limits, and [CIS-001] records rows 5.1 to 5.31 as Applied | `ops/deploy/*/docker-compose.yml`, [CIS-001] | The intended rights are written down, service by service |
| 2 | The evidence on the virtual machine (VM) is a manual read: `ops/scripts/cis-evidence.sh`, run by hand on 2026-10-24, 2026-10-28 and 2026-11-05. It prints the settings and compares them with nothing. Every deploy since 2026-11-05 recreated containers, and nothing has read them again | `nf-hotel` | A container started by hand with `--privileged`, a mounted Docker socket or the host's network, or a compose change that drops a setting, would not be noticed |
| 3 | `ops/tests/check-e2e-stack.sh` with `ops/tests/lib/hardening.sh` checked every container of the throwaway stack in the CI job "End-to-end stack" of NF-Hotel/DevOps. The job did not move with the operations code into NFHotel ([MIL-031]), and no workflow or script calls the script now, although [CIS-001] cites it as the evidence of its compose rows | `.github/workflows`, `.github/actions/e2e-tests` | A pull request that removes a hardening setting passes CI |
| 4 | The six containers of the monitoring in the LXC container `nf-monitoring` have the same settings in their compose file, but their running state has never been read | `ops/deploy/monitoring/docker-compose.yml` | [CIS-001] covers the VM only |

## Deliverable

- **The profiles.** `ops/security/container-profiles.tsv`: one row for each compose service that runs on the VM, in the monitoring LXC or on the backup host. A row gives the user that the main process of the container may run as (`nonroot`, `root` or a user ID), the capabilities it may hold (the `cap_add` of its compose file), whether its root filesystem may be writable, the sensitive host paths it may mount read-only, and whether it may mount the Docker socket (only the socket proxy).
- **The check.** `ops/scripts/check-container-runtime.sh` reads every running container two ways: its configuration (`docker inspect`) and the kernel's view of its main process (`/proc/<pid>/status` and `/proc/<pid>/attr/current`: the effective user ID, the bounding set of capabilities, `NoNewPrivs`, the seccomp mode and the AppArmor profile). It compares both with the profile of the container's compose service. The rules: not privileged; `cap_drop: ALL`; no added capability outside the profile, and none in the bounding set of the main process; `no-new-privileges` set and in force; seccomp not unconfined and in filter mode; AppArmor confined, on a host that has AppArmor; a read-only root; memory, CPU and PID limits; no namespace of the host (network, PID, IPC, UTS, user); no device; no writable bind mount; no sensitive host path outside the profile; the Docker socket only in the socket proxy, and a container that sees it through a parent mount cannot open it (not root, not in the socket's group). A running container without a profile is a violation. It prints one line per container, the versions of Docker Engine, containerd and runc, and no environment and no secret. With `--metrics` it writes a text file for Prometheus. It changes nothing.
- **The deploy gate.** `deploy-release.sh` runs the check after the secret check, for the containers of the services that have a profile; a violation fails the deploy, which then rolls back like a failed smoke test. An unknown container is printed as a warning there, so a container outside the release cannot block a rollback.
- **Every 15 minutes.** The update command of continuous deployment (CD) ([ADR-0018]) installs the check as `nfhotel-container-check` with a service and a timer that run it every 15 minutes, every container included, and write `container-runtime.prom`, which Alloy sends to Prometheus. The update runs it once, so the CD log shows it.
- **The alerts.** `ContainerRuntimeViolation` when a container breaks its profile, and `ContainerCheckStale` and the task in `TaskReportMissing` when the report stops, with tests for `promtool`.
- **The monitoring LXC.** `deploy-monitoring.sh --apply` runs the check after it starts the services, and fails when a container breaks its profile.
- **The CI.** A unit test of every rule, both ways, with a stand-in for `docker` and for `/proc`; a live test on the runner that starts compliant containers and one container per broken rule, as root with real Docker; and the end-to-end action runs `check-e2e-stack.sh` and the check over the throwaway stack again, so a compose change that breaks a profile fails the pull request.
- **The records.** [CIS-001] rows 4.1 and 5.x cite the check as evidence, and the compose row cites the end-to-end action; the wiki pages Host-Hardening (the check, the profile and how to change it) and Monitoring (the alerts).
- **Not part of this milestone:** a timer in the monitoring LXC, which has no Alloy to send its report (the check runs there at every deploy of the monitoring); the backup host, a laptop, keeps its profile for a manual run.

Order: the review of this document (QC-MIL-001, QC-LANG-001), then tasks 1 to 6 on one branch with their pull request, then task 7 after the merge, and the gate last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The unit test has a compliant and a breaking case for every rule, and passes; it fails when a rule of the check is removed | Yes | No |
| 2 | The live test on the CI runner passes: the compliant containers pass, and each breaking container fails with its rule (privileged, an added capability, root where the profile says `nonroot`, a writable host mount, the Docker socket, the host's network, `seccomp=unconfined`, no `no-new-privileges`, no profile) | Yes | No |
| 3 | The end-to-end job runs `check-e2e-stack.sh` and the check over the throwaway stack, and both pass | Yes | No |
| 4 | After the deploy, the CD log shows the check over every container of the release with no violation, the release recorded, and the timer enabled with one run of its service that passed | Yes | No |
| 5 | The alert rules pass `promtool test rules` in Ops CI, and Prometheus of `nf-monitoring` has them loaded after the deploy of the monitoring; there the check passes over its six containers | Yes | No |
| 6 | [CIS-001], the wiki pages Host-Hardening and Monitoring say what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |
| 7 | The code review against `QC-SH-001` and `QC-WF-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-023 | The hardening of the compose files and the CIS checklist ([CIS-001]) whose runtime rows this milestone checks |
| MIL-031 | The move of the operations code into NFHotel, where the CI check of the throwaway stack was lost |
| ADR-0018 | The update command installs the check and its timer from a release commit on main |
| ADR-0020 | Alloy sends the report of the check to Prometheus, which alerts |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25: harden and observe the server, the CIS items applied with evidence | Criteria 4 and 5: the evidence of the runtime rows is read at every deploy and every 15 minutes, on the VM and in the monitoring LXC |
| BC-001 success criterion 28: hardened server, every selected CIS item with a status and evidence | Criteria 3, 4 and 6: the check is the evidence that [CIS-001] cites, and a change that breaks it fails CI |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 (the AI as stand-in, autonomous mode) |

## Target Date

2026-11-14. S01 asked for the three controls to be finished in one autonomous run; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The profiles | `ops/security/container-profiles.tsv` with one row per compose service of the VM, the monitoring LXC and the backup host, taken from its compose file and its image: the user of the main process, the added capabilities, a writable root or not, the sensitive host paths it may mount and the Docker socket. A comment says how to change a row. | No | criteria 1, 2 |
| 2 | The check and its unit test | The unit test first, with a stand-in for `docker` and a folder in place of `/proc`: a compliant and a breaking case for every rule. Then `ops/scripts/check-container-runtime.sh`, which reads `docker inspect` and the kernel's view of each main process, compares them with the profiles, prints one line per container and the versions of Docker Engine, containerd and runc, writes the Prometheus report with `--metrics`, and exits 1 on a violation. | No | criterion 1 |
| 3 | The live test and the end-to-end check | `ops/tests/test-container-runtime-live.sh` runs as root with real Docker in the job Ops monitoring configuration: compliant containers pass, and one container per broken rule fails with that rule. The end-to-end action runs `check-e2e-stack.sh` and the check over the throwaway stack, as the lost CI job "End-to-end stack" did. | No | criteria 2, 3 |
| 4 | The deploy gate and the timer | `deploy-release.sh` runs the check after the secret check, for the services with a profile; a violation fails the deploy and rolls back. The update command installs `nfhotel-container-check`, `nfhotel-container-check.service` and `.timer` (every 15 minutes) and runs the service once. `ops/tests/test-deploy-release.sh` covers both. | No | criterion 4 |
| 5 | The alerts and the monitoring LXC | `ContainerRuntimeViolation`, `ContainerCheckStale` and the task in `TaskReportMissing`, with `promtool` tests in `rules-test.yml`. `deploy-monitoring.sh --apply` runs the check after the services start. | No | criterion 5 |
| 6 | The records, code review and pull request | [CIS-001] (rows 4.1, 5.x and the compose evidence), the wiki pages Host-Hardening and Monitoring, the review record against `QC-SH-001` and `QC-WF-001`, and the pull request, which closes one issue per task it completes. | No | criteria 6, 7 |
| 7 | The deploy and the checks | The AI approves the CD run of the merge (S01's words of 2026-11-12) and reads the check, the recorded release and the timer's run in the CD log. Then the deploy of the monitoring on `nf-monitoring` over Tailscale Secure Shell (SSH), which loads the alerts and runs the check over the six containers there, and a query of Prometheus for the report of the VM. | No | criteria 4, 5 |
| 8 | Gate and review record | The gate review record against criteria 1 to 7 with the CD log and the answers of Prometheus, the row of MIL-080 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 7 |

---

[BC-001]: ../business-case.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0018]: ../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-031]: ./mil-031-consolidate-devops-into-nfhotel.md
