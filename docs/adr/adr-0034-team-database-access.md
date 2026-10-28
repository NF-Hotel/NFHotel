# ADR-0034 Personal database logins for the team over the tailnet

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0034 |
| CrossReference | [MIL-048], [ADR-0011], [ADR-0024], [CIS-001], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-28 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-10-28 for one database login per team member, approved through Tailscale ("lav et username til hver af dem og de er godkendt via tailscale"), with read and write access; written by the AI under MIL-048, which S01 accepted in chat on 2026-10-28; accepted after RC-246 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** On 2026-10-28, S01 asked that the team can open the production database from DBeaver. S01 chose:

- one login per person;
- read and write access to the rows;
- a personal login for S01 too.

S01 named the people by their Tailscale logins. The AI assistant wrote the options and the design.

**Today.** PostgreSQL 16 runs on `nf-hotel` on the internal Docker network `nfhotel-db` (ADR-0011):

- it has no published port and no route out;
- the API logs in as `nfhotel_app`, and the superuser is `nfhotel`;
- the host firewall opens 80 and 443 to every interface, and 22 on `tailscale0` only (MIL-023, CIS-001).

S01 reaches the database with an SSH tunnel through Tailscale SSH to the container's address. The database holds personal data of guests (GDPR).

**Options considered:**

1. **Share `nfhotel_app` or the superuser.**
   - Nobody can tell who changed what.
   - A rotation reaches every person and the API at once.
   - The superuser can also drop the database.
2. **An SSH tunnel per person through Tailscale SSH**, with one Linux account each and no shell.
   - Tailscale SSH cannot limit where a forwarded port goes, so each person could reach every address the VM reaches, the home network and the Proxmox node included.
   - Each person also needs a Linux account on the production VM.
3. **A TCP relay on the tailnet address only**, with a personal database role per person.
   - The relay listens on `100.72.194.127:5432` and forwards to `postgres:5432` on `nfhotel-db`.
   - The firewall lets 5432 in from `tailscale0` only.
   - The Tailscale policy decides which people may reach it, and the database decides what each role may do.
   - Nobody needs an account on the VM, and the internet never sees the port.

## Decision

We follow option 3.

1. **Roles.** The group role `nfhotel_team` has the following rights:
   - It is `NOLOGIN`.
   - It gets `CONNECT` on the database `nfhotel`, `USAGE` on schema `public`, `SELECT, INSERT, UPDATE, DELETE` on its tables, and `USAGE, SELECT` on its sequences.
   - Default privileges for `nfhotel_owner` give it the same rights on the tables the migrations add later.
   - It gets no `CREATE` and no `TRUNCATE`. The schema stays with the migrations (ADR-0011).
2. **One login role per person,** `dev_<name>`, member of `nfhotel_team`. Each role is `LOGIN`, not a superuser, and has:
   - `CONNECTION LIMIT 3`;
   - `log_statement = 'mod'`, so every change is logged. The server's `log_line_prefix` gains the role and the database (`%u@%d`), so each line says who made the change;
   - `idle_in_transaction_session_timeout = '10min'` and `lock_timeout = '5s'`, so an open transaction in DBeaver cannot hold up the API for long.

   The first logins:

   | Role | Tailscale login |
   | --- | --- |
   | `dev_jakob` | `ZanjiOfficial@github` |
   | `dev_william` | `lordwhitboy@gmail.com` |
   | `dev_foxxy` | `outlashed@gmail.com` |
   | `dev_mike` | `syneuro@gmail.com` |
3. **Passwords.**
   - `ops/scripts/manage-team-db-users.sh` makes a random password with `openssl` for a new role.
   - It writes the password to `/etc/nfhotel/secrets/team/<role>.password` (root, 0600) and passes it to `psql` on standard input only.
   - S01 reads the file and gives the password to the person in private. The AI never sees it.
   - Nobody changes their own password with SQL. `log_statement = 'mod'` logs `ALTER ROLE` too, so the new password would end up in the log. S01 makes a new one with `--reset <name>`.
   - `--remove <name>` drops the role and its file when someone leaves.
4. **Relay.**
   - A stack `ops/deploy/postgres-tailnet` runs one HAProxy container in TCP mode. The image is pinned by digest and hardened like the other services.
   - It joins `nfhotel-db` and its own bridge network, and publishes `100.72.194.127:5432` only.
   - It finds `postgres` through Docker's DNS, so a new database container address does not break it.
   - The CD deploys it with the other services.
5. **Firewall.** The `docker-user` chain of `nfhotel.nft` accepts DNAT to 5432 only when the packet comes in on `tailscale0`, so the LAN and the internet cannot reach it, even if an address changes.
6. **Tailscale policy (S01).** A grant gives the four Tailscale logins access to `nf-hotel` on `tcp:5432`. Changing the policy is S01's action, because the AI cannot reach the tailnet admin console. Until the grant is in place, every tailnet device can reach the port, and the database password is still required.

## Consequences

**Positive:**

- Each change in the database has a name: the role is in every log line, and Loki keeps the logs 14 days (MIL-023).
- Removing one person is one command, with no rotation for the others or for the API.
- Nobody gets an account on the VM, and nothing opens to the LAN or the internet.

**Negative:**

- Four people can change personal data of guests. `log_statement = 'mod'` writes the changed values into the log, so the log in Loki holds personal data for 14 days. That is the same retention as the API logs, and DSA-001 records it.
- A wrong `UPDATE` or `DELETE` is restored from the nightly backup (MIL-023 task 6). Rows changed since the backup are lost.
- One more container and one more open port on the tailnet. MIL-023 criterion 2 still holds, because the port is not open from outside.

**Quality:** security (accountability, confidentiality) and maintainability (one command per person) improve over a shared login (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-048]: tasks 1 to 6.
- [CIS-001]: the firewall rows (4.3.x), which gain the 5432 rule on `tailscale0`.
- [DSA-001]: the access to personal data and the log retention.

---

[MIL-048]: ../milestones/mil-048-team-database-access.md
[ADR-0011]: ./adr-0011-postgresql-access-for-the-api.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
