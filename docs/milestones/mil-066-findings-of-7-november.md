# MIL-066 The findings of 7 November: the backups, the recovery, the database rights and the gates

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-066 |
| CrossReference | [BC-001], [DSA-001], [ADR-0008], [ADR-0011], [ADR-0025], [ADR-0034], [ADR-0038], [MIL-021], [MIL-022], [MIL-027], [MIL-037], [MIL-044], [MIL-065] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version: one task for each finding of 2026-11-07 that the repository can fix (the backup target, the copy off the VM, the settings archive, the migration history, the ignore check, the ZAP rules, the versions, the retention of Prometheus and the rebuild runbook); planned in autonomous mode at S01's request in chat ("godkendt") and accepted by S01 in chat ("3ja", RC-306) | [be88a83] |

---

## Purpose

Close the findings of 2026-11-07 that the repository can fix, so that a backup off the virtual machine (VM) can be neither replaced nor deleted, every settings archive holds what a rebuild needs, the runbook rebuilds the VM in an order that works, the database role of the application programming interface (API) cannot rewrite the migration history, and the checks of the scans still pass after 2026-11-25.

On 2026-11-07 the setup of each part of NF Hotel was rebuilt step by step in containers, from the operations scripts and the wiki pages alone. Each finding below was reproduced there, except the copy finding, which was found in the code on 2026-11-08 and which task 2 reproduces before it fixes it.

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The key of the VM may replace a file on the backup target and delete every file there (`rsync --delete` from an empty folder): `rrsync -wo` allows writes, not only new files. In a container test, `-no-del` stopped the deletion and `-no-overwrite` stopped the replacement; only the `rrsync` of Debian 13 has `-no-overwrite`, and the image is built on `debian:12-slim` | `ops/deploy/backup-target` | Someone with root on the VM, the threat that decision D7 of [DSA-001] keeps a copy off the VM for, can destroy that copy too |
| 2 | The copy runs `rsync --partial --ignore-existing`: an interrupted copy keeps the part it sent under the final name, and `--ignore-existing` never replaces it | `ops/scripts/backup-database.sh` | A cut-off backup on the target that looks complete |
| 3 | The list of paths for the settings archive is built in a process substitution under `set -e`. When `ops/deploy/nginx/tls` is missing, its `find` fails and ends the list, so `/etc/nfhotel` and the ssh key of the copy are left out, and the backup still ends with success (two paths in place of three in the test) | `ops/scripts/backup-database.sh` | A settings archive without the secrets that a rebuild needs; whether production lacks the folder is not yet checked |
| 4 | `roles.sql` grants `INSERT`, `UPDATE` and `DELETE` on every table, now and by default privileges later, to `nfhotel_app`; `team-roles.sql` takes them back on `__EFMigrationsHistory` only from `nfhotel_team` | `ops/scripts/db` | The API, if taken over, can mark a migration as applied or remove its row, so the next migration skips it or runs it twice ([ADR-0011]) |
| 5 | The steps of "Rebuild the VM" fail in their own order: the continuous deployment (CD) run of step 8 migrates before the restore of step 9, and its backup fails (`role "nfhotel_backup" does not exist`); with the backup left out, the migrator cannot sign in. Step 9 runs `prepare-database-roles.sh` without `--apply`. The VM is created without `--start`; its console has no password login, so step 3 cannot run there; Tailscale is not installed and the tag `tag:nfhotel-vm` is not set; `install-deploy-user.sh` is missing; the team roles are not in a dump; and `/opt/nfhotel` must belong to the user `nfhotel` | `docs/wiki/Disaster-Recovery.md`, `ops/scripts/create-docker-vm.sh` | A rebuild that stops halfway, under the recovery time objective of [ADR-0025] |
| 6 | `scan-gate.sh --check-ignore` reads the ignore file line by line and passes a file that Trivy 0.75.0 cannot parse, such as a statement with `: ` in a plain YAML value | `ops/scripts/scan-gate.sh` | The check passes a file that the scan of the release then cannot read ([MIL-022]) |
| 7 | The nine `IGNORE` rules of `ops/security/zap-rules.tsv` run out on 2026-11-25 | `ops/security/zap-rules.tsv` | From 2026-11-26 `dast-scan.sh --check-rules` fails, and with it the dynamic application security testing (DAST) scans ([MIL-027], [MIL-044]) |
| 8 | Versions behind on 2026-11-07: the Tailscale client of CD 1.102.4 (1.104.5 out), Trivy 0.75.0 (0.76.2), the SonarScanner for .NET 11.3.0 (12.1.0), Docker Engine 29.8.0 on the VM (29.9.3); `dotnet-ef` 10.0.13 while EF Core in `NFHotel.Infrastructure` is 10.0.14, although the script says to bump them together. The comments at the cloudflared and HAProxy pins say that Dependabot does not see their lines; since MIL-059 it does (pull request #546 bumped cloudflared) | `.github`, `ops/scripts`, `ops/deploy` | Known fixes come late, and a wrong comment invites a bump by hand next to Dependabot's |
| 9 | `--storage.tsdb.retention.time` is deprecated: the help of Prometheus v3.15.1 marks it `[DEPRECATED]`, and its `promtool` accepts the field `storage.tsdb.retention.time` of the configuration file in its place (checked on 2026-11-08) | `ops/deploy/monitoring` | A later version may drop the flag, and Prometheus would then not start |

Not in this milestone, because they are decisions of S01 and not changes of the repository: the `max-age` of 30 days in the `Strict-Transport-Security` (HSTS) header of the public site (`NGINX_HSTS` on the VM), the Domain Name System Security Extensions (DNSSEC) of the public domain, and the generic patterns of secret scanning in the settings of the repository.

## Deliverable

- **The backup target** on Debian 13, pinned by digest, whose key may only run `rrsync -wo -no-del -no-overwrite`, so it accepts new files and refuses to replace, delete or read one; redeployed by S01 on the backup host with the same host key.
- **The copy off the VM** without `--partial`, so an interrupted copy leaves nothing under the final name, and the next run sends the whole file.
- **The settings archive**, which always holds `/etc/nfhotel` and the ssh key of the copy when they exist, or fails the backup; the log names each archived path (the names only).
- **The migration history**, which `nfhotel_app` may read and not change, on an existing database and on a new one.
- **The ignore check**, which refuses an ignore file that Trivy cannot parse.
- **The ZAP rules**, each checked again before 2026-11-25 and renewed with S01's approval, or removed.
- **The versions** of finding 8 at the newest stable release on the day of the task, the stale comments removed, and Docker Engine on the VM updated.
- **The retention of Prometheus** in its configuration file in place of the deprecated flag.
- **The rebuild runbook** in the order tested on 2026-11-07, with `create-docker-vm.sh` to match, and a test that runs the database part of a rebuild in that order.
- **The wiki pages** that describe each change, and the proof: the deploys and their checks, as the criteria say.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then task 7 in a pull request of its own, so that the ZAP rules are renewed before they run out on 2026-11-25; then tasks 1 to 6, 8 and 9 on one branch, task 10 with its pull request and the deploys, and last task 11.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `test-backup-target.sh`, run on Debian 13, proves that the key of the VM can write a new file into the target, and that replacing a file, deleting files (`--delete` from an empty folder) and reading a file back are refused, with the files on the target unchanged | Yes | No |
| 2 | The backup host runs the new image (`docker inspect`), its host key is the one that the VM pins, and the next copy of the VM after the redeploy succeeds (`.last-copy` updated) | Yes | No |
| 3 | A test interrupts a copy midway: with the old options the part it sent stays under the final name and the next run keeps it; with the new options the next run sends the whole file, and its SHA-256 on the target equals the one on the VM | Yes | No |
| 4 | `test-backup-settings.sh` proves that the archive holds `/etc/nfhotel` and the ssh key of the copy without `ops/deploy/nginx/tls`, and that the backup fails when one of them exists and is not archived; on the VM, the log of the first backup after the deploy names `/etc/nfhotel` among the archived paths | Yes | No |
| 5 | `test-database.sh` proves that `nfhotel_app` can read `__EFMigrationsHistory` and is refused `INSERT`, `UPDATE` and `DELETE` on it, both on a database whose roles were prepared before its first migration and on one migrated before its roles, while the API still signs in and works; `verify-database.sh` on the VM reports the same after the deploy | Yes | No |
| 6 | `test-scan-gate.sh` proves that `scan-gate.sh --check-ignore` refuses an ignore file with a statement that Trivy cannot parse, and passes `ops/security/trivyignore.yaml` | Yes | No |
| 7 | Each rule of `ops/security/zap-rules.tsv` was checked again against the newest DAST report and either removed or renewed with S01's approval in the pull request, and `dast-scan.sh --check-rules ops/security/zap-rules.tsv 2026-11-26` passes | Yes | No |
| 8 | The pins of finding 8 equal the newest stable releases on the day of the task, and `make-migration-script.sh` uses the EF Core version of `NFHotel.Infrastructure`; the release scan and one run of the daily scan pass with the new Trivy, CD reaches the VM with the new Tailscale client, the SonarCloud check is green, `docker version` on the VM shows the new Docker Engine, and the two stale comments are gone | Yes | No |
| 9 | The compose file of the monitoring stack has no `--storage.tsdb.retention.time`, its `prometheus.yml` sets `storage.tsdb.retention.time` to 30d, `promtool` of the pinned version passes it, and after the monitoring deploy `/api/v1/status/runtimeinfo` reports a retention of 30d | Yes | No |
| 10 | The section "Rebuild the VM" of the wiki page Disaster-Recovery gives the tested order, and a test runs its database part in that order in containers (an empty PostgreSQL, the restore of a dump, the roles with `--apply` and `--transfer-ownership`, then the migration of CD, which skips, and a sign-in of the API) and passes | Yes | No |
| 11 | The code review against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` is Go, `lint.sh` is clean, `test-wiki-coverage.sh` passes, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-065 | Its deploys come first: one CD run at a time in `production`, and the monitoring stack on Prometheus v3.15.1 before task 6 moves its retention |
| ADR-0008 | The backup before each migration and the write-only copy off the VM |
| DSA-001 | Decision D7, the copy that root on the VM must not be able to destroy; section 8, an accepted finding and a ZAP rule are approved by S01 |
| ADR-0011 | The roles of the API and of the migrator, and the rights of each |
| ADR-0034 | The team roles, which a dump does not hold |
| ADR-0025 | The recovery objectives that the rebuild must meet |
| ADR-0038 | The CD run of the merge waits for the approval of S01 |
| MIL-037 | The runbook of a rebuild and the settings archive |
| MIL-021 | The backup target, task 7 |
| MIL-022 | The ignore file and the gate of the release |
| MIL-027 | The DAST scans and their rules, kept current by MIL-044 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23: the database has a backup whose restore is tested | Criteria 1 to 5 and 10: a backup that root on the VM cannot destroy, an archive that holds what a rebuild needs, and a rebuild in an order that works |
| BC-001 success criterion 26: a restore of the daily backup is recorded | Criterion 10: the database part of a rebuild runs in a test |
| BC-001 success criterion 27: 0 critical and 0 fixable high findings in the deployed images; 0 unpinned actions or image tags | Criteria 6 and 8: an ignore file that Trivy reads, and a current scanner |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-22, three days before the nine ZAP rules run out on 2026-11-25; task 7 comes first, so the DAST checks keep passing even if the rest runs late. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The backup target: new files only | `ops/deploy/backup-target/Dockerfile` starts from `debian:13-slim` pinned by digest, and the entry point writes `command="rrsync -wo -no-del -no-overwrite <folder>",restrict <key>`, so the key of the VM may add files and may neither replace, delete nor read one (finding 1). `test-backup-target.sh` runs on Debian 13 and proves each refusal. The host keys stay in their volume, so the VM keeps the same pinned host key; the redeploy on the backup host is done by S01, and the running container is not touched before that. | No | criteria 1, 2 |
| 2 | The copy off the VM: no partial files | A test first reproduces finding 2: a copy interrupted midway with `--partial` leaves the part it sent under the final name, and the next run with `--ignore-existing` keeps it. Then `backup-database.sh` copies without `--partial`, so an interrupted copy leaves nothing under the final name, and the test proves that the next run sends the whole file. | No | criterion 3 |
| 3 | The settings archive: complete or failed | In `backup-database.sh` a `find` that finds nothing no longer ends the list of paths (finding 3); after the list, the backup fails when `/etc/nfhotel` or the ssh key of the copy exists and is not in it, and the log names each archived path, never its content. `test-backup-settings.sh` covers a host without `ops/deploy/nginx/tls`. After the deploy, the backup log on the VM shows whether the archives so far lacked `/etc/nfhotel`; the gate record says so. | No | criterion 4 |
| 4 | The migration history: read-only for the API | `roles.sql` takes `INSERT`, `UPDATE` and `DELETE` on `__EFMigrationsHistory` back from `nfhotel_app` when the table exists, and the migrate step of `deploy-release.sh` does the same after each migration script, because on a new database the first migration creates the table and the default privileges grant it to `nfhotel_app` (finding 4). `verify-database.sh` checks it, and `test-database.sh` covers both orders. | No | criterion 5 |
| 5 | The ignore check: what Trivy cannot parse | `scan-gate.sh --check-ignore` refuses an ignore file that Trivy cannot parse, such as a statement with `: ` in a plain YAML value, before it checks each entry for its statement and expiry (finding 6). `test-scan-gate.sh` covers it with such a file and with the real `ops/security/trivyignore.yaml`. | No | criterion 6 |
| 6 | The retention of Prometheus in its configuration | The monitoring stack sets `storage.tsdb.retention.time: 30d` in `prometheus.yml` and drops the deprecated flag from its compose file (finding 9), after the deploy of MIL-065 has moved the monitoring container to v3.15.1. Ops CI checks the configuration with `promtool` of the pinned version; after the monitoring deploy, `/api/v1/status/runtimeinfo` shows the retention. | No | criterion 9 |
| 7 | The ZAP rules before 2026-11-25 | Each of the nine `IGNORE` rules of `ops/security/zap-rules.tsv` is checked again against the newest DAST report of `e2e-dast.yaml` (finding 7). A rule whose alert still comes and whose reason still holds gets a new expiry at most 30 days ahead, approved by S01 in the pull request, as the header of the file says; a rule whose alert no longer comes is removed. | No | criterion 7 |
| 8 | The versions | The Tailscale client of CD (`.github/actions/deploy-release/action.yml`), Trivy (`scan-image.sh`), the SonarScanner for .NET (`ci.yaml`) and `dotnet-ef` (`make-migration-script.sh`, at the EF Core version of `NFHotel.Infrastructure`) move to the newest stable release on the day of the task (finding 8). The comments at the cloudflared and HAProxy pins that say Dependabot does not see their lines go. Docker Engine on the VM is updated by hand, with S01, as the wiki page Host-Hardening says, since the patching installs only Debian's security updates. | No | criterion 8 |
| 9 | The rebuild runbook in the tested order | The section "Rebuild the VM" of Disaster-Recovery follows the order tested on 2026-11-07 (finding 5): create the VM with `--start`; sign in over ssh with the key of cloud-init; install Tailscale and join with the hostname `nf-hotel` and the tag `tag:nfhotel-vm`; harden the host; check out the repository as `nfhotel`, the owner of `/opt/nfhotel`; restore the settings; run `install-deploy-user.sh`; start PostgreSQL empty, restore the dump, then `prepare-database-roles.sh --apply` and `--transfer-ownership`; only then the CD run, whose migration skips; add the team roles again; install the backup. `create-docker-vm.sh` builds the Debian release of the VM it replaces (`/etc/os-release` on the VM). A test runs the database part in this order in containers. | No | criterion 10 |
| 10 | Code review, pull request and deploys | The review record of the shell scripts, the workflows and the configuration files against `QC-SH-001`, `QC-WF-001` and `QC-YML-001` before the pull request; each pull request closes one issue per task it completes and carries the milestone. The CD run of the merge, approved by S01 ([ADR-0038]), deploys the VM; `deploy-monitoring.sh --apply` deploys the monitoring stack; S01 redeploys the backup target and updates Docker Engine on the VM; then one run of the daily scan. | No | criteria 2, 4, 5, 8, 9, 11 |
| 11 | Gate and review record | The gate review record against criteria 1 to 11 with the logs of the deploys, the backup and the daily scan, the row of MIL-066 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 11 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0025]: ../adr/adr-0025-recovery-objectives.md
[ADR-0034]: ../adr/adr-0034-team-database-access.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[MIL-037]: ./mil-037-disaster-recovery.md
[MIL-044]: ./mil-044-security-overview.md
[MIL-065]: ./mil-065-third-party-image-remedies.md
[be88a83]: https://github.com/NF-Hotel/NFHotel/commit/be88a833a65947c26ee8c4d2cf53401ec0ffb22a
