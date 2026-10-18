# RC-101 Review of the MIL-024 scripts and the Go/No-Go evidence

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-101 |
| CrossReference | [MIL-024], [ADR-0011], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [891129b] |

---

## Artifact Under Review

- Instance reviewed: `scripts/prepare-database-roles.sh`, `scripts/verify-database.sh`, `scripts/check-tailnet-exposure.sh`, the changes to `scripts/backup-database.sh` and `scripts/restore-drill.sh`, `scripts/db/roles.sql`, `scripts/db/transfer-ownership.sql`, `deploy/nginx/optional/api.conf` and the include in `deploy/nginx/templates/default.conf.template`, at commit e3a9fca
- Checklist used: [QC-SH-001] for the shell scripts. The framework has no checklist for SQL or nginx files; they are covered by the tests below, a gap proposed upstream (rule 5).
- Scope: the changes of MIL-024
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`); an independent review is requested on the pull request
- Tools: shellcheck 0.11.0, shfmt 3.14.1, `bash -n`, PostgreSQL 16 (`postgres:16-alpine`) with the idempotent migration script of the repository (`dotnet ef migrations script --idempotent`), the `nfhotel-api` image of the release build, nginx 1.27

## Go / No-Go criteria of MIL-024

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | `nfhotel_app` may read and write every table, and is refused `CREATE`, `DROP`, `ALTER`, `TRUNCATE` and access to another database | Pass | Test against PostgreSQL 16 with the real migrations: SELECT/INSERT/UPDATE/DELETE pass; CREATE TABLE, DROP TABLE, ALTER TABLE, TRUNCATE, CREATE TEMP TABLE, connecting to `postgres` and reading `pg_authid` are refused |
| 2 | The idempotent migration script run as `nfhotel_migrator` succeeds; new objects are owned by `nfhotel_owner` and usable by `nfhotel_app` | Pass | The script ran again with no change; a new table is owned by `nfhotel_owner`; `nfhotel_app` used it with no extra grant; without `SET ROLE` the migrator cannot create (no INHERIT) |
| 3 | The ownership transfer leaves every row count and `__EFMigrationsHistory` unchanged | Pass | `3/250/0/20260907053716_InitialCreate,20261016214737_AddStaffMember` before and after; the container ID and start time unchanged |
| 4 | The API image answers `/health` 200 and reads the database through the alias `postgres` as `nfhotel_app` | Pass | `nfhotel-api` image of the Release build: `/health` 200, sign-in (the first manager written as `nfhotel_app`), `/api/rooms` returned the 3 rooms |
| 5 | A dump made as `nfhotel_backup` restores with the `EXCLUDE` constraint and the migration history | Pass | `backup-database.sh` with `BACKUP_CONTAINER` and `BACKUP_ROLE=nfhotel_backup`; the restore drill restored 5 tables and reported the latest migration `20261016214737_AddStaffMember`; the backup role cannot write |
| 6 | No password, key or production connection string in git or in script output; passwords on standard input only | Pass | The test searched the script output, the server log and the API log for the password: not found; files 0600; `git grep` finds no secret |
| 7 | On `nf-hotel`, `verify-database.sh` passes and 5432 answers only to `group:nfhotel-admins` | Fail | Not run: the assessor has no access to the VM. `check-tailnet-exposure.sh` from a non-administrator device shows 5432 open with no TLS (R15). Task 7 is for S03 |
| 8 | Reviews against `qc-programming-shell`, `qc-yaml` and `qc-adr` are `Go` | Pass | This record (shell), RC-100 (ADR-0011); no YAML file changed in MIL-024 |

## Defects found and fixed by the tests

| Defect | Fix |
| --- | --- |
| `restore-drill.sh` read the history column `MigrationId`; the project uses snake_case (`migration_id`), so the drill would fail on a real backup | Fixed in commit 5670ba3 on PR #117 |
| The restore drill failed on a dump that carries grants to the ADR-0011 roles | `pg_restore --no-acl` in the drill (5670ba3) |
| Turning on `optional/api.conf` stopped nginx (`proxy_read_timeout` set twice through `proxy.conf`) | The route sets its own headers (e3a9fca); tested off, on without the API, on with the API |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` (or a comment explains the exception) | Pass | `prepare-database-roles.sh`, `verify-database.sh`, `check-tailnet-exposure.sh`; `backup-database.sh` unchanged in this respect |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | shellcheck clean; the role and port lists are arrays |
| 3 | Names follow the conventions: `kebab-case.sh` files, `snake_case` functions and variables, `UPPER_SNAKE` constants and environment variables | Pass | `NETWORK`, `ALIAS`, `LOGIN_ROLES`, `OPEN_PORTS` are readonly constants |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck 0.11.0 `-x`: 0 findings; the one `disable=SC2016` in `backup-database.sh` says the expansion happens inside the container |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code; bad or missing arguments print a usage line | Pass | `die`, usage on exit 2; `verify-database.sh` and `check-tailnet-exposure.sh` exit 1 on a failed check |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup; no fixed `/tmp` names | N-A | No temporary file is written; the secrets go to their permanent folder |
| 7 | No secret is written in the script, echoed, or put on a command line; secrets come from the environment or a gitignored file | Pass | Passwords made by `openssl rand`, kept 0600, sent to `psql` on standard input with `log_statement` off; the settings files are written by the shell builtin `echo`, not a process argument; verified in the test |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag, and says so in its header | Pass | `prepare-database-roles.sh` is a dry run without `--apply`, and needs separate flags for the transfer and the network; the two check scripts are read-only (`default_transaction_read_only=on`) |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | All three new scripts; the two new settings of `backup-database.sh` are documented |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | Each cites ADR-0011 and its MIL-024 task |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | 34 checks in the end-to-end test: dry run, failing verify before, apply, idempotent rerun, privileges, refusals, migrator, API, backup, restore; the exposure check run against `nf-hotel` (fails on 5432 as expected) |
| 12 | Formatted with `shfmt` (or the project's formatter) | Pass | shfmt 3.14.1 `-i 2 -ci`: no diff |
| 13 | Safe to re-run: a second run does not duplicate or corrupt what the first did | Pass | The second run keeps the passwords (same hashes) and passes; SQL is idempotent |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | `Needs:` lines |
| 15 | A command sequence, constant, path or message used more than once is defined once (a function, a variable or a sourced helper); a helper is shared between scripts only where it expresses the same knowledge | Pass | `in_db`, `query`, `check`, `expect`; `die` and `usage` from `scripts/lib/common.sh` |

## Overall Verdict

Go-with-conditions — every Mandatory criterion of QC-SH-001 passes, and Go/No-Go criteria 1 to 6 and 8 of MIL-024 pass in the tests. Criterion 7 (the run on `nf-hotel` and the tailnet restriction of 5432) is open, because it needs S03.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run the steps of the wiki page Database Preparation on `nf-hotel`, record the output of `verify-database.sh` and `check-tailnet-exposure.sh` (closes criterion 7) | S03 | MIL-024 task 7 |
| Restrict `tcp:5432` to `group:nfhotel-admins` in the tailnet policy (R15) | S03 | At once |
| Propose SQL and nginx checklists to the framework | S01 | MIL-024 task 8 |
| Independent review of the scripts on the pull request | Tirsvad (S01) | Before the merge |

---

[MIL-024]: ../../milestones/mil-024-postgresql-api-readiness.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[891129b]: https://github.com/NF-Hotel/NFHotel/commit/891129bc88431c9ff29b0a221a2a82833959a275
