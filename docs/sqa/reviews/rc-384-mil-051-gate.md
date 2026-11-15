# RC-384 Gate review of MIL-051 Continuous deployment of the monitoring stack

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-384 |
| CrossReference | [MIL-051], [ADR-0044], [RC-254], [RC-356], [RC-365], [RC-366], [RC-383], [ADR-0007], [ADR-0020], [MIL-076], [PRC-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-051 on the install of the pull on `nf-monitoring`, a documents merge that deployed nothing and a monitoring merge that the pull deployed by itself in 13 minutes and 15 seconds; No-Go for now, criterion 7 fails on the live tailnet filter, and the risk exception that S01 chose waits for the approval of S04; mode autonomous, not independent | [9da77ab] |

---

## Artifact Under Review

- Instance reviewed: [MIL-051] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the code of tasks 2 to 7, merged in pull request 751 (9de9be4, 2026-11-14) after [RC-366], and the Ops CI run
    43683027646 of `main` at f7c2b91 (2026-11-15), its jobs "Ops script tests (test-pull-monitoring.sh)" and "Ops
    monitoring configuration (MIL-023)";
  - the first deploy by hand of that code on `nf-monitoring` and the install of the pull, on 2026-11-15 from 05:54 to
    06:02 UTC over Tailscale SSH, as the action item of [RC-366] asks;
  - the documents merge of pull request 758 (f7c2b91, 2026-11-15 05:53 UTC), the first commit the pull read;
  - the monitoring merge of pull request 759 (46cc3f5, 2026-11-15 06:22:53 UTC), which changes only two files of
    `ops/deploy/monitoring/prometheus/` and is reviewed in [RC-383];
  - Prometheus of `nf-monitoring`, read over Tailscale SSH from inside its container;
  - the packet filter and the SSH rules that `nf-monitoring` received from the tailnet, read on the LXC with
    `tailscale debug netmap` and reduced there to tags, owners and ports, and the ports and the firewall of the
    LXC.

  Before the gate: [RC-254] (the plan, Go), [RC-356] (the revision, Go), [RC-365] ([ADR-0044], Go) and [RC-366] (the
  code, Go-with-conditions; its condition, every check green at the merge, was met by pull request 751).
- Checklist used: the Go/No-Go criteria of [MIL-051]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criterion 1 rests on [RC-365], criteria 4 and 5 on the tests in Ops CI, criteria 2 and 3 on the
  LXC and Prometheus, criterion 6 on the logs of CI and of the pull, criterion 7 on [ADR-0044], the policy of the
  wiki and the filter of the LXC, and criterion 8 on Ops CI, the wiki and [RC-366] with [RC-383].
- Language and domain: en / it (the Metadata rows of MIL-051). The language of the document was reviewed in [RC-254]
  and [RC-356], and the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the install of the
  pull of MIL-051 in its second wave, to the AI in chat ("Du godkender for mig"). The AI wrote the code of MIL-051
  ([RC-366]), the change of pull request 759 ([RC-383]) and this record.

**Evidence of 2026-11-15 (UTC):**

- **The first deploy by hand.** On `nf-monitoring`, the checkout `/opt/nfhotel-monitoring` had no changed file and
  stood at db4867b. It was moved to a9a96a14, the head of `main` at that time, which holds the code of pull request 751.
  `deploy-monitoring.sh` without `--apply` ended with "configuration valid". With `--apply` it recreated Prometheus and
  the scan exporter (their configuration folders had changed) and logged "the five services answer, and Prometheus
  loaded all 35 rules", the check of the six containers against their profiles with "6 container(s): 6 passed, 0
  failed, 0 warned", and "runc 1.5.3: at or above the floor of its line"; exit 0.
- **The install of the pull.** `pull-monitoring.sh --install` logged "recorded
  a9a96a145f3de909406b7d4c0be0b9385465bf4a as the deployed commit", installed `/usr/local/sbin/nfhotel-monitoring-pull`
  and the two units, and enabled the timer, which ran at once, at 06:01:36, and every 2 minutes after.
- **The documents merge deployed nothing.** The first run of the pull, at 06:01:37, logged "nothing to deploy: the
  monitoring paths of f7c2b912986eed8f0be0efe2f0485acf19170f9e are those of the deployed commit a9a96a145". The
  report gave the deployed commit, `nfhotel_monitoring_deploy_failed` 0 and no change waiting; Prometheus held the
  same three values at 06:07:59, with only `Watchdog` firing.
- **The monitoring merge deployed itself.** Pull request 759 was merged at 06:22:53 as 46cc3f5. The runs of the pull at
  06:25:03, 06:27:13 and 06:29:23 logged "waiting: the Ops CI run of the push of 46cc3f510f7161f73192cc0961b69ab2804084d3 to
  main has not passed yet"; that run, 43685173138, ended with success at 06:31:05. The run at 06:31:33 logged
  "==> deploying 46cc3f510f7161f73192cc0961b69ab2804084d3; the deployed commit is a9a96a145f3de909406b7d4c0be0b9385465bf4a", "configuration valid"
  from the check with the pinned tools at 06:33:49, the start at 06:36:06, in which only Prometheus was recreated
  (its rules had changed), "the five services answer, and Prometheus loaded all 35 rules", "6 container(s): 6
  passed, 0 failed, 0 warned", "runc 1.5.3: at or above the floor of its line" and, at 06:36:08,
  "46cc3f510f7161f73192cc0961b69ab2804084d3 runs; installing its pull script and units". The deployed commit on the LXC
  was written at 06:36:08, 13 minutes and 15 seconds after the merge, and no `failed` or `waiting-since` file was
  left. At 06:37:27 Prometheus evaluated `TaskReportMissing` with the expression of pull request 759, `backup_check`
  in it, healthy and inactive, and all 33 alerting rules were healthy. At 06:42:30
  `nfhotel_monitoring_deployed_commit_info` held only 46cc3f5 (the series of a9a96a1 from before the restart of
  Prometheus had gone stale), `nfhotel_monitoring_deploy_failed` was 0, no change was waiting, and only `Watchdog`
  fired.
- **The tests in Ops CI.** Run 43683027646 of f7c2b91 ended with success. Its job "Ops script tests
  (test-pull-monitoring.sh)" logged "RESULT: 25 passed, 0 failed", among them "a change outside the monitoring paths
  deploys nothing and asks no API", "a broken rule is refused before the start, and the checkout returns to the
  deployed commit", "a failed start returns to the deployed commit and starts it again", "a head whose Ops CI run
  failed is refused, and the report names its commit and stage" and "a change made by hand in the checkout is
  refused and kept". Its job "Ops monitoring configuration (MIL-023)" logged the seven checks of
  `check-monitoring.sh` as PASS and, for the stack test, "a broken rule file is refused by the checks", "the running
  Prometheus keeps the rules it had" and "a failed deploy in the report folder reaches Alertmanager through the scan
  exporter", each as PASS, and "RESULT: 24 passed, 0 failed".
- **No secret.** The logs of both jobs have no string of the shape of a Discord webhook, a Healthchecks.io ping URL,
  a GitHub token, a private key or a password assignment (counted, not printed). The journal of the pull from 06:22 to 06:37, 70 lines, has none either.
- **The tailnet filter of the LXC.** At about 06:50 the packet filter that `nf-monitoring` had received from the
  tailnet let in `tag:nfhotel-vm` on 9090 and 3100, the devices of the admins on 3000, and the devices of the
  members, admins or not, on every port. Its SSH rules let the devices of the admins in as root in check mode, and
  one of them also without a check. The LXC publishes Prometheus (9090, with its receiver of remote writes) and Loki
  (3100, `auth_enabled: false`) on its tailnet address with no login, and Grafana (3000) with its login;
  Alertmanager, the blackbox exporter and the scan exporter are not published. Its input policy in nftables is
  accept and `DOCKER-USER` is empty, so nothing on the host narrows what the tailnet lets in. No node with `tag:ci`
  and no rule from it is in the filter. A grant to a tag with no node online would not show there, and the policy
  itself, in the admin console of Tailscale, was not read.

## Checklist Results

| # | Criterion (MIL-051) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ADR of task 1 is `Accepted` with a `Go` review, and compares push and pull with the access each one needs | Pass | [ADR-0044] is Accepted after [RC-365] Go; it compares the CD job over tailnet SSH (a grant from `tag:ci` to `tag:monitoring` on port 22 and a deploy user) with the timer on the LXC (no inbound access) |
| 2 | A merged pull request that changes only a file in `ops/deploy/monitoring/` is running on `nf-monitoring` within 15 minutes of the merge, shown by the deployed commit on the LXC and by the rule or dashboard in Prometheus or Grafana | Pass | Pull request 759 (46cc3f5) changes two files of `ops/deploy/monitoring/prometheus/` and nothing else; merged at 06:22:53, it ran on `nf-monitoring` at 06:36:08, 13 minutes and 15 seconds later, shown by the deployed commit on the LXC, by `nfhotel_monitoring_deployed_commit_info` and by the expression of `TaskReportMissing` in Prometheus; F3 has the margin |
| 3 | A pull request that changes no file in the monitoring paths starts no monitoring deploy | Pass | Pull request 758 (f7c2b91): "nothing to deploy" at 06:01:37, and no container was recreated; in CI, "a change outside the monitoring paths deploys nothing and asks no API" |
| 4 | A deliberately broken rule file in the test is refused before the start, and the running stack keeps its previous configuration | Pass | The pull test: "a broken rule is refused before the start, and the checkout returns to the deployed commit"; the stack test, with the real tools: "a broken rule file is refused by the checks" and "the running Prometheus keeps the rules it had" |
| 5 | A failed start in the test returns to the last deployed commit, and the alert for a failed deploy reaches the test receiver | Pass | The pull test: "a failed start returns to the deployed commit and starts it again"; the stack test: "a failed deploy in the report folder reaches Alertmanager through the scan exporter", whose receiver in the throwaway stack is a stand-in |
| 6 | No secret file content, token or password appears in a workflow log, a commit or a test output | Pass | The two logs of CI and the journal of the pull: none (counted, not printed); the pull reads the API of GitHub without a token and reads no secret file; the commits of pull requests 751 and 759 hold stand-in values only |
| 7 | The tailnet policy grants no more than the ADR of task 1 records; in the push form `tag:ci` reaches `tag:monitoring` only on port 22 | Fail | The pull form needs no grant ([ADR-0044]), and none was added (task 5, [RC-366]); no node with `tag:ci` and no rule from it is in the filter of the LXC, and the pull fetches from GitHub over HTTPS, an outbound connection. But the live filter grants more than [ADR-0044] and A3 of the wiki page Monitoring record (the VM on 9090 and 3100, the admins on 3000 and by SSH in check mode): the devices of members who are not admins reach every port of the LXC, Prometheus and Loki among them, and one device of an admin has SSH as root without a check (F5) |
| 8 | The tests of the deliverable pass in Ops CI, `docs/wiki/Monitoring.md` describes the new flow, and the review of the code is Go | Pass | Ops CI as above; the wiki page Monitoring has the section on how a merged change reaches the LXC, how to pause the deploy and how to deploy by hand, with the four alerts in its runbook; [RC-366] Go-with-conditions, its condition met, and [RC-383] Go for the change of pull request 759 |

## Findings

- **F1, note: the first deploy was made by hand, as planned.** The pull deploys only from a commit it has recorded,
  so the first deploy of its own code and its install are the manual step of [RC-366] and of the wiki ("first
  install"). From then on no shell was opened on the LXC to deploy; the readings above are read-only.
- **F2, note: the change of pull request 759 reverses F3 of the review of [MIL-076].** [RC-383] records why: the
  window that kept `backup_check` out of `TaskReportMissing` closed with the backup of 2026-11-14.
- **F3, minor, open for S01: the margin of criterion 2 is small.** The 13 minutes and 15 seconds were the Ops CI run
  of the push of the merge to `main` (8 minutes and 9 seconds, from 06:22:56 to 06:31:05), the wait for the next run
  of the timer (28 seconds; the runs came about every 2 minutes and 10 seconds) and the deploy (4 minutes and 35
  seconds). Of the deploy, 4 minutes and 33 seconds were the check with the pinned tools (promtool, amtool and loki
  in their pinned images), run twice: at the check stage of the pull (2 minutes and 16 seconds) and again inside
  `deploy-monitoring.sh --apply` (2 minutes and 17 seconds); the start took 2 seconds. With that deploy, an Ops CI
  run of more than 8 minutes and 15 seconds can, and one of more than 10 minutes and 25 seconds will, take a merge
  past the 15 minutes, depending on where the timer stands. The nine Ops CI runs of pushes to `main` since the code
  of pull request 751 merged (9de9be4, 2026-11-14 17:26) took from 7 minutes and 12 seconds to 12 minutes and 5
  seconds, three of them more than 8 minutes and 15 seconds, so not every merge will make it. Running the check once
  would win about 2 minutes and 15 seconds; that is a change outside the accepted plan, so it is S01's to plan or to
  leave. Criterion 2 asks that a merged pull request runs within 15 minutes, and this one did.
- **F4, open for S01: the new required check.** F8 of [RC-366] stays open: "Ops script tests
  (test-pull-monitoring.sh)" is not a required check, and only S01 changes the ruleset.
- **F5, blocking (criterion 7): the tailnet grants more than the ADR records.** The devices of the members of the
  tailnet who are not admins reach every port of `nf-monitoring`, and with them Prometheus and Loki, which take reads
  and writes without a login; one device of an admin reaches it by SSH as root without the check of A3. Rated Medium
  by the reviewer, as the project names no rating scheme: only members of the tailnet reach the LXC, but they can
  read and write every metric and every log line. The grants are older than MIL-051, which added none. A3 named a
  broad grant to the members as possible and left narrowing it to a decision for the whole tailnet, but weighed only
  Grafana, which has a login. On 2026-11-15 S01, who keeps the tailnet, chose a risk exception in place of narrowing
  the grant now: owner S01, expiry 2026-11-29. By [PRC-001] its approver is the escalation role, never the author or
  the owner: S04, the approver of the artifacts in the Stakeholder Analysis. The AI, his stand-in, may not approve
  it, so it is not valid until S04 does, and this record has no table of risk exceptions yet. The compensating
  controls proposed with it: Prometheus and Loki are reached only on the tailnet, not from the internet; Grafana
  keeps its login; the `Watchdog` pings Healthchecks.io every minute, so a Prometheus or an Alertmanager that is
  taken down is noticed; the filter of the LXC is read again, as tags, owners and ports only, at the expiry and at
  every gate that touches the monitoring. Nothing protects the integrity of the metrics and the logs: a member can
  write false ones.

## Overall Verdict

**No-Go for now.** Criteria 1 to 6 and 8 pass; criterion 7 fails (F5), and as a Go/No-Go criterion of the gate it
blocks. The risk exception that S01 chose covers it only once S04 approves it in his own words; then the gate is
checked again in a delta re-review, which adds the table of risk exceptions, and the gate row of [MIL-051] becomes
`Accepted`. Narrowing the grant to the grants of A3 also passes criterion 7, with no exception. Until then
[MIL-051] is unchanged and its task 7 stays open. Criterion 2 rests on the one monitoring merge made so far (F3).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Approve or reject the risk exception of F5 (owner S01, expiry 2026-11-29), in his own words | S04 | 2026-11-29 |
| Narrow the grant so that `tag:monitoring` is reached only through the grants of A3 of the wiki page Monitoring, or keep the risk until the expiry of the exception (F5) | S01 | 2026-11-29 |
| Add "Ops script tests (test-pull-monitoring.sh)" to the required checks (F4, F8 of [RC-366]) | S01 | 2026-11-28 |
| Decide whether the pull runs the check with the pinned tools once, so a merge reaches the LXC about 2 minutes sooner (F3) | S01 | 2026-11-28 |

---

[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[MIL-076]: ../../milestones/mil-076-every-nightly-backup-read-back.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0044]: ../../adr/adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[RC-254]: ./rc-254-mil-051-plan.md
[RC-356]: ./rc-356-mil-051-revision.md
[RC-365]: ./rc-365-adr-0044.md
[RC-366]: ./rc-366-mil-051-code.md
[RC-383]: ./rc-383-mil-051-first-pull-deploy.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[9da77ab]: https://github.com/NF-Hotel/NFHotel/commit/9da77ab7a72173754403f906237c210c1874d44c
