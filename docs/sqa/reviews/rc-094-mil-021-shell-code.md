# RC-094 Review of the MIL-021 shell scripts

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-094 |
| CrossReference | [MIL-021], [ADR-0007], [ADR-0008], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [064c27c] |

---

## Artifact Under Review

- Instance reviewed: `scripts/build-release-images.sh`, `deploy-release.sh`, `install-deploy-user.sh`, `backup-database.sh`, `install-backup.sh`, `restore-drill.sh`, `cd-steps.sh`, `lib/common.sh`, and `deploy/systemd/nfhotel-backup.{service,timer}`, at commit 6cd89c1
- Checklist used: [QC-SH-001]
- Scope: the changes of MIL-021
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`); an independent review is requested from Tirsvad on PR #117
- Tools: `shellcheck` 0.11.0, `shfmt` 3.14.1, `bash -n`, Debian 12 and PostgreSQL 16 containers

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | All nine scripts; `lib/common.sh` is sourced and has the shebang for the editor only |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | Arrays for images, names and old backups; the three intentional word splits in `deploy-release.sh` (`apply $last`) split five checked fields and carry an explained `disable=SC2086` |
| 3 | Names follow the conventions: `kebab-case.sh` files, `snake_case` functions and variables, `UPPER_SNAKE` constants and environment variables | Pass | Constants made `readonly UPPER_SNAKE` in commit 6cd89c1 (`IMAGES`, `NAMES`, `BACKUP_COMMAND`, `DAILY_KEEP`, `SUDOERS`, ...) |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | `shellcheck` 0.11.0 `-x` on all nine: 0 findings; `bash -n`: 0 errors |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `die` (exit 1), `refuse` (exit 2) and `fail` (`::error::` in CI); usage on exit 2, tested |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | Pass | `build-release-images.sh`, `restore-drill.sh` (the container is removed in the same trap); `install-deploy-user.sh` removes its `mktemp` file on both paths |
| 7 | No secret is written in the script, echoed, or put on a command line; secrets come from the environment or a gitignored file | Pass | The build uses throwaway values for two required compose variables (`release-build`), not secrets; the backup reads `/etc/nfhotel/backup.env` (0600); the GHCR token goes through `--password-stdin`; the age private key is passed as a file path |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | `--push`, `--apply` (installers, backup), the `deploy`/`rollback` subcommands; `restore-drill.sh` touches only a throwaway container and a temporary folder, stated in its header |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | Every script; `--help` prints it |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | Each header cites ADR-0007 or ADR-0008 and its MIL-021 task; the D7 rsync design is recorded in DSA-001 and the wiki |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | Debian 12 containers: argument refusal (injection, order, registry, a dot as wildcard), deploy, failed smoke test with automatic rollback, manual rollback, lock; sudo limits of `deploy`; dry-run defaults; CD steps with fake `gh` and `tailscale`; backup, retention, restore, a wrong key, a bad recipient against PostgreSQL 16; rrsync write-only copy (host key, read back, shell, escape refused). Not tested: the real VM (task 8) |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | `shfmt` 3.14.1 `-i 2 -ci -d`: no diff |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | Installers tested twice; the deploy of the same release only appends a log line; `backup.env` is never overwritten |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | `Needs:` line in each header; `find -printf` and `flock` are GNU/util-linux, both on Debian |
| 15 | A command sequence, constant, path or message used more than once is defined once (a function, a variable or a sourced helper); a helper is shared between scripts only where it expresses the same knowledge | Pass | `die` and `usage` in `scripts/lib/common.sh`; the two commands installed alone keep their own copy, stated in their headers; image names and patterns are one constant per script |

## Overall Verdict

Go — every Mandatory criterion of QC-SH-001 passes after the fixes of commit 6cd89c1 (constants, explicit flags, shared helpers). The behaviour on the real VM is not part of this record; it is MIL-021 task 8.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Independent review of the scripts on PR #117 | Tirsvad (S01) | Before the merge of PR #117 |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[064c27c]: https://github.com/NF-Hotel/NFHotel/commit/064c27c1d90793a6f3782e61cf4a91a4b96eba84
