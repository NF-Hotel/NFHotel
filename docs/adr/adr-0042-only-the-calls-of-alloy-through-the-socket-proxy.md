# ADR-0042 The socket proxy lets through only the calls of Alloy

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0042 |
| CrossReference | [MIL-081], [ADR-0020], [ADR-0024], [ADR-0040], [MIL-080], [CIS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 2 of MIL-081, planned in autonomous mode at S01's request in chat on 2026-11-12 (RC-344 Go); written by the AI; accepted after RC-348 Go | [f320dbf] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** S01 asked in chat on 2026-11-12 for three controls of the containers in autonomous mode, the Docker socket among them, with the merge and the deploy. [MIL-081] plans this one, and the artificial intelligence (AI) assistant reviewed the plan as a stand-in for S01 (RC-344, Go). This record is task 2 of the milestone; the code review of the milestone reviews it, and S01 can challenge it in the summary of the run.

**The problem.** Access to the Docker socket equals root on the host. Alloy reads the containers through a socket proxy ([ADR-0020]), the image of `tecnativa/docker-socket-proxy` v0.5.0 with the Alpine fixes of the release ([ADR-0040], decision 5). Its HAProxy template opens a whole section of the application programming interface (API) of Docker Engine for each switch in the environment. The compose file set `CONTAINERS`, `NETWORKS` and `POST`, and the image sets `EVENTS`, `PING` and `VERSION` by default. The rule of `CONTAINERS` is `^(/v[\d\.]+)?/containers` on the decoded path, with no end anchor, so every `GET` under `/containers` passed, not only the list, the inspect and the logs that the comment of the compose file named: also `archive` (any file of any container, the secret files under `/run/secrets` among them, [ADR-0024]), `export` (the whole filesystem of a container), `attach/ws` (a WebSocket that writes to the input of a container), `top`, `stats` and `changes` ([MIL-081], finding 2). A process that takes over Alloy could have read the secrets of every service without touching the socket.

**What Alloy needs.** `test-alloy-start.sh` runs Alloy with its pinned image, its configuration and the proxy at the log level `info`, and reads every request of Alloy from the log of the proxy ([MIL-081], task 1). On 2026-11-13, in the Windows Subsystem for Linux (WSL) with Docker Engine 29.9.3 and the configuration of the upstream image, which refuses none of them, Alloy v1.21.3 made these calls, each answered 200 (`<id>` is a container id of 64 hexadecimal characters, `/v1.NN` the API version that the client agreed with the daemon, 1.56 there):

| Method | Path | Made by |
| --- | --- | --- |
| `HEAD` | `/_ping` | the Docker client of Alloy, which agrees on the API version with the daemon |
| `GET` | `/v1.NN/containers/json` | `discovery.docker`, the list of the running containers |
| `GET` | `/v1.NN/networks` | `discovery.docker`, the labels of the networks of each container |
| `GET` | `/v1.NN/containers/<id>/json` | `loki.source.docker`, the inspect of a container before it reads its logs |
| `GET` | `/v1.NN/containers/<id>/logs` | `loki.source.docker`, the log stream of a container |

**Forces.**

- Alloy must keep finding the containers and reading their logs: a refused call stops the logs of every container quietly, as `NETWORKS: "0"` did on 2026-10-23 (RC-185).
- The proxy is the one container that mounts the socket; the `:ro` flag of the mount limits no API call, so its own rules are the limit.
- The image is built, scanned and attested by the release, and continuous deployment (CD) deploys its digest ([ADR-0040]); the virtual machine (VM) never builds it.
- The Docker API takes a path with or without a version, a method, query arguments and headers. A rule that decodes the path, or that has no end anchor, matches more than it names.
- A new version of Alloy can make another call. The change must show in continuous integration (CI) before it merges, not on the VM.

Options considered:

1. **Keep the switches of the upstream image.** No switch is narrower than a section of the API; `CONTAINERS` cannot be split into list, inspect and logs.
2. **Another proxy with an allowlist per call,** such as `wollomatic/socket-proxy` or `linuxserver/socket-proxy`. Either one is a new image to pin, scan, attest and update, for a list of five calls that HAProxy can hold in one rule.
3. **No proxy:** Alloy mounts the socket. It gives root on the host to the process that reads the logs; rejected in [ADR-0020] already.
4. **No container discovery:** Alloy reads the files of the containers' logs from the host. It needs a mount of `/var/lib/docker/containers`, loses the names and labels of the containers in Loki, and changes the dashboards and alerts that read them.
5. **Our own HAProxy configuration in the same image.** `ops/deploy/alloy/socket-proxy/haproxy.cfg` replaces the template of the upstream image; the entrypoint still writes the bind address into it at start, and the build checks it with `haproxy -c`.

## Decision

We follow option 5: the socket proxy lets through only the calls of Alloy, and refuses every other request with 403.

1. **Reads only.** `GET` and `HEAD` (the `METH_GET` of HAProxy matches both); every other method is refused.
2. **No upgrade of the connection.** A request with an `Upgrade` header, or `upgrade` in its `Connection` header, is refused, so no WebSocket or hijacked stream can write to a container.
3. **The exact paths.** The path, as it arrives and not decoded, must match `^(/v1[.][0-9]+)?/(_ping|containers/json|networks|containers/[0-9a-f]{64}/(json|logs))$`. An encoded character, a dot segment, a trailing slash, a container name instead of its id, or any other path matches nothing and is refused. The query is not part of the path, so the arguments of the list and the logs pass.
4. **The switches go.** The compose file keeps only `LOG_LEVEL`; `CONTAINERS`, `NETWORKS`, `POST`, `INFO`, `IMAGES`, `VOLUMES` and `EXEC` are removed, and the upstream defaults `EVENTS`, `PING` and `VERSION` are read by nothing.
5. **The list changes with a test.** `test-alloy-start.sh` prints Alloy's calls from the log of the proxy in Ops CI and fails when the proxy refuses one of them; it also sends the refused calls of [MIL-081] (archive, export, attach over a WebSocket, top, stats, changes, events, info, version, the image, volume, system, secret, plugin, swarm, node, service, task, config and distribution sections, and the writes), with and without a version, with an encoded slash, a dot segment and an upgrade header, and expects 403. A new version of Alloy, or a new tag of the upstream image, that breaks the list fails that job on its Dependabot pull request; the new call then goes into this list, its rule and the test together.
6. **The check on the VM.** The runtime check of [MIL-080] asks the running proxy from inside Alloy, its only client, for the list and the networks (200) and for archive, export and attach (403) at every deploy and every 15 minutes, and fails on any other answer.

## Consequences

**Positive:**

- A process that takes over Alloy can no longer read the files or the filesystem of a container, or write to one, through the proxy; the secret files of [ADR-0024] are out of its reach.
- The allowlist is what Alloy does, measured, not the name of an API section, and the test of CI keeps the two the same.
- The image, its scan, its attestations and its deploy stay as [ADR-0040] decided; only one file is copied in.

**Negative:**

- The logs and the inspect of every container still pass, as Alloy needs them: a process that takes over Alloy reads every log line and the configuration of every container, the environment among it. The secrets are files since [ADR-0024], and no secret is in an environment (`check-secret-env.sh`, MIL-035).
- A new call of Alloy is refused until this list changes, and the logs of the containers may stop meanwhile. The test of CI is the guard. On the VM the runtime check asks for the list and the networks every 15 minutes, but no alert watches a refused inspect or log call: it shows only in Alloy's own log.
- The image no longer runs the template of the upstream image. A new upstream tag that moves the template or changes its entrypoint breaks the proxy; the build (`haproxy -c`) and `test-alloy-start.sh` show it on the Dependabot pull request.
- The paths are matched as they arrive: a client that encodes a character of an allowed path, or uses a container name, is refused. Alloy's client sends neither.

**Quality:** security improves (confidentiality and integrity of the containers against a compromised log collector); reliability of the log path depends on the list, kept by a test; maintainability: one more file in the image, changed only with its test (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-081]: tasks 2 to 4 implement this record.
- [ADR-0020]: the proxy of the monitoring now allows five calls instead of the sections of the API.
- [ADR-0040]: the patched image of the proxy also carries its configuration.
- [MIL-080]: the runtime check asks the proxy at every run.
- [CIS-001]: row 5.31 names the allowlist as the limit of the one container with the socket.

---

[MIL-081]: ../milestones/mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[ADR-0040]: ./adr-0040-patched-nginx-image-from-the-release.md
[MIL-080]: ../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[CIS-001]: ../sqa/cis-checklist.md
[f320dbf]: https://github.com/NF-Hotel/NFHotel/commit/f320dbf3e2a8ba0f46944357dbf40c43423ae263
