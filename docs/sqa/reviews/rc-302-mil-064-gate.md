# RC-302 Gate review of MIL-064 A patched nginx image and a daily scan of the third-party images

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-302 |
| CrossReference | [MIL-064], [MIL-065], [RC-298], [RC-299], [RC-300], [RC-301], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Proposed | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-064 after the CD run of the merge and the first full daily scan; Go-with-conditions until `docker inspect` on the VM is read for criterion 2 | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-064] at its gate: the ten Go/No-Go criteria, with the evidence of pull request #542 (merged as b9ab513 on 2026-11-08), the release run 42442428848 and the CD run 42442894208 of that commit, and the run 42443407440 of the daily scan after the deploy. Before the gate: [RC-298] (the plan, Go), [RC-299] (ADR-0040, Go) and [RC-300] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-064]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 5, 6, 8 and 9 cite RC-300 and the green checks of #542.
- Language and domain: en / it (the Metadata rows of MIL-064); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 approved the plan (RC-298) and ADR-0040 (RC-299) in chat and lifted the hold on git for MIL-064 ("Ja, start nu"). On 2026-11-08 S01 asked the AI to decide as S01 in autonomous mode ("Du beslutter somom du er mig i auto mode on. Merge PR og kør CICD , og fix resten og test det virker og update alle sider og docs"). The AI merged #542, approved the CD run of the merge for S01 (ADR-0038), started the daily scan and wrote [MIL-065] with its review [RC-301]. The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence of 2026-11-08:**

- **The pull request.** #542: 49 checks passed and 2 skipped by design (`OWASP ZAP` and `DAST full scan`), among them `Ops linters`, `Ops nginx routes` (`test-nginx.sh`), `Ops script tests (test-cd-steps.sh)`, `Ops script tests (test-deploy-release.sh)`, `Ops script tests (test-scan-running-release.sh)`, `Ops wiki coverage` and `Release build (no push)`; merge commit b9ab513.
- **The release.** Run 42442428848 of b9ab513 built `nfhotel-nginx` as its fifth image, logged "PASS ghcr.io/nf-hotel/nfhotel-nginx:sha-b9ab513…: no secret, no critical finding and no high finding with a fix" at 03:27 UTC, pushed it as `ghcr.io/nf-hotel/nfhotel-nginx@sha256:c857c6ad3edbca3d1ed53e746aa4ad18b1d5b88712abc84c0bd3e9d42e49f150` and attested its build provenance and its software bill of materials (SBOM).
- **The deploy.** CD run 42442894208 of b9ab513, approved for S01 and finished at 03:33:39 UTC. Its verify step logged "build provenance and SBOM verified" for each of the five digests of the release, the proxy digest `c857c6ad3edb…` among them. On the VM the deploy pulled that digest, recreated `nfhotel-nginx-nginx-1`, which was healthy at 03:33:32, and `docker compose ps` lists the container with `ghcr.io/nf-hotel/nfhotel-nginx@sha256:c857c6ad3edb…`. The smoke test answered 301 on port 80 and 200 on port 443, the log ends "deployed b9ab51381c84…", and the prune removed 5 images that no release of the last two needs (71 GB to 72 GB free). The packages are private: an anonymous request for the manifest of `nfhotel-nginx` gets HTTP 403, so the VM pulled with a read credential of its own.
- **The daily scan.** Run 42443407440 (started by hand at 03:36 UTC) scanned the five release images, which all pass, and the twelve third-party images that the compose files of the production stacks pin at the operations commit b9ab513, each with its stack and digest; eleven fail the gate and `haproxy:3.2-alpine` passes. The run opened issue #553 "Vulnerabilities in the release in production".

## Checklist Results

| # | Criterion (MIL-064) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0040 is Accepted with a Go review record against `QC-ADR-001` before the first change of tasks 2 to 7 | Pass | RC-299 (Go, S01) and the Accepted row of ADR-0040 are in 3ed3361 (00:41 UTC); the first change of tasks 2 to 4 is 0ae8e27 (02:18 UTC), which descends from it |
| 2 | The container `nfhotel-nginx-nginx-1` on the VM runs `ghcr.io/nf-hotel/nfhotel-nginx@sha256:<digest>`, the digest in the `release.json` of the CD run of the merge; the deploy log and `docker inspect` on the VM show the same digest | Pending | The `release.json` of the run names `c857c6ad3edb…` (the release images of the deploy step), and the deploy log pulls that digest, recreates the container and lists it in `docker compose ps`. `docker inspect` on the VM is not read yet: Tailscale SSH to the VM is in check mode, and the check that it asks S01 for was not given in time (action item 3) |
| 3 | The release scan of that digest has 0 critical and 0 high findings with a fix (`scan-gate.sh`), against 1 critical and 54 high with a fix in the pinned upstream digest on 2026-11-08; the smoke test of the deploy (port 80 answers 301, HTTPS answers 200) and `test-nginx.sh` pass | Pass | Release run 42442428848: "PASS … no secret, no critical finding and no high finding with a fix"; the daily scan of the deployed digest passes as well; the smoke test of CD run 42442894208 answered 301 and 200; `Ops nginx routes` green on #542 |
| 4 | The verify step of CD accepts the build provenance and the SBOM of that digest with `gh attestation verify`, as for the four other images; a manifest without a valid `nfhotel-nginx` digest is refused, proven by `test-cd-steps.sh` | Pass | CD run 42442894208: "==> verifying ghcr.io/nf-hotel/nfhotel-nginx@sha256:c857c6ad3edb…", then "build provenance and SBOM verified", as for the four other digests; `Ops script tests (test-cd-steps.sh)` green on #542 (RC-300) |
| 5 | `deploy-release.sh` refuses a deploy without a valid `nfhotel-nginx` digest, writes it in the release log, keeps the images of the two latest releases in `prune`, and rolls back to a release whose log line has four digests; proven by `test-deploy-release.sh` in Ops CI | Pass | `Ops script tests (test-deploy-release.sh)` green on #542 (RC-300); on the VM the prune of the deploy kept the two latest releases ("no release of the last two needs") |
| 6 | `.github/dependabot.yml` has a `docker` entry for `/ops/deploy/nginx`, with the schedule, the cooldown and the commit prefix of the other image entries, and its `docker-compose` entry has an `open-pull-requests-limit` of at least the number of images it watches | Pass | At b9ab513 the `docker` entry lists `/ops/deploy/nginx` with `interval: weekly`, `default-days: 7` and the prefix `Images:`, and the `docker-compose` entry has `open-pull-requests-limit: 15` for the 13 images that its folders pin (RC-300) |
| 7 | One run of `running-image-scan.yaml` after the deploy lists every third-party image pinned in the compose files of the VM stacks and the monitoring stack, each by its digest at the operations commit of the running release, in its own section of the summary; `test-scan-running-release.sh` proves that a compose file with a known vulnerable image fails the scan | Pass | Run 42443407440: the section "Third-party images" of issue #553 names the twelve images, each with its stack and its digest at b9ab513, and the summary has a section of findings for each of the eleven that fail; `Ops script tests (test-scan-running-release.sh)` green on #542 |
| 8 | The wiki pages Containers-and-Images, Proxmox-Deployment and Workflows name the patched image and the wider daily scan, and `test-wiki-coverage.sh` passes | Pass | The pages of task 7 (RC-300); `Ops wiki coverage` green on #542, and 91 of 91 with the corrections of this record (finding 3) |
| 9 | The code review against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` is Go, `lint.sh` is clean, and every job of Ops CI and the release check of the pull request is green | Pass | RC-300 (Go); `Ops linters`, every `Ops …` job and `Release build (no push)` green on #542 |
| 10 | A Proposed milestone has one task for each third-party image that the first full daily scan fails, each with its remedy under ADR-0040, and its dry run of `sync-project.sh` lists them | Pass | [MIL-065] (Proposed; RC-301 Go-with-conditions): tasks 1 to 11, one for each of the eleven images that run 42443407440 fails, each with its remedy under ADR-0040 decision 5, checked with candidate scans on 2026-11-08; the dry run of `sync-project.sh --milestone MIL-065` lists the milestone and its fourteen issues |

## Findings

- **The proxy passes; eleven images do not yet.** The proxy went from 1 critical and 54 high findings with a fix to none, in the release scan and in the daily scan. The daily scan fails eleven of the twelve third-party images, as the census of the plan found, so BC-001 success criterion 27 stays unmet for them until MIL-065 is done, and MIL-065 waits for S01 (RC-301).
- **A failing image does not turn the run red.** Run 42443407440 succeeded and opened issue #553: the workflow reports a finding in the issue, and the colour of the run is kept for the scan itself (the comment of the step in `running-image-scan.yaml`). The issue is the signal.
- **The wiki had drifted.** Containers-and-Images named six tags that the compose files no longer pin (cloudflared 2026.10.3, ollama 0.40.8, Alloy v1.11.3, the socket proxy v0.4.1, Loki 3.5.7, Grafana 12.2.1): `test-wiki-coverage.sh` checks the names of the services, not the tags, and a Dependabot pull request moves only the pin. Step 3 of the page CI-CD said to make the packages public, but they are private, and the deploy is built for that. Both are corrected in the pull request of this record; neither is part of criterion 8. Whether the coverage test should also check the tags, so that each Dependabot pull request for a compose file must update the page, is S01's choice (action item 2).
- **Decision D2 is open on paper.** Section 15 of DSA-001 recommends public packages (D2, decision owner S01). The packages are private, CD verifies them with the job token, and the VM pulls with its own credential. Recording the decision is S01's (action item 1).
- **The running container is known from the deploy log only.** `docker inspect` on the VM would show what runs now, not only what the deploy started. Tailscale SSH to the VM asks S01 for a check in the browser, and that check was not given while this record was written.

## Overall Verdict

Go-with-conditions. Nine of the ten criteria pass on the green pull request, RC-300, the release run, the CD run of the merge and the daily scan. Criterion 2 is Pending until `docker inspect` on the VM has been read, so the gate is not passed yet: issues #539 and #540 close through the pull request of this record, and issue #541 and the GitHub milestone #46 stay open until criterion 2 passes. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Record decision D2 of DSA-001 as taken: private packages that the VM pulls with its own read credential, or make the packages public | S01 | 2026-11-15 |
| Choose whether `test-wiki-coverage.sh` also checks the tags of the pinned images on Containers-and-Images | S01 | With the acceptance of MIL-065 |
| Approve the Tailscale check of `docker inspect` on the VM, so that criterion 2 can be read and this record closed | S01 | 2026-11-15 |

---

[MIL-064]: ../../milestones/mil-064-patched-nginx-and-third-party-scan.md
[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[RC-298]: ./rc-298-mil-064.md
[RC-299]: ./rc-299-adr-0040.md
[RC-300]: ./rc-300-mil-064-code.md
[RC-301]: ./rc-301-mil-065.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
