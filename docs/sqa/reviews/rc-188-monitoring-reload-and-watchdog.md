# RC-188 Review of the configuration reload of deploy-monitoring.sh and the Watchdog timing (MIL-023 tasks 7, 9 and 10)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-188 |
| CrossReference | [MIL-023], [ADR-0020], [QC-SH-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - the configuration hashes in `ops/deploy/monitoring/docker-compose.yml` and `ops/scripts/deploy-monitoring.sh`;
  - the Watchdog route in `alertmanager/alertmanager.yml`;
  - the rule-change check in `ops/tests/test-monitoring-stack.sh`;
  - part E of the wiki page Monitoring.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

The alarm drill of 2026-10-24 (MIL-023 task 10) found two defects.

**1. A changed rule never took effect.**

- For the certificate drill, S03 raised the threshold in the LXC's copy of the rules and ran `deploy-monitoring.sh --apply`.
- All five containers stayed as they were ("Healthy"), and none was recreated.
- The configuration files are bind mounts, so compose sees no change. Prometheus reads its rules only when it starts or reloads.

So the documented way to change the monitoring (`git pull`, then `deploy-monitoring.sh --apply`) would never have applied a changed rule, route or probe.

The fix uses the pattern of the proxy on `nf-hotel` (MIL-021):

- each service gets `CONFIG_HASH`, a hash of its configuration folder;
- compose then recreates exactly the services whose folder changed;
- the data volumes stay.

**2. The Watchdog pinged every 2 minutes.** Healthchecks.io showed a ping every 2 minutes, while the check expects one each minute.

- Alertmanager sends a repeat only at a `group_interval` tick. With 1m and 1m, the tick came just before the repeat was due.
- With a 30 s tick and a 50 s repeat, the ping goes out each minute.

The measured time from a stopped Prometheus to the Healthchecks.io alert was about 8 minutes, not the 6 that the page stated. Alertmanager keeps the Watchdog until it expires, and only then does Healthchecks.io wait its period and grace. The page now gives the measured times.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The defect is tested | Pass | `test-monitoring-stack.sh` now changes the certificate threshold as the drill does, runs `deploy-monitoring.sh --apply` again, and checks that Prometheus serves the new rule. Before the fix this check could not pass, because no container was recreated |
| SH 5 | Errors | Pass | A folder that cannot be hashed stops the deploy before compose runs |
| YML | Checked | Pass | `promtool`, `amtool` and the other checks of `check-monitoring.sh` pass, and the linters are clean |
| Wiki | Drill steps | Pass | Each drill says that its ending step runs only after the alert, with the reason. The certificate drill gives the exact commands. Times are in UTC |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | In the LXC, `git pull --ff-only` and `deploy-monitoring.sh --apply` after the merge; then repeat the certificate drill | S03 with the AI | After the merge |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
