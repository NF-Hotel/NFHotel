# RC-285 Gate review of MIL-048 Team access to the database from DBeaver

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-285 |
| CrossReference | [MIL-048], [ADR-0034], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; S01 accepted the plan to close the open milestones in chat on 2026-11-05 ("Accepter planen"); the verdict is Go-with-conditions, so the milestone stays open | [e58a634] |

---

## Artifact Under Review

- Instance reviewed: [MIL-048] at its gate: the six Go/No-Go criteria, with what the AI could verify on `nf-hotel` and from the PC of S01 on 2026-11-05. The earlier records are [RC-245] (the milestone against QC-MIL-001, Go) and [RC-247] (the code of tasks 1 to 3 and 5 against QC-SH-001, QC-YML-001 and QC-WF-001; Go-with-conditions, with criteria 2, 3 and 5 left for the gate).
- Checklist used: the Go/No-Go criteria of [MIL-048]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: gate review of criteria 1 to 6. New evidence for the roles of criterion 1, the LAN side of criterion 3 and the deploy of criterion 6; the privilege test on the VM, criteria 2 and 5 and the internet side of criterion 3 wait on S01 and the team.
- Language and domain: en / it (the Metadata rows of MIL-048); this record is a technical type.
- Language reviewer: none
- Mode: autonomous, not independent. The AI wrote the milestone, the code review and this record; S01 asked in chat on 2026-11-05 to finish the open milestones and accepted the plan ("Accepter planen"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on 2026-11-05** (Tailscale SSH to `nf-hotel` in check mode at 07:11 UTC, approved by S01; read-only commands; and two TCP probes from the PC of S01, which is on the LAN and on the tailnet):

- Roles in the database (`psql` in `nfhotel-postgres-postgres-1`): `dev_foxxy`, `dev_jakob`, `dev_jens`, `dev_mike` and `dev_william`, each with `rolcanlogin` true and `rolsuper` false, all members of `nfhotel_team`, which cannot log in. Five roles, one more than the four people the plan named.
- Relay: `nfhotel-postgres-tailnet-relay-1` is up and healthy (7 days at the reading) and published on `100.72.194.127:5432` only; `ss -ltn` shows the single listener `100.72.194.127:5432`; `nfhotel-postgres-postgres-1` publishes no port.
- Firewall: the nft chain `docker-user` accepts DNAT to 5432 only with `iifname "tailscale0"`, DNAT to 80 and 443 from anywhere, and drops the rest with a rate-limited log line.
- From the PC of S01: `Test-NetConnection` to `192.168.10.72:5432` (the LAN address of the VM) fails, to `100.72.194.127:5432` (the tailnet address) succeeds.
- The deploy: pull request #396 merged on 2026-10-28 (`ca2924c`) with every check green and was deployed by the automatic CD of the time; the relay has run since 2026-10-29.

## Checklist Results

| # | Criterion (MIL-048) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The four `dev_*` roles exist on `nf-hotel`, `LOGIN`, not superuser, member of `nfhotel_team`; as one of them, `SELECT`, `INSERT`, `UPDATE` and `DELETE` work and `CREATE TABLE`, `TRUNCATE` and `DROP` are refused (test in CI on PostgreSQL 16, and on the VM) | Pending | The roles exist as specified (five of them), and `ops/tests/test-team-db-users.sh` proves the privileges in Ops CI (RC-247). The same statements as one of the roles on the VM need the password of that role, which the AI does not have and must not read: a team member runs them from DBeaver |
| 2 | Each of the four people connects from DBeaver to `100.72.194.127:5432`, database `nfhotel`, with their own role | Pending | No connection reported yet. The Tailscale grant (task 4, #393) is not in the policy; as long as the default rule of the tailnet stands, the grant adds nothing and the password is the only lock, as the wiki page Database-Access says |
| 3 | Port 5432 does not answer on the VM's LAN address or from the internet; MIL-023 criterion 2 (only 80 and 443 from outside) still holds | Pending | The LAN side passes: refused from the PC of S01, and the listener and the nft rule allow the port from `tailscale0` only; 80 and 443 answer on the tailnet address only (RC-192, and the live read of 2026-11-03). The internet side is not scanned: S01 scans from outside the home network once the router forwards 50023 and 50024 are removed (RC-244, action item 2) |
| 4 | No password on a command line, in git, in a log or in the chat; the script passes them to `psql` on standard input only | Pass | RC-247 for the script; this review read and printed nothing of a password, and the password files live in `/etc/nfhotel/secrets/team/` with mode 0600 |
| 5 | A write by a `dev_*` role is found in Loki with the role's name | Pending | No write by a team member yet; checked in Grafana once one has written a test row |
| 6 | Ops CI is green, the reviews against `qc-programming-shell`, `qc-yaml` and `qc-workflow` are Go, and the deploy after the merge succeeds | Pass | Pull request #396 merged green on 2026-10-28; RC-247 Go-with-conditions, whose condition (QC-WF-001 criterion 22) closed with that green Ops CI; the relay has run since the deploy that followed |

## Findings

- **Five roles, not four.** `dev_foxxy`, `dev_jakob`, `dev_jens`, `dev_mike` and `dev_william` exist; the plan said four people. Nothing in the criteria limits the number, and the fifth is a member of the team as well, so the gate takes the five as the set.
- **The grant is missing, and today it would change nothing.** The tailnet policy still has its default rule, so every device on the tailnet reaches the port and the password is the only lock. The grant matters the day the default rule goes; S01 adds it now so that day needs no change.
- **The traceability matrix had no row for MIL-048, ADR-0034, RC-245 or RC-247** since 2026-10-28. The rows are added with this record.

## Overall Verdict

Go-with-conditions. Everything the AI can check holds: the roles, the relay, the single listener, the firewall rule, the LAN side and the deploy. The privilege test on the VM (criterion 1), the connections (criterion 2), the scan from the internet (criterion 3) and the write in Loki (criterion 5) wait on S01 and the team, so the milestone stays open until a re-review records them. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Add the grant of the wiki page Database-Access to the tailnet policy (task 4, #393) | S01 | 2026-11-12 |
| Hand each team member the password of their role (from `/etc/nfhotel/secrets/team/`, never in chat); each connects from DBeaver, runs the four statements of criterion 1 and writes one test row | S01 and the team | 2026-11-12 |
| Scan the public address from outside the home network once the router forwards 50023 and 50024 are removed (also MIL-023 criterion 2) | S01 | 2026-11-12 |
| Re-review criteria 1, 2, 3 and 5 with that evidence; on Go, close #395 and the GitHub milestone | S01 (AI as stand-in) | After the three actions above |

---

[MIL-048]: ../../milestones/mil-048-team-database-access.md
[ADR-0034]: ../../adr/adr-0034-team-database-access.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[RC-245]: ./rc-245-mil-048-plan.md
[RC-247]: ./rc-247-mil-048-code.md
[RC-192]: ./rc-192-firewall-tailscale-only.md
[RC-244]: ./rc-244-mil-023-host-settings.md
[e58a634]: https://github.com/NF-Hotel/NFHotel/commit/e58a6341cf1d489e7256b297eb9764d1ecb56332
