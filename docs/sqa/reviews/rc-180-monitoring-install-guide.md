# RC-180 Review of the monitoring installation guide, the secret owners and the missing-report alerts (MIL-023 tasks 7 and 9)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-180 |
| CrossReference | [MIL-023], [ADR-0020], [RC-179], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [6133e29] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the wiki page `docs/wiki/Monitoring.md`, rewritten as a step-by-step installation with the reason for each choice, at S01's request;
  - the secret handling of `ops/scripts/deploy-monitoring.sh` and `secrets.example/README.md`;
  - the new rules `TaskReportMissing` and `HealthReportMissing`, and their tests in `rules-test.yml`;
  - `ops/tests/test-monitoring-stack.sh` (new), and its step in the job "Ops monitoring configuration".
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001], the rows that the change touches. ADR-0020 is used for the content.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). Nothing was changed on a host.

## Why

Two defects were found while the installation steps were written and checked against the code:

1. **The containers could not read their secrets.**
   - The steps make the secret files 0600 and owned by root.
   - Alertmanager runs as `nobody` and Grafana as 472 in their images, and `cap_drop: [ALL]` removes the override.
   - So the stack would have failed on the LXC. This is the same class of defect as the nginx key of #252.
   - The fix: `deploy-monitoring.sh --apply` gives each file to the one container that reads it, mode 0400, and the folder becomes 0711.
2. **A missing report raised no alert.**
   - ADR-0020 decision 4 says a missing report alerts, but a rule on the age of a report cannot fire for a report that does not exist.
   - The fix adds `TaskReportMissing` and `HealthReportMissing`. They fire only while `nf-hotel` sends metrics, so a VM that is down raises `NoMetricsFromNfHotel` alone.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 5, 8 | Errors; explicit flag for external changes | Pass | The owners change only with `--apply`, and only as root. The dry run checks that no group or other user can read a file |
| SH 11 | Behaviour is tested | Pass | `test-monitoring-stack.sh` follows the wiki steps on the CI runner, as root, with stand-in secrets. It checks: the five services run; each container reads its own secret and Alertmanager cannot read the Grafana password; the Grafana login works with the file's password; Prometheus loaded the rules; the Watchdog reaches Alertmanager |
| Found by the new test | Grafana answered before its first migration ended | Fixed | `up --wait` returned while Grafana was still migrating, because it had no health check, so the first login failed. Grafana now has a health check on `/api/health`, and the test passes 10 of 10 |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |
| YML | Rules tested | Pass | `promtool test rules`: a missing report fires while `nf-hotel` sends metrics, and it does not fire when no metrics arrive |
| WF 7 | Least permissions | Pass | `sudo` changes only the runner's own containers and files of the checkout, and the step's comment says so |
| Guide | Steps follow the code | Pass | Each command was checked against `create-monitoring-lxc.sh`, `deploy-monitoring.sh`, the compose files and the policy of RC-161. The secrets are read with `read -s`, so they stay out of the shell history and the screen |

## Overall Verdict

Go.

## Action Items

None beyond RC-179 action 1 (the installation by S03), which now follows this guide.

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[RC-179]: rc-179-mil-023-monitoring.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[6133e29]: https://github.com/NF-Hotel/NFHotel/commit/6133e2940dabe8874ae0428b338381e153654461
