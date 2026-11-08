# RC-307 Gate review of MIL-065 A remedy for each third-party image that the daily scan fails

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-307 |
| CrossReference | [MIL-065], [RC-304], [RC-305], [DSA-001], [MIL-051], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-065 after the CD run of the merge, the monitoring deploy and the daily scan; Go | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-065] at its gate: the nine Go/No-Go criteria, with the evidence of pull request #572 (merged as fda4e7f on 2026-11-08), the release run 42478119316 and the CD run 42478558104 of that commit, the deploy of the monitoring container the same day, and the run 42481073864 of the daily scan after both deploys. Before the gate: [RC-304] (the acceptance of the plan by S01) and [RC-305] (the code, Go after S01 approved the risk exceptions).
- Checklist used: the Go/No-Go criteria of [MIL-065]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 2, 4, 5, 7 and 8 cite RC-305 and the green checks of #572.
- Language and domain: en / it (the Metadata rows of MIL-065); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. On 2026-11-08 S01 asked the AI in chat to decide as S01 in autonomous mode ("Du beslutter somom du er mig i auto mode on. Merge PR og kør CICD , og fix resten og test det virker og update alle sider og docs") and to carry on with the last milestones ("Fortsæt så med at lave de sidste milestones i automode on"). S01 approved the seven groups of risk exceptions and let the AI approve the CD run of the merge ("1ja", "2ja"), and asked for the monitoring deploy ("2. Ja deploy").

**Evidence of 2026-11-08:**

- **The pull request.** #572: 49 checks passed and 2 skipped by design (`OWASP ZAP` and `DAST full scan`), among them `Ops monitoring configuration (MIL-023)`, which checks the configuration, starts the monitoring stack as the wiki page says and starts Alloy as the VM runs it, `Ops script tests (test-cd-steps.sh)`, `Ops script tests (test-deploy-release.sh)`, `Ops script tests (test-scan-gate.sh)`, `Ops wiki coverage`, `Ops linters` and `Release build (no push)`; merge commit fda4e7f at 11:53 UTC.
- **The release.** Run 42478119316 of fda4e7f logged "PASS … no secret, no critical finding and no high finding with a fix" for each of the six images between 12:00 and 12:02 UTC, `nfhotel-socket-proxy` among them. An anonymous request for a pull token and for the manifest of each of the 18 references of the six packages was refused with 401; the same two requests for a public image answered 200.
- **The deploy of the VM.** CD run 42478558104, approved for S01 at their request, ended with success at 12:10 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest, `ghcr.io/nf-hotel/nfhotel-socket-proxy@sha256:cc58128d7c74…` among them. The deploy pulled that digest and recreated `nfhotel-alloy-socket-proxy-1`, healthy at 12:10:03, and `nfhotel-alloy-alloy-1` was healthy at 12:10:08; the list of containers that the deploy prints on the VM names `nfhotel-alloy-socket-proxy-1` on that digest; the smoke test answered 301 on port 80 and 200 on port 443. At 12:12 UTC Loki counted log lines from 11 containers of the VM in the 10 minutes before (`sum by (container) (count_over_time({host="nf-hotel"}[10m]))`), the socket proxy and Alloy among them.
- **The deploy of the monitoring.** Over Tailscale SSH, at S01's request. The checkout on the monitoring container moved from c3c0374 (the merge of #434) to 50ed1e8, so the deploy also brought the pins that Dependabot had moved since then (Grafana 12.2.1 to 13.3.0, Loki 3.5.7 to 3.7.9). The dry run of `deploy-monitoring.sh` reported "configuration valid"; the data volume of Grafana was archived on the container before its upgrade (`/root/backups/grafana-data-20261108T123342Z.tgz`, mode 0600); `--apply` recreated the six services, each healthy. `docker inspect` shows each container on the pin of its compose file: Prometheus v3.15.1 `fa3302279710…`, Alertmanager v0.34.2 `39d8bb3966c6…`, blackbox exporter v0.29.1 `ea1c7aa3ef3f…`, Loki 3.7.9 `0db216b630ae…`, Grafana 13.3.0 `488984b716c0…` and Python 3.13-slim `e48f59cdba69…`. The 9 targets of Prometheus are up, its 4 rule groups hold 25 rules with the health `ok`, `/api/health` of Grafana answers `"database": "ok"` with the version 13.3.0, the retention is 30d, and the metrics of the VM still arrive (`count by (job) ({host="nf-hotel"})`).
- **The daily scan.** Run 42481073864, started by hand at 12:36 UTC after both deploys, passed the six release images and the eleven third-party images that the compose files of the production stacks pin at fda4e7f, each with its stack and digest, and closed issue #553 with its summary at 12:41 UTC.

## Checklist Results

| # | Criterion (MIL-065) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | One run of `running-image-scan.yaml` after both deploys passes: the six release images and the eleven third-party images that the compose files of the production stacks then pin pass the gate, and the issue of the daily scan (#553) is closed | Pass | Run 42481073864 (12:36 to 12:41 UTC): 6 of 6 release images and 11 of 11 third-party images pass; the run closed #553 at 12:41 UTC |
| 2 | The compose files pin the versions of tasks 1 to 11 (postgres `aa6cb33cf4c1`, cloudflared 2026.10.7, Alloy v1.21.3, Prometheus v3.15.1, Alertmanager v0.34.2, Blackbox exporter v0.29.1, Python `e48f59cdba69`; ollama, Loki, Grafana and haproxy unchanged), and the jobs of Ops CI that check the monitoring configuration, start the monitoring stack and start Alloy pass with them | Pass | The summary of run 42481073864 names each of these pins at fda4e7f, with ollama 0.40.11, Loki 3.7.9, Grafana 13.3.0 and haproxy 3.2-alpine unchanged (RC-305 read the diff); `Ops monitoring configuration (MIL-023)`, whose steps check the configuration, start the stack and start Alloy, is green on #572 |
| 3 | The socket proxy on the VM runs `ghcr.io/nf-hotel/nfhotel-socket-proxy@sha256:<digest>`, the digest in the `release.json` of the CD run of the merge; its release scan passes the gate, the verify step of CD accepts its provenance and its software bill of materials (SBOM), and after the deploy Alloy is healthy and logs of the VM's containers reach Loki | Pass | CD run 42478558104: the release manifest names `cc58128d7c74…`, the verify step accepts it, the deploy pulls it and lists `nfhotel-alloy-socket-proxy-1` on it; release run 42478119316 passes its scan; Alloy healthy at 12:10:08; Loki counts log lines of 11 containers of the VM at 12:12 UTC |
| 4 | `test-cd-steps.sh` and `test-deploy-release.sh` prove that a manifest or a deploy without a valid `nfhotel-socket-proxy` reference is refused, that a release line from before this change, without it, still rolls back, and that `prune` keeps its image | Pass | RC-305 (97 and 94 checks in `debian:12-slim`); `Ops script tests (test-cd-steps.sh)` and `Ops script tests (test-deploy-release.sh)` green on #572 |
| 5 | Every new entry in `ops/security/trivyignore.yaml` has a statement, an `expired_at` at most 30 days ahead and `paths` or `purls` that name a file or a package of a third-party image; `scan-gate.sh --check-ignore` passes, and the release of the merge passes its gate | Pass | RC-305 read each entry (statement, `expired_at` 2026-12-08, `paths` or `purls`), and S01 approved the seven groups in chat ("1ja"); `Ops script tests (test-scan-gate.sh)` green on #572; release run 42478119316 passes its gate for the six images |
| 6 | The monitoring container runs the pins of tasks 6, 7, 8 and 11 (`docker ps`), every target of Prometheus is up, its alert rules are loaded, and the health endpoint of Grafana answers `ok` | Pass | `docker inspect` after the deploy: Prometheus v3.15.1, Alertmanager v0.34.2, blackbox exporter v0.29.1 and Python 3.13-slim on the digests of the compose file; 9 of 9 targets up; 25 rules in 4 groups, all `ok`; Grafana `"database": "ok"` |
| 7 | The wiki pages Containers-and-Images, CI-CD, Workflows and Security-Controls name the new versions, the sixth release image and the accepted findings with their expiry; `test-wiki-coverage.sh` passes, and fails on a copy of Containers-and-Images that names another tag than a compose file or a Dockerfile pins | Pass | RC-305: the pages of task 12 changed; `test-wiki-coverage.sh` 111 passed with its check on a copy of the page with one tag changed, and 7 failed before the pages were changed; `Ops wiki coverage` green on #572 |
| 8 | The code review against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Pass | RC-305 Go after S01 approved the risk exceptions; `Ops linters` green; 49 checks passed and 2 skipped by design on #572 |
| 9 | Section 15 of DSA-001 records decision D2 as taken (private packages that the VM pulls with a read credential of its own), and an anonymous request for the manifest of each of the six release images is refused | Pass | Section 15 of [DSA-001] (dd55a67): "Decided 2026-11-08 by S01 (in chat, MIL-065 task 13): private"; the anonymous requests for the 18 references of the six packages answered 401 |

## Findings

- **The monitoring container had not been deployed since #434.** Its checkout was at c3c0374, so this deploy also brought the pins of Grafana and Loki that Dependabot had moved since then. The upgrade of Grafana from 12 to 13 migrates its database, so its data volume was archived first. Nothing deploys the monitoring container after a merge yet; [MIL-051] plans that step.
- **Issue #553 was closed once by mistake.** The words "closes #553" in a sentence of the description of #572 closed it at the merge, before the scan had passed; it was reopened, and the run of the daily scan closed it again after it passed.

## Overall Verdict

Go: the nine criteria pass on the evidence of 2026-11-08. The gate of MIL-065 is passed: issues #570 and #571 close through the pull request of this record, and the GitHub milestone #47 is closed after the merge. Section 15 of DSA-001 is Accepted with this Go. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Scan the newest upstream releases again and renew or remove each entry of `ops/security/trivyignore.yaml` before it runs out | S01 | 2026-12-08 |
| Deploy the monitoring container after each merge that changes `ops/deploy/monitoring`, by hand until [MIL-051] is in place | S01 | With each such merge |

---

[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[RC-304]: ./rc-304-mil-065-acceptance.md
[RC-305]: ./rc-305-mil-065-code.md
[DSA-001]: ../devsecops-assessment.md
[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
