# RC-300 Code review of MIL-064: the patched proxy image, its deploy by name and the daily scan of the third-party images

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-300 |
| CrossReference | [MIL-064], [ADR-0040], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 2 to 7 of MIL-064 before its pull request (task 8), with the one fix it asked for: the steps of the daily scan in a script | [55cb8a6] |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-064-patched-nginx-and-third-party-scan` against `main` at 9508e7a; the code of 0ae8e27, d58557d, 6257733 and fa7cecb, and the fix of this review):
  - shell: `ops/scripts/build-release-images.sh` (the fifth image), `ops/scripts/cd-steps.sh` (`image_pattern`, `release_refs`, the output `images`, `verify` and `deploy` by name), `ops/scripts/deploy-release.sh` (`check_release` by name with `--from-log`, `image_var`, `apply`, `previous_release`, `prune_images`, `--drill` as the last argument), `ops/scripts/deploy-services.sh` (the proxy built outside a release), `ops/scripts/lib/stack.sh` and `ops/scripts/test-compose-proxy.sh` (the proxy built in the test stacks), `ops/scripts/install-deploy-user.sh` (the usage line), `ops/scripts/scan-running-release.sh` (`--third-party`), the new `ops/scripts/running-image-scan-steps.sh`, and the tests `ops/tests/check-release-build.sh`, `test-cd-steps.sh`, `test-deploy-release.sh`, `test-nginx.sh` and `test-scan-running-release.sh` (tasks 2, 3, 4 and 6);
  - workflows and actions: `.github/workflows/release.yaml` and `release-check.yaml` (headers), `running-image-scan.yaml` (the checkout of `ops/deploy` at the operations commit, the steps as one line each, the timeout), `.github/actions/release-images/action.yml` (the scan upload, the provenance and the SBOM of `nfhotel-nginx`) and `.github/actions/deploy-release/action.yml` (`RELEASE_IMAGES`) (tasks 2, 3 and 6);
  - YAML: `.github/dependabot.yml` (task 5) and `ops/deploy/nginx/docker-compose.yml` (task 2); `ops/deploy/nginx/Dockerfile` and `.dockerignore` have no checklist of their own and were read against decision 1 of [ADR-0040];
  - the wiki pages of task 7: Containers-and-Images, Proxmox-Deployment, Workflows, CI-CD, DevOps-Lifecycle, Security-Controls and Technology-Choices.
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001].
- **Scope:** delta review of the changed files against `main`.
- **Language and domain:** n/a (technical type).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the code and this record. S01 accepted the plan of MIL-064 and ADR-0040 in chat on 2026-11-08 ("Godkend planen", "Godkend ADR'en"), and on the same day handed the rest of the milestone to the AI in chat ("Du beslutter somom du er mig i auto mode on. Merge PR og kør CICD"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-08, on the PC of S01 (Docker Desktop, Docker 29.7.2):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean, after the fixes of SC2181 in two tests and of three lines over 100 columns, and again after the fix of this review.
- `ops/tests/test-workflow-policy.sh`: the 14 workflows and actions follow the policy of ADR-0037; its self-test 8 of 8.
- In `debian:12-slim`, as the jobs of Ops CI run them: `test-cd-steps.sh` 90, `test-deploy-release.sh` 87, `test-scan-running-release.sh` 41 (32 before the fix of this review; the script of `main` fails 14 of those, a mutant without the folded value 6), `test-deploy-services-env.sh` 16, `test-switch-vm-checkout.sh` 14, `test-renew-certificate.sh` 19, `test-backup-settings.sh` 6, `test-dev-lite-portable.sh` 24; `test-wiki-coverage.sh` 91.
- `ops/tests/test-nginx.sh` from WSL against the same Docker daemon: 24 of 24, with the image built from `ops/deploy/nginx` (from Git Bash the path conversion of MSYS breaks its mounts; Ops CI runs it on Linux). `ops/tests/test-service-networks.sh` in `docker:29-cli` with bash and jq: 49 of 49.
- The compose build of the release check: the image carries `org.opencontainers.image.source` and `org.opencontainers.image.revision`, and `nginx -v` says 1.29.8, the version of the pinned base.
- Trivy 0.75.0 with the gate of `scan-gate.sh`, through `scan-running-release.sh --third-party` on three local images: the upstream nginx pin fails, `haproxy:3.2-alpine` passes, `tecnativa/docker-socket-proxy:v0.5.0` fails with 9 findings; the summary was 4666 bytes.
- Compose with an image pinned by digest that cannot be pulled and a `build:` section: it builds, and the build stops with "build tag cannot contain a digest", so no image is made (finding 3).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1 | Header line and `set` options | Pass | Every changed script keeps `#!/usr/bin/env bash` and `set -euo pipefail`; the tests keep their `set -uo pipefail`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | The references and arguments are arrays (`REFS`, `images`, `args`, `fields`, `refs`); the two unquoted `$last` and `$target` are a release line split on purpose, each with its `disable` comment, and `rollback` checks its fields first (`check_release --from-log`) |
| SH 3 | Names | Pass | `snake_case` functions (`image_pattern`, `release_refs`, `image_var`, `in_list`, `pinned_images`, `find_release`); `UPPER_SNAKE` constants (`IMAGE_NAMES`, `NAMES`, `ADDED_NAMES`, `NOT_IN_PRODUCTION`, `PINNED`, `MAX_SUMMARY`, `MANIFEST`); `running-image-scan-steps.sh` is kebab-case and named after its workflow, as `cd-steps.sh` is |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean; no `disable` comment added, two reworded |
| SH 5 | Errors | Pass | `fail`, `refuse` and `die` print `::error::` or `error:` and exit non-zero; a refusal names the argument and the image ("argument 5 names nfhotel-web a second time"); a usage error exits 2; all tested |
| SH 6 | Temporary files | Pass | No script adds one; the tests use `mktemp -d` with `trap ... EXIT` |
| SH 7 | Secrets | Pass | None added; the job token still reaches `docker login` on standard input; references and commits are not secrets |
| SH 8 | State outside its folder | Pass | `deploy-release.sh` changes the VM only through its existing commands, which CD runs; `prune` stays a dry run without `--apply`; `scan-running-release.sh` pulls and removes images in the Docker of the runner, as its header says |
| SH 9 | Header | Pass | The headers name the five images, the references by name in any order, the release lines with four or five references, `--third-party` and its folder, the steps of the workflow, the variables and the exit codes |
| SH 10 | Implements the task it cites | Pass | MIL-064 tasks 2 to 6 as planned. Deviations: the wiki update of task 7 covers seven pages, not three, because four more named the four images or the upstream proxy; the steps of the daily scan moved into a script (finding 1) |
| SH 11 | Tested for success, failure and the bypass path | Pass | `test-cd-steps.sh`: a manifest without, or with a malformed, `nfhotel-nginx` digest is refused; the references are passed by name. `test-deploy-release.sh`: a deploy by name in any order runs and is logged; one without nginx, with a name twice or an unknown name is refused; a rollback to a line with four references starts the proxy from the compose file of that release; `prune` keeps the nginx image of both releases; `--drill` as the last argument. `test-scan-running-release.sh`: a vulnerable compose image fails, a clean one passes; a variable, an alias, a comment and proxmox are left out; a folded value is read; a folder without pins is refused before a pull; a long summary is cut; `find` and `scan` of the steps |
| SH 13 | Safe to re-run | Pass | A deploy of the same release recreates nothing; a second scan pulls again and changes nothing else |
| SH 14 | Needs stated | Pass | bash 4+ (associative arrays in `check_release` and the scan), docker, jq, awk, sed, grep and gh, in the headers |
| SH 15 | Knowledge once | Pass | One pattern of a reference (`image_pattern`); the variable of an image is derived from its name (`image_var`), not listed; the list of release images on each side of the tailnet is discussed in finding 2 |
| WF 1, YML 1 to 3 | YAML checks | Pass | yamllint 1.35.1 clean; LF, two spaces, no duplicate key |
| WF 4 | Header comment | Pass | `release.yaml` names the five packages it changes; `running-image-scan.yaml` the third-party images, the checkout at the operations commit, and the script of its steps; `release-check.yaml` the five images |
| WF 5 | Step names | Pass | The new steps "Check out the compose files of the release", "Upload the scan of nfhotel-nginx", "Attest the provenance of nfhotel-nginx" and "Attest the SBOM of nfhotel-nginx" are unique in their jobs |
| WF 6 | actionlint | Pass | actionlint 1.7.7 clean |
| WF 7 | Least permissions | Pass | No permission added; the second checkout uses the `contents: read` of the job |
| WF 8 | Pinned actions | Pass | `actions/checkout@3d3c42e5…` (v7.0.1), `github/codeql-action/upload-sarif@9b325408…` (v4.39.1), `actions/attest-build-provenance@4d101475…` (v4.2.2) and `actions/attest@1e69f48a…` (v4.2.2): the SHAs the repository already uses; the policy test passes |
| WF 9 | Secrets | Pass | None added |
| WF 10 | Untrusted text | Pass | The operations commit comes from the `release.json` of a release run of `main`, is checked against `^[0-9a-f]{40}$` before it becomes an output, and reaches the scan through `env:`; `ref:` of the checkout is that value. No `${{ }}` in a `run:` (policy rule 6) |
| WF 11 | Triggers with secrets on untrusted code | Pass | The scan runs on a schedule and by hand, and checks out the operations commit of a release of `main`, which passed the release gate |
| WF 12 | Changes outside the repository | Pass | The release publishes the fifth package from `main` only; the scan opens or updates its issue as before |
| WF 13 | Token scope | Pass | The job token, as before |
| WF 14 | Runner and timeout | Pass | `ubuntu-24.04`; the timeout of the scan from 30 to 60 minutes, with the reason in a comment (seventeen images, ollama 4.8 GB) |
| WF 15, 16, 17 | Concurrency, triggers, pinned tools | Pass | Unchanged; Trivy keeps the pinned version of `scan-image.sh` |
| WF 19 | A failing step names the reason | Pass | `::error::the release.json of <commit> has no operations commit`, `::error::no successful CD run found`, `::error::no release run for <commit>` |
| WF 20, YML 7 | At most five lines of embedded program text | Pass (after a fix) | The steps "Find the release in production" and "Scan every image" held 19 and 8 lines after task 6; they are now one line each (finding 1) |
| WF 21 | Shared logic | Pass | The image steps stay in the composite action `release-images`; the steps of the scan are in a script, like the steps of CD |
| YML 4, 5, 6 | Coerced scalars, tags, secrets | Pass | `open-pull-requests-limit: 15` is a number on purpose; no tag; no secret |
| YML 8, 9 | Repeated blocks, anchors | Pass | The two attest steps of `nfhotel-nginx` repeat those of the four images, as a composite action cannot loop; no anchor is used |
| YML 11 | Header comment | Pass | `dependabot.yml` says why the proxy moved from the compose entry to the docker entry, and why the limit is 15; the compose file of the proxy says where the image comes from |
| YML 12, 14 | Key names, file names | Pass | The keys of each tool; the files keep their names |
| MIL-064 5 | `deploy-release.sh` refuses, records, prunes and rolls back | Pass (in test) | `test-deploy-release.sh`, 87 checks |
| MIL-064 6 | Dependabot | Pass | A `docker` entry for `/ops/deploy/nginx` with `interval: weekly`, `cooldown.default-days: 7` and the prefix `Images:` of the other image entries; `open-pull-requests-limit: 15` for the 13 images of the compose entry |
| MIL-064 7 | The scan test | Pass (in test) | A compose file with a known vulnerable image fails the scan; the run after the deploy is the proof on the host |
| MIL-064 8 | The wiki | Pass | The seven pages; `test-wiki-coverage.sh` 91 |
| MIL-064 9 | Review Go, lint clean, the checks green | Pass (locally) | This record and `lint.sh`; the checks of the pull request are the proof on the host |

## Findings

- **Fixed in this review: two steps of the daily scan held more than five lines of shell.** After task 6, "Find the release in production" had 19 lines (14 before MIL-064) and "Scan every image" 8 (7 before), over the limit of QC-YML-001 criterion 7 and QC-WF-001 criterion 20. Both are now one line, `bash ops/scripts/running-image-scan-steps.sh find` and `scan`, tested by nine new checks in `test-scan-running-release.sh`. That test already has a job in Ops CI, so the required checks of the ruleset do not change. The step "Open, update or close the issue" (16 lines) is not touched by MIL-064 and keeps its length; it is the natural next subcommand, `report`.
- **The list of release images is on both sides of the tailnet.** It is `IMAGE_NAMES` in `cd-steps.sh` on the runner, `NAMES` in `deploy-release.sh` on the VM (installed alone, without the library beside it), `IMAGES` in `build-release-images.sh` and `NAMES` in `check-release-build.sh`: one fact in four files, as it was before MIL-064. The two deploy lists are an allowlist on each side of a trust boundary, where the VM does not take the runner's word. A mismatch fails before anything is deployed: a name that the build lacks fails `read-manifest` ("bad digest"); a name that only the runner has is refused by the VM ("not one of"); a name that only the VM has is refused there too ("no reference of").
- **A pull of `nfhotel-nginx` that fails on the VM leads Compose to try a build, which cannot succeed.** The compose file of the proxy keeps `build:` beside `image:`, so the development and test stacks build it, as they do the application images; Compose builds an image whose pull fails. In a release the image is a reference by digest, which cannot be the tag of a build ("build tag cannot contain a digest", tried on 2026-11-08), so no image is made, the deploy fails and rolls back to the previous release. The VM never runs an image that the release did not scan. The precondition of the deploy is that the VM can pull the new package as it pulls the four others. Those are private (an anonymous token request to GHCR answers 401 on 2026-11-08, where a public package answers 200), and CD run 42106387565 pulled them on the VM, so the VM holds a pull credential of its own: the second option of decision D2 of DSA-001, which still records the first ("Public").
- **The scan of the third-party images costs time and pulls from Docker Hub.** Twelve images, ollama 4.8 GB, pulled without a login: the limit on anonymous pulls per address can turn an image into "could not be pulled", which fails the run as a finding would, and the summary says which one.
- **The monitoring stack runs the pins of its own clone.** The scan reads the pins at the operations commit of the release; the monitoring LXC runs them once its clone is at that commit (`deploy-monitoring.sh`, by hand).

## Overall Verdict

Go. Every Mandatory criterion of QC-SH-001, QC-WF-001 and QC-YML-001 passes after the one fix, and criteria 5, 6, 8 and 9 of MIL-064 are met in the tests and the pages; criteria 2 to 4 and 7 close with the CD run of the merge and the daily scan after it. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green | AI as stand-in for S01 | At the merge |
| Before approving the CD run of the merge, check that the new package `nfhotel-nginx` exists and has the access of the four other packages, so the VM can pull it | AI as stand-in for S01 | After the release run, before the approval |
| Approve the CD run of the merge (ADR-0038) through the API, as S01 asked in chat; its deploy and smoke test are the proof of criteria 2 to 4 | AI as stand-in for S01 | After the release run |
| Run the daily scan by hand after the deploy; it is the proof of criterion 7 and the input of task 9 | AI as stand-in for S01 | After the deploy |

---

[MIL-064]: ../../milestones/mil-064-patched-nginx-and-third-party-scan.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[55cb8a6]: https://github.com/NF-Hotel/NFHotel/commit/55cb8a6183ebf3697f801c51e49f7eace9e7bd98
