# ADR-0044 The monitoring deployed by a pull on its host

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0044 |
| CrossReference | [MIL-051], [ADR-0020], [ADR-0007], [ADR-0018], [MIL-023], [MIL-053], [RC-356] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Proposed | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-051, planned at S01's word in chat on 2026-11-01 and accepted with the plan of 2026-11-14 ("Du godkender for mig", RC-356 Go); written by the AI | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** On 2026-11-01 S01 asked in chat that a change of Grafana, Prometheus, Alloy or Loki asked for in chat
reaches the monitoring host without a manual step, the same way code reaches the virtual machine (VM). [MIL-051] plans
it. S01 gave the acceptance of the plan of 2026-11-14 to the artificial intelligence (AI) assistant in chat ("Du
godkender for mig"), and the revision of the plan was reviewed in [RC-356] (Go), which expected the pull form. This
record is task 1 of [MIL-051].

**The problem.** The monitoring runs in the Linux container (LXC) `nf-monitoring` from a checkout of the repository in
`/opt/nfhotel-monitoring` ([ADR-0020]). Ops CI checks its configuration on every pull request, but the configuration
reaches the LXC only when a person runs `deploy-monitoring.sh --apply` there ([MIL-023] task 7). A merged rule can
wait unapplied, and nothing tells that the running stack differs from `main`. Grafana Alloy on the VM is out of scope:
the release deploy already installs it.

**Forces.**

- The LXC is reached only over the tailnet. Its grants let the VM send metrics and logs to it, let it probe the VM,
  and let the admins open Grafana and Secure Shell (SSH) as root; the tailnet identity of the continuous deployment
  (CD) job, `tag:ci`, has no grant to it.
- The LXC holds the secrets of the monitoring: the Discord webhook, the ping address of Healthchecks.io, the password
  of Grafana and the optional token of the scan exporter. No deploy may carry them, and no CI job may see them.
- The repository is public. A host can fetch `main` over HTTPS and read the results of its workflows from the
  application programming interface (API) of GitHub without a credential, 60 calls an hour from one address.
- A commit reaches `main` only through a pull request whose required checks are green, and the push of the merge runs
  Ops CI again on the merged commit: 5 minutes 17 seconds for de82d34 on 2026-11-14.
- A deploy that breaks the monitoring breaks the alerting too. The Watchdog tells Healthchecks.io when Alertmanager
  stops, not when a rule is wrong or a service does not answer, so a failed start must go back to what ran before.
- Bash reads a script while it runs it, so a checkout must not change the file of the script that is running.

**Options considered.**

1. **Push.** A job in GitHub Actions joins the tailnet as `tag:ci` with OpenID Connect (OIDC), as `cd.yaml` does, and
   runs the deploy over SSH. It needs a new grant from `tag:ci` to `tag:monitoring` on port 22 and a deploy user that
   is not root, with a forced command as on the VM ([ADR-0007]). The deploy starts seconds after the checks, and a
   compromised workflow reaches the LXC.
2. **Pull.** A timer on the LXC fetches `main`, checks that the commit is on `main` and that its Ops CI run passed,
   and deploys when the monitoring paths changed. It needs no inbound access and no credential; the delay is the Ops
   CI run of the merge plus the interval of the timer.
3. **Keep the manual deploy** and only alert when the LXC is behind `main`. A person is still needed for every change.

## Decision

The monitoring is deployed by a pull on its host: every 2 minutes a timer on `nf-monitoring` fetches `main`, deploys
the head of `main` when the monitoring paths changed and its Ops CI run passed, and goes back to the commit that ran
before when a check or the start fails.

1. **What runs.** `nfhotel-monitoring-pull.timer` starts `nfhotel-monitoring-pull.service` every 2 minutes, which runs
   `/usr/local/sbin/nfhotel-monitoring-pull --apply`, an installed copy of `ops/scripts/pull-monitoring.sh`. It runs
   as root, as the manual deploy does, under a lock, so two runs never overlap.
2. **Which commit.** It fetches `origin/main` of the public repository over HTTPS and takes its head. It deploys only
   when a file of the monitoring paths differs between the deployed commit and that head: `ops/deploy/monitoring/`
   (with the units of the timer), `ops/scripts/deploy-monitoring.sh`, `ops/scripts/pull-monitoring.sh`,
   `ops/scripts/check-container-runtime.sh`, `ops/scripts/lib/` and `ops/security/container-profiles.tsv`. Any other
   change leaves the LXC as it is.
3. **Only a green commit of `main`.** The deployed commit must be an ancestor of the head, and the Ops CI run of the
   push of the head to `main` must have passed, read from the API of GitHub without a token. While that run is not
   finished, the timer waits; a run that failed refuses the commit, and the same head is not tried again.
4. **The checks first.** A checkout in which a tracked file was changed by hand is refused, because `git checkout`
   would carry the change over to the head and the stack would start what no review saw. Otherwise it checks out the
   head, detached, and runs `deploy-monitoring.sh` without `--apply`: `promtool check config`, `promtool test rules`,
   `amtool check-config` and `loki -verify-config`. A failed check returns the checkout to the deployed commit, and
   nothing starts.
5. **Start, then health.** `deploy-monitoring.sh --apply` starts the stack and now also checks that Prometheus,
   Alertmanager, Loki, Grafana and the blackbox exporter answer their ready endpoints and that Prometheus has loaded
   every rule of the rule files, so a manual deploy is checked the same way.
6. **Back on failure.** When the start or its checks fail, it checks out the deployed commit and runs
   `deploy-monitoring.sh --apply` again.
7. **Recorded and reported.** The deployed commit is kept in `/var/lib/nfhotel-monitoring`. The report
   `monitoring_pull.prom` gives the deployed commit, the time of the last run that ended well, whether the last
   attempt failed, and since when a change waits. The scan exporter of [MIL-053] serves every report of a mounted
   folder after its own metrics, so Prometheus reads it with the job `scans`, and no new service or image is needed.
8. **Alerts.** `MonitoringDeployFailed` (the last attempt failed), `MonitoringDeployBehind` (a change of the
   monitoring paths has waited 30 minutes), `MonitoringDeployStale` (no run has ended well for an hour) and
   `MonitoringDeployReportMissing` go to Discord through the existing route.
9. **It updates itself.** After a deploy, the script of the new commit installs itself and the units
   (`pull-monitoring.sh --install`), so a change of them is deployed like any other; the copy is replaced by a rename,
   which the running script does not see, as with the deploy command of the VM ([ADR-0018]).
10. **The tailnet policy does not change.** The LXC reaches GitHub outbound, as it does for its images and updates.
11. **The manual deploy stays** for the first install and for an emergency: `deploy-monitoring.sh --apply`, then
    `pull-monitoring.sh --install` once.

## Consequences

**Positive:**

- A merged change of the monitoring runs on the LXC about 10 minutes after the merge (the Ops CI run, the timer and
  the deploy), without a shell on the LXC.
- No new access: CI gets no way to the LXC, and the secrets never leave it.
- A configuration that fails a check never starts, a start that fails goes back to what ran, and an alert tells
  either, and also when the LXC lags behind `main`.

**Negative:**

- The LXC trusts GitHub for the content of `main` over HTTPS and for the result of Ops CI; a takeover of the
  repository would reach the monitoring, as it would reach the VM through CD.
- A deploy waits for the Ops CI run of the merge, and a change that only Ops CI on the merge refuses stays undeployed
  until a new commit, with `MonitoringDeployFailed` open.
- The checks cannot catch a rule that is valid but wrong; the rule tests stay the guard.
- The scan exporter serves a second kind of metric, so a fault in it also hides the report of the deploy; its
  absence raises `MonitoringDeployReportMissing`.

**Quality:** reliability (availability of the alerting, recoverability), security (no new access, no secret in CI) and
maintainability (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-051]: tasks 2 to 7 carry out this decision.
- [ADR-0020]: the monitoring is deployed by the pull; the manual deploy becomes the fallback.
- [MIL-053]: the scan exporter also serves the folder of reports.

---

[MIL-051]: ../milestones/mil-051-monitoring-continuous-deployment.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[MIL-053]: ../milestones/mil-053-security-scan-metrics.md
[RC-356]: ../sqa/reviews/rc-356-mil-051-revision.md
