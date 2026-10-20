# ADR-0011 PostgreSQL access, network and routing for the API on the production VM

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0011 |
| CrossReference | [ADR-0004], [ADR-0006], [ADR-0007], [ADR-0008], [ADR-0010], [MIL-024], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version | [af6710f] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-100 Go | [af6710f] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The ASP.NET Core API of ADR-0004 (`src/NFHotel.Api`, image `nfhotel-api`) exists in the repository, but it is not deployed on the production VM `nf-hotel` (ADR-0010). Today that VM runs one PostgreSQL 16 instance from a compose file outside git, and that instance answers on port 5432 to every tailnet device, without TLS (DSA-001 section 2.4.2, risk R15). The API needs the following before it can be deployed there:

- a network path to that database by service name;
- database roles with less power than the superuser of the image;
- secrets that never enter git;
- a decision on whether and how nginx routes to it.

What the API needs from the database, from its code and its migrations:

- **Connection.** The connection string `ConnectionStrings:HotelDatabase` (Npgsql), and `/health` for the health check.
- **Migrations** create the trusted extension `btree_gist`, four tables, an `EXCLUDE USING gist` constraint and `__EFMigrationsHistory`, all in the schema `public`.
- **At run time** it only reads and writes rows; it never changes the schema when `Database__MigrateOnStart=false` (ADR-0008).

Constraints: the existing data, volumes, users, schemas and migration history must be kept. There must be no second instance. Nothing that runs may be interrupted without a tested way back.

Options considered:

- **Roles:**
  1. the API keeps using the superuser of the image;
  2. one application role that owns the schema;
  3. separate owner, migration, runtime and backup roles.
- **Network:**
  1. the API reaches the database through the published port;
  2. an internal Docker network with service discovery, joined to the running container without a restart;
  3. a second database next to the first.
- **Routing:**
  1. a public route to the API;
  2. no route, as ADR-0004 decided;
  3. a route reachable only from the tailnet, off until the API runs.

## Decision

We use separate least-privilege roles on the one existing instance, an internal Docker network that the running PostgreSQL container joins without a restart, and an optional tailnet-only nginx route that stays off until the API runs.

1. **One instance.** The running PostgreSQL 16 container on `nf-hotel` is the only production database. No second instance and no second database are created.
2. **Roles** (`scripts/db/roles.sql`, idempotent):
   - `nfhotel_owner`, `NOLOGIN`: owns the schema objects of the application.
   - `nfhotel_migrator`, `LOGIN`: a member of `nfhotel_owner` (`SET` only, no `INHERIT`). It connects with `Options=-c role=nfhotel_owner`, so every object it creates is owned by `nfhotel_owner`. Used only by the migration step of CD (ADR-0008).
   - `nfhotel_app`, `LOGIN`: `CONNECT`, `USAGE` on `public`, `SELECT, INSERT, UPDATE, DELETE` on the tables and `USAGE, SELECT` on the sequences. `ALTER DEFAULT PRIVILEGES FOR ROLE nfhotel_owner` gives the same on future objects. No `CREATE`, `TRUNCATE`, `REFERENCES` or `TRIGGER`.
   - `nfhotel_backup`, `LOGIN`: a member of `pg_read_all_data`, for `pg_dump` only.
   - The superuser of the image stays as break-glass for S03. No application uses it.
   - `PUBLIC` loses `CREATE` and `TEMPORARY` on the database.
   - The login roles get connection limits (app 20, migrator 2, backup 2) and `SCRAM-SHA-256` passwords.
3. **Existing objects.** `scripts/db/transfer-ownership.sql` moves the ownership of every existing table, sequence and view in `public`, and of the schema, to `nfhotel_owner` with `ALTER ... OWNER TO`. That changes catalog entries only: no row and no migration history row is touched. It runs after a verified backup, in a maintenance window, and it is tested on a copy first.
4. **Secrets.** `scripts/prepare-database-roles.sh` creates the passwords on the VM (`openssl rand`) and keeps them in `/etc/nfhotel/secrets/` (root, 0600).
   - It writes the connection strings into git-ignored settings files that the compose file of the API reads.
   - It passes passwords to `psql` only on standard input, with `log_statement` turned off for that session, so they never reach a command line, the server log or git.
5. **Network.**
   - The internal network `nfhotel-db` (`--internal`, no route out) is created additively.
   - The running PostgreSQL container joins it with the alias `postgres` (`docker network connect`, no restart). The API reaches `Host=postgres;Port=5432`.
   - The API is on `nfhotel-db` and `nfhotel-api`; the web application only on `nfhotel-api` (ADR-0004 unchanged).
6. **Tailnet access to 5432.**
   - The published port is not removed now: that recreates the container and belongs to the reconciliation of ADR-0010.
   - The tailnet policy limits `tcp:5432` on `tag:nfhotel-vm` to `group:nfhotel-admins` at once.
   - The reconciliation then publishes 5432 on `127.0.0.1` only; administrators reach it through Tailscale SSH and a tunnel.
7. **nginx.**
   - The API is not published to the internet (ADR-0004).
   - `deploy/nginx/optional/api.conf.template` defines `/api/` on the tailnet name only (`allow 100.64.0.0/10; allow fd7a:115c:a1e0::/48; deny all`). It uses a variable upstream and Docker's resolver, so nginx starts even when the API does not run.
   - It is outside `templates/` and is not loaded until the API is deployed and it is copied in by a reviewed change.
   - The existing routes are not changed.
8. **CI/CD.**
   - The API image is already built and pushed by NF Hotel Release (ADR-0007).
   - CD deploys it after the reconciliation. The runtime connection string uses `nfhotel_app`; the migration step uses `nfhotel_migrator`.
9. **Verification.** `scripts/verify-database.sh` checks, read-only, on the VM:
   - roles, privileges and the owners of the objects;
   - the migration history and the row counts;
   - the internal network and the backup.

   S03 runs it before and after each step.

## Consequences

**Positive:**

- The API never holds a role that can drop the schema or read other databases.
- A leaked runtime password cannot change the schema.
- The backup cannot write.
- Preparing the network and the roles does not restart PostgreSQL or change any route that serves today.
- The design is tested against the real migrations of the repository.

**Negative:**

- Four roles and their passwords to maintain.
- The ownership transfer and the removal of the published port need a maintenance window.
- Until the tailnet policy is changed, R15 stays open: changing the policy is an action for S03 in the Tailscale admin console.
- The internal Docker network carries the database traffic without TLS. That is acceptable only because the network is internal to one VM.

**Quality:** security (least privilege, confidentiality) and maintainability improve, with no effect on performance (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0004]: the network and the API exposure are kept; the roles refine "the only service with the database login".
- [ADR-0006]: TLS for the optional route uses the existing tailnet certificate.
- [ADR-0007]: CD deploys the API with these settings after the reconciliation.
- [ADR-0008]: the migration step uses `nfhotel_migrator`; the backup may use `nfhotel_backup`.
- [ADR-0010]: the published port is removed in the reconciliation.
- [MIL-024]: implements this decision.

---

[ADR-0004]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0004-database-api.md
[ADR-0006]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0006-tls.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ./adr-0008-database-migration-and-backup.md
[ADR-0010]: ./adr-0010-production-vm-as-deployed.md
[MIL-024]: ../milestones/mil-024-postgresql-api-readiness.md
[DSA-001]: ../sqa/devsecops-assessment.md
[af6710f]: https://github.com/NF-Hotel/NFHotel/commit/af6710fce369f8d63795d67efe4f7a54a867997a
