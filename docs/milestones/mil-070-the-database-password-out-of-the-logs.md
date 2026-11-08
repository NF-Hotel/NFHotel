# MIL-070 The database password out of the logs

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-070 |
| CrossReference | [BC-001], [CIS-001], [DSA-001], [ADR-0011], [ADR-0024], [MIL-023], [MIL-068] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | S01 | Initial version: the password of `nfhotel_app` found in the log of `sudo` on 2026-11-08, with the counts of that evening; planned at S01's request in chat ("ja begge", to "Må jeg lave en plan (en ny milestone) for at rette deployet?") | [9bf2650] |

---

## Purpose

Decide that a deploy writes no secret into a log of the production virtual machine (VM), and that the password of the database role `nfhotel_app` ([ADR-0011]) that the logs hold no longer opens the database.

On 2026-11-08 S01 cancelled a continuous deployment (CD) run while it deployed. Reading the journal of the VM to see how far the deploy had come, the artificial intelligence (AI) assistant found the connection string of the application programming interface (API), with the password of `nfhotel_app`, in the log lines of `sudo`. Its own output showed the first characters of the password before it hid the field; it stopped and reported, as AGENTS.md requires for a security defect. A read-only count the same evening (20:51 UTC; only counts and times, no log line printed) gave these findings:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | `as_app` in `ops/scripts/deploy-release.sh` runs `deploy-services.sh` as `nfhotel` through `sudo -u nfhotel --preserve-env=NFHOTEL_API_CONNECTION_STRING,NFHOTEL_API_MIGRATE_ON_START`. `sudo` writes the variables that it keeps into the `ENV=` field of its log line, so every deploy since the roles of [ADR-0011] has written the password | The journal of the VM (persistent, at most 1 GB): 186 entries, the first on 2026-10-21 at 06:12 UTC, the last on 2026-11-08 at 20:26 UTC | Anyone who reads the journal can log in to the database as the role of the API |
| 2 | Since [MIL-023] `sudo` also writes every command to `/var/log/sudo.log` (`Defaults logfile`, rows 5.2.1 to 5.2.3 of [CIS-001]), with the same fields | `/var/log/sudo.log` and its two older files: 7, 50 and 43 entries | The same. The files are 0600 root, and the groups `adm` and `systemd-journal` have no members, so on the VM only root reads them |
| 3 | Alloy sends the whole journal to Loki (`loki.source.journal` in `ops/deploy/alloy/config.alloy`, with no stage that drops or masks a field); Loki keeps 14 days | Loki and Grafana on the monitoring container | Everyone who can read the logs in Grafana can read the password |
| 4 | Part of the password appeared in the output of the AI assistant | The session of the assistant | The password counts as exposed outside the VM, so only a new password closes the finding |

The job log of the cancelled run on GitHub holds no `Password=` field: the deploy names its steps and never prints a value ([ADR-0024]). No other `sudo` call in `ops/` passes a secret: `cd-steps.sh` passes commits and image references to `nfhotel-deploy`.

The order matters. A deploy after a new password and before the fix would write the new password too, so the fix is deployed first and the password changed after it. A deploy before the new password writes the same, already exposed password again and adds no new reader.

## Deliverable

- **The fix.** `as_app` hands the connection string to `deploy-services.sh` over standard input, where the shell that `sudo` starts reads it into the variable, instead of through the environment that `sudo` keeps and logs. `sudo` stays for the change of user: it resets the environment and logs the command, as rows 5.2.1 to 5.2.3 of [CIS-001] require. Task 1 records the choice in ADR-0041, with the ways it rejects: `runuser` or `setpriv` (the change of user would leave no log line), a temporary file for `nfhotel` (the secret on disk once more), and no log of `sudo` for root (the audit trail of [MIL-023] would go).
- **A regression test.** In `ops/tests`, in a `debian:13-slim` container with `sudo` and `Defaults logfile`: `as_app` with a known fake connection string passes the value to the command, and the value is in neither the log of `sudo` nor the arguments of a process. With the `as_app` of today the test fails. A second check refuses a `--preserve-env` or a `sudo VAR=value` in `ops/scripts` whose variable is not on a short list of names that hold no secret. The continuous integration (CI) workflow Ops CI runs both.
- **The deploy of the fix** through CD, with counts on the VM right before and after it that show no new entry (Loki takes its lines from the journal).
- **A new password for `nfhotel_app`.** The password file of the role is moved aside, `prepare-database-roles.sh --apply` makes a new one, sets it on the role (`roles.sql`) and writes `nfhotel-api-database.env` again, and CD deploys the release again, so the API reads the new secret file (the hash of [ADR-0024], decision 5, recreates the container). From then on the old password is refused.
- **The old entries stay** in the journal, `sudo.log` and Loki until their retention ends (1 GB of journal, the rotation of `sudo.log`, 14 days in Loki). After the new password they hold a password that opens nothing, and deleting them would also delete the audit trail of every other `sudo` command. The gate record keeps the decision.
- **The wiki.** The page Database-Preparation says how the password of `nfhotel_app` is changed; the page Host-Hardening says that the log of `sudo` holds no secret, and why.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch with their pull request, task 4 after S01 approves its CD run, task 5 after task 4, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The regression test fails with the `as_app` of 4d67484 and passes with the fix, and Ops CI runs it | Yes | No |
| 2 | No `sudo` call in `ops/scripts` passes a secret in its environment or its arguments: the second check of the test passes, and the code review finds none | Yes | No |
| 3 | After the first deploy with the fix: the journal of the VM and `/var/log/sudo.log*` hold no more entries with the connection string than right before it, the smoke test gives 301 and 200, and the API is healthy | Yes | No |
| 4 | After the new password: a login as `nfhotel_app` with the old password is refused, the API is healthy and answers a request that reads the database, and the counts of criterion 3 have not grown | Yes | No |
| 5 | The wiki pages Database-Preparation and Host-Hardening say what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` (and `QC-WF-001` when a workflow changes) is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-068 | The removal of the desktop came first (done on 2026-11-08), so no deploy overlapped the purge and the restart of the VM |
| MIL-023 | The log of `sudo` to its own file (rows 5.2.1 to 5.2.3 of CIS-001), which this milestone keeps |
| ADR-0011 | The roles `nfhotel_app` and `nfhotel_migrator` and their secrets, made by `prepare-database-roles.sh` |
| ADR-0024 | The secrets as files in the containers, and the hash that brings a new secret to the API |
| DSA-001 | A finding is fixed, or accepted by S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 18: the database reachable only through the API; the web application holds neither the database login nor the passport key | Criteria 3 and 4: the login of the API is in no new log line, and the exposed password no longer opens the database |
| BC-001 objective 25 and success criterion 28: hardened server | Criterion 2, and the log of `sudo` kept, so rows 5.2.1 to 5.2.3 of CIS-001 stay Applied |
| BC-001 objective 23 and success criterion 26: release only through an approved pipeline | Criteria 3 and 4: the fix and the new password reach the VM through CD, after the approval of S01 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-15. A week, because the exposed password opens the database until task 5; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The fix and ADR-0041 | `as_app` in `ops/scripts/deploy-release.sh` hands `NFHOTEL_API_CONNECTION_STRING` to the command over standard input instead of `--preserve-env`, so `sudo` no longer writes it into its log; `sudo` stays for the change of user. ADR-0041 records the choice and the ways it rejects (`runuser` or `setpriv`, a temporary file, no log of `sudo` for root). | No | criteria 1, 2 |
| 2 | The regression test | Written first: a test in `ops/tests`, in `debian:13-slim` with `sudo` and `Defaults logfile`, in which a fake connection string reaches the command and is in neither the log of `sudo` nor the arguments of a process; it fails with the `as_app` of today. A second check refuses a `--preserve-env` or a `sudo VAR=value` in `ops/scripts` with a variable outside a short list of names that hold no secret. Ops CI runs both. | No | criteria 1, 2 |
| 3 | The wiki, code review and pull request | Database-Preparation: how the password of `nfhotel_app` is changed. Host-Hardening: the log of `sudo` holds no secret, and why. The review record against `QC-SH-001` (and `QC-WF-001`) before the pull request, which closes one issue per task it completes. | No | criteria 5, 6 |
| 4 | The deploy and the counts | S01 approves the CD run of the merge. The AI assistant counts the entries with the connection string in the journal and in `/var/log/sudo.log*` right before and after it, without printing a line, and checks the smoke test and the health of the API. | No | criterion 3 |
| 5 | A new password for nfhotel_app | After task 4, at a time S01 names: over Tailscale Secure Shell (SSH), without printing the password or putting it on a command line, the AI assistant moves `postgres-app.password` aside and runs `prepare-database-roles.sh --apply`, which makes a new password, sets it on the role and writes `nfhotel-api-database.env` again. The API cannot open a new connection until the next deploy, so S01 approves a new CD run of the same commit right away, and the API reads the new secret file. Then the checks of criterion 4; the old file is deleted after the gate. | No | criterion 4 |
| 6 | Gate and review record | The gate review record against criteria 1 to 6, with the counts and the decision to leave the old entries until their retention ends, the row of MIL-070 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[CIS-001]: ../sqa/cis-checklist.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[MIL-023]: ./mil-023-operational-quality.md
[MIL-068]: ./mil-068-the-vm-without-a-desktop.md
[9bf2650]: https://github.com/NF-Hotel/NFHotel/commit/9bf2650c1d47233f83a2c7d436dbca02de5a033f
