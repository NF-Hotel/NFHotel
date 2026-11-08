# ADR-0040 A patched nginx image from the release

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0040 |
| CrossReference | [ADR-0007], [ADR-0030], [MIL-022], [MIL-036], [MIL-059], [MIL-064] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version; task 1 of MIL-064, which S01 accepted in chat on 2026-11-08 (RC-298 Go) and asked to start the same day ("Ja, start nu"); S01 approved it in chat on 2026-11-08 ("Godkend ADR'en") after RC-299 Go | [3ed3361] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** S01 accepted [MIL-064] in chat on 2026-11-08 and asked the same day to start the work. This record was written by the AI assistant as task 1 of that milestone; S01 reviews it, and tasks 2 to 7 wait for its acceptance (MIL-064 criterion 1).

**The problem.** The reverse proxy runs the third-party image `nginx:1.29-alpine@sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de`, pinned in `ops/deploy/nginx/docker-compose.yml`. Every request to the hotel passes it, from the internet through the tunnel and from the tailnet. On 2026-11-08 the scanner and the gate of the release (Trivy 0.75.0, `scan-gate.sh`) find 1 critical and 54 high findings with a fix in it, all in packages of Alpine 3.23.4 (curl, libcurl, libexpat, libssl3, libcrypto3, musl, zlib and eight more). The tag still points at the pinned digest, so Dependabot has nothing to propose. Business Case criterion 27 asks for 0 critical and 0 fixable high findings in the deployed images; the release gate enforces it only for the four images that the release builds ([MIL-022]), and the daily scan of [MIL-036] scans only those four.

The proxy is not alone: on the same day eleven of the other twelve third-party images pinned in the compose files fail the same gate. Most of their findings are Go modules compiled into the binaries, which only a newer upstream release fixes; MIL-064 task 9 plans them from the first full daily scan. This record decides the proxy, and the rule for the others.

**Forces.**

- Production builds nothing: the release builds each image once, continuous deployment (CD) verifies its attestations, and the virtual machine (VM) pulls it by digest ([ADR-0007], decisions 2, 3 and 6).
- `apk upgrade` installs the packages of the day of the build, so a patched image is rebuilt, not reproduced; only the digest that the release scanned may run.
- CD passes the four release digests to `nfhotel-deploy deploy` by position. `deploy-release.sh` writes them in the release log after the two commits, and `previous_release`, `rollback` and `prune` read a release line as seven fields. A fifth image changes all of them, and the census names a second candidate for a patch layer: `tecnativa/docker-socket-proxy`, whose nine findings are all Alpine packages.
- A rollback must still reach a release from before the change. Its log line has four digests, and its operations commit has the compose file that pins the upstream image.

Options considered:

1. **Wait for an upstream build.** Dependabot proposes the new digest when the tag is rebuilt. It costs nothing, but there is no date, and the proxy keeps 55 known findings with a fix until then.
2. **Accept the findings** with ignore entries of at most 30 days in `ops/security/trivyignore.yaml`. It records the risk and fixes nothing, although the fixes exist.
3. **Alpine's own nginx package on a plain Alpine base.** It gets the Alpine security updates, but nginx is then another build, with other paths and without the entry point of the upstream image that renders the templates of `ops/deploy/nginx/templates`; the configuration and its tests would change with it.
4. **Build or upgrade on the VM.** It breaks ADR-0007: the VM never builds, and an image built there has no scan, no software bill of materials (SBOM) and no provenance.
5. **A patch layer from the release.** `ops/deploy/nginx/Dockerfile` starts `FROM` the pinned digest and runs `apk upgrade --no-cache`. The release builds it as a fifth image, scans it with the gate and attests its SBOM and build provenance; CD verifies and deploys its digest. A lab test on 2026-11-06 upgraded 25 packages, moved Alpine from 3.23.4 to 3.23.8 and left 0 high or critical findings; nginx stayed at 1.29.8 and served the site. The image is 127 MB against 93.5 MB.

For the deploy command, two forms:

- **a. By position**, as today: a fifth argument, release lines of eight fields, and every reader of the log changed again for the next patched image.
- **b. By name**: each argument is a full reference, `ghcr.io/nf-hotel/<name>@sha256:<digest>`, and the command finds the image by its name. The release log already holds full references, so the lines written before the change read as they are, and the next patched image adds a name, not a position.

## Decision

We follow option 5 with form b: the proxy runs `ghcr.io/nf-hotel/nfhotel-nginx`, a patch layer on the pinned upstream digest that the release builds, scans and attests, and the deploy command takes the release digests by name.

1. **The image.** `ops/deploy/nginx/Dockerfile` is `FROM nginx:1.29-alpine@sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de` and `RUN apk upgrade --no-cache`. Nothing else changes: the entry point, the templates, the user and the hardening of the compose file stay. The compose file takes `image: ${NFHOTEL_NGINX_IMAGE:-nfhotel-nginx:local}` with a `build:` of the Dockerfile, so the development, end-to-end and dynamic application security testing (DAST) stacks build it locally. The Alpine 3.23 repository carries nginx 1.28.3, older than the 1.29.8 of the upstream image, so `apk upgrade` leaves nginx itself as it is (`apk policy nginx` on 2026-11-08); task 2 adds a check that the built image reports the nginx version of its base.
2. **The release path.** `build-release-images.sh` gets the entry `nfhotel-nginx nginx NFHOTEL_NGINX_IMAGE NGINX_ENV_FILE nginx`. The release action uploads its scan and attests its SBOM and build provenance like the other four, and `release.json` holds its digest. CD verifies it with `gh attestation verify` and refuses a manifest without it.
3. **Digests by name.** `nfhotel-deploy deploy <app-sha> <devops-sha> <reference>... [--drill]`. Every reference must match `ghcr.io/nf-hotel/<name>@sha256:<64 hex>` with a known name; a name may appear once, and a deploy needs all five. The command sets the image variable of each name for `deploy-services.sh`. A release line of the log stays the timestamp, the two commits and the references, as today; `previous_release`, `rollback` and `prune` read the references of a line by their names. A line with four references, from before the change, still rolls back, because its operations commit pins the upstream image in the compose file.
4. **Later findings in the proxy.** The next release upgrades the layer again. When the daily scan fails the deployed digest, its issue says that a release is due. A new build of the upstream tag is proposed by Dependabot through a `docker` entry for `/ops/deploy/nginx`, as a change of the `FROM` line. A finding in a package that `apk upgrade` does not move, such as nginx itself, which the lab test left at 1.29.8, waits for the upstream image.
5. **The rule for the other third-party images.** When the daily scan fails a third-party image: (1) when upstream has a release or a build of the pinned tag that passes the gate, the pin moves to it, through the Dependabot pull request or a manual bump when Dependabot is behind; (2) when the findings are only operating-system packages and no such release exists, the image gets a patch layer from the release, as here; (3) when neither exists, each finding gets an ignore entry of at most 30 days with a statement, checked again before it is renewed. MIL-064 task 9 applies the rule to each image.

## Consequences

**Positive:**

- The 1 critical and 54 high findings with a fix leave the image that every request passes, and criterion 27 holds for it again.
- The proxy gets what the four release images have: a scan at every release, an SBOM, a build provenance attestation and the verification by CD.
- The deploy command and the release log take the next patched image without a new argument position, and the lines written before the change still serve a rollback.
- Every failing third-party image gets one of three remedies, and an accepted finding ends after at most 30 days.

**Negative:**

- The image is no longer the upstream build. It is about 33 MB larger, and it is rebuilt at each release with the packages of that day, so two releases of the same commit can differ; only the scanned digest runs.
- A fixable finding that `apk upgrade` cannot reach, such as one in nginx itself, now stops the release of every image until the upstream image brings the fix or the finding is accepted for 30 days, as ADR-0030 accepted for market-analysis.
- The deploy command, its tests, `cd-steps.sh` and the deploy-release action change in the same release. A mistake there stops CD, and the rollback across the change is the riskiest path, so it gets its own test (MIL-064 criterion 5).
- The release, and the release check of a pull request, build one more image and take longer.

**Quality:** security improves (the known fixable findings leave the proxy, and every third-party image gets a remedy); the reliability of the release path is slightly lower, because one more image can stop it; maintainability improves for the next patched image, because the deploy arguments no longer depend on their position (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0007]: the release builds five images, and the deploy command takes the release digests by name.
- [MIL-036]: the daily scan also scans the third-party images (MIL-064 task 6).
- [MIL-059]: Dependabot watches the base of the proxy through a `docker` entry.
- [MIL-064]: tasks 2 to 9 implement this record.

---

[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0030]: ./adr-0030-patch-layer-on-frozen-market-analysis-image.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-036]: ../milestones/mil-036-security-operations.md
[MIL-059]: ../milestones/mil-059-github-actions-security-audit.md
[MIL-064]: ../milestones/mil-064-patched-nginx-and-third-party-scan.md
[3ed3361]: https://github.com/NF-Hotel/NFHotel/commit/3ed3361542de05c137c9978e95531f6dba98e8f2
