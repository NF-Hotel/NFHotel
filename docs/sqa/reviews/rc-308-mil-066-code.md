# RC-308 Code review of MIL-066: the backup target, the settings archive, the migration history, the ignore check, the retention, the versions and the rebuild order

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-308 |
| CrossReference | [MIL-066], [RC-306], [DSA-001], [ADR-0008], [ADR-0010], [ADR-0011], [ADR-0020], [ADR-0025], [ADR-0034], [ADR-0038], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Proposed | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 6, 8 and 9 of MIL-066 before their pull request (task 10); Go-with-conditions until every required check of the pull request is green | pending |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-066-findings` against `main` at 56941c3; the commits d210506, a0aef65, 8ed338e, 7f9954b, cb353cf and a450626, and the merge of `main`, c9d5a77):
  - shell: `ops/deploy/backup-target/entrypoint.sh` (task 1), `ops/scripts/backup-database.sh` (tasks 2 and 3), `ops/scripts/deploy-release.sh` and `verify-database.sh` (task 4), `ops/scripts/scan-gate.sh` (task 5), `ops/scripts/scan-image.sh` and `make-migration-script.sh` (task 8), `ops/scripts/create-docker-vm.sh` (task 9), and the tests `ops/tests/test-backup-target.sh`, `test-backup-settings.sh`, `test-database.sh`, `test-deploy-release.sh`, `test-scan-gate.sh`, `test-monitoring-stack.sh` and the new `test-make-migration-script.sh`;
  - workflows and actions: `.github/workflows/ops-ci.yaml` (the test of the backup target in Debian 13, the new test in the list of script tests), `.github/workflows/ci.yaml` (the SonarScanner for .NET) and `.github/actions/deploy-release/action.yml` (the Tailscale client of CD);
  - YAML: `ops/deploy/monitoring/docker-compose.yml` and `prometheus/prometheus.yml` (task 6), the compose files of `ops/deploy/cloudflared` and `ops/deploy/postgres-tailnet` (task 8, one comment each) and the header of `ops/security/trivyignore.yaml` (task 5); `ops/scripts/db/roles.sql` and `ops/deploy/backup-target/Dockerfile` have no checklist of their own and were read against [ADR-0011] and task 1;
  - the wiki pages CI-CD, Containers-and-Images, Database-Preparation, Disaster-Recovery, Monitoring, Proxmox-Deployment, Security-Controls, Technology-Choices and Workflows.
- **Not in this record:** task 7, the ZAP rules, is in pull request #585 of its own, as the order of [MIL-066] says, and waits for S01's approval of each renewed rule; tasks 10 and 11 are the pull request, the deploys and the gate.
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001].
- **Scope:** delta review of the changed files against `main`.
- **Language and domain:** n/a (technical type).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the code and this record. S01 asked for the last milestones in autonomous mode in chat ("Fortsæt så med at lave de sidste milestones i automode on") and accepted the plan of MIL-066 in chat ("3ja", [RC-306]). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-08, on the PC of S01 (WSL 2, Docker 29.7.2):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean. The layout check ran from Git Bash, because the git of WSL cannot read this worktree.
- `ops/tests/test-workflow-policy.sh`: the workflows and actions follow the policy of ADR-0037; its self-test 8 of 8.
- In `debian:12-slim`, as the jobs of Ops CI run them: `test-deploy-release.sh` 95, `test-scan-gate.sh` 38 (7 of its new checks fail against the `scan-gate.sh` of `main`), `test-make-migration-script.sh` 9 (8 fail against the script of `main`) and `test-backup-settings.sh` 10; in `debian:13-slim`, `test-backup-target.sh` 24; `test-wiki-coverage.sh` 111, with the repository mounted.
- `ops/tests/test-database.sh` against PostgreSQL 16, with the migration script of a release and an API image: 72 passed, 22 of them in part 10. age is not installed in WSL, so the run put a stand-in for age on the path that copies instead of encrypting; CI runs the test with the real age (finding 4).
- Task 4 before and after, against PostgreSQL 16: with the code of `main`, `nfhotel_app` held SELECT, INSERT, UPDATE and DELETE on `__EFMigrationsHistory` in both orders (`t|t|t|t`), which the new `verify-database.sh` reports as a failure; with the branch, `t|f|f|f` in both orders.
- `promtool` of the pinned Prometheus v3.15.1 passes `prometheus.yml` and refuses the flat key `retention.time`; the pinned Prometheus, started with the file alone, reports a `storageRetention` of 30d.
- Trivy 0.75.0, the version of `main`, run offline on the test file of the scan gate, stops with "yaml: line 3: mapping values are not allowed in this context", the line that the new check names; it reads the quoted file and `ops/security/trivyignore.yaml`.
- `create-docker-vm.sh --dry-run` prints the download of the Debian 13 cloud image from `images/cloud/trixie` and the `qm` steps that import it. HEAD requests answered 200 for that image, for the `trixie` release file of the Docker repository, and for the two `trixie` files of the Tailscale repository that step 4 of the runbook fetches.
- The versions: the Tailscale client 1.104.5 (published 2026-11-03), Trivy 0.76.2 (2026-11-05, pinned by the digest of its image index) and the SonarScanner for .NET 12.1.0 (2026-11-05) are the newest stable releases on 2026-11-08; EF Core is 10.0.14 in `NFHotel.Infrastructure`, and `dotnet-ef` 10.0.14 is on NuGet.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1 | Header line and `set` options | Pass | The changed scripts keep `#!/usr/bin/env bash` and `set -euo pipefail`; the tests keep `set -uo pipefail`, so that every check runs and is counted, and the new `test-make-migration-script.sh` does the same |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | The list of the settings archive is an array built part by part (`mapfile -t -O`, `paths+=`), so a missing folder adds nothing; the check of the two required paths reads the whole array (`printf '%s\n' "${paths[@]}"` into `grep -qxF --`); the patterns of the scan gate are `readonly` constants on the right of `=~`, unquoted as bash needs, and `BASH_REMATCH` is read right after each match |
| SH 3 | Names | Pass | `snake_case` functions (`psql_as_migrator`, `scalar_of`, `read_entries`, and in the tests `checkout`, `installs`, `role_secrets`, `restore_dump`, `start_compose_pg`, `add_rooms`, `sign_in`) and variables (`copy_key`, `ef_version`); `UPPER_SNAKE` constants (`EF_DESIGN`, `DEBIAN_VERSION`, `DEBIAN_CODENAME`, `DQ`, `SQ`, `INDICATOR`, `FLOW`); the new test is kebab-case |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean. Each `disable` comment added gives its reason on the line: SC2016 in `psql_as_migrator` (moved from `migrate`) and in part 10 of `test-database.sh` ("expanded by the shell inside the container"), and SC2015 in the new test (each check runs exactly one of `ok` and `ko`) |
| SH 5 | Errors | Pass | `make-migration-script.sh` stops with `error:` before `dotnet` runs when the project file gives no version of three numbers (no design package, `10.0`, `10.0.14.1`, `10.0.*`, `11.0.0-preview.1`, no file; all tested), and a wrong call prints its usage and exits 2; the backup fails with "the settings archive would lack …" when `/etc/nfhotel` or the key of the copy exists and is not in the list; the migrate step fails when the rights on the history cannot be taken back, and says that the migrations were applied; the scan gate prints a FAIL line with the line number and the reason for each line outside its format, and fails |
| SH 6 | Temporary files | Pass | The new test works in a `mktemp -d` folder with `trap 'rm -rf "$W"' EXIT`; part 10 of `test-database.sh` works in the `mktemp -d` folder of the test, which its `cleanup` trap removes; the scripts add no temporary file |
| SH 7 | Secrets | Pass | No secret added. The log of the settings archive names the paths, never their content; the URL of the migrator stays in its file and reaches the container through `--env-file`; in part 10 the password of the team role is written by `manage-team-db-users.sh` into its file and read from there, never printed |
| SH 8 | State outside its folder | Pass | The flags are unchanged: `backup-database.sh`, `prepare-database-roles.sh` and `manage-team-db-users.sh` need `--apply`; `deploy-release.sh migrate` applies nothing without the hash of the script; `create-docker-vm.sh` needs `--container` or `--ssh` to name the node, says in its header that it changes state there, refuses a VM id that exists, and has `--dry-run` (MIL-004) |
| SH 9 | Header | Pass | The headers say what changed: the entry point of the target its rrsync options and its need of an rrsync with `-no-overwrite`; `deploy-release.sh` the revoke after the migrations; `verify-database.sh` the read-only history; `scan-gate.sh` the format check; `roles.sql` the rights of `nfhotel_app`. The new test states its purpose, how to run it, its exit codes and its needs |
| SH 10 | Implements the task it cites | Pass | Tasks 1 to 6, 8 and 9 of MIL-066; each commit has its `Task:` trailer. Deviations, in the findings: the backup is installed before the database, not last (finding 1); a test of the version source that the task row does not name (finding 2); pins younger than the cooldown of Dependabot (finding 3) |
| SH 11 | Tested for success, failure and the bypass path | Pass | `test-backup-target.sh` (Debian 13): a new file is written; a replace, a delete with `--delete` from an empty folder and a read are refused, and the files stay; a copy cut off midway with the old options leaves its part under the final name, which the next run keeps, and with the script leaves nothing, so the next run sends the whole file with the same SHA-256. `test-backup-settings.sh`: the archive holds `/etc/nfhotel` and the key without `nginx/tls`, and a backup whose list lacks one fails. `test-database.sh`: the history read-only in both orders while the API signs in; part 10 the order of the runbook, and the old order stops at its backup. `test-scan-gate.sh`: a file that Trivy cannot parse, a key that Trivy does not read and a key twice are refused, and `trivyignore.yaml` passes. `test-make-migration-script.sh`: two versions install, six inputs are refused. `test-deploy-release.sh`: the second call of the migrator. `test-monitoring-stack.sh`: the retention of 30d |
| SH 13 | Safe to re-run | Pass | The revoke of `roles.sql` runs only on a history that exists, and a second revoke changes nothing; the copy without `--partial` sends only the files that the target lacks; part 10 starts its containers fresh |
| SH 14 | Needs stated | Pass | The entry point of the target names an rrsync with `-no-overwrite`, as in Debian 13; the new test names bash 4+, coreutils and grep |
| SH 15 | Knowledge once | Pass | `psql_as_migrator` replaces the inline call and serves both statements of the migrate step; `copy_key` is defined once for the archive and the copy; `DEBIAN_VERSION` and `DEBIAN_CODENAME` give both the cloud image and the Docker repository; the EF Core version is read from the project file in place of a second constant. The revoke is written twice, in `roles.sql` and in the migrate step, on purpose (finding 6) |
| WF 1, YML 1 to 3 | YAML checks | Pass | yamllint 1.35.1 clean; LF, two spaces, no duplicate key |
| WF 2, 3 | Location, `name`, `on` and `jobs` | Pass | Unchanged |
| WF 4 | Header comment | Pass | The header of `ops-ci.yaml` still holds; a comment at the `include` of the script tests says why the test of the backup target runs in Debian 13 |
| WF 5 | Step names | Pass | "Run the test in Debian (12 unless the matrix names another image)" is unique in its job |
| WF 6 | actionlint | Pass | actionlint 1.7.7 clean |
| WF 7 | Least permissions | Pass | No permission added |
| WF 8 | Pinned actions | Pass | No action added or moved; the Tailscale action keeps its SHA, and only its `version` input changes; the policy test passes |
| WF 9 | Secrets | Pass | None added |
| WF 10 | Untrusted text | Pass | `matrix.test` and `matrix.image` come from the workflow file itself and reach the `run:` line through `env:` |
| WF 11, 12, 13 | Triggers with secrets, changes outside the repository, token scope | Pass | Unchanged |
| WF 14, 15, 16 | Runner and timeout, concurrency, trigger filters | Pass | Unchanged |
| WF 17 | Pinned tools | Pass | The Tailscale client 1.104.5 in the `version` input of the action, the SonarScanner 12.1.0 in its `--version`, Trivy 0.76.2 by the digest of its image index, and `dotnet-ef` at the version of the project file; each in one place |
| WF 18 | Cache | N-A | No install added |
| WF 19 | A failing step names the reason | Pass | The scripts name the file and the reason (SH 5) |
| WF 20, YML 7 | At most five lines of embedded program text | Pass | The `run:` of the script tests stays one line |
| WF 21 | Shared logic | Pass | Unchanged; CD keeps its steps in the composite action `deploy-release` |
| WF 22 | Features verified on the host | Pending | A matrix `include` that adds a key of its own (`image`) and the default `matrix.image \|\| 'debian:12-slim'` are new in the workflows of this repository: `main` has no `include:`, and its only `\|\|` default is in `.github/actions/deploy-release/action.yml`. actionlint accepts both; the Ops CI run of the pull request on GitHub-hosted runners is the proof, and the gate record names that run |
| WF 23 | A reusable workflow states how a caller pins it | N-A | No reusable workflow changed |
| WF 24 | A static test of the workflows | Pass | `test-workflow-policy.sh` and actionlint run in Ops CI and pass here |
| YML 4, 5, 6 | Coerced scalars, tags, secrets | Pass | `30d` and `debian:13-slim` stay strings; no tag; no secret |
| YML 8, 9 | Repeated blocks, anchors | Pass | No block repeated; no anchor |
| YML 10 | Schema | Pass | `promtool` of the pinned Prometheus v3.15.1 passes `prometheus.yml`; Trivy reads `trivyignore.yaml` with the quoted example (0.75.0 here; the release check of the pull request reads it with 0.76.2); the compose files each lose one line |
| YML 11 | Header comment | Pass | The new `storage` block of `prometheus.yml` says why it is there ([ADR-0020], MIL-066 task 6); the header of `trivyignore.yaml` says that the example statement is quoted, and why |
| YML 12, 13, 14 | Key names, block scalars, file names | Pass | The keys of Prometheus; no multi-line string added; the file names are unchanged |
| YML 15 | A check without the real target | Pass | `promtool`, `scan-gate.sh --check-ignore` and the tests above |
| MIL-066 1 | `test-backup-target.sh` on Debian 13 | Pass (in test); Pending (Ops CI) | 24 checks in `debian:13-slim`; Ops CI runs it there through the `include` of WF 22 |
| MIL-066 2 | The backup host runs the new image | Pending (host) | S01 redeploys the target on the backup host |
| MIL-066 3 | An interrupted copy | Pass (in test) | Part of `test-backup-target.sh` (SH 11) |
| MIL-066 4 | `test-backup-settings.sh` and the log on the VM | Pass (in test); Pending (VM) | 10 checks; the log of the first backup after the deploy goes into the gate record |
| MIL-066 5 | The history read-only, in both orders | Pass (in test); Pending (VM) | `test-database.sh`, both orders, and the API signs in; `verify-database.sh` on the VM after the deploy |
| MIL-066 6 | `test-scan-gate.sh` | Pass | 38 checks, 7 of which fail against the script of `main`; Trivy 0.75.0 stops at the line that the check names |
| MIL-066 7 | The ZAP rules | N-A | Task 7, in pull request #585 |
| MIL-066 8 | The pins and their checks | Pass (in the files); Pending (the checks and the VM) | The versions above are the newest stable releases on 2026-11-08, `make-migration-script.sh` reads the version of the project file, and the two stale comments are gone; the release check and one run of the daily scan with Trivy 0.76.2, CD with the new Tailscale client, the SonarCloud check and Docker Engine on the VM remain |
| MIL-066 9 | The retention in `prometheus.yml` | Pass (in the files); Pending (the monitoring container) | No flag in the compose file, 30d in the file, `promtool` passes, and the pinned Prometheus reports 30d; `/api/v1/status/runtimeinfo` after the monitoring deploy remains |
| MIL-066 10 | The runbook and its test | Pass (in test) | The section "Rebuild the VM" in 12 steps; part 10 of `test-database.sh` runs its database part in that order (22 checks); CI runs it with the real age |
| MIL-066 11 | Review Go, lint, wiki coverage, the checks | Pending | `lint.sh` clean; `test-wiki-coverage.sh` 111; this record is Go-with-conditions until every required check of the pull request is green |

## Findings

1. **The backup is installed before the database, not last.** The task row lists `install-backup.sh` last, but `prepare-database-roles.sh --transfer-ownership` and the migrate step of CD each take a backup first ([ADR-0008]), and stop without `/usr/local/sbin/nfhotel-backup` ("install the backup first"; "the backup failed; no migration was applied"). The runbook installs it in step 8, as the test of 2026-11-07 did, and part 10 of `test-database.sh` puts the backup command in place first, as `install-backup.sh` does. The deviation is in the commit and in the *Why* of step 8.
2. **A test that the task row does not name.** `test-make-migration-script.sh` proves the version source of criterion 8 with a fake `dotnet`, so nothing is downloaded; it runs among the script tests of Ops CI, and the wiki page Workflows names it. The CI job "Ops PostgreSQL preparation" still runs the script with the real tool.
3. **The new pins are younger than the cooldown of Dependabot.** `.github/dependabot.yml` keeps a new release out for 7 days, so that a compromised release has time to be found. On 2026-11-08 Trivy 0.76.2 and the SonarScanner 12.1.0 are 3 days old, and the Tailscale client 1.104.5 is 5 days old. Task 8 and criterion 8 ask for the newest stable release on the day of the task, and Dependabot does not see these three pins (a constant in a shell script, an input of an action and a `--version` in a `run:` line), so its cooldown does not cover them. The stand-in kept the newest: each comes from the official repository of its project, Trivy is pinned by the digest of its image index, and the CD run of the merge, the first to bring the new Tailscale client and Trivy into use, waits for S01's approval ([ADR-0038]). S01 may ask for the release before each instead.
4. **The local run of `test-database.sh` used a stand-in for age.** age is not installed in WSL, so the run put a script on the path that copies instead of encrypting. The parts of the test that encrypt and decrypt a backup are proven only in CI, which installs age; the Ops CI run of the pull request is that proof.
5. **`create-docker-vm.sh` keeps its defaults.** Its defaults for the name (`nfhotel-services`) and the size come from MIL-004; the runbook passes the name `nf-hotel` and the size of the old VM from `qm config`, so the defaults did not need to change. Every VM that the script creates is now Debian 13, the release of `nf-hotel` ([ADR-0010]); it built Debian 12 before.
6. **The revoke on the history is written twice, on purpose.** `roles.sql` takes the rights back on a history that exists when the roles are prepared; the migrate step of `deploy-release.sh` takes them back after each migration script, because on a new database the first migration creates the table after `roles.sql` has run, and the default privileges then give it to `nfhotel_app`. The two run at different times and as different roles (the superuser of the container, and `nfhotel_migrator`), so they stay two statements; `verify-database.sh` checks the result (`t|f|f|f`), so a drift between them fails that check.
7. **Whether the settings archives so far lacked `/etc/nfhotel` is still open.** Finding 3 of MIL-066 depends on whether `ops/deploy/nginx/tls` exists on the VM. The log of the first backup after the deploy names the archived paths, and the gate record says what it shows (criterion 4). A rebuild before that should check that the newest archive holds `/etc/nfhotel` (`tar --list`) before it relies on it ([ADR-0025]).
8. **What S01 must do after the merge** (the deploys of task 10): approve the CD run ([ADR-0038]); start `deploy-monitoring.sh --apply` on the monitoring container through the Tailscale SSH check; redeploy the backup target on the backup host, the copy that root on the VM must not be able to destroy (decision D7 of [DSA-001]), keeping its volume of host keys, so the VM keeps the host key that it pins; and update Docker Engine on the VM to 29.9.3 or newer, as Host-Hardening says. The team roles of [ADR-0034] are not touched by the deploy; step 11 of the runbook adds them only on a new server.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion of QC-SH-001, QC-WF-001 and QC-YML-001 that the branch can show passes. Criterion 22 of QC-WF-001 and criterion 11 of MIL-066 are Pending until every required check of the pull request is green, because the matrix `include` with its own key and the `||` default are new in the workflows of this repository; a Pending criterion allows Go-with-conditions at most. The criteria of MIL-066 that need the hosts (2, 4, 5, 8 and 9: the VM, the monitoring container and the backup host) close with the deploys of task 10 and the gate of task 11. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green; the gate record names the Ops CI run that proves criterion 22 of QC-WF-001 and records here that the conditions are met | AI as stand-in for S01 | At the merge |
| Approve the CD run of the merge ([ADR-0038]); its migrate step, verify step and smoke test are the proof of criteria 5 and 8 on the VM | S01, or the AI at S01's request in chat for that run | After the release run |
| Start `deploy-monitoring.sh --apply` on the monitoring container through the Tailscale SSH check; `/api/v1/status/runtimeinfo` then shows the retention (criterion 9) | S01 | After the CD run |
| Redeploy the backup target on the backup host with the same volume of host keys, then check the next copy (`.last-copy`, criterion 2) | S01 | After the merge |
| Update Docker Engine on the VM to 29.9.3 or newer, as Host-Hardening says (criterion 8) | S01 | After the CD run |
| Run the daily scan by hand after the deploys, and read the log of the first backup after the deploy (criterion 4); both go into the gate record | AI as stand-in for S01 | After the deploys |
| Write the gate record of task 11 against criteria 1 to 11 | AI as stand-in for S01 | 2026-11-22 |

---

[MIL-066]: ../../milestones/mil-066-findings-of-7-november.md
[RC-306]: ./rc-306-mil-066.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0010]: ../../adr/adr-0010-production-vm-as-deployed.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0025]: ../../adr/adr-0025-recovery-objectives.md
[ADR-0034]: ../../adr/adr-0034-team-database-access.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
