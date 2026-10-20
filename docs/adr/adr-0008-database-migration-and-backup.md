# ADR-0008 PostgreSQL migrations and backups

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0008 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0007], [MIL-021], [MIL-022] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version | [f4649ad] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-093 Go | [f4649ad] |

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
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
