# RC-292 Re-review of the gate of MIL-048 Team access to the database from DBeaver

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-292 |
| CrossReference | [MIL-048], [ADR-0034], [RC-285], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the re-review that RC-285 asked for, with the evidence read on 2026-11-05, the answers of S01 in chat and the test of the public address from outside the home network by S01 | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-048] at its gate, the second time: criteria 1, 2, 3 and 5, which [RC-285] left Pending on 2026-11-05 at 07:11 UTC, with the evidence read on `nf-hotel` and `nf-monitoring` the same day at 09:56 to 09:59 UTC, the answers of S01 in chat and the test from outside the home network at 18:35 UTC; criteria 4 and 6 cite RC-285. Before RC-285: [RC-245] (the milestone against QC-MIL-001, Go) and [RC-247] (the code, Go-with-conditions, closed).
- Checklist used: the Go/No-Go criteria of [MIL-048]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: delta re-review of criteria 1, 2, 3 and 5; the earlier record is [RC-285] (Go-with-conditions).
- Language and domain: en / it (the Metadata rows of MIL-048); this record is a technical type.
- Language reviewer: none
- Mode: autonomous, not independent. The AI wrote the milestone, the code review, RC-285 and this record; S01 asked in chat on 2026-11-05 to finish the open milestones, accepted the plan ("Accepter planen"), answered the questions of the AI the same day and ran the test from outside. The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on 2026-11-05** (Tailscale SSH in check mode to `nf-hotel` at 09:56 UTC and to `nf-monitoring` at 09:59 UTC under the approval S01 gave at 07:09 UTC; read-only: `psql` as the database superuser inside `nfhotel-postgres-postgres-1` with the privilege functions of PostgreSQL, `docker logs` of the postgres and relay containers with the value of every statement cut before it was read, `docker inspect`, and HTTP GETs against Loki; no password was read or printed, and no tailnet device address or public address is copied here):

- **Roles.** Five `dev_*` roles (`dev_foxxy`, `dev_jakob`, `dev_jens`, `dev_mike`, `dev_william`): `rolcanlogin` true; `rolsuper`, `rolcreatedb`, `rolcreaterole` and `rolbypassrls` false; each a member of `nfhotel_team`, which cannot log in.
- **Privileges of `nfhotel_team`, and so of each role, per table of `public`.** SELECT, INSERT, UPDATE and DELETE on the 14 application tables; SELECT only on `__EFMigrationsHistory`, which `ops/scripts/db/team-roles.sql` revokes on purpose; no TRUNCATE, REFERENCES or TRIGGER on any table; no CREATE in the schema `public` and no CREATE in the database (`has_schema_privilege`, `has_database_privilege`); the 15 tables are owned by `nfhotel_owner`, of which no `dev_*` role is a member (`pg_has_role`), so DROP is impossible; USAGE on all 14 sequences; the default privileges of `nfhotel_owner` give the team `arwd` on new tables and `rU` on new sequences, so a migration does not take the access away.
- **The live test on the VM.** The container log holds, under `dev_mike@nfhotel` on 2026-10-28 at 12:17 UTC, `LOG: statement: UPDATE`, then `LOG: statement: CREATE` with `ERROR: permission denied for schema public` and `STATEMENT: CREATE TABLE t_mil048 (id int)`: a write that works and a `CREATE TABLE` that is refused, run by S01 from DBeaver.
- **Connections.** `dev_mike` (2026-10-28 12:17 UTC) and `dev_jakob` (2026-10-28 14:18 UTC: one `password authentication failed`, then a script "Tables the LoginApi expects" whose `GRANT` got `permission denied for database nfhotel` and whose `CREATE` got `schema "auth" does not exist`) are in the log. The server logs no connection (`log_connections off`) and every `dev_*` role logs writes only (`log_statement=mod`, ADR-0034), so a person who only reads leaves no line. The relay log holds 22 759 lines in 14 days from five distinct tailnet client addresses. S01 stated in chat on 2026-11-05 that all five have connected ("de er alle forbundet").
- **Loki.** On `nf-monitoring`, `GET /loki/api/v1/query_range` for `{container="nfhotel-postgres-postgres-1"} |~ "dev_(foxxy|jakob|jens|mike|william)@"` over 14 days returns 12 entries between 2026-10-28T12:17:31Z and 2026-10-28T14:18:10Z, among them `dev_mike@nfhotel LOG: statement: UPDATE`. Loki keeps 14 days, so these lines are gone around 2026-11-11; this record is their evidence afterwards.
- **LAN and tailnet.** From `nf-monitoring`, which is on the LAN and on the tailnet, nothing answers on the LAN address of the VM on 22, 80, 443, 5432, 50023 or 50024; on the tailnet address 80 and 443 answer and 22 and 5432 do not. The listener is still the tailnet address of the VM, port 5432 only, and the nft chain `docker-user` still allows 5432 from `tailscale0` only (RC-285). That 5432 is closed to the LXC on the tailnet fits the grant of task 4 (#393, closed by S01 on 2026-11-05): 5432 now reaches the devices of the team only, and the LXC is not one of them, while the PC of S01 reached it at 07:10 UTC (RC-285).
- **Router and the internet.** S01 stated in chat on 2026-11-05 that the router forwards 50023 and 50024 are removed ("fjernet"). At 18:35 UTC the same day S01, on a laptop on another network than the home's (neither the LAN nor the tailnet path), tested the public IPv4 address of the home network, read on the VM with `curl -4 https://icanhazip.com` and not copied here, with `Test-NetConnection` of PowerShell 7.6.9 on the ports 22, 80, 443, 5432, 50023 and 50024: `TcpTestSucceeded` False for all six. Two earlier runs had tested a placeholder instead of the address and were discarded; the result pasted in chat shows the address and the six lines. The public site is reached through the Cloudflare tunnel (ADR-0028), so no port of the home network needs to be open.
- **CI.** The job `Ops team database roles (MIL-048)` of the Ops CI run 42001404333 on `main` at `a35fb3f` passed on 2026-11-05 (`ops/tests/test-team-db-users.sh` against PostgreSQL 16).

## Checklist Results

| # | Criterion (MIL-048) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The four `dev_*` roles exist on `nf-hotel`, `LOGIN`, not superuser, member of `nfhotel_team`; as one of them, `SELECT`, `INSERT`, `UPDATE` and `DELETE` work and `CREATE TABLE`, `TRUNCATE` and `DROP` are refused (test in CI on PostgreSQL 16, and on the VM) | Pass | The roles and the privileges per table as read on the VM; the live test of S01 as `dev_mike` on the VM (UPDATE ok, `CREATE TABLE` refused) in the container log of 2026-10-28; TRUNCATE is not granted and DROP is impossible without ownership; in CI, the green job of 2026-11-05. Five roles, not four: as RC-285 decided, the five are the set |
| 2 | Each of the four people connects from DBeaver to the tailnet address of the VM, port 5432, database `nfhotel`, with their own role | Pass | S01's statement of 2026-11-05 for all five; the log proves `dev_mike` and `dev_jakob`; a read-only session is not logged by design, so the owner's statement is the evidence for the other three, with the five distinct client addresses of the relay as the sign that fits it |
| 3 | Port 5432 does not answer on the VM's LAN address or from the internet; MIL-023 criterion 2 (only 80 and 443 from outside) still holds | Pass | LAN side: refused from the PC of S01 (RC-285) and from `nf-monitoring` on 2026-11-05; the listener is the tailnet address only and the nft rule allows 5432 from `tailscale0` only. Internet side: the router forwards 50023 and 50024 are removed (S01, 2026-11-05), and none of the six ports answers on the public address from outside (S01's test of 18:35 UTC, above). The six closed ports are also the evidence of MIL-023 criterion 2 for its gate |
| 4 | No password on a command line, in git, in a log or in the chat; the script passes them to `psql` on standard input only | Pass | RC-285; this review read and printed no password, cut every statement value before it was read, and copied no device or public address |
| 5 | A write by a `dev_*` role is found in Loki with the role's name | Pass | The 12 entries in Loki of 2026-10-28, among them `dev_mike@nfhotel LOG: statement: UPDATE` at 12:17:31 UTC, read on 2026-11-05 |
| 6 | Ops CI is green, the reviews against `qc-programming-shell`, `qc-yaml` and `qc-workflow` are Go, and the deploy after the merge succeeds | Pass | RC-285; pull request #498 of RC-285 merged green on 2026-11-05 (`a35fb3f`); the relay has run since 2026-10-29 |

## Findings

- **The evidence of criterion 5 expires.** Loki keeps 14 days; the lines of 2026-10-28 are gone around 2026-11-11. This record quotes what is needed (the role, the kind of statement, the time) and nothing else.
- **A reader leaves no trace.** ADR-0034 keeps the log to writes, so a connection that only reads is not in the log or in Loki. Logging connections (`log_connections on`) would show every login with its role; it is a change of ADR-0034 for S01 to decide, not made here, and nothing in the criteria asks for it.
- **The grant works.** From a tailnet device outside the grant (`nf-monitoring`), 5432 is closed; from the PC of S01, it is open. The grant is what closes the port to the rest of the tailnet now that the default rule no longer covers it; the firewall rule of task 3 stays the second lock.
- **The test from outside is the owner's, not the AI's.** The AI cannot probe the public address from the PC of S01 (its tool refuses network probes), so S01 ran the six connection tests from another network and pasted the result; the record names the tool, the time and the vantage point, so the test can be repeated the same way after a change of the router or of the firewall.
- **Five people, four in the text.** The milestone says four people; five roles exist and five people use them. The criteria do not limit the number, so the text is not amended for it (RC-285).

## Overall Verdict

Go: every criterion passes on the evidence of 2026-11-05, the statements of S01 and the test from outside the home network; the milestone closes, with #395 and the GitHub milestone, through the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Repeat the six connection tests from outside the home network after a change of the router or of the firewall (also MIL-023 criterion 2) | S01 | At each such change |

---

[MIL-048]: ../../milestones/mil-048-team-database-access.md
[ADR-0034]: ../../adr/adr-0034-team-database-access.md
[RC-285]: ./rc-285-mil-048-gate.md
[RC-245]: ./rc-245-mil-048-plan.md
[RC-247]: ./rc-247-mil-048-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
