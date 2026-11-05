# MIL-062 Pruning the images of old releases on the VM

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-062 |
| CrossReference | [BC-001], [ADR-0018], [ADR-0021], [ADR-0038], [MIL-021], [MIL-023] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the alert `DiskAlmostFull` of 2026-11-05 (80.4 % of the disk of `nf-hotel`), which S01 asked in chat to fix ("Fix.") and then to plan the cleanup for ("Planlæg oprydning"); S01 accepted the plan in chat on 2026-11-05 ("Accepter planen") after RC-290 Go | pending |

---

## Purpose

Decide how the VM keeps its disk free of the images of old releases, so that the alert `DiskAlmostFull` of [MIL-023] does not return. On 2026-11-05 the alert fired at 80.4 %: of the 111 GB disk of `nf-hotel`, 84 GB were used, 61 GB of them by the image store of Docker (`/var/lib/containerd`), which held 348 images that no container used: 99 of the web application, 99 of the API, 90 of the benchmark and 51 of the market analysis, one set per release since October, pulled by digest by every deploy and never removed. The runbook of the alert says `docker image prune`, but that command removes only images without a tag that nothing references, and an image pulled by digest is not one of them: the command found nothing to remove.

The deploy keeps a release log (`/var/lib/nfhotel-deploy/releases.log`, [MIL-021]) and rolls back to the release before the latest one when a smoke test fails or on request. The rule is therefore: after a deploy whose smoke test passed, the images of the two latest releases in the log and every image a container uses stay, and every other image goes. The deploy itself does it, through the deploy command that [ADR-0018] installs from the release commit, so nobody has to remember it and the first run of the new command is the cleanup of the 348 images. The one-line command S01 was given on 2026-11-05 (`docker image prune -a --filter until=24h`) does the same once, by hand; this milestone makes it part of the deploy.

## Deliverable

- **A `prune` command of `ops/scripts/deploy-release.sh`** (`nfhotel-deploy prune`): it reads the digests of the two latest release lines of the log and the image of every container (running or stopped), removes every other image, and prints what it removed and the free space of `/` before and after. Without `--apply` it prints what it would remove and removes nothing, as `QC-SH-001` asks of a command that changes state.
- **The deploy calls it** after a smoke test that passed and after the release is recorded, never after a rollback or a drill; a failure inside the prune is a warning in the log, not a failed deploy.
- **Tests** in `ops/tests/test-deploy-release.sh`, with its fake `docker`: the set that goes and the set that stays, the dry run, the rollback and the drill that remove nothing, and the warning that does not fail a deploy.
- **Documentation**: the runbook of `DiskAlmostFull` on the wiki page Alerts names the command; the CI-CD page describes the step; the Proxmox Deployment page says what the disk keeps.
- **The first run on the VM**: the CD run of the merge installs the new deploy command (`update`, [ADR-0018]) and deploys with it, so the prune runs at the end of that deploy, approved by S01 ([ADR-0038]); then the gate reads the VM and the alert.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch, task 4 with the pull request and the deploy, then task 5.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | After a deploy whose smoke test passed, the only images left are those a container uses and those of the two latest releases of the log; proven by `ops/tests/test-deploy-release.sh` with a fake `docker` in Ops CI, and on `nf-hotel` after the first deploy with it (`docker image ls` against the release log and `docker ps -a`) | Yes | No |
| 2 | A failed deploy (with its rollback) and a rollback drill remove no image, and `nfhotel-deploy prune` without `--apply` removes nothing and prints what it would remove; proven by the test | Yes | No |
| 3 | A failure inside the prune (an image that cannot be removed) leaves the deploy that passed its smoke test green and is a warning in its log; proven by the test | Yes | No |
| 4 | On `nf-hotel`, after the first deploy with the prune, `/` is below 80 % and `DiskAlmostFull` is resolved: Alertmanager lists no firing instance of it and the expression of the rule is false in Prometheus | Yes | No |
| 5 | The runbook of `DiskAlmostFull` on the Alerts page names `nfhotel-deploy prune`, the CI-CD page describes the step, the Proxmox Deployment page says what the disk keeps, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, and the job `Ops script tests (test-deploy-release.sh)` is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-021 | `deploy-release.sh`, the release log and the rollback to the previous release, which the prune must keep possible |
| MIL-023 | The alert `DiskAlmostFull` and the monitoring that shows it resolved |
| ADR-0018 | The deploy command installs itself from the release commit, so the prune takes effect with the CD run of the merge |
| ADR-0021 | The deploy runs after the merge, with its smoke test and rollback; the prune runs only after a passed smoke test |
| ADR-0038 | The deploy, and so the first prune, waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 success criterion 26: a failed smoke test rolls back to the previous digests; the prune keeps those digests on the VM | Criteria 1, 2 |
| BC-001 success criterion 28: a hardened server with monitoring; the alert of MIL-023 found the full disk, and the deploy now keeps it free | Criteria 1, 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-10. It changes one script, its test and three wiki pages, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The `prune` command of `deploy-release.sh` | A function `prune_images` in `ops/scripts/deploy-release.sh`: the image ids of every container (`docker ps -a`), the image ids of the digests in the two latest release lines of the log (`previous_release 1` and `2`, fields 3 to 6, resolved with `docker image inspect`), and every other image id of `docker image ls` removed with `docker image rm -f`, with the free space of `/` before and after in the log. The command `prune` of the dispatcher calls it: without `--apply` it prints what would go and removes nothing; with `--apply` it removes. `deploy` calls it with `--apply` after `record`, never on a rollback or a drill, and a failure inside it is a warning, not a failed deploy. The header and the usage text name the command. | No | criteria 1, 2, 3 |
| 2 | Tests of the prune | `ops/tests/test-deploy-release.sh`: the fake `docker` answers `ps -a`, `image ls`, `image inspect` and `image rm` from files, and records every removal. Checks: after a passed deploy only the images of the two latest releases and of the containers are left, and an unused third-party image is gone; a failed deploy with its rollback and a drill remove nothing; `prune` without `--apply` lists and removes nothing; a removal that fails leaves the deploy green with a warning. The job `Ops script tests (test-deploy-release.sh)` of Ops CI runs it. | No | criteria 1, 2, 3, 6 |
| 3 | Documentation | Wiki page Alerts: the runbook of `DiskAlmostFull` becomes `df -h /`, `sudo nfhotel-deploy prune` to see what goes, `sudo nfhotel-deploy prune --apply` to remove it, and the "Why" column says that every deploy prunes since MIL-062. CI-CD: the deploy steps and the paragraph "During the deploy" name the prune after the smoke test. Proxmox Deployment: a short paragraph on what the disk keeps (the images in use and of the two latest releases, the volumes, the backups of `backup-database.sh` with their own retention) and the 111 GB disk. `bash ops/tests/test-wiki-coverage.sh` passes. | No | criterion 5 |
| 4 | Code review, pull request and deploy | The review record of `deploy-release.sh` and its test against `QC-SH-001` before the pull request; the pull request closes one issue per task and carries the milestone; merged in autonomous mode once every required check is green. The CD run of the merge installs the new command (`update`) and deploys with it, approved by S01 (ADR-0038); the prune at the end of that deploy is the first run on the VM, and its log lines are the evidence. | No | criteria 1, 4, 6 |
| 5 | Gate: the read on nf-hotel and the review record | After the deploy, a read-only check on `nf-hotel` over Tailscale SSH in check mode, under an approval of S01 in chat: `df -h /`, `docker image ls` against `docker ps -a` and the two latest lines of the release log, the prune lines of the deploy; from the PC over the tailnet, the Alertmanager API without a firing `DiskAlmostFull` and the Prometheus expression of the rule. The gate review record against criteria 1 to 6, the row of MIL-062 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0018]: ../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-023]: ./mil-023-operational-quality.md
