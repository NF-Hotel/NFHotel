# RC-291 Code review of MIL-062: the prune command of deploy-release.sh and its tests

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-291 |
| CrossReference | [MIL-062], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 3 of MIL-062 before its pull request | [c258f9b] |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-062-prune-old-release-images` against `main` at 0bdcc55):
  - `ops/scripts/deploy-release.sh`: the function `prune_images`, the command `prune`, the call after a passed smoke test in `deploy`, and the header (task 1);
  - `ops/tests/test-deploy-release.sh`: the fake `docker` extended for `ps -aq`, `inspect`, `image ls`, `image inspect` and `image rm`, and the nine checks of the prune (task 2);
  - the wiki pages Alerts (the runbook of `DiskAlmostFull`), CI-CD (the rollback keeps its images; the prune after the smoke test) and Proxmox Deployment (what fills the disk; the networks of the benchmark since ADR-0039) (task 3).
- **Checklist used:** [QC-SH-001].
- **Scope:** delta review of the changed files against `main`.
- **Mode:** autonomous, not independent. The AI wrote the code and this record; S01 accepted the plan of MIL-062 in chat on 2026-11-05 ("Accepter planen"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-05, on the PC of S01 (Docker Desktop, Docker 29.7.2):** `ops/tests/test-deploy-release.sh` in `debian:12-slim`, the way the Ops CI job `Ops script tests (test-deploy-release.sh)` runs it: 76 checks, 0 failed, the nine of the prune among them; `ops/tests/lint.sh` in containers (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1); `ops/tests/test-wiki-coverage.sh`, 91 of 91.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1, 2, 3 | Header line, `set` options, quoting, names | Pass | The script keeps `set -euo pipefail`; every expansion of the new function is quoted, and the two `for` loops over image ids split a list on purpose with a `shellcheck disable` that says so; the four references of a release line are read into four named variables with `read -r`. `snake_case` function and variables |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean on the final files. Two findings of the first run were fixed before this record: a `for` over the words of `cut` (SC2013; now `read -r` into four variables) and an `eval` in the test (SC2154; now an indirect expansion `${!v}`) |
| SH 5 | Errors, usage | Pass | `prune` with any argument but `--apply` is refused with exit 2 (tested); an image that cannot be removed is a `warning:` line on standard error and exit 1 of `prune --apply` (tested); inside `deploy` that exit becomes the warning "the prune did not finish; the release is deployed" and the deploy stays 0 (tested) |
| SH 6 | Temporary files | Pass | None made |
| SH 7 | No secret | Pass | The function reads ids and digests only; nothing of the settings or the secrets folder |
| SH 8 | State outside the script defaults to a dry run | Pass | `prune` without `--apply` prints `would remove <id>` per image and a count, and removes nothing (tested); the header says so. The call from `deploy` passes `--apply`, after `deploy` itself has already changed the VM and recorded the release |
| SH 9 | Header | Pass | The command, its rule and its reason (the latest release runs, the one before it is what `rollback` goes to), the usage line, and the exit codes are in the header; `prune --apply` is named among the commands that change the VM |
| SH 10 | Implements the task it cites | Pass | MIL-062 tasks 1 and 2: the rule of the Purpose, the call after `record`, never after a rollback or a drill, the warning that does not fail the deploy |
| SH 11 | Tested for success, failure and the bypass path | Pass | Nine checks: after a third deploy exactly the five images of the first release and the unused nginx go, and the images of releases 2 and 3 and of the six containers (a stopped one among them) stay; the deploy log counts them; the dry run lists and removes nothing; an unknown argument is refused; `prune --apply` removes the same five by hand; a failed deploy with its rollback and a drill remove nothing; an image that cannot be removed leaves the deploy green with the warning and fails `prune --apply` |
| SH 13 | Safe to re-run | Pass | A second `prune` finds the same keep set and nothing more to remove; the fake records show one removal per image per run |
| SH 14 | Needs stated | Pass | The header names docker; `xargs` is part of findutils, which Debian installs as a required package, so it is not listed; the test runs in `debian:12-slim` as the job does |
| SH 15 | Knowledge once | Pass | The release lines are read with the existing `previous_release`; the rule "the two latest releases" is in one function; the test reuses `expect_code`, `run`, `ok` and `ko` and adds one helper, `rel` |
| MIL-062 1 | The images of the containers and of the two latest releases stay, every other goes | Pass (in test) | The check "the images of release 1 and the unused nginx go; releases 2 and 3 and the containers' images stay"; on the VM at the gate |
| MIL-062 2 | Rollback, drill and dry run remove nothing | Pass | Three checks |
| MIL-062 3 | A failed removal is a warning, not a failed deploy | Pass | Two checks |
| MIL-062 5 | The three wiki pages | Pass | The runbook names `nfhotel-deploy prune` and `--apply` and says why `docker image prune` finds nothing; CI-CD says the rollback pulls nothing and names the prune after the smoke test; Proxmox Deployment says what fills the disk and corrects the networks of the benchmark (ADR-0039); the coverage test passes |
| MIL-062 6 | Review Go, lint clean, the job green | Pass (locally) | This record; `lint.sh` clean; the job of the pull request is the proof on the host |

## Findings

- **The rule is by release, not by age.** The one-line command S01 was given keeps images younger than 24 hours; the function keeps the images of the two latest releases whatever their age, so a quiet week never turns the rollback into a new pull. Third-party images (nginx, PostgreSQL, Ollama, cloudflared, Alloy, HAProxy, the socket proxy) stay while a container uses them; an older version of one of them, which a rollback to the previous release might pin, is pulled again when needed.
- **The one-shot service counts.** `docker ps -aq` lists stopped containers, so the image of `results-owner` (the one-shot service of the benchmark) is kept with the benchmark's, which the test covers with a stopped sixth container.
- **The first run is the cleanup of 2026-11-05.** The CD run of the merge installs the new command before it deploys (ADR-0018) and runs the prune at the end of that deploy: the 348 images, or what is left of them if S01 ran the one-line command, go then. The deploy log shows the count and the free space before and after.
- **A warning is still a signal.** A failed removal leaves the deploy green; it is in the log of the CD run, and the alert `DiskAlmostFull` stays the guard if the disk still fills. Nothing more is built for it until it happens.

## Overall Verdict

Go. Every Mandatory criterion of QC-SH-001 passes on the files as they are, and the criteria 1 to 3 and 5 of MIL-062 are met in the test and the pages; criterion 1 on the VM, criterion 4 and the green job close at the gate. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green, and read the job `Ops script tests (test-deploy-release.sh)` on it | S01 (AI as stand-in) | At the merge |
| Under "Review deployments", approve the CD run of the merge; its deploy runs the first prune (ADR-0038) | S01 | At the merge |
| Give the approval in chat for the check-mode read of `nf-hotel` after the deploy (MIL-062 task 5) | S01 | After the deploy |

---

[MIL-062]: ../../milestones/mil-062-prune-old-release-images.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[c258f9b]: https://github.com/NF-Hotel/NFHotel/commit/c258f9bff2ba46862f45fc7b78be6080e7c959ff
