# MIL-081 Only the calls of Alloy through the socket proxy

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-081 |
| CrossReference | [BC-001], [CIS-001], [ADR-0020], [ADR-0024], [ADR-0040], [MIL-065], [MIL-080] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the second of the three controls that S01 asked for in chat on 2026-11-12 (the Docker socket), planned in autonomous mode | pending |

---

## Purpose

Decide that only the socket proxy reaches the Docker socket, and that the proxy lets through exactly the calls that Alloy needs to collect the logs of the containers, checked in CI and on the virtual machine (VM).

Access to the Docker socket equals root on the host. Alloy reads the containers through a socket proxy ([ADR-0020]), the image of `tecnativa/docker-socket-proxy` v0.5.0 rebuilt by the release ([ADR-0040], decision 5). On 2026-11-12 the artificial intelligence (AI) assistant read its settings and the configuration of the upstream image:

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | Only the proxy mounts the socket, and only Alloy reaches the proxy, on an internal network with no route out. Alloy mounts the host's root folder read-only, so the socket file is visible inside it, but Alloy runs as user 473 without the group of the socket | `ops/deploy/alloy/docker-compose.yml`, [CIS-001] row 5.31 | Nothing else holds the socket; nothing checks this on the VM |
| 2 | The proxy decides by section of the Docker API. `CONTAINERS: "1"` lets every `GET` under `/containers` through, not only the list, inspect and logs that the compose file's comment names: also the `archive` call (any file of any container, the secret files under `/run/secrets` included, [ADR-0024]), `export` (the whole filesystem of a container), `attach/ws` (a WebSocket that writes to the input of a container), `top`, `stats` and `changes`. `EVENTS`, `PING` and `VERSION` are on by default | The configuration of the upstream image v0.5.0 (`haproxy.cfg` and its `Dockerfile`) | A process that takes over Alloy could read the secrets of every service through the proxy, without touching the socket itself |
| 3 | `ops/tests/test-alloy-start.sh` checks in CI that two reads answer 200 and four other calls 403 (network create, stop, the image list, exec). Nothing checks the archive, export or attach calls, and nothing asks the running proxy on the VM | Ops CI, `nf-hotel` | Finding 2 was not seen |

## Deliverable

- **Alloy's calls, measured.** A CI run of Alloy with the proxy logging every request gives the list of the calls Alloy makes; the allowlist is exactly that list.
- **The allowlist.** The socket proxy gets its own HAProxy configuration, built into its image: `GET` and `HEAD` only; a request that asks to upgrade the connection is refused; only the exact paths of the list pass (in any API version), and everything else gets 403. The switches in the environment of the compose file no longer decide anything and are removed, apart from the log level.
- **The test matrix.** `ops/tests/test-alloy-start.sh` checks that every call of the list answers 200, that Alloy's logs still reach Loki through the proxy, and that each refused call answers 403: archive, export, attach (a WebSocket), top, stats, changes, events, info, the image, volume, system, secret, plugin, swarm, node, service, task, config and distribution sections, and the writes (create, start, stop, kill, exec, build, commit, auth, network create), also with an API version in the path and with an encoded slash.
- **The check on the VM.** The runtime check of [MIL-080] asks the running proxy from inside Alloy, its only client, for two allowed calls and the refused archive, export and attach calls, and prints only the status codes. Its socket rule already proves finding 1 at every run: the proxy is the one container that mounts the socket, and Alloy, which sees the file, cannot open it.
- **ADR-0042.** The proxy allows only the calls that Alloy makes; a new version of Alloy that needs another call fails `test-alloy-start.sh` in CI before its update merges.
- **The records.** [CIS-001] row 5.31, the wiki page Containers-and-Images and the comments of the compose file and the Dockerfile.

Order: the review of this document, then tasks 1 to 5 on one branch with their pull request after [MIL-080] has merged, then task 6, and the gate last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The list of Alloy's calls, read from the proxy's log in CI, is in ADR-0042, and the allowlist is that list | Yes | No |
| 2 | `test-alloy-start.sh` passes in Ops CI: every call of the list answers 200, the log lines of a container reach Loki through the proxy, and every refused call of the deliverable answers 403 | Yes | No |
| 3 | With the configuration of the upstream image the same test fails on the archive, export and attach calls (the test is shown to catch finding 2) | Yes | No |
| 4 | After the deploy, the runtime check in the CD log shows that the proxy on the VM refuses archive, export and attach and allows the list and the networks, that the proxy is the one container with the socket, and that Alloy cannot open it | Yes | No |
| 5 | ADR-0042, [CIS-001] row 5.31 and the wiki page Containers-and-Images say what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, the image passes the scan gate of the release, and every check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-080 | The runtime check, which gets the probe of the proxy |
| MIL-065 | The release builds, scans and attests the image of the proxy ([ADR-0040], decision 5) |
| ADR-0020 | Alloy reads the containers through the proxy |
| ADR-0024 | The secrets are files under `/run/secrets`, which the archive call could read |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 25: harden and observe the server | Criteria 2 and 4: the only container with the Docker socket lets through only the calls the monitoring needs |
| BC-001 success criterion 28: hardened server, every selected CIS item with a status and evidence | Criteria 4 and 5: [CIS-001] row 5.31 with the evidence of the VM |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 (the AI as stand-in, autonomous mode) |

## Target Date

2026-11-14, with [MIL-080]; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Alloy's calls | Run `test-alloy-start.sh` with the proxy at the log level `info` and keep the method and path of every request Alloy makes, so the allowlist rests on what Alloy does and not on the name of an API section. | No | criterion 1 |
| 2 | The allowlist and ADR-0042 | A `haproxy.cfg` of NF Hotel in `ops/deploy/alloy/socket-proxy/`, copied into the image over the upstream template: `GET` and `HEAD`, no upgrade, only the exact paths of task 1, everything else 403. The compose file keeps only the log level; ADR-0042 records the decision and the list. | No | criteria 1, 5 |
| 3 | The test matrix | `test-alloy-start.sh` checks the allowed calls, the path to Loki, and every refused call of the deliverable, with an API version in the path and an encoded slash. Run once against the upstream configuration, where the archive, export and attach calls pass, to show that the test catches finding 2. | No | criteria 2, 3 |
| 4 | The check on the VM | The runtime check of [MIL-080] asks the proxy from inside Alloy (Bash's `/dev/tcp`, as Alloy's health check does) for the list, the networks, archive, export and attach, and fails on any other answer than 200 and 403; it prints only the status codes. Its unit and live tests cover the probe. | No | criterion 4 |
| 5 | The records, code review and pull request | [CIS-001] row 5.31, the wiki page Containers-and-Images, the comments of the compose file and the Dockerfile, the review record against `QC-SH-001`, and the pull request, which closes one issue per task it completes. | No | criteria 5, 6 |
| 6 | The deploy and the checks | The AI approves the CD run of the merge (S01's words of 2026-11-12) and reads the runtime check in the CD log: the proxy's answers, the socket rule and Alloy healthy. | No | criterion 4 |
| 7 | Gate and review record | The gate review record against criteria 1 to 6, the row of MIL-081 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[CIS-001]: ../sqa/cis-checklist.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[ADR-0040]: ../adr/adr-0040-patched-nginx-image-from-the-release.md
[MIL-065]: ./mil-065-third-party-image-remedies.md
[MIL-080]: ./mil-080-every-container-runs-with-the-rights-of-its-profile.md
