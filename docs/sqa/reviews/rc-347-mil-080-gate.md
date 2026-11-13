# RC-347 Gate review of MIL-080 Every container runs with the rights of its profile

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-347 |
| CrossReference | [MIL-080], [RC-343], [RC-346], [CIS-001], [ADR-0018], [ADR-0038], [MIL-082], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-080 after the CD run of the merge of #674; Go-with-conditions: the second half of criterion 5 waits for the deploy of the monitoring | [f0b2e32] |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | The evidence of the deploy corrected: the copy of the backup to the backup target failed, as its CD log shows (found at the gate of MIL-082, RC-351); the verdict and its condition stand | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-080] at its gate: the seven Go/No-Go criteria, with the evidence of pull request #674 (merged as ffb274a on 2026-11-13 at 19:52 UTC), the release run 43497034373 and the continuous deployment (CD) run 43498058217 of that commit, and the answers of Prometheus after the deploy. Before the gate: [RC-343] (the plan) and [RC-346] (the code, Go-with-conditions until every check of the pull request is green).
- Checklist used: the Go/No-Go criteria of [MIL-080]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 1, 6 and 7 cite [RC-346] and the green checks of #674.
- Language and domain: en / it (the Metadata rows of MIL-080); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-12 for the three controls and, where one was missing, for a plan carried out in autonomous mode with the right to merge, to open pull requests and to deploy ("færdiggør milestones i automode med rettigheder til self merge og PR og Deploy"), without being asked ("Du behøver ik spørg mig om noget"), and on 2026-11-13 to finish the milestones and the gaps found ("færdiggør disse milestones og de huller du har fundet ovenfor som du snakker om. fix").

**Evidence of 2026-11-13:**

- **The pull request.** #674: on its last commit fc35da6, 53 checks passed and 2 were skipped by design (`DAST full scan` and `OWASP ZAP`, which run by hand and on schedule). SonarQube Cloud passed its quality gate. The pull request closed #652 to #657 at the merge; #658 and #659 stay open for this gate.
- **The runner.** Ops CI run 43494943566, job "Ops monitoring configuration (MIL-023)", step "Check real containers and every compose file against the profiles (MIL-080)": `test-container-runtime-live.sh` as root with real Docker (Docker Engine 28.0.4, containerd v2.4.2, runc 1.5.2, AppArmor and seccomp on) passed 40 of 40. The three compliant containers passed, and the one that mounts `/` sees the socket and cannot open it. Each breaking container failed with its rule: `privileged`, `cap_add` and `cap_bounding`, `user` (root where the profile says `nonroot`), `writable_mount`, `docker_socket` (mounted, and seen through a parent mount by a member of the socket's group), `host_namespace` (the host's network), `seccomp`, `no_new_privileges` and `profile` (no profile). The check exited 1 and read the 13 containers of the test, its kernel view of a compliant container was user 10001, no capability, seccomp in filter mode and AppArmor `docker-default` in enforce mode, and with `--deploy` the container without a profile was a warning. The 20 compose services of the repository matched their profiles.
- **The throwaway stack.** End-to-end run 43494943495, job "End-to-end tests": `check-e2e-stack.sh` passed its 16 checks ("the end-to-end stack is as expected"), and the runtime check over the stack passed 4 of 4 containers (nginx, the web application, the API and PostgreSQL), each with seccomp in filter mode and AppArmor `docker-default` in enforce mode.
- **The release.** Run 43497034373 of ffb274a: the scan gate passed for the six release images (`nfhotel-web`, `nfhotel-api`, `market-analysis`, `benchmark`, `nfhotel-nginx` and `nfhotel-socket-proxy`) between 19:59 and 20:00 UTC.
- **The deploy.** CD run 43498058217, approved by the AI at 20:04 UTC under S01's words above ([ADR-0038] gives the approval to S01, who gave it to the AI for this run), ended with success; its deploy step ran from 20:13 to 20:18 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest. The update command ([ADR-0018]) installed `/usr/local/sbin/nfhotel-container-check` with `nfhotel-container-check.service` and `nfhotel-container-check.timer`, and enabled the timer. The deploy took a backup of the database before the migrations (its copy to the backup target failed, and the backups stayed on the VM), applied the migrations as `nfhotel_migrator`, and recreated the API, the web application, the market analysis, the benchmark, nginx and the socket proxy. Then "==> the runtime of the containers against container-profiles.tsv (MIL-080)" printed one PASS line for each of the 11 containers of the VM and "==> 11 container(s): 11 passed, 0 failed, 0 warned", with "==> Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2; AppArmor on, seccomp on". The smoke test answered 301 on port 80 and 200 on port 443, the release was recorded ("==> deployed ffb274aef6fb…"), and the service ran once: "==> the container check service passed (nfhotel-container-check.timer runs it every 15 minutes)".
- **Prometheus.** At about 20:20 UTC Prometheus of `nf-monitoring` answered `nfhotel_container_runtime_containers{host="nf-hotel"}` 11 and `nfhotel_container_runtime_failed` 0, had no series of `nfhotel_container_runtime_violation`, and the last success of the task `container_check` was 110 seconds old: the report of the timer reaches Prometheus through Alloy.
- **The monitoring.** At the same time Prometheus of `nf-monitoring` (v3.15.1) had 23 alert rules loaded, and `ContainerRuntimeViolation` and `ContainerCheckStale` were not among them: the monitoring is deployed by hand over Tailscale Secure Shell (SSH), not by CD, and its SSH session waits for S01 to approve its check (check mode). The first attempt ended unapproved at 20:24 UTC when the check expired; the second has waited since 20:25 UTC (finding 1).

## Checklist Results

| # | Criterion (MIL-080) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The unit test has a compliant and a breaking case for every rule, and passes; it fails when a rule of the check is removed | Pass | 81 of 81, with a compliant and a breaking case for each of the 17 rules, and each of the 18 mutants of [RC-346] fails it; green in Ops CI of #674 |
| 2 | The live test on the CI runner passes: the compliant containers pass, and each breaking container fails with its rule (privileged, an added capability, root where the profile says `nonroot`, a writable host mount, the Docker socket, the host's network, `seccomp=unconfined`, no `no-new-privileges`, no profile) | Pass | 40 of 40 in Ops CI run 43494943566; each of the nine named cases failed with its rule, and a member of the socket's group was caught too |
| 3 | The end-to-end job runs `check-e2e-stack.sh` and the check over the throwaway stack, and both pass | Pass | End-to-end run 43494943495: 16 of 16 checks, and 4 of 4 containers |
| 4 | After the deploy, the CD log shows the check over every container of the release with no violation, the release recorded, and the timer enabled with one run of its service that passed | Pass | CD run 43498058217: 11 passed, 0 failed, 0 warned; "deployed ffb274a…"; the timer enabled and its service passed; Prometheus has the report 110 seconds after a run of the timer |
| 5 | The alert rules pass `promtool test rules` in Ops CI, and Prometheus of `nf-monitoring` has them loaded after the deploy of the monitoring; there the check passes over its six containers | Pending | `promtool check config` and `promtool test rules` passed in Ops CI of #674, with the tests of `ContainerRuntimeViolation`, `ContainerCheckStale` and the task in `TaskReportMissing`. The deploy of the monitoring has not run yet, so the rules are not loaded in Prometheus of `nf-monitoring` and the check has not run there (finding 1) |
| 6 | [CIS-001], the wiki pages Host-Hardening and Monitoring say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | [CIS-001] rows 4.1, 5.5 and 5.31 and the evidence `compose`; the wiki pages Host-Hardening, Alerts, Monitoring and Workflows; `test-wiki-coverage.sh` 114 of 114 ([RC-346]), green again in Ops CI of #674 |
| 7 | The code review against `QC-SH-001` and `QC-WF-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Pass | [RC-346] Go-with-conditions, and its condition is met: every check of #674 passed on fc35da6 (53 passed, 2 skipped by design), the job Ops linters (`lint.sh`) among them |

## Findings

1. **The deploy of the monitoring waits for S01.** `deploy-monitoring.sh --apply` runs on `nf-monitoring` over Tailscale SSH, and the policy of the tailnet asks the owner to approve each such session in a browser (check mode). The AI cannot approve it, and does not try to work around it. Until the deploy runs, three things stay as they were on `nf-monitoring`: the two new alert rules are not loaded, so a violation on the VM is in Prometheus but raises no alert; the check has not run over the six containers there; and the blackbox exporter still runs as root, as finding F9 of [RC-346] found in the CI of #674 (the compose file of the pull request makes it `nobody`). The report of the VM is in Prometheus already, so the alert works from the moment the rules load.
2. **The runner and the VM run different versions of Docker Engine.** The live test ran on Docker Engine 28.0.4, the VM runs 29.9.3; both have containerd v2.4.2 and runc 1.5.2, which create the containers and apply their settings. The check reads the versions at every run, so a difference shows in its output and, with task 4 of [MIL-082], in Prometheus.
3. **Ollama runs as root on the VM, as its profile allows.** Its line reads "uid 0, caps none": the image has no user, and the profile says `root` until task 3 of [MIL-082] changes it. The check passes it because the profile says so; it is the gap that MIL-082 closes.

## Overall Verdict

Go-with-conditions: criteria 1 to 4, 6 and 7 pass on the evidence of 2026-11-13, and the condition of [RC-346] is met. Criterion 5 passes in CI, and its second half waits for the deploy of the monitoring (finding 1). The condition: that deploy, with the check over the six containers of `nf-monitoring` in its output and the two rules in the answer of `/api/v1/rules` of Prometheus, recorded in a new row of this record. Then the gate is Go, #658 and #659 close, and the GitHub milestone #55 is closed. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Approve the Tailscale SSH check of the deploy of the monitoring on `nf-monitoring` | S01 | When S01 is next at the PC |
| Deploy the monitoring, read the check over its six containers and the loaded rules, record them in a new row of this record, close #658 and #659 and the GitHub milestone #55 | AI as stand-in for S01 | After S01's approval |

---

[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-082]: ../../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-343]: ./rc-343-mil-080.md
[RC-346]: ./rc-346-mil-080-code.md
[CIS-001]: ../cis-checklist.md
[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[f0b2e32]: https://github.com/NF-Hotel/NFHotel/commit/f0b2e329f891aa4062869e27ec2c55ffd3e492e2
