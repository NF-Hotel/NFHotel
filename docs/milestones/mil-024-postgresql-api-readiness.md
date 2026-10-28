# MIL-024 PostgreSQL and tailnet readiness for the API

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-024 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0010], [ADR-0011], [MIL-021], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version | [af6710f] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-099 Go | [af6710f] |

---

## Purpose

Decide whether the production database on `nf-hotel` is ready for the ASP.NET Core API of ADR-0004 to be deployed. The API code is not needed for this; only what it needs from the database and the network is. The work prepares, without interrupting anything that runs:

- least-privilege roles;
- an internal network with service discovery;
- secrets outside git;
- a closed tailnet path to PostgreSQL;
- an optional tailnet-only route;
- a backup and restore proven against the real schema.

## Deliverable

- ADR-0011, reviewed.
- `scripts/db/roles.sql` and `scripts/db/transfer-ownership.sql` (idempotent), and `scripts/prepare-database-roles.sh`:
  - a dry run by default;
  - `--apply` creates the roles and the secrets;
  - `--transfer-ownership` moves the existing objects;
  - `--connect-network` joins the running container to `nfhotel-db` as `postgres`.
- `scripts/verify-database.sh`: read-only checks of the roles, privileges, owners, migration history, row counts, network and backup.
- `deploy/nginx/optional/api.conf.template`: tailnet-only, off by default.
- Backup support for the running container (`BACKUP_CONTAINER`, `BACKUP_ROLE`).
- Tests against PostgreSQL 16 with the real migrations of the repository:
  - the privileges of each role;
  - the ownership transfer with unchanged rows and migration history;
  - a migration run as `nfhotel_migrator`;
  - the API image reaching the database by the name `postgres`;
  - backup and restore.
- The wiki page CI-CD: the database preparation, in the order S03 runs it, with the checks before and after.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | In the test, `nfhotel_app` can select, insert, update and delete rows of every table, and is refused `CREATE`, `DROP`, `ALTER`, `TRUNCATE` and access to another database | Yes | No |
| 2 | In the test, the idempotent migration script run as `nfhotel_migrator` succeeds, and the objects it creates are owned by `nfhotel_owner` and usable by `nfhotel_app` without another grant | Yes | No |
| 3 | In the test, the ownership transfer leaves the row count of every table and the rows of `__EFMigrationsHistory` unchanged | Yes | No |
| 4 | In the test, the API image answers `/health` 200 and reads the database through the alias `postgres` on the internal network, as `nfhotel_app` | Yes | No |
| 5 | A dump made as `nfhotel_backup` restores into a throwaway PostgreSQL 16 with the `EXCLUDE` constraint and the migration history | Yes | No |
| 6 | No password, private key or production connection string is in git or in a script's output; the scripts pass passwords on standard input only | Yes | No |
| 7 | On `nf-hotel`, `scripts/verify-database.sh` passes after S03 has run the preparation, and port 5432 answers from the tailnet only to `group:nfhotel-admins` | Yes | No |
| 8 | Reviews against `qc-programming-shell`, `qc-yaml` and `qc-adr` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0010, MIL-021 task 10 | The published port is removed, and the API is deployed by CD, only after the reconciliation |
| MIL-021 | The Release workflow that builds the API image; the backup scripts |
| S03 | Runs the preparation on the VM and changes the tailnet policy; the assessor has no access to the VM |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 18 of BC-001: the database is reachable only through an API that is not exposed to the internet | All tasks |
| Objective 23 of BC-001: an approved pipeline, and data that can be restored | Tasks 5 and 6 |
| DSA-001 risks R7, R15 | Tasks 2, 4, 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for everything run on the VM and the tailnet policy |

## Target Date

2026-11-08, three weeks. The repository work is done in this milestone. The VM steps wait for S03, and the removal of the published port waits for MIL-021 task 10.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Record the decision in ADR-0011 | Roles, network, secrets, tailnet access, nginx route, CI/CD; reviewed. | No | |
| 2 | Role and ownership scripts | `roles.sql`, `transfer-ownership.sql`, `prepare-database-roles.sh` with a dry run by default. | No | |
| 3 | Verification script | `verify-database.sh`, read-only, for before and after each step on the VM. | No | |
| 4 | Network and nginx readiness | The additive network join; the optional tailnet-only route, off by default. | No | |
| 5 | Backup of the running container | `BACKUP_CONTAINER` and `BACKUP_ROLE` in the backup script; restore drill against the real schema. | No | |
| 6 | Tests against PostgreSQL 16 | Criteria 1 to 6 in containers, with the real migrations and the API image. | No | |
| 7 | Run on the VM | S03 runs the preparation and the checks on `nf-hotel` and restricts 5432 in the tailnet policy; record the output. | No | |
| 8 | Check and review | Linters; reviews against `qc-programming-shell`, `qc-yaml`, `qc-adr`; the wiki page. | No | |

---

[BC-001]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/business-case.md
[ADR-0004]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0004-database-api.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[DSA-001]: ../sqa/devsecops-assessment.md
[af6710f]: https://github.com/NF-Hotel/NFHotel/commit/af6710fce369f8d63795d67efe4f7a54a867997a
