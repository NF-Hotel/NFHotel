# RC-349 Gate review of MIL-081 Only the calls of Alloy through the socket proxy

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-349 |
| CrossReference | [MIL-081], [RC-344], [RC-348], [ADR-0042], [ADR-0020], [ADR-0038], [ADR-0040], [CIS-001], [MIL-080], [RC-347], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-081 after the CD run of the merge of #675; Go | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-081] at its gate: the six Go/No-Go criteria, with the evidence of pull request #675 (merged as 77d755f on 2026-11-13 at 20:50 UTC), the two Ops CI runs of its last commit and of the merge, the release run 43505813700 and the continuous deployment (CD) run 43506763115 of that commit. Before the gate: [RC-344] (the plan) and [RC-348] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-081]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 3, 5 and 6 cite [RC-348] and the green checks of #675.
- Language and domain: en / it (the Metadata rows of MIL-081); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-12 for the three controls and, where one was missing, for a plan carried out in autonomous mode with the right to merge, to open pull requests and to deploy ("færdiggør milestones i automode med rettigheder til self merge og PR og Deploy"), without being asked ("Du behøver ik spørg mig om noget"), and on 2026-11-13 to finish the milestones and the gaps found ("færdiggør disse milestones og de huller du har fundet ovenfor som du snakker om. fix").

**Evidence of 2026-11-13:**

- **The pull request.** #675: on its last commit 07c86fa, 53 checks passed and 2 were skipped by design (`DAST full scan` and `OWASP ZAP`, which run by hand and on schedule). SonarQube Cloud passed its quality gate. The pull request closed #660 to #664 (tasks 1 to 5) at the merge; #665 and #666 (tasks 6 and 7) stay open for this record. It also carried the gate of [MIL-080] ([RC-347]).
- **The calls of Alloy in CI.** Ops CI run 43503278485 of the pull request (its merge commit 4b20319) and Ops CI run 43505176793 of the merge 77d755f on `main`, job "Ops monitoring configuration (MIL-023)": `test-alloy-start.sh` read the calls of Alloy from the log of the proxy and printed the same five in both runs, each answered 200: `GET /v<api>/containers/json`, `GET /v<api>/networks`, `GET /v<api>/containers/<id>/json`, `GET /v<api>/containers/<id>/logs` and `HEAD /_ping`. Both runs passed 18 of 18: Alloy healthy as 473, the log lines of a real container in Loki through the proxy, "the Docker discovery logs no refused call", "the proxy allows the 12 calls of the list, with and without an API version", "the proxy refuses the other 69 calls with 403", and the runtime check over both containers with the probe answered from inside Alloy. In the same job the live test of the runtime check passed 40 of 40 and the test of the monitoring stack 20 of 20.
- **The upstream configuration.** [RC-348] criterion 3: with `--upstream`, in WSL (Docker Engine 29.9.3), the same test failed 2 of 18, because the matrix let 23 of the 69 refused calls through: archive (`GET` and `HEAD`), export and attach over a WebSocket among them.
- **The release.** Run 43505813700 of 77d755f built `nfhotel-socket-proxy` from `ops/deploy/alloy/socket-proxy` (the HAProxy configuration of [ADR-0042] over the template of `tecnativa/docker-socket-proxy` v0.5.0, pinned by digest, [ADR-0040] decision 5), and its scan gate passed it at 20:57 UTC: "no secret, no critical finding and no high finding with a fix".
- **The deploy.** CD run 43506763115, approved by the AI at 21:01 UTC under S01's words above ([ADR-0038] gives the approval to S01, who gave it to the AI for this run), ended with success; its deploy step ran from 21:01 to 21:05 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest, `nfhotel-socket-proxy` among them. The deploy took a backup of the database before the migrations and copied it to the backup target, recreated the API, the web application, the market analysis, the benchmark and nginx, then pulled the new image of the proxy and recreated `socket-proxy`; the proxy and Alloy turned healthy. "==> the runtime of the containers against container-profiles.tsv (MIL-080)" printed one PASS line for each of the 11 containers and "==> 11 container(s): 11 passed, 0 failed, 0 warned". The line of `socket-proxy` ends "socket mounted, networks nfhotel-alloy_internal, ports none, proxy list 200, networks 200, archive 403, export 403, attach 403"; the line of Alloy says "socket seen through /, cannot open it"; the other nine say "socket none". The release was recorded ("==> deployed 77d755f0c71d…"), and the container check service passed once.

## Checklist Results

| # | Criterion (MIL-081) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The list of Alloy's calls, read from the proxy's log in CI, is in ADR-0042, and the allowlist is that list | Pass | The five calls that both Ops CI runs printed are the five rows of the table of [ADR-0042]; the regular expression of its decision 3 allows those paths, with or without an API version, and no other |
| 2 | `test-alloy-start.sh` passes in Ops CI: every call of the list answers 200, the log lines of a container reach Loki through the proxy, and every refused call of the deliverable answers 403 | Pass | 18 of 18 in Ops CI runs 43503278485 (the pull request) and 43505176793 (`main`): the 12 calls of the list answer 200, the log lines reach Loki through the proxy, and the 69 refused calls answer 403 |
| 3 | With the configuration of the upstream image the same test fails on the archive, export and attach calls (the test is shown to catch finding 2) | Pass | [RC-348] criterion 3: 16 passed and 2 failed with `--upstream`; archive, export and attach were among the 23 calls let through |
| 4 | After the deploy, the runtime check in the CD log shows that the proxy on the VM refuses archive, export and attach and allows the list and the networks, that the proxy is the one container with the socket, and that Alloy cannot open it | Pass | CD run 43506763115: the proxy answered the probe from inside Alloy with list 200, networks 200, archive 403, export 403 and attach 403; `socket-proxy` is the one line with "socket mounted", and Alloy's line says "socket seen through /, cannot open it"; 11 passed, 0 failed, 0 warned |
| 5 | ADR-0042, [CIS-001] row 5.31 and the wiki page Containers-and-Images say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | [RC-348] criterion 5; the job Ops wiki coverage passed in #675 and on `main` |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, the image passes the scan gate of the release, and every check of the pull request is green | Pass | [RC-348] Go; the job Ops linters (`lint.sh`) passed in #675; the scan gate passed `nfhotel-socket-proxy` in release run 43505813700; every check of #675 passed (53 passed, 2 skipped by design) |

## Findings

1. **The rows of [CIS-001].** Its row of f320dbf (row 5.31, this milestone) becomes Accepted with this Go, and the row before it (6be61ac, the rows of [MIL-080]) becomes Deprecated: the newer row carries the content of the older one, whose criterion 6 passed at [RC-347].
2. **No alert watches a refused call yet.** As [ADR-0042] says, a refused call is answered 403 and nothing alerts on it. The runtime check runs the probe of the proxy every 15 minutes on the VM, so a proxy that lets archive, export or attach through fails the check and reports `nfhotel_container_runtime_violation`; the rule `ContainerRuntimeViolation` that turns it into an alert is loaded when the monitoring is deployed, the condition of [RC-347].

## Overall Verdict

Go: criteria 1 to 6 pass on the evidence of 2026-11-13. #665 and #666 close at the merge of the pull request that carries this record, and the GitHub milestone #56 is closed. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close #665 and #666 and the GitHub milestone #56 | AI as stand-in for S01 | At the merge of this record |
| Load the rule `ContainerRuntimeViolation` on `nf-monitoring` with the deploy of the monitoring (the condition of [RC-347]) | S01 (approves the Tailscale SSH check), then the AI | When S01 is next at the PC |

---

[MIL-081]: ../../milestones/mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
[RC-344]: ./rc-344-mil-081.md
[RC-348]: ./rc-348-mil-081-code.md
[ADR-0042]: ../../adr/adr-0042-only-the-calls-of-alloy-through-the-socket-proxy.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[CIS-001]: ../cis-checklist.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[RC-347]: ./rc-347-mil-080-gate.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
