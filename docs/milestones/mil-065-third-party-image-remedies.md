# MIL-065 A remedy for each third-party image that the daily scan fails

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-065 |
| CrossReference | [BC-001], [DSA-001], [ADR-0007], [ADR-0038], [ADR-0040], [MIL-022], [MIL-036], [MIL-064] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: one task for each of the eleven third-party images that the first full daily scan fails (run 42443407440), each with its remedy under ADR-0040 decision 5, checked with candidate scans on 2026-11-08; written in autonomous mode at S01's request in chat ("fix resten") | [c27ba7a] |
| 2026-11-08 | Accepted | mikemike | S01 | Accepted by S01 in chat ("godkendt", to the choice "Godkend alt: MIL-065 med wiki-tjekket, D2 som private"), which also confirms the language and the terms (RC-304)<br>Task 12: `test-wiki-coverage.sh` also checks the tags of the pinned images<br>New task 13 and criterion 9: decision D2 of DSA-001 taken as private packages; tasks 13 and 14 become 14 and 15 | pending |

---

## Purpose

Give each third-party image that the daily scan fails a remedy under [ADR-0040] decision 5, so that every image that runs NF Hotel meets BC-001 success criterion 27 again and the daily scan of [MIL-064] passes.

Since MIL-064 the daily scan ([MIL-036]) also scans the twelve third-party images that the compose files of the stacks on the virtual machine (VM) and of the monitoring stack pin by digest. Its first full run after the deploy of MIL-064 (run 42443407440, 2026-11-08, issue #553) fails eleven of them; only `haproxy:3.2-alpine` passes. The census of MIL-064 found the same eleven on the same day.

Candidate scans on 2026-11-08, with the scanner and the gate of the release (Trivy 0.75.0, `scan-gate.sh`), show what each remedy leaves. A blocking finding is a critical one, or a high one with a fixed version, counted once per vulnerability and package, as `scan-gate.sh` counts them.

| Image | Host | Pinned: blocking findings | Candidate: blocking findings | Remedy (ADR-0040 decision 5) |
| --- | --- | --- | --- | --- |
| `postgres:16-alpine` | VM | `721873c34ceb`: 29 (26 in `gosu`, 3 Alpine packages) | the build of 2026-11-05, `aa6cb33cf4c1`: 26, all in `gosu` | rule 1, then rule 3 for `gosu` |
| `ollama/ollama` | VM | 0.40.11: 3 (`golang.org/x/net`; Ubuntu's `apt` and `libapt-pkg6.0t64`) | 0.40.15: the same 3 | rule 3; the pin stays |
| `cloudflare/cloudflared` | VM | 2026.10.5: 4 | 2026.10.7: 3 (`golang.org/x/net`; Debian's `zlib1g` twice) | rule 1, then rule 3 |
| `tecnativa/docker-socket-proxy` | VM | v0.5.0: 9, all Alpine packages | a patch layer on the pinned digest (`apk upgrade --no-cache`): 0 | rule 2 |
| `grafana/alloy` | VM | v1.21.2: 4 | v1.21.3: 3 (`golang.org/x/net`; Ubuntu's `apt` and `libapt-pkg6.0t64`) | rule 1, then rule 3 |
| `prom/prometheus` | monitoring | v3.7.2: 54 | v3.15.1: 1 (`golang.org/x/net`, in `prometheus` and `promtool`); v3.16.0 leaves the same | rule 1, then rule 3 |
| `prom/alertmanager` | monitoring | v0.29.0: 48 | v0.34.2: 1 (`golang.org/x/net`) | rule 1, then rule 3 |
| `prom/blackbox-exporter` | monitoring | v0.27.0: 50 | v0.29.1: 1 (`golang.org/x/net`) | rule 1, then rule 3 |
| `grafana/loki` | monitoring | 3.7.9: 1 (`golang.org/x/net`) | none: 3.7.9 is the newest 3.7 release | rule 3 |
| `grafana/grafana` | monitoring | 13.3.0: 8, in the server and its bundled data source plugins | none: 13.3.0 is the newest release | rule 3 |
| `python:3.13-slim` (the scan exporter) | monitoring | `2d44738ebf4c`: 9 | the build of 2026-11-08, `e48f59cdba69`: 7 (packages vendored inside pip; Debian's `zlib1g` twice) | rule 1, then rule 3 |

Where a newer version or build exists, it lowers the count for every image but ollama, and none passes the gate. What is left has four causes. `golang.org/x/net` older than 0.62.0 (CVE-2026-78661, a denial of service in its HyperText Markup Language (HTML) parser) is in nine of the images, and no upstream release has 0.62.0 yet. `gosu` in the postgres image is built with Go 1.24.6, and the bundled plugins of Grafana with Go 1.26.7. Ubuntu has fixed `apt` and Debian has fixed `zlib1g`, but the upstream builds do not have the fixes yet; in these images they sit next to Go or Python findings, so a patch layer would not clear them, and rule 2 is for images with nothing but operating-system (OS) packages. The Python image has old copies of `setuptools`, `msgpack` and `urllib3` vendored inside pip. Only the socket proxy has nothing but OS packages, and its patch layer passes the gate.

Where rule 1 applies, the pin moves to the version of Dependabot's pull request (#545 to #549): the newest version past the seven-day cooldown of `.github/dependabot.yml`. For Prometheus that is v3.15.1, which leaves the same finding as v3.16.0. The new builds of `postgres:16-alpine` and `python:3.13-slim` are younger than the cooldown; they are rebuilds of the pinned tags with the security fixes of their distributions, and the cooldown keeps out new releases, not security updates. Ollama stays at 0.40.11, because 0.40.15 leaves the same findings.

An ignore entry in `ops/security/trivyignore.yaml` applies to every image that Trivy scans with the file, the release images of NF Hotel included, unless it names a file (`paths`) or a package (`purls`). A test on 2026-11-08 showed that Trivy 0.75.0 honours both: `paths` for a Go binary, as an exact path or a `*` pattern, and `purls` for an OS or a Python package, with or without its version. Every entry of this milestone names the files or packages of the third-party image, so the gate of the release images does not change. Each statement says what the check of the binary or the package found: whether the affected Go package is compiled into the binary, whether the file or library is used at run time, and who can reach the service. The entries run out after at most 30 days, and the daily scan reports a finding again once its entry has run out.

## Deliverable

- **The pins** of tasks 1 to 11 in the compose files of `ops/deploy`: postgres, cloudflared, Alloy, Prometheus, Alertmanager, the Blackbox exporter and the Python image of the scan exporter move; ollama, Loki, Grafana and haproxy stay.
- **The patched socket proxy**: `ghcr.io/nf-hotel/nfhotel-socket-proxy`, a patch layer on the pinned `tecnativa/docker-socket-proxy:v0.5.0` digest that the release builds, scans and attests as its sixth image, and that continuous deployment (CD) verifies and deploys by name, as ADR-0040 decided for the proxy; with a Dependabot entry for its `FROM` line.
- **The accepted findings**: one ignore entry for each finding that no release and no patch layer clears, each with a statement, an `expired_at` at most 30 days ahead and `paths` or `purls` that narrow it to the third-party image.
- **The deploys**: the CD run of the merge for the VM stacks, approved for S01 ([ADR-0038]), and `deploy-monitoring.sh --apply` on the monitoring container for the monitoring stack.
- **The wiki**: the pages Containers-and-Images, CI-CD, Workflows and Security-Controls, and a check in `test-wiki-coverage.sh` that Containers-and-Images names the tag that each compose file and Dockerfile pins.
- **Decision D2**: section 15 of [DSA-001] records D2 as taken: the packages of the release on GitHub Container Registry (GHCR) stay private, and the VM pulls them with a read credential of its own.
- **The proof**: one run of the daily scan after both deploys, in which every release image and every third-party image passes.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 13 on one branch, task 14 with the pull request and the deploys, then task 15.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | One run of `running-image-scan.yaml` after both deploys passes: the six release images and the eleven third-party images that the compose files of the production stacks then pin pass the gate, and the issue of the daily scan (#553) is closed | Yes | No |
| 2 | The compose files pin the versions of tasks 1 to 11 (postgres `aa6cb33cf4c1`, cloudflared 2026.10.7, Alloy v1.21.3, Prometheus v3.15.1, Alertmanager v0.34.2, Blackbox exporter v0.29.1, Python `e48f59cdba69`; ollama, Loki, Grafana and haproxy unchanged), and the jobs of Ops CI that check the monitoring configuration, start the monitoring stack and start Alloy pass with them | Yes | No |
| 3 | The socket proxy on the VM runs `ghcr.io/nf-hotel/nfhotel-socket-proxy@sha256:<digest>`, the digest in the `release.json` of the CD run of the merge; its release scan passes the gate, the verify step of CD accepts its provenance and its software bill of materials (SBOM), and after the deploy Alloy is healthy and logs of the VM's containers reach Loki | Yes | No |
| 4 | `test-cd-steps.sh` and `test-deploy-release.sh` prove that a manifest or a deploy without a valid `nfhotel-socket-proxy` reference is refused, that a release line from before this change, without it, still rolls back, and that `prune` keeps its image | Yes | No |
| 5 | Every new entry in `ops/security/trivyignore.yaml` has a statement, an `expired_at` at most 30 days ahead and `paths` or `purls` that name a file or a package of a third-party image; `scan-gate.sh --check-ignore` passes, and the release of the merge passes its gate | Yes | No |
| 6 | The monitoring container runs the pins of tasks 6, 7, 8 and 11 (`docker ps`), every target of Prometheus is up, its alert rules are loaded, and the health endpoint of Grafana answers `ok` | Yes | No |
| 7 | The wiki pages Containers-and-Images, CI-CD, Workflows and Security-Controls name the new versions, the sixth release image and the accepted findings with their expiry; `test-wiki-coverage.sh` passes, and fails on a copy of Containers-and-Images that names another tag than a compose file or a Dockerfile pins | Yes | No |
| 8 | The code review against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |
| 9 | Section 15 of DSA-001 records decision D2 as taken (private packages that the VM pulls with a read credential of its own), and an anonymous request for the manifest of each of the six release images is refused | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-064 | The daily scan of the third-party images, and the deploy that takes the release images by name |
| ADR-0040 | Decision 5, the three remedies, and the form of a patch layer from the release |
| MIL-022 | The gate of the release and the ignore file with its 30-day limit |
| DSA-001 | Section 8: an accepted finding is added by a reviewed pull request and approved by S01; section 15: decision D2 |
| ADR-0007 | The VM never builds; the patched socket proxy comes from the release |
| ADR-0038 | The CD run of the merge waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 success criterion 27: every deployed digest has an SBOM and a provenance attestation that `gh attestation verify` accepts; 0 critical and 0 fixable high findings in the deployed images | Criteria 1 to 6: every third-party image passes the gate, with its accepted findings recorded and limited in time |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-15. The ignore entries run out on 2026-12-08 at the latest; before then the newest upstream releases are scanned again, and an entry is renewed only after a new check. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | postgres: the build of 2026-11-05 and the findings in gosu | Move `ops/deploy/postgres/docker-compose.yml` to the build of `postgres:16-alpine` of 2026-11-05 (`aa6cb33cf4c1`), which clears its three Alpine findings; PostgreSQL stays at 16.15 and `gosu` at 1.19. The 26 findings left are in `gosu`, built with Go 1.24.6, which the entry point runs to change from root to the user `postgres` before the server starts; no build of the tag has a newer `gosu`. One ignore entry per finding, `paths: [usr/local/bin/gosu]`, whose statement says which of the affected Go packages are compiled into the binary. | No | criteria 1, 2, 5 |
| 2 | ollama: the findings of 0.40.11 | The pin stays at 0.40.11, because 0.40.15 leaves the same three findings. Ignore entries for CVE-2026-78661 in `usr/bin/ollama` and for CVE-2026-109518 in Ubuntu's `apt` and `libapt-pkg6.0t64` 2.8.3 (by `purls`); the statements say whether `golang.org/x/net/html` is in the binary and that apt never runs in the container. | No | criteria 1, 5 |
| 3 | cloudflared: 2026.10.7 | Move the tunnel to 2026.10.7, the version of Dependabot's pull request #546, which clears CVE-2026-116587 in `go-jose`. Ignore entries for CVE-2026-78661 in `usr/local/bin/cloudflared` and for the two findings in Debian's `zlib1g` (by `purls`); the statements say whether `golang.org/x/net/html` is in the binary and whether cloudflared loads `libz` at all. | No | criteria 1, 2, 5 |
| 4 | The socket proxy: a patch layer from the release | As ADR-0040 decision 5 rule 2 says for an image with nothing but OS findings: `ops/deploy/alloy/socket-proxy/Dockerfile` starts `FROM` the pinned `tecnativa/docker-socket-proxy:v0.5.0` digest and runs `apk upgrade --no-cache`, and the compose file takes `NFHOTEL_SOCKET_PROXY_IMAGE` with a local build as the default. The release builds, scans and attests it as `ghcr.io/nf-hotel/nfhotel-socket-proxy` (`build-release-images.sh`, the release-images action, `release.json`); `cd-steps.sh` and `deploy-release.sh` take it as a sixth name, one that a release line from before this change may lack; `deploy-services.sh` passes it to the Alloy stack, and with `--no-build` it passes `--no-build` to compose, so a failed pull of a release image stops the deploy instead of a build on the VM ([ADR-0007]); Dependabot gets a `docker` entry for its folder. `test-cd-steps.sh` and `test-deploy-release.sh` cover it. The candidate passed the gate on 2026-11-08 (9 findings to 0). | No | criteria 3, 4 |
| 5 | Alloy: v1.21.3 | Move Alloy to v1.21.3, the version of pull request #545, which clears CVE-2026-116587 in `go-jose`; the configuration is checked with `alloy validate` of the new version and by the Alloy job of Ops CI. Ignore entries for CVE-2026-78661 in `usr/bin/alloy` and CVE-2026-109518 in Ubuntu's `apt` and `libapt-pkg6.0t64` (by `purls`), with statements on `golang.org/x/net/html` in the binary and on apt, which never runs in the container. | No | criteria 1, 2, 5 |
| 6 | Prometheus: v3.15.1 | Move Prometheus to v3.15.1, the version of pull request #548 (v3.16.0 is younger than the cooldown and leaves the same finding); 53 of its 54 findings go. The configuration and the rules are checked with `promtool` of the new version by Ops CI. An ignore entry for CVE-2026-78661 in `bin/prometheus` and `bin/promtool`, with a statement on `golang.org/x/net/html` in the binaries. | No | criteria 1, 2, 5, 6 |
| 7 | Alertmanager: v0.34.2 | Move Alertmanager to v0.34.2, the version of pull request #547; 47 of its 48 findings go. The configuration is checked with `amtool check-config` of the new version by Ops CI. CVE-2026-78661 in `bin/alertmanager` and `bin/amtool` joins the entry of task 6. | No | criteria 1, 2, 5, 6 |
| 8 | Blackbox exporter: v0.29.1 | Move the Blackbox exporter to v0.29.1, the version of pull request #549; 49 of its 50 findings go. Its modules are checked with the new version by Ops CI. CVE-2026-78661 in `bin/blackbox_exporter` joins the entry of task 6. | No | criteria 1, 2, 5, 6 |
| 9 | Loki: the finding of 3.7.9 | 3.7.9 is the newest Loki 3.7 release, so the pin stays. CVE-2026-78661 in `usr/bin/loki` joins the entry of task 6, with a statement on `golang.org/x/net/html` in the binary. | No | criteria 1, 5 |
| 10 | Grafana: the findings of 13.3.0 | 13.3.0 is the newest Grafana release, so the pin stays. Ignore entries for its eight findings (seven vulnerabilities), narrowed to `usr/share/grafana/bin/grafana` and the bundled data source plugins under `usr/share/grafana/data/plugins-bundled`; the statements say which plugins the provisioned data sources use (Prometheus and Loki), which affected packages are in those binaries, and who can reach Grafana. | No | criteria 1, 5, 6 |
| 11 | The Python image of the scan exporter: the build of 2026-11-08 | Move the scan exporter to the build of `python:3.13-slim` of 2026-11-08 (`e48f59cdba69`), which clears Debian's `apt` findings. Ignore entries for the copies of `setuptools`, `msgpack` and `urllib3` vendored inside pip and for Debian's `zlib1g` (by `purls`); the statements say that the exporter runs its own module and never pip, and whether the Python modules that link `libz` call the `gz*` functions of the two zlib findings. The exporter's tests run with the new image in Ops CI. | No | criteria 1, 2, 5, 6 |
| 12 | The wiki | Containers-and-Images: the new versions, the patched socket proxy and the accepted findings with their expiry. CI-CD and Workflows: six release images. Security-Controls: the ignore entries narrowed to third-party images. `test-wiki-coverage.sh` also checks that Containers-and-Images names the tag that each compose file and Dockerfile pins, so a pin that moves without the page fails the test, as the six stale tags that RC-302 found would have; its test covers a stale tag. `test-wiki-coverage.sh` passes. | No | criterion 7 |
| 13 | Decision D2: private packages | As S01 chose in chat: section 15 of DSA-001 records D2 as taken, in the Recommendation cell with the date, as D4, D7 and D10 do. The packages of the release on GHCR stay private, the sixth (`nfhotel-socket-proxy`) included; CD logs in with the job token only to verify the attestations, and the VM pulls the images with a read credential of its own, as step 3 of the wiki page CI-CD already says. An anonymous request for the manifest of each release image is refused. | No | criterion 9 |
| 14 | Code review, pull request and deploys | The review record of the shell scripts, the workflows and the configuration files against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` before the pull request; the pull request closes one issue per task and carries the milestone. The CD run of the merge, approved for S01 (ADR-0038), deploys the VM stacks; `deploy-monitoring.sh --apply` on the monitoring container deploys the monitoring stack; then one run of the daily scan. | No | criteria 1, 3, 6, 8 |
| 15 | Gate and review record | The gate review record against criteria 1 to 9 with the logs of the CD run, the monitoring deploy and the daily scan, the row of MIL-065 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 9 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[ADR-0040]: ../adr/adr-0040-patched-nginx-image-from-the-release.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-036]: ./mil-036-security-operations.md
[MIL-064]: ./mil-064-patched-nginx-and-third-party-scan.md
[c27ba7a]: https://github.com/NF-Hotel/NFHotel/commit/c27ba7a193b1056bd13ed0d18cafb95f74c4ad18
