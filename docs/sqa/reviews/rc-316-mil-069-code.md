# RC-316 Code review of MIL-069: the prune of the backup target

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-316 |
| CrossReference | [MIL-069], [RC-315], [MIL-066], [RC-308], [RC-313], [DSA-001], [ADR-0008], [QC-SH-001], [QC-YML-001], [RC-322] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 3 of MIL-069 before their pull request (task 4); Go | [49defd4] |
| 2026-11-09 | Accepted | mikemike | AI as stand-in for S01 | The criteria that waited for task 5 and the pull request are met: every check that the ruleset required passed on #602, and the service `prune` runs on the backup host as the compose file says; its first pass had nothing to delete, and the gate read that pass at S01's request of 2026-11-09, not a pass after 2026-11-20 (RC-322, finding 1); the verdict stays Go | pending |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-069-retention` against `main` at e8403fd, the merge of the plan in #601):
  - shell: the new `ops/deploy/backup-target/prune-backups.sh` (task 1) and its part of `ops/tests/test-backup-target.sh` (task 1);
  - YAML: `ops/deploy/backup-target/docker-compose.yml`, the new service `prune` (task 2); the `Dockerfile` and `backup-target.env.example` have no checklist of their own and were read against task 2;
  - the wiki pages Disaster-Recovery (task 3) and Containers-and-Images.
- **Not in this record:** criteria 3 and 4 of [MIL-069] need the backup host (task 5, at S01's approval in chat), and criterion 5 includes this record and the checks of the pull request; the gate record of task 6 closes them.
- **Checklists used:** [QC-SH-001] and [QC-YML-001].
- **Scope:** delta review of the changed files against `main`.
- **Language and domain:** n/a (technical type).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the code and this record. S01 asked for the last milestones in autonomous mode in chat ("Fortsæt så med at lave de sidste milestones i automode on") and accepted the plan of MIL-069 in chat ("Ja til 1 ,2,3", [RC-315]). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-08, on the PC of S01 (WSL 2, Docker 29.7.2, Compose 5.5.0):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean.
- `test-backup-target.sh` in `debian:13-slim`, as Ops CI runs it: 37 passed, 0 failed: the 24 earlier checks, those of the write-only key of task 1 of [MIL-066] among them, and 13 checks of the prune.
- The mutation run: 13 copies of `prune-backups.sh`, each without one rule, against the 13 checks of the prune in `debian:13-slim` (a script stands in for what the first part of the test leaves: the user `nfhotel-backup` and four names in the format of `backup-database.sh`). The script passes 13 of 13, and every copy fails at least one check:
  - the limit with `<` in place of `<=`: the daily limit, the weekly limit and the log;
  - the time from the date of the file in place of the name: 11 of 13;
  - no floor: the daily and the weekly floor;
  - no dry run: the dry run and the log;
  - no check of the time in the name, and a `find` without `-type f`: the other names;
  - no check of `PRUNE_DAILY_DAYS`: 6 of 8 refusals;
  - no trap of TERM: the service loop does not end with 0 on TERM, in both loop checks;
  - no line per deleted file: the dry run, the log and the other names;
  - no failure on a file that cannot be deleted: both checks of that case;
  - a service loop that stops at a failed pass: the loop check of that case;
  - the names read one per line: the other names (a name with a line break);
  - a weekly floor of 4: the floors of the VM and the weekly floor.
- `test-wiki-coverage.sh`: 112 passed; the service `prune` is on Containers-and-Images.
- The compose check, in WSL, under the test project `mil069-check` and the tag `mil069-prune-check:local`, never the project `nfhotel-backup-target` or its image (both removed afterwards; the running target untouched):
  - the image, built from a copy whose two scripts have mode 0644, has both at 0755, and `/backups` owned by `nfhotel-backup`;
  - `docker compose config --quiet` passes, and shows the service `prune` as written: the entrypoint, `--apply --every 86400`, the user, no network, a read-only root file system, `cap_drop: ALL`, `no-new-privileges`, 64 MiB, 0.25 CPU and 32 processes;
  - against 56 test files (40 daily over 40 days, 15 weekly over 15 weeks, `.last-copy`), the dry run of the compose comment named 13 files and deleted nothing; `up -d prune` deleted those 13 and kept 43, `.last-copy` among them;
  - `docker inspect` of the running service shows the same settings, and `docker stop` ended it in 159 ms with exit code 0.
- The Dockerfile of `main`, built from a copy whose entry point has mode 0644, the mode in git: the entry point is 0644 in the image, and the container does not start (`exec: "/usr/local/sbin/backup-target": permission denied`, exit 126).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1 | Header line and `set` options | Pass | `prune-backups.sh` starts with `#!/usr/bin/env bash` and `set -euo pipefail`; the test keeps `set -uo pipefail`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | Every expansion is quoted; the names of a kind are an array read NUL-separated (`find -printf '%f\0'`, `sort -rz`, `mapfile -d ''`), so a name with a line break stays one name, which the check of the time skips (finding 2); the prefix and the suffix are removed with quoted patterns (`${name#"$prefix-"}`), and the pattern of `find -name` is quoted, so the shell does not expand it |
| SH 3 | Names | Pass | `prune-backups.sh` is kebab-case; `snake_case` functions (`die`, `whole_number`, `name_time`, `prune_kind`, `prune_all`) and variables; `UPPER_SNAKE` constants (`DAY_SECONDS`, `USAGE`) and environment variables (`BACKUP_DIR`, `PRUNE_DAILY_DAYS`, `PRUNE_WEEKLY_WEEKS`, `PRUNE_DAILY_KEEP`, `PRUNE_WEEKLY_KEEP`, `PRUNE_NOW`). The new helpers of the test are `snake_case` (`backup_name`, `make_backup`, `run_prune`, `present`, `count_named`, `fresh_folder`, `vm_keep`, `failed_passes`); its fixed paths and times keep the upper case that the file already gives them (`W`, `REPO`, `ENTRY`, so `PRUNE`, `P`, `NOW`, `DAY`) |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean. One `disable` added, SC2086 at the loop over lists of arguments in the test, with its reason on the line |
| SH 5 | Errors | Pass | `die` writes `error: …` to standard error and exits 1; a bad or missing argument prints the usage line; a limit that is not a whole number from 1 to 999999 is named with its value; a missing backup folder is named. A file that cannot be deleted gives `error: could not delete <name>` and, after the other kinds, exit 1; in the service, a failed pass gives `error: a file could not be deleted; the next prune is in <n> seconds`, and the next pass runs (both tested) |
| SH 6 | Temporary files | Pass | The script writes no file: the names come through a pipe, and a sort of a few hundred names (finding 1 of [MIL-069]: 201 daily dumps in 18 days) stays in memory, so the read-only root file system of the service does not stop it (the compose check). The test works in its `mktemp -d` folder, which its `trap … EXIT` removes; the folder that the user of the service may not write is inside it |
| SH 7 | Secrets | Pass | No secret. The script reads names, never the content of a file; its log names files and counts them |
| SH 8 | State outside its folder | Pass | It deletes in the backup folder, so a dry run is the default and `--apply` deletes, as the header says; the service passes `--apply` in the compose file, whose comment gives the dry run |
| SH 9 | Header | Pass | Purpose, with why the backup host and not the VM deletes ([DSA-001] D7, [ADR-0008], task 1 of [MIL-066]); the four steps; the floors and why; usage, options, the environment variables with their defaults, the exit codes and the needs. The header of the test names the new part |
| SH 10 | Implements the task it cites | Pass | Tasks 1 to 3 of MIL-069; the commit has its `Task:` trailers. Deviations, in the findings: the Dockerfile change reaches the entry point of the existing service (finding 1); checks that the task row does not name (finding 2); the floors are settings of the script that the service does not pass (finding 3) |
| SH 11 | Tested for success, failure and the bypass path | Pass | 13 checks in `debian:13-slim`: the limits to the second for daily and weekly files; the floors for all four kinds; the time from the name (a file named 20 days back but dated 2000 is kept, one named 40 days back is deleted); the dry run (the path without `--apply`) deletes nothing and names what it would; no other name is touched (the temporary files of `rsync`, other times and kinds, month 13, a link, a folder, a name with a line break); eight refusals that delete nothing; the service prunes at its start and after the wait, and ends with 0 on TERM; a file that the prune, run as `nfhotel-backup` through `setpriv` as the service runs it, cannot delete is named and fails the run, and the service goes on to the next pass; and the prune reads every name that `backup-database.sh` wrote in the first part of the test. The mutation run shows that each check catches the rule it tests |
| SH 13 | Safe to re-run | Pass | A second pass finds the expired files gone and deletes nothing more; the dry run changes nothing (the compose check ran it twice: 56 files after both); the restart policy starts a pass at every start of the container, which is safe for the same reason |
| SH 14 | Needs stated | Pass | "bash 4.4+ (mapfile -d), GNU coreutils and findutils (date -d, sort -z, find -printf)"; the image is Debian 13. The test uses `setpriv` of util-linux, which every Debian image has |
| SH 15 | Knowledge once | Pass | One function per pass and per kind, and the four kinds are four calls of `prune_kind`; `whole_number` checks all five numbers; `DAY_SECONDS` and `USAGE` once. The format of the names and the numbers that the VM keeps are the contract between `backup-database.sh` on the VM and the prune on the backup host, which are deployed apart (`install-backup.sh` and the image), so each keeps its own copy; a check of the test holds them together (finding 2), and a copy of the prune with a weekly floor of 4 fails it |
| YML 1 | Parses, no duplicate key | Pass | `docker compose config` of Compose 5.5.0 reads the file; yamllint 1.35.1 finds no duplicate key |
| YML 2 | Indentation, LF, end of file | Pass | 2 spaces, LF, one newline at the end, no trailing space (yamllint) |
| YML 3 | yamllint | Pass | yamllint 1.35.1 with `ops/.yamllint` clean; no `disable` comment |
| YML 4 | Coerced scalars | Pass | The command is a list of strings, `"86400"` among them; `cpus: 0.25` and `pids_limit: 32` are numbers, as Compose expects; `mem_limit: 64m` and `pull_policy: never` are strings |
| YML 5 | Safe types | Pass | No tag |
| YML 6 | Secrets | Pass | No secret; the public key of the VM stays in `backup-target.env`, which git ignores |
| YML 7 | Embedded program text | Pass | None; the command is a list of arguments |
| YML 8 | Repeated blocks | Pass | The image and the volume of the backups, which both services use, are each defined once (`x-target-image`, `x-backups`) |
| YML 9 | Anchors | Pass | The anchors sit in `x-` extension fields of the Compose specification, as in the compose file of the benchmark, and `docker compose config` of Compose 5.5.0 resolves them |
| YML 10 | Schema | Pass | `docker compose config --quiet` of Compose 5.5.0 validates the file against the Compose specification |
| YML 11 | Header comment | Pass | The header says what the file is for, the target and the prune, how to start both, and where the decisions are (D7, [ADR-0008], MIL-069); the service `prune` has a comment on why it exists, when it runs, and how to read its log and run a dry run |
| YML 12 | Key names | Pass | The keys of Compose, one style (`pull_policy`, `network_mode`, `read_only`, `cap_drop`, `security_opt`, `mem_limit`, `pids_limit`) |
| YML 13 | Block scalars | N-A | No multi-line string |
| YML 14 | File name | Pass | No YAML file added; the name of the compose file is unchanged |
| YML 15 | A check without the real target | Pass | `docker compose config` and the compose check under a test project and tag, without the backup host |
| MIL-069 1 | The retention proven in `debian:13-slim` | Pass | The 13 checks of SH 11, and every case fails against a script without the rule it tests (the mutation run) |
| MIL-069 2 | The tests of the write-only key unchanged | Pass | The 24 earlier checks pass, those of the write-only key of task 1 of [MIL-066] among them; the diff of their part of the test is its header only |

## Findings

1. **The Dockerfile change reaches the existing service.** Task 2 asks for the prune script in the image; the branch also gives the entry point of the target `COPY --chmod=0755`. `entrypoint.sh` has mode 100644 in git, so the Dockerfile of `main`, built from a Linux checkout, makes an image whose container does not start (`permission denied`, exit 126, shown above). The target on the backup host was built from the checkout on the Windows file system of that host and runs, so its entry point was executable there and the defect did not show. With `--chmod`, the mode of the checkout no longer matters: from a copy at 0644, both scripts are 0755 in the image. `COPY --chmod` needs BuildKit, the default builder of Docker Engine since 23.0, of Docker Desktop and of Compose; the builds above used it.
2. **Checks that the task row does not name.** Three checks came from this review. The prune reads every name that `backup-database.sh` wrote to the target in the first part of the test, and by default keeps the numbers of `DAILY_KEEP` and `WEEKLY_KEEP` of that script (SH 15). A file that the prune cannot delete, run as the user of the service (SH 11). And a name with a line break (SH 2): the first version read the names one per line, so such a name split in two, and its second line could be the valid name of an old copy. That could not delete a copy before its limit, because the time comes from the name of the copy itself, but it miscounted the floors and logged a deletion that did not happen. The names are now NUL-separated, and a copy of the script that reads them one per line fails the check.
3. **The floors are settings of the script, not of the service.** `PRUNE_DAILY_KEEP` and `PRUNE_WEEKLY_KEEP` default to the numbers that the VM keeps, and the tests set them to 1 to test one rule alone. The compose file and `backup-target.env.example` do not pass them, because a floor below the number of the VM brings back finding 3 of [MIL-069]: a file deleted on the target while the VM keeps it comes back with the next copy.
4. **What the retention holds against.** The prune deletes a copy only when the time in its own name is past the limit, and the key of the VM can neither rename nor replace a file on the target, so a VM that is taken over cannot make the target delete a copy sooner than 30 days (daily) or 12 weeks (weekly) after it was made. The floors keep the newest copies when the backups stop, for example when the VM is lost; they protect against that accident, not against the VM, whose key can add files with newer names, which then fill the floors. A name with a time in the future is kept until that time has passed, like any other file that the key adds, and the key could fill the disk with files anyway. The wiki page Disaster-Recovery says this.
5. **No health check and no alert of its own.** A failed pass is in `docker compose logs prune`, and the next pass runs a day later. If the prune stops for good, the folder grows by about 47 MB a day until the disk of the backup host is full; the copy of the VM then fails, and the alert `OffVmCopyStale` fires. The prune runs at the start of the container and then a day after each pass ends, so a restart of the backup host starts a pass at once.
6. **The first prune on the backup host will delete nothing.** The oldest copy there is from 2026-10-21 (finding 1 of [MIL-069]), so the first daily copies pass 30 days on 2026-11-20, and the first weekly copy, of Sunday 2026-10-25, passes 12 weeks on 2027-01-17, after the target date of 2026-12-31. The gate record can show the daily rule on the backup host with the log of a pass after 2026-11-20; the weekly rule rests on the tests until 2027-01-17.

## Overall Verdict

Go. Every Mandatory criterion of QC-SH-001 and QC-YML-001 passes, and criteria 1 and 2 of MIL-069 pass in the tests. Criteria 3 and 4 need the backup host (task 5), and criterion 5 needs the checks of the pull request; the gate record of task 6 closes them, and [RC-322] closed them on 2026-11-09. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green | AI as stand-in for S01 | At the merge |
| Publish MIL-068 and MIL-069 as milestones and issues on GitHub (`sync-project.sh --apply`), so that the pull request can close the issues of tasks 1 to 4 | S01 | Before the merge, or close them by hand after it |
| Approve in chat the redeploy of the backup target with the prune (task 5) | S01 | 2026-12-31 |
| Redeploy on the backup host, then read `docker inspect` of `prune`, the log of its first pass and the next copy of the VM (criteria 3 and 4) | AI as stand-in for S01 | After S01's approval |
| Write the gate record of task 6 against criteria 1 to 5, with the log of a pass after 2026-11-20 (finding 6) | AI as stand-in for S01 | 2026-12-31 |

---

[MIL-069]: ../../milestones/mil-069-retention-on-the-backup-target.md
[MIL-066]: ../../milestones/mil-066-findings-of-7-november.md
[RC-315]: ./rc-315-mil-069.md
[RC-308]: ./rc-308-mil-066-code.md
[RC-313]: ./rc-313-mil-066-gate.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[RC-322]: ./rc-322-mil-069-gate.md
[49defd4]: https://github.com/NF-Hotel/NFHotel/commit/49defd4fa3215cb4ef13e51aad4abd649658de38
