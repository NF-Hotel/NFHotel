# RC-506 MIL-024 run on nf-hotel and the check of its criteria

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-506 |
| CrossReference | [MIL-024], [ADR-0011], [RC-505] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S03 | Initial version | [7521bb5] |

---

## Artifact Under Review

- **Instance:** the run of the wiki page *Database Preparation* (steps 2 to 8) on `nf-hotel`, as root, with the checkout `/opt/nfhotel` of this repository at `24d979e`. Container `nfhotel-postgres-postgres-1` (PostgreSQL 16.15, database `nfhotel`).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S03`). S01 approved the production change in chat on 2026-10-20 ("DB roles (MIL-024)").

## The run

| Step | Result |
| --- | --- |
| 2 Baseline | `verify-database.sh`: 9 role checks fail as expected; 6 migrations, `staff_member` 1 row, the other tables 0 |
| 3 Backup first | `nfhotel-daily-20261020T120745Z.dump.age` (14:07 CEST) |
| 4 Dry run | No roles present; owner of `public` is the image superuser `nfhotel` |
| 5 Roles | `nfhotel_owner`, `nfhotel_migrator`, `nfhotel_app`, `nfhotel_backup` created; passwords and settings files in `/etc/nfhotel/secrets/` (0600, never printed) |
| 6 Network | Nothing to do: the container is on `nfhotel-db` with the alias `postgres` since the reconciliation |
| 7 Ownership | The script took its own backup (`…120810Z`), then one transaction; the migration history is unchanged (6 rows) |
| 7 Verify | `verify-database.sh --expect-transfer --expect-network`: **all 15 checks pass** |
| 8 Backup role | `BACKUP_ROLE=nfhotel_backup` in `/etc/nfhotel/backup.env`. The next backup (`…120821Z`) restored in a drill: 10 tables, 6 migrations, the same rows as production |

Nothing was restarted, no row changed, and no second database was created.

## Checklist Results (MIL-024 Go/No-Go)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `nfhotel_app` reads and writes rows, and is refused DDL, `TRUNCATE` and other databases | Pass | `tests/test-database.sh` in CI (job "PostgreSQL preparation (real migrations)"), and in production the checks "may read and write every table", "may not truncate", "may not create" |
| 2 | The idempotent script as `nfhotel_migrator` makes objects owned by `nfhotel_owner` and usable by `nfhotel_app` | Pass | `tests/test-database.sh` in CI, with the real migrations of NFHotel |
| 3 | The ownership transfer leaves rows and the migration history unchanged | Pass | In CI, and in production (6 migrations and the same row counts before and after) |
| 4 | The API image answers `/health` 200 as `nfhotel_app` through `postgres` on the internal network | Pass | `tests/test-database.sh` in CI. Production still connects as the superuser; see the findings |
| 5 | A dump made as `nfhotel_backup` restores with the constraints and the migration history | Pass | The drill of `…120821Z` above (`pg_restore --exit-on-error`) |
| 6 | No secret in git or in output; passwords on standard input | Pass | The run printed file names only; the secrets stay in `/etc/nfhotel/secrets/` |
| 7 | `verify-database.sh` passes on `nf-hotel`, and 5432 answers on the tailnet only to `group:nfhotel-admins` | Pass | 15 of 15 checks. 5432 is not published at all since the reconciliation (MIL-021 #141), which is stricter: no tailnet device reaches it, and administrators use Tailscale SSH and `docker exec` |
| 8 | Reviews against `qc-programming-shell`, `qc-yaml` and `qc-adr` are `Go` | Pass | RC-099 (MIL-024), RC-100 (ADR-0011) and RC-101 (the code and the gate) |

## Findings

- **The API still connects as the superuser.** The compose file of the API builds its connection string from the `POSTGRES_*` values, and the API applies migrations at start. Moving it to `nfhotel_app` (with `/etc/nfhotel/secrets/nfhotel-api-database.env` and `Database__MigrateOnStart=false`) has to come together with migrations by CD (MIL-022 task 6). Otherwise the next migration fails. This is outside MIL-024, whose scope is the preparation, and it is recorded on MIL-022 #132.
- The off-VM copy of the backups is open (MIL-021 #124, RC-505).

## Overall Verdict

Go: every criterion of MIL-024 passes.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Switch the API to `nfhotel_app` together with migrations by CD | S01 | MIL-022 task 6 |

---

[MIL-024]: ../../milestones/mil-024-postgresql-api-readiness.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[RC-505]: ./rc-505-restore-drill-2026-10-20.md
[7521bb5]: https://github.com/NF-Hotel/DevOps/commit/7521bb57c9970b6abdbd8e8c1675f67621598b26
