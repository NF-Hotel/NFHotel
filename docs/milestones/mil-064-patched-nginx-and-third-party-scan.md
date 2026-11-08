# MIL-064 A patched nginx image and a daily scan of the third-party images

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-064 |
| CrossReference | [BC-001], [ADR-0007], [ADR-0030], [ADR-0038], [MIL-022], [MIL-036], [MIL-059] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version; the finding of 2026-11-06 checked again on 2026-11-08 (1 critical and 54 high findings with a fix in the pinned `nginx:1.29-alpine` digest, which is still the newest build of that tag), a census of the other twelve pinned third-party images on the same day (eleven fail the gate), and S01's request in chat to plan the fix ("Ja, lav en plan"); S01 approved the plan in chat on 2026-11-08 ("Godkend planen") after RC-298 Go | [401da1a] |

---

## Purpose

Decide how the reverse proxy in production gets the security fixes of its base image while the upstream image is not rebuilt, and close the gap in the daily scan that let the finding go unseen.

The proxy runs the third-party image `nginx:1.29-alpine`, pinned by digest in `ops/deploy/nginx/docker-compose.yml`. Every request to the hotel passes it, from the internet through the tunnel and from the tailnet: it ends TLS (Transport Layer Security) and forwards to the web service and the API (application programming interface). A scan with the vulnerability database of 2026-11-08 finds 1 critical and 54 high findings with a fix in it, in 15 Alpine packages (among them curl, libcurl, libexpat, libssl3, libcrypto3, musl and zlib); on 2026-11-06 the count was 51. The pinned digest is still the newest build of the tag, so Dependabot has nothing to propose ([MIL-059]); the fixes exist only in the Alpine package repositories.

BC-001 success criterion 27 asks for 0 critical and 0 fixable high findings in the deployed images. The release gate of [MIL-022] enforces it for the four images that the release builds, and the daily scan of [MIL-036] scans those four again every day. No check scans the third-party images of the compose files after they are pinned, so this finding was found by hand.

A lab test on 2026-11-06 showed a fix. An image `FROM` the pinned digest with one layer `RUN apk upgrade --no-cache` upgrades 25 packages and Alpine from 3.23.4 to 3.23.8, has 0 high or critical findings, passes `scan-gate.sh` and serves the site. It is 127 MB against 93.5 MB, and its content depends on the day it is built, so production must run the digest that the release built and scanned, never a build of its own. [ADR-0030] made the same choice for the frozen market-analysis image: a patch layer on a fixed base, built, scanned and attested by the release.

The proxy is not the only one. A census on 2026-11-08 with the scanner and the gate of the release (Trivy 0.75.0, `scan-gate.sh`) of the other twelve third-party images pinned in the compose files of the stacks on the virtual machine (VM) and the monitoring stack: eleven fail the gate, and only `haproxy:3.2-alpine` passes. Most of their findings are Go modules compiled into the binaries (the Go standard library, `golang.org/x/net`, `golang.org/x/crypto`, gRPC), which only a newer upstream release can fix. For eight of them a newer version or a new build of the pinned tag exists. Dependabot proposes these weekly, but on 2026-11-04 it opened five pull requests for the compose files, its default limit, and the Prometheus images were not among them. Only nginx and `tecnativa/docker-socket-proxy` have nothing but operating-system (OS) packages, which a patch layer fixes. This milestone patches the proxy, makes every third-party image visible in the daily scan, and plans the rest from the first full run.

## Deliverable

- **ADR-0040**: the proxy runs a patched image that the release builds: `ops/deploy/nginx/Dockerfile` starts `FROM` the pinned digest and adds one `apk upgrade --no-cache` layer, published as `ghcr.io/nf-hotel/nfhotel-nginx`. It weighs the alternatives (wait for an upstream build, ignore entries of at most 30 days, Alpine's own nginx package on a plain Alpine base, a build on the VM), decides whether the deploy command takes the release digests by position, as today, or by name, and sets the rule for a third-party image that the daily scan fails: a Dependabot update when upstream has a fixed release, a patch layer when the findings are OS packages, and ignore entries of at most 30 days with a statement when neither exists yet.
- **The image in the release**: an entry in the image list of `ops/scripts/build-release-images.sh`; the scan upload, the build provenance and the software bill of materials (SBOM) steps for it in `.github/actions/release-images/action.yml`; its digest in `release.json`. The compose file takes `NFHOTEL_NGINX_IMAGE`, with a local build of the Dockerfile as the default for the development, end-to-end and dynamic application security testing (DAST) stacks.
- **The deploy of the digest**: `ops/scripts/cd-steps.sh` reads, verifies and passes the `nfhotel-nginx` digest; `ops/scripts/deploy-release.sh` checks it, starts the proxy with it, writes it in the release log, keeps it in `prune`, and still rolls back to a release from before this change (a log line with four digests starts the proxy from the pinned image of that release's compose file).
- **Dependabot**: a `docker` entry for `/ops/deploy/nginx`, so a new upstream build of the base is still proposed, and a limit of open pull requests for the compose files that lets every pinned image be proposed in one weekly run.
- **The daily scan of the third-party images**: `ops/scripts/scan-running-release.sh` also scans every third-party image pinned in the compose files of the VM stacks and the monitoring stack, at the operations commit of the running release, with the same gate; the summary for the issue lists them in their own section.
- **The wiki**: the nginx rows of the pages Containers-and-Images and Proxmox-Deployment (which still names `nginx:1.27-alpine`), and the release, continuous deployment (CD) and daily-scan rows of the page Workflows.
- **The plan for the rest**: a Proposed milestone with one task per third-party image that the first full daily scan fails, each with its remedy under ADR-0040.
- **The proof on the host**: the CD run of the merge verifies and deploys the digest of `nfhotel-nginx` from its release, and a run of the daily scan after the deploy covers it and every third-party image.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then task 1 and its review, then tasks 2 to 7 on one branch, task 8 with the pull request and the deploy, task 9 from the first full daily scan, then task 10.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0040 is Accepted with a Go review record against `QC-ADR-001` before the first change of tasks 2 to 7 | Yes | No |
| 2 | The container `nfhotel-nginx-nginx-1` on the VM runs `ghcr.io/nf-hotel/nfhotel-nginx@sha256:<digest>`, the digest in the `release.json` of the CD run of the merge; the deploy log and `docker inspect` on the VM show the same digest | Yes | No |
| 3 | The release scan of that digest has 0 critical and 0 high findings with a fix (`scan-gate.sh`), against 1 critical and 54 high with a fix in the pinned upstream digest on 2026-11-08; the smoke test of the deploy (port 80 answers 301, HTTPS (HTTP over TLS) answers 200) and `test-nginx.sh` pass | Yes | No |
| 4 | The verify step of CD accepts the build provenance and the SBOM of that digest with `gh attestation verify`, as for the four other images; a manifest without a valid `nfhotel-nginx` digest is refused, proven by `test-cd-steps.sh` | Yes | No |
| 5 | `deploy-release.sh` refuses a deploy without a valid `nfhotel-nginx` digest, writes it in the release log, keeps the images of the two latest releases in `prune`, and rolls back to a release whose log line has four digests; proven by `test-deploy-release.sh` in Ops CI | Yes | No |
| 6 | `.github/dependabot.yml` has a `docker` entry for `/ops/deploy/nginx`, with the schedule, the cooldown and the commit prefix of the other image entries, and its `docker-compose` entry has an `open-pull-requests-limit` of at least the number of images it watches | Yes | No |
| 7 | One run of `running-image-scan.yaml` after the deploy lists every third-party image pinned in the compose files of the VM stacks and the monitoring stack, each by its digest at the operations commit of the running release, in its own section of the summary; `test-scan-running-release.sh` proves that a compose file with a known vulnerable image fails the scan | Yes | No |
| 8 | The wiki pages Containers-and-Images, Proxmox-Deployment and Workflows name the patched image and the wider daily scan, and `test-wiki-coverage.sh` passes | Yes | No |
| 9 | The code review against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every job of Ops CI and the release check of the pull request is green | Yes | No |
| 10 | A Proposed milestone has one task for each third-party image that the first full daily scan fails, each with its remedy under ADR-0040, and its dry run of `sync-project.sh` lists them | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-022 | The release gate, the SBOM and the build provenance that the patched image goes through |
| MIL-036 | The daily scan of the running release, which task 6 extends to the third-party images |
| MIL-059 | Dependabot's watch of the pinned third-party images; for nginx it moves from the compose file to the Dockerfile |
| ADR-0007 | Decisions 2, 3 and 6: build once in the release, verify the attestations before a deploy, deploy by digest; the VM never builds |
| ADR-0030 | The precedent: a patch layer on a fixed base, through the full release path |
| ADR-0038 | The CD run of the merge, and so the proof of criteria 2 to 4, waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 success criterion 27: every deployed digest has an SBOM and a provenance attestation that `gh attestation verify` accepts; 0 critical and 0 fixable high findings in the deployed images | Criteria 2 to 5 restore it for the proxy; criteria 7 and 10 measure it and plan it for the other third-party images |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-15. It adds one image to the release path, extends one scan and plans the rest; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0040: a patched nginx image from the release | Record the choice of a patch layer on the pinned `nginx:1.29-alpine` digest, built, scanned and attested by the release like the market-analysis image of ADR-0030, against waiting for an upstream build, ignore entries of at most 30 days, Alpine's own nginx package and a build on the VM. Decide whether the deploy command takes the release digests by position, as today, or by name, so that a second patched image (the census names `tecnativa/docker-socket-proxy`) does not change its arguments again. Set the rule for a third-party image that the daily scan fails (a Dependabot update, a patch layer, or ignore entries of at most 30 days with a statement), and say how later findings in the patched image are fixed (the next release upgrades again). Reviewed against `QC-ADR-001`. | No | criterion 1 |
| 2 | The patched image in the release | `ops/deploy/nginx/Dockerfile` (`FROM` the pinned digest, `RUN apk upgrade --no-cache`); the compose file takes `NFHOTEL_NGINX_IMAGE` with a local build as the default; an entry in the image list of `build-release-images.sh`; the scan upload, provenance and SBOM steps for it in `.github/actions/release-images/action.yml`; the digest in `release.json`. The end-to-end and DAST stacks build it as they build the other images. | No | criteria 2, 3 |
| 3 | CD and the deploy command take the nginx digest | `cd-steps.sh`: the name in `IMAGE_NAMES`, the verify case and the digest for `nfhotel-deploy deploy`, in the form ADR-0040 chooses; the deploy-release action passes it. `deploy-release.sh`: the digest checked against its pattern, `NFHOTEL_NGINX_IMAGE` for `deploy-services.sh`, the digest in the release line of the log, `previous_release` and `rollback` that read the lines from before and after the change (a release line has seven fields today, and `previous_release` counts only those), and `prune` that keeps it. The `update` step installs the new command before `deploy` runs, so the CD run of the merge already uses it. | No | criteria 2, 4, 5 |
| 4 | Tests of the deploy chain | `test-cd-steps.sh`: a manifest with a missing or malformed `nfhotel-nginx` digest is refused. `test-deploy-release.sh`: a deploy with the nginx digest runs and is logged; one without it is refused; a rollback to a log line with four digests starts the proxy from the compose file of that release; `prune` keeps the nginx digest of both releases. | No | criteria 4, 5, 9 |
| 5 | Dependabot for the base of the proxy and every pin | A `docker` entry for `/ops/deploy/nginx` in `.github/dependabot.yml` (weekly, the seven-day cooldown and the prefix `Images:` of the other image entries), so a new build of `nginx:1.29-alpine` is proposed as a change of the `FROM` line. An `open-pull-requests-limit` on the `docker-compose` entry of at least the number of images it watches: on 2026-11-04 it opened five pull requests, the default limit, while Prometheus, Alertmanager and the Blackbox exporter stayed versions behind. | No | criterion 6 |
| 6 | The daily scan of the third-party images | `scan-running-release.sh` reads the images pinned in the compose files of the VM stacks and the monitoring stack at the operations commit of `release.json` (`devops_sha`) and scans each by its digest with the gate of `scan-gate.sh`; the summary for the issue lists them in their own section. `running-image-scan.yaml` checks out that commit. `test-scan-running-release.sh`: a compose file with a known vulnerable image fails the scan, and one without findings passes. On 2026-11-08, eleven of the twelve would fail. | No | criterion 7 |
| 7 | The wiki rows | Containers-and-Images: the nginx row names `ghcr.io/nf-hotel/nfhotel-nginx`, built from `nginx:1.29-alpine` with the patch layer. Proxmox-Deployment: the nginx row, which still says `nginx:1.27-alpine`. Workflows: the release and CD rows name the nginx image, and the row of the daily scan names the third-party images. `test-wiki-coverage.sh` passes. | No | criterion 8 |
| 8 | Code review, pull request and deploy | The review records of the scripts and workflows against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` before the pull request; the pull request closes one issue per task and carries the milestone; S01 merges once every required check is green. The CD run of the merge, approved by S01 (ADR-0038), is the proof of criteria 2 to 4, and one run of the daily scan after it is the proof of criterion 7. | No | criteria 2 to 4, 7, 9 |
| 9 | The plan for the other third-party images | From the first full daily scan after the deploy, a new Proposed milestone with one task per third-party image that fails, each with its remedy under ADR-0040, checked with a scan: the newer version that clears it, a patch layer, or ignore entries of at most 30 days with a statement. The census of 2026-11-08 found eleven: postgres, ollama, cloudflared, `docker-socket-proxy` and Alloy on the VM; Prometheus, Alertmanager, the Blackbox exporter, Loki, Grafana and the Python image of the scan exporter in the monitoring stack. | No | criterion 10 |
| 10 | Gate and review record | The gate review record against criteria 1 to 10 with the logs of the CD run and the daily scan, the row of MIL-064 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 10 |

---

[BC-001]: ../business-case.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0030]: ../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-036]: ./mil-036-security-operations.md
[MIL-059]: ./mil-059-github-actions-security-audit.md
[401da1a]: https://github.com/NF-Hotel/NFHotel/commit/401da1a2cc4c724997c1ad0e61ef770af5559245
