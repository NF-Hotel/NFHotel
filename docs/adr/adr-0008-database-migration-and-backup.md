# ADR-0008 PostgreSQL migrations and backups

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0008 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0007], [MIL-021], [MIL-022], [ADR-0011], [ADR-0018], [RC-161], [RC-166] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Deprecated | mikemike | AI as stand-in for S01 | Records what runs and what is not yet done (RC-161): the copy off the VM (D7) is not in place | [12a64c3] |
| 2026-10-22 | Accepted | mikemike | AI as stand-in for S01 | The copy off the VM is in place (legion, D7 revised); restore drill from the copy (RC-166) | [0aac8e5] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The database runs in the container `postgres` (PostgreSQL 16) with the named volume `pgdata` on the VM (ADR-0004). It holds guests, bookings and encrypted passport numbers.

There is no backup and no restore procedure (DSA-001 section 2.5). The API applies Entity Framework Core (EF Core) migrations itself when it starts, if `Database__MigrateOnStart=true`. A failed migration, a lost disk or a mistaken `docker volume rm` would therefore lose all data, and there is no defined way back.

Options considered for backups: a Proxmox VM backup only; `pg_dump` on a timer; continuous write-ahead log (WAL) archiving (point-in-time recovery). Options for migrations: at application start (as today); a migration bundle run by CD; reviewed idempotent SQL scripts run by CD.

## Decision

We take daily logical backups with a tested restore, and CD applies migrations as reviewed scripts in an expand-then-contract order.

1. **Logical backups.**
   - A systemd timer on the VM runs `pg_dump --format=custom` every day at 02:00.
   - Each dump is encrypted with `age` to a public key whose private key S03 keeps offline, then copied off the VM to a target that S03 chooses (decision D7 in DSA-001).
   - Retention is 14 daily and 8 weekly copies.
2. **Snapshot before risky releases.** Before a release that contains a migration, CD triggers a fresh dump. S03 also takes a Proxmox snapshot of the VM.
3. **Restore drill.**
   - Monthly, and before a major migration, the latest dump is restored into a throwaway PostgreSQL container.
   - The schema version and the row counts of the main tables are compared, and the API health check is run against it.
   - The result is recorded in an `RC-*` record.
4. **Migrations by CD, not at start.**
   - The Release workflow produces an idempotent SQL script (`dotnet ef migrations script --idempotent`), which is reviewed in the pull request and attached to the release.
   - CD applies it after the backup and before the new API starts.
   - `Database__MigrateOnStart` is `false` in production.
5. **Expand, then contract.** A migration only adds things or changes them compatibly, so the previous API still runs on the new schema. Removing columns or tables happens in a later release. A rollback of the images therefore never needs a rollback of the schema.
6. **Failed migration.**
   - CD stops and does not start the new API.
   - If the schema is still compatible, the previous release keeps running.
   - Otherwise S03 restores the pre-deploy dump or the VM snapshot, and the decision is recorded.
7. **Access.** The API login is not a PostgreSQL superuser. The backup uses a role with only the rights `pg_dump` needs.

WAL archiving is not chosen now: a daily dump with a tested restore fits the data volume and the team's size. It is reviewed when the data grows.

**Current state (2026-10-22, [RC-161], [RC-166]).** In place on `nf-hotel`: the timer `nfhotel-backup.timer` runs every day at 02:00 UTC; each dump is encrypted with `age`, and 14 dumps are on the VM. A dump runs before every migration and every deploy. CD applies the idempotent script as `nfhotel_migrator` ([ADR-0011]), and production runs with `Database__MigrateOnStart=false`. Since [ADR-0018], CD installs the backup command and its timer. **The copy off the VM is in place since 2026-10-22.** DSA-001 decision D7, revised by S01, sends it to the write-only target `ops/deploy/backup-target` on the legion laptop of S03, outside the Proxmox host. Every backup sends what the target lacks, and a laptop that is off gives a warning and a catch-up at the next run. The restore drill has run twice: on 2026-10-20 from the VM (RC-505), and on 2026-10-22 from the copy on the laptop (RC-166): 11 tables, 156 686 bookings, the latest migration `AddStaffPasswordAndAudit`. The monthly drill is not yet scheduled; that is planned for MIL-033.

## Consequences

**Positive:**

- Data can be recovered to the last daily dump, and a restore is proven every month.
- Migrations are reviewed before they run, and image rollbacks stay safe.

**Negative:**

- Up to one day of bookings can be lost in a disaster (recovery point objective, RPO, of 24 hours). Restore time is estimated at under one hour for the current size (recovery time objective, RTO), to be measured in the first drill.
- Expand-then-contract needs discipline in reviews.
- The decryption key must be kept safely; if it is lost, the backups are lost.

**Quality:** reliability (recoverability) and security improve (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-021]: phase 1 adds the backup timer, the off-VM copy and the first restore drill.
- [MIL-022]: phase 2 moves migrations from the start of the API into CD.
- [ADR-0004]: the database and its network are unchanged.

---

[BC-001]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/business-case.md
[ADR-0004]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0004-database-api.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[ADR-0011]: ./adr-0011-postgresql-access-for-the-api.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
[12a64c3]: https://github.com/NF-Hotel/NFHotel/commit/12a64c3225c75ebc1b83a0e6d0ee08115ec1a493
[RC-166]: ../sqa/reviews/rc-166-mil-021-gate.md
[0aac8e5]: https://github.com/NF-Hotel/NFHotel/commit/0aac8e50d72fd71b38b97ce6b19cefbcecec6ce0
