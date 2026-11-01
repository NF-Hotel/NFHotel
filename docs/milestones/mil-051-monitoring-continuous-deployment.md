# MIL-051 Continuous deployment of the monitoring stack

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-051 |
| CrossReference | [BC-001], [ADR-0007], [ADR-0020], [MIL-052] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-11-01 that a change to Grafana, Prometheus, Alloy or Loki asked for in chat reaches the monitoring host without a manual step, the same way code reaches production ("planlæg begge veje"); accepted after RC-254 Go | pending |

---

## Purpose

Decide whether a change to the monitoring configuration in git reaches the monitoring host `nf-monitoring` by itself after the merge, as the application does since ADR-0007, so that a change asked for in chat becomes a pull request and, once S01 merges it, a running configuration without anyone opening a shell on the LXC.

Today `ops-ci.yaml` checks the configuration of `ops/deploy/monitoring/` (the `monitoring` job, `check-monitoring.sh` and `test-monitoring-stack.sh`), but S03 still runs `deploy-monitoring.sh --apply` by hand on the LXC (ADR-0020, MIL-023 task 7). A merged change can therefore wait unapplied, and the running stack can differ from `main` without anyone noticing. Grafana Alloy on the VM is out of scope: it is already deployed by the release deploy (`deploy-release.sh` runs `deploy-services.sh`, which includes `alloy`).

## Deliverable

- **ADR-0035, how the change reaches the LXC.** It compares at least:
  - *push*: a job in GitHub Actions joins the tailnet as `tag:ci` (OIDC, as `cd.yaml` does) and runs the deploy over SSH, which needs a new grant from `tag:ci` to `tag:monitoring` on port 22 and a deploy user that is not `root`;
  - *pull*: a systemd timer on the LXC fetches `main`, verifies that the commit is on `main`, and runs `deploy-monitoring.sh --apply` when `ops/deploy/monitoring/` changed, which needs no new inbound access at all.

  It records the choice, the tailnet policy it needs (or that none is needed), and the reason.
- **The automatic deploy** in the chosen form:
  - it runs only when `ops/deploy/monitoring/` or `ops/scripts/deploy-monitoring.sh` changed on `main`;
  - it deploys only a commit that is on `main` and whose `ops-ci.yaml` run is green;
  - it runs the existing checks of `deploy-monitoring.sh` (`promtool check config`, `promtool test rules`, `amtool check-config`, `loki -verify-config`) before anything starts;
  - after the start it checks that Prometheus, Alertmanager, Loki, Grafana and the blackbox exporter answer their ready endpoints, and that Prometheus has loaded the new rule files;
  - on any failure it goes back to the last deployed commit and starts that again;
  - it records the deployed commit on the LXC, so `main` and the running stack can be compared.
- **An alert when the deploy fails or lags behind `main`**, to Discord through the existing Alertmanager route, with a runbook line in `docs/wiki/Monitoring.md`.
- **The secrets stay on the LXC.** The three secret files of `ops/deploy/monitoring/secrets/` are never read by the CI job or written to its log.
- **Tests** in `ops/tests` (run by `ops-ci.yaml`): a change outside the monitoring paths does not deploy; a broken rule file is refused before the start; a failed start returns to the previous commit.
- **Documentation:** `docs/wiki/Monitoring.md` gets a section on how a change reaches the LXC, and the step "S03 runs `deploy-monitoring.sh --apply`" becomes the fallback for a first install and for an emergency.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0035 is `Accepted` with a `Go` review, and compares push and pull with the access each one needs | Yes | No |
| 2 | A merged pull request that changes only a file in `ops/deploy/monitoring/` is running on `nf-monitoring` within 15 minutes of the merge, shown by the deployed commit on the LXC and by the rule or dashboard in Prometheus or Grafana | Yes | No |
| 3 | A pull request that changes no file in the monitoring paths starts no monitoring deploy | Yes | No |
| 4 | A deliberately broken rule file in the test is refused before the start, and the running stack keeps its previous configuration | Yes | No |
| 5 | A failed start in the test returns to the last deployed commit, and the alert for a failed deploy reaches the test receiver | Yes | No |
| 6 | No secret file content, token or password appears in a workflow log, a commit or a test output | Yes | No |
| 7 | The tailnet policy grants no more than ADR-0035 records; in the push form `tag:ci` reaches `tag:monitoring` only on port 22 | Yes | No |
| 8 | The tests of the deliverable pass in Ops CI, `docs/wiki/Monitoring.md` describes the new flow, and the review of the code is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0020 | The monitoring stack, its LXC and `deploy-monitoring.sh` |
| ADR-0007 | The CD job, its tailnet identity `tag:ci` and the OIDC login, reused in the push form |
| MIL-023 | Created the LXC and the manual deploy that this milestone automates |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23: a change reaches the server only after the required CI checks pass, over a private connection; here for the monitoring configuration | Tasks 1 to 3, 5 to 7 |
| BC-001 objective 25: observe the server, with its operations managed as code | Tasks 2, 4, 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-13. It changes no application code and no hosted service, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0035: push or pull deploy of the monitoring | Compare a CD job over tailnet SSH with a timer on the LXC that pulls `main`: the access each needs, how each proves the commit is reviewed, and how each rolls back. Record the choice and the tailnet policy. | No | criteria 1, 7 |
| 2 | Automatic deploy of `ops/deploy/monitoring/` | Implement the chosen form: run only on changes to the monitoring paths, only for a green commit on `main`, with the existing checks before the start and the deployed commit recorded on the LXC. | No | criteria 2, 3, 6 |
| 3 | Health check and rollback | After the start, check the ready endpoints of Prometheus, Alertmanager, Loki, Grafana and the blackbox exporter and that the rules loaded; on failure, start the last deployed commit again. | No | criteria 4, 5 |
| 4 | Alert on a failed or lagging deploy | A rule and a runbook line for a monitoring deploy that failed or that is behind `main`, routed to Discord through the existing Alertmanager receiver. | No | criterion 5 |
| 5 | Tailnet policy | Add only the grant ADR-0035 needs (none for pull; `tag:ci` to `tag:monitoring` on port 22 for push), and update the policy in `docs/wiki/Monitoring.md`. | No | criterion 7 |
| 6 | Tests in Ops CI | Tests in `ops/tests` for the path filter, the refused broken rule and the rollback, run by `ops-ci.yaml` in a throwaway container. | No | criteria 3 to 5, 8 |
| 7 | Wiki and release | `docs/wiki/Monitoring.md` describes the new flow and keeps the manual deploy as a fallback; review record; first real deploy through the new flow; gate review. | No | criteria 2, 8 |

---

[BC-001]: ../business-case.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-052]: mil-052-read-only-grafana-for-the-ai-assistant.md
