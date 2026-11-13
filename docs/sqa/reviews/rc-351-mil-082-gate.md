# RC-351 Gate review of MIL-082 A compromised container kept away from the host

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-351 |
| CrossReference | [MIL-082], [RC-345], [RC-350], [RC-347], [RC-349], [MIL-080], [MIL-081], [CIS-001], [ADR-0038], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-082 after the CD run of the merge of #676; Go-with-conditions: the rows of `nf-monitoring` in the host-access map wait for the deploy of the monitoring | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-082] at its gate: the six Go/No-Go criteria, with the evidence of pull request #676 (merged as 052c163 on 2026-11-13 at 21:37 UTC), the two Ops CI runs of its last commit and of the merge, the release run 43512937033 and the continuous deployment (CD) run 43513939028 of that commit. Before the gate: [RC-345] (the plan) and [RC-350] (the code, Go-with-conditions until every check of the pull request is green).
- Checklist used: the Go/No-Go criteria of [MIL-082]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 2, 5 and 6 cite [RC-350] and the green checks of #676.
- Language and domain: en / it (the Metadata rows of MIL-082); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-12 for the three controls and, where one was missing, for a plan carried out in autonomous mode with the right to merge, to open pull requests and to deploy ("færdiggør milestones i automode med rettigheder til self merge og PR og Deploy"), without being asked ("Du behøver ik spørg mig om noget"), and on 2026-11-13 to finish the milestones and the gaps found ("færdiggør disse milestones og de huller du har fundet ovenfor som du snakker om. fix").

**Evidence of 2026-11-13:**

- **The pull request.** #676: on its last commit 034e44d, 54 checks passed and 2 were skipped by design (`DAST full scan` and `OWASP ZAP`, which run by hand and on schedule). The pull request closed #668 to #671 (tasks 2 to 5) at the merge, with #665 and #666 of [MIL-081], whose gate [RC-349] it carried; #667 (task 1), #672 (task 6) and #673 (task 7) stay open for this record.
- **Ollama in CI.** Ops CI run 43510803753 of the pull request and Ops CI run 43512388551 of the merge 052c163 on `main` passed all 38 jobs each. In the job "Ops Ollama as a non-root user (MIL-082)", `test-ollama-user.sh` passed 20 of 20 in both runs. Ollama as root first stores a model on a volume whose key only root reads, as on the VM before this milestone. The project then starts with `models-owner`, which ends successfully with only `CHOWN` and `DAC_READ_SEARCH`, no network and a read-only root, and Ollama runs as 10001 in the kernel's view, turns healthy, lists the model that root wrote and owns the volume and the key. After a restart it is healthy again with the model listed, the runtime check passes it with its profile, and the release before this milestone still lists the models on a volume that 10001 owns, so a rollback keeps them.
- **The runtime check in CI.** In the job "Ops monitoring configuration (MIL-023)" of both runs, the live test of the runtime check passed 42 of 42, among them a container with `systempaths=unconfined` that fails with the rule `system_paths` ("the masked or read-only paths of /proc and /sys are not in force"). The test of the monitoring stack passed 20 of 20 and printed the check of its six containers, "6 passed, 0 failed, 0 warned", on a runner with Docker Engine 28.0.4, containerd v2.4.2 and runc 1.5.2, and `test-alloy-start.sh` passed 18 of 18. The job "Ops service networks (MIL-061)" passed 51 of 51, `models-owner` on no network among them.
- **The release.** Run 43512937033 of 052c163 built the six release images, and its scan gate passed each of them between 21:43 and 21:45 UTC: "no secret, no critical finding and no high finding with a fix". Ollama and `models-owner` run the upstream image of Ollama, pinned by digest, which the release does not build.
- **The deploy.** CD run 43513939028, approved by the AI at 21:49 UTC under S01's words above ([ADR-0038] gives the approval to S01, who gave it to the AI for this run), ended with success; its deploy step ran from 21:49 to 21:52 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest. The deploy of Ollama created `models-owner`, which exited before Ollama started, and recreated Ollama, which turned healthy; the market analysis service and the benchmark were recreated and turned healthy, as every other container did. "==> the runtime of the containers against container-profiles.tsv (MIL-080)" printed one PASS line for each of the 11 containers, "==> 11 container(s): 11 passed, 0 failed, 0 warned", then "==> Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2; AppArmor on, seccomp on". The line of Ollama reads "uid 10001, caps none, seccomp filter, apparmor docker-default (enforce), root ro, host paths none, writable mounts none, socket none, networks nfhotel-llm, ports none". The release was recorded ("==> deployed 052c1630dda2…"), and the container check service passed once.
- **The monitoring LXC.** The monitoring has not been deployed since [MIL-080]: its deploy over Tailscale SSH waits for S01, the condition of [RC-347]. No run of the runtime check on the LXC exists yet.

## Checklist Results

| # | Criterion (MIL-082) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The page Container-Escape has a row for each path of the deliverable with its measure, evidence, what is left and the decision, and a host-access map with a row for every container of the VM and of the monitoring LXC, taken from the runtime check | Pending | The page has a row for each of the 12 paths of the deliverable, each with its measure, evidence, what is left and the decision with its owner, and evaluates user namespaces and rootless Docker again with what would change the decision. The map of `nf-hotel` has a row for each of the 11 containers of CD run 43513939028, taken from its lines (this pull request moves it from the CD run of 77d755f, with Ollama as 10001), and says why `results-owner` and `models-owner` have none. The six rows of `nf-monitoring` are the lines of the CI job above, labelled as such, because no run of the check on the LXC exists yet: Pending until the deploy of the monitoring |
| 2 | The CI test of Ollama passes: user 10001, healthy, the models of a volume that root wrote are listed, and it is healthy again after a restart | Pass | 20 of 20 in Ops CI runs 43510803753 and 43512388551; [RC-350] criterion 11: the same test failed 8 of 20 against 7df3d47, before the compose file changed, so it catches Ollama as root |
| 3 | After the deploy, the runtime check in the CD log shows Ollama with user 10001 and no violation, and the market analysis service and the benchmark are healthy | Pass | CD run 43513939028: "PASS ollama-ollama-1 ollama/ollama - uid 10001, caps none, …" and 11 passed, 0 failed, 0 warned; `market-analysis-market-analysis-api-1` and `benchmark-benchmark-1` "(healthy)" in the status that the deploy printed |
| 4 | The runtime check prints the versions of Docker Engine, containerd and `runc`, and the page says how a fix for an escape is installed | Pass | CD run 43513939028: "==> Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.2; AppArmor on, seccomp on"; the metric `nfhotel_container_runtime_info` ([RC-350]); the section "When a fix for an escape is published" of the page, with runc 1.5.3 as its first case |
| 5 | [CIS-001] rows 2.1 and 4.1 and the wiki pages say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | [RC-350]; the job Ops wiki coverage passed in both Ops CI runs, and it runs again on the map of the VM that this pull request updates |
| 6 | The code review against `QC-SH-001` and `QC-WF-001` is Go, `lint.sh` is clean, and every check of the pull request is green | Pass | [RC-350] Go-with-conditions, its condition met: every check of #676 passed (54 passed, 2 skipped by design); the job Ops linters (`lint.sh`) passed in both Ops CI runs |

## Findings

1. **The rows of [CIS-001].** Its row of f7dbdae (rows 1.2.2, 2.1 and 4.1, this milestone) becomes Accepted, because criterion 5 passes, and the row before it (f320dbf, row 5.31 of [MIL-081]) becomes Deprecated: the newer row carries the content of the older one. The condition of this record touches the map of the LXC only, not a row of CIS-001.
2. **The map of `nf-monitoring` comes from CI.** The rows read `blackbox` as 65534, as the CI job did; on the LXC it runs as root until the deploy of the monitoring, as the page says. That deploy prints the six lines and the versions of the LXC, which replace the rows from CI.
3. **The copy of the backups off the VM.** The three deploys of 2026-11-13 (CD runs 43498058217, 43506763115 and 43513939028) each took the backups before the migrations, and each copy to the backup target failed: "the backups stay on the VM, and the next run sends what the target lacks"; the last copy off the VM was at 2026-11-12T16:51:23Z. [RC-347] and [RC-349] said the backup was copied; this pull request corrects both with a new row each. Their verdicts stand, because no criterion of either rests on the copy. The copy is outside the scope of MIL-082 and is reported to S01.
4. **runc 1.5.3.** The VM runs runc 1.5.2. Version 1.5.3 fixes CVE-2026-119101, which needs checkpoint and restore, and NF Hotel uses neither; the page plans it for the monthly window. It is not a condition of this gate.

## Overall Verdict

Go-with-conditions: criteria 2 to 6 pass on the evidence of 2026-11-13, and criterion 1 is Pending for the six rows of `nf-monitoring` in the host-access map, which wait for the deploy of the monitoring. #672 (task 6) closes at the merge of the pull request that carries this record. #667 (task 1), #673 (task 7) and the GitHub milestone #57 stay open until the condition is met and recorded in a new row of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close #672 | AI as stand-in for S01 | At the merge of this record |
| Deploy the monitoring on `nf-monitoring` (`git pull --ff-only` and `bash ops/scripts/deploy-monitoring.sh --apply` in its checkout, or approve the Tailscale SSH check for the AI); then replace the six rows of `nf-monitoring` in the map with the lines of that run, record it in a new row of this record and of [RC-347], and close #667, #673 and the GitHub milestone #57 | S01, then the AI | When S01 is next at the PC |
| Install runc 1.5.3 on `nf-hotel` and `nf-monitoring` in the monthly window, as the page says | S01, S03 (the reboot of the VM) | The next monthly window |
| Find why the copy of the backups to the backup target fails, and check that the next run sends what the target lacks | S01 | Before the next daily backup |

---

[MIL-082]: ../../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-345]: ./rc-345-mil-082.md
[RC-350]: ./rc-350-mil-082-code.md
[RC-347]: ./rc-347-mil-080-gate.md
[RC-349]: ./rc-349-mil-081-gate.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-081]: ../../milestones/mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
[CIS-001]: ../cis-checklist.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
