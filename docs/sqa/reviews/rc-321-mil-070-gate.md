# RC-321 Gate review of MIL-070 The database password out of the logs

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-321 |
| CrossReference | [MIL-070], [RC-318], [RC-320], [ADR-0041], [CIS-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-09 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-070 after the deploy of the fix (task 4), the new password of `nfhotel_app` (task 5) and a read-only check of the VM; Go | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-070] at its gate: the six Go/No-Go criteria, with the evidence of pull request #620 (merged as ec2d8a1 on 2026-11-08 at 23:13 UTC), of the deploy of the fix (task 4), of the new password of `nfhotel_app` (task 5) and of a read-only check of the VM on 2026-11-09 at 07:47 UTC. Before the gate: [RC-318] (the plan, accepted by S01) and [RC-320] (the code of tasks 1 to 3, Go).
- Checklist used: the Go/No-Go criteria of [MIL-070]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 1, 2, 5 and 6 cite RC-320 and the checks of #620, and criteria 3 and 4 the counts and checks on the VM below. The pull request of this record also changes three wiki pages: Host-Hardening (the date of the new password, and how to check the log of `sudo`), Workflows and Security-Controls (the 44 required checks, finding 4).
- Language and domain: en / it (the Metadata rows of MIL-070); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 accepted the plan in chat ([RC-318]) and asked for the last milestones in autonomous mode. S01 asked in their own words for both CD runs of tasks 4 and 5: "1 tjek og godkend" for the deploy of the fix, and "2 ja og du godkender den også" for the run after the new password. On 2026-11-09 S01 asked in chat for this gate ("færdiggør disse"), with a snapshot of the VM and a full backup of it on an external drive.

**Evidence of 2026-11-08 and 2026-11-09:**

- **How the VM was read.** Read-only scripts over Tailscale Secure Shell (SSH) that print counts, times and states only, never a log line or a value. The password files go to `grep -c -F -f` and to `psql` on standard input, and every output passes a filter that hides an `ENV=` field and a `Password=` value. A count by value counts the lines with the form `NFHOTEL_API_CONNECTION_STRING=Host=`, with `Password=`, or with the password itself; a count by the name counts `NFHOTEL_API_CONNECTION_STRING=` (finding 1).
- **The pull request.** #620, on its last commit 6c9b5dc: 52 checks passed, `DAST full scan` was skipped by design (it runs only by hand and on schedule), and `OWASP ZAP` was neutral (code scanning could not compare the alerts of the branch: the configuration `zap-full` on `main` comes from the full scan, which does not run on a branch). All 44 checks that the ruleset requires today passed: the 41 it required at the merge, and the three script tests it requires since (finding 4), `Ops script tests (test-sudo-log.sh)` among them. It closed #621, #622 and #623, the issues of tasks 1 to 3.
- **The deploy of the fix (task 4).** CD run 42534973921 of ec2d8a1 was approved at 23:25 UTC, after the checks S01 asked for, and deployed from 23:25:24 to 23:28:39 UTC. Right before it, at 23:25:02 UTC, the journal held 187 entries of `sudo` with the connection string, the first on 2026-10-21 at 06:12 UTC and the last on 2026-11-08 at 22:50 UTC, and `sudo.log`, `sudo.log.1` and `sudo.log.2.gz` held 8, 50 and 43. After it the counts were the same, and the lines since 23:25 UTC held none. The smoke test gave 301 and 200, and the API was healthy. Issue #624 holds the counts.
- **The new password (task 5).** At 23:40:53 UTC, with no deploy running and the database and the API healthy, `postgres-app.password` was moved aside as `postgres-app.password.old`, and `prepare-database-roles.sh --apply` made a new password, set it on `nfhotel_app` and wrote `nfhotel-api-database.env` again, as the page Database-Preparation says. At 23:40:54 UTC a login as `nfhotel_app` with the old password was refused, and one with the new password accepted. Attempt 2 of the same CD run was approved at 23:40:59 UTC and deployed from 23:41:12 to 23:43:05 UTC; the API container started at 23:42:49 UTC with the new secret file. At 23:43:41 UTC the API was healthy, a request that reads the database answered 200 with 120 rooms, the old password was refused again, and the counts had not grown. Issue #625 holds the checks.
- **The read of the gate,** on 2026-11-09 from 07:47 UTC: the checkout of the VM at ec2d8a1 and no deploy running; 11 containers, none starting or unhealthy; the API healthy, running since 23:42:49 UTC; the smoke test 301 and 200; `GET /api/bookings/available-rooms` answered 200 with 128 rooms and no error; the old password refused and the new one accepted. The log of PostgreSQL holds 3 failed logins of `nfhotel_app` since the change, at 23:40:54, 23:43:43 and 07:47:48 UTC: the three checks of the old password. The log of the API holds none. The 185 lines of `sudo` since 23:25 UTC, from the two deploys of run 42534973921, hold the connection string, `Password=`, the old password and the new password 0 times each, and the name 40 times (finding 1). The journal and the three `sudo.log` files still hold 187 and 8, 50 and 43 entries with the connection string. `postgres-app.password.old` is still on the VM (0600, root) until the action item below.
- **The other values of the VM.** The same 185 lines were searched for each value of 8 characters or more in the secret files and the settings files of the VM: 92 values. 12 were found, network names, the name of the environment and the names of upstream hosts, and each of them is text of the deployed commit of the repository. No secret was found.

## Checklist Results

| # | Criterion (MIL-070) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The regression test fails with the `as_app` of 4d67484 and passes with the fix, and Ops CI runs it | Pass | 17 passed and 2 failed against 4d67484 (03f1ca4), 19 passed with the fix ([RC-320]); `Ops script tests (test-sudo-log.sh)` passed on #620 and has been a required check since 2026-11-08 |
| 2 | No `sudo` call in `ops/scripts` passes a secret in its environment or its arguments: the second check of the test passes, and the code review finds none | Pass | The second check passed on #620, and the review of every call in [RC-320] found none. On the VM, the lines of `sudo` from the two deploys since the fix hold no secret value |
| 3 | After the first deploy with the fix: the journal of the VM and `/var/log/sudo.log*` hold no more entries with the connection string than right before it, the smoke test gives 301 and 200, and the API is healthy | Pass | 187 in the journal and 8, 50 and 43 in the `sudo.log` files before the deploy, after it and at the gate, and none in the lines since 23:25 UTC; 301 and 200; the API healthy. A count by the name grows by 20 with each deploy, and none of those lines holds the value (finding 1) |
| 4 | After the new password: a login as `nfhotel_app` with the old password is refused, the API is healthy and answers a request that reads the database, and the counts of criterion 3 have not grown | Pass | Refused at 23:40:54 and 23:43:43 UTC, and on 2026-11-09 at 07:47:48 UTC; the API healthy, and the request answered 200 with 120 rooms at 23:43 UTC and with 128 at the gate; the counts unchanged |
| 5 | The wiki pages Database-Preparation and Host-Hardening say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | The sections of #620 ([RC-320]); the pull request of this record adds to Host-Hardening the date of the new password and how to check the log by value; `test-wiki-coverage.sh`: 112 passed, 0 failed |
| 6 | The code review against `QC-SH-001` (and `QC-WF-001` when a workflow changes) is Go, `lint.sh` is clean, and every required check of the pull request is green | Pass | [RC-320] Go, with `lint.sh` clean; `Ops linters` passed on #620, and all 44 required checks passed on 6c9b5dc |

## Findings

1. **Count by value, not by the name.** The text of the shell that `as_app` starts ([ADR-0041]) names `NFHOTEL_API_CONNECTION_STRING` in its `export`, and `sudo` logs that text with every call: 20 lines per deploy. Counted by the name, the entries went from 187 to 207 in the journal and from 8 to 28 in `sudo.log` with the deploy of the fix, which looked like a failure. Counted by value, none of the 20 new lines holds the connection string. Host-Hardening now says how to check.
2. **The old entries hold two passwords, and neither opens the database.** Of the 187 entries, 100 hold the password in use from 2026-10-28 until the change, and 87, written from 2026-10-21 to 2026-10-28, an earlier one. A role has one password at a time, so neither opens the database now; the login with the later one is refused.
3. **The new password interrupted nothing.** From the change at 23:40:54 UTC until the new container started at 23:42:49 UTC, the API used the connection it had open and opened no new one. The log of PostgreSQL holds no failed login other than the three checks of the old password, and the log of the new API container holds none.
4. **The ruleset requires 44 checks.** [RC-320] left to S01 whether the ruleset should require the script tests of the migration script, of the removal of the desktop and of the log of `sudo` (finding 6). S01 left the change to the AI in chat on 2026-11-08. Since 23:26 UTC the ruleset "Approve merges" has required 44 checks, and a diff of the ruleset before and after shows no other change. The pages Workflows and Security-Controls said 41 and "every check that runs on a pull request", but `Ops service networks (MIL-061)` runs on every pull request and is not required (an open action of RC-289). The pages now give the count and name that check.
5. **How long the old entries stay.** The deliverable keeps them until their retention ends, because each holds a password that opens nothing (finding 2), and deleting them would also delete the trail of every other `sudo` command that rows 5.2.1 to 5.2.3 of [CIS-001] keep. Loki keeps 14 days, so the last entry (2026-11-08, 22:50 UTC) leaves it by about 2026-11-22. `sudo.log` is rotated weekly and keeps 13 old files, so the last of its 101 entries goes by about mid-February 2027. The journal keeps at most 1 GB; it uses 933 MB and starts on 2026-09-04, so at the rate of the last two months, about 14 MB a day, it reaches its limit within about a week and then drops its oldest files. The entries of 2026-10-21 to 2026-11-08 then go between about the turn of the year and mid-January 2027.

## Overall Verdict

Go: the six criteria pass on the evidence of 2026-11-08 and 2026-11-09. The gate of MIL-070 is passed: issue #626 closes through the pull request of this record, and the GitHub milestone #52 is closed after the merge. The old entries stay until their retention ends (finding 5). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Delete `postgres-app.password.old` on the VM after the merge of this record (task 5 of MIL-070; step 3 of "A new password for `nfhotel_app`" on Database-Preparation), after one more check that the old password is refused | AI as stand-in for S01 | At the merge |
| Close the GitHub milestone #52 after the merge of this record, and check that #626 is closed | AI as stand-in for S01 | At the merge |

---

[MIL-070]: ../../milestones/mil-070-the-database-password-out-of-the-logs.md
[RC-318]: ./rc-318-mil-070.md
[RC-320]: ./rc-320-mil-070-code.md
[ADR-0041]: ../../adr/adr-0041-connection-string-over-standard-input.md
[CIS-001]: ../cis-checklist.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
