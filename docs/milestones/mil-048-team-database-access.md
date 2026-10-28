# MIL-048 Team access to the database from DBeaver

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-048 |
| CrossReference | [BC-001], [ADR-0011], [ADR-0034], [MIL-023], [CIS-001], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-28 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-28 for a database login per team member, approved through Tailscale, with read and write access, and accepted the plan ("Accepter planen"); accepted after RC-245 Go | pending |

---

## Purpose

Decide whether the team (S01) can open the production database from DBeaver. Each person gets their own login, and the port can be reached only from the tailnet. Today the only way in is the superuser, or the API's own login through an SSH tunnel. ADR-0034 gives the design.

## Deliverable

- `ops/scripts/db/team-roles.sql` and `ops/scripts/manage-team-db-users.sh`:
  - the group role `nfhotel_team`;
  - one `dev_<name>` role per person;
  - each password in a root-only file, never printed.
- The stack `ops/deploy/postgres-tailnet`: an HAProxy relay that publishes `100.72.194.127:5432` only. The CD deploys it.
- The rule for 5432 on `tailscale0` in `ops/deploy/host/nftables/nfhotel.nft`.
- The roles `dev_jakob`, `dev_william`, `dev_foxxy` and `dev_mike` on `nf-hotel`.
- The wiki page Database-Access: DBeaver settings, how to change your own password, and what the log keeps.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The four `dev_*` roles exist on `nf-hotel`, `LOGIN`, not superuser, member of `nfhotel_team`; as one of them, `SELECT`, `INSERT`, `UPDATE` and `DELETE` work and `CREATE TABLE`, `TRUNCATE` and `DROP` are refused (test in CI on PostgreSQL 16, and on the VM) | Yes | No |
| 2 | Each of the four people connects from DBeaver to `100.72.194.127:5432`, database `nfhotel`, with their own role | Yes | No |
| 3 | Port 5432 does not answer on the VM's LAN address or from the internet; MIL-023 criterion 2 (only 80 and 443 from outside) still holds | Yes | No |
| 4 | No password on a command line, in git, in a log or in the chat; the script passes them to `psql` on standard input only | Yes | No |
| 5 | A write by a `dev_*` role is found in Loki with the role's name | Yes | No |
| 6 | Ops CI is green, the reviews against `qc-programming-shell`, `qc-yaml` and `qc-workflow` are Go, and the deploy after the merge succeeds | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0011 | The roles, the owner of the schema and the internal network `nfhotel-db` |
| MIL-023 | The host firewall, the 14-day log retention and the nightly backup |
| S01 | Adds the Tailscale grant for `tcp:5432` and hands each person their password (tasks 4 and 6) |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001: the team develops and supports NF Hotel on its own server | Tasks 1 to 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-04.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Team roles and script | `ops/scripts/db/team-roles.sql` (group role `nfhotel_team`, grants and default privileges as in ADR-0034) and `ops/scripts/manage-team-db-users.sh` (`--add <name>`, `--remove <name>`, `--list`; dry run without `--apply`; the password from `openssl` to `/etc/nfhotel/secrets/team/<role>.password`, 0600, passed on standard input). Tests in `ops/tests/test-team-db-users.sh` against PostgreSQL 16 in CI. | No | criteria 1, 4 |
| 2 | Relay on the tailnet | `ops/deploy/postgres-tailnet/docker-compose.yml` and `haproxy.cfg`: HAProxy in TCP mode, pinned by digest, `read_only`, `cap_drop: [ALL]`, `no-new-privileges`, PID and memory limits, on `nfhotel-db` and its own network, published on `${NFHOTEL_TAILNET_IP}:5432`; added to the services of `deploy-services.sh` and to its tests. | No | criteria 2, 3 |
| 3 | Firewall | In `nfhotel.nft`, chain `docker-user`: accept DNAT to 5432 only from `tailscale0`; a test in `test-host-setup.sh`; applied with `apply-firewall.sh` (automatic revert unless confirmed). | No | criterion 3 |
| 4 | Tailscale grant (S01) | S01 adds to the tailnet policy a grant from the four Tailscale logins to `nf-hotel` on `tcp:5432`; the wiki page gives the exact text. | No | criteria 2, 3 |
| 5 | Documents | ADR-0034; the wiki page Database-Access and a line in Proxmox-Deployment; CIS-001 rows 4.3.x; DSA-001 (personal data in the log); reviews. | No | criterion 6 |
| 6 | Release and check | Merge when green and deploy; S01 runs `manage-team-db-users.sh --add` for the four people and gives each their password; each person connects from DBeaver; port scan of the LAN address and from outside; a test write found in Loki; gate review. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0034]: ../adr/adr-0034-team-database-access.md
[MIL-023]: ./mil-023-operational-quality.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
