# MIL-082 A compromised container kept away from the host

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-082 |
| CrossReference | [BC-001], [CIS-001], [DSA-001], [MIL-080], [MIL-081] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the third of the three controls that S01 asked for in chat on 2026-11-12 (container escape), planned in autonomous mode | pending |

---

## Purpose

Decide how NF Hotel keeps a compromised container away from the resources of its Docker host: which paths out of a container are closed, by which measure and with which evidence, what is left and why, and one more path closed where the investigation shows that it is cheap to close.

On 2026-11-12 S01 asked to investigate and document how the risk is reduced that a compromised container gains access to the resources of the Docker host. The artificial intelligence (AI) assistant read the repository:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | The measures exist, but they are spread over [CIS-001] (one row per item of the benchmark), the table "How every container is locked down" of the wiki page Containers-and-Images, and section 7 of [DSA-001] (user namespaces evaluated, not chosen). No page says, path by path, how a container could reach the host, what closes each path, and what is left | `docs/sqa`, `docs/wiki` | The answer to S01's question has to be assembled from three documents, and the paths with no measure are not named |
| 2 | Four long-running containers start their main process as root: the nginx master (to bind ports 80 and 443; [CIS-001] row 4.1 Deferred), PostgreSQL (which then drops to its own user), the socket proxy, and Ollama. Ollama runs as root only because the image does; it downloads models from the internet and parses them, and it is the one of the four whose change needs no new port or design | `ops/deploy/*/docker-compose.yml`, [CIS-001] row 4.1 | A bug in Ollama gives an attacker uid 0 inside the container, without capabilities, as the starting point of an escape |
| 3 | Rootless Docker and user namespaces were evaluated in [DSA-001] and not chosen ([CIS-001] row 2.1). Root inside a container is therefore root on the host if a bug of the kernel or of `runc` lets it out. The versions of Docker Engine, containerd and `runc` are read only by hand | [DSA-001] section 7, [CIS-001] rows 1.2.2 and 2.1 | A fix for an escape in `runc` is found only when someone looks |

## Deliverable

- **The host-access map.** For every container of the virtual machine (VM) and of the monitoring LXC: the user of its main process, its capabilities, seccomp and AppArmor, a read-only root, its host mounts, whether it sees the Docker socket, its devices, its namespaces, its networks and published ports. Taken from the output of the runtime check of [MIL-080] in the deploy of the VM and of the monitoring, not from the compose files.
- **The wiki page Container-Escape.** One row per path out of a container: a bug of the kernel reached through a system call, a capability, privileged mode and devices, a namespace of the host, a writable host mount, the Docker socket or API, `/proc` and `/sys`, a bug of the container runtime (`runc`, containerd), root inside the container, exhaustion of memory, processes or disk, the networks and the host's ports, and the secrets of other containers. For each: the measure, its evidence (a rule of the runtime check, a CI test, a row of [CIS-001]), what is left, and the decision with its owner. The decisions not to use user namespaces and rootless Docker are evaluated again there, with what would change them.
- **Ollama as a non-root user.** A one-shot service gives the volume of the models to user 10001 at every start, as the benchmark does for its results ([MIL-045]), and Ollama runs as 10001 with its home on that volume. Its profile becomes `nonroot`, so the runtime check holds it there. A CI test starts the pinned image on a volume written by root, as the volume of production is, and checks the user, the health check, the list of models and a restart.
- **The runtime versions.** The runtime check of [MIL-080] reports the versions of Docker Engine, containerd and `runc` in its output and as a metric; the page Container-Escape says how a fix for an escape is installed (outside the monthly window of the wiki page Host-Hardening when it is critical).
- **The records.** [CIS-001] rows 2.1 and 4.1, the wiki pages Containers-and-Images and Host-Hardening, and the sidebar of the wiki.

Order: the review of this document, then tasks 1 to 5 on one branch with their pull request after [MIL-081] has merged, then task 6, and the gate last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The page Container-Escape has a row for each path of the deliverable with its measure, evidence, what is left and the decision, and a host-access map with a row for every container of the VM and of the monitoring LXC, taken from the runtime check | Yes | No |
| 2 | The CI test of Ollama passes: user 10001, healthy, the models of a volume that root wrote are listed, and it is healthy again after a restart | Yes | No |
| 3 | After the deploy, the runtime check in the CD log shows Ollama with user 10001 and no violation, and the market analysis service and the benchmark are healthy | Yes | No |
| 4 | The runtime check prints the versions of Docker Engine, containerd and `runc`, and the page says how a fix for an escape is installed | Yes | No |
| 5 | [CIS-001] rows 2.1 and 4.1 and the wiki pages say what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` and `QC-WF-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-080 | The runtime check gives the host-access map, holds the profiles and reports the runtime versions |
| MIL-081 | The path through the Docker API is closed there |
| MIL-045 | The pattern of the one-shot service that gives a volume to the user of the image |
| DSA-001 | Section 7, the evaluation of user namespaces, which the page takes up again |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25: harden and observe the server | Criteria 1 to 3: every path out of a container has a measure with evidence or a recorded decision, and one container fewer runs as root |
| BC-001 success criterion 28: hardened server, every selected CIS item with a status and evidence | Criterion 5: [CIS-001] rows 2.1 and 4.1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 (the AI as stand-in, autonomous mode) |

## Target Date

2026-11-14, with [MIL-080] and [MIL-081]; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The host-access map | From the output of the runtime check in the CD log of [MIL-080] (and of [MIL-081]) and in the deploy of the monitoring: one row per container with its user, capabilities, seccomp, AppArmor, read-only root, host mounts, the Docker socket, devices, namespaces, networks and ports. | No | criterion 1 |
| 2 | The page Container-Escape | The wiki page with one row per path out of a container, the measure, its evidence, what is left and the decision; user namespaces and rootless Docker evaluated again; the host-access map; linked from the sidebar, Containers-and-Images and Host-Hardening. | No | criteria 1, 5 |
| 3 | Ollama as a non-root user | The CI test first, on a volume that root wrote. Then the one-shot service that gives the volume of the models to 10001, Ollama as 10001 with its home on the volume, its row in the profiles as `nonroot`, and the job in Ops CI. | No | criterion 2 |
| 4 | The runtime versions | The versions of Docker Engine, containerd and `runc` in the output of the runtime check and as a metric, and on the page how a fix for an escape is installed. | No | criterion 4 |
| 5 | The records, code review and pull request | [CIS-001] rows 2.1 and 4.1, the wiki pages, the review record against `QC-SH-001` and `QC-WF-001`, and the pull request, which closes one issue per task it completes. | No | criteria 5, 6 |
| 6 | The deploy and the checks | The AI approves the CD run of the merge (S01's words of 2026-11-12) and reads in the CD log the runtime check with Ollama as 10001, and the health of Ollama, the market analysis service and the benchmark. | No | criterion 3 |
| 7 | Gate and review record | The gate review record against criteria 1 to 6, the row of MIL-082 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
[MIL-045]: ./mil-045-benchmark-results-volume.md
[MIL-080]: ./mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-081]: ./mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
