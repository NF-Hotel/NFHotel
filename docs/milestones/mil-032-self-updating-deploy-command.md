# MIL-032 The deploy command updates itself in CD

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-032 |
| CrossReference | [BC-001], [ADR-0018], [ADR-0007], [MIL-021] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Deprecated | mikemike | S01 | Initial version | pending |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-158 Go (accepted by S01 in chat) | pending |

---

## Purpose

Decide whether a merged change to the operations commands on the VM reaches production through CD alone (ADR-0018). S03 should not have to install anything by hand.

## Deliverable

- **`nfhotel-deploy update <sha>`.** It refuses a commit that is not on `origin/main`. It installs the deploy command, the backup command and the backup timer from that commit, keeps the old copies as `.previous`, and logs the hashes.
- **CD calls it.** The CD step runs `update` before `migrate` and `deploy`, so the commands of the release do the migration and the deploy.
- **Tests** of the update, the refusal of a commit that is not on `main`, the case with no change, and the order in CD.
- **Documents.** ADR-0018. The script headers, the install scripts and the wiki page CI-CD say that the install is automatic after the first one.
- **The VM brought over once.** S03 runs `install-deploy-user.sh` one last time. The next CD then shows the update working.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `test-deploy-release.sh` shows four things. `update` installs the three commands from a commit on `main`. It refuses a commit that is only on a pull-request ref. It changes nothing when the files are equal. It keeps `.previous` | Yes | No |
| 2 | `test-cd-steps.sh` shows that CD runs `update`, then `migrate`, then `deploy`, and stops when `update` fails | Yes | No |
| 3 | The sudo rule on the VM is unchanged: `deploy` may run only `/usr/local/sbin/nfhotel-deploy` | Yes | No |
| 4 | A CD from `main` after the last manual install logs an update. `/usr/local/sbin/nfhotel-deploy` and `/usr/local/sbin/nfhotel-backup` then equal the files of the release commit (`cmp`) | Yes | No |
| 5 | No document or script header tells S03 to install the commands again after a change | Yes | No |
| 6 | Reviews against `qc-adr`, `qc-programming-shell` and `qc-workflow` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0018 | The decision this milestone carries out |
| NFHotel#220 | The backup path and rollback fixes that the first update installs |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 26 (approved pipeline): what reaches production is only what passed review, CI and the production approval | Tasks 2 and 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-10-28, one week. Task 5 needs one approved CD after the merge.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Review ADR-0018 | Review the decision against `qc-adr`. On Go, ADR-0007 gets a note that ADR-0018 amends its manual install. | No | ADR-0018 |
| 2 | The update subcommand | Add `update <sha>` to `ops/scripts/deploy-release.sh`. It fetches `main`, checks that the commit is reachable from `origin/main`, and installs the deploy command, the backup command and the timer from `git show`. It keeps `.previous`, reloads systemd when the units change, and logs the hashes. Tests in `test-deploy-release.sh`. | No | ADR-0018 decision 1 to 4 |
| 3 | CD runs the update first | `ops/scripts/cd-steps.sh` and the deploy-release action call `update` before `migrate` and `deploy`. Tests in `test-cd-steps.sh`. | No | ADR-0018 decision 1 |
| 4 | Documents | Update the headers of `deploy-release.sh`, `install-deploy-user.sh` and `install-backup.sh`, the wiki page CI-CD and `ops/README.md`. They say the first install is manual and every later one comes with CD. | No | ADR-0018 decision 5 |
| 5 | Bring the VM over once | After the merge, S03 runs `install-deploy-user.sh --apply` one last time. The next approved CD proves criterion 4. | No | ADR-0018 decision 5 |
| 6 | Check and review | Prove criteria 1 to 5; reviews against the three checklists. | No | |

---

[BC-001]: ../business-case.md
[ADR-0018]: ../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
