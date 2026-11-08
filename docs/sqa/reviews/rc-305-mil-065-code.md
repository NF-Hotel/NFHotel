# RC-305 Code review of MIL-065: the third-party pins, the patched socket proxy, the accepted findings and the wiki

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-305 |
| CrossReference | [MIL-065], [ADR-0040], [DSA-001], [RC-300], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Proposed | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 13 of MIL-065 before its pull request (task 14); Go-with-conditions until S01 approves the risk exceptions of `ops/security/trivyignore.yaml` | [339f707] |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-065-third-party-image-remedies` against `main` at a0c796c; the commits 774425e, b604034, 5f32aba and dd55a67):
  - shell: `ops/scripts/build-release-images.sh` (the sixth image, the throwaway `NFHOTEL_JOURNAL_GID` of the build), `ops/scripts/cd-steps.sh` (`IMAGE_NAMES`), `ops/scripts/deploy-release.sh` (`NAMES`, `ADDED_NAMES`, the header of the release lines), `ops/scripts/deploy-services.sh` (`--build` for the Alloy stack outside a release, `--no-build` in a release), and the tests `ops/tests/check-release-build.sh`, `test-cd-steps.sh`, `test-deploy-release.sh`, `test-deploy-services-env.sh` and `test-wiki-coverage.sh` (tasks 4 and 12);
  - workflows and actions: `.github/workflows/release.yaml` and `release-check.yaml` (headers), `.github/actions/release-images/action.yml` (the scan upload, the provenance and the SBOM of `nfhotel-socket-proxy`) (task 4);
  - YAML: the compose files of `ops/deploy/postgres`, `cloudflared`, `monitoring` and `alloy` (tasks 1 to 11), `ops/security/trivyignore.yaml` (tasks 1 to 3 and 5 to 11) and `.github/dependabot.yml` (task 4); `ops/deploy/alloy/socket-proxy/Dockerfile` and `SonarQube.Analysis.xml` have no checklist of their own and were read against decision 5 of [ADR-0040] and finding 5 of [RC-300];
  - the wiki pages of task 12: Containers-and-Images, CI-CD, Workflows, Security-Controls, DevOps-Lifecycle and Technology-Choices; section 15 of [DSA-001] (task 13).
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001].
- **Scope:** delta review of the changed files against `main`.
- **Language and domain:** n/a (technical type).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the code and this record. S01 accepted the plan of MIL-065 in chat on 2026-11-08 ("godkendt", RC-304) after asking for the last milestones in autonomous mode ("Fortsæt så med at lave de sidste milestones i automode on"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge. The risk exceptions below are not the stand-in's to approve: section 8 of [DSA-001] names S01.

**Tools and runs of 2026-11-08, on the PC of S01 (Docker Desktop, Docker 29.7.2):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean.
- `ops/tests/test-workflow-policy.sh`: the 14 workflows and actions follow the policy of ADR-0037; its self-test 8 of 8.
- In `debian:12-slim`, as the jobs of Ops CI run them: `test-cd-steps.sh` 97 (90 before), `test-deploy-release.sh` 94 (87), `test-deploy-services-env.sh` 17 (16), `test-scan-gate.sh` 26, `test-scan-running-release.sh` 41, `test-publish-wiki.sh` 9, `test-check-secret-env.sh` 7, `test-write-secret-files.sh` 15; `test-wiki-coverage.sh` 111 (91 before), with the repository mounted; `scan-gate.sh --check-ignore` on `ops/security/trivyignore.yaml` passes.
- `test-wiki-coverage.sh` before the pages of task 12 were changed: 104 passed and 7 failed, on the five tags that tasks 3 and 5 to 8 move and on the two .NET images, which the page named without their registry; its stale-tag check passed.
- `ops/tests/test-service-networks.sh` in `docker:29-cli` with bash and jq: 49 of 49.
- The compose build of the socket proxy, as the release builds it: the image carries `org.opencontainers.image.source` and `org.opencontainers.image.revision` (dd55a67), and `haproxy -v` gives the same line as the pinned upstream image (3.4.2-9feeb3287), so the upgrade left HAProxy as it was.
- Trivy 0.75.0 with the gate of `scan-gate.sh`, through `scan-running-release.sh --third-party` on the compose files and the ignore file of the branch: ten of the eleven third-party images pass. Loki 3.7.9 could not be read from the image store of Docker Desktop ("file blobs/sha256/… not found in tar"), an error of the scan and not a finding; read from the registry (`--image-src remote`) it passes the gate. The socket proxy built from its Dockerfile passes `scan-image.sh` with the gate. The native jq of Windows writes CRLF, which the manifest check of the script refuses; the run used `jq -b`, which writes LF as jq does on the runner.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1 | Header line and `set` options | Pass | The four changed scripts keep `#!/usr/bin/env bash` and `set -euo pipefail`; the tests keep `set -uo pipefail`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | The names of a release are arrays (`NAMES`, `ADDED_NAMES`, `IMAGE_NAMES`, `IMAGES`); `--no-build` is added to the `args` array; the wiki test reads its names into arrays and finds the Dockerfiles with `find … -exec awk … {} +`, so a path is never split |
| SH 3 | Names | Pass | `snake_case` functions (`has_name`, `third_party_refs`, `third_party_images`, `third_party_tags`) and variables (`probe`, `stale_page`); `UPPER_SNAKE` constants; the new folder `socket-proxy` is kebab-case |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean; no `disable` comment added |
| SH 5 | Errors | Pass | A deploy without a valid socket proxy reference is refused before anything runs ("no reference of nfhotel-socket-proxy", exit 2); `read-manifest` refuses a manifest without the digest or with an image of another registry; `verify` refuses an unattested digest; a release deploy passes `--no-build`, so a failed pull fails the deploy, which rolls back; all tested |
| SH 6 | Temporary files | Pass | The copy of the page in the wiki test is a `mktemp` file with `trap 'rm -f …' EXIT` |
| SH 7 | Secrets | Pass | None added; `NFHOTEL_JOURNAL_GID=0` is a throwaway value that only lets the compose file of Alloy load for the build |
| SH 8 | State outside its folder | Pass | No new state is changed; `--no-build` makes a release deploy stricter, not wider |
| SH 9 | Header | Pass | The headers name the six images and the release lines with four or five references; the header of the wiki test describes the tag check and its stale-tag check and names the tools it needs |
| SH 10 | Implements the task it cites | Pass | Tasks 1 to 13 of MIL-065 as planned. Deviations: the wiki update covers six pages, not four, because DevOps-Lifecycle and Technology-Choices named five release images; the wiki test finds the Dockerfiles at any depth (finding 4); the pull request refers to #553 and does not close it, because the daily scan closes it after a clean run (criterion 1) |
| SH 11 | Tested for success, failure and the bypass path | Pass | `test-cd-steps.sh`: the six references by name; a manifest without the socket proxy digest or with an image of another registry is refused; `verify` checks its attestations and refuses an unattested digest. `test-deploy-release.sh`: a deploy passes the digest to Alloy; a release without it or with a malformed one is refused and named; a rollback to a line with four or five references starts the socket proxy from the compose file of that release; a failed deploy rolls back to a line with five references; `prune` keeps the socket proxy images of the kept releases and removes the unused upstream one. `test-deploy-services-env.sh`: a release passes `--no-build` and never `--build`. `test-wiki-coverage.sh`: the stale-tag check on a copy of the page |
| SH 13 | Safe to re-run | Pass | A deploy of the same release recreates nothing; the wiki test changes only its temporary copy |
| SH 14 | Needs stated | Pass | The header of the wiki test names bash, awk, find, grep, mktemp and sed; the others are unchanged |
| SH 15 | Knowledge once | Pass | The list of release images stays in four files, an allowlist on each side of the tailnet ([RC-300] finding 2), now with six names in each. The tags on Containers-and-Images repeat the pins for the reader, and the wiki test fails when they drift apart |
| WF 1, YML 1 to 3 | YAML checks | Pass | yamllint 1.35.1 clean; LF, two spaces, no duplicate key |
| WF 4 | Header comment | Pass | `release.yaml` names the six packages it changes and the six digests of `release.json`; `release-check.yaml` and the action name the six images |
| WF 5 | Step names | Pass | "Upload the scan of nfhotel-socket-proxy", "Attest the provenance of nfhotel-socket-proxy" and "Attest the SBOM of nfhotel-socket-proxy" are unique in the action |
| WF 6 | actionlint | Pass | actionlint 1.7.7 clean |
| WF 7 | Least permissions | Pass | No permission added |
| WF 8 | Pinned actions | Pass | `github/codeql-action/upload-sarif@9b325408…` (v4.39.1), `actions/attest-build-provenance@4d101475…` (v4.2.2) and `actions/attest@1e69f48a…` (v4.2.2): the SHAs the action already uses; the policy test passes |
| WF 9 | Secrets | Pass | None added |
| WF 10 | Untrusted text | Pass | The digest reaches the attest steps through `with:`, from the step output that `release-steps.sh` writes from the digest files of the build, which `build-release-images.sh` checks against `^[a-z0-9./_-]+@sha256:[0-9a-f]{64}$`; no `${{ }}` in a `run:` |
| WF 11 | Triggers with secrets on untrusted code | Pass | Unchanged |
| WF 12 | Changes outside the repository | Pass | The release publishes the sixth package from `main` only, after the gate |
| WF 13 | Token scope | Pass | The job token, as before |
| WF 14, 15, 16, 17 | Runner, timeout, concurrency, triggers, pinned tools | Pass | Unchanged; Trivy keeps the pinned version of `scan-image.sh` |
| WF 19 | A failing step names the reason | Pass | Unchanged; the refusals above name the image |
| WF 20, YML 7 | At most five lines of embedded program text | Pass | No `run:` block added or grown |
| WF 21 | Shared logic | Pass | The image steps stay in the composite action `release-images` |
| WF 22 | Features verified on the host | Pending | The release check of the pull request builds the six images with the same action; it is the proof on the host |
| WF 24 | A static test of the workflows | Pass | `test-workflow-policy.sh` and actionlint run in Ops CI and pass here |
| YML 4, 5, 6 | Coerced scalars, tags, secrets | Pass | The labels of the build are strings; no tag; no secret |
| YML 8, 9 | Repeated blocks, anchors | Pass | The three steps of `nfhotel-socket-proxy` repeat those of the five images, as a composite action cannot loop; the ignore entries repeat their `paths` per finding, because Trivy reads one finding per entry; no anchor is used |
| YML 10 | Schema | Pass | The compose file of Alloy loads and builds (the compose build above); the other compose files change only an image line |
| YML 11 | Header comment | Pass | `trivyignore.yaml` says that an entry for a third-party image names its files or packages, and what each statement begins with; `dependabot.yml` says why the base of the socket proxy moved to the docker entry and how many images the compose entry pins; the compose file of Alloy says where the image of the socket proxy comes from |
| YML 12, 14 | Key names, file names | Pass | The keys of each tool; the files keep their names |
| YML 15 | A check without the real target | Pass | The script tests and the wiki test above; `scan-gate.sh --check-ignore` reads the ignore file |
| MIL-065 2 | The pins of tasks 1 to 11 | Pass (in the files); Pending (Ops CI) | The compose files pin the versions of the criterion; the jobs of Ops CI that check the monitoring configuration and start the monitoring stack and Alloy run on the pull request |
| MIL-065 4 | `test-cd-steps.sh` and `test-deploy-release.sh` | Pass (in test) | 97 and 94 checks, criterion SH 11 |
| MIL-065 5 | The ignore entries | Pass | 41 entries for 37 vulnerability IDs; each has a statement that begins with what the check found, `expired_at: 2026-12-08`, 30 days ahead, and `paths` or `purls` of a third-party image; `--check-ignore` passes. The gate of the release of the merge is the proof on the host |
| MIL-065 7 | The wiki | Pass | The six pages; `test-wiki-coverage.sh` 111, and it failed on the stale page |
| MIL-065 8 | Review Go, lint clean, the checks green | Pending | `lint.sh` is clean; this record is Go-with-conditions until S01 approves the risk exceptions; the checks of the pull request are the proof on the host |
| MIL-065 9 | Decision D2 | Pass (in the document); Pending (the request) | Section 15 of DSA-001 records the decision; the anonymous request for the six manifests follows the release of the merge, when the sixth package exists |

## Risk Exceptions

The findings that the newest builds of the pinned third-party images still have, accepted in `ops/security/trivyignore.yaml` until 2026-12-08 (MIL-065 tasks 1 to 3 and 5 to 11). The ratings are those of the daily scan of 2026-11-08 (#553) and of the scans of the candidate pins. S01 is the approver that section 8 of [DSA-001] names for a container scan exception; S01 has not approved them yet (action item 1).

| Finding | Owner | Approver | Expiry | Compensating controls |
| --- | --- | --- | --- | --- |
| `postgres:16-alpine`: 26 findings of the Go standard library in `gosu` 1.19, built with Go 1.24.6: one critical (CVE-2025-68121, crypto/tls) and 25 high, each fixed in a newer Go | S01 | S01 | 2026-12-08 | Not in the binary: `gosu` has functions of `os`, `syscall` and `github.com/moby/sys/user` only (its function names, 2026-11-08); the entry point runs it once, to start the server as `postgres`; PostgreSQL is only on the network `nfhotel-db`. No build of the tag has a newer `gosu`. The daily scan reports the findings again after the expiry |
| CVE-2026-78661 (high, the HTML parser of `golang.org/x/net`) in `ollama`, `cloudflared`, `alloy`, `prometheus`, `promtool`, `alertmanager`, `amtool`, `blackbox_exporter`, `loki` and 14 bundled data source plugins of Grafana | S01 | S01 | 2026-12-08 | Not in the binary: the parser has no function in these binaries (function names, 2026-11-08); no release of these images has x/net 0.62.0 yet |
| CVE-2026-78661 (high) in the Grafana 13.3.0 server, which has the parser | S01 | S01 | 2026-12-08 | Grafana is reached only on the tailnet address of the monitoring container, after a login, with sign-up off; a denial of service stops the dashboards, not the hotel; 13.3.0 is the newest release |
| The bundled data source plugins of Grafana 13.3.0: CVE-2026-78667 (high, not in the binary), CVE-2026-111943, CVE-2026-78669, CVE-2026-97031, and CVE-2026-21728 and CVE-2026-28377 in the Tempo plugin (high) | S01 | S01 | 2026-12-08 | Grafana provisions only the Prometheus and Loki data sources, over plain HTTP inside the monitoring network, and no provisioned data source uses TLS; it provisions no Tempo data source; it is reached only on the tailnet after a login; 13.3.0 is the newest release |
| CVE-2026-109518 (high) in `apt` 2.8.3 of Ubuntu 24.04, in `grafana/alloy` and `ollama/ollama` | S01 | S01 | 2026-12-08 | The finding needs apt to download packages; apt runs only when an image is built, never in these containers, whose root file systems are read-only. Ubuntu has 2.8.4ubuntu0.1; no build of these images has it yet |
| CVE-2026-76844 and CVE-2026-85091 (high) in Debian's `zlib1g`, in `cloudflare/cloudflared` and `python:3.13-slim` | S01 | S01 | 2026-12-08 | Not used: both are in the `gz*` functions of zlib; `cloudflared` is a static Go binary that never loads `libz`, and the Python of the scan exporter links `libz` only in its zlib and binascii modules, which call no `gz*` function |
| CVE-2025-47273, CVE-2026-114613, GHSA-6v7p-g79w-8964, CVE-2026-97687 and CVE-2026-97689 (high) in `setuptools`, `msgpack` and `urllib3`, vendored inside pip 26.2.2 in `python:3.13-slim` | S01 | S01 | 2026-12-08 | Not used: the scan exporter runs `python -B -E -s /app/run.py` with the standard library only and never runs pip; the image has no other copy of these packages |

## Findings

- **Every pinned third-party image and the patched socket proxy pass the gate with these entries.** The re-scan of 2026-11-08 covered the eleven third-party images of the compose files of the branch and the socket proxy built from its Dockerfile. The one error, Loki, was in reading the image from the local store of Docker Desktop, not in the image: read from the registry it passes. On the runner the daily scan pulls each image itself.
- **The risk exceptions need S01, so the verdict is Go-with-conditions.** The 41 entries accept 37 vulnerability IDs in third-party images for 30 days, one critical and 36 high. The stand-in checked every statement against the image (function names, linked libraries, what runs), but section 8 of [DSA-001] names S01 as the approver, and RC-304 recorded that a stand-in may not approve a risk exception. S01 is also the owner here; in this team S01 holds both roles, and DSA-001 names no other approver for a container scan exception.
- **The first deploy pulls a new private package.** As with `nfhotel-nginx` ([RC-300] finding 3), `nfhotel-socket-proxy` is created by the push of the release of the merge, linked to the repository by its source label (checked in the compose build above), and the VM must be able to pull it as it pulls the five others. With `--no-build`, a failed pull fails the deploy at once, and the deploy rolls back to the release before it.
- **The wiki test would have dropped the base image of the socket proxy without the change of its search.** Before MIL-065, `tecnativa/docker-socket-proxy` was named in the compose file of Alloy; it is now only in `ops/deploy/alloy/socket-proxy/Dockerfile`, two levels down, where the old glob `ops/deploy/*/Dockerfile` did not look. The image index check of Technology-Choices would have stopped checking it without a failure. The test now uses `find` at any depth, and still finds 17 images to check.
- **SonarQube Cloud: docker:S6471 is excluded for the new Dockerfile before the pull request.** The socket proxy runs as root, as the upstream image always did, because root owns the Docker socket; the compose file drops every capability and keeps the root file system and the socket read-only (MIL-023). The exclusion follows the one for the proxy ([RC-300] finding 5), with the reason in `SonarQube.Analysis.xml`.
- **The monitoring stack runs the pins of its own clone.** Prometheus, Alertmanager, the Blackbox exporter and the scan exporter move only when `deploy-monitoring.sh --apply` runs on the monitoring container (criterion 6), which S01 starts through the Tailscale SSH check.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion of QC-SH-001, QC-WF-001 and QC-YML-001 passes; criterion 22 of QC-WF-001 and criteria 2, 8 and 9 of MIL-065 close with the checks of the pull request and the release of the merge. The condition is the approval of the seven risk exceptions above by S01 before the merge; with it this record is Go. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Approve the risk exceptions of `ops/security/trivyignore.yaml` (DSA-001 section 8), or name the entries to change | S01 | Before the merge |
| Merge the pull request only when every required check is green and the exceptions are approved | AI as stand-in for S01 | At the merge |
| Before the approval of the CD run, check that the package `nfhotel-socket-proxy` exists with the access of the five others, and that an anonymous request for each of the six manifests is refused (criterion 9) | AI as stand-in for S01 | After the release run |
| Approve the CD run of the merge (ADR-0038); its verify step, deploy and smoke test are the proof of criterion 3 | S01 | After the release run |
| Start `deploy-monitoring.sh --apply` on the monitoring container through the Tailscale SSH check (criterion 6) | S01 | After the CD run |
| Run the daily scan by hand after both deploys; it is the proof of criterion 1 and closes #553 | AI as stand-in for S01 | After the deploys |
| Scan the newest upstream builds again before the entries run out, and remove or renew each entry in a reviewed pull request | S01 | 2026-12-08 |

---

[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[DSA-001]: ../devsecops-assessment.md
[RC-300]: ./rc-300-mil-064-code.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[339f707]: https://github.com/NF-Hotel/NFHotel/commit/339f707308fedd9629139338c5859192d049dd52
