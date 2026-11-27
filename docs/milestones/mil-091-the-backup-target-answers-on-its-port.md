# MIL-091 The backup target answers on its port

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-091 |
| CrossReference | [BC-001], [DSA-001], [ADR-0008], [MIL-069], [MIL-074], [RC-361] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Deprecated | mikemike | S01 | Initial version: no copy reached the backup target from 2026-11-21 to 2026-11-27, because its service ran without its published port and nothing on the backup host mended that; planned at S01's request in chat on 2026-11-27 ("Ja, planlæg det"), after S01's restart of the service the same day; supervised mode; accepted as it stands by S01 in chat on 2026-11-27 ("Ja, accepter") after RC-450, Go | [3bddcfe] |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Gate passed: RC-452 Go on criteria 1 to 5; tasks 1 and 2 in pull request 891 (RC-451, its conditions met at the gate), and its fix F10 of `register-start-task.ps1` in the gate pull request; task 3 on the backup host on S01's yes in chat: the installation at 16:06 UTC, and the drill from 19:09 to 20:10 UTC with S01 at the keyboard; task 4: in Prometheus the newest copy off the VM minutes old, and `BackupTargetDown` and `OffVmCopyStale` silent; mode autonomous since S01's word in chat at 11:36 UTC, not independent | pending |

---

## Purpose

Keep the backup target on the backup host taking the copy whenever the host is on, also after the host has started before Tailscale gave it its tailnet address; and let the scheduled task on the backup host mend a target that runs without its port within the hour, instead of a person days later.

Since [MIL-074] the virtual machine (VM) tries the copy off the VM every hour, and the scheduled task `\NFHotel\Backup target up` on the backup host runs `docker compose up -d` at sign-in and every hour, so a stopped service of the backup target ([MIL-069]) starts again. Read on 2026-11-27:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | No copy reached the backup target from 2026-11-21 21:30 UTC to 2026-11-27 09:37 UTC. Every hourly copy timed out on port 2222 of the backup host (the journal of `nfhotel-offvm-copy.service`), while the host answered a Tailscale ping on most runs from 2026-11-22 on. The service `backup-target` had run since 2026-11-24 09:53 UTC without a published port: `docker ps` showed `22/tcp` with no address and no host port, and nothing listened on port 2222, although `BACKUP_TARGET_BIND` names the tailnet address that the host had. A restart of the service at 09:34 UTC on 2026-11-27 published the port, and the copy of 09:37 UTC sent the 24 files that the target lacked | The backup target in Docker Desktop on the backup host | `OffVmCopyStale` fired from 2026-11-24 21:30 UTC: for three days the newest copy off the VM was older than 72 hours while the backup host was on, which success criterion 35 of [BC-001] does not allow |
| 2 | The backup host went to sleep at 2026-11-21 21:35 UTC and was started again on 2026-11-22 at 06:09 UTC and on 2026-11-24 at 09:51 UTC. Docker Desktop starts the service at sign-in by its restart policy. Most likely the host did not have its tailnet address yet at that moment (on 2026-11-22 Tailscale on the backup host was seen signed out), so the port could not be published, and Docker does not try again when the address comes later | Docker Desktop and Tailscale on the backup host | A service that runs without its port looks healthy: it is `Up`, so its restart policy and `docker compose up -d` leave it as it is |
| 3 | The scheduled task ran every hour with the result 0 the whole time, because `docker compose up -d` changes nothing while both services run | `ops/deploy/backup-target/start-target.js` | Nothing on the backup host mends a service that runs without its port |
| 4 | `BackupTargetDown` fired from 2026-11-22 10:13 UTC, as [MIL-074] designed it, but the wiki page Alerts says to start Docker Desktop and lets the scheduled task start the target, and the wiki page Disaster-Recovery checks the target with `docker compose ps` only. Neither names a service that runs without its port, and the description of the rule names only a target that is down | `ops/deploy/monitoring/prometheus/rules/nfhotel.yml`; the wiki pages Alerts and Disaster-Recovery | The advice does not fit this case: a service that already runs does not start again |

## Deliverable

- **A target without its port is restarted.** After `docker compose up -d`, `start-target.js` checks that something listens on the address and port that `backup-target.env` gives (`BACKUP_TARGET_BIND`, and `BACKUP_TARGET_PORT`, 2222 when it is not set). When nothing listens there while the host has that address, it restarts the service `backup-target` once (`docker compose restart backup-target`) and checks again. When the host does not have the address yet, it changes nothing, and the next run tries again. Everything runs hidden, as now. The result of the task names the case: 0 when the port answers, and a code of its own for a failed `docker compose`, for an address that the host does not have, and for a port that still does not answer after the restart, so that the last result of the task shows the problem.
- **The run at sign-in waits.** `register-start-task.ps1` gives the trigger at sign-in a delay of 5 minutes, so that its first run comes after Tailscale and Docker Desktop are up. The hourly trigger stays, so a target that loses its port later is mended within the hour.
- **Tests in Ops CI.** The tests run `start-target.js` in Node.js, in an image of a long-term support line of Node.js, with stand-ins for the shell and the files of Windows Script Host, so they need no Windows. They cover each case above, and each fails against the script of `main` at 1d99f32f.
- **On the backup host.** On S01's yes in chat: the new `start-target.js` in the folder of the backup target, and `register-start-task.ps1` run again there, which replaces the task.
- **The documents.** The description of `BackupTargetDown` in the rules names a target that runs without its port. The wiki page Disaster-Recovery says what the task does now, how to see the port in `docker ps`, and how to restart the service by hand; the wiki page Alerts says what to do; and the image of the tests is in the image list of the wiki page Containers-and-Images.
- **Not part of this milestone:** a second target that is always on, which stays S01's decision of [MIL-074]; and Tailscale on the backup host, which the artificial intelligence (AI) assistant does not change.

Order: the review of this document (QC-MIL-001 and QC-LANG-001) and S01's acceptance first, then tasks 1 and 2 on one branch with their pull request, task 3 on the backup host on S01's yes, and task 4 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The tests show that `start-target.js`: leaves a service whose port answers alone and ends with 0; restarts the service once when nothing listens while the host has the address, and ends with 0 when the port then answers; and ends with its own code, and no further restart, when the port still does not answer after the restart, when the host does not have the address, and when `docker compose up -d` fails. Each test fails against the script of `main` at 1d99f32f, and Ops CI runs them | Yes | No |
| 2 | On the backup host, after task 3: the task has a trigger at sign-in with a delay of 5 minutes and an hourly trigger; and a run while the port answers leaves the service alone (its start time does not change) and ends with the last result 0 | Yes | No |
| 3 | A drill on the backup host, on S01's yes and with S01 at the keyboard: with Tailscale down, the service is restarted and its state is read; with Tailscale up again, one run of the task brings the port back, so that it answers on the tailnet address, and the next copy off the VM succeeds | Yes | No |
| 4 | In Prometheus, at the gate, the newest copy off the VM is less than 2 hours old, and `BackupTargetDown` and `OffVmCopyStale` are silent | Yes | No |
| 5 | The description of the rule and the wiki pages say what the deliverable says; `test-wiki-coverage.sh` and the rule tests pass; the code review against `QC-SH-001` (for the two Windows scripts, as [RC-361] did) and `QC-YML-001` is Go; `lint.sh` is clean; and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-074 | The hourly copy, `BackupTargetDown` and the scheduled task, which this milestone changes |
| MIL-069 | The two services of the backup target and their restart policy |
| ADR-0008 | Decision D7, the copy off the VM, which stays as it is |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 32 and success criterion 35: the backups off the VM within their time | Criteria 1 to 4: the newest copy less than 72 hours old whenever the backup host is on in that time, also when the host starts before it has its tailnet address |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-04. A short window, because finding 2 can happen again at any start of the backup host. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The target restarted when it has no port | Written first: a test in `ops/tests` that runs `start-target.js` in Node.js with stand-ins for the shell and the files of Windows Script Host, one case per part of criterion 1, as a new entry of the script tests of Ops CI with a Node.js image. Then the check of the port and the one restart in `start-target.js`, and the delay of 5 minutes at sign-in in `register-start-task.ps1`. The new check is required only once S01 adds it to the rules of the branch | No | criterion 1 |
| 2 | The documents, code review and pull request | The description of `BackupTargetDown` in `rules/nfhotel.yml`; the wiki pages Disaster-Recovery, Alerts and Containers-and-Images; the review record against `QC-SH-001` and `QC-YML-001`; the pull request, which closes the issues of tasks 1 and 2 | No | criterion 5 |
| 3 | The installation and the drill on the backup host | On S01's yes in chat: the new `start-target.js` in the folder of the backup target, and `register-start-task.ps1` run again there. The AI assistant reads the triggers and a run that leaves the service alone. Then comes the drill with S01 at the keyboard (taking Tailscale down and up is S01's), and the AI assistant reads the copy off the VM that follows in the journal inside the VM over Tailscale Secure Shell (SSH) | No | criteria 2 and 3 |
| 4 | Gate and review record | The gate review record against criteria 1 to 5, with the age of the newest copy and the two alerts in Prometheus; the row of MIL-091 in the traceability matrix; the Accepted row of this document; and the closing of the GitHub milestone | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[MIL-069]: ./mil-069-retention-on-the-backup-target.md
[MIL-074]: ./mil-074-the-off-vm-copy-within-its-time.md
[RC-361]: ../sqa/reviews/rc-361-mil-074-code.md
[3bddcfe]: https://github.com/NF-Hotel/NFHotel/commit/3bddcfe38ec75df7854ca8ad14a083a4396b17fc
