# RC-179 Review of the monitoring stack, Alloy, and the alerts and watchdog (MIL-023 tasks 7 to 9)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-179 |
| CrossReference | [MIL-023], [ADR-0020], [ADR-0018], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - **Task 7 (NF-Hotel/NFHotel#238).**
    - `ops/deploy/monitoring/`: compose file, Prometheus configuration, rules and their unit tests, Alertmanager, blackbox, Loki, Grafana provisioning and dashboard, settings example, secrets README.
    - `ops/scripts/create-monitoring-lxc.sh` and `ops/scripts/deploy-monitoring.sh`.
    - `ops/tests/check-monitoring.sh`, and the CI job "Ops monitoring configuration (MIL-023)".
    - The wiki page `docs/wiki/Monitoring.md`.
  - **Task 8 (NF-Hotel/NFHotel#239).**
    - `ops/deploy/alloy/`: compose file with a read-only socket proxy, `config.alloy`, settings example.
    - Alloy as an optional service of `ops/scripts/deploy-services.sh`.
  - **Task 9 (NF-Hotel/NFHotel#240).**
    - The job reports of `backup-database.sh` and `renew-certificate.sh`.
    - `ops/scripts/report-health.sh` with `nfhotel-report-health.service` and `.timer`, installed and enabled by the update command (`deploy-release.sh`).
    - `ops/tests/test-report-health.sh`, and the new checks in `test-backup-target.sh`, `test-renew-certificate.sh` and `test-deploy-release.sh`.
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001], the rows that the change touches. MIL-023 criteria 7 and 8 are used for the content.
- **Scope:** full review of the new files, delta review of the changed scripts, tests and `ops-ci.yaml`.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). Nothing was changed on `nf-hotel` or on the Proxmox node.
- **Tools:**
  - `ops/tests/lint.sh` (actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1);
  - `ops/tests/check-monitoring.sh` with the pinned images;
  - the script tests in `debian:12-slim` on Docker Desktop.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, strict mode, quoting, `[[ ]]` | Pass | `report-health.sh` reads the docker output into a variable before `mapfile`, because a failure inside `<(...)` would not stop the script. The test "a failed docker keeps the old report" found that case |
| SH 4 | shellcheck and bash -n | Pass | 0 findings. SC2174 (`mkdir -p -m`) is avoided with `install -d -m 0755` |
| SH 5 | Errors and usage | Pass | A failed report write only warns, so the monitoring can never fail a backup or a renewal. `report-health.sh` exits 1 when docker fails and keeps the old file |
| SH 6 | Temporary files | Pass | Each report is written to a temporary file and moved into place, so Alloy never reads half a file |
| SH 7 | No secret | Pass | The webhook, ping URL and Grafana password are files in a git-ignored `secrets/` folder, with mode 0600 checked by `deploy-monitoring.sh`. CI uses stand-in values |
| SH 8 | Explicit flag for external changes | Pass | `create-monitoring-lxc.sh` and `deploy-monitoring.sh` are dry runs without `--apply` |
| SH 10–11 | Implements a cited task; behaviour is tested | Pass | Results: `test-report-health.sh` 11, `test-backup-target.sh` 17, `test-renew-certificate.sh` 14, `test-deploy-release.sh` 59, `test-deploy-services-env.sh` 7, `test-vm-setup.sh` 19, all passed. A failed copy reports the backup but not the copy, and a failed renewal reports nothing |
| SH 12 | shfmt | Pass | No diff |
| SH 15 | Defined once | Pass with note | `report_success` is in both `backup-database.sh` and `renew-certificate.sh`, because each is installed as a single file (the reason `die` is copied too). The format is the same, and both tests check it |
| WF 4, 7 | Header comment; least permissions | Pass | The Ops CI header names the monitoring check. The job adds no permission |
| YML | yamllint, readable rules | Pass | Long rule expressions and annotations are folded (`>-`); `promtool test rules` shows that they still match |
| MIL-023 criterion 7 timing | A stopped web container alerts within 3 minutes | Pass in design | The report runs every minute, the scrape and evaluation every 30 s, the rule uses `for: 30s`, and `group_wait` is 30 s, about 3 minutes at worst. A starting container counts as healthy, so a deploy raises no alert. The alarm drill (task 10) measures it |
| Bug found | `deploy-monitoring.sh` image lookup | Fixed | The `sed` expression used `/` as its delimiter, but the image names hold a slash, so every check would have failed on the LXC. It now uses `|`. `check-monitoring.sh` runs the same lookup in CI |

Also reviewed, in their own pull requests:

- #251 waits for the proxy and retries the smoke test.
- #252 gives nginx `DAC_READ_SEARCH`. It is the cause of the two rolled-back deploys: root without `DAC_OVERRIDE` could not open the 0600 key of the checkout user. It was reproduced with the pinned image and has a regression test in `test-nginx.sh`.

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Create the LXC, the tailnet grants, the Discord webhook and the Healthchecks.io check, as the wiki page Monitoring says; then put `alloy.env` on `nf-hotel` | S03 | Before task 10 |
| 2 | Run the alarm drill of criterion 7 and record the times | S03 with the AI | Task 10 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-and-alerting.md
[ADR-0018]: ../../adr/adr-0018-cd-installs-the-operations-commands.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
